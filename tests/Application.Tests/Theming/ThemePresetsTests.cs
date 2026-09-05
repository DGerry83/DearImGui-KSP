using DearImGuiKSP;
using DearImGuiKSP.Application;
using Xunit;
using Color32 = UnityEngine.Color32;

namespace Application.Tests
{
    /// <summary>
    /// Theme value-table tests (C8, M3 tuning pass): the "ksp" preset carries the
    /// design spec §6.1 anchors (window gradient lighter-on-top, frame backfill
    /// chain, primary/secondary button gradients), differs from stock dark at
    /// those anchors, and every table entry indexes a real ImGuiCol/ImGuiStyleVar
    /// slot; "dark" is a zero--override marker.
    /// </summary>
    public class ThemePresetsTests
    {
        private static Color32 FindColor(ThemePreset preset, ImGuiCol col)
        {
            foreach ((ImGuiCol entryCol, Color32 color) in preset.Colors)
            {
                if (entryCol == col)
                {
                    return color;
                }
            }
            throw new Xunit.Sdk.XunitException("Preset '" + preset.Name + "' has no entry for " + col);
        }

        [Fact]
        public void Ksp_ColorTable_ContainsSpecAnchors()
        {
            ThemePreset ksp = ThemePresets.Ksp();

            Assert.Equal(new Color32(102, 114, 135, 255), FindColor(ksp, ImGuiCol.Button));
            Assert.Equal(new Color32(57, 72, 90, 255), FindColor(ksp, ImGuiCol.TitleBg));
            Assert.Equal(new Color32(57, 72, 90, 255), FindColor(ksp, ImGuiCol.TitleBgActive));
            Assert.Equal(new Color32(236, 236, 236, 255), FindColor(ksp, ImGuiCol.Text));
            Assert.Equal(new Color32(181, 252, 0, 255), FindColor(ksp, ImGuiCol.CheckMark));
        }

        [Fact]
        public void Ksp_FrameBgChain_MatchesTuningPassAnchors()
        {
            ThemePreset ksp = ThemePresets.Ksp();

            // M3 tuning pass: frame backfill is the darkest window grey
            // rgb(58,58,63); hover/active lighten it ~15%/~30% toward white
            // (the same lightening approach as ButtonHover).
            Assert.Equal(new Color32(58, 58, 63, 255), FindColor(ksp, ImGuiCol.FrameBg));
            Assert.Equal(new Color32(88, 88, 92, 255), FindColor(ksp, ImGuiCol.FrameBgHovered));
            Assert.Equal(new Color32(117, 117, 121, 255), FindColor(ksp, ImGuiCol.FrameBgActive));
        }

        [Fact]
        public void Ksp_GradientParams_MatchSpecAnchors()
        {
            ThemePreset ksp = ThemePresets.Ksp();

            Assert.Equal(new Color32(102, 114, 135, 255), ksp.ButtonGradientTop);
            Assert.Equal(new Color32(57, 72, 90, 255), ksp.ButtonGradientBottom);
            Assert.Equal(new Color32(135, 143, 158, 255), ksp.ButtonSecondaryGradientTop);
            Assert.Equal(new Color32(69, 77, 92, 255), ksp.ButtonSecondaryGradientBottom);
            // M3 tuning pass: the window gradient is lighter on top, darkest at
            // the bottom (the C8 launch values were the other way around).
            Assert.Equal(new Color32(94, 97, 106, 255), ksp.WindowBgGradientTop);
            Assert.Equal(new Color32(58, 58, 63, 255), ksp.WindowBgGradientBottom);
        }

        [Fact]
        public void Ksp_OverridesDifferFromStockDarkAtAnchors()
        {
            ThemePreset ksp = ThemePresets.Ksp();

            // Stock ImGui dark: Text is pure white, Button is the classic blue
            // (0.26, 0.59, 0.98) -> rgb(66, 150, 250). The ksp theme must not
            // leave those stock values at the mapped anchors.
            Assert.NotEqual(new Color32(255, 255, 255, 255), FindColor(ksp, ImGuiCol.Text));
            Assert.NotEqual(new Color32(66, 150, 250, 255), FindColor(ksp, ImGuiCol.Button));
            Assert.NotEqual(new Color32(255, 255, 255, 255), FindColor(ksp, ImGuiCol.CheckMark));
        }

        [Fact]
        public void Ksp_EveryColorEntry_IndexInRange()
        {
            ThemePreset ksp = ThemePresets.Ksp();

            foreach ((ImGuiCol col, Color32 _) in ksp.Colors)
            {
                Assert.InRange((int)col, 0, (int)ImGuiCol.COUNT - 1);
            }
        }

        [Fact]
        public void Ksp_EveryVarEntry_IndexInRange()
        {
            ThemePreset ksp = ThemePresets.Ksp();

            foreach ((ImGuiStyleVar var, float _) in ksp.FloatVars)
            {
                Assert.InRange((int)var, 0, (int)ImGuiStyleVar.COUNT - 1);
            }

            foreach ((ImGuiStyleVar var, UnityEngine.Vector2 _) in ksp.Vec2Vars)
            {
                Assert.InRange((int)var, 0, (int)ImGuiStyleVar.COUNT - 1);
            }
        }

        [Fact]
        public void Ksp_ResizeGripChain_IsVisibleAndBrightens()
        {
            ThemePreset ksp = ThemePresets.Ksp();

            // C31: the stock-dark fallback is white at ~20% alpha (51) — nearly
            // invisible on the theme background. The ksp grip must sit clearly
            // above that at rest and brighten monotonically hover -> active.
            Color32 rest = FindColor(ksp, ImGuiCol.ResizeGrip);
            Color32 hovered = FindColor(ksp, ImGuiCol.ResizeGripHovered);
            Color32 active = FindColor(ksp, ImGuiCol.ResizeGripActive);

            Assert.InRange(rest.a, 90, 160);
            Assert.True(hovered.a > rest.a);
            Assert.True(active.a > hovered.a);
            Assert.Equal(KspPalette.TextLightGrey.r, rest.r);
            Assert.Equal(KspPalette.TextOffWhite.r, active.r);
        }

        [Fact]
        public void Dark_HasZeroOverrides()
        {
            ThemePreset dark = ThemePresets.Dark();

            Assert.Equal(LibraryConfig.DarkThemeName, dark.Name);
            Assert.Empty(dark.Colors);
            Assert.Empty(dark.FloatVars);
            Assert.Empty(dark.Vec2Vars);
        }

        [Fact]
        public void ForName_ResolvesKnownThemesCaseInsensitively_EverythingElseKsp()
        {
            Assert.Equal(LibraryConfig.DarkThemeName, ThemePresets.ForName("dark").Name);
            Assert.Equal(LibraryConfig.DarkThemeName, ThemePresets.ForName("DARK").Name);
            Assert.Equal(LibraryConfig.DefaultTheme, ThemePresets.ForName("ksp").Name);
            Assert.Equal(LibraryConfig.DefaultTheme, ThemePresets.ForName("KSP").Name);
            Assert.Equal(LibraryConfig.DefaultTheme, ThemePresets.ForName("neon").Name);
        }
    }
}
