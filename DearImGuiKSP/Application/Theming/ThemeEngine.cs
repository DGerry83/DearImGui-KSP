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
    /// after the font load); a theme or uiScale change only sets a dirty flag
    /// and the frame loop re-applies at frame start, never mid-callback.
    /// Steady-state per-frame cost is one bool check; nothing here allocates
    /// per frame.
    /// Application-layer; no KSP types. Native calls route through Interop.
    /// </summary>
    internal sealed class ThemeEngine
    {
        private const float ByteToFloat = 1f / 255f;

        private readonly SettingsModel _settings;
        private readonly ILogger _log;
        private ThemePreset _active;
        private bool _dirty;

        // The uiScale value the last apply forwarded to the native side.
        // Initialized to the default: the startup apply (DearImGuiKSPAddon.Start)
        // runs before anything can mutate the setting, so treating "not yet
        // applied" as the default is exact — and an unrelated settings change
        // must not arm the re-apply.
        private float _appliedUiScale = LibraryConfig.DefaultUiScale;

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
        /// a theme or uiScale change dirtied the engine since the last apply.
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

        /// <summary>
        /// True when a theme or uiScale change has armed the deferred re-apply.
        /// Test observability for the dirty-flag logic: <see cref="ApplyIfDirty"/>
        /// itself reaches native code and is exercised by the native harness.
        /// </summary>
        internal bool HasPendingApply => _dirty;

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

            // C9: two-stop vertical window-background gradient (spec §6.1).
            // The ksp preset supplies its stops; "dark" disables the native
            // EndFrame shading pass so it stays byte-exact stock. The return
            // code is ignored like the style setters above: a missing context
            // means nothing here can run anyway.
            bool gradient = !string.Equals(preset.Name, LibraryConfig.DarkThemeName, StringComparison.Ordinal);
            Color32 wbgTop = preset.WindowBgGradientTop;
            Color32 wbgBottom = preset.WindowBgGradientBottom;
            ImGuiInternal.SetWindowBgGradient(
                gradient ? 1 : 0,
                wbgTop.r * ByteToFloat,
                wbgTop.g * ByteToFloat,
                wbgTop.b * ByteToFloat,
                wbgTop.a * ByteToFloat,
                wbgBottom.r * ByteToFloat,
                wbgBottom.g * ByteToFloat,
                wbgBottom.b * ByteToFloat,
                wbgBottom.a * ByteToFloat);

            _active = preset;
            _appliedUiScale = ApplyUiScale();
        }

        // Live uiScale (C31): applied LAST, after the whole-style reset at the
        // top of Apply — ScaleAllSizes then always multiplies the default
        // sizes, so repeated applies (and the ksp<->dark switch) cannot
        // compound. FontGlobalScale is set absolutely by the same native call;
        // it scales rendered glyph size on top of the loaded font size, so the
        // startup font pipeline (18 px base) is unaffected by a 1.0 scale.
        private float ApplyUiScale()
        {
            ImGuiInternal.SetUiScale(_settings.UiScale);
            return _settings.UiScale;
        }

        private void OnSettingsChanged()
        {
            // C31: a uiScale change needs the same deferred re-apply as a theme
            // change (the apply path forwards the current scale to the native
            // side). Other settings (font, fontScale, verboseLogging, ...) do
            // not touch the live style.
            if (!string.Equals(_settings.Theme, CurrentThemeName, StringComparison.Ordinal) ||
                _settings.UiScale != _appliedUiScale)
            {
                _dirty = true;
            }
        }
    }
}
