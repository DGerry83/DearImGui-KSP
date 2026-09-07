using DearImGuiKSP.Interop;

namespace DearImGuiKSP
{
    /// <summary>
    /// Layout orientation for <see cref="DearImGuiKSP.Wheel"/>; values are the
    /// raw ints of <c>WheelOrientation_</c> in the vendored
    /// Engineer162/imgui-wheels <c>imgui-wheels.h</c> (pinned in
    /// <c>DearImGuiKSPNative/vendor/PIN_RECORD.md</c>). The widget rolls along
    /// its orientation axis.
    /// </summary>
    public enum WheelOrientation
    {
        /// <summary>The barrel rolls along X when dragged horizontally (imgui-wheels.h: WheelOrientation_::Horizontal = 0).</summary>
        Horizontal = 0,

        /// <summary>The barrel rolls along Y when dragged vertically (imgui-wheels.h: WheelOrientation_::Vertical = 1).</summary>
        Vertical = 1,
    }

    public static partial class DearImGuiKSP
    {
        /// <summary>
        /// Draws a barrel-shaped rolling wheel input (vendored
        /// Engineer162/imgui-wheels, upstream <c>WheelFloat</c>) bound to a
        /// float. The default look is a horizontal wheel 96 x 22 px (about
        /// right at the 18 px base font, uiScale 1.0), dragged with the
        /// upstream sensitivity curve and a "%.2f" value label. Only valid
        /// inside a registered callback.
        /// </summary>
        /// <returns>
        /// True when the value changed this frame; <paramref name="value"/> is
        /// updated in place. False when unavailable.
        /// </returns>
        public static bool Wheel(string label, ref float value, float min, float max)
        {
            if (!CanDeclareUi)
            {
                return false;
            }
            return ExtensionShimsNative.WheelFloat(
                label, ref value, min, max, 96f, 22f, (int)WheelOrientation.Horizontal, null, 1f, false);
        }

        /// <summary>
        /// Draws a rolling wheel (vendored Engineer162/imgui-wheels, upstream
        /// <c>WheelFloat</c>) bound to a float, with full control. The layout
        /// box is (<paramref name="sizeX"/>, <paramref name="sizeY"/>); pick an
        /// elongated box along the drag axis. <paramref name="speed"/> is the
        /// drag sensitivity multiplier (upstream default 1.0).
        /// <paramref name="format"/> is a printf-style format (null for
        /// "%.2f"). <paramref name="invertColors"/> swaps the wheel's color
        /// profile against the current theme. Only valid inside a registered
        /// callback.
        /// </summary>
        /// <returns>
        /// True when the value changed this frame; <paramref name="value"/> is
        /// updated in place. False when unavailable.
        /// </returns>
        public static bool Wheel(
            string label,
            ref float value,
            float min,
            float max,
            float sizeX,
            float sizeY,
            WheelOrientation orientation,
            float speed = 1f,
            string format = null,
            bool invertColors = false)
        {
            if (!CanDeclareUi)
            {
                return false;
            }
            return ExtensionShimsNative.WheelFloat(
                label, ref value, min, max, sizeX, sizeY, (int)orientation, format, speed, invertColors);
        }

        /// <summary>
        /// Draws a rolling wheel (vendored Engineer162/imgui-wheels, upstream
        /// <c>WheelInt</c>) bound to an int, with detent resistance between
        /// integer steps. Same defaults as the float simple overload, with a
        /// "%d" value label. Only valid inside a registered callback.
        /// </summary>
        /// <returns>
        /// True when the value changed this frame; <paramref name="value"/> is
        /// updated in place. False when unavailable.
        /// </returns>
        public static bool Wheel(string label, ref int value, int min, int max)
        {
            if (!CanDeclareUi)
            {
                return false;
            }
            return ExtensionShimsNative.WheelInt(
                label, ref value, min, max, 96f, 22f, (int)WheelOrientation.Horizontal, null, 1f, false);
        }

        /// <summary>
        /// Draws a rolling wheel (vendored Engineer162/imgui-wheels, upstream
        /// <c>WheelInt</c>) bound to an int, with full control. Arguments as
        /// per the float full overload; <paramref name="format"/> is null for
        /// "%d". Only valid inside a registered callback.
        /// </summary>
        /// <returns>
        /// True when the value changed this frame; <paramref name="value"/> is
        /// updated in place. False when unavailable.
        /// </returns>
        public static bool Wheel(
            string label,
            ref int value,
            int min,
            int max,
            float sizeX,
            float sizeY,
            WheelOrientation orientation,
            float speed = 1f,
            string format = null,
            bool invertColors = false)
        {
            if (!CanDeclareUi)
            {
                return false;
            }
            return ExtensionShimsNative.WheelInt(
                label, ref value, min, max, sizeX, sizeY, (int)orientation, format, speed, invertColors);
        }
    }
}
