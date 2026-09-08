# Chunk Contract: C09 — Infrastructure hygiene & timing
## Plan: Opus Review Remediation (`notes\plans\opus-review-remediation-contracts.md`)
## Date: 2026-09-07
## Chunk ID: C09
## Advances Milestone: Post-1.0.0 remediation wave (G2-06, G3-06, G3-11, G3-12, G3-13, G3-15, G3-47)

### Scope
- **G2-06 (unscaled time)**: `DearImGuiKSPAddon.Update` now passes `Time.unscaledDeltaTime` into `RunFrame` — that single value is the source for both `io.DeltaTime` (via `BeginUiFrame`) and the tween clock (via `TweenEngine.Tick`), so the one-line source fix covers both. User-visible behavior change, deliberate: tweens keep animating while the game is paused, and ImGui timing (double-click, key repeat, blinking caret) stays correct under physics warp instead of running 4x fast. F2/loading suspension still pauses tweens (frame loop gate), as docs/50 promises. `TweenEngine.cs` listed in the plan but unchanged — it has no clock of its own; the dt arrives as a parameter. C02's try/catch fault containment in `Tick` untouched.
- **G3-12**: `StartCoroutine(RenderEventPump())` moved into the successful-init branch of `Start()` — after a failed init there is no render event to issue. The pump now yields a cached static `WaitForEndOfFrame` instead of allocating one per frame.
- **G3-13**: `InputLockGateway.ApplyLocks` reuses a session-cached `_desiredConsumerIds` HashSet (cleared on entry) instead of allocating one per frame. No aliasing: the scratch set never leaves the method and `_heldConsumerIds` copies its contents via `UnionWith`.
- **G3-06**: `INativeBridge.RebuildViewport` doc no longer advertises "real work lands in C12" — it documents the intentional no-op (DisplaySize set every BeginUiFrame; backend draws into the currently bound target; atlas is resolution-independent), matching `NativeBridge.RebuildViewport`.
- **G3-11**: `NativeBridge` class doc no longer claims every native function is GetProcAddress-bound — it now describes the actual split (bootstrap functions GetProcAddress-bound; Interop's implicit cimgui DllImports resolve by module name once the DLL is loaded).
- **G3-15**: GraphicsApi failure log no longer says "milestone 2 PoC"/"GL support arrives in C5" — now "DearImGui-KSP 1.x requires D3D11 (OpenGL support is planned for a later release)"; the adjacent comment cites D35/D37.
- **G3-47**: `ImGuiNative` class doc's stale SetDllDirectory rationale fixed — SetDllDirectory only covers NativeBridge's one explicit LoadLibrary and is restored immediately after; the implicit imports resolve against the already-loaded module.

### Inputs
- Triage sweep rows G2-06, G3-06/11/12/13/15/47 (all VALID WORK).
- C04 landed earlier on `NativeBridge.cs` (diagnostics drain, handshake v7, commit 99159be) — this chunk works on top of it; none of its code touched.

### Outputs
- Changed: `DearImGuiKSP/Infrastructure/DearImGuiKSPAddon.cs`, `DearImGuiKSP/Infrastructure/InputLockGateway.cs`, `DearImGuiKSP/Infrastructure/NativeBridge.cs`, `DearImGuiKSP/Application/Interfaces/INativeBridge.cs`, `DearImGuiKSP/Interop/ImGuiNative.cs`.
- No test changes: the timing source and the Infrastructure gateways are Unity/KSP-touching seams not covered by the Application-layer suite (Infrastructure.Tests intentionally deferred — `tests/*/README.md`); behavior verified in gate B.

### Constraints
- Public API surface unchanged (patch class, D36). No-throw widget contract unaffected.
- §5.9 native-interop checklist applies (per-frame coroutine/lock paths):
  - **Process-global state**: none new (cached WaitForEndOfFrame is an immutable Unity yield instruction; cached HashSet is instance-local, never aliased out).
  - **Hot-path allocation**: REMOVES two steady-state per-frame allocations (WaitForEndOfFrame, HashSet); adds none. Verdict: net negative allocation.
  - **Resource-acquisition symmetry**: N/A (no acquisition); the pump simply never starts when there is no render event.

### Verification
- `dotnet build DearImGui-KSP.slnx`: 0 errors, 0 warnings.
- `dotnet test DearImGui-KSP.slnx`: 129/129 green (unchanged from the post-C06 count).
- In-game: gate B (user) — pause the game: panel/theme tweens keep animating; physics warp 4x: UI timing (double-click, caret blink, tween durations) stays real-time; failed-init path (e.g. stale native DLL) shows the popup with no render-pump side effects.

### Rollback
- Revert the five listed files.
