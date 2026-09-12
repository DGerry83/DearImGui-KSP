# Planning Worksheet: Consumer API Additions — Row / Combo / Tooltip (1.3.0)
## Date: 2026-09-12
## Type: Feature
## Related Investigation: [INVESTIGATION_LOG.md](INVESTIGATION_LOG.md)

### Step 1 — Core Entities and State
| Entity | Identity | Attributes | Relationships | Lifetime |
|--------|----------|------------|---------------|----------|
| `RowState` (new, `Application/RowState.cs`) | none (per-frame static, mirrors OpenScopeTracker pattern) | `Depth: int` (open row scopes this frame); `ItemsInCurrentRow: int`; `CurrentSpacing: float` (−1 = style ItemSpacing.x) | Pushed/popped by `ImGuiEx.Row()` / `RowScope.Dispose`; consulted by the widget hook; reset by frame loop + fault unwind | transient (per-frame) |
| Combo preview (value object, no type) | n/a | `previewText: string` — `items[i]` in range, else `"(none)"` constant | Produced by pure helper `ResolveComboPreview`; consumed by `igBeginCombo` preview arg | transient (per call) |
| Tooltip text | n/a | `text: string` (null/empty = no-op) | Passed through existing `ToUtf8` → `igTextUnformatted` path | transient (per call) |

No persistent state. No new config keys. No new entities in `Application/Interfaces/`.

### Step 2 — Behaviors and Responsibilities
| Component | Single-Sentence Responsibility | Command / Query / Both |
|-----------|-------------------------------|------------------------|
| `RowState` (static) | Tracks per-frame row nesting depth, item count, and spacing so the widget hook knows when to separate items | Both |
| `ImGuiEx.Row()` / `RowScope` | Pushes row state on creation and pops it exactly once on Dispose | Command |
| Widget hook (`DearImGuiKSP.RowItemHook`, internal) | Emits `igSameLine` before every item after the first inside an active row | Command |
| `DearImGuiKSP.Combo` (new partial) | Renders a dropdown bound to an index into a string list and reports selection changes | Both |
| `DearImGuiKSP.ResolveComboPreview` (internal pure) | Computes the combo preview text with empty/out-of-range tolerance | Query |
| `DearImGuiKSP.Tooltip` (new partial) | Shows a wrapped hover tooltip for the previously declared item | Command |
| `ImGuiInternal` additions | Wrap the new non-variadic cimgui calls with UTF-8 handling | Command |
| `ImGuiNative` additions | P/Invoke declarations for the new cimgui exports | Command |

### Step 3 — Data Flow
```
Row:
consumer callback --(ImGuiEx.Row(spacing?))--> RowState.Push --(per widget)--> RowItemHook
  --(items>0 ? igSameLine(0, spacing))--> ImGuiNative --> cimgui
RowScope.Dispose --(pop)--> RowState (frame-open reset / fault unwind = backstop)

Combo:
consumer callback --(label, ref index, IReadOnlyList<string>)--> DearImGuiKSP.Combo
  --(CanDeclareUi gate)--> ResolveComboPreview --(preview string)--> ImGuiInternal.BeginCombo
  --(per item: ToIdUtf8)--> igSelectable_Bool --(clicked)--> igCloseCurrentPopup + ref write
  --(try/finally)--> igEndCombo                          --> ImGuiNative --> cimgui

Tooltip:
consumer callback --(text)--> DearImGuiKSP.Tooltip
  --(CanDeclareUi gate; null/empty no-op)--> igIsItemHovered(ForTooltip flags)
  --(true)--> igBeginTooltip --> igPushTextWrapPos(fontSize*35) --> igTextUnformatted(utf8)
  --> igPopTextWrapPos --> igEndTooltip          --> ImGuiNative --> cimgui
```
No splits needing an event bus; all flows are direct calls inside one consumer callback.

### Step 4 — Boundaries and Interfaces
| Interface | Defined In | Implemented By | Consumed By | Purpose |
|-----------|------------|----------------|-------------|---------|
| (none new) | — | — | — | New surface is facade statics + Interop wrappers, the established pattern; no cross-layer abstraction is warranted (KISS/YAGNI) |

Existing seams reused: `DearImGuiKSP` facade (public, Application) → `ImGuiInternal` (Interop) → `ImGuiNative` (Interop, raw P/Invoke). `RowState` lives in `Application/` beside `OpenScopeTracker` and follows its statics-with-reset convention.

### Layering Check
- [x] Core imports nothing from Application or Infrastructure. (No Core changes.)
- [x] Application imports from Core only. (New partials use `DearImGuiKSP.Interop` only — same as existing widget partials; no KSP/Unity APIs in signatures.)
- [x] Infrastructure imports from Core and Application. (No Infrastructure changes required.)
- [x] No KSP or engine-lifecycle APIs in public signatures; no `UnityEngine` types needed by any of the three APIs (D24 not exercised).
