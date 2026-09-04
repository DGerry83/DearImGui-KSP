using System;
using System.Runtime.InteropServices;
using System.Text;

namespace DearImGuiKSP.Interop
{
    /// <summary>
    /// Raw + safe P/Invoke surface for the hand-written native extension shims
    /// (chunk C10: imgui_toggle; M5 adds knobs/wheels). Same seam as
    /// <see cref="ImGuiNative"/>: implicit <c>[DllImport("DearImGuiKSPNative")]</c>
    /// resolves against the module NativeBridge already LoadLibrary'd. The shim
    /// ABI uses <c>int</c> for bools (1-byte C++ bools stay behind the shim) and
    /// returns 1 when the value changed, 0 otherwise. C9 owns ImGuiNative.cs /
    /// ImGuiInternal.cs, so this file is self-contained (its own ToUtf8).
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

        /// <summary>
        /// Draws imgui_toggle's default toggle. The label is encoded to a
        /// null-terminated UTF-8 buffer per call (same ToUtf8 convention as
        /// <c>ImGuiInternal</c>); toggle labels are short-lived and the buffer
        /// is not retained.
        /// </summary>
        /// <returns>True when <paramref name="value"/> changed this frame.</returns>
        internal static bool Toggle(string label, ref bool value)
        {
            int v = value ? 1 : 0;
            bool changed = DK_Toggle(ToUtf8(label), ref v) != 0;
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
            bool changed = DK_ToggleFlags(ToUtf8(label), ref v, flags) != 0;
            value = v != 0;
            return changed;
        }

        // Null-terminated UTF-8. Null becomes "\0" (empty string).
        // Duplicated from ImGuiInternal (C9-owned; not editable here).
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
