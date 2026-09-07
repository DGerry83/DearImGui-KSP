using System;
using System.Collections.Generic;
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
        // One buffer per distinct capacity, created on first use and reused (G2-10):
        // the native call's buf_size is the buffer's length, so a shared grown buffer
        // would let editing exceed a smaller caller capacity. ImGui calls are
        // single-threaded (frame loop), so sharing buffers is safe.
        private static readonly Dictionary<int, byte[]> _inputTextBuffers = new Dictionary<int, byte[]>();

        // Reused across InputTextWithHiddenLabel calls; holds
        // "##" + display text + "###" + label + NUL (G2-09).
        // Same single-threaded rationale as _inputTextBuffer.
        private static byte[] _hiddenLabelBuffer;

        // G3-20: docs/10 §6 promises every library widget guards the empty-label
        // collision with the window's own ImGui ID (the ISSUES #005 family:
        // GetID("") at window root returns the window's ID and trips ItemAdd's
        // assert — compiled out in the release native build, so the collision is
        // silent there). ID-bearing calls route through ToIdUtf8: an empty label
        // gets this shared invisible sentinel ID instead. Two empty labels in one
        // window now collide with EACH OTHER (harmless shared-item behavior),
        // never with the window. Silent substitution, same convention as the
        // Spinner facade's default per-type IDs.
        private static readonly byte[] EmptyIdSentinel = ToUtf8("##dk_empty_id");

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
            return ImGuiNative.Button(ToIdUtf8(label), new ImVec2(0f, 0f));
        }

        /// <summary>
        /// Draws a float slider. Wraps cimgui <c>igSliderFloat</c> with format = NULL;
        /// ImGui's SliderScalar substitutes the float default "%.3f"
        /// (imgui_widgets.cpp:2744). AlwaysClamp is set (G3-30): Ctrl+Click type-in
        /// entry is clamped to [min, max], matching the drag behavior.
        /// </summary>
        /// <returns>True when the value changed this frame; <paramref name="value"/> is updated in place.</returns>
        internal static bool SliderFloat(string label, ref float value, float min, float max)
        {
            return ImGuiNative.SliderFloat(ToIdUtf8(label), ref value, min, max, ImGuiSliderFlags.AlwaysClamp);
        }

        /// <summary>
        /// Sets the pixel width of the next widget declared this frame. Wraps cimgui
        /// <c>igSetNextItemWidth</c>; applies to exactly one item, then the default
        /// (fill available width) returns. Positive values size the widget frame
        /// itself, label excluded.
        /// </summary>
        internal static void SetNextItemWidth(float width)
        {
            ImGuiNative.SetNextItemWidth(width);
        }

        /// <summary>
        /// Draws a numeric type-in field bound to a float. Wraps cimgui
        /// <c>igInputFloat</c> with step = step_fast = 0 (no step buttons) and
        /// format = NULL (the float default "%.3f" applies). A "##" prefix in
        /// <paramref name="label"/> hides the label text inside the field while
        /// the label still anchors the widget's ID. Seeding <paramref name="value"/>
        /// from the backing setting each frame gives two-way sync: typing applies
        /// edits in place (return true), and an external change (e.g. a slider
        /// drag) shows up the moment the field is not being edited.
        /// </summary>
        /// <returns>True when a typed value was applied this frame; <paramref name="value"/> is updated in place.</returns>
        internal static bool InputFloat(string label, ref float value)
        {
            return ImGuiNative.InputFloat(ToIdUtf8(label), ref value);
        }

        /// <summary>
        /// Draws a single-line text input. Wraps cimgui <c>igInputText</c> with null
        /// callback/user_data (no callbacks in MVP) and no EnterReturnsTrue flag —
        /// returns true on every edit, not just Enter.
        /// </summary>
        /// <param name="capacity">
        /// Buffer size in bytes, including the NUL terminator; edited text is
        /// truncated to capacity-1 UTF-8 bytes. Buffers are reused across calls,
        /// one per distinct capacity, so the native buf_size always equals the
        /// requested capacity.
        /// </param>
        /// <returns>True when the user edited the text this frame; <paramref name="value"/> is updated in place.</returns>
        internal static bool InputText(string label, ref string value, int capacity = 256)
        {
            return InputTextCore(ToIdUtf8(label), ref value, capacity);
        }

        /// <summary>
        /// Draws a single-line text input with no visible label: the widget runs
        /// under a hidden ID ("##" + display text + "###" + <paramref name="label"/>),
        /// so ImGui renders no label text inside the field — the caller draws the
        /// label itself (ksp theme colors typed text by pushing Col_Text around
        /// this call). The mid-string "###" resets the ID hash (imgui.cpp
        /// ImHashStr), so the widget's identity equals the stock single-call
        /// <c>igInputText(label)</c> identity under every theme (G2-09), and the
        /// label still anchors it — it must stay unique among the window's
        /// inputs. Same buffer semantics as
        /// <see cref="InputText(string, ref string, int)"/>; the label is encoded
        /// into a reused buffer (zero per-frame allocation).
        /// </summary>
        /// <returns>True when the user edited the text this frame; <paramref name="value"/> is updated in place.</returns>
        internal static bool InputTextWithHiddenLabel(string label, ref string value, int capacity = 256)
        {
            return InputTextCore(HiddenLabelUtf8(label), ref value, capacity);
        }

        // Shared body of the InputText wrappers: stages the value into the
        // per-capacity reused buffer and round-trips edits back.
        private static bool InputTextCore(byte[] labelUtf8, ref string value, int capacity)
        {
            byte[] buffer = GetInputTextBuffer(capacity < 2 ? 2 : capacity);
            StageInputTextSeed(value, buffer);

            bool edited = ImGuiNative.InputText(labelUtf8, buffer, ImGuiInputTextFlags.None);
            if (edited)
            {
                value = FromUtf8(buffer);
            }
            return edited;
        }

        // One reused buffer per distinct capacity (G2-10): the native call's
        // buf_size is the buffer's length, so a shared grown buffer would let
        // editing exceed a smaller caller capacity. Created once per capacity;
        // steady-state frames allocate nothing. Internal for tests.
        internal static byte[] GetInputTextBuffer(int capacity)
        {
            byte[] buffer;
            if (!_inputTextBuffers.TryGetValue(capacity, out buffer))
            {
                buffer = new byte[capacity];
                _inputTextBuffers.Add(capacity, buffer);
            }
            return buffer;
        }

        // Seeds the value into the buffer, clamped to length-1 bytes (G2-10) on a
        // UTF-8 codepoint boundary (G3-18): a raw-byte clamp can split a multi-byte
        // sequence, and the persisted U+FFFD replacement char would corrupt the
        // value. NUL-terminates at the clamped length. Internal for tests.
        internal static void StageInputTextSeed(string value, byte[] buffer)
        {
            byte[] current = Encoding.UTF8.GetBytes(value ?? string.Empty);
            int copyLength = current.Length < buffer.Length - 1 ? current.Length : buffer.Length - 1;
            copyLength = ClampToUtf8Boundary(current, copyLength);
            for (int i = 0; i < copyLength; i++)
            {
                buffer[i] = current[i];
            }
            buffer[copyLength] = 0;
        }

        // Backs length off to the start of a split multi-byte UTF-8 sequence
        // (continuation bytes are 0x80-0xBF), so truncation never persists U+FFFD.
        private static int ClampToUtf8Boundary(byte[] bytes, int length)
        {
            if (length >= bytes.Length)
            {
                return length; // no truncation — nothing to split
            }
            while (length > 0 && (bytes[length] & 0xC0) == 0x80)
            {
                length--;
            }
            return length;
        }

        // Hidden-label ID with a THEME-INDEPENDENT identity (G2-09): the buffer
        // holds "##" + display text + "###" + label + NUL. The leading "##"
        // hides the label inside the field (ImGui renders nothing before the
        // marker — the caller draws the label itself); the mid-string "###"
        // resets ImHashStr to the window seed (imgui.cpp:2578/2596), so
        // everything before it contributes nothing and the widget ID hashes
        // exactly as the stock single-call path's GetID(label) — identical
        // under every theme. Encoded into a reused buffer (zero per-frame
        // allocation). Internal for tests.
        internal static byte[] HiddenLabelUtf8(string label)
        {
            string s = label ?? string.Empty;
            int displayChars = DearImGuiKSP.StripIdSuffixLength(s);
            int byteCount = Encoding.UTF8.GetByteCount(s);
            int needed = byteCount + byteCount + 6; // "##" + display + "###" + label + NUL (display <= label in bytes)
            if (_hiddenLabelBuffer == null || _hiddenLabelBuffer.Length < needed)
            {
                _hiddenLabelBuffer = new byte[needed];
            }
            _hiddenLabelBuffer[0] = (byte)'#';
            _hiddenLabelBuffer[1] = (byte)'#';
            int pos = 2 + Encoding.UTF8.GetBytes(s, 0, displayChars, _hiddenLabelBuffer, 2);
            _hiddenLabelBuffer[pos] = (byte)'#';
            _hiddenLabelBuffer[pos + 1] = (byte)'#';
            _hiddenLabelBuffer[pos + 2] = (byte)'#';
            pos += 3;
            pos += Encoding.UTF8.GetBytes(s, 0, s.Length, _hiddenLabelBuffer, pos);
            _hiddenLabelBuffer[pos] = 0;
            return _hiddenLabelBuffer;
        }

        /// <summary>
        /// Begins a fixed-height, bordered scrolling child region. Wraps cimgui
        /// <c>igBeginChild_Str</c> with child_flags = ImGuiChildFlags_Borders and
        /// window_flags = 0. Null <paramref name="id"/> renders as an empty string.
        /// </summary>
        /// <returns>False when the region is clipped — caller must still call <see cref="EndScrollRegion"/>.</returns>
        internal static bool BeginScrollRegion(string id, float height)
        {
            return ImGuiNative.BeginScrollRegion(ToIdUtf8(id), height);
        }

        /// <summary>
        /// Begins a bordered scrolling child region of the given size. Wraps cimgui
        /// <c>igBeginChild_Str</c> with child_flags = ImGuiChildFlags_Borders and
        /// window_flags = 0. Null <paramref name="id"/> renders as an empty string.
        /// </summary>
        /// <returns>False when the region is clipped — caller must still call <see cref="EndScrollRegion"/>.</returns>
        internal static bool BeginScrollRegion(string id, ImVec2 size)
        {
            return ImGuiNative.BeginScrollRegion(ToIdUtf8(id), size);
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
        /// Keeps the next widget on the current line instead of advancing to the
        /// next one. Wraps cimgui <c>igSameLine</c> with default offset and
        /// spacing (0, 0). Used to place a label beside a hidden-label input.
        /// </summary>
        internal static void SameLine()
        {
            ImGuiNative.SameLine();
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

        // ---- Draw-list primitives + cursor position (chunk C20) ----

        /// <summary>
        /// Position where the next widget will be drawn, in screen coordinates —
        /// the anchor for custom drawing over the current line's layout slot.
        /// Wraps cimgui <c>igGetCursorScreenPos</c> (by-value return, cimgui.h:4179).
        /// Valid only between <see cref="BeginWindow"/> and <see cref="EndWindow"/>.
        /// </summary>
        internal static ImVec2 GetCursorScreenPos()
        {
            return ImGuiNative.GetCursorScreenPos();
        }

        /// <summary>
        /// Adds an ellipse outline to a draw list. Wraps cimgui
        /// <c>ImDrawList_AddEllipse</c>; <paramref name="numSegments"/> = 0 lets
        /// ImGui auto-calculate from the radii.
        /// </summary>
        internal static void DrawListAddEllipse(ImDrawListHandle drawList, ImVec2 center, ImVec2 radius, uint col, float rot, int numSegments = 0, float thickness = 1f)
        {
            ImGuiNative.DrawListAddEllipse(drawList.NativePointer, center, radius, col, rot, numSegments, thickness);
        }

        /// <summary>
        /// Adds a filled ellipse to a draw list. Wraps cimgui
        /// <c>ImDrawList_AddEllipseFilled</c>; <paramref name="numSegments"/> = 0
        /// lets ImGui auto-calculate from the radii.
        /// </summary>
        internal static void DrawListAddEllipseFilled(ImDrawListHandle drawList, ImVec2 center, ImVec2 radius, uint col, float rot, int numSegments = 0)
        {
            ImGuiNative.DrawListAddEllipseFilled(drawList.NativePointer, center, radius, col, rot, numSegments);
        }

        /// <summary>
        /// Draws a radio button whose state is an explicit boolean. Wraps cimgui
        /// <c>igRadioButton_Bool</c>. Null label renders as an empty string.
        /// </summary>
        /// <returns>True on the frame the button is clicked.</returns>
        internal static bool RadioButton(string label, bool active)
        {
            return ImGuiNative.RadioButton(ToIdUtf8(label), active);
        }

        /// <summary>
        /// Draws a radio button bound to an integer value; the button is selected when
        /// <paramref name="v"/> equals <paramref name="vButton"/>, and clicking sets it.
        /// Wraps cimgui <c>igRadioButton_IntPtr</c>. Null label renders as an empty string.
        /// </summary>
        /// <returns>True on the frame the button is clicked; <paramref name="v"/> is updated in place.</returns>
        internal static bool RadioButton(string label, ref int v, int vButton)
        {
            return ImGuiNative.RadioButton(ToIdUtf8(label), ref v, vButton);
        }

        // ---- Custom-widget interaction + text measurement (chunk C9) ----

        /// <summary>
        /// Submits an invisible button of the given size and runs the full
        /// ButtonBehavior logic over it (imgui_widgets.cpp:863) without
        /// drawing anything. The gradient helpers read the resulting item rect
        /// back via <see cref="GetItemRectMin"/>/<see cref="GetItemRectMax"/>
        /// and draw over it. Wraps cimgui <c>igInvisibleButton</c> with
        /// ImGuiButtonFlags_None. Null label renders as an empty string.
        /// </summary>
        /// <returns>True on the frame the button is clicked (pressed).</returns>
        internal static bool InvisibleButton(string label, ImVec2 size)
        {
            return ImGuiNative.InvisibleButton(ToIdUtf8(label), size);
        }

        /// <summary>
        /// True when the last item (e.g. an <see cref="InvisibleButton"/>) is
        /// hovered by the mouse. Wraps cimgui <c>igIsItemHovered</c> with
        /// ImGuiHoveredFlags_None.
        /// </summary>
        internal static bool IsItemHovered()
        {
            return ImGuiNative.IsItemHovered();
        }

        /// <summary>
        /// True while the last item (e.g. an <see cref="InvisibleButton"/>) is
        /// being held active (mouse held down after press). Wraps cimgui
        /// <c>igIsItemActive</c>.
        /// </summary>
        internal static bool IsItemActive()
        {
            return ImGuiNative.IsItemActive();
        }

        /// <summary>
        /// Top-left corner of the last item's rectangle, in screen coordinates.
        /// Wraps cimgui <c>igGetItemRectMin</c>.
        /// </summary>
        internal static ImVec2 GetItemRectMin()
        {
            return ImGuiNative.GetItemRectMin();
        }

        /// <summary>
        /// Bottom-right corner of the last item's rectangle, in screen coordinates.
        /// Wraps cimgui <c>igGetItemRectMax</c>.
        /// </summary>
        internal static ImVec2 GetItemRectMax()
        {
            return ImGuiNative.GetItemRectMax();
        }

        /// <summary>
        /// Measures the on-screen size of a text string in the current font.
        /// Wraps cimgui <c>igCalcTextSize</c> with hide-after-"##" enabled
        /// (ID suffixes are not rendered) and no wrapping. Null measures as
        /// an empty string.
        /// </summary>
        internal static ImVec2 CalcTextSize(string text)
        {
            return ImGuiNative.CalcTextSize(ToUtf8(text));
        }

        /// <summary>
        /// Adds a text string to a draw list at the given position, drawn with
        /// the current font at its current size. Wraps cimgui
        /// <c>ImDrawList_AddText_Vec2</c> with text_end = NULL. Null text
        /// renders as an empty string.
        /// </summary>
        internal static void DrawListAddText(ImDrawListHandle drawList, ImVec2 pos, uint col, string text)
        {
            ImGuiNative.DrawListAddText(drawList.NativePointer, pos, col, ToUtf8(text));
        }

        /// <summary>
        /// Packs a global style color slot into the packed <c>ImU32</c> format,
        /// applying the style's alpha. Wraps cimgui <c>igGetColorU32_Col</c>.
        /// <paramref name="idx"/> is a <c>DearImGuiKSP.ImGuiCol</c> value passed as int.
        /// </summary>
        internal static uint GetColorU32(int idx, float alphaMul = 1f)
        {
            return ImGuiNative.GetColorU32(idx, alphaMul);
        }

        // ---- Tab bar / tab items (chunk C18) ----

        /// <summary>
        /// Begins a tab bar. Wraps cimgui <c>igBeginTabBar</c> with
        /// ImGuiTabBarFlags_None. Null <paramref name="id"/> renders as an empty string.
        /// </summary>
        /// <returns>
        /// False when the tab bar is clipped (or the library is unavailable) — in that
        /// case <see cref="EndTabBar"/> must NOT be called (imgui.h:965); the
        /// <c>ImGuiEx.TabBar</c> scope encodes that pairing.
        /// </returns>
        internal static bool BeginTabBar(string id)
        {
            return ImGuiNative.BeginTabBar(ToIdUtf8(id));
        }

        /// <summary>
        /// Ends the current tab bar. Wraps cimgui <c>igEndTabBar</c>. Only call after a
        /// <see cref="BeginTabBar"/> that returned true (imgui.h:965).
        /// </summary>
        internal static void EndTabBar()
        {
            ImGuiNative.EndTabBar();
        }

        /// <summary>
        /// Begins a non-closable tab inside the current tab bar. Wraps cimgui
        /// <c>igBeginTabItem</c> with p_open = NULL and ImGuiTabItemFlags_None.
        /// Null <paramref name="label"/> renders as an empty string.
        /// </summary>
        /// <returns>
        /// True when the tab is selected (its content should be drawn this frame).
        /// False when unselected or clipped — <see cref="EndTabItem"/> must NOT be
        /// called then (imgui.h:967); the <c>ImGuiEx.TabItem</c> scope encodes that pairing.
        /// </returns>
        internal static bool BeginTabItem(string label)
        {
            return ImGuiNative.BeginTabItem(ToIdUtf8(label));
        }

        /// <summary>
        /// Ends the current tab. Wraps cimgui <c>igEndTabItem</c>. Only call after a
        /// <see cref="BeginTabItem"/> that returned true (imgui.h:967).
        /// </summary>
        internal static void EndTabItem()
        {
            ImGuiNative.EndTabItem();
        }

        /// <summary>
        /// Draws a collapsible section header. Wraps cimgui
        /// <c>igCollapsingHeader_TreeNodeFlags</c> with the flags subset None /
        /// DefaultOpen (cimgui.h:362, 368). Not a Begin/End pair: the caller
        /// gates the section's content on the return value. Null
        /// <paramref name="label"/> renders as an empty label.
        /// </summary>
        /// <returns>True while the section is open (draw its content this frame).</returns>
        internal static bool CollapsingHeader(string label, bool defaultOpen)
        {
            return ImGuiNative.CollapsingHeader(ToIdUtf8(label), defaultOpen);
        }

        /// <summary>
        /// Sets the native two-stop vertical window-background gradient
        /// descriptor (C9, spec §6.1). <paramref name="enabled"/> != 0 turns on
        /// the per-frame EndFrame shading pass; 0 disables it so the pass is a
        /// strict no-op (the "dark" preset's byte-exact stock rendering).
        /// Wraps the native <c>DearImGuiKSPNative_SetWindowBgGradient</c> export.
        /// Returns 0 on success, 1 when the native context does not exist.
        /// </summary>
        internal static int SetWindowBgGradient(int enabled, float r1, float g1, float b1, float a1, float r2, float g2, float b2, float a2)
        {
            return ImGuiNative.SetWindowBgGradient(enabled, r1, g1, b1, a1, r2, g2, b2, a2);
        }

        /// <summary>
        /// Vertex count of a live draw list. cimgui exports no VtxBuffer
        /// accessor, so this wraps the native
        /// <c>DearImGuiKSPNative_GetDrawListVtxCount</c> pass-through (C9).
        /// Returns -1 for a null handle.
        /// </summary>
        internal static int GetDrawListVtxCount(ImDrawListHandle drawList)
        {
            return ImGuiNative.GetDrawListVtxCount(drawList.NativePointer);
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
        /// Applies the live UI scale: multiplies every style size by
        /// <paramref name="scale"/> (the native call operates on the CURRENT
        /// style, so the caller must reset the whole style first — the theme
        /// engine's apply does) and sets the global font scale absolutely.
        /// Wraps the native <c>DearImGuiKSPNative_SetUiScale</c> export (C31).
        /// Returns 0 on success, 1 when there is no context, 2 for a
        /// non-positive scale (nothing written).
        /// </summary>
        internal static int SetUiScale(float scale)
        {
            return ImGuiNative.SetUiScale(scale);
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

        // Null-terminated UTF-8 in a single allocation (G3-29): size the result
        // buffer from GetByteCount and encode straight into it, instead of
        // encoding to a scratch array and copying. Null/empty returns a shared
        // static empty string (callers never mutate the returned buffer).
        private static readonly byte[] EmptyUtf8 = { 0 };

        private static byte[] ToUtf8(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return EmptyUtf8;
            }
            byte[] terminated = new byte[Encoding.UTF8.GetByteCount(value) + 1];
            Encoding.UTF8.GetBytes(value, 0, value.Length, terminated, 0);
            return terminated;
        }

        // G3-20: ID-bearing variant of ToUtf8 — an empty label would hash to the
        // window's own ImGui ID at window root (ISSUES #005 family), so it gets
        // the shared invisible sentinel instead. Raw-render paths (Text,
        // CalcTextSize, DrawListAddText) keep ToUtf8: the sentinel would render
        // literally there, and they seed no ID. Internal for tests.
        internal static byte[] ToIdUtf8(string value)
        {
            return string.IsNullOrEmpty(value) ? EmptyIdSentinel : ToUtf8(value);
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
