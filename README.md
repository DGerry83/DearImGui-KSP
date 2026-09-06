# DearImGui-KSP

DearImGui-KSP brings [Dear ImGui](https://github.com/ocornut/imgui) — the
industry-standard immediate-mode UI library by Omar Cornut, on which this
entire project is built — to Kerbal Space Program, packaged as a shared
library that any mod can use.

It is a UI framework for KSP mods: windows, buttons, sliders, and text, but
also live plots and graphs, dials and knobs, toggles, spinners, gradients,
and smooth C#-driven animation — all rendered on the GPU with high
performance, even when the UI is large, dense, or updating every frame.

**Status: 1.0.0 — first public release** (2026-09-05). KSP 1.12.x, Windows
x64, D3D11. OpenGL support is planned for a post-release update.

## Why should I use this?

Honestly — maybe you shouldn't, and that's fine.

If the stock Unity IMGUI gives you everything you need — the formatting,
the visual delivery, the scaling — at a performance cost you can't feel,
then switching gains you little. DearImGui-KSP is not "the stock UI but
better." It adds a dependency your users must install, and it means
learning a new API. Those are real costs, and for a great many mods the
stock UI is genuinely enough.

DearImGui-KSP earns its place when stock IMGUI leaves you wanting more:

- Your UI has so many fields, buttons, or readouts that IMGUI bogs the
  framerate down — IMGUI rebuilds its interface on the CPU every frame,
  and large UIs pay for that. DearImGui-KSP renders on the GPU and costs
  a fraction of a millisecond per frame even with busy dashboards open.
- You want live plots and graphs of flight data, not just text fields.
- You want animation — meters that sweep, values that ease between states,
  spinners and dials — without hand-rolling it.
- You want a modern, consistent look: themes, custom fonts, gradient
  buttons, collapsible sections.

There is also a possible community-level benefit: if many mods draw their
UIs through one shared, cheap renderer, players running lots of mods with
lots of windows open feel less cumulative drag. Whether the community
moves that way is its own decision — this library simply makes the option
available.

## For players

You only need this if a mod you use lists DearImGui-KSP as a dependency.
Extract `DearImGuiKSP-x.y.z.zip` into your KSP root so
`GameData\DearImGuiKSP\` sits alongside the game. Once installed, the mod
adds a toolbar button (DearImGui-KSP Settings) where you can pick the
theme and font and adjust UI and font scaling; settings persist between
sessions.

The optional demo mod (`DearImGuiKSPDemo-x.y.z.zip`, a separate install)
shows what the framework can do — a themed widget showcase, a live
telemetry window with graphs, a stage/Δv readout, and an orbit display.
It's a demonstration, not a gameplay mod; most players can skip it.

## For modders

Your mod hard-depends on DearImGui-KSP and draws its UI through a clean
C# API — declare `[assembly: KSPAssemblyDependencyEqualMajor("DearImGuiKSP",
1, 0)]`, register a frame callback, and call the facade. Full modder
documentation ships in [`docs/`](docs/) (and as plain files inside the
release zip), starting with
[00-getting-started](docs/00-getting-started.md).

On versioning: patch (1.0.**x**) and minor (1.**x**.0) updates never break
consumers — bugfixes and new features are always safe for your users to
install. Only a major bump (**x**.0.0) signals breaking API changes and
requires you to update your dependency attribute; those will be rare and
announced.

## How it works

- `DearImGuiKSPNative.dll` (C++) — Dear ImGui 1.92.9 + cimgui compiled
  directly in, with a D3D11 backend rendered on Unity's render thread via
  the low-level native plugin API.
- `DearImGuiKSP.dll` (C#) — the KSP plugin: frame loop, input locking,
  lifecycle management, settings, and the public consumer API.
- `DearImGuiKSPDemo.dll` — the demo/benchmark mod, shipped separately so
  dependency-only installs get no demo UI.

## Building from source

Prerequisites:

- Visual Studio 2026 (C++ desktop workload), .NET SDK 10+
- A sibling clone of cimgui with its imgui submodule, checked out next to
  this repo's root (the build scripts reference `..\..\cimgui`)
- A `DearImGui-KSP.props.user` next to the solution pinning your KSP
  install (gitignored):
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

Release zips are produced by `package_release.bat` at the repo root.

## Credits and licenses

DearImGui-KSP itself is released under the MIT License (Copyright (c) 2026
DGerry83) — see [`LICENSE.txt`](LICENSE.txt); a copy ships in the mod
folder as `License.txt`.

The shipped binaries are built on, and bundle, the following third-party
work (exact pins in
[`DearImGuiKSPNative/vendor/PIN_RECORD.md`](DearImGuiKSPNative/vendor/PIN_RECORD.md)):

- Dear ImGui (MIT, Copyright (c) 2014-2026 Omar Cornut) and cimgui (MIT,
  Copyright (c) 2015 Stephan Dilly) — compiled in directly from the
  sibling `cimgui` clone (pinned imgui 1.92.9), not vendored.
- ImPlot v1.0 (MIT, Copyright (c) 2020 Evan Pezent) and its cimplot C
  bindings (MIT; generated by cimgui/cimplot's generator from the ImPlot
  sources).
- imgui_toggle (0BSD, Copyright © 2022 nitz — chris marc dailey https://cmd.wtf).
- imgui-knobs (MIT, Copyright (c) 2022 Simon Altschuler).
- imgui-wheels (MIT, Copyright (c) 2026 Engineer162).
- imspinner (MIT, Copyright (c) 2021-2022 Dalerank) and its generated
  cimspinner C bindings (MIT, same project and copyright).
- IBM Plex Sans (SIL Open Font License 1.1, Copyright (c) 2017 IBM Corp.,
  Reserved Font Name "Plex") — the bundled UI fonts in
  `GameData/DearImGuiKSP/Fonts/`; the full license text ships alongside
  them in
  [`GameData/DearImGuiKSP/Fonts/OFL.txt`](GameData/DearImGuiKSP/Fonts/OFL.txt).

Each vendored tree keeps its upstream license file under
`DearImGuiKSPNative/vendor/`.
