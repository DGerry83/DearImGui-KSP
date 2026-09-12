using System;
using DearImGuiKSP.Application;
using DearImGuiKSP.Interop;

namespace DearImGuiKSP
{
    public static partial class DearImGuiKSP
    {
        /// <summary>
        /// Draws a circular radio button (spec §6.1 "Radio buttons"). The
        /// button is selected when <paramref name="value"/> is true; clicking
        /// selects it (sets it to true). Only valid inside a registered
        /// callback. The selected fill color is the theme's
        /// <c>ImGuiCol.CheckMark</c> slot (the ksp preset sets it to the KSP
        /// light green). Under the "dark" theme the KSP-palette rim is skipped,
        /// keeping the stock rendering byte-exact.
        /// </summary>
        /// <returns>True on the frame the button is clicked; false when unavailable.</returns>
        public static bool RadioButton(string label, ref bool value)
        {
            if (!CanDeclareUi)
            {
                return false;
            }
            RowItemHook();
            // igRadioButton_Bool takes `active` by value and cannot write back,
            // so the click applies here (G2-07).
            bool clicked = ImGuiInternal.RadioButton(label, value);
            ApplyRadioClick(clicked, ref value);
            DrawRadioRimIfKspTheme();
            return clicked;
        }

        /// <summary>
        /// Draws a circular radio button bound to an integer value (spec §6.1
        /// "Radio buttons"). The button is selected when
        /// <paramref name="value"/> equals <paramref name="option"/>; clicking
        /// sets <paramref name="value"/> to <paramref name="option"/>. Use a
        /// group of these with distinct <paramref name="option"/> values for a
        /// mutually-exclusive option set. Only valid inside a registered
        /// callback. The selected fill color is the theme's
        /// <c>ImGuiCol.CheckMark</c> slot (the ksp preset sets it to the KSP
        /// light green). Under the "dark" theme the KSP-palette rim is skipped,
        /// keeping the stock rendering byte-exact.
        /// </summary>
        /// <returns>True on the frame the button is clicked; false when unavailable.</returns>
        public static bool RadioButton(string label, ref int value, int option)
        {
            if (!CanDeclareUi)
            {
                return false;
            }
            RowItemHook();
            bool clicked = ImGuiInternal.RadioButton(label, ref value, option);
            DrawRadioRimIfKspTheme();
            return clicked;
        }

        // G2-07: a click selects the button (sets the ref target true; a radio
        // is never unselected by clicking). Internal seam so the assignment is
        // testable without a native context.
        internal static void ApplyRadioClick(bool clicked, ref bool value)
        {
            if (clicked)
            {
                value = true;
            }
        }

        // G3-21: the rim is a KSP-palette decoration; the "dark" preset's
        // byte-exact-stock guarantee forbids it. Same stock-when-unwired rule
        // as the InputText facade path: no ThemeEngine means no theming, hence
        // no KSP decoration.
        private static void DrawRadioRimIfKspTheme()
        {
            if (ThemeEngine == null ||
                string.Equals(ThemeEngine.CurrentThemeName, LibraryConfig.DarkThemeName, StringComparison.Ordinal))
            {
                return;
            }
            DrawRadioRim();
        }

        // Light-grey interior rim so the ring stays readable against the dark
        // end of the window-bg gradient (M3 in-game fix). Drawn over the stock
        // radio geometry at the same item rect — the button does not grow.
        private static void DrawRadioRim()
        {
            ImVec2 min = ImGuiInternal.GetItemRectMin();
            ImVec2 max = ImGuiInternal.GetItemRectMax();
            float frameHeight = max.Y - min.Y;
            var center = new ImVec2(min.X + frameHeight * 0.5f, min.Y + frameHeight * 0.5f);
            ImGuiInternal.DrawListAddCircle(
                ImGuiInternal.GetWindowDrawList(),
                center,
                frameHeight * 0.5f - 1f,
                ImGuiInternal.GetColorU32(ToImVec4(KspPalette.TextLightGrey)),
                0,
                1.5f);
        }
    }
}
