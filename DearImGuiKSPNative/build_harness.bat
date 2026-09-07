@echo off
rem DearImGuiKSPNative headless smoke-test harness (chunk C3, CinematicRecorder convention).
rem Links the same TUs as the DLL, minus the Unity plugin entry file (no Unity
rem exports needed in an exe), into build\harness.exe. Object files go to
rem build\intermediate-harness\ to avoid clobbering the DLL build objects.
call check_build_env.bat
if errorlevel 1 exit /b 1
if not exist build mkdir build
if not exist build\intermediate-harness mkdir build\intermediate-harness
cl /nologo /EHsc /std:c++17 /O2 /DNDEBUG /DCIMSPINNER_API=__declspec^(dllexport^) /Isrc /Ivendor /Ivendor\imgui_toggle /Ivendor\imgui-knobs /Ivendor\imgui-wheels /Ivendor\implot /Ivendor\cimplot /Ivendor\imspinner /Ivendor\cimspinner /I..\..\cimgui /I..\..\cimgui\imgui src\ContextHost.cpp src\shims\imgui_toggle_shim.cpp src\shims\imgui_knobs_shim.cpp src\shims\imgui_wheels_shim.cpp vendor\imgui_toggle\imgui_toggle.cpp vendor\imgui_toggle\imgui_toggle_palette.cpp vendor\imgui_toggle\imgui_toggle_presets.cpp vendor\imgui_toggle\imgui_toggle_renderer.cpp vendor\imgui-knobs\imgui-knobs.cpp vendor\imgui-wheels\imgui-wheels.cpp vendor\implot\implot.cpp vendor\implot\implot_items.cpp vendor\implot\implot_demo.cpp vendor\cimplot\cimplot.cpp vendor\cimspinner\cimspinner.cpp vendor\cimspinner\cimspinner_dots.cpp vendor\cimspinner\cimspinner_bars.cpp vendor\cimspinner\cimspinner_shapes.cpp vendor\cimspinner\cimspinner_text.cpp harness\harness_main.cpp ..\..\cimgui\cimgui.cpp ..\..\cimgui\imgui\imgui.cpp ..\..\cimgui\imgui\imgui_draw.cpp ..\..\cimgui\imgui\imgui_tables.cpp ..\..\cimgui\imgui\imgui_widgets.cpp ..\..\cimgui\imgui\imgui_demo.cpp /Fe:build\harness.exe /Fo:build\intermediate-harness\ /Fd:build\intermediate-harness\
if errorlevel 1 exit /b 1
echo Harness build successful: build\harness.exe
