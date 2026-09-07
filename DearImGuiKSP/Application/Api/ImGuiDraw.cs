using DearImGuiKSP.Interop;
using Color32 = UnityEngine.Color32;
using Vector2 = UnityEngine.Vector2;

namespace DearImGuiKSP
{
    /// <summary>
    /// Public draw-list primitives over the current window's ImGui draw list
    /// (C20, spec §5.5 orbit-radar sanction): cursor screen position plus
    /// line/circle/ellipse/rect/text geometry in SCREEN coordinates. All
    /// members are no-ops when <see cref="DearImGuiKSP.IsAvailable"/> is false,
    /// and all geometry lands on the current window's draw list — draw within
    /// the same registered callback (and the same window scope) that queried
    /// <see cref="GetCursorScreenPos"/>, and never retain coordinates across
    /// frames (windows move). Colors are packed with the shared
    /// <c>ImGuiGradients.Pack</c> helper; <see cref="AddText(Vector2, Color32, string)"/>
    /// carries the per-call UTF-8 allocation documented there.
    /// </summary>
    public static class ImGuiDraw
    {
        /// <summary>
        /// Screen-coordinate position where the next widget will be drawn in
        /// the current window — the anchor for custom drawing over a layout
        /// slot. Pair with <see cref="Dummy(float, float)"/> to reserve the
        /// canvas area, then draw primitives at and around the returned
        /// position. Only valid inside a registered callback, between the
        /// window's begin and end.
        /// </summary>
        /// <returns>The cursor position in screen coordinates; zero when unavailable.</returns>
        public static Vector2 GetCursorScreenPos()
        {
            if (!DearImGuiKSP.CanDeclareUi)
            {
                return Vector2.zero;
            }
            ImVec2 pos = ImGuiInternal.GetCursorScreenPos();
            return new Vector2(pos.X, pos.Y);
        }

        /// <summary>
        /// Adds a straight line to the current window's draw list. No-op when unavailable.
        /// </summary>
        /// <param name="p1">Start point, screen coordinates.</param>
        /// <param name="p2">End point, screen coordinates.</param>
        /// <param name="color">Line color (sRGB bytes).</param>
        /// <param name="thickness">Line thickness in pixels.</param>
        public static void AddLine(Vector2 p1, Vector2 p2, Color32 color, float thickness = 1f)
        {
            if (!DearImGuiKSP.CanDeclareUi)
            {
                return;
            }
            ImGuiInternal.DrawListAddLine(
                ImGuiInternal.GetWindowDrawList(),
                new ImVec2(p1.x, p1.y),
                new ImVec2(p2.x, p2.y),
                ImGuiGradients.Pack(color),
                thickness);
        }

        /// <summary>
        /// Adds a circle outline to the current window's draw list. No-op when unavailable.
        /// </summary>
        /// <param name="center">Circle center, screen coordinates.</param>
        /// <param name="radius">Radius in pixels.</param>
        /// <param name="color">Outline color (sRGB bytes).</param>
        /// <param name="thickness">Outline thickness in pixels.</param>
        public static void AddCircle(Vector2 center, float radius, Color32 color, float thickness = 1f)
        {
            if (!DearImGuiKSP.CanDeclareUi)
            {
                return;
            }
            ImGuiInternal.DrawListAddCircle(
                ImGuiInternal.GetWindowDrawList(),
                new ImVec2(center.x, center.y),
                radius,
                ImGuiGradients.Pack(color),
                numSegments: 0,
                thickness);
        }

        /// <summary>
        /// Adds a filled circle to the current window's draw list. No-op when unavailable.
        /// </summary>
        /// <param name="center">Circle center, screen coordinates.</param>
        /// <param name="radius">Radius in pixels.</param>
        /// <param name="color">Fill color (sRGB bytes).</param>
        public static void AddCircleFilled(Vector2 center, float radius, Color32 color)
        {
            if (!DearImGuiKSP.CanDeclareUi)
            {
                return;
            }
            ImGuiInternal.DrawListAddCircleFilled(
                ImGuiInternal.GetWindowDrawList(),
                new ImVec2(center.x, center.y),
                radius,
                ImGuiGradients.Pack(color),
                numSegments: 0);
        }

        /// <summary>
        /// Adds an ellipse outline to the current window's draw list. No-op when unavailable.
        /// </summary>
        /// <param name="center">Ellipse center, screen coordinates.</param>
        /// <param name="radii">Half-extents in pixels (x = horizontal, y = vertical).</param>
        /// <param name="color">Outline color (sRGB bytes).</param>
        /// <param name="rotation">Rotation in radians.</param>
        /// <param name="thickness">Outline thickness in pixels.</param>
        public static void AddEllipse(Vector2 center, Vector2 radii, Color32 color, float rotation = 0f, float thickness = 1f)
        {
            if (!DearImGuiKSP.CanDeclareUi)
            {
                return;
            }
            ImGuiInternal.DrawListAddEllipse(
                ImGuiInternal.GetWindowDrawList(),
                new ImVec2(center.x, center.y),
                new ImVec2(radii.x, radii.y),
                ImGuiGradients.Pack(color),
                rotation,
                numSegments: 0,
                thickness);
        }

        /// <summary>
        /// Adds a filled ellipse to the current window's draw list. No-op when unavailable.
        /// </summary>
        /// <param name="center">Ellipse center, screen coordinates.</param>
        /// <param name="radii">Half-extents in pixels (x = horizontal, y = vertical).</param>
        /// <param name="color">Fill color (sRGB bytes).</param>
        /// <param name="rotation">Rotation in radians.</param>
        public static void AddEllipseFilled(Vector2 center, Vector2 radii, Color32 color, float rotation = 0f)
        {
            if (!DearImGuiKSP.CanDeclareUi)
            {
                return;
            }
            ImGuiInternal.DrawListAddEllipseFilled(
                ImGuiInternal.GetWindowDrawList(),
                new ImVec2(center.x, center.y),
                new ImVec2(radii.x, radii.y),
                ImGuiGradients.Pack(color),
                rotation,
                numSegments: 0);
        }

        /// <summary>
        /// Adds a filled rectangle to the current window's draw list. No-op when unavailable.
        /// </summary>
        /// <param name="min">Top-left corner, screen coordinates.</param>
        /// <param name="max">Bottom-right corner, screen coordinates.</param>
        /// <param name="color">Fill color (sRGB bytes).</param>
        /// <param name="rounding">Corner radius in pixels.</param>
        public static void AddRectFilled(Vector2 min, Vector2 max, Color32 color, float rounding = 0f)
        {
            if (!DearImGuiKSP.CanDeclareUi)
            {
                return;
            }
            ImGuiInternal.DrawListAddRectFilled(
                ImGuiInternal.GetWindowDrawList(),
                new ImVec2(min.x, min.y),
                new ImVec2(max.x, max.y),
                ImGuiGradients.Pack(color),
                rounding);
        }

        /// <summary>
        /// Adds a text string to the current window's draw list at the given
        /// position, drawn with the current font at its current size. No-op
        /// when unavailable.
        /// </summary>
        /// <param name="pos">Baseline anchor position, screen coordinates.</param>
        /// <param name="color">Text color (sRGB bytes).</param>
        /// <param name="text">The text to draw; null renders nothing.</param>
        /// <remarks>
        /// Carries the facade's documented per-call UTF-8 allocation convention
        /// (same as label-taking facade methods): the string is encoded on every
        /// call. Keep such calls off the per-frame hot path or cache the encoded
        /// result at the call site.
        /// </remarks>
        public static void AddText(Vector2 pos, Color32 color, string text)
        {
            if (!DearImGuiKSP.CanDeclareUi || string.IsNullOrEmpty(text))
            {
                return;
            }
            ImGuiInternal.DrawListAddText(
                ImGuiInternal.GetWindowDrawList(),
                new ImVec2(pos.x, pos.y),
                ImGuiGradients.Pack(color),
                text);
        }

        /// <summary>
        /// Submits an invisible item of the given size, advancing the cursor and
        /// growing the window's content bounds — the layout counterpart of
        /// <see cref="GetCursorScreenPos"/>: query the cursor, draw primitives
        /// over that canvas, then call this to reserve the area so subsequent
        /// widgets lay out below it. No-op when unavailable.
        /// </summary>
        /// <param name="width">Reserved width in pixels.</param>
        /// <param name="height">Reserved height in pixels.</param>
        public static void Dummy(float width, float height)
        {
            if (!DearImGuiKSP.CanDeclareUi)
            {
                return;
            }
            ImGuiInternal.Dummy(width, height);
        }
    }
}
