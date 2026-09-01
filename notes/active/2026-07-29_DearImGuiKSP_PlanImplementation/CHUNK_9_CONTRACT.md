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

---

## Addendum C9b (2026-08-31): Input Feeding — found at in-game verification

**Root cause**: the ImGui IO is never fed input. `DearImGuiKSPNative_BeginFrame` sets only `DisplaySize`/`DeltaTime` before `NewFrame`; no mouse position, buttons, wheel, keys, or characters ever reach ImGui. Result (user-verified): window renders but is entirely non-interactive in all scenes, and `WantCaptureMouse`/`WantCaptureKeyboard` stay false (locks never engage — AC7 untestable). Input feeding was an unscoped prerequisite of C9; this addendum completes the chunk.

### Scope (adds to C9)

- **Native** (`ContextHost.h/.cpp`): new export
  ```cpp
  void DearImGuiKSPNative_FeedFrameInput(float mouseX, float mouseY, float wheel, int mouseButtons, int keyBits, const char* utf8Chars);
  ```
  Called by the bridge *before* `BeginFrame` each frame. Internally: `io.AddMousePosEvent(mouseX, mouseY)`; `io.AddMouseWheelEvent(0, wheel)` when nonzero; `io.AddMouseButtonEvent` for buttons 0–2 on change (track previous mask); `io.AddKeyEvent` on change for the fixed key-bit list below (track previous bits); `io.AddInputCharactersUTF8(utf8Chars)` when non-empty. No-op when the context doesn't exist.
- **Key-bit list** (fixed order, both sides): 0 Backspace, 1 Delete, 2 LeftArrow, 3 RightArrow, 4 UpArrow, 5 DownArrow, 6 Home, 7 End, 8 Enter, 9 Escape, 10 Tab, 11 LeftCtrl, 12 RightCtrl. Native maps bits to the matching `ImGuiKey_*`.
- **Managed** (`NativeBridge.cs`): `BeginUiFrame` samples Unity input before calling `_beginFrame` (input sampling lives in Infrastructure — Application stays Unity-free):
  - `Input.mousePosition`, Y flipped against the `height` parameter (`height - y`).
  - Buttons via `Input.GetMouseButton(0..2)`.
  - Key bits via `Input.GetKey(...)` per the list above.
  - `Input.inputString` → UTF-8 bytes, filtering chars `< 0x20` and `0x7F` (backspace etc. arrive as control chars and must not reach `AddInputCharactersUTF8`).
- **Handshake bump to 3** on both sides (lockstep, invariant 4).
- Harness: keep `HARNESS PASS`.

### Constraints (addendum)

- Same exclusive file ownership as C9 (`NativeBridge.cs`, `ContextHost.h/.cpp`, `DearImGuiKSPNative.cpp`, `harness/*`). No edits to orchestrator/Composition/addon — no wiring changes needed; sampling rides inside `NativeBridge.BeginUiFrame`.
- Mouse-wheel + the key list are the MVP input surface; gamepad/navigation config flags are out of scope.
- No git commits — the lead commits.

### Verification (addendum)

- Native build 0/0; harness `HARNESS PASS`; `dotnet build` 0/0.
- **User in-game (AC7 completion)**: demo window draggable by title bar; button clickable (counter increments); slider draggable; input field focusable and editable (typing, backspace, arrows, enter); camera stays locked while hovering the window and unlocks off it.
