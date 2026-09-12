# Feature Request — Consumer-Required API Additions for DearImGui-KSP 1.3.0

**Date:** 2026-09-12
**Requesting consumer:** CinematicRecorder (CR) — KSP video-capture mod, migrating
its entire UI from Unity IMGUI to DearImGui-KSP.
**Request origin:** CR migration design spec (finalized, user-validated):
`C:\Users\Matt\source\repos\CinematicRecorder\ReferenceNotes\active\2026-09-12_DesignSpec_DearImGuiUIMigration\DESIGN_SPEC.md`
(decision D-U3, questions Q-U3/Q-U4 — approved by the library owner).
**Requested release vehicle:** library minor **1.3.0** (non-breaking per the
versioning contract; consumers declare `KSPAssemblyDependencyEqualMajor("DearImGuiKSP", 1, 3)`).
**Blocking status:** CR's migration does not begin until these ship. Horizontal
layout (FR-1) is the hard blocker; combo (FR-2) and tooltip (FR-3) are needed
shortly after and should ride the same minor.

---

## How to read this document

Each request specifies: the consumer use cases (concrete, from CR's shipping UI),
the proposed public API shape, behavior/edge-case requirements, and the required
docs/demo updates. Proposed shapes are **suggestions consistent with the library's
existing facade conventions** (`ImGuiEx` static class, `using`-scope pattern,
curated cimgui subsets — not raw bindings exposure); the implementing agent should
follow the library's own layering rules (`Application/` = API surface, `Interop/`
= P/Invoke bindings, no KSP APIs in `Application/`) and adjust names to fit.

Routing for the implementing agent: this is a Feature-class request per
`FlyByWire v3 Router.md` → `BugfixPlanning.md` (feature mode), or treat as three
small features sharing one release. All three must land in one minor (1.3.0) with
docs updated, because CR's dependency declaration and migration spec assume the
combined surface.

---

## FR-1 — Horizontal layout (row scope / SameLine)  ·  HARD BLOCKER

### Consumer use cases (all from CR's current IMGUI UI)
1. **FPS selector rows**: `◀  [label]  ▶  Lock-toggle` on one line (capture FPS and
   playback FPS selectors in the main settings dialog).
2. **Duration row**: `-5s` button, text field, `+5s` button, `∞` button on one line.
3. **Zoom curve grid**: 2×2 grid of curve buttons (Linear/EaseIn/EaseOut/EaseInOut).
4. **16-slot camera panel**: a **4×4 button grid** — the single most
   layout-critical widget in CR. Each cell is a square button with a short label
   and per-state tinting.
5. Preset management row: text field + Save/Delete/Load buttons on one line.
6. Label + control pairs that must sit on one row for compactness (slider with a
   trailing value readout, toggle with a trailing status badge).

Without this, the migration guide's own advice ("stack horizontal groups
vertically") produces a UI that is strictly worse than the IMGUI original — the
stated purpose of the migration is compactness, so this is not acceptable as the
final answer. (Consumer-side ImGuiDraw canvas grids were considered and rejected
by the library owner in favor of a real API.)

### Proposed API shape
Minimal, in keeping with the curated-facade style:

```text
using (var row = ImGuiEx.Row())            // wraps igSameLine between items
{
    ImGuiEx.Button("◀");  ImGuiEx.Text(label);  ImGuiEx.Button("▶");
}
// RowScope.Dispose() ends the row (returns to vertical flow)
```

- `ImGuiEx.Row()` → scope; each widget inside after the first calls
  `igSameLine(0, spacing)` before itself. Configurable spacing overload optional
  (`Row(float spacing)`); default = current style item spacing.
- If the maintainers prefer exposing `SameLine()` directly instead of a scope,
  that also satisfies the request; the scope form is preferred because it matches
  the library's existing `Window`/`TabBar`/`ScrollRegion` idiom and prevents the
  classic "SameLine leaked past the intended group" consumer bug.

### Behavior requirements / edge cases
- Must compose correctly inside `autoResize: true` windows (row width participates
  in fit-to-content measurement) and inside `ScrollRegion`.
- Must compose with `CollapsingHeader`, tabs, and toggles/sliders/InputText.
- Must respect the global UI scale (spacing via style vars, not pixel constants —
  remember knob/plot pixel sizes are explicitly NOT scaled; rows must not
  replicate that mistake for standard widgets).
- Multiple independent rows per window; nested rows explicitly NOT required (may
  assert/disallow in debug builds).
- Grid use case: consumers will emit rows of N buttons in a loop — identical
  labels across cells are plausible, so the docs example should show `##` id
  suffixing for uniqueness (already the library's convention for spinners).

### Docs / demo updates required
- `docs/20-widgets.md`: new Layout section (replaces/updates the "no public
  SameLine — vertical-first only" note at ~20:644-647).
- `docs/60-migration-from-imgui.md`: add mapping-table row
  `GUILayout.BeginHorizontal` / horizontal groups → `ImGuiEx.Row()`; remove or
  amend the "stack vertically for now" guidance.
- Demo mod: add a row/grid example (a 4×4 button grid mirroring CR's use case is
  ideal — it doubles as the manual test page).

---

## FR-2 — Combo / dropdown  ·  needed by CR 0.2.4 (presets) and 0.3.0 (pickers)

### Consumer use cases
1. **Preset selector** (CR 0.2.4, phase U3): choose from a list of named camera
   presets; typical count 1–20.
2. **Celestial-body selector** (CR 0.3.0 camera library): all KSP bodies, ~16
   stock, more with planet packs.
3. **Part picker** (CR 0.3.0): parts on the active vessel — potentially 100+
   entries; needs scrolling; a search/filter field would be ideal but is NOT
   required for v1 of this API (CR can pre-filter the list).

### Proposed API shape
```text
// Curated wrapper over igBeginCombo/igEndCombo + igSelectable:
int newIndex;
if (ImGuiEx.Combo("Preset", ref selectedIndex, items, out newIndex)) { ... }
// items: IReadOnlyList<string>; preview = items[selectedIndex]
```
- Returns true on selection change; index-based (matches `RadioButton` int-group
  precedent).
- Long lists must scroll inside the popup (stock ImGui behavior; verify no
  clipping against window edges when the combo sits near the bottom of an
  autoResize window).
- Optional `maxHeightItems` parameter acceptable but not required.

### Behavior requirements / edge cases
- Empty list: preview shows a placeholder (e.g. "(none)") and the popup does not
  open; must not throw.
- `selectedIndex` out of range (e.g. persisted index vs shrunk list): clamp and
  show placeholder; no exception (CR's presets can be deleted between sessions).
- Identity: label doubles as ImGui id per library convention; docs should show
  `##` suffixing for two combos with the same label.
- String encoding: items go through the same UTF-8 path as other widgets.

### Docs / demo updates required
- `docs/20-widgets.md`: Combo section (replacing the "no combo/dropdown" absence
  note at ~20:648-651).
- `docs/60-migration-from-imgui.md`: mapping-table row for IMGUI dropdown
  patterns (CR's current preset list is a hand-rolled toggle-list).
- Demo mod: one combo with a long (50+) item list to exercise popup scrolling.

---

## FR-3 — Tooltip API  ·  needed by CR 0.2.4 (U3 strings) and 0.3.0

### Consumer use cases
1. Hover tooltips on compact controls whose full explanation doesn't fit the
   layout (CR's existing pattern: `new GUIContent(text, tooltip)` + a hand-rolled
   `DrawHoverTooltip`; issue #007 added one for the "Advanced" button).
2. 0.3.0 camera library tooltips (e.g. the slow-motion audio-silence note, the
   velocity-rigidity slider explanation).
3. Any `KnobFlags.ValueTooltip`-style discoverability on dense panels.

### Proposed API shape
```text
ImGuiEx.Button("Advanced");
ImGuiEx.Tooltip("Opens advanced encoding and rendering options.");
// wraps igSetItemTooltip (hover of the previous item)
```
- Also acceptable: an optional `tooltip:` parameter on the common widgets — but
  the standalone call is preferred (works with any widget, matches
  `igSetItemTooltip` semantics, zero signature churn).
- Consider `TooltipFlags` passthrough for delay behavior if trivial; not required.

### Behavior requirements / edge cases
- Rendered above all consumer windows (stock ImGui tooltip layer); follows theme
  and font scale.
- No-op-safe when called without a preceding item (debug assert acceptable).
- Multi-line text (`\n`) must work; long text should wrap at a sane width.

### Docs / demo updates required
- `docs/20-widgets.md`: Tooltip section.
- `docs/60-migration-from-imgui.md`: mapping-table row `GUIContent.tooltip` /
  `GUI.tooltip` → `ImGuiEx.Tooltip` (this mapping is currently absent — grep
  confirms no tooltip API exists anywhere in `DearImGuiKSP/`).

---

## Cross-cutting requirements (apply to all three)

1. **Non-breaking minor**: no changes to existing public signatures; handshake
   version bump only if the protocol actually changes (note: `docs/70` says v7,
   AGENTS.md says v9 shipped in 1.2.0 — fix that doc drift while touching docs).
   Also fix `docs/00-getting-started.md:90` ("current version 1.0.0") → 1.3.0.
2. **Layering**: bindings in `Interop/`, public surface in `Application/`, no KSP
   or engine lifecycle APIs in public signatures (`UnityEngine.CoreModule` math
   structs allowed, D24). Native side should only need new cimgui calls — no
   backend/render-thread changes expected; if any ARE needed, run the
   Native Interop & Hot-Path Checklist (`FlyByWire v3 reference/08-native-interop.md`).
3. **Fault-tolerance contract**: misuse by consumers (e.g. unbalanced scopes) must
   throw or no-op in a way the per-consumer exception isolation handles — never
   corrupt the ImGui stack for other consumers. Debug-build asserts should catch
   it dev-side.
4. **Tests**: add xUnit coverage in `tests/Application.Tests` for the pure managed
   logic (row state tracking, combo index clamping) per the repo's test-layer
   conventions.
5. **Release protocol** (library AGENTS.md): bump library csproj `<Version>` + AVC
   template to 1.3.0 in lockstep; demo bump only if the demo changed (it will —
   new examples) — demo pair bumps too; CHANGELOG sections; `package_release.bat`;
   new GitHub release with both zips; **never replace a published zip in place**.
   The `KSPAssemblyDependencyEqualMajor` stepping-stone (ISSUES #016) does NOT
   apply to a minor bump.
6. **Acceptance (CR-side)**: after 1.3.0 ships, CR declares dependency `(1, 3)`
   and ports: the 4×4 camera grid (FR-1), preset selector (FR-2), and the #007
   tooltip (FR-3) as the smoke test of all three APIs in a real consumer.

## Priority & sequencing guidance

| Order | Item | Why |
|---|---|---|
| 1 | FR-1 horizontal layout | Hard-blocks CR phase U1 (FPS/duration rows are in the main dialog) and U3 (4×4 grid) |
| 2 | FR-2 combo | Blocks CR phase U3 (presets) and all of 0.3.0's pickers |
| 3 | FR-3 tooltip | Blocks nice-to-have polish in U3; CR can ship without tooltips if absolutely necessary, but they are specified in the migration spec |

If only one can make 1.3.0, ship FR-1 and cut a 1.4.0 for the rest — but the
preference is one minor carrying all three.
