# Plan Digest: DearImGui-KSP Implementation

## Date: 2026-07-29
## Source Plan: `notes/active/2026-07-29_NewProject_DearImGuiKSP/IMPLEMENTATION_PLAN.md` (+ `PLANNING_WORKSHEET.md`, confirmed `DESIGN_SPEC.md`)

### Plan-Specific Invariants (filled in for this codebase — always enforced)

1. **Layer boundaries** — `DearImGuiKSP/Application/` stays Unity/KSP-free; only `DearImGuiKSP/Infrastructure/` references KSP/Unity APIs; `DearImGuiKSPNative/` has zero KSP/Unity knowledge and talks C ABI only.
2. **Public API stability** — once milestone 3 lands, the consumer-facing `DearImGuiKSP` facade is additive-only within a major version (D17).
3. **Runtime compatibility** — managed targets net48 on KSP's Mono (no .NET Core-only APIs); native is C++17 x64 only; no CMake/vcxproj/vcpkg.
4. **Managed/native version lockstep** — every native change that touches the handshake surface bumps the handshake; mismatched builds must trip the `Failed` state, never limp along (spec §5.4).
5. **Rendering constraints** — no IMGUI/uGUI for library-rendered UI (sole exception: the stock failure `PopupDialog`); must not break Deferred/TUFX (D5); no network or external I/O beyond KSP/Unity APIs.

### Goals (one-line summary each)

1. Prove Unity render-thread injection works in KSP (D3D11 + OpenGL) — the gating technical risk.
2. Deliver the managed frame loop + consumer API so mods build UIs with zero IMGUI.
3. Enforce input locking, fault isolation, lifecycle states, and failure UX per spec.
4. Persist global settings in a ConfigNode with forward migration.
5. Validate performance (torture test vs IMGUI) and the full D16 compatibility environment.

### Structural Sections (plan §9 milestones)

| Section | Summary | Files/Records Touched | Risk Level |
|---------|---------|----------------------|------------|
| M1 Build pipeline | slnx/csproj/native scripts verified; in-game startup log line | All skeleton files; `DearImGuiKSPAddon.cs` | Low |
| M2 Render-injection PoC | Unity plugin exports, ImGui context, D3D11+GL backends, minimal NativeBridge | `DearImGuiKSPNative/src/*`, `NativeBridge.cs`, cimgui/imgui sources | **High** (unproven pattern) |
| M3 Consumer API + widgets | DearImGuiKSP facade, registry, cimgui bindings for MVP widgets, demo window | `Application/*.cs`, `DearImGuiKSPDemo/*` | Med |
| M4 Input locks + fault isolation | Capture tracking, lock gateway, 5-strike barrier | `InputCaptureTracker.cs`, `InputLockGateway.cs`, `FaultBarrier.cs` | Med |
| M5 Settings + lifecycle + failure UX | ConfigNode store, state machine, game-event hooks, popup | `SettingsStore.cs`, `LifecycleStateMachine.cs`, `GameEventHooks.cs`, `FailureNotifier.cs` | Med |
| M6 Benchmark + compat | Torture-test UI, instrumentation, full mod environment | `DearImGuiKSPDemo/*` | Low |

### Files Requiring Changes

| File | Change Type | Plan Section | Depends On |
|------|-------------|--------------|------------|
| `DearImGuiKSPNative/src/DearImGuiKSPNative.cpp` | Modify | M2 | M1 |
| `DearImGuiKSPNative/src/` (new: context host, backends, atlas, cimgui/imgui TUs) | Add | M2 | M1 |
| `DearImGuiKSPNative/build*.bat` | Modify (add imgui/cimgui sources) | M2 | M1 |
| `DearImGuiKSP/Infrastructure/NativeBridge.cs` | Modify | M2 | M1 |
| `DearImGuiKSP/Infrastructure/DearImGuiKSPLogger.cs`, `Composition.cs` | Modify | M1 | — |
| `DearImGuiKSP/Application/*.cs` (facade, registry, orchestrator) | Modify | M3 | M2 |
| `DearImGuiKSPDemo/DemoConsumer.cs` | Modify | M3, M6 | M3 |
| `DearImGuiKSP/Application/InputCaptureTracker.cs`, `Infrastructure/InputLockGateway.cs`, `Application/FaultBarrier.cs` | Modify | M4 | M3 |
| `DearImGuiKSP/Application/SettingsModel.cs`, `LifecycleStateMachine.cs`, `Infrastructure/{SettingsStore,GameEventHooks,FailureNotifier}.cs` | Modify | M5 | M3 |

### New Records / Assets

None beyond what the spec's §8.1 inventory lists (embedded ProggyClean — no file; `settings.cfg` already staged). No new assets required by implementation.

### External Dependencies

| Dependency | Required? | How Verified |
|------------|-----------|--------------|
| cimgui + imgui 1.92.9 at `C:\Users\Matt\source\repos\cimgui` | Yes | Cloned 2026-07-29, submodule checked out |
| KSP test instance `C:\SSDGames\ReformTestInstance` | Yes | Present; DearImGuiKSP + DearImGuiKSPDemo already deploy there |
| Unity native plugin pattern reference `repos\nativerenderingplugin` | Reference | Present on disk |
| VS 2026 vcvars64 + .NET SDK 10 | Yes | Both builds verified during bootstrap |
| KSP Knowledge Library | Reference | NOTES cover assembly loading + render/input facts |

### Risk Flags

- **M2 is the kill-or-continue gate**: `GL.IssuePluginEvent` has no precedent in the KSP dump. If the PoC fails, the fallback is uGUI (RESEARCH_NOTES §2) — that would invalidate most of this plan and require re-planning.
- Injection point (camera event / command-buffer timing) requires runtime scene probing — explicitly open in plan §10.
- Deferred + TUFX active during PoC testing (not a clean-room test) — their render hooks may interact with ours.
- cimgui master drift: bindings are generated from pinned imgui 1.92.9; do not update the submodule casually.

### Open Questions

- None requiring user input. Injection-point selection is a runtime-probing task inside M2, not a plan ambiguity.

---

Phase 0 complete. Plan ingested. Waiting for approval to proceed to Phase 1 (chunk decomposition).
