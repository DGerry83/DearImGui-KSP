@echo off
rem DearImGuiKSPNative release build: /O2 /DNDEBUG, cl.exe after vcvars64 (CinematicRecorder convention).
rem Requires the sibling cimgui clone at ..\..\cimgui (with its imgui submodule).
call "C:\Program Files\Microsoft Visual Studio\18\Community\VC\Auxiliary\Build\vcvars64.bat" >nul
if not exist build mkdir build
cl /nologo /EHsc /std:c++17 /O2 /DNDEBUG /Iinclude /I..\..\cimgui /I..\..\cimgui\imgui /I..\..\cimgui\imgui\backends /LD src\DearImGuiKSPNative.cpp src\ContextHost.cpp src\BackendD3D11.cpp ..\..\cimgui\cimgui.cpp ..\..\cimgui\imgui\imgui.cpp ..\..\cimgui\imgui\imgui_draw.cpp ..\..\cimgui\imgui\imgui_tables.cpp ..\..\cimgui\imgui\imgui_widgets.cpp ..\..\cimgui\imgui\imgui_demo.cpp ..\..\cimgui\imgui\backends\imgui_impl_dx11.cpp /Fe:build\DearImGuiKSPNative.dll /Fo:build\ /link /DLL d3d11.lib dxgi.lib
if errorlevel 1 exit /b 1
if not exist ..\GameData\DearImGuiKSP\PluginData mkdir ..\GameData\DearImGuiKSP\PluginData
copy /y build\DearImGuiKSPNative.dll ..\GameData\DearImGuiKSP\PluginData\ >nul
echo DearImGuiKSPNative.dll (release) -^> GameData\DearImGuiKSP\PluginData\
