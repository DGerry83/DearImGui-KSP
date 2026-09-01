using DearImGuiKSP.Application;

namespace DearImGuiKSP.Application.Interfaces
{
    /// <summary>
    /// Loads and saves LibrarySettings as a KSP ConfigNode (spec §4.4).
    /// Implemented by Infrastructure.SettingsStore. Thin repository per D18.
    /// </summary>
    internal interface ISettingsStore
    {
        /// <summary>
        /// Loads settings from disk. Tolerant: missing or unreadable files return defaults
        /// without writing; unknown keys are ignored; unparseable values fall back to defaults.
        /// </summary>
        LibrarySettings Load();

        /// <summary>
        /// Writes the complete settings node with the current format version.
        /// </summary>
        void Save(LibrarySettings settings);
    }
}
