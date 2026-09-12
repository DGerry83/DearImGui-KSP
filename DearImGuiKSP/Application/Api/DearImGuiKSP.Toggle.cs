using DearImGuiKSP.Interop;

namespace DearImGuiKSP
{
    /// <summary>
    /// Mode flags for <see cref="DearImGuiKSP.Toggle(string, ref bool, ToggleFlags)"/>;
    /// values are the raw bitmasks of <c>ImGuiToggleFlags_</c> in the vendored
    /// cmdwtf/imgui_toggle <c>imgui_toggle.h</c> (pinned in
    /// <c>DearImGuiKSPNative/vendor/PIN_RECORD.md</c>). Only the flags the shim
    /// forwards are exposed; the knob inset is a config-struct field upstream,
    /// not a flag, and config-struct overloads are out of scope (spec §4.1, D27).
    /// </summary>
    [System.Flags]
    public enum ToggleFlags
    {
        /// <summary>No mode flags; the plain toggle (imgui_toggle.h: ImGuiToggleFlags_None = 0).</summary>
        None = 0,

        /// <summary>Animates the knob between states (imgui_toggle.h: ImGuiToggleFlags_Animated = 1 &lt;&lt; 0).</summary>
        Animated = 1 << 0,

        /// <summary>Draws a border on the toggle frame (imgui_toggle.h: ImGuiToggleFlags_BorderedFrame = 1 &lt;&lt; 3).</summary>
        BorderedFrame = 1 << 3,

        /// <summary>Draws a border on the toggle knob (imgui_toggle.h: ImGuiToggleFlags_BorderedKnob = 1 &lt;&lt; 4).</summary>
        BorderedKnob = 1 << 4,

        /// <summary>Draws a shadow under the toggle frame (imgui_toggle.h: ImGuiToggleFlags_ShadowedFrame = 1 &lt;&lt; 5).</summary>
        ShadowedFrame = 1 << 5,

        /// <summary>Draws a shadow under the toggle knob (imgui_toggle.h: ImGuiToggleFlags_ShadowedKnob = 1 &lt;&lt; 6).</summary>
        ShadowedKnob = 1 << 6,

        /// <summary>Draws on/off glyphs indicating state (imgui_toggle.h: ImGuiToggleFlags_A11y = 1 &lt;&lt; 8).</summary>
        A11y = 1 << 8,

        /// <summary>Shorthand for <see cref="BorderedFrame"/> | <see cref="BorderedKnob"/> (imgui_toggle.h: ImGuiToggleFlags_Bordered).</summary>
        Bordered = BorderedFrame | BorderedKnob,

        /// <summary>Shorthand for <see cref="ShadowedFrame"/> | <see cref="ShadowedKnob"/> (imgui_toggle.h: ImGuiToggleFlags_Shadowed).</summary>
        Shadowed = ShadowedFrame | ShadowedKnob,
    }

    public static partial class DearImGuiKSP
    {
        /// <summary>
        /// Draws an animated toggle switch (vendored cmdwtf/imgui_toggle), the
        /// KSP theme's replacement for a checkbox. Only valid inside a registered
        /// callback.
        /// </summary>
        /// <returns>
        /// True when the value changed this frame; <paramref name="value"/> is
        /// updated in place. False when unavailable.
        /// </returns>
        public static bool Toggle(string label, ref bool value)
        {
            if (!CanDeclareUi)
            {
                return false;
            }
            RowItemHook();
            return ExtensionShimsNative.Toggle(label, ref value);
        }

        /// <summary>
        /// Draws a toggle switch (vendored cmdwtf/imgui_toggle) with mode flags.
        /// Combine <see cref="ToggleFlags"/> values, e.g.
        /// <c>ToggleFlags.Animated | ToggleFlags.Bordered</c>. Only valid inside a
        /// registered callback.
        /// </summary>
        /// <returns>
        /// True when the value changed this frame; <paramref name="value"/> is
        /// updated in place. False when unavailable.
        /// </returns>
        public static bool Toggle(string label, ref bool value, ToggleFlags flags)
        {
            if (!CanDeclareUi)
            {
                return false;
            }
            RowItemHook();
            return ExtensionShimsNative.Toggle(label, ref value, (int)flags);
        }
    }
}
