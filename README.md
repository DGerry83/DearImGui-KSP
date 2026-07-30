# Dear KSP

A shared KSP mod library providing a modern, high-performance UI framework as a drop-in replacement for Unity IMGUI. Other mods hard-depend on it and get a clean C# ImGui-style API; players get snappy, non-IMGUI mod UIs with no measurable framerate cost.

**Status: pre-implementation.** The confirmed design spec lives at
[`notes/active/2026-07-29_DesignSpec_DearKSP_UI_Library/DESIGN_SPEC.md`](notes/active/2026-07-29_DesignSpec_DearKSP_UI_Library/DESIGN_SPEC.md);
the implementation plan at
[`notes/active/2026-07-29_NewProject_DearKSP/IMPLEMENTATION_PLAN.md`](notes/active/2026-07-29_NewProject_DearKSP/IMPLEMENTATION_PLAN.md).
Agent onboarding: see [AGENTS.md](AGENTS.md).

## How it works

- `DearKSPNative.dll` (C++ Core) — Dear ImGui 1.92.9 + cimgui compiled directly in, D3D11/OpenGL backends, rendered on Unity's render thread via the low-level native plugin API.
- `DearKSP.dll` (C#) — KSP plugin owning the frame loop, input locking, lifecycle state machine, settings, and the public consumer API.
- `DearKSPDemo.dll` — example/benchmark mod, shipped as a **separate install** so dependency-only users get no demo UI.

## Build

Prerequisites:

- Visual Studio 2026 (C++ desktop workload), .NET SDK 10+
- A sibling clone of cimgui with its imgui submodule: `C:\Users\Matt\source\repos\cimgui`
- A `DearKSP.props.user` next to the solution pinning your KSP install (gitignored):
  ```xml
  <Project><PropertyGroup>
    <KSPBT_GameRoot>C:\path\to\KSP</KSPBT_GameRoot>
  </PropertyGroup></Project>
  ```

Then:

```powershell
cd DearKSPNative; .\build_release.bat        # native DLL -> GameData\DearKSP\PluginData\
dotnet build DearKSP.slnx -c Release         # managed DLLs; deploys GameData trees into the game
```

(KSPBuildTools auto-references the game assemblies from `KSPBT_GameRoot`, stages `GameData\DearKSP\` and `GameData\DearKSPDemo\`, generates the AVC `.version` files, and mirrors both into the game on every build.)

## Install (players)

Extract the release zip into the KSP root so `GameData\DearKSP\` sits alongside the game. Requires KSP 1.12.x, Windows x64, D3D11 or OpenGL.

## Layout

- `DearKSP/` — managed library (`Application/` = Unity-free core logic + public API; `Infrastructure/` = the only KSP/Unity-touching layer)
- `DearKSPNative/` — C++ core + batch build scripts
- `DearKSPDemo/` — demo mod (separate install)
- `GameData/` — staging tree deployed into the game on build
- `tests/` — mirrors the layers (placeholders)
- `notes/` — workflow artifacts: design spec, decision/question logs, plans (see AGENTS.md)
