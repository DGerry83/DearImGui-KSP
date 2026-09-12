# Architecture Contract: Consumer API Additions — Row / Combo / Tooltip (1.3.0)
## Date: 2026-09-12
## Type: Feature
## Related Investigation: [INVESTIGATION_LOG.md](INVESTIGATION_LOG.md)
## Source Request: `notes\plans\FEATURE-REQUEST-CinematicRecorder-widgets-1.3.0.md`

### Change Specifics
- **Feature Scope**: Three curated public API additions in library minor **1.3.0**
  (non-breaking; no existing public signature changes):
  1. **FR-1 Row** — `ImGuiEx.Row()` / `ImGuiEx.Row(float spacing)` returning a
     `RowScope` readonly struct. Widgets declared inside the scope are separated
     by `igSameLine`; the first item is not preceded by SameLine. Scope-only —
     **no public raw `SameLine()`** (prevents the leaked-SameLine bug class the
     FR warns about; keeps the curated idiom). Default spacing = style
     `ItemSpacing.x` (`igSameLine(0, -1)` — style vars already follow UI scale).
     Explicit `Row(float spacing)` is pixels, NOT scaled (documented, mirrors
     knob/plot convention). Nested `Row()` inside an active row: inert no-op
     scope in release; `Debug.Assert` in Debug builds. An undisposed RowScope is
     pure managed state — cleared by the per-frame reset and fault unwind;
     worst case is wrong layout, never ImGui stack corruption.
  2. **FR-2 Combo** — `public static bool Combo(string label, ref int selectedIndex,
     IReadOnlyList<string> items)` in a new `Application/Api/DearImGuiKSP.Combo.cs`
     partial. Returns true on selection change and updates `selectedIndex` in
     place (matches the `RadioButton(ref int)` precedent; the FR's `out newIndex`
     is dropped as redundant). Empty/null items → disabled combo showing
     `"(none)"` (`igPushItemFlag(ImGuiItemFlags_Disabled)`), popup never opens,
     never throws. `selectedIndex` out of range → preview `"(none)"`, no
     exception, no silent rewrite of the ref (a real selection writes a valid
     index). Selected item highlighted; `igCloseCurrentPopup` on click. Long
     lists scroll per stock combo behavior (no `maxHeightItems` param — YAGNI).
  3. **FR-3 Tooltip** — `public static void Tooltip(string text)` in a new
     `Application/Api/DearImGuiKSP.Tooltip.cs` partial. Hover tooltip on the
     previously declared item. Null/empty text → no-op. No preceding item →
     no-op (IsItemHovered reads zeroed LastItemData, returns false).
     **Variadic `igSetItemTooltip` is NOT bound** (repo convention,
     `ImGuiNative.cs:112`); composed from non-variadic exports:
     `igIsItemHovered(ImGuiHoveredFlags_ForTooltip)` → `igBeginTooltip` →
     `igPushTextWrapPos(wrap)` → `igTextUnformatted` → `igPopTextWrapPos` →
     `igEndTooltip`. Hover flags = the `ImGuiHoveredFlags_ForTooltip` composite
     (stock `SetItemTooltip` semantics: stationary-or-delay, allow-when-disabled,
     no-shared-delay) — pin the exact value from the pinned cimgui clone's
     `cimgui.h`. Wrap width = `igGetFontSize() * 35` (stock value) — implementer
     verifies against `imgui.cpp` `BeginTooltipEx` in the pinned clone whether
     BeginTooltip already wraps; do not double-wrap. No TooltipFlags passthrough
     in v1 (FR: not required).
- **Success criteria**: FR acceptance — docs updated, demo examples added (4×4
  button grid, 50+ item combo, tooltips), xUnit coverage for row state + combo
  clamping, all three verified in-game via the demo, release protocol executed
  for 1.3.0.

### Structural Invariants
- Public facade pattern preserved: `CanDeclareUi` gate first in every widget;
  unavailable/outside-callback = safe no-op, never an exception (C01).
- Begin/End pairing under exceptions: combo uses try/finally around
  `igBeginCombo`/`igEndCombo`; tooltip begin/wrap/end likewise. Row has NO
  native pairing (managed state only).
- `RowState` follows the `OpenScopeTracker` convention: internal statics, reset
  at frame open (`FrameLoopOrchestrator.cs:131` site) and on fault unwind
  (`DearImGuiKSP.UnwindOpenScopes`), single-threaded (frame loop only).
- Interop conventions preserved: no variadic P/Invoke; strings via existing
  `ToUtf8`/`ToIdUtf8` byte[] path; `bool` returns `UnmanagedType.I1`; cimgui
  header line citations in comments.
- Empty-label/ID safety: combo label and items route through `ToIdUtf8`
  (sentinel-ID convention, G3-20). Docs show `##` suffixing for duplicate
  labels/items.
- Non-breaking minor: existing public signatures byte-identical; handshake
  stays v9 (no native changes — all new calls are exports already compiled into
  the shipped DLL from the pinned cimgui `docking_inter` clone).
- Fault-tolerance contract (FR cross-cutting 3): consumer misuse no-ops or
  throws inside the callback so FaultBarrier isolation handles it; nothing can
  unbalance ImGui's stacks for later consumers.

### Native Interop & Hot-Path Checklist (CORE_PROTOCOLS §5.9 — applies: new P/Invoke on per-frame path)
- **Check 1 — Process-global state**: N/A. All new calls operate on the ImGui
  context only; no DLL search path, env, or OS handle mutation.
- **Check 2 — Hot-path allocation**: Combo iterates items only while its popup
  is open (user-interaction-bound); per-item `ToIdUtf8` allocation matches the
  established per-widget per-frame precedent (every existing widget encodes its
  label each frame). Row hook: zero allocation (float compare + counter).
  Tooltip: encodes only while hovered. Justification recorded; no buffers added.
- **Check 3 — Resource-acquisition symmetry**: `igEndCombo` released via
  try/finally on every path incl. encoding throws; tooltip `End/Pop` pairs
  likewise; `igPushItemFlag`/`igPopItemFlag` paired around the disabled combo.

### Files to Modify
| File | Change Type | Invariants Applied | Risk Level | Lines Affected (Est.) |
|------|-------------|-------------------|------------|---------------------|
| `DearImGuiKSP/Interop/ImGuiNative.cs` | Add DllImports + wrappers: `igBeginCombo`, `igEndCombo`, `igSelectable_Bool`, `igCloseCurrentPopup`, `igPushItemFlag`, `igPopItemFlag`, `igIsItemHovered`, `igBeginTooltip`, `igEndTooltip`, `igPushTextWrapPos`, `igPopTextWrapPos`, `igGetFontSize` | Non-variadic only; I1 bools; cimgui.h citations | Low | ~120 |
| `DearImGuiKSP/Interop/ImGuiInternal.cs` | Add UTF-8 wrappers for the above + `SameLine(float spacing)` overload | ToUtf8/ToIdUtf8 path; EmptyIdSentinel | Low | ~90 |
| `DearImGuiKSP/Application/RowState.cs` | **New** — per-frame row depth/item/spacing state + Reset | OpenScopeTracker convention | Low | ~70 |
| `DearImGuiKSP/Application/Api/ImGuiEx.cs` | Add `Row()`/`Row(float)` factories + `RowScope` readonly struct | Scope idiom; inert-on-nested; Dispose always safe | Med (public API, permanent) | ~90 |
| `DearImGuiKSP/Application/DearImGuiKSP.cs` + widget partials (`Api/DearImGuiKSP.{Radio,Toggle,Knob,Wheel,Header,Spinner,TextColored}.cs`) | Insert one `RowItemHook()` line after the `CanDeclareUi` gate in each standard-widget method (Text, TextColored, Button, RadioButton×2, Toggle, SliderFloat, InputText, Header, Spinner, Knob, Wheel, Dummy, Combo) | Gate-first order preserved | Med (many small touch points) | ~15 files × 1–3 lines |
| `DearImGuiKSP/Application/Api/DearImGuiKSP.Row.cs` | **New** — `RowItemHook()` + Row push/pop facade internals | Managed-only state | Low | ~50 |
| `DearImGuiKSP/Application/Api/DearImGuiKSP.Combo.cs` | **New** — `Combo` + `ResolveComboPreview` | try/finally EndCombo; disabled-empty path | Med (public API) | ~110 |
| `DearImGuiKSP/Application/Api/DearImGuiKSP.Tooltip.cs` | **New** — `Tooltip` | Non-variadic compose; try/finally | Med (public API) | ~70 |
| `DearImGuiKSP/Application/DearImGuiKSP.cs` (UnwindOpenScopes) / `Application/FrameLoopOrchestrator.cs:131` | Add `RowState.Reset()` to frame-open reset + fault unwind | Backstop symmetry | Low | ~4 |
| `DearImGuiKSP/Interop/ImGuiStyleEnums.cs` (or sibling enum file — implementer confirms home) | Add internal `ImGuiHoveredFlags` incl. `ForTooltip` composite, pinned to cimgui.h | Enum-pin test precedent | Low | ~40 |
| `tests/Application.Tests/RowStateTests.cs` | **New** — push/pop, first-vs-subsequent separation, nested inert, reset | Internal-seam test style | Low | ~120 |
| `tests/Application.Tests/ComboLogicTests.cs` | **New** — preview resolution: null/empty/−1/count/count+N → `"(none)"`; valid → item; selection mapping | Internal-seam test style | Low | ~100 |
| `tests/Application.Tests/WidgetGuardTests.cs` (or new `TooltipGuardTests.cs`) | Add tooltip no-op guard cases; hovered-flags pin test | Existing collection conventions | Low | ~60 |
| `DearImGuiKSPDemo/DemoConsumer.cs` | Extend showcase: row examples, 4×4 tinted button grid (`##` ids), 50+ item combo, tooltips | Demo-only | Low | ~120 |
| `docs/20-widgets.md` | New Layout (Row) + Combo + Tooltip sections; replace absence notes at :644-651 | Doc accuracy | Low | ~120 |
| `docs/60-migration-from-imgui.md` | Mapping rows: BeginHorizontal→Row, dropdown→Combo, GUI.tooltip→Tooltip; remove "stack vertically" guidance | Doc accuracy | Low | ~40 |
| `docs/70-troubleshooting.md` | :31 handshake "version 7" → 9 | Doc-drift fix | Low | 1 |
| `docs/00-getting-started.md` | :90 "1.0.0" → "1.3.0" | Doc-drift fix | Low | 1 |
| `DearImGuiKSP/DearImGuiKSP.csproj` + `DearImGuiKSP/DearImGuiKSP.version` | `<Version>` + `"VERSION"` → 1.3.0 (lockstep) | Release protocol | Low | 2 |
| `DearImGuiKSPDemo/DearImGuiKSPDemo.csproj` + `DearImGuiKSPDemo/DearImGuiKSPDemo.version` | Demo pair 1.2.1 → 1.3.0 (demo changed) | Release protocol | Low | 2 |
| `CHANGELOG.md` | "1.3.0" + "Demo 1.3.0" sections | Release protocol | Low | ~40 |

**Explicitly NOT changed**: `DearImGuiKSPNative/` (no rebuild — verified exports
already compiled in), handshake version, any existing public signature,
`GameData/` content other than what builds mirror, `DearImGuiKSPDemo`'s
`KSPAssemblyDependencyEqualMajor` (minor bump — ISSUES #016 does not apply).

### Migration Strategy
- None required — pure additive minor. Consumers opt in by declaring
  `KSPAssemblyDependencyEqualMajor("DearImGuiKSP", 1, 3)` (docs updated to say so).

### Sub-Agent Scopes
- **Scope A (one implementer, sequential M1→M3)**: all library code + unit tests
  (bindings, RowState, Row scope + hooks, Combo, Tooltip, tests). Single agent
  because `ImGuiNative.cs`/`ImGuiInternal.cs`/facade partials are shared files —
  parallel edits would collide. Verifiable output → default sub-agent model.
- **Scope B (docs)** and **Scope C (demo)**: parallel after Scope A lands (disjoint
  files; both need the final API signatures). Verifiable output → default model.
- **Parent agent (M5–M6)**: version bumps, CHANGELOG, `package_release.bat`, audit,
  in-game verification with the user. GitHub release upload is a user action
  (or explicitly confirmed `gh` call) — never a silent step.

## Native Interop checklist verdicts
Recorded above (all three checks answered). Chunk-level confirmations go in
PROGRESS_LOG.md during Phase 2.
