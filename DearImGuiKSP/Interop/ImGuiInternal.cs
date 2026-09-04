using System;
using System.Text;

namespace DearImGuiKSP.Interop
{
    /// <summary>
    /// Safe internal ImGui surface consumed by the C7 frame loop. Wraps the raw
    /// cimgui P/Invokes in <see cref="ImGuiNative"/> with UTF-8 string handling and
    /// buffer management — no raw pointers or IntPtr above this layer (Q46).
    /// All methods must only be called between native BeginFrame/EndFrame (gated by C7).
    /// </summary>
    internal static class ImGuiInternal
    {
        // Reused across InputText calls; grown when a larger capacity is requested.
        // ImGui calls are single-threaded (frame loop), so sharing one buffer is safe.
        private static byte[] _inputTextBuffer;

        /// <summary>
        /// Begins an ImGui window. Wraps cimgui <c>igBegin</c> with p_open = NULL
        /// (no close button in MVP).
        /// </summary>
        /// <returns>False when the window is collapsed/clipped — caller must still call <see cref="EndWindow"/>.</returns>
        internal static bool BeginWindow(string name, ImGuiWindowFlags flags = ImGuiWindowFlags.None)
        {
            return ImGuiNative.Begin(ToUtf8(name), flags);
        }

        /// <summary>
        /// Ends the current ImGui window. Wraps cimgui <c>igEnd</c>. Always required
        /// after <see cref="BeginWindow"/>, regardless of its return value.
        /// </summary>
        internal static void EndWindow()
        {
            ImGuiNative.EndWindow();
        }

        /// <summary>
        /// Draws unformatted text. Wraps cimgui <c>igTextUnformatted</c> with
        /// text_end = NULL (null-terminated string). Null renders as an empty string.
        /// </summary>
        internal static void Text(string text)
        {
            ImGuiNative.TextUnformatted(ToUtf8(text));
        }

        /// <summary>
        /// Draws an auto-sized button. Wraps cimgui <c>igButton</c> with size = (0,0),
        /// which ImGui treats as "fit to label".
        /// </summary>
        /// <returns>True on the frame the button is clicked.</returns>
        internal static bool Button(string label)
        {
            return ImGuiNative.Button(ToUtf8(label), new ImVec2(0f, 0f));
        }

        /// <summary>
        /// Draws a float slider. Wraps cimgui <c>igSliderFloat</c> with format = NULL;
        /// ImGui's SliderScalar substitutes the float default "%.3f"
        /// (imgui_widgets.cpp:2744).
        /// </summary>
        /// <returns>True when the value changed this frame; <paramref name="value"/> is updated in place.</returns>
        internal static bool SliderFloat(string label, ref float value, float min, float max)
        {
            return ImGuiNative.SliderFloat(ToUtf8(label), ref value, min, max, ImGuiSliderFlags.None);
        }

        /// <summary>
        /// Draws a single-line text input. Wraps cimgui <c>igInputText</c> with null
        /// callback/user_data (no callbacks in MVP) and no EnterReturnsTrue flag —
        /// returns true on every edit, not just Enter.
        /// </summary>
        /// <param name="capacity">
        /// Buffer size in bytes, including the NUL terminator; edited text is
        /// truncated to capacity-1 UTF-8 bytes. The buffer is reused across calls
        /// and grown when a larger capacity is requested.
        /// </param>
        /// <returns>True when the user edited the text this frame; <paramref name="value"/> is updated in place.</returns>
        internal static bool InputText(string label, ref string value, int capacity = 256)
        {
            if (capacity < 2)
            {
                capacity = 2;
            }
            if (_inputTextBuffer == null || _inputTextBuffer.Length < capacity)
            {
                _inputTextBuffer = new byte[capacity];
            }

            byte[] current = Encoding.UTF8.GetBytes(value ?? string.Empty);
            int copyLength = current.Length < _inputTextBuffer.Length - 1 ? current.Length : _inputTextBuffer.Length - 1;
            for (int i = 0; i < copyLength; i++)
            {
                _inputTextBuffer[i] = current[i];
            }
            _inputTextBuffer[copyLength] = 0;

            bool edited = ImGuiNative.InputText(ToUtf8(label), _inputTextBuffer, ImGuiInputTextFlags.None);
            if (edited)
            {
                value = FromUtf8(_inputTextBuffer);
            }
            return edited;
        }

        /// <summary>
        /// Begins a fixed-height, bordered scrolling child region. Wraps cimgui
        /// <c>igBeginChild_Str</c> with child_flags = ImGuiChildFlags_Borders and
        /// window_flags = 0. Null <paramref name="id"/> renders as an empty string.
        /// </summary>
        /// <returns>False when the region is clipped — caller must still call <see cref="EndScrollRegion"/>.</returns>
        internal static bool BeginScrollRegion(string id, float height)
        {
            return ImGuiNative.BeginScrollRegion(ToUtf8(id), height);
        }

        /// <summary>
        /// Begins a bordered scrolling child region of the given size. Wraps cimgui
        /// <c>igBeginChild_Str</c> with child_flags = ImGuiChildFlags_Borders and
        /// window_flags = 0. Null <paramref name="id"/> renders as an empty string.
        /// </summary>
        /// <returns>False when the region is clipped — caller must still call <see cref="EndScrollRegion"/>.</returns>
        internal static bool BeginScrollRegion(string id, ImVec2 size)
        {
            return ImGuiNative.BeginScrollRegion(ToUtf8(id), size);
        }

        /// <summary>
        /// Ends the current child region. Wraps cimgui <c>igEndChild</c>. Always required
        /// after <see cref="BeginScrollRegion"/>, regardless of its return value.
        /// </summary>
        internal static void EndScrollRegion()
        {
            ImGuiNative.EndScrollRegion();
        }

        /// <summary>
        /// Current vertical scroll offset of the active region/window, in pixels.
        /// Wraps cimgui <c>igGetScrollY</c>.
        /// </summary>
        internal static float GetScrollY()
        {
            return ImGuiNative.GetScrollY();
        }

        /// <summary>
        /// Sets the vertical cursor position within the active region/window, in local
        /// coordinates. Wraps cimgui <c>igSetCursorPosY</c>.
        /// </summary>
        internal static void SetCursorY(float y)
        {
            ImGuiNative.SetCursorY(y);
        }

        /// <summary>
        /// Submits an invisible item of the given size, advancing the cursor and growing
        /// the window's content bounds. Wraps cimgui <c>igDummy</c>. Required after using
        /// <see cref="SetCursorY"/> to extend a region's scrollable range — ImGui asserts
        /// (imgui.cpp ErrorCheckUsingSetCursorPosToExtendParentBoundaries) when a cursor
        /// move extends parent boundaries without a following item.
        /// </summary>
        internal static void Dummy(float width, float height)
        {
            ImGuiNative.Dummy(width, height);
        }

        /// <summary>
        /// Submits an invisible item of the given size, advancing the cursor and growing
        /// the window's content bounds. Wraps cimgui <c>igDummy</c>. Required after using
        /// <see cref="SetCursorY"/> to extend a region's scrollable range — ImGui asserts
        /// (imgui.cpp ErrorCheckUsingSetCursorPosToExtendParentBoundaries) when a cursor
        /// move extends parent boundaries without a following item.
        /// </summary>
        internal static void Dummy(ImVec2 size)
        {
            ImGuiNative.Dummy(size);
        }

        /// <summary>
        /// Pushes a packed <c>ImU32</c> (ABGR, as produced by <see cref="GetColorU32"/>) onto
        /// the style-color stack. Wraps cimgui <c>igPushStyleColor_U32</c>.
        /// <paramref name="idx"/> is a <c>DearImGuiKSP.ImGuiCol</c> value passed as int.
        /// </summary>
        internal static void PushStyleColor(int idx, uint packedColor)
        {
            ImGuiNative.PushStyleColor(idx, packedColor);
        }

        /// <summary>
        /// Pushes an RGBA float vector onto the style-color stack. Wraps cimgui
        /// <c>igPushStyleColor_Vec4</c>. <paramref name="idx"/> is a
        /// <c>DearImGuiKSP.ImGuiCol</c> value passed as int.
        /// </summary>
        internal static void PushStyleColor(int idx, ImVec4 color)
        {
            ImGuiNative.PushStyleColor(idx, color);
        }

        /// <summary>
        /// Pops <paramref name="count"/> entries from the style-color stack. Wraps cimgui
        /// <c>igPopStyleColor</c>; every Push must be paired with exactly one Pop before
        /// the end of the frame.
        /// </summary>
        internal static void PopStyleColor(int count = 1)
        {
            ImGuiNative.PopStyleColor(count);
        }

        /// <summary>
        /// Pushes a float style variable (e.g. <c>DearImGuiKSP.ImGuiStyleVar.WindowRounding</c>)
        /// onto the style stack. Wraps cimgui <c>igPushStyleVar_Float</c>.
        /// <paramref name="idx"/> is a <c>DearImGuiKSP.ImGuiStyleVar</c> value passed as int.
        /// </summary>
        internal static void PushStyleVar(int idx, float val)
        {
            ImGuiNative.PushStyleVar(idx, val);
        }

        /// <summary>
        /// Pushes an <see cref="ImVec2"/> style variable (e.g.
        /// <c>DearImGuiKSP.ImGuiStyleVar.WindowPadding</c>) onto the style stack.
        /// Wraps cimgui <c>igPushStyleVar_Vec2</c>. <paramref name="idx"/> is a
        /// <c>DearImGuiKSP.ImGuiStyleVar</c> value passed as int.
        /// </summary>
        internal static void PushStyleVar(int idx, ImVec2 val)
        {
            ImGuiNative.PushStyleVar(idx, val);
        }

        /// <summary>
        /// Pops <paramref name="count"/> entries from the style-variable stack. Wraps cimgui
        /// <c>igPopStyleVar</c>; every Push must be paired with exactly one Pop before
        /// the end of the frame.
        /// </summary>
        internal static void PopStyleVar(int count = 1)
        {
            ImGuiNative.PopStyleVar(count);
        }

        /// <summary>
        /// Packs an RGBA float vector into the packed <c>ImU32</c> color format used by the
        /// draw-list API and <see cref="PushStyleColor(int, uint)"/>. Wraps cimgui
        /// <c>igGetColorU32_Vec4</c>.
        /// </summary>
        internal static uint GetColorU32(ImVec4 color)
        {
            return ImGuiNative.GetColorU32(color);
        }

        /// <summary>
        /// Returns the draw list of the current window, for custom geometry. Wraps cimgui
        /// <c>igGetWindowDrawList</c>. Valid only between <see cref="BeginWindow"/> and
        /// <see cref="EndWindow"/>; the handle must not be retained across frames.
        /// </summary>
        internal static ImDrawListHandle GetWindowDrawList()
        {
            return new ImDrawListHandle(ImGuiNative.GetWindowDrawList());
        }

        /// <summary>
        /// Adds a filled rectangle to a draw list. Wraps cimgui
        /// <c>ImDrawList_AddRectFilled</c> with <c>ImDrawFlags_None</c>.
        /// </summary>
        internal static void DrawListAddRectFilled(ImDrawListHandle drawList, ImVec2 pMin, ImVec2 pMax, uint col, float rounding = 0f)
        {
            ImGuiNative.DrawListAddRectFilled(drawList.NativePointer, pMin, pMax, col, rounding, flags: 0);
        }

        /// <summary>
        /// Adds a filled rectangle with a per-corner gradient to a draw list. Wraps cimgui
        /// <c>ImDrawList_AddRectFilledMultiColor</c>.
        /// </summary>
        internal static void DrawListAddRectFilledMultiColor(ImDrawListHandle drawList, ImVec2 pMin, ImVec2 pMax, uint upperLeft, uint upperRight, uint bottomRight, uint bottomLeft)
        {
            ImGuiNative.DrawListAddRectFilledMultiColor(drawList.NativePointer, pMin, pMax, upperLeft, upperRight, bottomRight, bottomLeft);
        }

        /// <summary>
        /// Rewrites the vertex colors of draw-list entries added since the recorded index
        /// range as a linear gradient, preserving alpha. Wraps cimgui
        /// <c>igShadeVertsLinearColorGradientKeepAlpha</c> (cimgui.h:5595). Pair with
        /// <c>ImDrawList_PrimReserve</c>-style recording via the draw-list vertex count
        /// before/after adding geometry.
        /// </summary>
        internal static void ShadeVertsLinearColorGradientKeepAlpha(ImDrawListHandle drawList, int vertStartIdx, int vertEndIdx, ImVec2 gradientP0, ImVec2 gradientP1, uint col0, uint col1)
        {
            ImGuiNative.ShadeVertsLinearColorGradientKeepAlpha(drawList.NativePointer, vertStartIdx, vertEndIdx, gradientP0, gradientP1, col0, col1);
        }

        /// <summary>
        /// Adds a circle outline to a draw list. Wraps cimgui <c>ImDrawList_AddCircle</c>;
        /// <paramref name="numSegments"/> = 0 lets ImGui auto-calculate from the radius.
        /// </summary>
        internal static void DrawListAddCircle(ImDrawListHandle drawList, ImVec2 center, float radius, uint col, int numSegments = 0, float thickness = 1f)
        {
            ImGuiNative.DrawListAddCircle(drawList.NativePointer, center, radius, col, numSegments, thickness);
        }

        /// <summary>
        /// Adds a filled circle to a draw list. Wraps cimgui <c>ImDrawList_AddCircleFilled</c>;
        /// <paramref name="numSegments"/> = 0 lets ImGui auto-calculate from the radius.
        /// </summary>
        internal static void DrawListAddCircleFilled(ImDrawListHandle drawList, ImVec2 center, float radius, uint col, int numSegments = 0)
        {
            ImGuiNative.DrawListAddCircleFilled(drawList.NativePointer, center, radius, col, numSegments);
        }

        /// <summary>
        /// Adds a line to a draw list. Wraps cimgui <c>ImDrawList_AddLine</c>.
        /// </summary>
        internal static void DrawListAddLine(ImDrawListHandle drawList, ImVec2 p1, ImVec2 p2, uint col, float thickness = 1f)
        {
            ImGuiNative.DrawListAddLine(drawList.NativePointer, p1, p2, col, thickness);
        }

        /// <summary>
        /// Draws a radio button whose state is an explicit boolean. Wraps cimgui
        /// <c>igRadioButton_Bool</c>. Null label renders as an empty string.
        /// </summary>
        /// <returns>True on the frame the button is clicked.</returns>
        internal static bool RadioButton(string label, bool active)
        {
            return ImGuiNative.RadioButton(ToUtf8(label), active);
        }

        /// <summary>
        /// Draws a radio button bound to an integer value; the button is selected when
        /// <paramref name="v"/> equals <paramref name="vButton"/>, and clicking sets it.
        /// Wraps cimgui <c>igRadioButton_IntPtr</c>. Null label renders as an empty string.
        /// </summary>
        /// <returns>True on the frame the button is clicked; <paramref name="v"/> is updated in place.</returns>
        internal static bool RadioButton(string label, ref int v, int vButton)
        {
            return ImGuiNative.RadioButton(ToUtf8(label), ref v, vButton);
        }

        /// <summary>
        /// Writes one slot of the global style color table (NOT the push/pop stack).
        /// Wraps the native <c>DearImGuiKSPNative_SetStyleColor</c> export (C8).
        /// <paramref name="idx"/> is a <c>DearImGuiKSP.ImGuiCol</c> value passed as int.
        /// Returns 0 on success, 1 when the native context does not exist, 2 for a
        /// bad index (nothing written).
        /// </summary>
        internal static int SetStyleColor(int idx, float r, float g, float b, float a)
        {
            return ImGuiNative.SetStyleColor(idx, r, g, b, a);
        }

        /// <summary>
        /// Writes one float field of the global style (rounding, border size, ...).
        /// Wraps the native <c>DearImGuiKSPNative_SetStyleVarFloat</c> export (C8);
        /// the native side accepts only the float-var subset the theme presets use.
        /// <paramref name="idx"/> is a <c>DearImGuiKSP.ImGuiStyleVar</c> value passed as int.
        /// Returns 0 on success, 1 when the native context does not exist, 2 for an
        /// unknown/wrong-arity variable (nothing written).
        /// </summary>
        internal static int SetStyleVarFloat(int idx, float v)
        {
            return ImGuiNative.SetStyleVarFloat(idx, v);
        }

        /// <summary>
        /// Writes one <see cref="ImVec2"/> field of the global style (paddings, spacing).
        /// Wraps the native <c>DearImGuiKSPNative_SetStyleVarVec2</c> export (C8);
        /// the native side accepts only the Vec2-var subset the theme presets use.
        /// <paramref name="idx"/> is a <c>DearImGuiKSP.ImGuiStyleVar</c> value passed as int.
        /// Returns 0 on success, 1 when the native context does not exist, 2 for an
        /// unknown/wrong-arity variable (nothing written).
        /// </summary>
        internal static int SetStyleVarVec2(int idx, float x, float y)
        {
            return ImGuiNative.SetStyleVarVec2(idx, x, y);
        }

        /// <summary>
        /// Re-applies the stock ImGui dark style to the global style color table.
        /// Wraps the native <c>DearImGuiKSPNative_StyleColorsDark</c> export (C8) —
        /// the "dark" preset's exactness comes from calling this, never from a
        /// managed color table. Returns 0 on success, 1 when there is no context.
        /// </summary>
        internal static int StyleColorsDark()
        {
            return ImGuiNative.StyleColorsDark();
        }

        /// <summary>
        /// Opaque handle to a native <c>ImDrawList</c>, obtained from
        /// <see cref="GetWindowDrawList"/>. Keeps raw pointers out of the safe surface (Q46);
        /// do not retain across frames.
        /// </summary>
        internal readonly struct ImDrawListHandle
        {
            private readonly IntPtr _nativePointer;

            internal ImDrawListHandle(IntPtr nativePointer)
            {
                _nativePointer = nativePointer;
            }

            internal IntPtr NativePointer
            {
                get { return _nativePointer; }
            }
        }

        // Null-terminated UTF-8. Null becomes "\0" (empty string).
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

        // Decodes up to the first NUL (or end of buffer).
        private static string FromUtf8(byte[] buffer)
        {
            int length = 0;
            while (length < buffer.Length && buffer[length] != 0)
            {
                length++;
            }
            return length == 0 ? string.Empty : Encoding.UTF8.GetString(buffer, 0, length);
        }
    }
}
