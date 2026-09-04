using DearImGuiKSP.Application;
using DearImGuiKSP.Interop;
using Color32 = UnityEngine.Color32;
using Vector2 = UnityEngine.Vector2;

namespace DearImGuiKSP
{
    /// <summary>
    /// Selects the gradient stops for <see cref="ImGuiGradients.GradientButton(string, Vector2, GradientButtonStyle)"/>.
    /// Both styles render identically in every theme: a gradient button is an
    /// explicit consumer call, so the stops come from the KSP palette even when
    /// the active theme is "dark" (which has no gradient parameters of its own).
    /// </summary>
    public enum GradientButtonStyle
    {
        /// <summary>The signature blue-grey gradient: 102,114,135 to 57,72,90 (spec §6.1 "Buttons").</summary>
        Primary = 0,

        /// <summary>The plain grey gradient added in the M3 tuning pass: 135,143,158 to 69,77,92.</summary>
        Secondary = 1,
    }

    /// <summary>
    /// Two-stop vertical gradient drawing helpers (spec §6.1, technique D28):
    /// fill a rect with the mid color via <c>ImDrawList_AddRectFilled</c>, then
    /// rewrite exactly the verts that fill appended with
    /// <c>igShadeVertsLinearColorGradientKeepAlpha</c> — rounding and alpha are
    /// preserved (unlike <c>ImDrawList_AddRectFilledMultiColor</c>, which is
    /// square-cornered). Also hosts <see cref="GradientButton"/>, the KSP
    /// theme's signature button. Only valid inside a registered callback;
    /// every member is a no-op/false when <see cref="DearImGuiKSP.IsAvailable"/>
    /// is false. All color math is struct-only — zero heap allocation per call.
    /// </summary>
    public static class ImGuiGradients
    {
        // Hover: both gradient stops lightened toward white (spec §6.1
        // "Buttons": hover ~15% lighter).
        private const float HoverLightenAmount = 0.15f;

        // Active: both stops shifted toward the KSP light green
        // (spec §6.1 "Buttons": active shifted toward KSP green). Tunable;
        // raised 0.30 -> 0.55 in the M3 tuning pass per user feedback ("could
        // be brighter").
        private const float ActiveGreenMix = 0.55f;

        // Rounding of the gradient button rect; matches the ksp preset's
        // FrameRounding (4 px, spec §6.1 "Frame backgrounds").
        private const float ButtonRounding = 4f;

        // Fit-to-label padding when a GradientButton size component is <= 0.
        // ImGuiStyle.FramePadding has no cimgui accessor, so the fallback is a
        // fixed, deliberately generous constant instead of the stock (4,3).
        private const float FitPadX = 24f;
        private const float FitPadY = 12f;

        /// <summary>
        /// Adds a filled rectangle with a two-stop vertical gradient to the
        /// current window's draw list (spec §6.1, technique D28): the rect is
        /// filled with the midpoint of <paramref name="top"/>/<paramref name="bottom"/>,
        /// then exactly the verts that fill appended are shaded from
        /// <paramref name="top"/> (at <paramref name="min"/>) to
        /// <paramref name="bottom"/> (at <paramref name="max"/>), preserving
        /// each vert's alpha and the rect's rounding. No-op when unavailable.
        /// </summary>
        /// <param name="min">Top-left corner, in screen coordinates.</param>
        /// <param name="max">Bottom-right corner, in screen coordinates.</param>
        /// <param name="top">Gradient stop at <paramref name="min"/> (sRGB bytes).</param>
        /// <param name="bottom">Gradient stop at <paramref name="max"/> (sRGB bytes).</param>
        /// <param name="rounding">Corner radius in pixels (D28 keeps rounding).</param>
        public static void AddRectFilledGradientVertical(Vector2 min, Vector2 max, Color32 top, Color32 bottom, float rounding)
        {
            if (!DearImGuiKSP.IsAvailable)
            {
                return;
            }

            ImGuiInternal.ImDrawListHandle drawList = ImGuiInternal.GetWindowDrawList();
            int vertStart = ImGuiInternal.GetDrawListVtxCount(drawList);
            ImGuiInternal.DrawListAddRectFilled(
                drawList,
                new ImVec2(min.x, min.y),
                new ImVec2(max.x, max.y),
                Pack(Midpoint(top, bottom)),
                rounding);
            int vertEnd = ImGuiInternal.GetDrawListVtxCount(drawList);
            if (vertEnd > vertStart)
            {
                ImGuiInternal.ShadeVertsLinearColorGradientKeepAlpha(
                    drawList,
                    vertStart,
                    vertEnd,
                    new ImVec2(min.x, min.y),
                    new ImVec2(min.x, max.y),
                    Pack(top),
                    Pack(bottom));
            }
        }

        /// <summary>
        /// Draws the KSP theme's signature button (spec §6.1 "Buttons"): a
        /// two-stop vertical gradient rect with a centered label. Interaction
        /// runs through ImGui's stock ButtonBehavior (via an invisible button),
        /// so clicking, hovering, and held state behave exactly like a normal
        /// button. While hovered, both gradient stops lighten ~15%; while held,
        /// both shift toward the KSP light green.
        /// </summary>
        /// <param name="label">Button text; also its ImGui identity.</param>
        /// <param name="top">Gradient top stop (sRGB bytes).</param>
        /// <param name="bottom">Gradient bottom stop (sRGB bytes).</param>
        /// <param name="size">
        /// Button size in pixels; a component &lt;= 0 fits that dimension to the
        /// label plus a fixed padding (see <see cref="AddRectFilledGradientVertical"/>
        /// for the gradient itself).
        /// </param>
        /// <returns>True on the frame the button is clicked; false when unavailable.</returns>
        public static bool GradientButton(string label, Color32 top, Color32 bottom, Vector2 size)
        {
            if (!DearImGuiKSP.IsAvailable)
            {
                return false;
            }

            if (size.x <= 0f || size.y <= 0f)
            {
                ImVec2 labelSize = ImGuiInternal.CalcTextSize(label);
                if (size.x <= 0f)
                {
                    size.x = labelSize.X + FitPadX;
                }
                if (size.y <= 0f)
                {
                    size.y = labelSize.Y + FitPadY;
                }
            }

            // InvisibleButton runs the full ButtonBehavior logic and advances
            // the cursor; the rect is read back and drawn over. (igButtonBehavior
            // itself is exported — cimgui.h:5565 — but needs the bounding box up
            // front, which would require a managed ImGuiStyle.FramePadding read;
            // cimgui exports no accessor for it, so the contract's sanctioned
            // fallback is used instead.)
            bool pressed = ImGuiInternal.InvisibleButton(label, new ImVec2(size.x, size.y));
            bool hovered = ImGuiInternal.IsItemHovered();
            bool held = ImGuiInternal.IsItemActive();

            Color32 topStop = top;
            Color32 bottomStop = bottom;
            if (held)
            {
                topStop = Lerp(top, KspPalette.GreenLight, ActiveGreenMix);
                bottomStop = Lerp(bottom, KspPalette.GreenLight, ActiveGreenMix);
            }
            else if (hovered)
            {
                topStop = Lighten(top, HoverLightenAmount);
                bottomStop = Lighten(bottom, HoverLightenAmount);
            }

            ImVec2 bbMin = ImGuiInternal.GetItemRectMin();
            ImVec2 bbMax = ImGuiInternal.GetItemRectMax();
            AddRectFilledGradientVertical(
                new Vector2(bbMin.X, bbMin.Y),
                new Vector2(bbMax.X, bbMax.Y),
                topStop,
                bottomStop,
                ButtonRounding);

            // Centered label in the current text color.
            ImVec2 textSize = ImGuiInternal.CalcTextSize(label);
            ImGuiInternal.DrawListAddText(
                ImGuiInternal.GetWindowDrawList(),
                new ImVec2(
                    bbMin.X + (size.x - textSize.X) * 0.5f,
                    bbMin.Y + (size.y - textSize.Y) * 0.5f),
                ImGuiInternal.GetColorU32((int)ImGuiCol.Text),
                label);

            return pressed;
        }

        /// <summary>
        /// Draws a gradient button whose stops come from the active theme
        /// preset (M3 tuning pass): <see cref="GradientButtonStyle.Primary"/>
        /// uses the signature blue-grey gradient, <see cref="GradientButtonStyle.Secondary"/>
        /// the plain grey one. Identical interaction and hover/active feedback
        /// to the explicit-color overload, which this delegates to. Only valid
        /// inside a registered callback.
        /// </summary>
        /// <param name="label">Button text; also its ImGui identity.</param>
        /// <param name="size">
        /// Button size in pixels; a component &lt;= 0 fits that dimension to the
        /// label plus a fixed padding.
        /// </param>
        /// <param name="style">Which themed gradient stop pair to draw.</param>
        /// <returns>True on the frame the button is clicked; false when unavailable.</returns>
        /// <remarks>
        /// Gradient buttons are an explicit consumer call rather than a theme
        /// default, so they render with the KSP palette stops in every theme —
        /// under "dark" (which carries no gradient parameters) both styles fall
        /// back to the same KSP constants the presets hold.
        /// </remarks>
        public static bool GradientButton(string label, Vector2 size, GradientButtonStyle style)
        {
            if (!DearImGuiKSP.IsAvailable)
            {
                return false;
            }

            // Stops from the active preset; the preset always carries them
            // (the dark preset holds the ksp values to stay non-degenerate),
            // so the palette fallback only covers an unwired ThemeEngine.
            Application.ThemePreset preset = DearImGuiKSP.ThemeEngine?.ActivePreset;
            Color32 top = KspPalette.ButtonGradientTop;
            Color32 bottom = KspPalette.ButtonGradientBottom;
            if (preset != null)
            {
                bool secondary = style == GradientButtonStyle.Secondary;
                top = secondary ? preset.ButtonSecondaryGradientTop : preset.ButtonGradientTop;
                bottom = secondary ? preset.ButtonSecondaryGradientBottom : preset.ButtonGradientBottom;
            }

            return GradientButton(label, top, bottom, size);
        }

        // Per-channel lerp of two sRGB byte colors; pure struct math.
        internal static Color32 Lerp(Color32 a, Color32 b, float t)
        {
            return new Color32(
                (byte)(a.r + (b.r - a.r) * t),
                (byte)(a.g + (b.g - a.g) * t),
                (byte)(a.b + (b.b - a.b) * t),
                (byte)(a.a + (b.a - a.a) * t));
        }

        // Lightens a color toward white by the given amount; pure struct math.
        internal static Color32 Lighten(Color32 c, float amount)
        {
            return Lerp(c, new Color32(255, 255, 255, 255), amount);
        }

        // Midpoint of the two gradient stops (the pre-shade fill color).
        private static Color32 Midpoint(Color32 a, Color32 b)
        {
            return Lerp(a, b, 0.5f);
        }

        // Packs sRGB bytes into ImGui's ImU32 (A<<24 | B<<16 | G<<8 | R,
        // imgui.h IM_COL32). Pure struct math.
        private static uint Pack(Color32 c)
        {
            return (uint)(c.r | (c.g << 8) | (c.b << 16) | (c.a << 24));
        }
    }
}
