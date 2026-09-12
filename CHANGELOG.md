# Changelog

## Unreleased — demo fixes

- Demo: fixed duplicate and dead green toolbar buttons piling up across scene loads (three in the VAB) — the demo consumer is now a persistent once-addon with one-shot toolbar registration, the same pattern that fixed the library's own settings button (ISSUES #015).
- Demo: window visibility now survives scene changes — closing the demo in one scene keeps it closed in the next instead of reopening every time.
- Demo: the benchmark window and its IMGUI reference window are now hidden by default instead of forced open in every scene; show them from the main demo window when wanted. The docked-window showcase appears once both the Plots and Benchmark windows are shown.
- Demo: the IMGUI reference window's "Use Virtualization" toggle now actually starts ON — a first-frame layout commit was silently flipping it off.
- Demo: the in-window docking area now appears whenever at least one of the Plots/Benchmark windows is visible instead of requiring both — a single visible window docks into the full area, both get the split layout, and layout changes rebuild cleanly when windows are shown or hidden.

## 1.2.0 — minor

Window docking; no breaking changes.

- Windows rendered by the library can now be docked: drag any window's title bar onto another window or a dock region to split or tab them together. Docking is enabled by default; players can turn it off live in the DearImGui-KSP settings panel (new `docking` key in `settings.cfg`).
- Docked windows cannot leave the game window (multi-viewport platform windows are deliberately not supported), and dock layouts are not persisted between game sessions — consumers reapply their preferred layout each session.
- New consumer API (additive only): `DockSpace` / `DockSpaceOverViewport`, a curated DockBuilder subset for programmatic one-time layouts, public `ImGuiDockNodeFlags` / `ImGuiDir` enums, and a `noDocking` option on `BeginWindow` / `ImGuiEx.Window` (declare dockspace-host windows with it — docking a host is unsupported by ImGui). Existing signatures are unchanged.
- Consumers cannot read the player's docking setting; write docking paths so windows also make sense floating. See the "Window docking" section of `docs/20-widgets.md` for the recommended pattern (in-window dockspace with a return-value gate; pass an explicit dockspace height inside auto-resize windows).
- The viewport clamp now skips docked windows instead of fighting the dock node on resolution changes.
- The "ksp" theme now colors the docking drop preview and empty dock background.
- Demo: the main demo window hosts a dockspace with the Plots and Benchmark windows docked into it by default.
- Internal: managed/native handshake v9 (both DLLs ship in lockstep as always); the native base build has tracked the ImGui docking branch (`v1.92.9-docking`) since 1.1.0.

## 1.1.0 — minor

OpenGL support; no public API changes, no breaking changes.

- KSP now works under OpenGL Core (`-force-glcore`) in addition to Direct3D 11: the native library gained an imgui_impl_opengl3 backend alongside the D3D11 one, selected automatically at startup from the detected graphics API. No consumer action needed.
- Graphics mods that fail under OpenGL themselves (Cinematic Shaders, Cinematic Recorder) remain unusable in GL mode regardless of this library.

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
