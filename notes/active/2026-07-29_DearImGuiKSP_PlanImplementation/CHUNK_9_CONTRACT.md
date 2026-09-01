# Chunk Contract: Input Capture + Locking

## Plan: DearImGui-KSP Implementation
## Date: 2026-08-31
## Chunk ID: C9
## Advances Milestone: M4 (parallel group A — runs concurrently with C10, C11)

### Scope

- Real mouse/keyboard capture sampling: fill `InputCaptureState` from the ImGui IO each frame.
- KSP input locks applied **only while capturing** (spec §5.3): hover over a library window locks camera/click-through controls; an active text field locks keyboard input.
- Native IO-capture export + managed/native version handshake bump (lockstep, invariant 4).
- Dead PoC scaffolding removal (flagged in the 2026-08-31 status snapshot): `s_DemoWindowVisible`, the `DearImGuiKSPNative_SetDemoWindowVisible` export, and the `ShowDemoWindow` call in the native context host.

### Inputs (must exist before starting)

- C7 frame loop locked: `RunFrame` = `BeginUiFrame` → consumer callbacks → `EndUiFrame` (`Application/FrameLoopOrchestrator.cs`).
- C6 interop conventions: implicit `[DllImport("DearImGuiKSPNative")]`; NativeBridge binds its own C ABI via GetProcAddress delegates (`Infrastructure/NativeBridge.cs`).
- `Application/InputCaptureState.cs` — mutable snapshot type, already defined (do not change its shape).
- Skeletons: `Application/InputCaptureTracker.cs`, `Application/Interfaces/IInputLockGateway.cs`, `Infrastructure/InputLockGateway.cs`.
- Spec §5.3 (frame loop + input locking); Knowledge Library `NOTES/ui-rendering-and-input.md` (InputLockManager/ControlTypes API with file:line citations).

### Outputs (must be created/changed) — exclusive file ownership

**Native (`DearImGuiKSPNative/`):**
- `src/ContextHost.h/.cpp`: new export `void DearImGuiKSPNative_GetIoCaptureState(int* wantMouse, int* wantKeyboard)` — reads `ImGui::GetIO().WantCaptureMouse` / `.WantCaptureKeyboard` when the context exists; writes 0/0 otherwise. Remove the dead PoC demo-window scaffolding here (`s_DemoWindowVisible`, `ShowDemoWindow` call).
- `src/DearImGuiKSPNative.cpp`: remove the `DearImGuiKSPNative_SetDemoWindowVisible` export; bump `DearImGuiKSPNative_GetVersion()` return from 1 to 2.
- `harness/`: keep `HARNESS PASS` — update it if it references the removed export or the old version.

**Managed (`DearImGuiKSP/`):**
- `Infrastructure/NativeBridge.cs`: bind `DearImGuiKSPNative_GetIoCaptureState` (same GetProcAddress delegate pattern as the existing exports); implement `GetIoSnapshot()` to fill the reused `_captureState` instance from it (defaults when not initialized). Bump `ExpectedNativeVersion` from 1 to 2. Update the class doc comment where it describes the handshake constant.
- `Application/Interfaces/IInputLockGateway.cs` — replace the TODO with:
  ```csharp
  void ApplyLocks(InputCaptureState state, System.Collections.Generic.IReadOnlyList<string> consumerIds);
  void ReleaseLocks(); // removes every lock this gateway currently holds
  ```
- `Infrastructure/InputLockGateway.cs` — implement over `InputLockManager`:
  - Lock mask: `MouseCaptured` → `ControlTypes.CAMERACONTROLS | ControlTypes.GUI`; `KeyboardCaptured` → `ControlTypes.KEYBOARDINPUT`; both → OR; neither → no locks.
  - One lock per enabled consumer under ID `"DearImGuiKSP." + consumerId` (spec §5.3 per-consumer lock IDs). `ApplyLocks` diffs against the set it currently holds: set new/changed locks, remove ones no longer warranted. `ReleaseLocks` removes all held. Never touch locks it did not set.
- `Application/InputCaptureTracker.cs` — implement: ctor takes `IInputLockGateway` + `ConsumerRegistry`; `Update(InputCaptureState state)` collects the enabled consumer IDs from the registry and calls `ApplyLocks`; `ReleaseAll()` forwards to the gateway (used by lifecycle chunks later).

**Explicitly NOT owned by this chunk** (lead wires after group A): `FrameLoopOrchestrator.cs`, `Composition.cs`, `DearImGuiKSPAddon.cs`. Do not touch them.

### Constraints

- Managed/native handshake changes ship in the same chunk on both sides (invariant 4) — the version bump to 2 is mandatory so a stale native DLL fails the handshake loudly.
- Application stays Unity-free: `InputCaptureTracker` must not reference UnityEngine/KSP types; `InputCaptureState` shape is unchanged.
- Frame-loop ordering note for the lead (do not implement): `GetIoSnapshot` is sampled at the top of `RunFrame`, *before* `BeginUiFrame` — capture state intentionally reflects the previous frame's ImGui IO (spec §5.3 order: sample → locks → callbacks).
- The whole cimgui API is already exported from our DLL; the new context-host export is for IO state only — do not add widget bindings.
- No git commits — the lead commits after review.

### Verification

- `cd DearImGuiKSPNative && cmd //c build.bat` — 0 errors/0 warnings; `dumpbin /exports` shows `DearImGuiKSPNative_GetIoCaptureState` and no `..._SetDemoWindowVisible`.
- `cmd //c build_harness.bat && build/harness.exe` — `HARNESS PASS`.
- `dotnet build DearImGui-KSP.slnx` — 0 errors, 0 warnings (gate G3).
- AC7 (locks engage on hover/text focus, release cleanly) is user-verified in-game after the lead wires the tracker into the frame loop — not this chunk's build-time gate.

### Rollback

- `git checkout -- DearImGuiKSPNative/src DearImGuiKSPNative/harness DearImGuiKSP/Infrastructure/NativeBridge.cs DearImGuiKSP/Infrastructure/InputLockGateway.cs DearImGuiKSP/Application/InputCaptureTracker.cs DearImGuiKSP/Application/Interfaces/IInputLockGateway.cs`
