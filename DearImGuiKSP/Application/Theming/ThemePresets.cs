using System;
using Color32 = UnityEngine.Color32;
using Vector2 = UnityEngine.Vector2;

namespace DearImGuiKSP.Application
{
    /// <summary>
    /// A named theme preset: value tables applied over the stock dark base
    /// (spec §5.1). The color/var tables hold only the slots a theme overrides —
    /// <see cref="ThemeEngine"/> resets to stock dark before every apply, so
    /// unmapped slots keep their exact stock values. Gradient parameters are
    /// stored for the C9 gradient helpers; C8 wires only colors and vars.
    /// Application-layer; UnityEngine.CoreModule for <c>Color32</c>/<c>Vector2</c> only (D24).
    /// </summary>
    internal sealed class ThemePreset
    {
        internal ThemePreset(
            string name,
            (ImGuiCol Col, Color32 Color)[] colors,
            (ImGuiStyleVar Var, float Value)[] floatVars,
            (ImGuiStyleVar Var, Vector2 Value)[] vec2Vars,
            Color32 windowBgGradientTop,
            Color32 windowBgGradientBottom,
            Color32 buttonGradientTop,
            Color32 buttonGradientBottom,
            Color32 buttonSecondaryGradientTop,
            Color32 buttonSecondaryGradientBottom)
        {
            Name = name;
            Colors = colors;
            FloatVars = floatVars;
            Vec2Vars = vec2Vars;
            WindowBgGradientTop = windowBgGradientTop;
            WindowBgGradientBottom = windowBgGradientBottom;
            ButtonGradientTop = buttonGradientTop;
            ButtonGradientBottom = buttonGradientBottom;
            ButtonSecondaryGradientTop = buttonSecondaryGradientTop;
            ButtonSecondaryGradientBottom = buttonSecondaryGradientBottom;
        }

        /// <summary>Preset name as it appears in settings.cfg ("ksp" or "dark").</summary>
        internal string Name { get; }

        /// <summary>Color slots this preset overrides; every entry is written over the stock dark base.</summary>
        internal (ImGuiCol Col, Color32 Color)[] Colors { get; }

        /// <summary>Float style vars this preset overrides (rounding, border sizes).</summary>
        internal (ImGuiStyleVar Var, float Value)[] FloatVars { get; }

        /// <summary>Vec2 style vars this preset overrides (paddings, spacing).</summary>
        internal (ImGuiStyleVar Var, Vector2 Value)[] Vec2Vars { get; }

        /// <summary>Window background gradient top stop (C9 draws the gradient; spec §6.1).</summary>
        internal Color32 WindowBgGradientTop { get; }

        /// <summary>Window background gradient bottom stop (C9; spec §6.1).</summary>
        internal Color32 WindowBgGradientBottom { get; }

        /// <summary>Button gradient top stop (C9; spec §6.1 "Buttons").</summary>
        internal Color32 ButtonGradientTop { get; }

        /// <summary>Button gradient bottom stop (C9; spec §6.1 "Buttons").</summary>
        internal Color32 ButtonGradientBottom { get; }

        /// <summary>Secondary (plain grey) button gradient top stop (C9; spec §6.1 "Buttons", M3 tuning pass).</summary>
        internal Color32 ButtonSecondaryGradientTop { get; }

        /// <summary>Secondary (plain grey) button gradient bottom stop (C9; spec §6.1 "Buttons", M3 tuning pass).</summary>
        internal Color32 ButtonSecondaryGradientBottom { get; }
    }

    /// <summary>
    /// The built-in theme presets (spec §5.1, D25): "ksp" (the default) and
    /// "dark" (the exact stock ImGui dark, retained for regression). "dark" is
    /// a marker with zero overrides — the engine applies it as the native
    /// <c>StyleColorsDark()</c> reset plus nothing, never a hand-copied table.
    /// </summary>
    internal static class ThemePresets
    {
        /// <summary>The "ksp" preset: KSP UI palette and rounding (spec §6.1).</summary>
        internal static ThemePreset Ksp()
        {
            var colors = new (ImGuiCol Col, Color32 Color)[]
            {
                (ImGuiCol.Text, KspPalette.TextOffWhite),
                (ImGuiCol.TextDisabled, KspPalette.TextLightGrey),
                // Flat slot for the window background; C9 draws the two-stop
                // gradient (WindowBgGradientTop/Bottom) over it.
                (ImGuiCol.WindowBg, KspPalette.WindowBgBottom),
                (ImGuiCol.ChildBg, KspPalette.WindowBgBottom),
                (ImGuiCol.PopupBg, KspPalette.WindowBgBottom),
                (ImGuiCol.Border, KspPalette.BorderDark),
                (ImGuiCol.FrameBg, KspPalette.FrameBg),
                (ImGuiCol.FrameBgHovered, KspPalette.FrameBgHovered),
                (ImGuiCol.FrameBgActive, KspPalette.FrameBgActive),
                (ImGuiCol.TitleBg, KspPalette.TitleBar),
                (ImGuiCol.TitleBgActive, KspPalette.TitleBar),
                (ImGuiCol.TitleBgCollapsed, KspPalette.TitleBar),
                (ImGuiCol.MenuBarBg, KspPalette.TitleBar),
                (ImGuiCol.ScrollbarBg, KspPalette.WindowBgTop),
                (ImGuiCol.ScrollbarGrab, KspPalette.ButtonGradientTop),
                (ImGuiCol.ScrollbarGrabHovered, KspPalette.TextLightGrey),
                (ImGuiCol.ScrollbarGrabActive, KspPalette.TextOffWhite),
                // Checkbox tick and radio button circle: radio active fill is the
                // KSP light green (spec §6.1 "Radio buttons"; circular drawing lands in C9).
                (ImGuiCol.CheckMark, KspPalette.GreenLight),
                (ImGuiCol.SliderGrab, KspPalette.ButtonGradientTop),
                (ImGuiCol.SliderGrabActive, KspPalette.TextLightGrey),
                // Flat slot for buttons; C9 draws the two-stop gradient
                // (ButtonGradientTop/Bottom) via the ShadeVerts technique (D28).
                (ImGuiCol.Button, KspPalette.ButtonGradientTop),
                (ImGuiCol.ButtonHovered, KspPalette.ButtonHover),
                (ImGuiCol.ButtonActive, KspPalette.ButtonActive),
                (ImGuiCol.Header, KspPalette.TitleBar),
                (ImGuiCol.HeaderHovered, KspPalette.ButtonGradientTop),
                (ImGuiCol.HeaderActive, KspPalette.TitleBar),
                // C31: resize-grip visibility — stock dark leaves these at
                // white ~20% alpha, nearly invisible on the theme background.
                // Rest is subtle but clearly present; hover/active brighten
                // toward off-white. RGB comes from the palette, alpha only
                // distinguishes the three states.
                (ImGuiCol.ResizeGrip, new Color32(KspPalette.TextLightGrey.r, KspPalette.TextLightGrey.g, KspPalette.TextLightGrey.b, 117)),
                (ImGuiCol.ResizeGripHovered, new Color32(KspPalette.TextOffWhite.r, KspPalette.TextOffWhite.g, KspPalette.TextOffWhite.b, 160)),
                (ImGuiCol.ResizeGripActive, new Color32(KspPalette.TextOffWhite.r, KspPalette.TextOffWhite.g, KspPalette.TextOffWhite.b, 220)),
            };

            var floatVars = new (ImGuiStyleVar Var, float Value)[]
            {
                (ImGuiStyleVar.WindowRounding, 6f),     // spec §6.1 "Window background": rounding 6 px
                (ImGuiStyleVar.WindowBorderSize, 1f),   // spec §6.1 "Window background": 1 px border
                (ImGuiStyleVar.FrameRounding, 4f),      // spec §6.1 "Frame backgrounds": rounding 4 px
                (ImGuiStyleVar.GrabRounding, 4f),       // spec §6.1 "Slider grab / scrollbar grab": 4 px
                (ImGuiStyleVar.ScrollbarRounding, 4f),  // spec §6.1 "Slider grab / scrollbar grab": 4 px
                (ImGuiStyleVar.TabRounding, 4f),        // spec §6.1 "Headers / selectables" family: 4 px
                (ImGuiStyleVar.ChildRounding, 4f),
                (ImGuiStyleVar.PopupRounding, 4f),
            };

            var vec2Vars = new (ImGuiStyleVar Var, Vector2 Value)[]
            {
                // Starting value, tuned in-game per spec §13.
                (ImGuiStyleVar.WindowPadding, new Vector2(8f, 8f)),
            };

            return new ThemePreset(
                LibraryConfig.DefaultTheme,
                colors,
                floatVars,
                vec2Vars,
                KspPalette.WindowBgTop,
                KspPalette.WindowBgBottom,
                KspPalette.ButtonGradientTop,
                KspPalette.ButtonGradientBottom,
                KspPalette.ButtonSecondaryGradientTop,
                KspPalette.ButtonSecondaryGradientBottom);
        }

        /// <summary>
        /// The "dark" preset: a marker with zero overrides. Applying it is the
        /// native <c>StyleColorsDark()</c> reset plus nothing, which keeps it
        /// byte-exact stock ImGui dark (spec §5.1; regression-gated).
        /// </summary>
        /// The gradient stops are never rendered under "dark" (gradient drawing
        /// is a ksp-theme feature; C9 decides by preset name) — the ksp palette
        /// values are carried only to keep the params non-degenerate.
        internal static ThemePreset Dark()
        {
            return new ThemePreset(
                LibraryConfig.DarkThemeName,
                new (ImGuiCol Col, Color32 Color)[0],
                new (ImGuiStyleVar Var, float Value)[0],
                new (ImGuiStyleVar Var, Vector2 Value)[0],
                KspPalette.WindowBgTop,
                KspPalette.WindowBgBottom,
                KspPalette.ButtonGradientTop,
                KspPalette.ButtonGradientBottom,
                KspPalette.ButtonSecondaryGradientTop,
                KspPalette.ButtonSecondaryGradientBottom);
        }

        /// <summary>
        /// Resolves a normalized theme name to a preset. "dark" (any case) is the
        /// stock-dark marker; everything else resolves to "ksp" — the settings
        /// model normalizes before names reach this point (spec §9, D25).
        /// </summary>
        internal static ThemePreset ForName(string name)
        {
            return string.Equals(name, LibraryConfig.DarkThemeName, StringComparison.OrdinalIgnoreCase)
                ? Dark()
                : Ksp();
        }
    }
}
