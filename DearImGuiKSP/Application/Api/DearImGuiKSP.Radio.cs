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
        /// light green).
        /// </summary>
        /// <returns>True on the frame the button is clicked; false when unavailable.</returns>
        public static bool RadioButton(string label, ref bool value)
        {
            if (!IsAvailable)
            {
                return false;
            }
            return ImGuiInternal.RadioButton(label, value);
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
        /// light green).
        /// </summary>
        /// <returns>True on the frame the button is clicked; false when unavailable.</returns>
        public static bool RadioButton(string label, ref int value, int option)
        {
            if (!IsAvailable)
            {
                return false;
            }
            return ImGuiInternal.RadioButton(label, ref value, option);
        }
    }
}
