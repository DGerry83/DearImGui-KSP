# Chunk Contract: Native Plugin Exports + Device Detection

## Plan: Dear KSP Implementation
## Date: 2026-07-29
## Chunk ID: C2
## Advances Milestone: M2

### Scope

- Vendor the minimal Unity low-level plugin headers (`IUnityInterface.h`, `IUnityGraphics.h`) into `DearKSPNative/include/`, copied from `C:\Users\Matt\source\repos\nativerenderingplugin\PluginSource\source\Unity\`.
- Rework `DearKSPNative/src/DearKSPNative.cpp` so that:
  - `UnityPluginLoad` stores `IUnityInterfaces*` and obtains `IUnityGraphics`.
  - New export `DearKSPNative_GetGraphicsDeviceKind()` returns the `UnityGfxRenderer` enum value from `IUnityGraphics::GetRenderer()`, or -1 when unavailable.
  - New export `DearKSPNative_GetRenderEventFunc()` returns a `UnityRenderingEvent` callback stub (no-op body; render work arrives in C4/C5).
  - `UnityPluginUnload` releases/clears the stored pointers.
  - `DearKSPNative_GetVersion()` stays as-is (placeholder handshake; C4 replaces it).
- Update `build.bat` and `build_release.bat` to add the `include/` directory to the compiler include path.

### Inputs (must exist before starting)

- M1 verified (`[DearKSP]` line in KSP.log).
- C1 committed; native build scripts deploy to `GameData/DearKSP/PluginData/` (D19).
- Header sources at `C:\Users\Matt\source\repos\nativerenderingplugin\PluginSource\source\Unity\`.

### Outputs (must be created/changed)

- New: `DearKSPNative/include/IUnityInterface.h`, `DearKSPNative/include/IUnityGraphics.h` (verbatim copies).
- Modified: `DearKSPNative/src/DearKSPNative.cpp`
- Modified: `DearKSPNative/build.bat`, `DearKSPNative/build_release.bat`

### Constraints

- No imgui/cimgui yet (that is C3). No backend work, no device-object creation (that is C4/C5).
- Native Core keeps zero KSP/Unity-game knowledge — Unity *plugin API* headers are the allowed exception; game APIs are not.
- C++17, x64, MSVC via `cl.exe` batch build only.
- Export naming: `DearKSPNative_*` prefix, `extern "C"`, `__declspec(dllexport)`.

### Verification

- `build.bat` compiles with 0 errors.
- `dumpbin /exports build\DearKSPNative.dll` (available after vcvars64) lists: `UnityPluginLoad`, `UnityPluginUnload`, `DearKSPNative_GetVersion`, `DearKSPNative_GetGraphicsDeviceKind`, `DearKSPNative_GetRenderEventFunc`.
- `dotnet build DearKSP.slnx` still clean (managed untouched, but confirm no accidental edits).

### Rollback

- `git checkout -- DearKSPNative/` and remove `DearKSPNative/include/`.
