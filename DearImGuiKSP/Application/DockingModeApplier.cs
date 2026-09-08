using System;
using DearImGuiKSP.Application.Interfaces;
using DearImGuiKSP.Interop;

namespace DearImGuiKSP.Application
{
    /// <summary>
    /// Applies the persisted <c>docking</c> setting to the native context
    /// (ISSUES #011): a settings change only sets a dirty flag, and the frame
    /// loop forwards the new value through
    /// <c>DearImGuiKSPNative_SetDockingEnabled</c> at frame start — never
    /// mid-callback (the ThemeEngine OnSettingsChanged/ApplyIfDirty pattern).
    /// ThemeEngine keeps its style-only scope (SRP); this class owns the
    /// docking flag alone. Steady-state per-frame cost is one bool check;
    /// nothing here allocates per frame.
    /// A native failure is non-fatal (logged, spec §5.4): it never trips the
    /// lifecycle Failed state — a missing context means nothing here can run
    /// anyway. Application-layer; no KSP types. Native calls route through Interop.
    /// </summary>
    internal sealed class DockingModeApplier
    {
        private readonly SettingsModel _settings;
        private readonly ILogger _log;
        private bool _dirty;

        // The docking value the last apply forwarded to the native side.
        // Initialized to the default: the startup apply (DearImGuiKSPAddon.Start)
        // runs before anything can mutate the setting, so treating "not yet
        // applied" as the default is exact — and an unrelated settings change
        // must not arm a re-apply.
        private bool _appliedDocking = LibraryConfig.DefaultDocking;

        internal DockingModeApplier(SettingsModel settings, ILogger log)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _log = log;
            _settings.Changed += OnSettingsChanged;
        }

        /// <summary>
        /// Applies the configured docking value immediately. Called once at
        /// startup (DearImGuiKSPAddon.Start, after the native context exists);
        /// style writes are legal any time, and a persisted
        /// <c>docking=false</c> must take effect before frames run.
        /// </summary>
        internal void ApplyCurrent()
        {
            _dirty = false;
            Apply(_settings.Docking);
        }

        /// <summary>
        /// Deferred apply driven by the frame loop at frame start: no-op unless
        /// a docking change dirtied the applier since the last apply.
        /// </summary>
        internal void ApplyIfDirty()
        {
            if (!_dirty)
            {
                return;
            }
            _dirty = false;
            Apply(_settings.Docking);
        }

        /// <summary>
        /// True when a docking change has armed the deferred apply. Test
        /// observability for the dirty-flag logic: <see cref="ApplyIfDirty"/>
        /// itself reaches native code and is proven by the native harness.
        /// </summary>
        internal bool HasPendingApply => _dirty;

        private void Apply(bool enabled)
        {
            int code = ImGuiInternal.SetDockingEnabled(enabled ? 1 : 0);
            if (code != 0)
            {
                _log?.Warn("SetDockingEnabled(" + (enabled ? 1 : 0) + ") failed with code " + code + "; docking preference was not applied (non-fatal).");
            }
            _appliedDocking = enabled;
        }

        private void OnSettingsChanged()
        {
            // Only a docking change arms the re-apply; other settings (theme,
            // uiScale, font, ...) do not touch the native docking flag.
            if (_settings.Docking != _appliedDocking)
            {
                _dirty = true;
            }
        }
    }
}
