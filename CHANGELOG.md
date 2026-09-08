# Changelog

## 1.0.1 — patch

Bugfixes and hardening from the post-release review wave; no public API changes, no breaking changes.

- Fixed a crash-to-desktop when a consumer made a widget call outside its registered frame callback — such calls are now a safe no-op.
- Fixed a game freeze when a consumer registered or unregistered another consumer from inside its own frame callback.
- Tween setter exceptions are now contained and logged instead of freezing every consumer's UI for the session.
- Fixed UI and tween animations freezing while the game is paused and running fast under physics warp (all UI timing now uses unscaled time).
- Settings saves are now debounced and written atomically; the release zip no longer bundles `settings.cfg`, so upgrading no longer resets your saved UI settings.
- Knobs and value wheels no longer display raw `##`/`###` identifier suffixes in their labels.
- Rendering hardening: UI scale can no longer round down to zero pixels, a failed D3D11 device recovery now latches instead of retrying every frame, and internal ImGui errors surface in `KSP.log` under `[DearImGuiKSP]`.
- Added managed↔native ABI validation guards (enum sizes, counts, buffer capacities) so a mismatched DLL pair fails loud at startup instead of corrupting state.
- Removed a dead font load at startup (IBM Plex Medium was fetched but never used).
- Packaging/metadata: `.version` files now correctly pin KSP 1.12.x, the AVC update URL points at the raw file, and the demo zip ships its own Readme and License files.
- Demo: fixed the IMGUI reference window's "Use Virtualization" toggle not sticking, corrected orbit-panel math, and removed the Stages/dV tab (matching stock's dV requires its crossfeed simulation — abandoned deliberately).

## 1.0.0 — first public release

DearImGui-KSP is a shared KSP mod library that gives other mods a modern, high-performance immediate-mode UI framework as a drop-in replacement for Unity IMGUI: you get a clean C# ImGui-style API, players get snappy mod UIs with no measurable framerate cost, and the library handles the frame loop, input locking, and lifecycle for you. The 1.0.0 public API includes the window helpers and the exception-safe `ImGuiEx` scopes (window, scroll region, tab bar/item, style color/var); the full widget set (buttons, sliders, text, collapsing headers, tabs, and the new toggle switches, knobs, value wheels, and activity spinners); rounded-corner fills, borders, and two-stop linear gradients; plotting through ImPlot with axis-fitting helpers such as `SetupAxesAutoFit` for live telemetry graphs; a small C# tween/easing animation helper; the named global theme engine (the "ksp" theme is the default, bundled with IBM Plex Sans under the SIL Open Font License); and the in-game control panel for players to adjust font, theme, and scale. Versioning is SemVer `major.minor.patch`: major = finalized/breaking updates, minor = backward-compatible feature additions, patch = bugfixes/tweaks. Components are integers, not digits — bumping one resets everything after it, so 1.3.12 is a valid version and 1.1.0 → 2.0.0 skips nothing. No breaking changes within a major; deprecated APIs carry `[Obsolete]` migration guidance for at least one full minor cycle before removal at the next major. Declare your dependency with `KSPAssemblyDependencyEqualMajor("DearImGuiKSP", 1, 0)` so a future breaking 2.0 can never silently load under a mod built for 1.x, and check this changelog before upgrading. Requires KSP 1.12.x, Windows x64, D3D11 (OpenGL support is planned for a post-release update). Install by extracting the release zip into your KSP root; the demo/benchmark mod ships as a separate optional zip.
