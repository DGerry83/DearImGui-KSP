# DearImGui-KSP

A shared KSP mod library providing a modern, high-performance UI framework as a drop-in replacement for Unity IMGUI. Other mods hard-depend on it and get a clean C# ImGui-style API; players get snappy, non-IMGUI mod UIs with no measurable framerate cost.

**Status: feature-complete and verified in-game** (2026-09-03). All milestones M1–M6 pass their acceptance criteria in a heavily modded environment (Deferred, TUFX, Scatterer, Parallax, Cinematic Shaders, Cinematic Recorder, IMGUI mods). Post-MVP follow-ups landed 2026-09-03: uGUI click bleed-through fix, viewport clamp on resolution change (`clampWindowsToViewport` setting), and the xUnit Application test suite. OpenGL support is deferred (D3D11 only for now). The confirmed design spec lives at
[`notes/finished/2026-07-29_DesignSpec_DearImGuiKSP_UI_Library/DESIGN_SPEC.md`](notes/finished/2026-07-29_DesignSpec_DearImGuiKSP_UI_Library/DESIGN_SPEC.md);
the implementation record and final audit at
[`notes/finished/2026-07-29_DearImGuiKSP_PlanImplementation/FINAL_AUDIT.md`](notes/finished/2026-07-29_DearImGuiKSP_PlanImplementation/FINAL_AUDIT.md).
Agent onboarding: see [AGENTS.md](AGENTS.md).

## How it works

- `DearImGuiKSPNative.dll` (C++ Core) — Dear ImGui 1.92.9 + cimgui compiled directly in, D3D11 backend, rendered on Unity's render thread via the low-level native plugin API.
- `DearImGuiKSP.dll` (C#) — KSP plugin owning the frame loop, input locking, lifecycle state machine, settings, and the public consumer API.
- `DearImGuiKSPDemo.dll` — example/benchmark mod, shipped as a **separate install** so dependency-only users get no demo UI.

## Build

Prerequisites:

- Visual Studio 2026 (C++ desktop workload), .NET SDK 10+
- A sibling clone of cimgui with its imgui submodule, checked out next to this repo's root (the build scripts reference `..\..\cimgui`)
- A `DearImGui-KSP.props.user` next to the solution pinning your KSP install (gitignored):
  ```xml
  <Project><PropertyGroup>
    <KSPBT_GameRoot>C:\path\to\KSP</KSPBT_GameRoot>
  </PropertyGroup></Project>
  ```

Then:

```powershell
cd DearImGuiKSPNative; .\build_release.bat        # native DLL -> GameData\DearImGuiKSP\PluginData\
dotnet build DearImGui-KSP.slnx -c Release      # managed DLLs; deploys GameData trees into the game
```

(KSPBuildTools auto-references the game assemblies from `KSPBT_GameRoot`, stages `GameData\DearImGuiKSP\` and `GameData\DearImGuiKSPDemo\`, generates the AVC `.version` files, and mirrors both into the game on every build.)

## Install (players)

Extract the release zip into the KSP root so `GameData\DearImGuiKSP\` sits alongside the game. Requires KSP 1.12.x, Windows x64, D3D11.

## Layout

- `DearImGuiKSP/` — managed library (`Application/` = Unity-free core logic + public API; `Infrastructure/` = the only KSP/Unity-touching layer)
- `DearImGuiKSPNative/` — C++ core + batch build scripts
- `DearImGuiKSPDemo/` — demo mod (separate install)
- `GameData/` — staging tree deployed into the game on build
- `tests/` — mirrors the layers (`Application.Tests` = real xUnit suite, `dotnet test`; Infrastructure/Core intentionally placeholders — see their READMEs)

## Known limitations

- Clicks pass through ImGui windows to Unity **IMGUI** (`OnGUI`) windows/menus beneath them (stock uGUI is blocked correctly). IMGUI input bypasses Unity's EventSystem, so the library's uGUI raycast blocker cannot intercept it (ISSUES #003).
- `notes/` — FlyByWire workflow artifacts: design spec, decision/question logs, plans (see AGENTS.md)
