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
