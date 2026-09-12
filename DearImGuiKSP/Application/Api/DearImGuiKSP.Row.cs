using DearImGuiKSP.Application;
using DearImGuiKSP.Interop;

namespace DearImGuiKSP
{
    public static partial class DearImGuiKSP
    {
        // FR-1 row layout hook (1.3.0): every standard facade widget calls this
        // right after its CanDeclareUi gate. Inside an open ImGuiEx.Row scope,
        // each widget after the first is preceded by igSameLine(0, spacing) so
        // items share one line; the first item keeps the vertical cursor, so
        // row width participates in autoResize fit-to-content like any item.
        // Outside a row this is two compares (hot-path checklist Check 2:
        // zero allocation). Pure managed decision state — a faulting consumer
        // can leave RowState unbalanced, but the per-frame reset and the fault
        // unwind backstop clear it (worst case: wrong layout, never a native
        // stack imbalance).
        internal static void RowItemHook()
        {
            if (RowState.OnRowItem())
            {
                ImGuiInternal.SameLine(RowState.CurrentSpacing);
            }
        }
    }
}
