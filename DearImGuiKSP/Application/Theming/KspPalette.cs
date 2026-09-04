using Color32 = UnityEngine.Color32;

namespace DearImGuiKSP.Application
{
    /// <summary>
    /// KSP UI palette constants (design spec §6.1, sourced from
    /// VisualReferenceMaterial/KSP_Theme_Palette_Notes.md). These values are
    /// starting points, tuned in-game against UIStylingRef.png per spec §13 —
    /// change them here only, never inline in the presets. Public so consumers
    /// can opt into the same palette for their own accents (e.g. colored
    /// headers via <see cref="DearImGuiKSP.DearImGuiKSP.TextColored(UnityEngine.Color32, string)"/>;
    /// the theme itself never auto-colors consumer text).
    /// Application-layer; UnityEngine.CoreModule for <c>Color32</c> only (D24).
    /// </summary>
    public static class KspPalette
    {
        /// <summary>Window background two-stop gradient top stop (spec §6.1 "Window background"; lighter grey at the top after the M3 tuning pass).</summary>
        public static readonly Color32 WindowBgTop = new Color32(94, 97, 106, 255);

        /// <summary>Window background two-stop gradient bottom stop (spec §6.1 "Window background"; darkest grey at the bottom after the M3 tuning pass).</summary>
        public static readonly Color32 WindowBgBottom = new Color32(58, 58, 63, 255);

        /// <summary>1 px window border (spec §6.1 "Window background").</summary>
        public static readonly Color32 BorderDark = new Color32(30, 32, 38, 255);

        /// <summary>Title bar, flat (spec §6.1 "Title bar").</summary>
        public static readonly Color32 TitleBar = new Color32(57, 72, 90, 255);

        /// <summary>Primary button two-stop gradient top stop (spec §6.1 "Buttons"; blue-grey); also the slider/scrollbar grab color (spec §6.1 "Slider grab / scrollbar grab").</summary>
        public static readonly Color32 ButtonGradientTop = new Color32(102, 114, 135, 255);

        /// <summary>Primary button two-stop gradient bottom stop (spec §6.1 "Buttons"; blue-grey).</summary>
        public static readonly Color32 ButtonGradientBottom = new Color32(57, 72, 90, 255);

        /// <summary>Secondary button two-stop gradient top stop (spec §6.1 "Buttons"; plain grey — added in the M3 tuning pass).</summary>
        public static readonly Color32 ButtonSecondaryGradientTop = new Color32(135, 143, 158, 255);

        /// <summary>Secondary button two-stop gradient bottom stop (spec §6.1 "Buttons"; plain grey — added in the M3 tuning pass).</summary>
        public static readonly Color32 ButtonSecondaryGradientBottom = new Color32(69, 77, 92, 255);

        /// <summary>Button hover: gradient top lightened ~15% (spec §6.1 "Buttons").</summary>
        public static readonly Color32 ButtonHover = new Color32(125, 135, 153, 255);

        /// <summary>Button active: gradient top shifted ~30% toward KSP light green (spec §6.1 "Buttons": "active shifted toward KSP green").</summary>
        public static readonly Color32 ButtonActive = new Color32(126, 155, 95, 255);

        /// <summary>Frame background (inputs, sliders, combos): the darkest window grey rgb(58,58,63) per the M3 tuning pass.</summary>
        public static readonly Color32 FrameBg = new Color32(58, 58, 63, 255);

        /// <summary>Frame background hover: <see cref="FrameBg"/> lightened ~15% (same hover-lightening approach as <see cref="ButtonHover"/>).</summary>
        public static readonly Color32 FrameBgHovered = new Color32(88, 88, 92, 255);

        /// <summary>Frame background active: <see cref="FrameBg"/> lightened ~30% (hover approach at a stronger amount so held frames read clearly).</summary>
        public static readonly Color32 FrameBgActive = new Color32(117, 117, 121, 255);

        /// <summary>Text: default off-white on the main fill grey (spec §6.1 "Text").</summary>
        public static readonly Color32 TextOffWhite = new Color32(236, 236, 236, 255);

        /// <summary>Text: secondary light grey (spec §6.1 "Text").</summary>
        public static readonly Color32 TextLightGrey = new Color32(188, 188, 188, 255);

        /// <summary>Text: dark variant for text sitting on dark infill.</summary>
        public static readonly Color32 TextOnDarkFill = new Color32(58, 58, 63, 255);

        /// <summary>KSP light green accent (spec §6.1 "Accents"); checkbox tick, radio fill, active-state mix target.</summary>
        public static readonly Color32 GreenLight = new Color32(181, 252, 0, 255);

        /// <summary>KSP dark green accent (spec §6.1 "Accents").</summary>
        public static readonly Color32 GreenDark = new Color32(51, 230, 51, 255);

        /// <summary>KSP light orange accent (spec §6.1 "Accents"); the ksp theme types this into text inputs.</summary>
        public static readonly Color32 OrangeLight = new Color32(255, 198, 0, 255);

        /// <summary>KSP dark orange accent (spec §6.1 "Accents").</summary>
        public static readonly Color32 OrangeDark = new Color32(255, 150, 0, 255);
    }
}
