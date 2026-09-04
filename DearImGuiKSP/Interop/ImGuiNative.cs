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
    /// Blittable mirror of cimgui's <c>ImVec4_c</c> (<c>struct { float x, y, z, w; }</c>, cimgui.h:264-268).
    /// 16 bytes, passed by value to <c>igPushStyleColor_Vec4</c>/<c>igGetColorU32_Vec4</c> — safe on Win64 Cdecl.
    /// </summary>
    internal struct ImVec4
    {
        public float X;
        public float Y;
        public float Z;
        public float W;

        public ImVec4(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }
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

        // CIMGUI_API void igDummy(const ImVec2_c size);
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igDummy(ImVec2 size);

        // CIMGUI_API void igPushStyleColor_U32(ImGuiCol idx,ImU32 col); (cimgui.h:4158)
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igPushStyleColor_U32(int idx, uint col);

        // CIMGUI_API void igPushStyleColor_Vec4(ImGuiCol idx,const ImVec4_c col); (cimgui.h:4159)
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igPushStyleColor_Vec4(int idx, ImVec4 col);

        // CIMGUI_API void igPopStyleColor(int count); (cimgui.h:4160)
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igPopStyleColor(int count);

        // CIMGUI_API void igPushStyleVar_Float(ImGuiStyleVar idx,float val); (cimgui.h:4161)
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igPushStyleVar_Float(int idx, float val);

        // CIMGUI_API void igPushStyleVar_Vec2(ImGuiStyleVar idx,const ImVec2_c val); (cimgui.h:4162)
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igPushStyleVar_Vec2(int idx, ImVec2 val);

        // CIMGUI_API void igPopStyleVar(int count); (cimgui.h:4165)
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igPopStyleVar(int count);

        // CIMGUI_API ImU32 igGetColorU32_Vec4(const ImVec4_c col); (cimgui.h:4176)
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern uint igGetColorU32_Vec4(ImVec4 col);

        // CIMGUI_API ImDrawList* igGetWindowDrawList(void); (cimgui.h:4119)
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr igGetWindowDrawList();

        // CIMGUI_API void ImDrawList_AddRectFilled(ImDrawList* self,const ImVec2_c p_min,const ImVec2_c p_max,ImU32 col,float rounding,ImDrawFlags flags); (cimgui.h:4678)
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern void ImDrawList_AddRectFilled(IntPtr self, ImVec2 p_min, ImVec2 p_max, uint col, float rounding, int flags);

        // CIMGUI_API void ImDrawList_AddRectFilledMultiColor(ImDrawList* self,const ImVec2_c p_min,const ImVec2_c p_max,ImU32 col_upr_left,ImU32 col_upr_right,ImU32 col_bot_right,ImU32 col_bot_left); (cimgui.h:4679)
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern void ImDrawList_AddRectFilledMultiColor(IntPtr self, ImVec2 p_min, ImVec2 p_max, uint col_upr_left, uint col_upr_right, uint col_bot_right, uint col_bot_left);

        // CIMGUI_API void igShadeVertsLinearColorGradientKeepAlpha(ImDrawList* draw_list,int vert_start_idx,int vert_end_idx,ImVec2_c gradient_p0,ImVec2_c gradient_p1,ImU32 col0,ImU32 col1); (cimgui.h:5595)
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igShadeVertsLinearColorGradientKeepAlpha(IntPtr draw_list, int vert_start_idx, int vert_end_idx, ImVec2 gradient_p0, ImVec2 gradient_p1, uint col0, uint col1);

        // CIMGUI_API void ImDrawList_AddCircle(ImDrawList* self,const ImVec2_c center,float radius,ImU32 col,int num_segments,float thickness); (cimgui.h:4684)
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern void ImDrawList_AddCircle(IntPtr self, ImVec2 center, float radius, uint col, int num_segments, float thickness);

        // CIMGUI_API void ImDrawList_AddCircleFilled(ImDrawList* self,const ImVec2_c center,float radius,ImU32 col,int num_segments); (cimgui.h:4685)
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern void ImDrawList_AddCircleFilled(IntPtr self, ImVec2 center, float radius, uint col, int num_segments);

        // CIMGUI_API void ImDrawList_AddLine(ImDrawList* self,const ImVec2_c p1,const ImVec2_c p2,ImU32 col,float thickness); (cimgui.h:4674)
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern void ImDrawList_AddLine(IntPtr self, ImVec2 p1, ImVec2 p2, uint col, float thickness);

        // CIMGUI_API bool igRadioButton_Bool(const char* label,bool active); (cimgui.h:4251)
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool igRadioButton_Bool([In] byte[] label, [MarshalAs(UnmanagedType.I1)] bool active);

        // CIMGUI_API bool igRadioButton_IntPtr(const char* label,int* v,int v_button); (cimgui.h:4252)
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool igRadioButton_IntPtr([In] byte[] label, ref int v, int v_button);

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

        // Style-color/style-variable identifiers are the public DearImGuiKSP.ImGuiCol /
        // DearImGuiKSP.ImGuiStyleVar enums (Application/Api/ImGuiStyleEnums.cs); these
        // wrappers take the raw int (C1: single source of truth, no duplicated table).

        internal static bool BeginScrollRegion(byte[] idUtf8, float height)
        {
            return igBeginChild_Str(idUtf8, new ImVec2(0f, height), (int)ImGuiChildFlags.Borders, (int)ImGuiWindowFlags.None);
        }

        internal static bool BeginScrollRegion(byte[] idUtf8, ImVec2 size)
        {
            return igBeginChild_Str(idUtf8, size, (int)ImGuiChildFlags.Borders, (int)ImGuiWindowFlags.None);
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

        internal static void Dummy(float width, float height)
        {
            igDummy(new ImVec2(width, height));
        }

        internal static void Dummy(ImVec2 size)
        {
            igDummy(size);
        }

        internal static void PushStyleColor(int idx, uint col)
        {
            igPushStyleColor_U32(idx, col);
        }

        internal static void PushStyleColor(int idx, ImVec4 col)
        {
            igPushStyleColor_Vec4(idx, col);
        }

        internal static void PopStyleColor(int count)
        {
            igPopStyleColor(count);
        }

        internal static void PushStyleVar(int idx, float val)
        {
            igPushStyleVar_Float(idx, val);
        }

        internal static void PushStyleVar(int idx, ImVec2 val)
        {
            igPushStyleVar_Vec2(idx, val);
        }

        internal static void PopStyleVar(int count)
        {
            igPopStyleVar(count);
        }

        internal static uint GetColorU32(ImVec4 col)
        {
            return igGetColorU32_Vec4(col);
        }

        internal static IntPtr GetWindowDrawList()
        {
            return igGetWindowDrawList();
        }

        internal static void DrawListAddRectFilled(IntPtr drawList, ImVec2 pMin, ImVec2 pMax, uint col, float rounding, int flags)
        {
            ImDrawList_AddRectFilled(drawList, pMin, pMax, col, rounding, flags);
        }

        internal static void DrawListAddRectFilledMultiColor(IntPtr drawList, ImVec2 pMin, ImVec2 pMax, uint upperLeft, uint upperRight, uint bottomRight, uint bottomLeft)
        {
            ImDrawList_AddRectFilledMultiColor(drawList, pMin, pMax, upperLeft, upperRight, bottomRight, bottomLeft);
        }

        internal static void ShadeVertsLinearColorGradientKeepAlpha(IntPtr drawList, int vertStartIdx, int vertEndIdx, ImVec2 gradientP0, ImVec2 gradientP1, uint col0, uint col1)
        {
            igShadeVertsLinearColorGradientKeepAlpha(drawList, vertStartIdx, vertEndIdx, gradientP0, gradientP1, col0, col1);
        }

        internal static void DrawListAddCircle(IntPtr drawList, ImVec2 center, float radius, uint col, int numSegments, float thickness)
        {
            ImDrawList_AddCircle(drawList, center, radius, col, numSegments, thickness);
        }

        internal static void DrawListAddCircleFilled(IntPtr drawList, ImVec2 center, float radius, uint col, int numSegments)
        {
            ImDrawList_AddCircleFilled(drawList, center, radius, col, numSegments);
        }

        internal static void DrawListAddLine(IntPtr drawList, ImVec2 p1, ImVec2 p2, uint col, float thickness)
        {
            ImDrawList_AddLine(drawList, p1, p2, col, thickness);
        }

        internal static bool RadioButton(byte[] labelUtf8, bool active)
        {
            return igRadioButton_Bool(labelUtf8, active);
        }

        internal static bool RadioButton(byte[] labelUtf8, ref int v, int vButton)
        {
            return igRadioButton_IntPtr(labelUtf8, ref v, vButton);
        }
    }
}
