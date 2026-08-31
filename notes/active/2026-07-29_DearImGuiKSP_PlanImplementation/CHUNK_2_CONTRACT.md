# Chunk Contract: Native Plugin Exports + Device Detection

## Plan: DearImGui-KSP Implementation
## Date: 2026-07-29
## Chunk ID: C2
## Advances Milestone: M2

### Scope

- Vendor the minimal Unity low-level plugin headers (`IUnityInterface.h`, `IUnityGraphics.h`) into `DearImGuiKSPNative/include/`, copied from `C:\Users\Matt\source\repos\nativerenderingplugin\PluginSource\source\Unity\`.
- Rework `DearImGuiKSPNative/src/DearImGuiKSPNative.cpp` so that:
  - `UnityPluginLoad` stores `IUnityInterfaces*` and obtains `IUnityGraphics`.
  - New export `DearImGuiKSPNative_GetGraphicsDeviceKind()` returns the `UnityGfxRenderer` enum value from `IUnityGraphics::GetRenderer()`, or -1 when unavailable.
  - New export `DearImGuiKSPNative_GetRenderEventFunc()` returns a `UnityRenderingEvent` callback stub (no-op body; render work arrives in C4/C5).
  - `UnityPluginUnload` releases/clears the stored pointers.
  - `DearImGuiKSPNative_GetVersion()` stays as-is (placeholder handshake; C4 replaces it).
- Update `build.bat` and `build_release.bat` to add the `include/` directory to the compiler include path.

### Inputs (must exist before starting)

- M1 verified (`[DearImGuiKSP]` line in KSP.log).
- C1 committed; native build scripts deploy to `GameData/DearImGuiKSP/PluginData/` (D19).
- Header sources at `C:\Users\Matt\source\repos\nativerenderingplugin\PluginSource\source\Unity\`.

### Outputs (must be created/changed)

- New: `DearImGuiKSPNative/include/IUnityInterface.h`, `DearImGuiKSPNative/include/IUnityGraphics.h` (verbatim copies).
- Modified: `DearImGuiKSPNative/src/DearImGuiKSPNative.cpp`
- Modified: `DearImGuiKSPNative/build.bat`, `DearImGuiKSPNative/build_release.bat`

### Constraints

- No imgui/cimgui yet (that is C3). No backend work, no device-object creation (that is C4/C5).
- Native Core keeps zero KSP/Unity-game knowledge — Unity *plugin API* headers are the allowed exception; game APIs are not.
- C++17, x64, MSVC via `cl.exe` batch build only.
- Export naming: `DearImGuiKSPNative_*` prefix, `extern "C"`, `__declspec(dllexport)`.

### Verification

- `build.bat` compiles with 0 errors.
- `dumpbin /exports build\DearImGuiKSPNative.dll` (available after vcvars64) lists: `UnityPluginLoad`, `UnityPluginUnload`, `DearImGuiKSPNative_GetVersion`, `DearImGuiKSPNative_GetGraphicsDeviceKind`, `DearImGuiKSPNative_GetRenderEventFunc`.
- `dotnet build DearImGuiKSP.slnx` still clean (managed untouched, but confirm no accidental edits).

### Rollback

- `git checkout -- DearImGuiKSPNative/` and remove `DearImGuiKSPNative/include/`.
