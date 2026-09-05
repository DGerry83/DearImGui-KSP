using System;
using DearImGuiKSP.Application;
using DearImGuiKSP.Interop;

namespace DearImGuiKSP.Infrastructure
{
    /// <summary>
    /// The library's own in-game settings window (chunk C31): "DearImGui-KSP
    /// Settings", so players can adjust the library config without hand-editing
    /// settings.cfg. Registered as a regular consumer (id
    /// <see cref="LibraryConfig.ModName"/>) through the same
    /// ConsumerRegistry/frame-loop path any consumer uses — the fault barrier
    /// covers it like any other consumer, no special-casing.
    /// Every edit persists immediately via the <see cref="SettingsModel"/>
    /// setters. Theme and UI scale apply live (the ThemeEngine dirty path
    /// re-applies at the next frame start); font and font scale are read only
    /// at startup (atlas rebuild), so the panel states plainly that they apply
    /// on the next KSP start. Steady state with the window hidden is one bool
    /// check and zero allocation (the early return in <see cref="OnFrame"/>);
    /// while open, widget labels are constants and only the ImGui UTF-8
    /// encodings allocate — user-driven, per the demo consumer's convention.
    /// </summary>
    internal sealed class LibraryControlPanel
    {
        internal const string WindowTitle = "DearImGui-KSP Settings";

        private const string ThemeHeading = "Theme";
        private const string KspRadioLabel = "ksp (KSP styling)";
        private const string DarkRadioLabel = "dark (stock ImGui dark)";
        private const string UiScaleSliderLabel = "UI scale";
        private const string UiScaleHint = "Scales the whole interface live; font scale below refines text only.";
        private const string FontHeading = "Font";
        private const string PlexRadioLabel = "IBM Plex Sans";
        private const string ProggyRadioLabel = "ProggyClean (embedded)";
        private const string FontScaleSliderLabel = "Font scale";
        private const string FontRestartNote = "Font changes apply on next KSP start.";
        private const string VerboseToggleLabel = "Verbose logging";
        private const string VerboseHint = "Writes extra [DearImGuiKSP] diagnostics to KSP.log.";

        private readonly SettingsModel _settings;

        internal LibraryControlPanel(SettingsModel settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>Set by the toolbar button. False = steady-state closed: OnFrame early-returns.</summary>
        internal bool Visible { get; set; }

        // Per-frame callback, invoked by the frame loop through the fault barrier.
        internal void OnFrame()
        {
            if (!Visible)
            {
                return; // steady-state closed path: one bool check, zero allocation
            }

            using (var window = ImGuiEx.Window(WindowTitle))
            {
                if (!window.Visible)
                {
                    return;
                }

                DrawThemeSection();
                DrawUiScaleSection();
                DrawFontSection();
                DrawLoggingSection();
            }
        }

        private void DrawThemeSection()
        {
            ImGuiInternal.Text(ThemeHeading);
            if (ImGuiInternal.RadioButton(
                KspRadioLabel,
                string.Equals(_settings.Theme, LibraryConfig.DefaultTheme, StringComparison.Ordinal)))
            {
                _settings.Theme = LibraryConfig.DefaultTheme; // live: ThemeEngine re-applies next frame start
            }
            if (ImGuiInternal.RadioButton(
                DarkRadioLabel,
                string.Equals(_settings.Theme, LibraryConfig.DarkThemeName, StringComparison.Ordinal)))
            {
                _settings.Theme = LibraryConfig.DarkThemeName;
            }
        }

        private void DrawUiScaleSection()
        {
            float uiScale = _settings.UiScale;
            if (ImGuiInternal.SliderFloat(UiScaleSliderLabel, ref uiScale, LibraryConfig.MinScale, LibraryConfig.MaxScale))
            {
                _settings.UiScale = uiScale; // persists; ThemeEngine forwards it to the native side live
            }
            ImGuiInternal.Text(UiScaleHint);
        }

        private void DrawFontSection()
        {
            ImGuiInternal.Text(FontHeading);
            if (ImGuiInternal.RadioButton(
                PlexRadioLabel,
                string.Equals(_settings.Font, LibraryConfig.DefaultFont, StringComparison.OrdinalIgnoreCase)))
            {
                _settings.Font = LibraryConfig.DefaultFont;
            }
            if (ImGuiInternal.RadioButton(
                ProggyRadioLabel,
                string.Equals(_settings.Font, LibraryConfig.EmbeddedFontName, StringComparison.OrdinalIgnoreCase)))
            {
                _settings.Font = LibraryConfig.EmbeddedFontName;
            }

            float fontScale = _settings.FontScale;
            if (ImGuiInternal.SliderFloat(FontScaleSliderLabel, ref fontScale, LibraryConfig.MinScale, LibraryConfig.MaxScale))
            {
                _settings.FontScale = fontScale;
            }
            ImGuiInternal.Text(FontRestartNote);
        }

        private void DrawLoggingSection()
        {
            bool verbose = _settings.VerboseLogging;
            if (ExtensionShimsNative.Toggle(VerboseToggleLabel, ref verbose))
            {
                _settings.VerboseLogging = verbose; // live: the logger reads the setting per line
            }
            ImGuiInternal.Text(VerboseHint);
        }
    }
}
