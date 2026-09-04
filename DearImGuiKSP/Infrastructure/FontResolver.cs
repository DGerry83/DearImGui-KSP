using System;
using System.IO;

namespace DearImGuiKSP.Infrastructure
{
    /// <summary>
    /// Result of resolving the normalized <c>font</c> setting + <c>fontScale</c> to concrete
    /// font files (spec §5.2, §9). <see cref="UseEmbeddedDefault"/> is the fallback signal:
    /// the consumer loads the embedded default and logs. Paths are absolute; null when unused.
    /// </summary>
    internal sealed class FontResolution
    {
        internal bool UseEmbeddedDefault;
        internal string PrimaryPath;
        internal string SecondaryPath;
        internal float SizePixels;
    }

    /// <summary>
    /// Maps the normalized <c>font</c> setting to TTF files under <see cref="LibraryConfig.FontsDir"/>
    /// (spec §5.2, §9). Pure resolution only: no logging, no native calls. KSP-aware
    /// (Infrastructure) via <c>KSPUtil.ApplicationRootPath</c>. Consumed by C5's startup wiring.
    /// </summary>
    internal static class FontResolver
    {
        /// <summary>Base font size in pixels before <c>fontScale</c> is applied (tunable). 18 px = the KSP-comfortable default confirmed in-game at the M2 gate (15 px read small).</summary>
        internal const float BaseSizePixels = 18f;

        private const string PlexPrimaryFile = "IBMPlexSans-Regular.ttf";
        private const string PlexSecondaryFile = "IBMPlexSans-Medium.ttf";
        private const string TtfExtension = ".ttf";

        internal static FontResolution Resolve(string font, float fontScale)
        {
            bool isPlex = string.Equals(font, LibraryConfig.DefaultFont, StringComparison.OrdinalIgnoreCase);

            if (!isPlex && string.Equals(font, LibraryConfig.EmbeddedFontName, StringComparison.OrdinalIgnoreCase))
            {
                return EmbeddedDefault(fontScale);
            }

            string fileName = isPlex ? PlexPrimaryFile : EnsureTtfExtension(font);

            string primaryPath = ResolveExistingPath(fileName);
            if (primaryPath == null)
            {
                return EmbeddedDefault(fontScale);
            }

            string secondaryPath = isPlex ? ResolveExistingPath(PlexSecondaryFile) : null;

            return new FontResolution
            {
                UseEmbeddedDefault = false,
                PrimaryPath = primaryPath,
                SecondaryPath = secondaryPath,
                SizePixels = BaseSizePixels * fontScale,
            };
        }

        private static FontResolution EmbeddedDefault(float fontScale) =>
            new FontResolution { UseEmbeddedDefault = true, SizePixels = BaseSizePixels * fontScale };

        private static string ResolveExistingPath(string fileName)
        {
            string path = Path.Combine(KSPUtil.ApplicationRootPath, LibraryConfig.FontsDir, fileName);
            try
            {
                // Length probe, not an open stream: no handle retained (§5.9 Check 3).
                var info = new FileInfo(path);
                if (info.Exists && info.Length > 0)
                {
                    return path;
                }
            }
            catch (Exception)
            {
                // Unreadable or invalid path — treated as missing; the consumer logs the fallback.
            }

            return null;
        }

        private static string EnsureTtfExtension(string font) =>
            Path.HasExtension(font) ? font : font + TtfExtension;
    }
}
