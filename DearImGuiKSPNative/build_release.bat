@echo off
rem DearImGuiKSPNative release build: /O2 /DNDEBUG, cl.exe after vcvars64 (CinematicRecorder convention).
rem Requires the sibling cimgui clone at ..\..\cimgui (with its imgui submodule).
rem cimspinner's generated header leaves CIMSPINNER_API empty by default (#ifndef guard,
rem vendor/cimspinner/cimspinner.h:118-119); define it dllexport on the command line so
rem the spinner symbols are exported from the DLL (same mechanism cimgui's own API macro
rem uses). No vendored source is patched.
call "C:\Program Files\Microsoft Visual Studio\18\Community\VC\Auxiliary\Build\vcvars64.bat" >nul
if not exist build mkdir build
cl /nologo /EHsc /std:c++17 /O2 /DNDEBUG /DCIMSPINNER_API=__declspec^(dllexport^) /Iinclude /Ivendor /Ivendor\imgui_toggle /Ivendor\imgui-knobs /Ivendor\imgui-wheels /Ivendor\implot /Ivendor\cimplot /Ivendor\imspinner /Ivendor\cimspinner /I..\..\cimgui /I..\..\cimgui\imgui /I..\..\cimgui\imgui\backends /LD src\DearImGuiKSPNative.cpp src\ContextHost.cpp src\BackendD3D11.cpp src\shims\imgui_toggle_shim.cpp src\shims\imgui_knobs_shim.cpp src\shims\imgui_wheels_shim.cpp vendor\imgui_toggle\imgui_toggle.cpp vendor\imgui_toggle\imgui_toggle_palette.cpp vendor\imgui_toggle\imgui_toggle_presets.cpp vendor\imgui_toggle\imgui_toggle_renderer.cpp vendor\imgui-knobs\imgui-knobs.cpp vendor\imgui-wheels\imgui-wheels.cpp vendor\implot\implot.cpp vendor\implot\implot_items.cpp vendor\implot\implot_demo.cpp vendor\cimplot\cimplot.cpp vendor\cimspinner\cimspinner.cpp vendor\cimspinner\cimspinner_dots.cpp vendor\cimspinner\cimspinner_bars.cpp vendor\cimspinner\cimspinner_shapes.cpp vendor\cimspinner\cimspinner_text.cpp ..\..\cimgui\cimgui.cpp ..\..\cimgui\imgui\imgui.cpp ..\..\cimgui\imgui\imgui_draw.cpp ..\..\cimgui\imgui\imgui_tables.cpp ..\..\cimgui\imgui\imgui_widgets.cpp ..\..\cimgui\imgui\imgui_demo.cpp ..\..\cimgui\imgui\backends\imgui_impl_dx11.cpp /Fe:build\DearImGuiKSPNative.dll /Fo:build\ /link /DLL d3d11.lib dxgi.lib
if errorlevel 1 exit /b 1
if not exist ..\GameData\DearImGuiKSP\PluginData mkdir ..\GameData\DearImGuiKSP\PluginData
copy /y build\DearImGuiKSPNative.dll ..\GameData\DearImGuiKSP\PluginData\ >nul
echo DearImGuiKSPNative.dll (release) -^> GameData\DearImGuiKSP\PluginData\
