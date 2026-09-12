# Progress Log: Consumer API Additions — Row / Combo / Tooltip (1.3.0)
## Session: 2026-09-12_Feature_RowComboTooltipApi

## Scope A — Library implementation (M1 Row, M2 Combo, M3 Tooltip + unit tests)
- Status: **M1–M3 COMPLETE** — build green (Debug + Release), 229/229 xUnit tests
  pass (191 pre-existing + 38 new)
- Disagreements / rulings: Phase 0 list below (D1–D8). Submitted for ruling;
  resolved per the recorded proposals under auto mode (no interactive ruling
  available). All resolutions either follow the contract text literally or use
  explicit contract escape hatches — flagged here for parent audit.
- Gate verdicts updated: G1–G5 (Scope A parts) + G10 in GATES.md

### Phase 0 disagreement list (evidence-verified against working tree 2026-09-12)

**D1 — Internal enum home (contract table says `Interop/ImGuiStyleEnums.cs`).**
No such file exists under `Interop/`. Reality: `DearImGuiKSP/Application/Api/ImGuiStyleEnums.cs`
holds only PUBLIC enums (ImGuiCol/ImGuiStyleVar); internal interop enums
(ImGuiWindowFlags, ImGuiInputTextFlags, ImGuiSliderFlags, ImGuiChildFlags,
ImGuiTabBarFlags, ImGuiTabItemFlags) live at the top of
`DearImGuiKSP/Interop/ImGuiNative.cs`.
Resolution (pending audit): internal `ImGuiHoveredFlags` + `ImGuiItemFlags` added
to `ImGuiNative.cs` beside the existing internal enums — the established
convention. Contract row already carries the escape hatch "(or sibling enum file
— implementer confirms home)".

**D2 — `igIsItemHovered` is already bound.**
Contract binding list said to add it; reality: binding at `ImGuiNative.cs:268-272`
(cimgui.h:4455), internal wrapper `IsItemHovered()` at `ImGuiNative.cs:648-651`,
`ImGuiInternal.IsItemHovered()` at `ImGuiInternal.cs:537-540`. No new DllImport
was needed; only a flags-taking internal overload was added for the tooltip path.

**D3 — Hook ALL public overloads.**
Contract listed "RadioButton×2" explicitly but named other widgets without
overload counts. Reality: Toggle×2, Knob×4, Wheel×2, Dummy×2 public overloads
all gate on CanDeclareUi. Resolution (pending audit): `RowItemHook()` inserted
after the `CanDeclareUi` gate in ALL 20 public widget methods (Text,
TextColored, Button, RadioButton×2, Toggle×2, SliderFloat, InputText,
CollapsingHeader, Spinner, Knob×4, Wheel×2, Dummy×2, Combo).

**D4 — Existing internal `SameLine()` wrapper = ZERO spacing.**
`ImGuiNative.SameLine()` (`ImGuiNative.cs:539-541`) calls `igSameLine(0f, 0f)`;
verified in pinned `imgui.cpp SameLine` that only `spacing < 0` selects
`style.ItemSpacing.x` — `0` is a literal zero gap. The Row default therefore
uses the new spacing-parameterized overload; existing wrapper and its callers
(InputText label pairing `DearImGuiKSP.cs:316`, `LibraryControlPanel.cs:115,150`)
left untouched — no behavior change to existing paths.

**D5 — `EndCombo` is conditional inside try/finally.**
Verified pinned `imgui.cpp EndCombo`: asserts "Calling EndCombo() in wrong
window!" when no combo popup is open — EndCombo is only valid when BeginCombo
returned true. Resolution: `try { if (open) { items } } finally { if (open)
EndCombo(); if (disabled) PopItemFlag(); }` — pairing preserved per imgui
semantics on every path including encoding throws.

**D6 — Spinner hook placement.**
Spinner has a second guard after `CanDeclareUi` (out-of-range type → logged
no-op). Resolution: hook literally right after the `CanDeclareUi` gate per
contract wording, before the range check — uniform rule; the programmer-error
path consumes a row slot (worst case: gappy row layout in a logged-error case).

**D7 — `Row()` factory gates on `CanDeclareUi`.**
Contract doesn't state Row behavior outside a callback. Resolution: factory
returns an inert scope without touching `RowState` when `CanDeclareUi` is false
(availability races never assert/throw); nested-row `Debug.Assert` fires only
when a frame is open. Consistent with C01 and the ImGuiEx documented idiom.

**D8 — Tooltip wrap verified: NO double-wrap.**
Verified pinned `imgui.cpp BeginTooltipEx` (12923+) pushes no wrap pos and
`SetTooltipV` = BeginTooltipEx + TextV + EndTooltip only — stock tooltips do
NOT wrap by themselves. `PushTextWrapPos(GetFontSize()*35)` is required (stock
demo value) and does not double-wrap. `ForTooltip = 1 << 12` pinned from
cimgui.h:474; imgui.h:1533 confirms the composite resolves to Stationary |
DelayShort | AllowWhenDisabled (+NoSharedDelay) — stock SetItemTooltip
semantics. `ImGuiItemFlags_Disabled = 1 << 6` pinned from cimgui.h:328.
`ImGuiComboFlags_None = 0` (cimgui.h:411), `ImGuiSelectableFlags_None = 0`
(cimgui.h:401). Empty-combo disabled path verified: `ButtonBehavior` early-outs
hover/press for Disabled items, so the popup never opens.

### Changes (M1 — FR-1 Row)
- **New** `DearImGuiKSP/Application/RowState.cs` — per-frame row state
  (Depth/ItemsInCurrentRow/CurrentSpacing), TryPush (nested rejection), Pop,
  Reset, OnRowItem hook decision. OpenScopeTracker convention.
- **New** `DearImGuiKSP/Application/Api/DearImGuiKSP.Row.cs` — `RowItemHook()`
  (SameLine emission inside a row).
- `DearImGuiKSP/Application/Api/ImGuiEx.cs` — `Row()` / `Row(float)` factories
  + `RowScope` readonly struct (factory-only construction, Dispose pops only
  when pushed, nested = inert + `Debug.Assert` in Debug, unavailable = inert).
- `DearImGuiKSP/Interop/ImGuiNative.cs` / `ImGuiInternal.cs` —
  `SameLine(float spacing)` overloads (`igSameLine(0, spacing)`; spacing < 0 =
  style ItemSpacing.x, which follows UI scale).
- Hook insertions (1 line each, after the CanDeclareUi gate): Text, Button,
  SliderFloat, InputText, Dummy×2 (`Application/DearImGuiKSP.cs`); RadioButton×2;
  Toggle×2; Knob×4; Wheel×2; CollapsingHeader; Spinner; TextColored. Combo hook
  landed with M2. 20 call sites total.
- Reset wiring: `FrameLoopOrchestrator.cs:131` area (`RowState.Reset()` beside
  `OpenScopeTracker.Reset()`) and `DearImGuiKSP.UnwindOpenScopes` (fault-unwind
  backstop, inside the FrameOpen guard).
- **New** `tests/Application.Tests/RowStateTests.cs` — 18 tests: push/pop/reset,
  nested rejection, first-item-never-SameLine'd, factory semantics (incl.
  unavailable/unframe inert, double-dispose), frame-open reset via RunFrame,
  fault-unwind reset + no-frame no-op.

### Changes (M2 — FR-2 Combo)
- `DearImGuiKSP/Interop/ImGuiNative.cs` — internal `ImGuiItemFlags` enum
  (Disabled = 1 << 6, cimgui.h:328); DllImports + wrappers: `igBeginCombo`/
  `igEndCombo` (cimgui.h:4260-4261), `igSelectable_Bool` (:4341),
  `igCloseCurrentPopup` (:4386), `igPushItemFlag`/`igPopItemFlag` (:4166-4167).
- `DearImGuiKSP/Interop/ImGuiInternal.cs` — string wrappers (label/items via
  ToIdUtf8 sentinel-ID path; preview via ToUtf8 display-only).
- **New** `DearImGuiKSP/Application/Api/DearImGuiKSP.Combo.cs` —
  `Combo(string, ref int, IReadOnlyList<string>)` + internal seams
  `ResolveComboPreview` ("(none)" on null/empty/out-of-range, never rewrites the
  ref) and `ApplyComboSelection` (valid click writes index + true). Empty/null →
  PushItemFlag(Disabled) + combo chrome, popup never opens, never throws.
  try/finally: conditional EndCombo (open flag) + conditional PopItemFlag.
- **New** `tests/Application.Tests/ComboLogicTests.cs` — 12 tests: preview
  clamping (null/empty/−1/count/count+N/valid/null-entry), selection mapping
  (valid/already-selected/negative/beyond), facade guard outside frame.

### Changes (M3 — FR-3 Tooltip)
- `DearImGuiKSP/Interop/ImGuiNative.cs` — internal `ImGuiHoveredFlags` enum
  (ForTooltip = 1 << 12, cimgui.h:474); DllImports + wrappers: `igBeginTooltip`/
  `igEndTooltip` (cimgui.h:4367-4368), `igPushTextWrapPos`/`igPopTextWrapPos`
  (:4172-4173), `igGetFontSize` (:4156); `IsItemHovered(ImGuiHoveredFlags)`
  overload over the existing binding. NO variadic P/Invoke introduced.
- `DearImGuiKSP/Interop/ImGuiInternal.cs` — string/scalar re-exposures.
- **New** `DearImGuiKSP/Application/Api/DearImGuiKSP.Tooltip.cs` —
  `Tooltip(string)`: CanDeclareUi gate → null/empty no-op →
  IsItemHovered(ForTooltip) → BeginTooltip → PushTextWrapPos(GetFontSize()*35)
  → Text → PopTextWrapPos (nested try/finally) → EndTooltip (outer try/finally).
  No preceding item = no-op (hover predicate reads zeroed LastItemData).
  Not a layout item — no RowItemHook.
- **New** `tests/Application.Tests/TooltipGuardTests.cs` — 8 tests: enum pins
  (ForTooltip/Disabled against pinned cimgui.h), wrap-factor pin (35), null/
  empty/outside-frame/unavailable no-op guards.

### Verification record
- `dotnet build DearImGui-KSP.slnx` — Debug: succeeded, 0 warnings, 0 errors.
- `dotnet build DearImGui-KSP.slnx -c Release` — succeeded, 0 warnings, 0 errors.
- `dotnet test DearImGui-KSP.slnx` — **Passed! 229 total (Failed: 0, Skipped: 0)**
  = 191 pre-existing + 38 new (18 row + 12 combo + 8 tooltip).
- G1 public-surface evidence: `git diff --numstat` — every modified file is
  insertions-only (deletions = 0 across all 12 modified files); 4 new library
  files + 3 new test files are pure additions. No existing public signature
  modified or removed.

### Native Interop & Hot-Path Checklist chunk verdicts (G10)

**M1 Row chunk (SameLine(float) overload + RowItemHook):**
- Check 1 process-global state: N/A — igSameLine operates on the current ImGui
  context/window cursor only; no DLL search path, env, or OS handle mutation.
- Check 2 hot-path allocation: PASS — zero allocation on the per-frame path
  (float compare + int increment in RowState.OnRowItem; igSameLine takes two
  floats; RowScope is a readonly struct, `using` does not box).
- Check 3 acquisition symmetry: N/A — SameLine is a fire-and-forget cursor call
  with no Begin/End pairing; the managed RowState it consults is pop-symmetric
  via RowScope.Dispose (guarded by _pushed, safe on every path incl. throw) and
  doubly backstopped by the frame-open reset + fault unwind.

**M2 Combo chunk (BeginCombo/EndCombo/Selectable/CloseCurrentPopup/PushItemFlag):**
- Check 1 process-global state: N/A — context-only calls (popup stack lives in
  the context).
- Check 2 hot-path allocation: PASS with justification — per-item ToIdUtf8
  allocation matches the established per-widget per-frame precedent (every
  existing widget encodes its label each frame); iteration runs ONLY while the
  popup is open (user-interaction-bound, not steady-state per-frame); label +
  preview encode once per call. No new buffers added.
- Check 3 acquisition symmetry: PASS — igEndCombo released via try/finally on
  every path, conditionally on the open flag (required: imgui.cpp EndCombo
  asserts when Begin returned false); igPopItemFlag paired under the same
  finally on the disabled-empty path; igCloseCurrentPopup is fire-and-forget
  after a successful selection, inside the open branch only.

**M3 Tooltip chunk (BeginTooltip/EndTooltip/PushTextWrapPos/PopTextWrapPos/GetFontSize + IsItemHovered flags overload):**
- Check 1 process-global state: N/A — context-only calls.
- Check 2 hot-path allocation: PASS — encodes text only while hovered
  (IsItemHovered gates every subsequent call; a non-hovered frame costs one
  predicate call); GetFontSize/PushTextWrapPos are scalar calls, no allocation.
- Check 3 acquisition symmetry: PASS — igBeginTooltip/igEndTooltip under
  try/finally (EndTooltip only when Begin returned true); igPushTextWrapPos/
  igPopTextWrapPos under a nested try/finally so an encoding throw mid-Text
  still pops the wrap pos before EndTooltip runs.

**No variadic P/Invoke introduced** (G4): tooltip composed from
igIsItemHovered + igBeginTooltip + igPushTextWrapPos + igTextUnformatted +
igPopTextWrapPos + igEndTooltip; the variadic igSetItemTooltip/igSetTooltip
remain unbound per `ImGuiNative.cs:112` convention.

## Scope B — Docs (M4)

### Phase 0 disagreement list (evidence-verified against working tree 2026-09-12)

Submitted for ruling; resolved per the recorded proposals under auto mode (no
interactive ruling available), following the Scope A D1–D8 precedent. All
resolutions either follow the contract/work-item text literally or keep the
docs internally coherent — flagged here for parent audit.

**D-B1 — `docs/70-troubleshooting.md` has TWO "version 7" occurrences, not one.**
Work item 3 names only :31 (the log-quote line). Reality: :26 prose says
"(currently expected version **7** on the managed side)" — the same stale
claim one paragraph above the named line.
Resolution: fix both to 9 (shipped handshake verified `ExpectedNativeVersion =
9` at `DearImGuiKSP/Infrastructure/NativeBridge.cs:44`). Leaving :26 would
contradict :31 within the same section.

**D-B2 — `docs/00-getting-started.md:90` version sentence is tied to two more spots.**
Work item 4 mandates `"1.0.0" → 1.3.0` only. Reality: the same paragraph reads
"The current library version is **1.0.0**, so the dependency reads 'major 1,
minor 0'", and the attribute example at :87 shows `(1, 0)`. Changing only the
version string leaves the doc self-contradictory ("1.3.0, so ... minor 0").
Resolution: change all three consistently — version **1.3.0**, prose "major 1,
minor 3", example `(1, 3)` — per the contract's Migration Strategy
("Consumers opt in by declaring `KSPAssemblyDependencyEqualMajor("DearImGuiKSP",
1, 3)` — docs updated to say so"). The later "built against 1.0 loads fine
against library 1.1" sentence stays as a hypothetical semantics illustration.

**D-B3 — `docs/20-widgets.md:13-16` (General conventions) holds a SECOND
"no public SameLine yet" claim.** Work item 1 names only the end-of-page note
(:644-647). The final verification grep ("no remaining 'No public SameLine'")
and sweep item 5 require amending this one too.
Resolution: rewrite the bullet to point at the new Row section; no "no public
SameLine" phrasing retained anywhere in docs.

**D-B4 — `docs/70-troubleshooting.md:139` known-limitation bullet is a stale
claim** ("No public horizontal layout helper ... vertical-first for now") —
squarely inside sweep item 5 ("no SameLine" / "vertical-first").
Resolution: amend in place (not delete) to the accurate residual limitation:
horizontal layout exists but is row-scoped; no public raw `SameLine` for
arbitrary same-line placement.

**D-B5 — `docs/10-api-fundamentals.md` scope table omits `Row`/`RowScope`.**
An omission, not a stale claim. The contract's Files-to-Modify table lists
only docs/20, 60, 70, 00; G6 checks only those four files. Work items 1–6 do
not name 10-api-fundamentals.md.
Resolution: leave untouched (out of assigned scope, Minimal Change Principle);
flag to parent as an optional follow-up (same for the "Only dispose what a
factory returned" paragraph there, which names the other scopes).

**D-B6 — `GradientButton`/`ImGuiDraw` are NOT row-hooked; the Rows section
must not claim "all widgets".** Verified: exactly 20 `RowItemHook()` call
sites (DearImGuiKSP.cs ×6, Combo, Header, Knob ×4, Radio ×2, Spinner, Toggle
×2, TextColored, Wheel ×2) — none in the gradients or draw-list files.
`Tooltip` is explicitly not a layout item (`DearImGuiKSP.Tooltip.cs:31-34`).
Resolution: the Rows section lists the participating widgets explicitly and
names GradientButton/ImGuiDraw/Tooltip as non-participants.

### Changes (per file)

- `docs/20-widgets.md`
  - General-conventions bullet (:13-17): replaced the "no public SameLine
    yet" claim with a pointer to the new Row section.
  - **New** `## Combo` section (after RadioButton): signature, preview/
    selection semantics, `ref`-in-place update, true-on-change return,
    disabled `"(none)"` empty/null path, out-of-range index tolerance,
    `##`-suffix rule for duplicate labels AND duplicate item strings, row
    participation.
  - **New** `## Tooltip` section (after Combo): signature, hover-on-previous-
    item semantics, stock delay, no-op cases, `\n` multi-line, 35×font-size
    wrap, not-a-layout-item note.
  - **New** `## Layout: Rows (ImGuiEx.Row)` section (before "Layout: Dummy /
    SetCursorY"): both factory signatures, scaled default vs unscaled-explicit
    spacing (negative = scaled default), no-nesting rule, managed-state-only
    dispose/reset contract, participating-widget list (explicitly naming
    GradientButton/ImGuiDraw/Tooltip as non-participants per D-B6), and the
    4×4 button-grid example with `##cell_{x}_{y}` ids plus the identical-
    labels-need-##-suffixes note.
  - End-of-page "Known layout gaps" note: removed the "No public SameLine /
    post-release candidate" bullet; amended the absence bullet so combo is no
    longer listed as absent; added the accurate residual-gap bullet
    (horizontal layout is row-scoped; no public raw SameLine).
- `docs/60-migration-from-imgui.md`
  - Concept mapping table: `BeginHorizontal/EndHorizontal` → `ImGuiEx.Row()`
    row; `BeginVertical/EndVertical` → nothing (vertical by default) row;
    hand-rolled dropdown → `Combo` row; `GUIContent.tooltip`/`GUI.tooltip` →
    `Tooltip` row.
  - "Layout" section rewritten: "vertical-first, with a known gap" heading and
    the "no public horizontal-layout helper yet / until it lands" guidance
    removed; Row scope example added; "wait for the layout helpers" closing
    line amended to "use a row (or stack vertically)".
  - Full-port checklist item 5: "Stack horizontally-grouped controls
    vertically for now" → group them in an `ImGuiEx.Row` scope.
- `docs/70-troubleshooting.md`
  - Handshake section: "expected version **7**" prose (:26) and the
    "expected version 7" log-quote line (:31) both → 9 (verified against
    `NativeBridge.cs:44` `ExpectedNativeVersion = 9`; D-B1).
  - Known-limitations bullet (:139): "No public horizontal layout helper ...
    vertical-first for now" amended to "Horizontal layout is row-scoped" with
    the residual no-raw-SameLine limitation and links (D-B4).
- `docs/00-getting-started.md`
  - §2: dependency example `(1, 0)` → `(1, 3)`; "current library version is
    **1.0.0**" → **1.3.0**; "major 1, minor 0" → "major 1, minor 3" (D-B2,
    per contract Migration Strategy).
- **Not touched** (out of assigned scope, flagged for parent): D-B5 —
  `docs/10-api-fundamentals.md` ImGuiEx scope table does not list Row/
  RowScope (omission, not a stale claim; contract Files-to-Modify names only
  docs 20/60/70/00). Also a pre-existing duplicated phrase ("grows the
  content bounds that grows the content bounds") at 60-migration Layout
  bullet 2 — pre-dates this change, left per Minimal Change Principle.

### Verification record

- Stale-claim greps over `docs/` (all zero matches):
  - `No public SameLine|no public horizontal|vertical-first|post-release
    candidate|stack vertically for now|version 7|1\.0\.0|no public checkbox,
    combo|no combo|no tooltip` (case-insensitive) — **no matches**.
  - `SameLine` remaining hits are only the accurate residual limitation
    ("no public raw `SameLine`") in 20-widgets (:774), 60-migration (:129,
    :162), 70-troubleshooting (:139) — the designed API fact, not a stale
    claim.
- Structure read-backs: 20-widgets Combo (:166-204), Tooltip (:206-230),
  Rows (:530-590), gaps note (:773-778), conventions (:13-17);
  60-migration Layout section (:115-135) — headings, code fences, and table
  rows render correctly; anchor `#layout-rows-imguiexrow` used consistently
  (GitHub slug of `## Layout: Rows (ImGuiEx.Row)`).
- `git status --short docs/` — 4 files modified, nothing else (see handoff).

- Status: **COMPLETE** — Scope B docs done; G6 verdict recorded below

## Scope C — Demo (M4)
- Status: **COMPLETE** — Release build green (0 warnings / 0 errors), demo
  version pair at 1.3.0 (csproj `<Version>`, `.version` template, built
  assembly 1.3.0.0, KSPBT-regenerated staged copy 1.3.0.0)
- Disagreements / rulings: Phase 0 list below (D-C1–D-C4). Auto mode (no
  interactive ruling available) — resolved per the recorded proposals,
  following the Scope A D1–D8 precedent; flagged here for parent audit.
- Gate verdicts updated: G7 in GATES.md

### Phase 0 disagreement list (evidence-verified against working tree 2026-09-12)

**D-C1 — ◀/▶ glyphs are not renderable with the shipped font (proposed MODIFY,
implemented).** Work item 1a asks for "a ◀ label ▶ selector-style row".
Reality: the only bundled UI font is
`GameData/DearImGuiKSP/Fonts/IBMPlexSans-Regular.ttf` (`FontResolver.cs:32`
loads that single file; no fallback chain), and a WPF
`GlyphTypeface.CharacterToGlyphMap` probe on the font file returned
U+25C0 (◀) = False, U+25B6 (▶) = False — those buttons would render as
missing glyphs in-game. U+2190 (←) / U+2192 (→) = True. Resolution:
selector buttons use "←"/"→" labels — the same selector idiom, with
glyphs the font actually has.

**D-C2 — G2 wording vs. work item 1e (interpretation flagged).** GATES.md G2
in-game text: "the demo 4×4 button grid renders as 4 rows inside an
`autoResize: true` window and inside a `ScrollRegion`". Work item 1e
interprets these as two separate composition checks (grid in the
autoResize main window; ≥1 Row inside a ScrollRegion). Resolution:
implemented per 1e — grid in the main autoResize window (DemoConsumer.cs:478),
two Rows inside a ScrollRegion (DemoConsumer.cs:509-525). If the auditor
reads G2 literally (the grid itself nested in a ScrollRegion), it is a
3-line wrap around the grid loop.

**D-C3 — Section placement (documented choice).** The contract "Files to
Modify" table lists only `DearImGuiKSPDemo/DemoConsumer.cs`; the showcase
is therefore inline there as a private `DrawLayoutShowcase()` called from
`OnFrame` (DemoConsumer.cs:328), not a new ThemeDemo-style class file.
Matches Minimal Change + the contract's file list; the class-doc comment
summarizes the section.

**D-C4 — `Button` has no size overload (fact).** `DearImGuiKSP.cs:261` is
`Button(string label)` only. Resolution: uniform grid cells via
zero-padded two-character visible labels "01".."16" (constant width) with
`##camslotNN` unique ids (`BuildCellLabels`), matching the contract's
"4×4 tinted button grid (`##` ids)".

### Changes
- `DearImGuiKSPDemo/DemoConsumer.cs` (+269 lines, insertions only):
  - Constants + state fields for the showcase (~line 87-150): 4×4 grid
    geometry, slot states (empty/assigned/live), 64 camera items,
    out-of-range seed (`OrphanedIndexSeed = 71`), palette-based tint
    constants via a `WithAlpha` helper, `CameraItems`/`CellLabels` built
    once.
  - `DrawLayoutShowcase()` (DemoConsumer.cs:438) — called inside the
    autoResize main window after `_themeDemo.DrawImGui()` (:328):
    - Selector row `ImGuiEx.Row()` with ←/→ buttons cycling 64 generated
      camera names + multi-line tooltip on the "next" button (:447-459).
    - Label+control pair row (label + `SliderFloat("##pairrowslider")`)
      with the slider tooltip (:463-469).
    - 4×4 camera-slot grid built with `Row(GridRowSpacing)` in loops
      (:472-486); per-state tint via stacked `ImGuiEx.StyleColor` scopes
      over Button/ButtonHovered/ButtonActive (green = assigned, orange =
      live, empty = theme default) (:532-564); click cycles
      empty→assigned→live→empty with a single live slot (CinematicRecorder
      mirror); cell 0 carries the multi-line grid tooltip (:595).
    - `Combo("Camera combo", …)` over the 64 generated items with the
      selection echoed as text (:489-493).
    - `Combo("Recovered combo", …)` whose index is seeded beyond the list
      (preview "(none)", no throw) + "Re-orphan the index (test)" button
      re-seeding it, with a multi-line tooltip (:496-504).
    - Two `ImGuiEx.Row()` blocks inside an `ImGuiEx.ScrollRegion`
      composition block (:509-525).
  - Class-doc comment updated to describe the new section.
- `DearImGuiKSPDemo/DearImGuiKSPDemo.csproj` — `<Version>` 1.2.1 → 1.3.0.
- `DearImGuiKSPDemo/DearImGuiKSPDemo.version` — `"VERSION"` 1.2.1 → 1.3.0.
- NOT touched per constraints: `Properties/AssemblyInfo.cs`
  (`KSPAssemblyDependencyEqualMajor("DearImGuiKSP", 1, 0)` — minor bump,
  ISSUES #016 does not apply), library source, docs, tests, CHANGELOG,
  native tree, library version files.

### Verification record (Scope C)
- `dotnet build DearImGui-KSP.slnx -c Release` — **Build succeeded. 0
  Warning(s), 0 Error(s)** (DearImGuiKSPDemo.dll built against the new
  library API: `ImGuiEx.Row()`/`Row(float)`/`RowScope`,
  `DearImGuiKSP.Combo(string, ref int, IReadOnlyList<string>)`,
  `DearImGuiKSP.Tooltip(string)`).
- Version pair: csproj `<Version>1.3.0</Version>` (:10), template
  `"VERSION": "1.3.0"` (:5), built assembly version **1.3.0.0**,
  KSPBuildTools-regenerated staged copy `GameData/DearImGuiKSPDemo/
  DearImGuiKSPDemo.version` `"VERSION": "1.3.0.0"` — lockstep ✓.
- `git status --porcelain` — demo scope touched exactly
  `DemoConsumer.cs`, `DearImGuiKSPDemo.csproj`, `DearImGuiKSPDemo.version`
  (library/docs/tests entries in the same tree are Scope A/B work).
- In-game verification (G2/G3/G4 in-game halves, G9) — user-driven, pending.

## Parent — Release prep + in-game acceptance (M5–M6)
- Status: M5 done; M6 done (user-verified); session verdict CONTINUE recorded
- Disagreement rulings: Scope A D1–D8 ACCEPT; Scope B D-B1..D-B4 + D-B6 ACCEPT, D-B5 ACCEPT-with-fix (Row/RowScope row added to `docs/10-api-fundamentals.md:92` scope table by parent); Scope C D-C1 ACCEPT (◀/▶ missing from IBM Plex Sans → ←/→, glyph-probed), D-C2 ACCEPT (grid-in-autoResize + rows-in-ScrollRegion satisfies G2 intent), D-C3/D-C4 noted
- M5 changes: library csproj `<Version>` 1.2.0→1.3.0; `DearImGuiKSP.version` "VERSION" 1.2.0→1.3.0; CHANGELOG "1.3.0 — minor" + "Demo 1.3.0" sections; `package_release.bat` run → `dist\DearImGuiKSP-1.3.0.zip`, `dist\DearImGuiKSPDemo-1.3.0.zip`, `dist\symbols\DearImGuiKSPNative-1.3.0.pdb` (log verified; library zip entries verified; native sources untouched per git status)
- M6 in-game acceptance (user, 2026-09-12): ALL PASS — layout showcase, ScrollRegion rows, combos, tooltips, no regression on existing widgets, GL smoke. The "disabled combo has no effect" report was resolved as an expectation mismatch after a walkthrough of intended behavior (the out-of-range "Recovered combo" is interactive by design — selection repairs the index; user confirmed exact match). It exposed a demo-coverage gap: no genuinely EMPTY (disabled) combo existed. Added "Empty combo (disabled)" + "Recovered index:" echo to the demo, rebuilt (0 errors), re-packaged; quick in-game look pending user's next launch
- Post-audit addendum (user-approved, 2026-09-12): `GradientButton` made row-participating — hook in the explicit-color overload only (`ImGuiGradients.cs:126`); themed overload inherits via delegation (double-count avoided, in-code comment); `docs/20-widgets.md` participant list updated; 229/229 tests green; zips re-packaged (not a published-zip replacement — 1.3.0 unreleased)
- Remaining: GitHub release upload (user action / confirmed `gh` call — never silent); session closure after the empty-combo quick look
