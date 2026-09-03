# Progress Log: ISSUES #001, #002, unit tests
## Session: notes/active/2026-09-03_Bug_ClickThrough_WindowAnchor/

- 2026-09-03 — Phase 0 complete: investigation log; spike validated #001 Candidate A (Knowledge Library note `ugui-click-blocking-and-canvas-sorting.md`); user decisions: #001=A, #002=B (opt-out settings.cfg clamp, default true), tests=xUnit Application-only with documented deferrals.
- 2026-09-03 — Phase 1 complete: PLANNING_WORKSHEET.md, ARCHITECTURE_CONTRACT.md, MILESTONES.md, GATES.md frozen. User approved Phase 2 (styling deferred until after this work).

## Scope A (MS1, #001 pointer blocker) — IMPLEMENTED (awaiting user in-game gates G1/G2)

- 2026-09-03 — Implementation complete, `dotnet build DearImGui-KSP.slnx` green (0 warnings, 0 errors). Changes:
  - **Add** `DearImGuiKSP/Application/Interfaces/IPointerBlockerGateway.cs` — internal interface, one method `void SetBlocked(bool blocked)`, house XML-doc style, no Unity types.
  - **Add** `DearImGuiKSP/Infrastructure/PointerBlockerGateway.cs` — internal sealed `IPointerBlockerGateway` impl. Lazily creates (first `SetBlocked(true)`, main thread) a GameObject `DearImGuiKSP.PointerBlocker` (DontDestroyOnLoad) with: Canvas (ScreenSpaceOverlay, overrideSorting, sortOrder 30000 — TMPro dropdown precedent), GraphicRaycaster, and a full-screen anchor-stretched Image (alpha 0, raycastTarget=true). `SetBlocked` toggles `GameObject.SetActive`; no-op when state unchanged. Blocker name/sort-order constants live in this file (sole consumer; avoids touching shared `LibraryConfig.cs`, which Scope B also modifies).
  - **Modify** `DearImGuiKSP/Application/InputCaptureTracker.cs` — new ctor param `IPointerBlockerGateway` (injected by Composition); tracks `_blocked`; calls `SetBlocked(state.MouseCaptured)` only on transitions inside `Update(...)`; `ReleaseAll()` forces `SetBlocked(false)` when blocked.
  - **Modify** `DearImGuiKSP/Infrastructure/InputLockGateway.cs` — `ComputeMask` adds `ControlTypes.MAIN_MENU` when `state.MouseCaptured` (closes the TextProButton3D 3D-button gap; stock uses the same lock).
  - **Modify** `DearImGuiKSP/Infrastructure/Composition.cs` — new `PointerBlocker` singleton property; injected into `new InputCaptureTracker(LockGateway, PointerBlocker, Registry)`.
- Diff summary (git diff --stat): `InputCaptureTracker.cs +23/-2`, `Composition.cs +6/-1`, `InputLockGateway.cs +4/-1`; two new files. `DearImGuiKSP/Application/DearImGuiKSP.cs` public surface untouched (empty diff); `LibraryConfig.cs`, `settings.cfg`, native handshake untouched (Scope B items). No public interfaces changed; no shared state fields renamed.
- Per-frame allocation in modified `Update` path: my additions are one bool field compare + transition-only call — zero allocation. Pre-existing allocation in the same path (unchanged, out of scope): `InputLockGateway.ApplyLocks` allocates one `HashSet<string>` per call (`InputLockGateway.cs:20`).
- Adjacent-code check: `ReleaseAll` callers are `Composition.WireLifecycle` UI-hide (`Composition.cs:117`) and loading (`Composition.cs:122`) handlers; both now release the blocker too (via the new `ReleaseAll` body). Blocker lifecycle mirrors lock lifecycle exactly, so no path regresses. Failed-state path releases neither locks nor blocker (identical to pre-fix behavior — frames stop; in-game gates verify).
- Sweep: capture-state consumers are `NativeBridge.GetIoSnapshot` (writer), `InputLockGateway.ComputeMask` (via the tracker's `ApplyLocks`), and the tracker itself — no other place consumes capture transitions (matches contract's similar-code search).

## Scope B (MS2, #002 viewport clamp + handshake v4) — IMPLEMENTED (awaiting user in-game gates G3/G4)

- 2026-09-03 — Implementation complete. All three verification builds green (raw output below). Changes:
  - **Modify** `DearImGuiKSP/LibraryConfig.cs` — `internal const bool DefaultClampWindowsToViewport = true;` in the settings-defaults block (spec §9.1).
  - **Modify** `DearImGuiKSP/Application/LibrarySettings.cs` — `internal bool ClampWindowsToViewport = LibraryConfig.DefaultClampWindowsToViewport;`.
  - **Modify** `DearImGuiKSP/Application/SettingsModel.cs` — `_clampWindowsToViewport` field, loaded in ctor, `Set`+`Persist` setter following the exact `Enabled` pattern; added to the `Persist()` snapshot.
  - **Modify** `DearImGuiKSP/Infrastructure/SettingsStore.cs` — `ClampWindowsToViewportKey = "clampWindowsToViewport"`; ReadBool on Load (missing key → default), AddValue on Save. `SettingsFormatVersion` unchanged (1).
  - **Modify** `GameData/DearImGuiKSP/settings.cfg` — shipped default gains `clampWindowsToViewport = true` (after `enabled`).
  - **Modify** `DearImGuiKSP/Application/Interfaces/INativeBridge.cs` — `void ClampWindowsToViewport();` with XML doc ("No-op when not initialized"), after `RebuildViewport`.
  - **Modify** `DearImGuiKSP/Infrastructure/NativeBridge.cs` — `ExpectedNativeVersion` 3 → 4; new `ClampWindowsToViewportDelegate` (Cdecl, void, no args); bound in `BindExports()`; `ClampWindowsToViewport()` guards `_initialized` then invokes.
  - **Modify** `DearImGuiKSP/Infrastructure/Composition.cs` — `WireLifecycle` `ResolutionChanged` lambda is now a block: `RebuildViewport(w,h)` then `if (Settings.ClampWindowsToViewport) Bridge.ClampWindowsToViewport();`.
  - **Modify** `DearImGuiKSPNative/src/ContextHost.cpp/.h` — `ContextHost_ClampWindowsToViewport(void)` (plain internal, declared in the header without the export macro; `imgui_internal.h` added to the .cpp): iterates `s_Context->Windows`, skips `nullptr` / `window->Hidden` / `!window->WasActive`, clamps `window->Pos` to `[0, max(0, io.DisplaySize - window->Size)]` per axis (imgui 1.92.9 fields verified in the vendored `imgui_internal.h`: `ImGuiContext::Windows` :2480, `ImGuiWindow::Pos/Size/WasActive/Hidden`).
  - **Modify** `DearImGuiKSPNative/src/DearImGuiKSPNative.cpp` — `GetVersion` returns 4 (comment updated); new `DearImGuiKSPNative_ClampWindowsToViewport` export wrapper delegating to `ContextHost_ClampWindowsToViewport()` (SetD3D11DeviceTexture wrapper pattern); `#include "ContextHost.h"` added.
- **Deliberate deviation (required by frozen G4), flagged for auditor:** the contract said "bind new export following the existing Bind/delegate patterns" — i.e. inside `BindExports()`, which runs BEFORE the version check. That would make a v3 native DLL fail with `InitErrMissingExport` (→ `FailureKind.NativeComponent` popup) before the version check ever ran, contradicting frozen G4 ("v3 native with v4 managed still terminates … with the one plain-language version-mismatch popup"). Fix: `Initialize()` now binds only `GetVersion` first, runs the handshake, and only then calls `BindExports()` (which binds the remaining nine + the new clamp export). `BindExports()` doc comment updated. All other delegate/Bind patterns untouched. Resource symmetry preserved: `Unload()` (FreeLibrary) still runs on every failure path, including the two new pre-bind ones; `_deviceTexture` is created after binding so the reorder cannot leak it.
- **Reproduction reasoning (pre-fix):** `Composition.cs:130` subscribed `Hooks.ResolutionChanged += (w, h) => Bridge.RebuildViewport(w, h)`; `NativeBridge.RebuildViewport` (`NativeBridge.cs:278`) is an intentional no-op (ImGui DisplaySize is re-fed every `BeginFrame`). Nothing adjusted consumer-owned pixel-absolute window `Pos` values, so on resolution reduction `io.DisplaySize` shrank underneath unchanged positions and near-edge windows ended mostly off-screen.
- Diff summary (git diff --stat for Scope B files only): `LibraryConfig.cs +1`, `LibrarySettings.cs +1`, `SettingsModel.cs +15/-0`, `SettingsStore.cs +3/-0`, `settings.cfg +1`, `INativeBridge.cs +8/-0`, `NativeBridge.cs +37/-12` (includes the reorder), `Composition.cs` (shared with Scope A; my hunk is the 6-line lambda body), `ContextHost.h +7/-0`, `ContextHost.cpp +24/-1`, `DearImGuiKSPNative.cpp +11/-2`. `DearImGuiKSP/Application/DearImGuiKSP.cs` public surface untouched (not in diff). No public interfaces changed; no shared state fields renamed (`ClampWindowsToViewport` is additive). (Working tree also contains Scope A's uncommitted changes — not mine.)
- Raw build output:
  - `cmd //c build.bat` → `DearImGuiKSPNative.dll (debug) -> GameData\DearImGuiKSP\PluginData\` (0 errors; cl warnings none shown).
  - `cmd //c build_harness.bat && ./build/harness.exe` → `Font atlas: 512 x 128 RGBA32, pixels=0000026D69F578E0` then `HARNESS PASS`.
  - `dotnet build DearImGui-KSP.slnx` → `Build succeeded. 0 Warning(s) 0 Error(s)`.
  - `dumpbin //exports build/DearImGuiKSPNative.dll | grep ClampWindowsToViewport` → `2 1 00005DEE DearImGuiKSPNative_ClampWindowsToViewport = @ILT+19945(...)` (export present).
- Adjacent-code check (by inspection): version-mismatch path — `Initialize()` now: LoadLibrary → bind GetVersion (null → InitErrMissingExport + Unload) → `nativeVersion != 4` → log + Unload + `InitErrVersionMismatch`; `KindForInitResult` unchanged (`NativeBridge.cs` — `InitErrVersionMismatch → FailureKind.VersionMismatch`), so a v3 native still lands in the Failed state with the version-mismatch popup (G4). Device-gate and render-hook failure paths unchanged and still after the handshake.
- Native interop & hot-path checklist (reference 08): (1) process-global state — the new export mutates only the DLL's own ImGui window positions; no OS/process-global state; the existing `SetDllDirectory` scoped-and-restore is untouched. (2) hot-path allocation — `ClampWindowsToViewport` fires only on resolution change, not per frame; the loop is allocation-free (raw `ImVector` iteration). (3) resource symmetry — no new handles; every partial-failure return between `LoadLibrary` and init success calls `Unload()`.
- "Do not fix what is not broken" checks:
  - Settings round-trip: new key uses the identical `ReadBool`/`AddValue` pattern as `verboseLogging`/`enabled`; a pre-existing settings.cfg without the key loads with default true (missing key → default), and `formatVersion` stays 1 so no migration rewrite is triggered (in-game round-trip is G3's; store pattern verified by inspection + managed build).
  - Harness smoke (proxy for Core.Tests) still prints HARNESS PASS (raw output above).
  - Public facade `DearImGuiKSP.cs` — empty diff (verified via git status: file not modified).
  - Single `INativeBridge` implementer confirmed (`NativeBridge`); `FrameLoopOrchestrator` consumes the interface unchanged — additive member is source-compatible.
- Deferred to user gates: G3 (in-game clamp behavior + settings round-trip) and G4 (mismatch run in-game). G6's `build_release.bat` is the auditor's item; debug native + harness + managed builds verified here.

## Scope C (MS3, unit tests) — IMPLEMENTED (G5 evidence below; verdict recorded by auditor)

- 2026-09-03 — Implementation complete. `dotnet build DearImGui-KSP.slnx` green; `dotnet test` green both project-level and solution-level (raw output below). Phase 0 disagreement check: **no disagreements found** — contract matches files (`LibraryConfig.DefaultClampWindowsToViewport = true` at `LibraryConfig.cs:33`, `SettingsModel.ClampWindowsToViewport` persists-on-change at `SettingsModel.cs:97-107`, three-arg `InputCaptureTracker` ctor with transition-only `SetBlocked` at `InputCaptureTracker.cs:22-30/50-54`, `INativeBridge.ClampWindowsToViewport()` at `INativeBridge.cs:54`, `ConsumerFailureThreshold = 5` at `LibraryConfig.cs:25`). All `Application.Interfaces` types are `internal`, so the `InternalsVisibleTo` line is genuinely required.
- Changes:
  - **Modify** `DearImGuiKSP/DearImGuiKSP.csproj` — `<InternalsVisibleTo Include="Application.Tests" />` item (+4 lines incl. comment). The ONLY production-code change.
  - **Add** `tests/Application.Tests/Application.Tests.csproj` — SDK-style, net48, AssemblyName `Application.Tests`, ProjectReference to `..\..\DearImGuiKSP\DearImGuiKSP.csproj`; packages xunit 2.9.2, xunit.runner.visualstudio 2.8.2, Microsoft.NET.Test.Sdk 17.12.0 (all in the local NuGet cache). Conditional `<Import>` of `DearImGui-KSP.props.user` so standalone `dotnet test tests/Application.Tests/...` works outside the solution (KSPBuildTools errors without a KSP pin when `$(SolutionDir)` is unset).
  - **Add** `tests/Application.Tests/TestDoubles.cs` — hand-written fakes: `FakeLogger` (ILogger), `FakeSettingsStore` (ISettingsStore), `FakeInputLockGateway` (IInputLockGateway), `FakePointerBlockerGateway` (IPointerBlockerGateway). No mocking framework.
  - **Add** `tests/Application.Tests/LifecycleStateMachineTests.cs` — 13 tests (12 facts + 1 theory×4): initial state, Uninitialized→Initializing→Running, illegal/no-op transitions (with warn logged), suspend/resume via SetUiVisible/SetLoading, pre-running SetUiVisible ignored-but-flag-retained (documents actual behavior), Fail from any state fires `EnteredFailed` exactly once with the kind, post-Failed transitions ignored.
  - **Add** `tests/Application.Tests/ConsumerRegistryTests.cs` — 7 tests: register, duplicate-id rejection, id reuse after unregister, unregister true/false, registration-order preservation, callback storage + Enabled default.
  - **Add** `tests/Application.Tests/FaultBarrierTests.cs` — 8 tests: success resets counter, below-threshold counting, auto-disable at `LibraryConfig.ConsumerFailureThreshold` (5), disabled consumer not invoked, other consumers unaffected, counter reset on intermittent success, ArgumentNullException on null consumer / null logger.
  - **Add** `tests/Application.Tests/SettingsModelTests.cs` — 12 tests: defaults from LibraryConfig (incl. `ClampWindowsToViewport` default true), ctor does not persist, ctor clamps loaded scale, UiScale/FontScale clamping to [0.5, 2.0], change notification only on actual change, persist-on-change via fake store, persisted snapshot contains all values, ClampWindowsToViewport set-false persists.
  - **Add** `tests/Application.Tests/InputCaptureTrackerTests.cs` — 9 tests: no blocker call when not capturing, first capturing frame calls SetBlocked(true), transition-only on repeated frames, full toggle sequence [true,false,true], enabled-consumer-ids-only in registration order, ReleaseAll releases locks + forces SetBlocked(false), ReleaseAll-when-not-blocked toggles nothing, re-capture after ReleaseAll toggles once more.
  - **Modify** `DearImGui-KSP.slnx` — one `<Project>` entry following existing syntax.
  - **Modify** `tests/Application.Tests/README.md`, `tests/Infrastructure.Tests/README.md`, `tests/Core.Tests/README.md` — placeholder wording replaced with the real-suite description; Infrastructure deferral (SettingsStore ConfigNode round-trip needs Assembly-CSharp at test runtime; InputLockGateway mask logic KSP-type-coupled, covered by G2/G7) and Core deferral (native smoke via `build_harness.bat`/`harness.exe`) recorded per the contract's documented-deferrals requirement.
- One fix during bring-up (test-side only, no production change): xUnit requires public test classes, so the FailureKind theory passes enum values as ints and casts inside the method (internal type can't appear in a public method signature); and a FaultBarrier test's loop count was corrected to end on a throw so the reset assertion is meaningful.
- Unity-free verification: the test output directory (`tests/Application.Tests/bin/Debug/net48/`) contains NO UnityEngine.dll / Assembly-CSharp.dll — only Application.Tests.dll, DearImGuiKSP.dll, and the xUnit/testplatform binaries. The 49-test run therefore exercised internal Application types without loading any KSP/Unity assembly.
- "Do not fix what is not broken" check: `git diff -- DearImGuiKSP/DearImGuiKSP.csproj` is exactly the +4-line InternalsVisibleTo block; `DearImGuiKSP/Application/DearImGuiKSP.cs` has an empty diff (not in `git status`). All other modified files under `DearImGuiKSP/` belong to Scopes A/B (already uncommitted before this scope started).
- Raw output:
  - `dotnet build DearImGui-KSP.slnx` → `Build succeeded. 0 Warning(s) 0 Error(s)` (after fixing the test-side CS0051 accessibility error; production compile untouched).
  - `dotnet test tests/Application.Tests/Application.Tests.csproj` → `Passed!  - Failed:     0, Passed:    49, Skipped:     0, Total:     49, Duration: 114 ms - Application.Tests.dll (net48)`
  - `dotnet test DearImGui-KSP.slnx` → `Passed!  - Failed:     0, Passed:    49, Skipped:     0, Total:     49, Duration: 129 ms - Application.Tests.dll (net48)`
- G5 is the auditor's verdict; evidence is the dotnet test output above. G6's solution build includes the new test project and is green.

## Phase 3 — Audit (2026-09-03) — COMPLETE, verdict CONTINUE

- Auditor re-ran the mechanical gates: `dotnet test DearImGui-KSP.slnx` → 49/49 PASS; `build_release.bat` → OK (release DLL mirrored to GameData); public API file confirmed absent from git status.
- Verdicts recorded in GATES.md: **G5 PASS, G6 PASS**; G4 code path verified by diff (version check precedes full bind); G1–G4 (in-game portions) and G7 pending the user's verification run.
- Session verdict **CONTINUE** — full audit in AUDIT_REPORT.md, including the user in-game checklist (§Recommendations).
- Similar-bugs sweep: `InputCaptureTracker` is the only `InputCaptureState` consumer (grep-verified); window enumeration is first/only use. No identical bugs found.
- INVESTIGATION_LOG.md wording fixed ("opt-in" → opt-out, default true — matching the binding user decision).
- Rulings on implementer disagreements: Scope A items 1–3 ACCEPT; Scope B items 1–5 ACCEPT (item 2 = the G4-required reorder, validated in audit).
- Next: user in-game verification → session closure (move to `notes/finished/`, update `notes/indices/master_index.md`, resolve ISSUES #001/#002) → then the deferred styling planning.


## Rework after G1/G3 FAIL (2026-09-03)

Gate re-run verdicts from the user's in-game run: **G1 FAIL** (uGUI still clickable through the demo window, incl. drag/reposition gestures) and **G3 FAIL** (window still nearly off-screen after a resolution reduction; clamp had no visible effect). G2/G5/G6/G7 PASS. GATES.md criteria untouched (frozen); verdict column edits are the parent's job.

### Handshake note

Managed/native handshake **STAYS at version 4 on both sides** (`NativeBridge.ExpectedNativeVersion = 4`, `DearImGuiKSPNative_GetVersion` returns 4). v4 never shipped — the `ClampWindowsToViewport` export signature change (void → two floats) folds into it. No version-constant edits were made in this rework.

### Phase 0 disagreement check

No blocking disagreements. Confirmations with evidence: `PointerBlockerGateway.cs` hardcoded sortOrder 30000 with no `sortingLayerID` and a parameterless ctor; `Composition.cs` constructed it with no args and held the clamp call inside the `ResolutionChanged` lambda; `FrameLoopOrchestrator` had no settings/size tracking; `INativeBridge.ClampWindowsToViewport()` was parameterless; native `ContextHost_ClampWindowsToViewport` read `io.DisplaySize`. Observations (not disagreements): (1) `SetBlocked` transition-only calling was already guaranteed by `InputCaptureTracker`, so per-activation scene introspection is cheap as the spec assumes; (2) `ILogger.Debug` is verbose-gated in `DearImGuiKSPLogger`, so the new diagnostic logs and the red tint appear only with verboseLogging on — matching the spec's diagnostic intent; (3) the harness compiles `ContextHost.cpp` into the exe and never calls the clamp, so no harness edit was needed; (4) `build.bat` mirrors the DLL into `GameData/DearImGuiKSP/PluginData/` on every run, including the temporary v3 build — the final v4 rebuild restored it (md5-verified identical to `build/DearImGuiKSPNative.dll`).

### FIX 1 — G1: blocker adopts the scene's top sorting position (root cause: RaycastComparer sorts sorting layer before sorting order; stock values are prefab-serialized, so hardcoded order 30000 could lose)

- **Modify `DearImGuiKSP/Infrastructure/PointerBlockerGateway.cs`** (rewritten, was untracked-from-Scope-A):
  - New ctor `PointerBlockerGateway(ILogger log, System.Func<bool> verboseLogging)` (aliases `ILogger` like `NativeBridge.cs` does — `UnityEngine.ILogger` collides otherwise; `using System;` was dropped to avoid the `object`/`UnityEngine.Object` collision, hence `System.Func`/`System.Math`).
  - `SetBlocked(true)` now calls `AdoptTopSortOrder()`: `Object.FindObjectsOfType<Canvas>()`, skipping our own canvas, finds the highest `SortingLayer.GetLayerValueFromID(...)`, then the max `sortingOrder` within that layer; sets `sortingLayerID`/`sortingOrder = System.Math.Min(topOrder + 1, short.MaxValue)`. Runs on every activation — callers only invoke on capture-state transitions, so it is not a per-frame cost.
  - Diagnostics: `_log?.Debug` on creation (with chosen layer/order) and on each activate/deactivate transition; tint decision re-evaluated at every `SetBlocked` call — `Color(1, 0.4, 0.4, 0.15)` while `verboseLogging()` is true, else `Color(1, 1, 1, 0)`. Verbose-off default is fully transparent.
  - Removed the `CanvasSortOrder = 30000` constant and the initial `sortingOrder` assignment.
- **Modify `DearImGuiKSP/Infrastructure/Composition.cs`**: `PointerBlocker` now wires `new PointerBlockerGateway(Logger, () => Settings.VerboseLogging)` (mirrors the logger's deferred-lambda pattern at `Composition.cs:36`).

### FIX 2 — G3: clamp driven from the live frame size, clamping against the passed size (covers both candidate causes: stale `io.DisplaySize` at event time, and `onScreenResolutionModified` possibly never firing)

- **Modify `DearImGuiKSP/Application/FrameLoopOrchestrator.cs`**: new `SettingsModel` ctor param; fields `_hasLastSize/_lastWidth/_lastHeight`; in `RunFrame` after the `IsRunning` check and before capture sampling: if a previous size exists AND (w,h) changed AND `settings.ClampWindowsToViewport` → `_bridge.ClampWindowsToViewport(width, height)`; tracked size always updated. Zero per-frame allocation (two float compares + bool).
- **Modify `DearImGuiKSP/Application/Interfaces/INativeBridge.cs`**: `void ClampWindowsToViewport(float width, float height);` with updated XML doc (internal interface, additive-signature change folded into the unshipped v4 handshake).
- **Modify `DearImGuiKSP/Infrastructure/NativeBridge.cs`**: `ClampWindowsToViewportDelegate(float width, float height)` (Cdecl), method forwards both args, `_initialized` guard unchanged.
- **Modify `DearImGuiKSP/Infrastructure/Composition.cs`**: `ResolutionChanged` lambda reverted to just `Bridge.RebuildViewport(w, h);` (clamp lives in the orchestrator now).
- **Modify `DearImGuiKSPNative/src/ContextHost.cpp/.h` + `DearImGuiKSPNative.cpp`**: `ContextHost_ClampWindowsToViewport(float width, float height)` and export `DearImGuiKSPNative_ClampWindowsToViewport(float width, float height)`; clamp math unchanged except it runs against the PASSED size; the stale-size comment replaced (old comment claimed DisplaySize was always the new size — the G3 root-cause hypothesis).
- **Add `tests/Application.Tests/FrameLoopOrchestratorTests.cs`**: hand-written `FakeNativeBridge` records clamp calls; real `LifecycleStateMachine`/`SettingsModel`/`FakeSettingsStore`/`FakeInputLockGateway`/`FakePointerBlockerGateway`/`ConsumerRegistry`/`FaultBarrier`. 4 cases: (i) no clamp on the first observed frame; (ii) one clamp with the NEW (1280, 720) on size change, none on the following unchanged frame; (iii) no clamp on size change with `ClampWindowsToViewport = false`; (iv) no clamp across 5 same-size frames. No production change was needed for testability.

### G4 support artifact: v3 backup DLL

Per spec: `DearImGuiKSPNative/src/DearImGuiKSPNative.cpp` `GetVersion` temporarily set to `return 3;` (everything else unchanged), `cmd //c build.bat`, `build/DearImGuiKSPNative.dll` copied to **`DearImGuiKSPNative/build/DearImGuiKSPNative-v3.dll`** (3,394,560 bytes), `return 4;` restored, rebuilt. `DearImGuiKSPNative/build/` is gitignored (`.gitignore:8`), so the backup never touches history. The v3 backup intentionally carries the new two-float clamp signature — G4 only exercises the version handshake (managed rejects 3 before binding any export), so this is harmless and per spec ("leave everything else").

### Raw outputs

- `dotnet build DearImGui-KSP.slnx` → `Build succeeded. 0 Warning(s) 0 Error(s)` (after fixing two compile errors in the new `PointerBlockerGateway.cs`: CS0104 `ILogger` ambiguous with `UnityEngine.ILogger` → alias; CS0104 `Object` ambiguous via `using System;` → dropped the using, qualified `System.Func`/`System.Math`).
- `cmd //c build.bat` (v3 temp build) → `DearImGuiKSPNative.dll (debug) -> GameData\DearImGuiKSP\PluginData\`; backup copied; `return 4;` restored.
- `cmd //c build.bat` (final v4 build) → `DearImGuiKSPNative.dll (debug) -> GameData\DearImGuiKSP\PluginData\`.
- `cmd //c 'dumpbin /exports build\DearImGuiKSPNative.dll'` (single quotes so MSYS doesn't mangle `/exports`) → `DearImGuiKSPNative_ClampWindowsToViewport = @ILT+19945(...)` present, alongside `DearImGuiKSPNative_GetVersion`, `DearImGuiKSPNative_BeginFrame`, `DearImGuiKSPNative_FeedFrameInput`, etc.
- `cmd //c build_harness.bat` → `Harness build successful: build\harness.exe`; `./build/harness.exe` → `Font atlas: 512 x 128 RGBA32, pixels=0000026BE4DA7D40` + `HARNESS PASS`.
- `dotnet test DearImGui-KSP.slnx` → `Passed!  - Failed:     0, Passed:    53, Skipped:     0, Total:     53, Duration: 125 ms - Application.Tests.dll (net48)` (49 pre-existing + 4 new).
- md5: `dcfdd481ab93c7dfb8702aa7d7a2070a` for both `DearImGuiKSPNative/build/DearImGuiKSPNative.dll` and `GameData/DearImGuiKSP/PluginData/DearImGuiKSPNative.dll` (mirrored copy is the final v4 build).

### Validation traces (by inspection)

- Suspend/fail paths unchanged: `Composition.WireLifecycle` still calls `CaptureTracker.ReleaseAll()` on UI-hide and scene-load before the state-machine transitions (`Composition.cs:120-128`); my edits to that file touch only `PointerBlocker`, `Orchestrator`, and the `ResolutionChanged` lambda.
- `ReleaseAll` still forces unblock: `InputCaptureTracker.ReleaseAll` untouched; it calls `SetBlocked(false)`, which now deactivates the GameObject and resets the tint to transparent when verbose is off.
- Tint default: `_image.color = _verboseLogging() ? VerboseTint : InvisibleTint;` runs on every `SetBlocked` call — verbose-off ⇒ alpha 0.
- Orchestrator clamp check: two float equality compares + one bool per frame, no allocation; clamp call happens before `_frameWatch.Restart()`, so timing stats are unaffected.
- "Do not fix what is not broken": harness prints `HARNESS PASS`; G2's `MAIN_MENU` mask change is intact (`git diff InputLockGateway.cs` vs HEAD shows only the Scope A hunk — this rework never touched the file); settings round-trip files (`SettingsModel`/`LibrarySettings`/`SettingsStore`/`LibraryConfig`/`GameData` settings.cfg) carry only the earlier session's `clampWindowsToViewport` additions — no rework edits; `DearImGuiKSP/Application/DearImGuiKSP.cs` absent from `git status` (public API unchanged); handshake constants still 4 on both sides.
- Native interop checklist (§5.9): Check 1 — no new process-global state (FindObjectsOfType is a read; blocker GameObject is session-owned, DontDestroyOnLoad, created once); Check 2 — hot path stays allocation-free (the per-frame clamp check boxes/compares only; FindObjectsOfType runs on blocker transitions, not per frame); Check 3 — no new native handles; `_initialized` guard preserved on the clamp path.

### Cumulative diff

The session's pre-rework changes were never committed, so `git diff` vs HEAD is cumulative (includes Scope B's original clamp export). Rework-touched files: `DearImGuiKSP/Infrastructure/PointerBlockerGateway.cs` (rewrite), `DearImGuiKSP/Infrastructure/Composition.cs`, `DearImGuiKSP/Application/FrameLoopOrchestrator.cs`, `DearImGuiKSP/Application/Interfaces/INativeBridge.cs`, `DearImGuiKSP/Infrastructure/NativeBridge.cs`, `DearImGuiKSPNative/src/ContextHost.cpp`, `DearImGuiKSPNative/src/ContextHost.h`, `DearImGuiKSPNative/src/DearImGuiKSPNative.cpp`; added `tests/Application.Tests/FrameLoopOrchestratorTests.cs`. GATES.md not edited.

## Session closure (2026-09-03)

- User run #3: **G1 PASS** (stock toolbar + save-load menu blocked; residual is IMGUI-only), **G3 PASS**, **G4 PASS** (v3 backup DLL mismatch run), **G7 PASS**. Prior: G2, G5, G6 PASS.
- Final session verdict: **CONTINUE — all gates PASS** (GATES.md, AUDIT_REPORT.md Addendum 2).
- ISSUES: #001 and #002 Resolved (front matter + Resolution written; rows moved to ARCHIVED_TRACKER.md). New KNOWNLIMIT #003 filed for the IMGUI click-through residual. TRACKER.md Next ID: #004.
- DECISION_LOG.md: appended D21 (viewport clamp = settings-governed library policy; §6.2 stands for the API) and D22 (uGUI blocker approach + IMGUI limitation).
- README.md: status line, tests description, new Known limitations section. AGENTS.md: status, test command, D1–D22, knowledge-note list.
- Session folder moving to notes/finished/; master index updated.
- Backlog remainder (from notes/plans/2026-09-03_PostImplementation_Backlog_HANDOFF.md): styling/theming (user holds details — next session), then OpenGL (D20).
