# Knowledge: Composing variadic cimgui APIs from non-variadic exports

**Origin session:** `notes\finished\2026-09-12_Feature_RowComboTooltipApi\` (1.3.0).

The managed binding layer forbids variadic P/Invoke (`ImGuiNative.cs`: "No
variadic functions — P/Invoke cannot call varargs"), but several useful cimgui
APIs only exist in variadic form (`igText`, `igSetTooltip`, `igSetItemTooltip`,
`igLogText`, ...). Text already routes through `igTextUnformatted`. For
anything else, compose from the non-variadic primitives instead of trying to
bind the variadic entry point.

## The tooltip recipe (1.3.0, proven in-game)

`igSetItemTooltip(fmt, ...)` decomposes to:

```
if (igIsItemHovered(ImGuiHoveredFlags_ForTooltip))   // cimgui.h:474, 1 << 12
    if (igBeginTooltip())                            // EndTooltip only when true
        try {
            igPushTextWrapPos(igGetFontSize() * 35f) // stock demo wrap width
            try { igTextUnformatted(utf8, NULL) }
            finally { igPopTextWrapPos() }
        } finally { igEndTooltip() }
```

Verified facts (pinned cimgui `docking_inter` clone, imgui v1.92.9-docking-1):

- `ImGuiHoveredFlags_ForTooltip = 1 << 12` (cimgui.h:474). ImGui expands it to
  `style.HoverFlagsForTooltipMouse` (Stationary | DelayShort |
  AllowWhenDisabled) plus NoSharedDelay (imgui.h:1532-1533) — exactly what
  `SetItemTooltip` passes, so the composed version gets stock delay semantics.
- `BeginTooltipEx` pushes **no** wrap pos of its own (imgui.cpp) — without an
  explicit `PushTextWrapPos` a long tooltip renders on one line, and adding one
  is NOT a double-wrap.
- `igBeginTooltip` currently always returns true, but honor the return anyway
  (EndTooltip pairing rule, same shape as EndCombo).

## Combo recipe notes

- `igEndCombo` asserts unless `igBeginCombo` returned true — keep the
  begin-result flag and make the finally-block conditional.
- Disabled-but-visible chrome (e.g. an empty-list combo): wrap BeginCombo in
  `igPushItemFlag(ImGuiItemFlags_Disabled = 1 << 6, true)` / `igPopItemFlag` —
  ButtonBehavior early-outs Disabled items so the popup can never open, but the
  preview still renders. Note it does NOT dim like `BeginDisabled`.

## Delegating-overload hook rule (row layout)

When a public overload delegates to another public overload that declares the
actual ImGui item, a per-item hook (like 1.3.0's `RowItemHook`) belongs in the
item-declaring overload ONLY. Hooking both double-counts the single item (in
the row case: an unwanted SameLine pushes the widget off its own row).
