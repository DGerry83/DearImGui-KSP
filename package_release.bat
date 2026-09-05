@echo off
rem DearImGui-KSP release packaging: builds both halves, then zips the GameData trees.
rem Usage: package_release.bat  (from the repo root). Output: dist\DearImGuiKSP-<version>.zip,
rem dist\DearImGuiKSPDemo-<version>.zip. Both zips root at GameData\... (extract into KSP root).
rem Docs (*.md from docs\ + CHANGELOG.md) ship inside the library zip under GameData\DearImGuiKSP\Docs\.
setlocal

rem --- 1. Native release build -> GameData\DearImGuiKSP\PluginData\
pushd DearImGuiKSPNative
call build_release.bat
if errorlevel 1 (popd & echo Native release build FAILED & exit /b 1)
popd
rem vcvars64 (inside build_release.bat) leaks Platform=x64 into this environment,
rem which makes dotnet pick the nonexistent "Release|x64" solution configuration.
set Platform=

rem --- 2. Managed release build -> GameData staging (+ mirrors into the pinned game)
dotnet build DearImGui-KSP.slnx -c Release
if errorlevel 1 (echo Managed release build FAILED & exit /b 1)

rem --- 3. Read the version from the library csproj (never hardcode)
set VERSION=
for /f "tokens=3 delims=<>" %%a in ('type DearImGuiKSP\DearImGuiKSP.csproj ^| findstr /c:"<Version>"') do set VERSION=%%a
if not defined VERSION (echo Could not read ^<Version^> from DearImGuiKSP\DearImGuiKSP.csproj & exit /b 1)
echo Packaging version %VERSION%

rem --- 4. Verify every expected input exists before staging (fail loud, don't ship partial)
set MISSING=0
for %%f in (
  "GameData\DearImGuiKSP\Plugins\DearImGuiKSP.dll"
  "GameData\DearImGuiKSP\PluginData\DearImGuiKSPNative.dll"
  "GameData\DearImGuiKSP\Fonts\IBMPlexSans-Regular.ttf"
  "GameData\DearImGuiKSP\Fonts\IBMPlexSans-Medium.ttf"
  "GameData\DearImGuiKSP\Fonts\OFL.txt"
  "GameData\DearImGuiKSP\Textures\toolbar.png"
  "GameData\DearImGuiKSP\settings.cfg"
  "GameData\DearImGuiKSP\DearImGuiKSP.version"
  "GameData\DearImGuiKSP\Readme.txt"
  "GameData\DearImGuiKSPDemo\Plugins\DearImGuiKSPDemo.dll"
  "GameData\DearImGuiKSPDemo\DearImGuiKSPDemo.version"
  "CHANGELOG.md"
) do if not exist %%f (echo MISSING INPUT: %%f & set MISSING=1)
for %%f in (docs\*.md) do if not exist %%f (echo MISSING INPUT: %%f & set MISSING=1)
if %MISSING%==1 (echo Aborting: expected input files are missing & exit /b 1)

rem --- 5. Stage library package: GameData\DearImGuiKSP\ recursive, no .pdb, + Docs
if exist dist\staging rmdir /s /q dist\staging
if not exist dist mkdir dist
robocopy GameData\DearImGuiKSP dist\staging\lib\GameData\DearImGuiKSP /MIR /XF *.pdb >nul
if %ERRORLEVEL% GEQ 8 (echo Staging library GameData tree FAILED & exit /b 1)
mkdir dist\staging\lib\GameData\DearImGuiKSP\Docs >nul
copy /y docs\*.md dist\staging\lib\GameData\DearImGuiKSP\Docs\ >nul
copy /y CHANGELOG.md dist\staging\lib\GameData\DearImGuiKSP\Docs\ >nul

rem --- 6. Stage demo package: GameData\DearImGuiKSPDemo\ recursive, no .pdb
robocopy GameData\DearImGuiKSPDemo dist\staging\demo\GameData\DearImGuiKSPDemo /MIR /XF *.pdb >nul
if %ERRORLEVEL% GEQ 8 (echo Staging demo GameData tree FAILED & exit /b 1)

rem --- 7. Zip (Compress-Archive on the staged GameData folder keeps GameData\ at zip root)
powershell -NoProfile -Command "Compress-Archive -Path 'dist\staging\lib\GameData' -DestinationPath 'dist\DearImGuiKSP-%VERSION%.zip' -Force -CompressionLevel Optimal"
if errorlevel 1 (echo Zipping library package FAILED & exit /b 1)
powershell -NoProfile -Command "Compress-Archive -Path 'dist\staging\demo\GameData' -DestinationPath 'dist\DearImGuiKSPDemo-%VERSION%.zip' -Force -CompressionLevel Optimal"
if errorlevel 1 (echo Zipping demo package FAILED & exit /b 1)

rmdir /s /q dist\staging
echo.
echo dist\DearImGuiKSP-%VERSION%.zip
echo dist\DearImGuiKSPDemo-%VERSION%.zip
echo Done.
