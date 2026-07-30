# DearKSPNative — Core layer (native C++)

The Core layer of Dear KSP: owns the single ImGui context, the frame lifecycle, and draw-data → GPU translation. **Zero KSP/Unity knowledge** — communicates with the managed layers through a pure C ABI (cimgui) and the Unity low-level native plugin export surface.

## Sources

Dear ImGui 1.92.9 + cimgui are **not vendored here**. They live in the sibling clone `C:\Users\Matt\source\repos\cimgui` (with its pinned `imgui` submodule) and are compiled directly into this DLL by the build scripts. Re-pin deliberately, never casually.

## Build

- `build.bat` — debug build (`/Od /Zi`) via `cl.exe` after `vcvars64.bat`.
- `build_release.bat` — release build (`/O2 /DNDEBUG`).
- Both place `DearKSPNative.dll` into `..\GameData\DearKSP\PluginData\`, which the managed build then deploys into the KSP test instance. **Native DLLs must live in `PluginData/`, never `Plugins/`** — KSP's assembly loader tries to load every DLL in the scan path as a managed assembly and hangs on native DLLs (CinematicRecorder/CinematicShaders convention, confirmed 2026-07-29).

No CMake, no vcxproj, no vcpkg — plain `cl.exe` batch scripts, matching the CinematicRecorder convention. Links only system libs (`d3d11`, `dxgi`, `opengl32`) plus the imgui/cimgui translation units.

## Versioning

Managed and native DLLs release in lockstep; a version handshake during init turns a mismatch into the session-permanent `Failed` state (spec §5.4, D17).
