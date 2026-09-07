# Interop layer (managed)

The managed↔native seam: every P/Invoke into `DearImGuiKSPNative.dll` that is not part of NativeBridge's explicit `LoadLibrary` bootstrap lives here. This layer is a **leaf**: it references no Unity/KSP types and no other layer of this project, and nothing in it calls back up. **Caller direction is one-way only — Application and Infrastructure call into Interop; Interop never references them.** Keep that rule when adding bindings: new native surface gets a raw `[DllImport]` here plus a safe wrapper, and consumers above use the wrapper.

Components:

- `ImGuiNative.cs` — raw implicit `[DllImport("DearImGuiKSPNative")]` declarations for the cimgui exports, plus the blittable enum mirrors of the imgui.h flag/enum values the wrappers need. Implicit imports resolve against the module NativeBridge already loaded (see `Infrastructure/NativeBridge.cs` class doc).
- `ImGuiInternal.cs` — the safe internal ImGui surface consumed by the frame loop and the facade: wraps the raw calls with UTF-8 label handling (including the empty-label sentinel guard), InputText buffer management, and guard clamps. No raw pointers or `IntPtr` escape above this layer. Call between native BeginFrame/EndFrame only.
- `ImPlotNative.cs` — raw + safe P/Invoke surface for cimplot (plot windows, lines, subplots), with the same label-guard routing.
- `ImSpinnerNative.cs` — raw + safe P/Invoke surface for cimspinner, plus `ImSpinnerColor`, the blittable mirror of the C++ `imgui::ImColor` the generated cimspinner ABI passes by value.
- `ExtensionShimsNative.cs` — raw + safe P/Invoke surface for the hand-written native extension shims (imgui_toggle, imgui-knobs, imgui-wheels); shim ABI uses `int` for bools and returns 1 on value change.
- `ImVec2.cs` — blittable mirror of cimgui's `ImVec2_c`, passed by value on Win64 Cdecl.

Anything the native side exports through its **own** ABI rather than cimgui's naming (context init, BeginFrame/EndFrame, capture snapshot, diagnostics drain) is GetProcAddress-bound in `Infrastructure/NativeBridge.cs`, not here.
