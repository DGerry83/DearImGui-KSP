@echo off
rem DearKSPNative headless smoke-test harness (chunk C3, CinematicRecorder convention).
rem Links the same TUs as the DLL, minus the Unity plugin entry file (no Unity
rem exports needed in an exe), into build\harness.exe. Object files go to
rem build\intermediate-harness\ to avoid clobbering the DLL build objects.
call "C:\Program Files\Microsoft Visual Studio\18\Community\VC\Auxiliary\Build\vcvars64.bat" >nul
if not exist build mkdir build
if not exist build\intermediate-harness mkdir build\intermediate-harness
cl /nologo /EHsc /std:c++17 /O2 /DNDEBUG /Isrc /I..\..\cimgui /I..\..\cimgui\imgui src\ContextHost.cpp harness\harness_main.cpp ..\..\cimgui\cimgui.cpp ..\..\cimgui\imgui\imgui.cpp ..\..\cimgui\imgui\imgui_draw.cpp ..\..\cimgui\imgui\imgui_tables.cpp ..\..\cimgui\imgui\imgui_widgets.cpp ..\..\cimgui\imgui\imgui_demo.cpp /Fe:build\harness.exe /Fo:build\intermediate-harness\ /Fd:build\intermediate-harness\
if errorlevel 1 exit /b 1
echo Harness build successful: build\harness.exe
