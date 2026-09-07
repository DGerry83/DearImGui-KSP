# Environment Cache — DearImGui-KSP

## Project Root

`~/source/repos/DearImGui-KSP`

## Shell

- **Name:** Git Bash (MSYS2 bash bundled with Git for Windows)
- **Path:** `/usr/bin/bash`
- **Version:** GNU bash, version 5.2.37(1)-release (x86_64-pc-msys)
- **Chaining operator:** `&&`
- **Path separator:** forward slash `/`

## Build/Test Commands

- **Managed:** `dotnet build DearImGui-KSP.slnx` (Debug) / `dotnet build DearImGui-KSP.slnx -c Release`
- **Native:** `cd DearImGuiKSPNative && ./build.bat` (debug) / `./build_release.bat` (release)
- **Test:** in-game acceptance per `IMPLEMENTATION_PLAN.md`; unit tests deferred to milestone 3+

## Tools

- `dotnet` SDK 10.0.301 reachable from PATH
- `cl.exe` / Visual Studio 2026 C++ desktop workload assumed for native builds

## Machine Quirks

- Bash tool executes through Git Bash; use Unix syntax (`/dev/null`, forward slashes) inside Bash commands.
- `DearImGui-KSP.props.user` pins `KSPBT_GameRoot` and is gitignored.
- Two local KSP instances (2026-09-06):
  - `C:\SSDGames\DearImGui-KSP_TESTING` — **currently pinned.** Stripped-down GL-capable instance with modern-modded basics (Deferred, TUFX, Scatterer, ParallaxContinued, IMGUI_Helper, ModuleManager, Harmony, Kopernicus, …; deliberately no CinematicShaders/CinematicRecorder, which fail under GL — D20). Default target for test builds; unblocks the D35 OpenGL work.
  - `C:\SSDGames\ReformTestInstance` — full D16 compatibility environment. Flip the `KSPBT_GameRoot` pin back here for full-compat checks.

## Verified

- Shell probe: `echo $0 && bash --version | head -n 1 && ls && pwd` succeeded.
- Build tool probe: `dotnet --version` returned `10.0.301`.
- Scope directories `DearImGuiKSP/Application/` and `DearImGuiKSP/Infrastructure/` readable.
