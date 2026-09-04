using DearImGuiKSP.Interop;

namespace DearImGuiKSP
{
    /// <summary>
    /// Visual styles for <see cref="DearImGuiKSP.Knob"/>; values are the raw
    /// bitmasks of <c>ImGuiKnobVariant_</c> in the vendored altschuler/imgui-knobs
    /// <c>imgui-knobs.h</c> (pinned in
    /// <c>DearImGuiKSPNative/vendor/PIN_RECORD.md</c>). Only the variants are
    /// exposed; the color_set theming struct stays extension-internal.
    /// </summary>
    public enum KnobVariant
    {
        /// <summary>A tick hand on a filled circle (imgui-knobs.h: ImGuiKnobVariant_Tick = 1 &lt;&lt; 0).</summary>
        Tick = 1 << 0,

        /// <summary>A dot hand on a filled circle (imgui-knobs.h: ImGuiKnobVariant_Dot = 1 &lt;&lt; 1).</summary>
        Dot = 1 << 1,

        /// <summary>A filled circle with a wiper arc showing the value (imgui-knobs.h: ImGuiKnobVariant_Wiper = 1 &lt;&lt; 2).</summary>
        Wiper = 1 << 2,

        /// <summary>A wiper arc only, no circle (imgui-knobs.h: ImGuiKnobVariant_WiperOnly = 1 &lt;&lt; 3).</summary>
        WiperOnly = 1 << 3,

        /// <summary>A wiper arc with a dot at the value position (imgui-knobs.h: ImGuiKnobVariant_WiperDot = 1 &lt;&lt; 4).</summary>
        WiperDot = 1 << 4,

        /// <summary>Stepped ticks around a filled circle with a dot hand; uses the <c>steps</c> argument (imgui-knobs.h: ImGuiKnobVariant_Stepped = 1 &lt;&lt; 5).</summary>
        Stepped = 1 << 5,

        /// <summary>A space-opera style stack of arcs around a small core (imgui-knobs.h: ImGuiKnobVariant_Space = 1 &lt;&lt; 6).</summary>
        Space = 1 << 6,
    }

    /// <summary>
    /// Behavior flags for <see cref="DearImGuiKSP.Knob"/>; values are the raw
    /// bitmasks of <c>ImGuiKnobFlags_</c> in the vendored altschuler/imgui-knobs
    /// <c>imgui-knobs.h</c> (pinned in
    /// <c>DearImGuiKSPNative/vendor/PIN_RECORD.md</c>).
    /// </summary>
    [System.Flags]
    public enum KnobFlags
    {
        /// <summary>No flags; title, inline input, and vertical/horizontal drag as defaulted (imgui-knobs.h: ImGuiKnobFlags_None = 0, the 0 default).</summary>
        None = 0,

        /// <summary>Hides the label title above the knob (imgui-knobs.h: ImGuiKnobFlags_NoTitle = 1 &lt;&lt; 0).</summary>
        NoTitle = 1 << 0,

        /// <summary>Hides the inline drag-scalar input under the knob (imgui-knobs.h: ImGuiKnobFlags_NoInput = 1 &lt;&lt; 1).</summary>
        NoInput = 1 << 1,

        /// <summary>Shows a tooltip with the formatted value while hovered or active (imgui-knobs.h: ImGuiKnobFlags_ValueTooltip = 1 &lt;&lt; 2).</summary>
        ValueTooltip = 1 << 2,

        /// <summary>Drags horizontally only (imgui-knobs.h: ImGuiKnobFlags_DragHorizontal = 1 &lt;&lt; 3).</summary>
        DragHorizontal = 1 << 3,

        /// <summary>Drags vertically only (imgui-knobs.h: ImGuiKnobFlags_DragVertical = 1 &lt;&lt; 4).</summary>
        DragVertical = 1 << 4,

        /// <summary>Logarithmic drag scale; requires a range not spanning zero (imgui-knobs.h: ImGuiKnobFlags_Logarithmic = 1 &lt;&lt; 5).</summary>
        Logarithmic = 1 << 5,

        /// <summary>Always clamps the value to the range (imgui-knobs.h: ImGuiKnobFlags_AlwaysClamp = 1 &lt;&lt; 6).</summary>
        AlwaysClamp = 1 << 6,
    }

    public static partial class DearImGuiKSP
    {
        /// <summary>
        /// Draws a rotary knob (vendored altschuler/imgui-knobs) bound to a
        /// float. The default look is a Tick-variant knob sized to four lines
        /// of the current font (about 72 px at the 18 px base font, uiScale
        /// 1.0), with a "%.3f" inline input. Only valid inside a registered
        /// callback.
        /// </summary>
        /// <returns>
        /// True when the value changed this frame; <paramref name="value"/> is
        /// updated in place. False when unavailable.
        /// </returns>
        public static bool Knob(string label, ref float value, float min, float max)
        {
            if (!IsAvailable)
            {
                return false;
            }
            return ExtensionShimsNative.Knob(
                label, ref value, min, max, 0f, null, (int)KnobVariant.Tick, 0f, (int)KnobFlags.None, 10);
        }

        /// <summary>
        /// Draws a rotary knob (vendored altschuler/imgui-knobs) bound to a
        /// float, with full control. <paramref name="speed"/> is 0 for the
        /// upstream default ((max - min) / 250); <paramref name="size"/> is 0
        /// to size from the font (4 x line height). <paramref name="format"/>
        /// is a printf-style format (null for "%.3f"). Only valid inside a
        /// registered callback.
        /// </summary>
        /// <returns>
        /// True when the value changed this frame; <paramref name="value"/> is
        /// updated in place. False when unavailable.
        /// </returns>
        public static bool Knob(
            string label,
            ref float value,
            float min,
            float max,
            float speed,
            KnobVariant variant,
            float size,
            KnobFlags flags,
            int steps,
            string format = null)
        {
            if (!IsAvailable)
            {
                return false;
            }
            return ExtensionShimsNative.Knob(
                label, ref value, min, max, speed, format, (int)variant, size, (int)flags, steps);
        }

        /// <summary>
        /// Draws a rotary knob (vendored altschuler/imgui-knobs) bound to an
        /// int (the upstream <c>KnobInt</c> overload). Same defaults as the
        /// float simple overload, with a "%i" inline input. Only valid inside
        /// a registered callback.
        /// </summary>
        /// <returns>
        /// True when the value changed this frame; <paramref name="value"/> is
        /// updated in place. False when unavailable.
        /// </returns>
        public static bool Knob(string label, ref int value, int min, int max)
        {
            if (!IsAvailable)
            {
                return false;
            }
            return ExtensionShimsNative.KnobInt(
                label, ref value, min, max, 0f, null, (int)KnobVariant.Tick, 0f, (int)KnobFlags.None, 10);
        }

        /// <summary>
        /// Draws a rotary knob (vendored altschuler/imgui-knobs) bound to an
        /// int, with full control. <paramref name="format"/> is null for "%i";
        /// other arguments as per the float full overload. Only valid inside a
        /// registered callback.
        /// </summary>
        /// <returns>
        /// True when the value changed this frame; <paramref name="value"/> is
        /// updated in place. False when unavailable.
        /// </returns>
        public static bool Knob(
            string label,
            ref int value,
            int min,
            int max,
            float speed,
            KnobVariant variant,
            float size,
            KnobFlags flags,
            int steps,
            string format = null)
        {
            if (!IsAvailable)
            {
                return false;
            }
            return ExtensionShimsNative.KnobInt(
                label, ref value, min, max, speed, format, (int)variant, size, (int)flags, steps);
        }
    }
}
