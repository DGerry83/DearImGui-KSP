@echo off
rem DearImGui-KSP release packaging: builds both halves, then zips the GameData trees.
rem Usage: package_release.bat  (from the repo root). Output: dist\DearImGuiKSP-<libversion>.zip,
rem dist\DearImGuiKSPDemo-<demoversion>.zip. Both zips root at GameData\... (extract into KSP root).
rem Docs (*.md from docs\ + CHANGELOG.md) ship inside the library zip under GameData\DearImGuiKSP\Docs\.
rem settings.cfg is deliberately EXCLUDED from the library zip (G2-16): extracting an upgrade
rem must not reset the player's saved UI settings. It stays in GameData\ for dev mirroring, and
rem the library falls back to defaults when the file is missing (SettingsStore.Load).
rem PDBs stay OUT of both zips (the native release PDB compresses to ~20 MB, 10x the library
rem zip, and KSP never loads it); the native PDB is archived per-release in dist\symbols\ so
rem crash reports against a shipped DLL stay symbolisable dev-side (C05/G2-03).
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

rem --- 3. Read the versions from the csproj files (never hardcode).
rem Library version names the library zip; the demo zip uses the demo's own
rem <Version> (G3-45) so the demo can rev independently.
set VERSION=
for /f "tokens=3 delims=<>" %%a in ('type DearImGuiKSP\DearImGuiKSP.csproj ^| findstr /c:"<Version>"') do set VERSION=%%a
if not defined VERSION (echo Could not read ^<Version^> from DearImGuiKSP\DearImGuiKSP.csproj & exit /b 1)
set DEMOVERSION=
for /f "tokens=3 delims=<>" %%a in ('type DearImGuiKSPDemo\DearImGuiKSPDemo.csproj ^| findstr /c:"<Version>"') do set DEMOVERSION=%%a
if not defined DEMOVERSION (echo Could not read ^<Version^> from DearImGuiKSPDemo\DearImGuiKSPDemo.csproj & exit /b 1)
echo Packaging library version %VERSION%, demo version %DEMOVERSION%

rem --- 4. Verify every expected input exists before staging (fail loud, don't ship partial).
rem Docs are listed by name, not globbed: a "for %%f in (docs\*.md)" loop only iterates
rem files that exist, so an existence check inside it can never fire (G3-42).
rem settings.cfg is checked as a dev-tree input even though it is excluded from the zip.
set MISSING=0
for %%f in (
  "GameData\DearImGuiKSP\Plugins\DearImGuiKSP.dll"
  "GameData\DearImGuiKSP\PluginData\DearImGuiKSPNative.dll"
  "DearImGuiKSPNative\build\DearImGuiKSPNative.pdb"
  "GameData\DearImGuiKSP\Fonts\IBMPlexSans-Regular.ttf"
  "GameData\DearImGuiKSP\Fonts\OFL.txt"
  "GameData\DearImGuiKSP\Textures\toolbar.png"
  "GameData\DearImGuiKSP\settings.cfg"
  "GameData\DearImGuiKSP\DearImGuiKSP.version"
  "GameData\DearImGuiKSP\Readme.txt"
  "GameData\DearImGuiKSP\License.txt"
  "GameData\DearImGuiKSPDemo\Plugins\DearImGuiKSPDemo.dll"
  "GameData\DearImGuiKSPDemo\DearImGuiKSPDemo.version"
  "GameData\DearImGuiKSPDemo\Readme.txt"
  "GameData\DearImGuiKSPDemo\License.txt"
  "CHANGELOG.md"
  "docs\00-getting-started.md"
  "docs\10-api-fundamentals.md"
  "docs\20-widgets.md"
  "docs\30-theming.md"
  "docs\40-plotting.md"
  "docs\50-animation.md"
  "docs\60-migration-from-imgui.md"
  "docs\70-troubleshooting.md"
) do if not exist %%f (echo MISSING INPUT: %%f & set MISSING=1)
if %MISSING%==1 (echo Aborting: expected input files are missing & exit /b 1)

rem --- 5. Stage library package: GameData\DearImGuiKSP\ recursive, no .pdb, + Docs.
rem /XF settings.cfg keeps player settings out of the zip (G2-16); the GameData
rem staging tree itself still carries it for dev mirroring.
if exist dist\staging rmdir /s /q dist\staging
if not exist dist mkdir dist
robocopy GameData\DearImGuiKSP dist\staging\lib\GameData\DearImGuiKSP /MIR /XF *.pdb settings.cfg >nul
if %ERRORLEVEL% GEQ 8 (echo Staging library GameData tree FAILED & exit /b 1)
mkdir dist\staging\lib\GameData\DearImGuiKSP\Docs >nul
copy /y docs\*.md dist\staging\lib\GameData\DearImGuiKSP\Docs\ >nul
copy /y CHANGELOG.md dist\staging\lib\GameData\DearImGuiKSP\Docs\ >nul

rem --- 6. Stage demo package: GameData\DearImGuiKSPDemo\ recursive, no .pdb
robocopy GameData\DearImGuiKSPDemo dist\staging\demo\GameData\DearImGuiKSPDemo /MIR /XF *.pdb >nul
if %ERRORLEVEL% GEQ 8 (echo Staging demo GameData tree FAILED & exit /b 1)

rem --- 7. Zip (Compress-Archive on the staged GameData folder keeps GameData\ at zip root).
rem -ErrorAction Stop + try/catch (G3-44): Compress-Archive per-entry errors are
rem non-terminating and exit 0 by default, so the errorlevel guard alone could report
rem success with a truncated zip. The post-hoc existence check catches a silent no-op.
powershell -NoProfile -Command "try { Compress-Archive -Path 'dist\staging\lib\GameData' -DestinationPath 'dist\DearImGuiKSP-%VERSION%.zip' -Force -CompressionLevel Optimal -ErrorAction Stop } catch { Write-Host $_.Exception.Message; exit 1 }"
if errorlevel 1 (echo Zipping library package FAILED & exit /b 1)
if not exist "dist\DearImGuiKSP-%VERSION%.zip" (echo Library zip missing after Compress-Archive & exit /b 1)
powershell -NoProfile -Command "try { Compress-Archive -Path 'dist\staging\demo\GameData' -DestinationPath 'dist\DearImGuiKSPDemo-%DEMOVERSION%.zip' -Force -CompressionLevel Optimal -ErrorAction Stop } catch { Write-Host $_.Exception.Message; exit 1 }"
if errorlevel 1 (echo Zipping demo package FAILED & exit /b 1)
if not exist "dist\DearImGuiKSPDemo-%DEMOVERSION%.zip" (echo Demo zip missing after Compress-Archive & exit /b 1)

rem --- 8. Archive the native PDB for this release (symbols stay dev-side, out of the zip).
if not exist dist\symbols mkdir dist\symbols
copy /y DearImGuiKSPNative\build\DearImGuiKSPNative.pdb "dist\symbols\DearImGuiKSPNative-%VERSION%.pdb" >nul
if errorlevel 1 (echo Archiving native PDB FAILED & exit /b 1)

rmdir /s /q dist\staging
echo.
echo dist\DearImGuiKSP-%VERSION%.zip
echo dist\DearImGuiKSPDemo-%DEMOVERSION%.zip
echo dist\symbols\DearImGuiKSPNative-%VERSION%.pdb
echo Done.
