@echo off
rem DearImGuiKSPNative shared build-environment check (C05, review items G2-02/G3-02).
rem Called by build.bat / build_release.bat / build_harness.bat before compiling:
rem   1. locates vcvars64.bat (hard-coded VS 18 Community path first, vswhere
rem      fallback for any other install) and calls it;
rem   2. enforces the cimgui/imgui pin (PIN_RECORD.md "build must fail loud on
rem      drift"): the sibling clone's HEAD and its imgui submodule must match
rem      the pinned commits below. Update this constant TOGETHER with
rem      vendor\PIN_RECORD.md when the pin moves.
rem Exits 1 with a clear message on any failure; on success vcvars64 has run and
rem the caller can invoke cl.exe directly.
set "PINNED_CIMGUI_COMMIT=b705b2465a17428a3ab6b19c893b452a70dcbb14"
set "PINNED_IMGUI_COMMIT=b334d19b667958ed970000073644d911fae17e57"

set "VCVARS=C:\Program Files\Microsoft Visual Studio\18\Community\VC\Auxiliary\Build\vcvars64.bat"
if not exist "%VCVARS%" (
    set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
    if exist "%VSWHERE%" (
        for /f "usebackq tokens=*" %%i in (`"%VSWHERE%" -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath`) do set "VCVARS=%%i\VC\Auxiliary\Build\vcvars64.bat"
    )
)
if not exist "%VCVARS%" (
    echo ERROR: vcvars64.bat not found. No Visual Studio with the C++ x64 toolchain
    echo ERROR: was located ^(default path tried, then vswhere -latest^). Install
    echo ERROR: "Desktop development with C++" or edit check_build_env.bat.
    exit /b 1
)
call "%VCVARS%" >nul
if errorlevel 1 (
    echo ERROR: vcvars64.bat failed to initialize the build environment.
    exit /b 1
)

if not exist "..\..\cimgui\.git" (
    echo ERROR: sibling cimgui clone not found at ..\..\cimgui ^(expected layout per
    echo ERROR: DearImGuiKSPNative\README.md, pinned imgui 1.92.9^).
    exit /b 1
)
set "ACTUAL_CIMGUI="
for /f %%i in ('git -C "..\..\cimgui" rev-parse HEAD 2^>nul') do set "ACTUAL_CIMGUI=%%i"
if not defined ACTUAL_CIMGUI (
    echo ERROR: could not read the sibling cimgui clone's HEAD ^(git missing or not
    echo ERROR: a repository^). The pin cannot be verified; refusing to build.
    exit /b 1
)
if not "%ACTUAL_CIMGUI%"=="%PINNED_CIMGUI_COMMIT%" (
    echo ERROR: cimgui pin mismatch ^(PIN_RECORD.md fail-loud rule^):
    echo   pinned: %PINNED_CIMGUI_COMMIT%
    echo   actual: %ACTUAL_CIMGUI%
    echo Checkout the pinned commit, or update PIN_RECORD.md and check_build_env.bat
    echo together when the pin deliberately moves.
    exit /b 1
)
set "ACTUAL_IMGUI="
for /f %%i in ('git -C "..\..\cimgui\imgui" rev-parse HEAD 2^>nul') do set "ACTUAL_IMGUI=%%i"
if not defined ACTUAL_IMGUI (
    echo ERROR: the cimgui clone's imgui submodule is not checked out ^(git
    echo ERROR: submodule update --init^). The pin cannot be verified; refusing to build.
    exit /b 1
)
if not "%ACTUAL_IMGUI%"=="%PINNED_IMGUI_COMMIT%" (
    echo ERROR: imgui submodule pin mismatch ^(PIN_RECORD.md fail-loud rule^):
    echo   pinned: %PINNED_IMGUI_COMMIT% ^(v1.92.9-docking^)
    echo   actual: %ACTUAL_IMGUI%
    echo Checkout the pinned submodule commit, or update PIN_RECORD.md and
    echo check_build_env.bat together when the pin deliberately moves.
    exit /b 1
)
exit /b 0
