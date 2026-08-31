# Chunk Contract: cimgui Interop Layer (MVP Widgets)

## Plan: DearImGui-KSP Implementation
## Date: 2026-07-29
## Chunk ID: C6
## Advances Milestone: M3

### Scope

- An **internal** interop layer at `DearImGuiKSP/Interop/` exposing the MVP widget set (windows, buttons, text, sliders, input fields) as safe C# methods over the cimgui exports already present in `DearImGuiKSPNative.dll` (C3 verified ~530 cimgui exports).
- No raw pointers, no `IntPtr`, no cimgui naming leaks above this layer — Q46 leaky-abstraction mitigation. The public consumer API is C7's job; this layer is `internal` only.

### Inputs (must exist before starting)

- C3/C4 done: cimgui 1.92.9 compiled into `DearImGuiKSPNative.dll`; the managed bridge `LoadLibrary`s the DLL during `NativeBridge.Initialize()`.
- Ground truth for signatures: `C:\Users\Matt\source\repos\cimgui\cimgui.h` (read it — do not guess signatures) and `C:\Users\Matt\source\repos\cimgui\imgui\imgui.h` for flag enums.

### Outputs (must be created/changed)

- New: `DearImGuiKSP/Interop/ImGuiNative.cs` — raw `[DllImport("DearImGuiKSPNative", CallingConvention = CallingConvention.Cdecl)]` declarations, private surface.
- New: `DearImGuiKSP/Interop/ImGuiInternal.cs` — internal safe wrappers (UTF-8 string handling, buffer management).
- New: `DearImGuiKSP/Interop/ImVec2.cs` — 8-byte blittable struct (only if a needed signature requires it).

### Design decisions (locked by this contract)

- **Implicit `[DllImport("DearImGuiKSPNative")]` is the mechanism** (not GetProcAddress): Windows resolves P/Invoke against the already-loaded module once `NativeBridge.Initialize()` has run, and `SetDllDirectory(PluginData)` covers the search path regardless. Calls before initialization are prevented by C7's frame-loop gating, not by this layer.
- **No variadic cimgui functions** (`igText`, `igLogText`, …) — P/Invoke cannot call varargs. Use the non-variadic variants (`igTextUnformatted`, etc.); verify exact names in `cimgui.h`.
- **Strings**: cimgui is `const char*` UTF-8. Marshal as null-terminated UTF-8 `byte[]` from the safe wrapper (old-Mono-safe), not `LPUTF8Str`.
- **Struct returns by value** (e.g. `igGetCursorPos`) are ABI-fragile — avoid them for MVP; none of the MVP widget calls need them. `ImVec2` passed BY VALUE as an argument (e.g. `igButton` size) is fine on Win64.
- **No callbacks** in MVP: `igInputText` gets null callback/user_data.
- Widget mapping (verify exact cimgui names): windows = `igBegin`/`igEnd`; text = `igTextUnformatted`; button = `igButton`; slider = `igSliderFloat`; input field = `igInputText`. Flag enums (`ImGuiWindowFlags`, `ImGuiInputTextFlags`, `ImGuiSliderFlags`) as C# `[Flags]` enums with only the values we use.

### Constraints

- Application layer rules apply to `DearImGuiKSP/Interop/`: **no Unity references**.
- Everything `internal`; XML-doc each wrapper noting the cimgui function it wraps.
- Keep it minimal: MVP widgets only — no tables, no trees, no popups (later milestones/versions).
- Do not modify `NativeBridge.cs` or any other existing file; do not git-commit.

### Verification

- `dotnet build DearImGuiKSP.slnx` — 0 errors, 0 warnings.
- Signature audit: for every DllImport, quote the matching `cimgui.h` line in the chunk report (name, parameter types, return type).
- Native build untouched (harness not affected).

### Rollback

- Delete `DearImGuiKSP/Interop/`.
