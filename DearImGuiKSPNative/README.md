# DearImGuiKSPNative — Core layer (native C++)

The Core layer of DearImGui-KSP: owns the single ImGui context, the frame lifecycle, and draw-data → GPU translation. **Zero KSP/Unity knowledge** — communicates with the managed layers through a pure C ABI (cimgui) and the Unity low-level native plugin export surface.

## Sources

Dear ImGui 1.92.9 + cimgui are **not vendored here**. They live in a sibling clone `cimgui` next to this repo's root (with its pinned `imgui` submodule) and are compiled directly into this DLL by the build scripts. Re-pin deliberately, never casually.

## Build

- `build.bat` — debug build (`/Od /Zi`) via `cl.exe` after `vcvars64.bat`.
- `build_release.bat` — release build (`/O2 /DNDEBUG /Zi` + `link /DEBUG`), which also emits `build\DearImGuiKSPNative.pdb` next to the DLL so shipped-DLL crash addresses stay symbolisable.
- Both call `check_build_env.bat` first, which locates `vcvars64.bat` (hard-coded VS path, then a `vswhere` fallback) and **enforces the cimgui/imgui pin**: the sibling clone's HEAD and its imgui submodule must match the commits in `vendor\PIN_RECORD.md`, and any drift fails the build loud (update `PIN_RECORD.md` and `check_build_env.bat` together when the pin deliberately moves).
- `build_harness.bat` — builds the native test harness (`build\harness.exe`) under the same environment check.
- Both DLL builds place `DearImGuiKSPNative.dll` into `..\GameData\DearImGuiKSP\PluginData\`, which the managed build then deploys into the KSP test instance. **Native DLLs must live in `PluginData/`, never `Plugins/`** — KSP's assembly loader tries to load every DLL in the scan path as a managed assembly and hangs on native DLLs (CinematicRecorder/CinematicShaders convention, confirmed 2026-07-29). The shipped library package tree is `GameData/DearImGuiKSP/` with `Plugins/` (managed DLL), `PluginData/` (this DLL), `Fonts/`, `Textures/` (toolbar icon), and `Docs/` (the docs\*.md guides + CHANGELOG.md, staged by `package_release.bat`).

No CMake, no vcxproj, no vcpkg — plain `cl.exe` batch scripts, matching the CinematicRecorder convention. Links only system libs (`d3d11`, `dxgi`) plus the imgui/cimgui translation units. D3D11 is the only shipped backend; OpenGL is deferred to post-release (D20/D35), so no GL library is linked.

## Versioning

Managed and native DLLs release in lockstep; a version handshake during init (currently version **7**) turns a mismatch into the session-permanent `Failed` state (spec §5.4, D17).
