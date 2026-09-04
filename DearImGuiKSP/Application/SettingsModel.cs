using System;
using System.Collections.Generic;
using DearImGuiKSP.Application.Interfaces;

namespace DearImGuiKSP.Application
{
    /// <summary>
    /// Holds the in-memory settings snapshot and raises change notifications (spec §9).
    /// Application-layer; no UnityEngine/KSP types.
    /// </summary>
    internal sealed class SettingsModel
    {
        private readonly ISettingsStore _store;
        private float _uiScale;
        private float _fontScale;
        private string _theme;
        private string _font;
        private bool _verboseLogging;
        private bool _enabled;
        private bool _clampWindowsToViewport;

        internal SettingsModel(ISettingsStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));

            LibrarySettings loaded = _store.Load();
            _uiScale = ClampScale(loaded.UiScale);
            _fontScale = ClampScale(loaded.FontScale);
            _theme = NormalizeTheme(loaded.Theme);
            _font = NormalizeFont(loaded.Font);
            _verboseLogging = loaded.VerboseLogging;
            _enabled = loaded.Enabled;
            _clampWindowsToViewport = loaded.ClampWindowsToViewport;
        }

        /// <summary>Raised after any setting actually changes.</summary>
        internal event Action Changed;

        internal float UiScale
        {
            get => _uiScale;
            set
            {
                if (Set(ref _uiScale, ClampScale(value)))
                {
                    Persist();
                }
            }
        }

        internal float FontScale
        {
            get => _fontScale;
            set
            {
                if (Set(ref _fontScale, ClampScale(value)))
                {
                    Persist();
                }
            }
        }

        internal string Theme
        {
            get => _theme;
            set
            {
                if (Set(ref _theme, NormalizeTheme(value)))
                {
                    Persist();
                }
            }
        }

        internal string Font
        {
            get => _font;
            set
            {
                if (Set(ref _font, NormalizeFont(value)))
                {
                    Persist();
                }
            }
        }

        internal bool VerboseLogging
        {
            get => _verboseLogging;
            set
            {
                if (Set(ref _verboseLogging, value))
                {
                    Persist();
                }
            }
        }

        internal bool Enabled
        {
            get => _enabled;
            set
            {
                if (Set(ref _enabled, value))
                {
                    Persist();
                }
            }
        }

        internal bool ClampWindowsToViewport
        {
            get => _clampWindowsToViewport;
            set
            {
                if (Set(ref _clampWindowsToViewport, value))
                {
                    Persist();
                }
            }
        }

        private bool Set<T>(ref T field, T value)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            Changed?.Invoke();
            return true;
        }

        private void Persist()
        {
            _store.Save(new LibrarySettings
            {
                UiScale = _uiScale,
                FontScale = _fontScale,
                Theme = _theme,
                Font = _font,
                VerboseLogging = _verboseLogging,
                Enabled = _enabled,
                ClampWindowsToViewport = _clampWindowsToViewport,
            });
        }

        // The raw theme name most recently rejected by NormalizeTheme. The model
        // has no logger, so ThemeEngine consumes this at the apply site to log
        // the spec §7 line ("Unknown theme '<name>'; using 'ksp'."). Null when
        // the last normalization recognized the name (or it was empty).
        private string _rejectedTheme;

        /// <summary>
        /// Returns the raw theme name most recently rejected by normalization,
        /// once, then clears it — so the apply site logs exactly one line per
        /// unknown name (spec §7).
        /// </summary>
        internal string ConsumeRejectedTheme()
        {
            string rejected = _rejectedTheme;
            _rejectedTheme = null;
            return rejected;
        }

        private static float ClampScale(float value) =>
            Math.Max(LibraryConfig.MinScale, Math.Min(LibraryConfig.MaxScale, value));

        private string NormalizeTheme(string value)
        {
            string trimmed = value?.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                return LibraryConfig.DefaultTheme;
            }
            if (string.Equals(trimmed, LibraryConfig.DefaultTheme, StringComparison.OrdinalIgnoreCase))
            {
                return LibraryConfig.DefaultTheme;
            }
            if (string.Equals(trimmed, LibraryConfig.DarkThemeName, StringComparison.OrdinalIgnoreCase))
            {
                return LibraryConfig.DarkThemeName;
            }
            _rejectedTheme = trimmed;
            return LibraryConfig.DefaultTheme;
        }

        private static string NormalizeFont(string value)
        {
            string trimmed = value?.Trim();
            return string.IsNullOrEmpty(trimmed) ? LibraryConfig.DefaultFont : trimmed;
        }
    }
}
