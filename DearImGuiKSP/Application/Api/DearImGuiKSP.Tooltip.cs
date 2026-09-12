using DearImGuiKSP.Interop;

namespace DearImGuiKSP
{
    public static partial class DearImGuiKSP
    {
        // Stock tooltip wrap width in font-size units (imgui demo pattern:
        // PushTextWrapPos(GetFontSize() * 35)). BeginTooltip pushes no wrap
        // pos of its own (imgui.cpp BeginTooltipEx), so without this a long
        // tooltip would render on one line.
        internal const float TooltipWrapWidthFactor = 35f;

        /// <summary>
        /// Shows <paramref name="text"/> as a hover tooltip for the item
        /// declared immediately before this call (FR-3, 1.3.0) — the curated
        /// equivalent of stock <c>SetItemTooltip</c>, composed from
        /// non-variadic exports. The tooltip appears after the stock hover
        /// delay (<c>ImGuiHoveredFlags_ForTooltip</c>: stationary-or-delay,
        /// allow-when-disabled, no shared delay) on the stock tooltip window,
        /// which renders above consumer windows and follows the theme and
        /// font scale. Only valid inside a registered callback.
        /// </summary>
        /// <param name="text">
        /// Tooltip content. Null or empty is a no-op (no tooltip window is
        /// begun). Multi-line text (<c>\n</c>) renders as multiple lines;
        /// long single-line text wraps at 35 × the current font size (the
        /// stock demo width).
        /// </param>
        /// <remarks>
        /// With no item declared before it (e.g. at the very start of a
        /// window), the call is a safe no-op — the hover test reads no item
        /// and returns false. The tooltip is not a layout item: it does not
        /// participate in <c>ImGuiEx.Row</c> spacing. No
        /// <c>ImGuiTooltipFlags</c> passthrough in this version.
        /// </remarks>
        public static void Tooltip(string text)
        {
            if (!CanDeclareUi)
            {
                return;
            }
            if (string.IsNullOrEmpty(text))
            {
                return;
            }
            if (!ImGuiInternal.IsItemHovered(ImGuiHoveredFlags.ForTooltip))
            {
                return;
            }
            if (!ImGuiInternal.BeginTooltip())
            {
                return;
            }
            try
            {
                ImGuiInternal.PushTextWrapPos(ImGuiInternal.GetFontSize() * TooltipWrapWidthFactor);
                try
                {
                    ImGuiInternal.Text(text);
                }
                finally
                {
                    ImGuiInternal.PopTextWrapPos();
                }
            }
            finally
            {
                ImGuiInternal.EndTooltip();
            }
        }
    }
}
