using System;
using DearImGuiKSP.Application.Interfaces;
using DearImGuiKSP.Interop;
using Color32 = UnityEngine.Color32;
using Vector2 = UnityEngine.Vector2;

namespace DearImGuiKSP.Application
{
    /// <summary>
    /// Applies theme presets to the global ImGui style (spec §5.1, D34). Every
    /// apply resets to the stock dark style first — both presets share that
    /// base, so slots a preset does not map are never stale — then writes the
    /// preset's color and var overrides through the C8 native setters.
    /// Startup applies the configured theme once (DearImGuiKSPAddon.Start,
    /// after the font load); a settings change only sets a dirty flag and the
    /// frame loop re-applies at frame start, never mid-callback. Steady-state
    /// per-frame cost is one bool check; nothing here allocates per frame.
    /// Application-layer; no KSP types. Native calls route through Interop.
    /// </summary>
    internal sealed class ThemeEngine
    {
        private const float ByteToFloat = 1f / 255f;

        private readonly SettingsModel _settings;
        private readonly ILogger _log;
        private ThemePreset _active;
        private bool _dirty;

        internal ThemeEngine(SettingsModel settings, ILogger log)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _log = log;
            _settings.Changed += OnSettingsChanged;
        }

        /// <summary>Name of the currently applied preset ("ksp" or "dark").</summary>
        internal string CurrentThemeName => _active?.Name ?? LibraryConfig.DefaultTheme;

        /// <summary>The active preset; C9 reads its gradient parameters from here.</summary>
        internal ThemePreset ActivePreset => _active;

        /// <summary>
        /// Applies the configured theme immediately. Called once at startup,
        /// after the native context exists; style writes are legal any time.
        /// </summary>
        internal void ApplyCurrent()
        {
            // The model has no logger, so the spec §7 line for a rejected theme
            // name is logged here at the apply site (contract fallback): the
            // model hands over the raw name and we consume it exactly once.
            string rejected = _settings.ConsumeRejectedTheme();
            if (rejected != null)
            {
                _log?.Info("Unknown theme '" + rejected + "'; using 'ksp'.");
            }

            Apply(ThemePresets.ForName(_settings.Theme));
        }

        /// <summary>
        /// Deferred apply driven by the frame loop at frame start: no-op unless
        /// a theme change dirtied the engine since the last apply.
        /// </summary>
        internal void ApplyIfDirty()
        {
            if (!_dirty)
            {
                return;
            }
            _dirty = false;
            ApplyCurrent();
        }

        private void Apply(ThemePreset preset)
        {
            ImGuiInternal.StyleColorsDark();

            (ImGuiCol Col, Color32 Color)[] colors = preset.Colors;
            for (int i = 0; i < colors.Length; i++)
            {
                Color32 c = colors[i].Color;
                ImGuiInternal.SetStyleColor(
                    (int)colors[i].Col,
                    c.r * ByteToFloat,
                    c.g * ByteToFloat,
                    c.b * ByteToFloat,
                    c.a * ByteToFloat);
            }

            (ImGuiStyleVar Var, float Value)[] floatVars = preset.FloatVars;
            for (int i = 0; i < floatVars.Length; i++)
            {
                ImGuiInternal.SetStyleVarFloat((int)floatVars[i].Var, floatVars[i].Value);
            }

            (ImGuiStyleVar Var, Vector2 Value)[] vec2Vars = preset.Vec2Vars;
            for (int i = 0; i < vec2Vars.Length; i++)
            {
                Vector2 v = vec2Vars[i].Value;
                ImGuiInternal.SetStyleVarVec2((int)vec2Vars[i].Var, v.x, v.y);
            }

            _active = preset;
        }

        private void OnSettingsChanged()
        {
            if (!string.Equals(_settings.Theme, CurrentThemeName, StringComparison.Ordinal))
            {
                _dirty = true;
            }
        }
    }
}
