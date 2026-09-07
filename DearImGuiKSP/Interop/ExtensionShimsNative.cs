using System;
using System.Runtime.InteropServices;
using System.Text;

namespace DearImGuiKSP.Interop
{
    /// <summary>
    /// Raw + safe P/Invoke surface for the hand-written native extension shims
    /// (chunk C10: imgui_toggle; chunk C15: imgui-knobs + imgui-wheels). Same seam as
    /// <see cref="ImGuiNative"/>: implicit <c>[DllImport("DearImGuiKSPNative")]</c>
    /// resolves against the module NativeBridge already LoadLibrary'd. The shim
    /// ABI uses <c>int</c> for bools (1-byte C++ bools stay behind the shim) and
    /// returns 1 when the value changed, 0 otherwise. ID-bearing labels route
    /// through <see cref="ImGuiInternal.ToIdUtf8"/> (C14, G3-20 closure: the
    /// docs/10 §6 empty-label guard applies to these widgets too); only the
    /// printf <c>format</c> strings — display text, never an ID — keep this
    /// file's local <c>ToUtf8</c>/<c>ToUtf8OrNull</c>.
    /// </summary>
    internal static class ExtensionShimsNative
    {
        private const string Dll = "DearImGuiKSPNative";

        // int DK_Toggle(const char* label, int* value); (src/shims/imgui_toggle_shim.h)
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern int DK_Toggle([In] byte[] label, ref int value);

        // int DK_ToggleFlags(const char* label, int* value, int flags); (src/shims/imgui_toggle_shim.h)
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern int DK_ToggleFlags([In] byte[] label, ref int value, int flags);

        // int DK_Knob(const char* label, float* value, float v_min, float v_max, float speed,
        //             const char* format, int variant, float size, int flags, int steps);
        // (src/shims/imgui_knobs_shim.h:20)
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern int DK_Knob(
            [In] byte[] label,
            ref float value,
            float v_min,
            float v_max,
            float speed,
            [In] byte[] format,
            int variant,
            float size,
            int flags,
            int steps);

        // int DK_KnobInt(const char* label, int* value, int v_min, int v_max, float speed,
        //                const char* format, int variant, float size, int flags, int steps);
        // (src/shims/imgui_knobs_shim.h:34)
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern int DK_KnobInt(
            [In] byte[] label,
            ref int value,
            int v_min,
            int v_max,
            float speed,
            [In] byte[] format,
            int variant,
            float size,
            int flags,
            int steps);

        // int DK_WheelFloat(const char* label, float* value, float v_min, float v_max,
        //                   float size_x, float size_y, int orientation, const char* format,
        //                   float speed, int invert_colors); (src/shims/imgui_wheels_shim.h:20)
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern int DK_WheelFloat(
            [In] byte[] label,
            ref float value,
            float v_min,
            float v_max,
            float size_x,
            float size_y,
            int orientation,
            [In] byte[] format,
            float speed,
            int invert_colors);

        // int DK_WheelInt(const char* label, int* value, int v_min, int v_max,
        //                 float size_x, float size_y, int orientation, const char* format,
        //                 float speed, int invert_colors); (src/shims/imgui_wheels_shim.h:34)
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern int DK_WheelInt(
            [In] byte[] label,
            ref int value,
            int v_min,
            int v_max,
            float size_x,
            float size_y,
            int orientation,
            [In] byte[] format,
            float speed,
            int invert_colors);

        /// <summary>
        /// Draws imgui_toggle's default toggle. The label is encoded per call
        /// through <see cref="ImGuiInternal.ToIdUtf8"/> (ID-bearing: null/empty
        /// gets the invisible sentinel instead of the window's own ID); toggle
        /// labels are short-lived and the buffer is not retained.
        /// </summary>
        /// <returns>True when <paramref name="value"/> changed this frame.</returns>
        internal static bool Toggle(string label, ref bool value)
        {
            int v = value ? 1 : 0;
            bool changed = DK_Toggle(ImGuiInternal.ToIdUtf8(label), ref v) != 0;
            value = v != 0;
            return changed;
        }

        /// <summary>
        /// Draws imgui_toggle's toggle with mode flags (subset of
        /// <c>ImGuiToggleFlags_</c>, imgui_toggle.h). <paramref name="flags"/>
        /// is the raw int bitmask of the public <c>DearImGuiKSP.ToggleFlags</c>.
        /// </summary>
        /// <returns>True when <paramref name="value"/> changed this frame.</returns>
        internal static bool Toggle(string label, ref bool value, int flags)
        {
            int v = value ? 1 : 0;
            bool changed = DK_ToggleFlags(ImGuiInternal.ToIdUtf8(label), ref v, flags) != 0;
            value = v != 0;
            return changed;
        }

        /// <summary>
        /// Draws ImGuiKnobs::Knob (float). <paramref name="format"/> is null for
        /// the widget default ("%.3f"), else encoded to a null-terminated UTF-8
        /// buffer per call; knob labels/formats are short-lived and the buffers
        /// are not retained.
        /// </summary>
        /// <returns>True when <paramref name="value"/> changed this frame.</returns>
        internal static bool Knob(
            string label,
            ref float value,
            float v_min,
            float v_max,
            float speed,
            string format,
            int variant,
            float size,
            int flags,
            int steps)
        {
            return DK_Knob(
                ImGuiInternal.ToIdUtf8(label),
                ref value,
                v_min,
                v_max,
                speed,
                ToUtf8OrNull(format),
                variant,
                size,
                flags,
                steps) != 0;
        }

        /// <summary>
        /// Draws ImGuiKnobs::KnobInt. <paramref name="format"/> is null for the
        /// widget default ("%i"); encoding as per <see cref="Knob"/>.
        /// </summary>
        /// <returns>True when <paramref name="value"/> changed this frame.</returns>
        internal static bool KnobInt(
            string label,
            ref int value,
            int v_min,
            int v_max,
            float speed,
            string format,
            int variant,
            float size,
            int flags,
            int steps)
        {
            return DK_KnobInt(
                ImGuiInternal.ToIdUtf8(label),
                ref value,
                v_min,
                v_max,
                speed,
                ToUtf8OrNull(format),
                variant,
                size,
                flags,
                steps) != 0;
        }

        /// <summary>
        /// Draws ImGuiWheels::WheelFloat (the vendored Engineer162/imgui-wheels
        /// barrel wheel). The ImVec2 layout box crosses the ABI as
        /// (<paramref name="size_x"/>, <paramref name="size_y"/>).
        /// <paramref name="orientation"/> is the raw int of the vendored
        /// WheelOrientation_ enum (0 = horizontal, 1 = vertical).
        /// <paramref name="format"/> is null for the widget default ("%.2f");
        /// encoding as per <see cref="Knob"/>.
        /// </summary>
        /// <returns>True when <paramref name="value"/> changed this frame.</returns>
        internal static bool WheelFloat(
            string label,
            ref float value,
            float v_min,
            float v_max,
            float size_x,
            float size_y,
            int orientation,
            string format,
            float speed,
            bool invertColors)
        {
            return DK_WheelFloat(
                ImGuiInternal.ToIdUtf8(label),
                ref value,
                v_min,
                v_max,
                size_x,
                size_y,
                orientation,
                ToUtf8OrNull(format),
                speed,
                invertColors ? 1 : 0) != 0;
        }

        /// <summary>
        /// Draws ImGuiWheels::WheelInt; ABI and encoding as per
        /// <see cref="WheelFloat"/> (widget-default format "%d").
        /// </summary>
        /// <returns>True when <paramref name="value"/> changed this frame.</returns>
        internal static bool WheelInt(
            string label,
            ref int value,
            int v_min,
            int v_max,
            float size_x,
            float size_y,
            int orientation,
            string format,
            float speed,
            bool invertColors)
        {
            return DK_WheelInt(
                ImGuiInternal.ToIdUtf8(label),
                ref value,
                v_min,
                v_max,
                size_x,
                size_y,
                orientation,
                ToUtf8OrNull(format),
                speed,
                invertColors ? 1 : 0) != 0;
        }

        // Null becomes a real null pointer (native shim falls back to the
        // widget's default format). Non-null is encoded like ToUtf8.
        private static byte[] ToUtf8OrNull(string value)
        {
            return value == null ? null : ToUtf8(value);
        }

        // Null-terminated UTF-8. Null becomes "\0" (empty string).
        // Kept locally for the printf format path only (display text, never an
        // ID — formats intentionally skip the sentinel); ID-bearing labels go
        // through ImGuiInternal.ToIdUtf8 (C14).
        private static byte[] ToUtf8(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return new byte[] { 0 };
            }
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            byte[] terminated = new byte[bytes.Length + 1];
            for (int i = 0; i < bytes.Length; i++)
            {
                terminated[i] = bytes[i];
            }
            terminated[bytes.Length] = 0;
            return terminated;
        }
    }
}
