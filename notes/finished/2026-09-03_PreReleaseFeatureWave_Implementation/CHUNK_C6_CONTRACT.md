# Chunk Contract: Font Settings + FontResolver + Fallback
## Plan: DearImGui-KSP Pre-Release Feature Wave
## Date: 2026-09-03
## Chunk ID: C6
## Advances Milestone: M2 (font pipeline)

### Scope
The managed settings/resolution half of the font pipeline — everything except the native
export (C4) and the startup wiring that consumes this (C5). **Managed-only chunk.**

1. **`font` setting end-to-end in settings plumbing**:
   - `LibraryConfig.cs`: add `DefaultFont = "IBMPlexSans"`, `EmbeddedFontName = "ProggyClean"`, `FontsDir = "GameData/DearImGuiKSP/Fonts"` (relative to KSP root, same convention as `SettingsPath`).
   - `LibrarySettings.cs`: add `Font` (string) field.
   - `SettingsStore.cs`: load/save the `font` key in the existing ConfigNode (root `DEARIMGUIKSP_SETTINGS`); absent key → `DefaultFont`. `SettingsFormatVersion` stays 1 (additive key with default, same as existing pattern).
   - `SettingsModel.cs`: add `Font` property with normalization — trim; empty/null → `DefaultFont`; persist-on-set and `Changed` event exactly like the existing properties. Do NOT touch `NormalizeTheme` (C8 owns theme).

2. **New `DearImGuiKSP/Infrastructure/FontResolver.cs`** — thin, KSP-aware (Infrastructure
   is the only KSP-touching layer):
   - Input: the normalized `font` setting + `fontScale`.
   - Rules (spec §5.2, §9): `"ProggyClean"` → embedded-default signal. `"IBMPlexSans"` → `Fonts/IBMPlexSans-Regular.ttf` (primary) + `Fonts/IBMPlexSans-Medium.ttf` (secondary, for theme emphasis). Any other value → treated as a TTF filename resolved under `Fonts/` (append `.ttf` if no extension).
   - Resolution uses `KSPUtil.ApplicationRootPath` + `LibraryConfig.FontsDir`; a file that does not exist (or `File.ReadAllBytes` probe fails) → fallback signal.
   - Output type (define in Infrastructure, e.g. `FontResolution`): `UseEmbeddedDefault` (bool), `PrimaryPath` (string or null), `SecondaryPath` (string or null), `SizePixels` (float, base size × fontScale — base size is a named constant, start 15f, tunable).
   - **Pure resolution only: no logging, no native calls.** The consumer (C5) logs the fallback line and calls the bridge.

3. **Tests** — extend `tests/Application.Tests/SettingsModelTests.cs`: font default is `IBMPlexSans`, empty/whitespace normalizes to default, set persists once and fires `Changed`, round-trips through `FakeSettingsStore`. FontResolver itself is Infrastructure/KSP-bound — not unit-tested (per the repo's Infrastructure test deferral).

### Inputs (must exist before starting)
- M1 verified (C1–C3 done, in-game gate passed 2026-09-03).
- `SettingsStore.cs`, `SettingsModel.cs`, `LibrarySettings.cs`, `LibraryConfig.cs` as-is.
- `tests/Application.Tests/TestDoubles.cs` (`FakeSettingsStore`) as-is.

### Outputs (must be created/changed)
- `DearImGuiKSP/LibraryConfig.cs` — new constants.
- `DearImGuiKSP/Application/LibrarySettings.cs` — `Font` field.
- `DearImGuiKSP/Application/SettingsModel.cs` — `Font` property + normalization.
- `DearImGuiKSP/Infrastructure/SettingsStore.cs` — `font` key load/save.
- `DearImGuiKSP/Infrastructure/FontResolver.cs` — NEW.
- `tests/Application.Tests/SettingsModelTests.cs` — new font cases.

### Constraints
- Do NOT touch `theme`/`NormalizeTheme` (C8's scope), the native side, or `NativeBridge`/`INativeBridge` (C4/C5's scope).
- SettingsModel changes must follow the existing persist-on-set + `Changed` event pattern exactly; the 59-test suite plus new tests must pass.
- FontResolver does not log and does not call native code; fallback is a signal, not an action.
- No public API changes (FontResolver and FontResolution are internal).
- §5.9 checklist: Check 1 N/A; Check 2 N/A (startup-only path, not per-frame); Check 3 N/A (no resources retained — the file probe must not leave open handles; use File.Exists/File length check, not an open stream).

### Verification
- `dotnet build DearImGui-KSP.slnx` — 0 errors, 0 new warnings.
- `dotnet test DearImGui-KSP.slnx` — 59 baseline + new font cases all passing (report the new total).
- Grep evidence: `NormalizeTheme` untouched; no changes under `DearImGuiKSPNative\`; no edits to `NativeBridge.cs` / `INativeBridge.cs`.
- Milestone contribution: C6 supplies the resolver contract C5 consumes; M2 gate verifies after C5.

### Rollback
- Revert the 6 touched/added files to the pre-chunk checkpoint; rebuild to confirm 59/59.
