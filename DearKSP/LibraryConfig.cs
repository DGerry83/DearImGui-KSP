namespace DearKSP
{
    /// <summary>
    /// Compile-time constants and setting defaults for Dear KSP.
    /// Single source of truth — no magic strings elsewhere (CORE_PROTOCOLS §5.7).
    /// </summary>
    internal static class LibraryConfig
    {
        /// <summary>Assembly/dependency name consumers declare.</summary>
        internal const string ModName = "DearKSP";

        /// <summary>Log prefix for every library log line (spec §7.1, DK_LogPrefix).</summary>
        internal const string LogPrefix = "[DearKSP]";

        /// <summary>Path of the settings ConfigNode, relative to the KSP root.</summary>
        internal const string SettingsPath = "GameData/DearKSP/settings.cfg";

        /// <summary>Native DLL file name loaded by NativeBridge.</summary>
        internal const string NativeDllName = "DearKSPNative.dll";

        /// <summary>Consecutive throwing frames before a consumer is auto-disabled (spec §5.3).</summary>
        internal const int ConsumerFailureThreshold = 5;

        // Setting defaults (spec §9.1)
        internal const float DefaultUiScale = 1.0f;
        internal const float DefaultFontScale = 1.0f;
        internal const string DefaultTheme = "dark";
        internal const float MinScale = 0.5f;
        internal const float MaxScale = 2.0f;
    }
}
