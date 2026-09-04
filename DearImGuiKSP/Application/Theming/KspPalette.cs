using Color32 = UnityEngine.Color32;

namespace DearImGuiKSP.Application
{
    /// <summary>
    /// KSP UI palette constants (design spec §6.1, sourced from
    /// VisualReferenceMaterial/KSP_Theme_Palette_Notes.md). These values are
    /// starting points, tuned in-game against UIStylingRef.png per spec §13 —
    /// change them here only, never inline in the presets.
    /// Application-layer; UnityEngine.CoreModule for <c>Color32</c> only (D24).
    /// </summary>
    internal static class KspPalette
    {
        // Window background two-stop gradient (spec §6.1 "Window background").
        internal static readonly Color32 WindowBgTop = new Color32(58, 58, 63, 255);
        internal static readonly Color32 WindowBgBottom = new Color32(94, 97, 106, 255);

        // 1 px window border (spec §6.1 "Window background").
        internal static readonly Color32 BorderDark = new Color32(30, 32, 38, 255);

        // Title bar, flat (spec §6.1 "Title bar").
        internal static readonly Color32 TitleBar = new Color32(57, 72, 90, 255);

        // Button two-stop gradient (spec §6.1 "Buttons"); also the slider/scrollbar
        // grab color (spec §6.1 "Slider grab / scrollbar grab").
        internal static readonly Color32 ButtonGradientTop = new Color32(102, 114, 135, 255);
        internal static readonly Color32 ButtonGradientBottom = new Color32(57, 72, 90, 255);

        // Button hover: gradient top lightened ~15% (spec §6.1 "Buttons").
        internal static readonly Color32 ButtonHover = new Color32(125, 135, 153, 255);

        // Button active: gradient top shifted ~30% toward KSP light green
        // (spec §6.1 "Buttons": "active shifted toward KSP green").
        internal static readonly Color32 ButtonActive = new Color32(126, 155, 95, 255);

        // Frame backgrounds (inputs, sliders, combos): rgb(57,72,90) darkened ~20%
        // (spec §6.1 "Frame backgrounds").
        internal static readonly Color32 FrameBg = new Color32(46, 58, 72, 255);

        // Text: default off-white on the main fill grey (spec §6.1 "Text");
        // secondary light grey; dark variant for text sitting on dark infill.
        internal static readonly Color32 TextOffWhite = new Color32(236, 236, 236, 255);
        internal static readonly Color32 TextLightGrey = new Color32(188, 188, 188, 255);
        internal static readonly Color32 TextOnDarkFill = new Color32(58, 58, 63, 255);

        // Accents (spec §6.1 "Accents").
        internal static readonly Color32 GreenLight = new Color32(181, 252, 0, 255);
        internal static readonly Color32 GreenDark = new Color32(51, 230, 51, 255);
        internal static readonly Color32 OrangeLight = new Color32(255, 198, 0, 255);
        internal static readonly Color32 OrangeDark = new Color32(255, 150, 0, 255);
    }
}
