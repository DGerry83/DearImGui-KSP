using System;
using System.Runtime.InteropServices;

namespace DearImGuiKSP.Interop
{
    /// <summary>
    /// Window flags for <see cref="ImGuiInternal.BeginWindow"/>; values match
    /// <c>ImGuiWindowFlags_</c> in imgui.h (typedef int, imgui.h:273). Only the values we use.
    /// </summary>
    [Flags]
    internal enum ImGuiWindowFlags
    {
        None = 0,
    }

    /// <summary>
    /// Input-text flags for <see cref="ImGuiInternal.InputText"/>; values match
    /// <c>ImGuiInputTextFlags_</c> in imgui.h (typedef int, imgui.h:258). Only the values we use.
    /// </summary>
    [Flags]
    internal enum ImGuiInputTextFlags
    {
        None = 0,

        /// <summary>Return true only when Enter is pressed, not on every edit (imgui.h:1327, 1 &lt;&lt; 6).</summary>
        EnterReturnsTrue = 0x40,
    }

    /// <summary>
    /// Slider flags for <see cref="ImGuiInternal.SliderFloat"/>; values match
    /// <c>ImGuiSliderFlags_</c> in imgui.h (typedef int, imgui.h:265). Only the values we use.
    /// </summary>
    [Flags]
    internal enum ImGuiSliderFlags
    {
        None = 0,

        /// <summary>
        /// Clamp Ctrl+Click manual input to the slider bounds
        /// (imgui.h:2057, ClampOnInput | ClampZeroRange = (1 &lt;&lt; 9) | (1 &lt;&lt; 10)).
        /// </summary>
        AlwaysClamp = 0x600,
    }

    /// <summary>
    /// Child-region flags for <see cref="ImGuiInternal.BeginScrollRegion"/>; values match
    /// <c>ImGuiChildFlags_</c> in imgui.h (typedef int, imgui.h:249). Only the values we use.
    /// </summary>
    [Flags]
    internal enum ImGuiChildFlags
    {
        None = 0,

        /// <summary>Outer border + WindowPadding (imgui.h:1270, 1 &lt;&lt; 0).</summary>
        Borders = 1,
    }

    /// <summary>
    /// Raw cimgui P/Invoke declarations (private). Implicit <c>[DllImport("DearImGuiKSPNative")]</c>
    /// is the locked mechanism (chunk C6 contract): Windows resolves against the module that
    /// NativeBridge already LoadLibrary'd, with SetDllDirectory(PluginData) covering the search
    /// path. Calls are gated by C7's frame loop, not this layer.
    /// cimgui <c>bool</c> is a 1-byte C++ bool, so returns use UnmanagedType.I1.
    /// No variadic functions — P/Invoke cannot call varargs.
    /// </summary>
    internal static class ImGuiNative
    {
        private const string Dll = "DearImGuiKSPNative";

        // Private externs keep their cimgui names (EntryPoint = method name).

        // CIMGUI_API bool igBegin(const char* name, bool* p_open, ImGuiWindowFlags flags);
        // p_open is NULL here (no close button in MVP).
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool igBegin([In] byte[] name, IntPtr p_open, int flags);

        // CIMGUI_API void igEnd(void);
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igEnd();

        // CIMGUI_API void igTextUnformatted(const char* text, const char* text_end);
        // text_end is NULL (string is null-terminated).
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igTextUnformatted([In] byte[] text, IntPtr text_end);

        // CIMGUI_API bool igButton(const char* label, const ImVec2_c size);
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool igButton([In] byte[] label, ImVec2 size);

        // CIMGUI_API bool igSliderFloat(const char* label, float* v, float v_min, float v_max, const char* format, ImGuiSliderFlags flags);
        // format is NULL (SliderScalar substitutes the float default "%.3f").
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool igSliderFloat([In] byte[] label, ref float v, float v_min, float v_max, IntPtr format, int flags);

        // CIMGUI_API bool igInputText(const char* label, char* buf, size_t buf_size, ImGuiInputTextFlags flags, ImGuiInputTextCallback callback, void* user_data);
        // callback/user_data are NULL in MVP; buf round-trips edited text via [In, Out].
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool igInputText([In] byte[] label, [In, Out] byte[] buf, UIntPtr buf_size, int flags, IntPtr callback, IntPtr user_data);

        // CIMGUI_API bool igBeginChild_Str(const char* str_id, const ImVec2_c size, ImGuiChildFlags child_flags, ImGuiWindowFlags window_flags);
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool igBeginChild_Str([In] byte[] str_id, ImVec2 size, int child_flags, int window_flags);

        // CIMGUI_API void igEndChild(void);
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igEndChild();

        // CIMGUI_API float igGetScrollY(void);
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern float igGetScrollY();

        // CIMGUI_API void igSetCursorPosY(float local_y);
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igSetCursorPosY(float local_y);

        // ---- Internal surface for ImGuiInternal (keeps the raw P/Invokes private) ----

        internal static bool Begin(byte[] nameUtf8, ImGuiWindowFlags flags)
        {
            return igBegin(nameUtf8, IntPtr.Zero, (int)flags);
        }

        internal static void EndWindow()
        {
            igEnd();
        }

        internal static void TextUnformatted(byte[] textUtf8)
        {
            igTextUnformatted(textUtf8, IntPtr.Zero);
        }

        internal static bool Button(byte[] labelUtf8, ImVec2 size)
        {
            return igButton(labelUtf8, size);
        }

        internal static bool SliderFloat(byte[] labelUtf8, ref float value, float min, float max, ImGuiSliderFlags flags)
        {
            return igSliderFloat(labelUtf8, ref value, min, max, IntPtr.Zero, (int)flags);
        }

        internal static bool InputText(byte[] labelUtf8, byte[] buffer, ImGuiInputTextFlags flags)
        {
            return igInputText(labelUtf8, buffer, (UIntPtr)buffer.Length, (int)flags, IntPtr.Zero, IntPtr.Zero);
        }

        internal static bool BeginScrollRegion(byte[] idUtf8, float height)
        {
            return igBeginChild_Str(idUtf8, new ImVec2(0f, height), (int)ImGuiChildFlags.Borders, (int)ImGuiWindowFlags.None);
        }

        internal static void EndScrollRegion()
        {
            igEndChild();
        }

        internal static float GetScrollY()
        {
            return igGetScrollY();
        }

        internal static void SetCursorY(float y)
        {
            igSetCursorPosY(y);
        }
    }
}
