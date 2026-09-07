namespace DearImGuiKSP
{
    /// <summary>
    /// Compile-time constants and setting defaults for DearImGui-KSP.
    /// Single source of truth — no magic strings elsewhere (CORE_PROTOCOLS §5.7).
    /// </summary>
    internal static class LibraryConfig
    {
        /// <summary>Assembly/dependency name consumers declare.</summary>
        internal const string ModName = "DearImGuiKSP";

        /// <summary>Log prefix for every library log line (spec §7.1, DK_LogPrefix).</summary>
        internal const string LogPrefix = "[DearImGuiKSP]";

        /// <summary>Path of the settings ConfigNode, relative to the KSP root.</summary>
        internal const string SettingsPath = "GameData/DearImGuiKSP/settings.cfg";

        /// <summary>Native DLL file name loaded by NativeBridge.</summary>
        internal const string NativeDllName = "DearImGuiKSPNative.dll";

        /// <summary>Directory NativeBridge loads the native DLL from, relative to the KSP root.</summary>
        internal const string NativePluginDataDir = "GameData/DearImGuiKSP/PluginData";

        /// <summary>Library control-panel toolbar icon as a GameDatabase URL (no extension), relative to GameData.</summary>
        internal const string ToolbarIconUrl = "DearImGuiKSP/Textures/toolbar";

        /// <summary>On-disk toolbar icon path, relative to the KSP root (existence probe before the GameDatabase lookup).</summary>
        internal const string ToolbarIconPath = "GameData/DearImGuiKSP/Textures/toolbar.png";

        /// <summary>Consecutive throwing frames before a consumer is auto-disabled (spec §5.3).</summary>
        internal const int ConsumerFailureThreshold = 5;

        // Setting defaults (spec §9.1)
        internal const float DefaultUiScale = 1.0f;
        internal const float DefaultFontScale = 1.0f;
        /// <summary>Default theme preset (D25): "ksp"; the stock ImGui dark stays available as <see cref="DarkThemeName"/>.</summary>
        internal const string DefaultTheme = "ksp";

        /// <summary>The retained stock-ImGui-dark preset name (spec §5.1, D25).</summary>
        internal const string DarkThemeName = "dark";
        internal const string DefaultFont = "IBMPlexSans";
        internal const string EmbeddedFontName = "ProggyClean";
        internal const string FontsDir = "GameData/DearImGuiKSP/Fonts";
        internal const bool DefaultVerboseLogging = false;
        internal const bool DefaultEnabled = true;
        internal const bool DefaultClampWindowsToViewport = true;
        internal const float MinScale = 0.5f;
        internal const float MaxScale = 2.0f;

        // Settings persistence (spec §4.4, §10.5)
        internal const int SettingsFormatVersion = 1;

        /// <summary>
        /// Quiet period before a pending settings change is written to disk
        /// (C06 debounce): a slider drag rewrites settings.cfg once after the
        /// changes settle, not once per changed frame.
        /// </summary>
        internal const float SettingsSaveDebounceSeconds = 0.5f;
    }
}
