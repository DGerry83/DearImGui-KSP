# Chunk Contract: Bundle Font Assets
## Plan: DearImGui-KSP Pre-Release Feature Wave
## Date: 2026-09-03
## Chunk ID: C7
## Advances Milestone: M2 (font pipeline)

### Scope
Pure asset copy — no code.

1. Create `GameData/DearImGuiKSP/Fonts/` and copy in:
   - `IBMPlexSans-Regular.ttf` and `IBMPlexSans-Medium.ttf` from `C:\Users\Matt\source\repos\Fonts\IBM_Plex_Sans\static\`
   - `OFL.txt` from `C:\Users\Matt\source\repos\Fonts\IBM_Plex_Sans\OFL.txt` (OFL obligation, spec §8.1)
2. Verify the managed build's `CopyToGame` target mirrors the new folder into the game install: `$(KSPBT_GameRoot)/GameData/DearImGuiKSP/Fonts/` after `dotnet build DearImGui-KSP.slnx`.
3. Check whether `.gitignore` excludes GameData binaries; record whether the TTFs should be committed (match however the repo already treats `GameData/**` staged binaries — check if `GameData/DearImGuiKSP/PluginData/DearImGuiKSPNative.dll` is tracked). Report the finding; commit only if consistent with repo convention.

### Inputs
- None beyond the verified baseline (M1 done, C6 done).

### Outputs
- `GameData/DearImGuiKSP/Fonts/IBMPlexSans-Regular.ttf`
- `GameData/DearImGuiKSP/Fonts/IBMPlexSans-Medium.ttf`
- `GameData/DearImGuiKSP/Fonts/OFL.txt`
- Mirrored copies in the game install after build.

### Constraints
- No code changes; no csproj changes (the existing `ModFiles` glob already covers new folders — verify this rather than assuming).
- Static TTFs only — never the variable-font files in the parent dir (stb_truetype can't handle them, D29).

### Verification
- Three files exist with nonzero size at both the staging path and the mirrored game path.
- `dotnet build DearImGui-KSP.slnx` — 0 errors (mirroring runs as part of build).

### Rollback
- Delete `GameData/DearImGuiKSP/Fonts/` and the mirrored copy in the game install.
