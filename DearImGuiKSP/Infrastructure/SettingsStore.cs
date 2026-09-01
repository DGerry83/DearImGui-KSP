using System;
using System.IO;
using DearImGuiKSP.Application;
using DearImGuiKSP.Application.Interfaces;
using ILogger = DearImGuiKSP.Application.Interfaces.ILogger;

namespace DearImGuiKSP.Infrastructure
{
    /// <summary>
    /// Implements <see cref="ISettingsStore"/> over KSP ConfigNode.
    /// Read at startup, written on change; migrates older formatVersions forward (spec §4.4, §10.5).
    /// </summary>
    internal sealed class SettingsStore : ISettingsStore
    {
        private const string RootNodeName = "DEARIMGUIKSP_SETTINGS";
        private const string FormatVersionKey = "formatVersion";
        private const string UiScaleKey = "uiScale";
        private const string FontScaleKey = "fontScale";
        private const string ThemeKey = "theme";
        private const string VerboseLoggingKey = "verboseLogging";
        private const string EnabledKey = "enabled";

        private readonly ILogger _logger;

        internal SettingsStore(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public LibrarySettings Load()
        {
            string path = GetPath();
            LibrarySettings defaults = CreateDefaults();

            ConfigNode root;
            try
            {
                root = ConfigNode.Load(path);
            }
            catch (Exception ex)
            {
                _logger.Warn($"Settings file could not be read; using defaults. Path: {path}, error: {ex.Message}");
                return defaults;
            }

            if (root == null)
            {
                _logger.Warn($"Settings file not found; using defaults. Path: {path}");
                return defaults;
            }

            var settings = new LibrarySettings();
            int formatVersion = LibraryConfig.SettingsFormatVersion;

            if (root.HasValue(FormatVersionKey))
            {
                string rawFormat = root.GetValue(FormatVersionKey);
                if (!int.TryParse(rawFormat, out formatVersion))
                {
                    formatVersion = LibraryConfig.SettingsFormatVersion;
                }
            }

            settings.UiScale = ReadFloat(root, UiScaleKey, defaults.UiScale);
            settings.FontScale = ReadFloat(root, FontScaleKey, defaults.FontScale);
            settings.Theme = ReadString(root, ThemeKey, defaults.Theme);
            settings.VerboseLogging = ReadBool(root, VerboseLoggingKey, defaults.VerboseLogging);
            settings.Enabled = ReadBool(root, EnabledKey, defaults.Enabled);

            if (formatVersion != LibraryConfig.SettingsFormatVersion)
            {
                _logger.Info($"Migrating settings from format version {formatVersion} to {LibraryConfig.SettingsFormatVersion}.");
                Save(settings);
            }

            return settings;
        }

        public void Save(LibrarySettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            string path = GetPath();

            try
            {
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var root = new ConfigNode(RootNodeName);
                root.AddValue(FormatVersionKey, LibraryConfig.SettingsFormatVersion);
                root.AddValue(UiScaleKey, settings.UiScale);
                root.AddValue(FontScaleKey, settings.FontScale);
                root.AddValue(ThemeKey, settings.Theme);
                root.AddValue(VerboseLoggingKey, settings.VerboseLogging);
                root.AddValue(EnabledKey, settings.Enabled);
                root.Save(path);
            }
            catch (Exception ex)
            {
                _logger.Warn($"Failed to save settings. Path: {path}, error: {ex.Message}");
            }
        }

        private static string GetPath() =>
            Path.Combine(KSPUtil.ApplicationRootPath, LibraryConfig.SettingsPath);

        private static LibrarySettings CreateDefaults() => new LibrarySettings();

        private static float ReadFloat(ConfigNode node, string key, float defaultValue)
        {
            node.TryGetValue(key, ref defaultValue);
            return defaultValue;
        }

        private static bool ReadBool(ConfigNode node, string key, bool defaultValue)
        {
            node.TryGetValue(key, ref defaultValue);
            return defaultValue;
        }

        private static string ReadString(ConfigNode node, string key, string defaultValue)
        {
            string value = node.GetValue(key);
            return string.IsNullOrEmpty(value) ? defaultValue : value;
        }
    }
}
