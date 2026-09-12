using System.Collections.Generic;
using DearImGuiKSP.Interop;

namespace DearImGuiKSP
{
    public static partial class DearImGuiKSP
    {
        // Preview shown when the list is empty or the index is out of range.
        // Internal so the clamping rules are unit-testable without a native frame.
        internal const string ComboEmptyPreview = "(none)";

        /// <summary>
        /// Draws a combo (dropdown) bound to an index into a list of items
        /// (FR-2, 1.3.0) — the curated wrapper over cimgui BeginCombo /
        /// Selectable / EndCombo. The closed combo shows
        /// <c>items[selectedIndex]</c> as its preview; opening the popup lists
        /// all items with the selected one highlighted, and clicking an item
        /// updates <paramref name="selectedIndex"/> in place and closes the
        /// popup. Only valid inside a registered callback.
        /// </summary>
        /// <param name="label">
        /// Combo label, drawn beside the closed value; also the combo's ImGui
        /// identity. Standard <c>##</c> suffix rules apply — two combos with
        /// the same visible label in one window need distinct suffixes.
        /// </param>
        /// <param name="selectedIndex">
        /// Index into <paramref name="items"/> of the current selection. An
        /// out-of-range value (e.g. a persisted index whose list shrank) is
        /// tolerated: the preview shows <c>"(none)"</c>, no exception is
        /// thrown, and the reference is left untouched until the user makes a
        /// real selection.
        /// </param>
        /// <param name="items">
        /// The selectable entries. Null or empty renders a disabled combo
        /// showing <c>"(none)"</c> — its popup never opens and the call never
        /// throws. Long lists scroll inside the popup per stock ImGui. A null
        /// entry renders as an empty row.
        /// </param>
        /// <returns>
        /// True on the frame the user selects an item (the reference has been
        /// updated); false otherwise, including when unavailable. The selected
        /// row is highlighted while the popup is open and the popup closes on
        /// click (stock combo behavior).
        /// </returns>
        public static bool Combo(string label, ref int selectedIndex, IReadOnlyList<string> items)
        {
            if (!CanDeclareUi)
            {
                return false;
            }
            RowItemHook();
            string preview = ResolveComboPreview(items, selectedIndex);
            bool disabled = items == null || items.Count == 0;
            if (disabled)
            {
                // Empty list: render the combo chrome but make the item
                // non-interactive so the popup can never open (imgui.cpp
                // ButtonBehavior early-outs a Disabled item).
                ImGuiInternal.PushItemFlag(ImGuiItemFlags.Disabled, true);
            }
            bool open = ImGuiInternal.BeginCombo(label, preview);
            try
            {
                if (open)
                {
                    for (int i = 0; i < items.Count; i++)
                    {
                        if (ImGuiInternal.Selectable(items[i], i == selectedIndex) &&
                            ApplyComboSelection(items.Count, i, ref selectedIndex))
                        {
                            ImGuiInternal.CloseCurrentPopup();
                            return true;
                        }
                    }
                }
                return false;
            }
            finally
            {
                // EndCombo only when Begin succeeded (imgui.cpp EndCombo
                // asserts otherwise); both pairs release on every exit path,
                // including an encoding throw mid-loop.
                if (open)
                {
                    ImGuiInternal.EndCombo();
                }
                if (disabled)
                {
                    ImGuiInternal.PopItemFlag();
                }
            }
        }

        // FR-2 preview resolution (internal seam so the clamping rules are
        // unit-testable on the net48 runner, which cannot satisfy the P/Invoke
        // past the guard): in range -> the item text (a null entry renders as
        // an empty preview); null list, empty list, or any out-of-range index
        // -> "(none)". Never rewrites the caller's index — a real selection
        // does that via ApplyComboSelection.
        internal static string ResolveComboPreview(IReadOnlyList<string> items, int selectedIndex)
        {
            if (items == null || selectedIndex < 0 || selectedIndex >= items.Count)
            {
                return ComboEmptyPreview;
            }
            return items[selectedIndex] ?? string.Empty;
        }

        // FR-2 selection mapping (internal seam, same rationale): a click on a
        // valid row writes the index and reports the change; an out-of-range
        // click (not reachable from the facade loop) changes nothing.
        internal static bool ApplyComboSelection(int itemCount, int clickedIndex, ref int selectedIndex)
        {
            if (clickedIndex < 0 || clickedIndex >= itemCount)
            {
                return false;
            }
            selectedIndex = clickedIndex;
            return true;
        }
    }
}
