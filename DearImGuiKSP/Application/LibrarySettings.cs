namespace DearImGuiKSP.Application
{
    /// <summary>
    /// Plain mutable settings record used by <see cref="ISettingsStore"/> and <see cref="SettingsModel"/>.
    /// Defaults come from <see cref="LibraryConfig"/> (spec §9.1).
    /// Application-layer; no UnityEngine/KSP types.
    /// </summary>
    internal sealed class LibrarySettings
    {
        internal float UiScale = LibraryConfig.DefaultUiScale;
        internal float FontScale = LibraryConfig.DefaultFontScale;
        internal string Theme = LibraryConfig.DefaultTheme;
        internal string Font = LibraryConfig.DefaultFont;
        internal bool VerboseLogging = LibraryConfig.DefaultVerboseLogging;
        internal bool Enabled = LibraryConfig.DefaultEnabled;
        internal bool ClampWindowsToViewport = LibraryConfig.DefaultClampWindowsToViewport;
        internal bool Docking = LibraryConfig.DefaultDocking;
    }
}
