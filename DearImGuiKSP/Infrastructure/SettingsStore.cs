using System;
using System.IO;
using DearImGuiKSP.Application;
using DearImGuiKSP.Application.Interfaces;
using ILogger = DearImGuiKSP.Application.Interfaces.ILogger;

namespace DearImGuiKSP.Infrastructure
{
    /// <summary>
    /// Implements <see cref="ISettingsStore"/> over KSP ConfigNode.
    /// Read at startup, written on change-settle (debounced by SettingsModel, C06);
    /// migrates older formatVersions forward (spec §4.4, §10.5). Writes are atomic:
    /// the new content goes to a temp sibling first, then replaces the real file.
    /// </summary>
    internal sealed class SettingsStore : ISettingsStore
    {
        private const string RootNodeName = "DEARIMGUIKSP_SETTINGS";
        private const string FormatVersionKey = "formatVersion";
        private const string UiScaleKey = "uiScale";
        private const string FontScaleKey = "fontScale";
        private const string ThemeKey = "theme";
        private const string FontKey = "font";
        private const string VerboseLoggingKey = "verboseLogging";
        private const string EnabledKey = "enabled";
        private const string ClampWindowsToViewportKey = "clampWindowsToViewport";

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

            // ConfigNode.Load wraps the file in a synthetic node named "root"; the real
            // settings node is its child (KSP Knowledge Library, ConfigNode.cs:8005-8011).
            ConfigNode node = root.GetNode(RootNodeName);
            if (node == null)
            {
                // Tolerate a file that IS the settings node (no wrapper).
                node = string.Equals(root.name, RootNodeName, StringComparison.OrdinalIgnoreCase) ? root : null;
            }
            if (node == null)
            {
                _logger.Warn($"Settings file has no {RootNodeName} node; using defaults. Path: {path}");
                return defaults;
            }

            var settings = new LibrarySettings();
            int formatVersion = LibraryConfig.SettingsFormatVersion;

            if (node.HasValue(FormatVersionKey))
            {
                string rawFormat = node.GetValue(FormatVersionKey);
                if (!int.TryParse(rawFormat, out formatVersion))
                {
                    formatVersion = LibraryConfig.SettingsFormatVersion;
                }
            }

            settings.UiScale = ReadFloat(node, UiScaleKey, defaults.UiScale);
            settings.FontScale = ReadFloat(node, FontScaleKey, defaults.FontScale);
            settings.Theme = ReadString(node, ThemeKey, defaults.Theme);
            settings.Font = ReadString(node, FontKey, defaults.Font);
            settings.VerboseLogging = ReadBool(node, VerboseLoggingKey, defaults.VerboseLogging);
            settings.Enabled = ReadBool(node, EnabledKey, defaults.Enabled);
            settings.ClampWindowsToViewport = ReadBool(node, ClampWindowsToViewportKey, defaults.ClampWindowsToViewport);

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
            string tempPath = path + ".tmp";

            try
            {
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // ConfigNode.Save writes only the node's values/children, not its own
                // name+braces (ConfigNode.cs WriteRootNode) — so save a wrapper whose
                // child is the named settings node, matching the shipped file layout.
                var file = new ConfigNode("root");
                ConfigNode node = file.AddNode(RootNodeName);
                node.AddValue(FormatVersionKey, LibraryConfig.SettingsFormatVersion);
                node.AddValue(UiScaleKey, settings.UiScale);
                node.AddValue(FontScaleKey, settings.FontScale);
                node.AddValue(ThemeKey, settings.Theme);
                node.AddValue(FontKey, settings.Font);
                node.AddValue(VerboseLoggingKey, settings.VerboseLogging);
                node.AddValue(EnabledKey, settings.Enabled);
                node.AddValue(ClampWindowsToViewportKey, settings.ClampWindowsToViewport);

                // Atomic write (C06, G3-16): write the temp sibling first, then
                // replace the real file, so a crash or concurrent reader mid-write
                // never sees a truncated settings.cfg. File.Replace needs the
                // destination to exist; the first-ever save moves instead.
                file.Save(tempPath);
                if (File.Exists(path))
                {
                    File.Replace(tempPath, path, null);
                }
                else
                {
                    File.Move(tempPath, path);
                }
            }
            catch (Exception ex)
            {
                try
                {
                    if (File.Exists(tempPath))
                    {
                        File.Delete(tempPath);
                    }
                }
                catch (Exception)
                {
                    // Best-effort cleanup only; the save failure below is the real signal.
                }
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
