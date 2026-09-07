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

        /// <summary>Resize every window to its content every frame (imgui.h:1225, 1 &lt;&lt; 6).</summary>
        AlwaysAutoResize = 0x40,
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
    /// Tab-bar flags for <see cref="ImGuiInternal.BeginTabBar"/>; values match
    /// <c>ImGuiTabBarFlags_</c> in imgui.h (typedef int, imgui.h:266). Only the values we use.
    /// </summary>
    [Flags]
    internal enum ImGuiTabBarFlags
    {
        None = 0,
    }

    /// <summary>
    /// Tab-item flags for <see cref="ImGuiInternal.BeginTabItem"/>; values match
    /// <c>ImGuiTabItemFlags_</c> in imgui.h (typedef int, imgui.h:267). Only the values we use.
    /// </summary>
    [Flags]
    internal enum ImGuiTabItemFlags
    {
        None = 0,
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
    /// is the locked mechanism (chunk C6 contract): by the time any call here runs,
    /// NativeBridge has already LoadLibrary'd the DLL, so Windows resolves the import
    /// by module name against the loaded module — SetDllDirectory(PluginData) only
    /// covers that one explicit load and is restored immediately after it.
    /// Calls are gated by C7's frame loop, not this layer.
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

        // CIMGUI_API void igSetNextItemWidth(float item_width); (cimgui.h:4170)
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igSetNextItemWidth(float item_width);

        // CIMGUI_API bool igInputFloat(const char* label,float* v,float step,float step_fast,const char* format,ImGuiInputTextFlags flags); (cimgui.h:4294)
        // step/step_fast are 0: ImGui::InputFloat passes a NULL step pointer
        // unless step > 0, so no step buttons render (imgui.cpp). format is
        // NULL: InputScalar substitutes the float default "%.3f"
        // (imgui_widgets.cpp:3814-3815).
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool igInputFloat([In] byte[] label, ref float v, float step, float step_fast, IntPtr format, int flags);

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

        // CIMGUI_API void igSameLine(float offset_from_start_x,float spacing); (cimgui.h:4190)
        // Both 0: next widget on the same line, default item spacing.
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igSameLine(float offset_from_start_x, float spacing);

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

        // ---- Custom-widget interaction + text measurement (chunk C9) ----
        // All verified against the pinned cimgui.h (sibling clone, imgui 1.92.9).

        // CIMGUI_API bool igInvisibleButton(const char* str_id,const ImVec2_c size,ImGuiButtonFlags flags); (cimgui.h:4246)
        // ImGuiButtonFlags_None = 0 (cimgui.h:854). Runs the full ButtonBehavior
        // logic (imgui_widgets.cpp:863) without drawing — the gradient helpers
        // read the rect back and draw over it.
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool igInvisibleButton([In] byte[] str_id, ImVec2 size, int flags);

        // CIMGUI_API bool igIsItemHovered(ImGuiHoveredFlags flags); (cimgui.h:4455)
        // ImGuiHoveredFlags_None = 0.
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool igIsItemHovered(int flags);

        // CIMGUI_API bool igIsItemActive(void); (cimgui.h:4456)
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool igIsItemActive();

        // CIMGUI_API ImVec2_c igGetItemRectMin(void); (cimgui.h:4469)
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern ImVec2 igGetItemRectMin();

        // CIMGUI_API ImVec2_c igGetItemRectMax(void); (cimgui.h:4470)
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern ImVec2 igGetItemRectMax();

        // CIMGUI_API ImVec2_c igCalcTextSize(const char* text,const char* text_end,bool hide_text_after_double_hash,float wrap_width); (cimgui.h:4485)
        // hide_text_after_double_hash = true so "##id" suffixes are excluded
        // (matches what InvisibleButton measures for the label part).
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern ImVec2 igCalcTextSize([In] byte[] text, IntPtr text_end, [MarshalAs(UnmanagedType.I1)] bool hide_text_after_double_hash, float wrap_width);

        // CIMGUI_API void ImDrawList_AddText_Vec2(ImDrawList* self,const ImVec2_c pos,ImU32 col,const char* text_begin,const char* text_end); (cimgui.h:4690)
        // Draws with the current font at its current size; the gradient button
        // centers the label inside its rect using igCalcTextSize.
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern void ImDrawList_AddText_Vec2(IntPtr self, ImVec2 pos, uint col, [In] byte[] text_begin, IntPtr text_end);

        // CIMGUI_API ImU32 igGetColorU32_Col(ImGuiCol idx,float alpha_mul); (cimgui.h:4175)
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern uint igGetColorU32_Col(int idx, float alpha_mul);

        // ---- Draw-list primitives + cursor position (chunk C20) ----
        // Verified against the pinned cimgui.h (sibling clone, imgui 1.92.9).

        // CIMGUI_API ImVec2_c igGetCursorScreenPos(void); (cimgui.h:4179)
        // NOTE: cimgui 1.92.9 returns ImVec2_c BY VALUE (the generator does not
        // emit the ImVec2* out-param sketched in the C20 contract) — same ABI
        // pattern as igGetItemRectMin (cimgui.h:4469), so the blittable 8-byte
        // ImVec2 struct carries it on Win64 Cdecl.
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern ImVec2 igGetCursorScreenPos();

        // CIMGUI_API void ImDrawList_AddEllipse(ImDrawList* self,const ImVec2_c center,const ImVec2_c radius,ImU32 col,float rot,int num_segments,float thickness); (cimgui.h:4688)
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern void ImDrawList_AddEllipse(IntPtr self, ImVec2 center, ImVec2 radius, uint col, float rot, int num_segments, float thickness);

        // CIMGUI_API void ImDrawList_AddEllipseFilled(ImDrawList* self,const ImVec2_c center,const ImVec2_c radius,ImU32 col,float rot,int num_segments); (cimgui.h:4689)
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern void ImDrawList_AddEllipseFilled(IntPtr self, ImVec2 center, ImVec2 radius, uint col, float rot, int num_segments);

        // ---- Tab bar / tab items (chunk C18) ----
        // Verified against the pinned cimgui.h (sibling clone, imgui 1.92.9).

        // CIMGUI_API bool igBeginTabBar(const char* str_id,ImGuiTabBarFlags flags); (cimgui.h:4418)
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool igBeginTabBar([In] byte[] str_id, int flags);

        // CIMGUI_API void igEndTabBar(void); (cimgui.h:4419)
        // Pairing rule: only call EndTabBar() if BeginTabBar() returned true (imgui.h:965).
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igEndTabBar();

        // CIMGUI_API bool igBeginTabItem(const char* label,bool* p_open,ImGuiTabItemFlags flags); (cimgui.h:4420)
        // p_open is NULL (non-closable tab; the imgui.h:966 default allows NULL).
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool igBeginTabItem([In] byte[] label, IntPtr p_open, int flags);

        // CIMGUI_API void igEndTabItem(void); (cimgui.h:4421)
        // Pairing rule: only call EndTabItem() if BeginTabItem() returned true (imgui.h:967).
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igEndTabItem();

        // ---- Collapsing header (chunk C28) ----
        // Verified against the pinned cimgui.h (sibling clone, imgui 1.92.9).

        // CIMGUI_API bool igCollapsingHeader_TreeNodeFlags(const char* label,ImGuiTreeNodeFlags flags); (cimgui.h:4336)
        // Not a Begin/End pair: returns whether the section is open and the
        // caller draws the content inside an if. Only the flags subset the
        // wrapper exposes is forwarded (ImGuiTreeNodeFlags_None = 0,
        // cimgui.h:362; ImGuiTreeNodeFlags_DefaultOpen = 1 << 5, cimgui.h:368).
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool igCollapsingHeader_TreeNodeFlags([In] byte[] label, int flags);

        // ---- Native theme exports (chunk C8) ----
        // Own DearImGuiKSPNative_* ABI (ContextHost.h), not cimgui: cimgui exports
        // no per-field style setters, so the DLL provides these pass-throughs over
        // the live ImGuiStyle. Returns: 0 ok, 1 no context, 2 bad index/unknown var.

        // int DearImGuiKSPNative_SetStyleColor(int idx, float r, float g, float b, float a); (ContextHost.h)
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern int DearImGuiKSPNative_SetStyleColor(int idx, float r, float g, float b, float a);

        // int DearImGuiKSPNative_SetStyleVarFloat(int idx, float v); (ContextHost.h)
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern int DearImGuiKSPNative_SetStyleVarFloat(int idx, float v);

        // int DearImGuiKSPNative_SetStyleVarVec2(int idx, float x, float y); (ContextHost.h)
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern int DearImGuiKSPNative_SetStyleVarVec2(int idx, float x, float y);

        // int DearImGuiKSPNative_StyleColorsDark(void); (ContextHost.h)
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern int DearImGuiKSPNative_StyleColorsDark();

        // int DearImGuiKSPNative_SetUiScale(float scale); (ContextHost.h, chunk C31)
        // Live UI scale: ScaleAllSizes(scale) on the current style plus an
        // absolute io.FontGlobalScale. Legal only right after a whole-style
        // reset (StyleColorsDark), which every theme apply performs.
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern int DearImGuiKSPNative_SetUiScale(float scale);

        // ---- Native window-bg gradient descriptor (chunk C9) ----

        // int DearImGuiKSPNative_SetWindowBgGradient(int enabled, float r1, float g1, float b1, float a1, float r2, float g2, float b2, float a2); (ContextHost.h)
        // enabled != 0 turns on the per-frame native EndFrame shading pass over
        // every visible window's background fill; enabled == 0 disables it
        // (strict no-op, byte-exact stock rendering).
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern int DearImGuiKSPNative_SetWindowBgGradient(int enabled, float r1, float g1, float b1, float a1, float r2, float g2, float b2, float a2);

        // int DearImGuiKSPNative_GetDrawListVtxCount(ImDrawList* drawList); (ContextHost.h)
        // cimgui exports no VtxBuffer accessor; the gradient helpers record
        // before/after counts to shade exactly the verts a fill appended.
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern int DearImGuiKSPNative_GetDrawListVtxCount(IntPtr drawList);

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

        internal static void SetNextItemWidth(float width)
        {
            igSetNextItemWidth(width);
        }

        internal static bool InputText(byte[] labelUtf8, byte[] buffer, ImGuiInputTextFlags flags)
        {
            return igInputText(labelUtf8, buffer, (UIntPtr)buffer.Length, (int)flags, IntPtr.Zero, IntPtr.Zero);
        }

        internal static bool InputFloat(byte[] labelUtf8, ref float value)
        {
            return igInputFloat(labelUtf8, ref value, 0f, 0f, IntPtr.Zero, (int)ImGuiInputTextFlags.None);
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

        internal static void SameLine()
        {
            igSameLine(0f, 0f);
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

        // Draw-list primitives + cursor position (C20).

        internal static ImVec2 GetCursorScreenPos()
        {
            return igGetCursorScreenPos();
        }

        internal static void DrawListAddEllipse(IntPtr drawList, ImVec2 center, ImVec2 radius, uint col, float rot, int numSegments, float thickness)
        {
            ImDrawList_AddEllipse(drawList, center, radius, col, rot, numSegments, thickness);
        }

        internal static void DrawListAddEllipseFilled(IntPtr drawList, ImVec2 center, ImVec2 radius, uint col, float rot, int numSegments)
        {
            ImDrawList_AddEllipseFilled(drawList, center, radius, col, rot, numSegments);
        }

        internal static bool RadioButton(byte[] labelUtf8, bool active)
        {
            return igRadioButton_Bool(labelUtf8, active);
        }

        internal static bool RadioButton(byte[] labelUtf8, ref int v, int vButton)
        {
            return igRadioButton_IntPtr(labelUtf8, ref v, vButton);
        }

        // Custom-widget interaction + text measurement (C9).

        internal static bool InvisibleButton(byte[] labelUtf8, ImVec2 size)
        {
            return igInvisibleButton(labelUtf8, size, flags: 0); // ImGuiButtonFlags_None
        }

        internal static bool IsItemHovered()
        {
            return igIsItemHovered(flags: 0); // ImGuiHoveredFlags_None
        }

        internal static bool IsItemActive()
        {
            return igIsItemActive();
        }

        internal static ImVec2 GetItemRectMin()
        {
            return igGetItemRectMin();
        }

        internal static ImVec2 GetItemRectMax()
        {
            return igGetItemRectMax();
        }

        internal static ImVec2 CalcTextSize(byte[] textUtf8)
        {
            return igCalcTextSize(textUtf8, IntPtr.Zero, hide_text_after_double_hash: true, wrap_width: -1.0f);
        }

        internal static void DrawListAddText(IntPtr drawList, ImVec2 pos, uint col, byte[] textUtf8)
        {
            ImDrawList_AddText_Vec2(drawList, pos, col, textUtf8, IntPtr.Zero);
        }

        internal static uint GetColorU32(int idx, float alphaMul)
        {
            return igGetColorU32_Col(idx, alphaMul);
        }

        // Tab bar / tab items (C18). Both Begins take flags from the matching
        // internal enum; p_open is always NULL (non-closable tabs).

        internal static bool BeginTabBar(byte[] idUtf8)
        {
            return igBeginTabBar(idUtf8, (int)ImGuiTabBarFlags.None);
        }

        internal static void EndTabBar()
        {
            igEndTabBar();
        }

        internal static bool BeginTabItem(byte[] labelUtf8)
        {
            return igBeginTabItem(labelUtf8, IntPtr.Zero, (int)ImGuiTabItemFlags.None);
        }

        internal static void EndTabItem()
        {
            igEndTabItem();
        }

        // Collapsing header (C28). Flags subset only: None (cimgui.h:362) or
        // DefaultOpen = 1 << 5 (cimgui.h:368); no other ImGuiTreeNodeFlags.

        internal static bool CollapsingHeader(byte[] labelUtf8, bool defaultOpen)
        {
            return igCollapsingHeader_TreeNodeFlags(labelUtf8, defaultOpen ? 1 << 5 : 0);
        }

        // Window-bg gradient descriptor (C9). <paramref name="enabled"/> != 0
        // turns on the native per-frame shading pass; 0 disables it (no-op).

        internal static int SetWindowBgGradient(int enabled, float r1, float g1, float b1, float a1, float r2, float g2, float b2, float a2)
        {
            return DearImGuiKSPNative_SetWindowBgGradient(enabled, r1, g1, b1, a1, r2, g2, b2, a2);
        }

        internal static int GetDrawListVtxCount(IntPtr drawList)
        {
            return DearImGuiKSPNative_GetDrawListVtxCount(drawList);
        }

        // Theme style setters (C8). <paramref name="idx"/> is a DearImGuiKSP.ImGuiCol /
        // DearImGuiKSP.ImGuiStyleVar value passed as int (single source of truth, C1).

        internal static int SetStyleColor(int idx, float r, float g, float b, float a)
        {
            return DearImGuiKSPNative_SetStyleColor(idx, r, g, b, a);
        }

        internal static int SetStyleVarFloat(int idx, float v)
        {
            return DearImGuiKSPNative_SetStyleVarFloat(idx, v);
        }

        internal static int SetStyleVarVec2(int idx, float x, float y)
        {
            return DearImGuiKSPNative_SetStyleVarVec2(idx, x, y);
        }

        internal static int StyleColorsDark()
        {
            return DearImGuiKSPNative_StyleColorsDark();
        }

        internal static int SetUiScale(float scale)
        {
            return DearImGuiKSPNative_SetUiScale(scale);
        }
    }
}
