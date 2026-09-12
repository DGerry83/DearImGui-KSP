using DearImGuiKSP.Interop;
using Color32 = UnityEngine.Color32;

namespace DearImGuiKSP
{
    public static partial class DearImGuiKSP
    {
        /// <summary>
        /// Draws unformatted text in an explicit color — the consumer-choice
        /// pattern for accents such as colored headers. The theme never
        /// auto-colors consumer text; opting in is the consumer's decision, e.g.
        /// <c>TextColored(KspPalette.GreenLight, "Header")</c> for the KSP light
        /// green. Only valid inside a registered callback. Null renders as an
        /// empty string.
        /// </summary>
        /// <param name="color">The text color; sRGB bytes in 0–255 range.</param>
        /// <param name="text">The text to draw.</param>
        public static void TextColored(Color32 color, string text)
        {
            if (!CanDeclareUi)
            {
                return;
            }
            RowItemHook();
            PushStyleColor(ImGuiCol.Text, color);
            ImGuiInternal.Text(text);
            PopStyleColor();
        }
    }
}
