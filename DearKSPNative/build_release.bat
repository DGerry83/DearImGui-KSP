@echo off
rem DearKSPNative release build: /O2 /DNDEBUG, cl.exe after vcvars64 (CinematicRecorder convention).
rem Requires the sibling cimgui clone at ..\..\cimgui (with its imgui submodule).
call "C:\Program Files\Microsoft Visual Studio\18\Community\VC\Auxiliary\Build\vcvars64.bat" >nul
if not exist build mkdir build
cl /nologo /EHsc /std:c++17 /O2 /DNDEBUG /Iinclude /I..\..\cimgui /I..\..\cimgui\imgui /LD src\DearKSPNative.cpp src\ContextHost.cpp ..\..\cimgui\cimgui.cpp ..\..\cimgui\imgui\imgui.cpp ..\..\cimgui\imgui\imgui_draw.cpp ..\..\cimgui\imgui\imgui_tables.cpp ..\..\cimgui\imgui\imgui_widgets.cpp ..\..\cimgui\imgui\imgui_demo.cpp /Fe:build\DearKSPNative.dll /Fo:build\ /link /DLL
if errorlevel 1 exit /b 1
if not exist ..\GameData\DearKSP\PluginData mkdir ..\GameData\DearKSP\PluginData
copy /y build\DearKSPNative.dll ..\GameData\DearKSP\PluginData\ >nul
echo DearKSPNative.dll (release) -^> GameData\DearKSP\PluginData\
