using DearImGuiKSP.Interop;

namespace DearImGuiKSP
{
    public static partial class DearImGuiKSP
    {
        /// <summary>
        /// Draws a collapsible section header inside the current window — the
        /// group-level equivalent of minimizing a whole window. Only valid
        /// inside a registered callback.
        /// </summary>
        /// <param name="label">
        /// Section title; also the section's ImGui ID within the window. The
        /// immediate-mode ID rules apply: two headers with the same visible
        /// title in one window share their open state, so disambiguate with a
        /// <c>"##"</c> suffix (see the ID rules in docs/10-api-fundamentals.md).
        /// </param>
        /// <param name="defaultOpen">
        /// True starts (and re-starts) the section open; false starts it
        /// collapsed. Only consulted when ImGui has no stored state for this
        /// header yet.
        /// </param>
        /// <returns>
        /// True while the section is open. Draw the section's content inside
        /// the <c>if</c> body — this is a single call, not a Begin/End pair,
        /// so there is no scope to dispose. False when unavailable.
        /// </returns>
        /// <remarks>
        /// The open/closed state lives per window and is keyed by the label's
        /// ID, so it survives frames while the window lives but is not
        /// persisted across sessions — every launch starts from
        /// <paramref name="defaultOpen"/> again, the same contract as window
        /// positions.
        /// </remarks>
        public static bool CollapsingHeader(string label, bool defaultOpen = false)
        {
            if (!CanDeclareUi)
            {
                return false;
            }
            RowItemHook();
            return ImGuiInternal.CollapsingHeader(label, defaultOpen);
        }
    }
}
