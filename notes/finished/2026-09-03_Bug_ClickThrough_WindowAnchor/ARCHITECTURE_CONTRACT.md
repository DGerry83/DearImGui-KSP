# Architecture Contract: ISSUES #001 (pointer blocker), #002 (viewport clamp setting), unit tests
## Date: 2026-09-03
## Type: Bugfix (#001, #002) + Feature (unit tests)
## Related Investigation: [INVESTIGATION_LOG.md](INVESTIGATION_LOG.md) · Worksheet: [PLANNING_WORKSHEET.md](PLANNING_WORKSHEET.md)

### Change Specifics

**#001 (bug, P2)** — clicks pass through ImGui windows to KSP uGUI below.
- Root cause: ImGui windows are not uGUI objects; EventSystem graphic raycasts never see them (confirmed 2026-08-31; spike 2026-09-03 confirmed all stock clickable UI routes through GraphicRaycaster, no PhysicsRaycaster exists).
- Trigger: any ImGui window overlapping a uGUI element (or world-pick target gated by `IsPointerOverGameObject()`).
- First appearance: since first in-game build; found at C9b.
- Data loss risk: none.

**#002 (bug, P3)** — windows anchored top-left can end nearly off-screen after a resolution reduction.
- Root cause: pixel-absolute consumer-owned window positions; DisplaySize changes underneath (architectural, not a viewport-path defect).
- Spec tension resolution (user decision 2026-09-03): the clamp is a **player-facing library behavior** governed by the library-owned global `settings.cfg` (spec §4.4), not a consumer-API positioning rule — spec §6.2 stands unchanged for the API. Record as decision D21 candidate ("viewport clamp is library policy, opt-out via settings.cfg, default on").
- Data loss risk: none.

**Unit tests (feature)** — external review finding 5; user pulled forward 2026-09-03. Scope: Application layer only, xUnit. Success: `dotnet test` green locally, covering the pure-C# Application types, catching the state-machine/registry/fault-barrier/settings regression classes without the game.

### Structural Invariants (from INTEGRATION_CONTRACT "Locked" + handoff §3)
- Public API additive-only within major version; `DearImGuiKSP/Application/DearImGuiKSP.cs` surface must be **unchanged** by this session (no new public members needed — the clamp setting is edited in settings.cfg per spec §6.1).
- Layer rules: `Application/` + `Interop/` Unity-free; only `Infrastructure/` touches KSP/Unity. The new blocker is the first UnityEngine.UI usage — it lives in Infrastructure only, behind `IPointerBlockerGateway`.
- Managed/native handshake lockstep: **bump 3 → 4 on both sides** (`NativeBridge.ExpectedNativeVersion`, `DearImGuiKSPNative_GetVersion`); mismatch must still fail safely with the version-mismatch popup.
- Failure UX (spec §5.4/§7) and failure-init paths untouched.
- Compatibility with Deferred (hard), TUFX, Scatterer, Parallax, Cinematic Shaders, Cinematic Recorder, IMGUI mods — verified in-game in the D16 environment (gates G2/G7).
- Minimal change: no refactoring of adjacent code; no "while I'm here" work.
- Settings format: adding `clampWindowsToViewport` is backward compatible at **formatVersion 1** (missing key → default; older library versions ignore unknown keys). No format bump.

### Files to Modify
| File | Change Type | Invariants Applied | Risk | Lines (Est.) |
|------|-------------|--------------------|------|--------------|
| `DearImGuiKSP/Application/Interfaces/IPointerBlockerGateway.cs` | Add | Layer rule (interface only) | Low | ~15 |
| `DearImGuiKSP/Application/InputCaptureTracker.cs` | Modify | Unity-free; transition-only toggling | Low | ~25 |
| `DearImGuiKSP/Infrastructure/PointerBlockerGateway.cs` | Add | Only UnityEngine.UI usage, Infrastructure-only | Medium | ~80 |
| `DearImGuiKSP/Infrastructure/InputLockGateway.cs` | Modify | ComputeMask gains `MAIN_MENU` when MouseCaptured | Medium | ~5 |
| `DearImGuiKSP/Infrastructure/Composition.cs` | Modify | Wire blocker gateway; resolution handler calls clamp when setting on | Low | ~15 |
| `DearImGuiKSP/LibraryConfig.cs` | Modify | `DefaultClampWindowsToViewport = true` constant | Low | ~2 |
| `DearImGuiKSP/Application/LibrarySettings.cs` | Modify | New bool field | Low | ~2 |
| `DearImGuiKSP/Application/SettingsModel.cs` | Modify | New property, persisted on change | Low | ~15 |
| `DearImGuiKSP/Infrastructure/SettingsStore.cs` | Modify | Read/write `clampWindowsToViewport` key | Low | ~5 |
| `GameData/DearImGuiKSP/settings.cfg` | Modify | Shipped default gains the key | Low | ~1 |
| `DearImGuiKSP/Application/Interfaces/INativeBridge.cs` | Modify | Add `ClampWindowsToViewport()` (internal iface) | Low | ~8 |
| `DearImGuiKSP/Infrastructure/NativeBridge.cs` | Modify | Bind new export; bump ExpectedNativeVersion to 4 | Medium | ~20 |
| `DearImGuiKSPNative/src/ContextHost.cpp/.h` | Modify | Clamp implementation over GImGui->Windows | Medium | ~40 |
| `DearImGuiKSPNative/src/DearImGuiKSPNative.cpp` | Modify | New export + GetVersion returns 4 | Low | ~10 |
| `DearImGuiKSP/DearImGuiKSP.csproj` | Modify | `InternalsVisibleTo("Application.Tests")` | Low | ~3 |
| `tests/Application.Tests/` (csproj + test classes) | Add | xUnit, net48, ProjectReference to library | Low | ~400 |
| `DearImGui-KSP.slnx` | Modify | Add Application.Tests project | Low | ~2 |
| `tests/Infrastructure.Tests/README.md`, `tests/Core.Tests/README.md` | Modify | Record deferral decisions (user requirement) | Low | ~4 |

### Fix Strategy

**#001 — pointer blocker (minimal, spike-validated)**
- Approach: `PointerBlockerGateway` creates one GameObject (`DearImGuiKSP.PointerBlocker`, DontDestroyOnLoad) with a Screen Space - Overlay Canvas (sortOrder 30000, per TMPro precedent), a GraphicRaycaster, and a full-screen alpha-0 Image with raycastTarget=true. `SetBlocked(true/false)` toggles GameObject active state. `InputCaptureTracker` drives it from `InputCaptureState.MouseCaptured`, firing only on transitions; `ReleaseAll()` forces unblocked. `InputLockGateway.ComputeMask` adds `ControlTypes.MAIN_MENU` when MouseCaptured (closes the main-menu 3D-button gap found by the spike).
- Why full-screen-while-hovered and not per-window rects: `io.WantCaptureMouse` stays true through active drags out of a window; no native changes; zero per-frame rect sync. Per-frame cost: one bool compare; SetActive only on transitions.
- Rollback: remove the SetBlocked call (one line) — locks behavior unchanged from current release.
- Similar-code search: any other place capture state is consumed (only the tracker).

**#002 — viewport clamp (opt-out setting)**
- Approach: on `GameEventHooks.ResolutionChanged`, after `Bridge.RebuildViewport(w,h)`, call `Bridge.ClampWindowsToViewport()` iff `Settings.ClampWindowsToViewport`. Native export iterates the ImGui context's window list and clamps each window's Pos so a window that fits the new viewport ends fully inside it (corner-anchored clamp: pos = min(pos, display − size), floored at 0); oversized windows pin top-left at 0. Skip the ImGui debug/hidden windows as appropriate.
- Rollback: setting `clampWindowsToViewport = false` restores prior behavior without a rebuild.
- Similar-code search: none — first consumer of window enumeration.

**Unit tests**
- Approach: `tests/Application.Tests/Application.Tests.csproj` (net48, xunit 2.x, xunit.runner.visualstudio, Microsoft.NET.Test.Sdk) with a ProjectReference to `DearImGuiKSP.csproj`; `InternalsVisibleTo("Application.Tests")` added to the library csproj. Suites: LifecycleStateMachine (transitions, illegal-transition handling), ConsumerRegistry (register/unregister/ordering/enabled), FaultBarrier (throw → disable after threshold, isolation of other consumers), SettingsModel (clamping, defaults, change notification, persist-on-change with a fake ISettingsStore; new ClampWindowsToViewport included), InputCaptureTracker (fake IInputLockGateway + IPointerBlockerGateway: transition-only toggling, ReleaseAll).
- **Documented deferrals (user requirement)**: `tests/Infrastructure.Tests` SettingsStore ConfigNode round-trip deferred — it needs Assembly-CSharp (KSP install) references at test runtime; record in that folder's README. `tests/Core.Tests` stays a placeholder — native smoke coverage already exists via `build_harness.bat`/`harness.exe`; record in its README. `InputLockGateway` mask computation (incl. the new MAIN_MENU bit) is KSP-type-coupled (ControlTypes) and is covered by in-game gates G2/G7, not unit tests — note in the Infrastructure.Tests README too.

### Migration Strategy
- None breaking. settings.cfg gains one key (default true); existing player files without the key load fine (default applies) and are rewritten with the key on next settings change. Handshake bump 3→4 is the lockstep mechanism working as designed — a stale native DLL produces the existing version-mismatch failure path.

### Sub-Agent Scopes
- **Scope A (MS1, #001)**: managed-only — new interface, PointerBlockerGateway, tracker/gateway changes, Composition wiring. One agent.
- **Scope B (MS2, #002)**: native clamp export + handshake bump + managed setting/wiring. One agent (crosses native/managed but is one cohesive, small change).
- **Scope C (MS3, tests)**: test project + suites + README deferral notes. One agent. Runs after A and B so suites cover the new tracker/settings logic.
- Ordering follows the user-set backlog order (1→2→3); A and C touch disjoint files but C's coverage targets include A/B code, so keep sequential.
