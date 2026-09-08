# Chunk Contract: C11 — Packaging & release metadata
## Plan: Opus Review Remediation (`notes\plans\opus-review-remediation-contracts.md`)
## Date: 2026-09-07
## Chunk ID: C11
## Advances Milestone: Post-1.0.0 remediation wave (G2-16, G3-04, G3-05, G3-37, G3-38, G3-42, G3-44, G3-45, G3-46 = G4-06)

### Scope
- **G2-16** — `package_release.bat` step 5 robocopy gains `/XF settings.cfg`: the library release zip no longer ships `settings.cfg`, so extracting an upgrade can no longer reset the player's saved UI settings. The file stays in `GameData\DearImGuiKSP\` for dev mirroring (C06's `font` key untouched) and stays in the step-4 input check as a dev-tree input. Runtime tolerance verified: `SettingsStore.Load` (`DearImGuiKSP/Infrastructure/SettingsStore.cs:42-54`) treats a missing file as `ConfigNode.Load == null` → warn + `CreateDefaults()`, so first install without settings.cfg works and the first debounced save creates the file.
- **G3-04 + G3-38** — both csproj `KSPVersionFile` items gain `<KSP_Version>1.12</KSP_Version>`, `<KSP_Version_Min>1.12</KSP_Version_Min>`, `<KSP_Version_Max>1.12</KSP_Version_Max>` metadata, overriding KSPBuildTools 1.1.1's `ItemDefinitionGroup` defaults (KSP_Version 1.12 / Min 1.8 / Max 1.12, `KSPBuildTools.props:63-67`). Shipped `.version` files now gate AVC/CKAN to 1.12.x. The demo's generated `.version` did emit KSP_VERSION via the KSPBT defaults (the sweep's "emits no KSP_VERSION" refers to the source template); the fix pins the range for both regardless.
- **G3-05** — both source `.version` files: `URL` changed from the repo HTML page to the canonical raw form `https://raw.githubusercontent.com/DGerry83/DearImGui-KSP/main/<ProjectDir>/<File>.version` (remote `origin` = github.com/DGerry83/DearImGui-KSP, default branch `main`). Because the generated `GameData\*.version` is gitignored, the raw URL serves the source template — so the source files also gained `VERSION`/`KSP_VERSION`/`KSP_VERSION_MIN`/`KSP_VERSION_MAX` (1.0.0 / 1.12) to make the AVC-remote file self-contained. KSPBuildTools' JsonPoke overwrites all four fields at build, so the shipped file is unchanged in shape. **Gate D must keep the source `.version` VERSION fields in sync at each release** (no bump performed here per the hard rule).
- **G3-37** — `DearImGuiKSPDemo.csproj` gains the same `CopyTextFiles` target the library has: `DearImGuiKSPDemo/Readme.txt` (new, minimal: what it is, install path, DearImGuiKSP 1.x dependency, MIT) → `Readme.txt`, root `LICENSE.txt` → `License.txt`, both into `GameData\DearImGuiKSPDemo\` (and mirrored to the pinned game, matching library behavior).
- **G3-42** — step 4 now checks `GameData\DearImGuiKSP\License.txt` plus the two new demo text files, and the unreachable `for %%f in (docs\*.md) do if not exist` loop (a glob loop only iterates existing files, so the check could never fire) is replaced by the eight docs files listed by name.
- **G3-44** — both `Compress-Archive` calls wrapped in `try { ... -ErrorAction Stop } catch { ...; exit 1 }` so PS 5.1 per-entry non-terminating errors fail the run, plus a post-hoc `if not exist` check on each zip to catch a silent no-op.
- **G3-45** — step 3 reads `DEMOVERSION` from `DearImGuiKSPDemo.csproj` `<Version>` (fail-loud if absent); the demo zip and final echo use `%DEMOVERSION%`.
- **G3-46** — both csproj files: `<Copyright>Copyright © DGerry83 2026</Copyright>` (was "DGerry"), plus `<Authors>DGerry83</Authors>` and `<Company>DGerry83</Company>`, matching LICENSE.txt/README.

### Inputs
- Triage sweep rows G2-16, G3-04, G3-05, G3-37, G3-38, G3-42, G3-44, G3-45, G3-46 (all VALID WORK), `notes\active\2026-09-07_Bug_OpusReviewTriage\TRIAGE_SWEEP.md`.
- C10's step-4 edit (IBM Plex Medium check removed) verified consistent: the Medium TTF stays out of the check list.
- C06's `font` key in `GameData\DearImGuiKSP\settings.cfg`: untouched; G2-16 excludes the file from the zip only.

### Outputs
- Changed: `package_release.bat`, `DearImGuiKSP/DearImGuiKSP.csproj`, `DearImGuiKSP/DearImGuiKSP.version`, `DearImGuiKSPDemo/DearImGuiKSPDemo.csproj`, `DearImGuiKSPDemo/DearImGuiKSPDemo.version`.
- New: `DearImGuiKSPDemo/Readme.txt`.
- Untouched per hard rules: no version numbers bumped; no runtime code changed; `GameData\` staging contents unchanged (settings.cfg still present, still mirrored to the game).

### Constraints
- No runtime code; P3/Presentational per the plan. §5.9: N/A.
- No version bumps — the SemVer release decision is Gate D's. The `VERSION: "1.0.0"` added to the source `.version` files restates the current version, not a bump.

### Verification
- `cmd //c package_release.bat` end-to-end from repo root: green (native release build, managed release build 0 warnings/0 errors, gate, staging, both zips). Re-run clean after both error-injection tests; `dist\staging` cleaned up by the script.
- `unzip -l dist\DearImGuiKSP-1.0.0.zip` (17 files): no `settings.cfg`; contains Plugins/DearImGuiKSP.dll, PluginData/DearImGuiKSPNative.dll, Fonts/IBMPlexSans-Regular.ttf + OFL.txt, Textures/toolbar.png, DearImGuiKSP.version, Readme.txt, License.txt, Docs/ (8 md + CHANGELOG.md).
- `unzip -l dist\DearImGuiKSPDemo-1.0.0.zip` (4 files): Plugins/DearImGuiKSPDemo.dll, DearImGuiKSPDemo.version, Readme.txt, License.txt.
- Shipped `.version` in both zips (`unzip -p`): `KSP_VERSION`/`MIN`/`MAX` = 1.12, `URL` = raw.githubusercontent.com form, `VERSION` = 1.0.0.0 (poked from FileVersion at build).
- Error injection 1 (G3-42 gate): renamed `docs\40-plotting.md` away → step 4 printed `MISSING INPUT: "docs\40-plotting.md"` / `Aborting: expected input files are missing`, exit code 1. Reverted.
- Error injection 2 (G3-44 zip): created a directory at the `dist\DearImGuiKSP-1.0.0.zip` destination → Compress-Archive threw under `-ErrorAction Stop` (`Access to the path ... is denied.`), script printed `Zipping library package FAILED`, exit code 1. Reverted (the prior good zip was restored from backup).
- `dotnet build DearImGui-KSP.slnx`: 0 errors, 0 warnings.
- dist\ zips left uncommitted (`/dist/` is gitignored).

### Rollback
- Revert the five changed files; delete `DearImGuiKSPDemo/Readme.txt`.
