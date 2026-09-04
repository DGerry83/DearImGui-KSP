# src/shims

Created in milestones M3/M5 of the pre-release feature wave
(`notes/active/2026-09-03_NewProject_PreReleaseFeatureWave/IMPLEMENTATION_PLAN.md`).

Thin hand-written C ABI translation units adapting vendored C++ extensions
(imgui-knobs, imgui-wheels, imgui_toggle) to `extern "C"` functions the managed
Interop layer can call. ImPlot and imspinner need no shims — cimplot/cimspinner
provide generated C ABIs. Each shim is added to the explicit file lists in
`build.bat`, `build_release.bat`, and `build_harness.bat`.

Planned contents:

- `imgui_knobs_shim.cpp/.h`
- `imgui_wheels_shim.cpp/.h`
- `imgui_toggle_shim.cpp/.h`
