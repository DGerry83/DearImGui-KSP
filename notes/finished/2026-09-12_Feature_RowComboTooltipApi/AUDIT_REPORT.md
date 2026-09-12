# Audit Report: Consumer API Additions — Row / Combo / Tooltip (1.3.0)
## Date: 2026-09-12
## Type: Feature

### Gate Verdicts
| Gate ID | Criterion | Verdict | Evidence |
|---------|-----------|---------|----------|
| G1 | Release build + no existing public signature changes | PASS | Auditor re-ran: packaging log "Build succeeded. 0 Warning(s) 0 Error(s)" (Release, both DLLs + tests); `git diff --numstat` — all 12 modified library files show deletions = 0 (insertions-only); new public surface = 4 new files + `ImGuiEx` Row additions; full public-method inventory swept (see Similar Bugs Sweep) |
| G2 | Row API per contract | PASS (unit half; in-game half pending user) | `RowState.cs` (TryPush nested rejection, OnRowItem first-item rule, Reset), `ImGuiEx.Row()`/`Row(float)`/`RowScope` (factory gates on CanDeclareUi; nested/unavailable → inert; Debug.Assert debug-only), reset wiring verified at `FrameLoopOrchestrator.cs:132` and `UnwindOpenScopes` (DearImGuiKSP.cs:66-70); 18 RowStateTests green in auditor's own `dotnet test` run (229/229) |
| G3 | Combo API per contract | PASS (unit half; in-game half pending user) | `DearImGuiKSP.Combo.cs` reviewed line-by-line: `ResolveComboPreview` (null/empty/out-of-range → "(none)", never rewrites ref), `ApplyComboSelection` (bounds-guarded write), disabled-empty via `PushItemFlag(Disabled)` with conditional `PopItemFlag`, conditional `EndCombo` under try/finally (correct per imgui pairing rule), `CloseCurrentPopup` on selection; 12 ComboLogicTests green |
| G4 | Tooltip API per contract | PASS (unit half; in-game half pending user) | `DearImGuiKSP.Tooltip.cs` reviewed: guard order correct (CanDeclareUi → null/empty → hover → BeginTooltip), nested try/finally for EndTooltip + PopTextWrapPos, wrap = GetFontSize()*35 with pinned-imgui.cpp verification that BeginTooltipEx pushes no wrap pos (no double-wrap); **no variadic P/Invoke** — auditor confirmed every new DllImport is non-variadic and `igSetItemTooltip` is absent; enum pins re-verified by auditor against pinned cimgui.h: `ImGuiItemFlags_Disabled = 1<<6` (cimgui.h:328), `ImGuiHoveredFlags_ForTooltip = 1<<12` (cimgui.h:474), expansion comment matches imgui.h:1532-1533; 8 TooltipGuardTests green |
| G5 | Full xUnit suite | PASS | Auditor's own run: "Passed! - Failed: 0, Passed: 229, Skipped: 0, Total: 229" (191 pre-existing + 38 new) |
| G6 | Docs | PASS | Auditor re-ran greps over `docs/`: no "No public SameLine", no "vertical-first", no "post-release candidate", no "stack vertically for now", no "expected version 7", no "current library version is **1.0.0**"; `70-troubleshooting.md:26,31` both say version 9 (verified vs `NativeBridge.cs:44`); `00-getting-started.md:87` shows `(1, 3)`; parent-added `Row`/`RowScope` row in `10-api-fundamentals.md` scope table (D-B5 ruling) |
| G7 | Demo + demo version pair | PASS | Auditor reviewed `DrawLayoutShowcase` (DemoConsumer.cs:438-608): selector row, label+control row, 4×4 `Row(GridRowSpacing)` grid with `##camslotNN` ids + per-state StyleColor tints, 64-item combo, out-of-range combo + re-orphan button, tooltips on button/slider/grid-cell (multi-line), rows inside ScrollRegion; demo pair 1.3.0 lockstep (csproj + template + staged regenerated file "1.3.0.0"); dependency attribute untouched |
| G8 | Release prep | PASS | Both zips + `dist\symbols\DearImGuiKSPNative-1.3.0.pdb` exist; library zip entries verified (managed+native DLLs, all docs, CHANGELOG; no settings.cfg, no PDB); library csproj/template read 1.3.0, staged `GameData\DearImGuiKSP\DearImGuiKSP.version` = "1.3.0.0"; `git status --porcelain` on `DearImGuiKSPNative/{src,include,vendor,*.bat}` empty — native sources untouched, handshake stays v9 |
| G9 | In-game acceptance | PENDING (user-driven) | Build mirrored into the pinned test instance by the Release build; awaiting user run |
| G10 | Native Interop & Hot-Path Checklist | PASS | Per-chunk verdicts recorded in PROGRESS_LOG.md; auditor confirmed: Check 1 N/A (no process-global mutation — all new calls are ImGui-context-local); Check 2 verdicts match code (RowItemHook = 2 compares, zero alloc; combo iterates only while popup open; tooltip encodes only while hovered); Check 3 verified in code (conditional EndCombo/EndTooltip/PopTextWrapPos/PopItemFlag under try/finally) |

### Session Verdict
- **Verdict**: CONTINUE
- **Reason**: No gate is FAIL. G1–G8 and G10 are PASS. G9 and the in-game halves of G2/G3/G4 are user-owned verification steps, pending by design (same pattern as previous sessions' user-verified acceptance). No FAILs require follow-up acceptance.

### Frozen Gates Integrity
- [x] GATES.md exists and was frozen before implementation (2026-09-12T20:49:06Z, pre-Phase-2)
- [x] GATES.md criteria and freeze header unchanged — auditor re-read the file; only Verdict/Evidence cells were filled (by gate owners, per protocol). `notes/` is untracked in git so timestamp verification is by content inspection
- [x] No post-freeze criterion modification detected

### Invariant Check Results
- [x] Public interfaces preserved (check: `git diff --numstat` — insertions-only in all 12 modified library files; public-method inventory shows only additive `Row`/`Combo`/`Tooltip`)
- [x] Shared state shape stable (check: `RowState` is new; `OpenScopeTracker`/facade statics untouched)
- [x] Language/runtime compliance (check: Release build 0 warnings/0 errors on net48; no post-4.8 BCL APIs observed in new code)
- [x] Public utility function signatures stable (check: no modifications to any existing method body except additive `RowItemHook()` line after guards + the two reset-wiring lines)
- [x] Minimal change principle followed (check: 897 insertions / 26 deletions across 24 files vs contract estimate ~1,100 lines; deletions are version strings, doc replacements, and one demo comment — all in-scope)
- [x] Layered dependency direction preserved (check: new Application files import only `DearImGuiKSP.Interop`/`System.*`; no KSP/Unity APIs in new signatures; no Infrastructure changes)

### Principles & Anti-Patterns Check
- [x] Single Responsibility Principle: `RowState` (row state tracking), `DearImGuiKSP.Row.cs` (hook), `DearImGuiKSP.Combo.cs` (combo widget), `DearImGuiKSP.Tooltip.cs` (tooltip widget) — each describable in one sentence
- [x] Separation of Concerns: bindings in Interop, surface in Application, demo in Demo — no cross-layer bleed
- [x] Dependency Inversion: no new cross-boundary abstractions needed (facade statics pattern; KISS/YAGNI — contract Step 4 records this explicitly)
- [x] No global mutable state introduced — `RowState` follows the documented `OpenScopeTracker` per-frame-statics convention (single-threaded frame loop, reset at frame open + fault unwind)
- [x] No God Classes, Spaghetti Code, Golden Hammer, or Leaky Abstractions observed
- [x] Magic numbers/strings replaced with named constants (`StyleDefaultSpacing = -1f`, `ComboEmptyPreview`, `TooltipWrapWidthFactor = 35f`, enum pins)

### Similar Bugs Sweep
- **Pattern Searched**: "standard item-declaring facade widget missing the `RowItemHook()` insertion" (a copy-paste-prone 20-site mechanical change) — plus the inverse, hooks in non-item methods
- **Files Checked**: full public-method inventory of `DearImGuiKSP/Application/DearImGuiKSP.cs` + all `Api/*.cs` facade partials (grep for `public static` methods vs 20 `RowItemHook()` call sites)
- **Findings**: All 20 standard-widget overloads hooked (Text, TextColored, Button, RadioButton ×2, Toggle ×2, SliderFloat, InputText, CollapsingHeader, Spinner, Knob ×4, Wheel ×4, Dummy ×2, Combo). Correctly NOT hooked: scopes/Begin*/End*/Push/Pop, `GetScrollY`/`SetCursorY` (non-item), `ImGuiDraw.*` (cursor-canvas primitives), docking/plot helpers. No misplaced hooks.
- **Post-audit addendum (2026-09-12, user-approved)**: `GradientButton` was the one item-declaring widget outside the sweep's hooked set (contract scope, not a defect). At the user's request it was added: hook inserted after the `CanDeclareUi` gate in the explicit-color overload (`ImGuiGradients.cs:126`) only — the themed overload delegates to it, so hooking both would double-count the single item (guarded by an in-code comment). `docs/20-widgets.md` participant list updated. Rebuilt, 229/229 tests green, zips re-packaged (1.3.0 was never published, so rebuilding the local zips is not a zip replacement).

### Violations Found
- None. Observations (non-blocking):
  1. ~~`GradientButton` not row-participating~~ — **resolved post-audit** (user-approved addendum, see Similar Bugs Sweep): the explicit-color overload now hooks; the themed overload inherits via delegation.
  2. The empty-list combo renders non-interactive chrome but does not dim like `BeginDisabled` (contract-chosen `PushItemFlag(Disabled)` path) — cosmetic; check during G9.
  3. The nested-row `Debug.Assert` is intentionally untested on the release runner (nested rejection is covered via `RowState.TryPush` directly).

### Recommendations
- Clear to proceed. Remaining work is user-owned: G9/M6 in-game acceptance (demo showcase on the pinned D3D11 instance + GL smoke), then the GitHub release upload (user action or explicitly confirmed `gh` call — per release protocol, never replace a published zip; both 1.3.0 zips are new files, safe to attach).
