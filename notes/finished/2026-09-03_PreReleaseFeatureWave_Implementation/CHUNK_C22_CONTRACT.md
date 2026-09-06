# Chunk Contract: Modder Docs Set A (Getting Started, API, Widgets, Theming)
## Plan: DearImGui-KSP Pre-Release Feature Wave
## Date: 2026-09-04
## Chunk ID: C22
## Advances Milestone: M7 (documentation)

### Scope
Write docs set A (spec §8.2, D31), replacing the placeholder stubs in `docs/`:
`00-getting-started.md`, `10-api-fundamentals.md`, `20-widgets.md`,
`30-theming.md`. Parallel with C23 (set B) — disjoint files. Audience: a KSP
modder who has never used the library; the M7 gate is a from-zero dry run against
these docs ALONE, so accuracy against the REAL public API is the whole game.
Read the actual source for every signature you document — never invent API.

1. **`docs/00-getting-started.md`** — install (GameData layout, what ships),
   dependency declaration (`KSPAssemblyDependencyEqualMajor("DearImGuiKSP", x, y)`
   — get the exact current major/minor from `DearImGuiKSP/DearImGuiKSP.version`
   and cite the versioning rule D17), the minimal working consumer: availability
   check → `Register(id, callback)` → first window via `ImGuiEx.Window` →
   `Unregister` on destroy. Include a complete minimal class example.

2. **`docs/10-api-fundamentals.md`** — the programming model: registration and
   the per-frame callback (widget calls valid ONLY inside it), `IsAvailable`
   semantics (suspended = pause, don't tear down), the scope system
   (`ImGuiEx` Window/Child/TabBar/TabItem + their `Visible` and pairing rules),
   public types (`Vector2`/`Color`/`Color32`, `ImGuiCol`/`ImGuiStyleVar`,
   push/pop style), availability-race rules (never throw; warn+ignore), the fault
   barrier (what happens when a consumer throws), immediate-mode ID rules
   (`##` ids; the spinner `id` param; ISSUES #005 lesson), settings.cfg surface
   (theme, font, uiScale/fontScale, clampWindowsToViewport, verboseLogging — read
   `LibrarySettings.cs`/`SettingsModel.cs` for the real keys and defaults).

3. **`docs/20-widgets.md`** — the full widget catalog with real signatures and
   one minimal example each: Text/TextColored, Button, GradientButton (both
   overloads), SliderFloat, InputText, Toggle (+ToggleFlags), RadioButton (circular
   KSP style), Knob (+KnobVariant/KnobFlags), Wheel (+WheelOrientation), Spinner
   (+SpinnerType — note the 15-type curated subset, the `id` param rule for
   same-type duplicates, and the tint behavior: RainbowMix hue comes from the
   tint, Atom dots are fixed RGB — ISSUES #006 known limits), TabBar/TabItem via
   ImGuiEx, scroll regions, Dummy/SetCursorY layout, ImGuiDraw custom drawing
   (cursor anchor + Dummy canvas pattern). Note known layout gaps honestly:
   no public SameLine yet (stack vertically or use ImGuiDraw).
   Sources: `Application/DearImGuiKSP.cs`, `Application/Api/*.cs`.

4. **`docs/30-theming.md`** — the two presets ("ksp" default, "dark" = exact stock
   ImGui dark), `CurrentTheme` read, how to change the theme (settings.cfg, applies
   next frame), style push/pop for local overrides, gradient helpers
   (`ImGuiGradients`), the public `KspPalette` constants (read the real list),
   colorblind/accessibility note (spec §6.4: accents always pair with labels).
   Sources: `Application/Theming/{ThemePresets,KspPalette,ThemeEngine}.cs`.

### Constraints
- **Accuracy over coverage**: every signature, enum value, default, and setting
  key must match the source — cite nothing you haven't read. Cross-check against
  the XML docs in code.
- D31: no emojis, no symbol glyphs (ASCII only in examples; "dV" not "Δv").
- Code examples are C# for KSP mods (Unity 2019.4 era — C# 7.3; no modern syntax).
- Match the existing docs' tone/front matter if the stubs have any; each file
  links its neighbors (00 → 10 → 20 ...).
- Docs-only chunk: NO code changes anywhere.

### Verification
- Self-review every documented signature against source (state the files read).
- `dotnet build`/`dotnet test` untouched-green (nothing to build — state so).
- Modder-from-zero dry run is the M7 gate (user-assisted) after C23/C24.

### Rollback
- Restore the four placeholder stubs from git.
