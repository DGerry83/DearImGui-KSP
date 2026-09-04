# Chunk Contract: Theme Engine + Presets + Palette + Theme Default
## Plan: DearImGui-KSP Pre-Release Feature Wave
## Date: 2026-09-04
## Chunk ID: C8
## Advances Milestone: M3 (KSP theme)

### Scope
The theme preset engine end-to-end: named value tables applied to the global ImGui style
at startup (after font load) and re-applied on settings change. Gradients render in C9;
this chunk stores gradient params in the preset but only wires colors/vars.

1. **Native style setters (new exports, DllImport-bound — the DLL is already
   LoadLibrary'd by NativeBridge before any DllImport fires, same as the C2 cimgui
   externs).** cimgui exports no per-field style setters, so add four tiny pass-throughs:
   - `int DearImGuiKSPNative_SetStyleColor(int idx, float r, float g, float b, float a)` — writes `ImGui::GetStyle().Colors[idx]`; bounds-checked; returns 0 ok / 1 no context / 2 bad index.
   - `int DearImGuiKSPNative_SetStyleVarFloat(int idx, float v)` / `int DearImGuiKSPNative_SetStyleVarVec2(int idx, float x, float y)` — writes the matching `ImGuiStyle` field via an internal switch over the `ImGuiStyleVar_*` subset the presets use (WindowRounding, WindowBorderSize, WindowPadding, FrameRounding, FrameBorderSize, FramePadding, ItemSpacing, GrabRounding, GrabMinSize, ScrollbarRounding, TabRounding, ChildRounding, PopupRounding); unknown idx → return 2, write nothing.
   - `int DearImGuiKSPNative_StyleColorsDark()` — `ImGui::StyleColorsDark(&ImGui::GetStyle())`; the "dark" preset's exactness comes from calling this, not from a managed table.
   Files: `src/ContextHost.cpp/.h` (implementation), `src/DearImGuiKSPNative.cpp` (exports). All three build scripts already cover these TUs — no script edits.

2. **Managed bindings** — externs in `Interop/ImGuiNative.cs` + safe wrappers in
   `Interop/ImGuiInternal.cs` per the C2 pattern (Cdecl, per-extern signature comments,
   int returns). Params are primitives (`int`, `float`) — no new structs.

3. **`Application/Theming/` (new public surface is minimal; engine internals stay internal):**
   - `KspPalette.cs` — `internal static class` of `Color32` constants from spec §6.1 (window bg top/bottom, title bar, button gradient top/bottom, frame bg, grab, radio fill, text variants, green/orange accents). Header comment: values are starting points, tuned in-game per spec §13.
   - `ThemePresets.cs` — `internal sealed class ThemePreset` (name, color table as `(ImGuiCol, Color32)[]`, var table as `(ImGuiStyleVar, float)` / `(ImGuiStyleVar, Vector2)` entries, gradient params for C9) + `static ThemePreset Ksp()` and a marker for Dark (dark applies via the native reset + zero overrides).
   - `ThemeEngine.cs` — `internal sealed class`: `Apply(ThemePreset)` → native `StyleColorsDark()` first (both presets share the stock-dark base so unmapped colors are never stale), then every color/var override. Subscribes to `SettingsModel.Changed`; sets a dirty flag when `Theme` changed; `FrameLoopOrchestrator` applies at frame start when dirty (never mid-callback). Public read: add `DearImGuiKSP.CurrentTheme` (string, XML-doc'd) to the facade.
   - Remove the placeholder `Application/Theming/README.md`.

4. **Settings/default flip (D25)**:
   - `LibraryConfig.DefaultTheme` `"dark"` → `"ksp"`.
   - `SettingsModel.NormalizeTheme` becomes real: `"ksp"`/`"dark"` pass through (case-insensitive, normalized to lowercase); anything else → `"ksp"` + one log line `Unknown theme '<name>'; using 'ksp'.` (spec §7, Info level, via the model's existing logging path — check how it logs; if it has none, log at the apply site in ThemeEngine instead and say which).
   - `GameData/DearImGuiKSP/settings.cfg` (tracked source): update `theme = dark` → `theme = ksp`; add `font = IBMPlexSans` line if the file enumerates all keys.
   - Startup wiring: `DearImGuiKSPAddon.Start` applies the theme after `LoadStartupFont()`, before `MarkRunning()`.

5. **Tests** — `tests/Application.Tests/Theming/ThemePresetsTests.cs` (NEW): ksp table contains the spec §6.1 anchors (e.g. button gradient top rgb(102,114,135), title bg rgb(57,72,90), text off-white rgb(236,236,236), radio-active light green rgb(181,252,0)); ksp overrides ≠ stock dark at those anchors; every table entry's enum index is in range. `SettingsModelTests.cs` +: theme default is "ksp", unknown theme normalizes to "ksp", "DARK"/"KSP" case-normalize, round-trip through the fake store.

### Inputs (must exist before starting)
- M2 verified (C4–C7 done; I-03 fix landed; build 0/0, tests 63/63).
- C1's public `ImGuiCol`/`ImGuiStyleVar` enums; C2's binding pattern.
- `VisualReferenceMaterial/KSP_Theme_Palette_Notes.md` for the palette (RGB values also in spec §6.1).

### Outputs (must be created/changed)
- `DearImGuiKSPNative/src/ContextHost.h`, `ContextHost.cpp`, `DearImGuiKSPNative.cpp` — 4 exports.
- `DearImGuiKSP/Interop/ImGuiNative.cs`, `ImGuiInternal.cs` — 4 externs + wrappers.
- `DearImGuiKSP/Application/Theming/{KspPalette,ThemePresets,ThemeEngine}.cs` — NEW (README.md removed).
- `DearImGuiKSP/Application/SettingsModel.cs`, `LibraryConfig.cs`, `Application/FrameLoopOrchestrator.cs` (dirty-flag apply at frame start), `Application/DearImGuiKSP.cs` (`CurrentTheme`), `Infrastructure/DearImGuiKSPAddon.cs` (startup apply call) — minimal edits.
- `GameData/DearImGuiKSP/settings.cfg` — theme default.
- `tests/Application.Tests/Theming/ThemePresetsTests.cs` — NEW; `SettingsModelTests.cs` — extended.

### Constraints
- "dark" must be byte-exact stock ImGui dark: implement it as native `StyleColorsDark()` + zero overrides — never a hand-copied table (regression-gated, spec §5.1).
- Theme application never allocates per frame: dirty-flag apply happens once per change, not per frame; steady-state cost is one bool check.
- §5.9 checklist: Check 1 none (style writes are context-local); Check 2 zero (apply on change only); Check 3 N/A (no resources).
- Public additions are exactly `DearImGuiKSP.CurrentTheme` — everything else internal. XML docs on it.
- Do NOT implement gradient rendering, circular radio drawing, or toggle widgets (C9/C10).
- Rebuild all three native scripts; harness must still print HARNESS PASS.

### Verification
- `dotnet build` 0/0; `dotnet test` — 63 baseline + new cases green (report new total).
- Native: 3 builds 0 errors; harness PASS; export-table check lists the 4 new exports.
- xUnit: preset anchor tests + settings normalization tests pass.
- In-game (deferred to M3 gate, user-assisted): "ksp" applies by default on a fresh settings.cfg; live switch to "dark" matches stock; visual pass vs `UIStylingRef.png`.
- Milestone gate M3 closes after C9 + C10 + the in-game visual sign-off.

### Rollback
- Revert all listed files; re-run `build.bat` to restore the prior native DLL; rebuild managed to confirm 63/63.
