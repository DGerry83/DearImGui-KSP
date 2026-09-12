namespace DearImGuiKSP.Application
{
    /// <summary>
    /// Per-frame state of the facade's row scope (FR-1, 1.3.0). While a row is
    /// open, the widget hook (<c>DearImGuiKSP.RowItemHook</c>) separates items
    /// with <c>igSameLine</c>; the first item is never preceded by SameLine.
    /// Managed-only state: an undisposed RowScope can leave wrong layout for a
    /// frame, never ImGui stack corruption. Follows the OpenScopeTracker
    /// convention: plain internal statics, only the frame-loop thread touches
    /// them, reset when a frame opens and on fault unwind. Rows do not nest —
    /// a push while a row is already open is rejected (the nested scope is
    /// inert), so Depth is 0 or 1.
    /// </summary>
    internal static class RowState
    {
        // Passed to igSameLine(0, spacing): imgui.cpp SameLine maps spacing < 0
        // to style.ItemSpacing.x, so the default row spacing follows the UI
        // scale through the style vars (FR-1: no unscaled pixel constant).
        internal const float StyleDefaultSpacing = -1f;

        /// <summary>Open row scopes this frame (0 or 1 — nested pushes are rejected).</summary>
        internal static int Depth;

        /// <summary>Widgets declared inside the current row since it opened.</summary>
        internal static int ItemsInCurrentRow;

        /// <summary>The current row's spacing argument; <see cref="StyleDefaultSpacing"/> = style ItemSpacing.x.</summary>
        internal static float CurrentSpacing;

        /// <summary>
        /// Opens a row when none is open, recording the spacing and zeroing the
        /// item counter. Returns false — changing nothing — when a row is
        /// already open (nested rows are inert by contract) so a nested
        /// RowScope stays a no-op on Dispose.
        /// </summary>
        internal static bool TryPush(float spacing)
        {
            if (Depth != 0)
            {
                return false;
            }
            Depth = 1;
            ItemsInCurrentRow = 0;
            CurrentSpacing = spacing;
            return true;
        }

        /// <summary>Closes the current row (idempotent; safe on every path).</summary>
        internal static void Pop()
        {
            Depth = 0;
            ItemsInCurrentRow = 0;
            CurrentSpacing = StyleDefaultSpacing;
        }

        /// <summary>Zeroes all state; called by the frame loop when a frame opens.</summary>
        internal static void Reset()
        {
            Pop();
        }

        /// <summary>
        /// Row hook decision for one declared widget: true when a SameLine with
        /// <see cref="CurrentSpacing"/> must precede it (every item after the
        /// first inside an open row). Outside a row it is always false and the
        /// counter is untouched. Zero allocation (hot-path checklist Check 2).
        /// </summary>
        internal static bool OnRowItem()
        {
            if (Depth == 0)
            {
                return false;
            }
            bool separate = ItemsInCurrentRow > 0;
            ItemsInCurrentRow++;
            return separate;
        }
    }
}
