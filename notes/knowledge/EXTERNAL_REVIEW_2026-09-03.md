# External Code Review — 2026-09-03 (Claude)

External review of the DearImGui-KSP codebase (M1–M2 era). Verdict: **no major issues**; all findings are non-urgent follow-ups. Claims 1–4 below were spot-verified against the code on 2026-09-03 before recording here.

**Status 2026-09-03: findings 1–4 fixed in code/spec (see PROGRESS_LOG "Decisions Made"); finding 5 remains deferred per the original plan (user-confirmed).**

## Findings worth fixing

### 1. Per-frame allocation in the input hot path
`InputCaptureTracker.Update` (`Application/InputCaptureTracker.cs:27`) allocates a fresh `List<string>` every frame to collect enabled consumer IDs before calling `InputLockGateway.ApplyLocks`. Steady-state GC pressure under Mono's GC → periodic frame hitches, which cuts directly against the "no measurable framerate cost" goal.
**Fix:** give `ApplyLocks` an overload that iterates `ConsumerRegistry.Ordered` directly, or use a cached reusable buffer — it only needs the enabled IDs, not a new list.

### 2. `SetDllDirectory` is process-global and never reset
`NativeBridge.Initialize` (`Infrastructure/NativeBridge.cs:82`) calls `SetDllDirectory(pluginDataPath)` and leaves the process-wide DLL search path pointed at PluginData for the rest of the session. Other mods (Deferred, TUFX, Scatterer, Cinematic Recorder — all hard-compatibility requirements) that do their own `LoadLibrary` later could have their search resolution altered.
**Fix:** call `SetDllDirectory(null)` immediately after our `LoadLibrary` succeeds — or better, switch to `AddDllDirectory` / `LoadLibraryEx` with `LOAD_LIBRARY_SEARCH_*` flags, which are scoped rather than global.

### 3. `_deviceTexture` leaked on partial-init failure
Created in `NativeBridge.Initialize` (`NativeBridge.cs:122`) and kept alive intentionally on success — but no code path ever calls `UnityEngine.Object.Destroy(_deviceTexture)`, including `Shutdown()`. If `_setD3D11DeviceTexture` succeeds but a later init step (e.g. `ContextInit`) fails, the native library is unloaded while the managed texture and its D3D11 resource stay orphaned for the session.
**Fix:** destroy the texture on every failure path after its creation, and in `Shutdown()`. One-liner class of fix; matters because this is a shared dependency other mods load unconditionally.

### 4. No Shift/Alt modifiers fed to ImGui
`FeedFrameInput`'s key bitmask covers navigation/edit keys and Ctrl only. Ctrl+A works in `InputText`; Shift+Arrow and Shift+Home/End selection do not. Acceptable for MVP widgets, but consumers shouldn't discover it by surprise.
**Fix:** record it explicitly in the design spec's Out of Scope / known-limitations section (§3.2) — a documentation entry, not necessarily code.

### 5. Zero automated tests so far
The `tests/*/README.md` files are placeholders; all verification is in-game acceptance. Appropriate for M1–M2, but `LifecycleStateMachine`, `ConsumerRegistry`, `FaultBarrier`, and `SettingsStore`'s ConfigNode parsing are pure C# with no Unity dependency — the exact layer the architecture was designed to make cheaply testable. The ConfigNode wrapper-node bug (settings silently never parsing) is the class of regression a few `SettingsStore` unit tests would catch instantly.
**Status:** already an explicit, dated decision in `IMPLEMENTATION_PLAN.md` (unit tests deferred to milestone 3+). Worth pulling forward now that M3 is underway — establish the habit before the surface area grows.

## Smaller notes (no action needed now)

- **Version handshake magic number**: `ExpectedNativeVersion = 3` is hand-maintained in two files/languages. A shared generated header or build-time check would remove a manual step. Nice-to-have.
- **`ConsumerRegistry.Unregister` O(n) `List.Remove`**: irrelevant at expected consumer counts.
- **Draw-data tearing caveat** (game thread → render thread): already flagged in comments as accepted PoC risk with a double-buffering plan. Correct way to carry known debt.

## Workflow follow-up

These findings were also used as input to a review of the FlyByWire workflow itself. Items 1–3 trace to a genuine workflow gap (nothing in the templates prompts hot-path allocation checks, process-global side-effect checks, or resource-acquisition symmetry on partial-init failure). The next FlyByWire version (v3) adds a Native Interop & Hot-Path Checklist aimed at preventing exactly this class of mistake on future chunks — see `notes/V3_PLAN.md` in the FlyByWire repo.
