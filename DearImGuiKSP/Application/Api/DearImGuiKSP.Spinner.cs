using System.Text;
using DearImGuiKSP.Interop;
using Color = UnityEngine.Color;

namespace DearImGuiKSP
{
    /// <summary>
    /// Visually distinct animated spinner types for <see cref="DearImGuiKSP.Spinner"/>,
    /// drawn by the vendored dalerank/imspinner library (pinned in
    /// <c>DearImGuiKSPNative/vendor/PIN_RECORD.md</c>). Each value cites the upstream
    /// C++ function it dispatches to in the vendored <c>imspinner.h</c> /
    /// <c>imspinner_*.h</c> headers. Spinners animate themselves natively from the
    /// ImGui clock (spec §6.3) — no tween interaction. Curated subset only: the
    /// upstream hand-written <c>cimspinner_config.h</c> enables a subset of spinners
    /// at compile time, and only enabled spinners are bound (see IMPEDIMENTS I-08
    /// for the candidate swaps vs. the original contract list).
    /// </summary>
    public enum SpinnerType
    {
        /// <summary>
        /// A rotating rainbow arc (imspinner.h:331, <c>SpinnerRainbowMix</c>).
        /// Constants: speed 2.8f (upstream defines no default for this function),
        /// sweep 0 to 2π, 1 arc, mode 0.
        /// </summary>
        RainbowMix = 0,

        /// <summary>
        /// Eight-tick activity arc rotating around a circle (imspinner.h:410,
        /// <c>SpinnerAng8</c>). Constants: background white, speed 2.8f, angle π,
        /// mode 0, radius coefficient 0.5f.
        /// </summary>
        Ang8 = 1,

        /// <summary>
        /// A clock face with a sweeping hand over a dim ring (imspinner.h:484,
        /// <c>SpinnerClock</c>). Constants: background half-white, speed 2.8f.
        /// The hands are butt-capped lines drawn at twice the given thickness,
        /// so keep the thickness at or below about one sixth of the radius —
        /// at thickness = radius/4 the short hand is a square rotating in the
        /// center at any size.
        /// </summary>
        Clock = 2,

        /// <summary>
        /// A fading pulse ring sequence (imspinner.h:497, <c>SpinnerPulsar</c>).
        /// Constants: speed 2.8f, sequence on, angle 0, mode 0. Note: this
        /// spinner's color parameter is its background ring color (upstream has
        /// no foreground color argument); the default tint is half-white.
        /// </summary>
        Pulsar = 3,

        /// <summary>
        /// An atom with orbiting electrons (imspinner.h:2535, <c>SpinnerAtom</c>).
        /// Constants: speed 2.8f, 3 ellipses. The electron dots are filled
        /// circles whose segment count is derived from the overall radius, so
        /// at small radii they render as coarse polygons (five-sided at
        /// radius 12); they read as round from roughly 32px radius up, with
        /// the thickness at about one sixth of the radius.
        /// </summary>
        Atom = 4,

        /// <summary>
        /// Dots that collapse into a bar and back (imspinner_dots.h:88,
        /// <c>SpinnerDotsToBar</c>). Constants: offset 0.5f (the upstream demo
        /// value — the function has no default), speed 2.8f, 5 dots.
        /// </summary>
        DotsToBar = 5,

        /// <summary>
        /// Two dots swinging on a pivot (imspinner_dots.h:606,
        /// <c>SpinnerSwingDots</c>). Constants: speed 2.8f.
        /// </summary>
        SwingDots = 6,

        /// <summary>
        /// A DNA double-helix of rungs and dots (imspinner_dots.h:636,
        /// <c>SpinnerDnaDots</c>). Constants: speed 2.8f, 8 rungs, delta 0.5f,
        /// mode off.
        /// </summary>
        DnaDots = 7,

        /// <summary>
        /// A row of fading vertical bars (imspinner_bars.h:20,
        /// <c>SpinnerFadeBars</c>). The <c>radius</c> argument is forwarded as the
        /// bar row width (<c>w</c> upstream); <c>thickness</c> is unused by this
        /// spinner. Constants: speed 2.8f, 3 bars, no scale.
        /// </summary>
        FadeBars = 8,

        /// <summary>
        /// A shape morphing through its corners (imspinner_shapes.h:22,
        /// <c>SpinnerMorphShape</c>). Constants: speed 1f, mode 0.
        /// </summary>
        MorphShape = 9,

        /// <summary>
        /// A triangle flipping around its axis (imspinner_shapes.h:97,
        /// <c>SpinnerFlipTriangle</c>). Constants: speed 1f, mode 0.
        /// </summary>
        FlipTriangle = 10,

        /// <summary>
        /// A square folding through its diagonal (imspinner_shapes.h:127,
        /// <c>SpinnerFoldSquare</c>). Constants: speed 1f, mode 0.
        /// </summary>
        FoldSquare = 11,

        /// <summary>
        /// A pinwheel of rotating blades (imspinner_shapes.h:183,
        /// <c>SpinnerPinwheel</c>). Constants: speed 1f, mode 0.
        /// </summary>
        Pinwheel = 12,

        /// <summary>
        /// Four corner squares chasing each other (imspinner_shapes.h:225,
        /// <c>SpinnerCornerSquares</c>). Constants: speed 1f, mode 0.
        /// </summary>
        CornerSquares = 13,

        /// <summary>
        /// A square splitting into a mirrored pair (imspinner_shapes.h:263,
        /// <c>SpinnerSplitSquare</c>). Constants: speed 1f, mode 0.
        /// </summary>
        SplitSquare = 14,
    }

    public static partial class DearImGuiKSP
    {
        // ISSUES #005: a spinner's label seeds its ImGuiID via SpinnerBegin's
        // GetID + ItemAdd (vendor/imspinner/imspinner.h:110) — an EMPTY label
        // returns the window's own ID and trips ItemAdd's assert
        // (imgui.cpp:12203). The default IDs below are unique per SpinnerType
        // and invisible ("##" prefix). Placing two spinners of the SAME type in
        // one window needs distinct caller-provided ids (standard ImGui ID rules).
        private static readonly byte[][] s_spinnerDefaultIds =
        {
            Label("##dk_spinner_rainbowmix"),
            Label("##dk_spinner_ang8"),
            Label("##dk_spinner_clock"),
            Label("##dk_spinner_pulsar"),
            Label("##dk_spinner_atom"),
            Label("##dk_spinner_dotstobar"),
            Label("##dk_spinner_swingdots"),
            Label("##dk_spinner_dnadots"),
            Label("##dk_spinner_fadebars"),
            Label("##dk_spinner_morphshape"),
            Label("##dk_spinner_fliptriangle"),
            Label("##dk_spinner_foldsquare"),
            Label("##dk_spinner_pinwheel"),
            Label("##dk_spinner_cornersquares"),
            Label("##dk_spinner_splitsquare"),
        };

        // Null-terminated UTF-8, static-init only (default IDs) or the
        // caller-provided id path. Same convention as the Interop ToUtf8 helpers.
        private static byte[] Label(string value)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            byte[] terminated = new byte[bytes.Length + 1];
            for (int i = 0; i < bytes.Length; i++)
            {
                terminated[i] = bytes[i];
            }
            return terminated;
        }

        /// <summary>
        /// Draws an animated spinner (vendored dalerank/imspinner) of the given
        /// type. Spinners indicate ongoing work; they animate themselves natively
        /// from the ImGui clock and have no interaction or state. Only valid
        /// inside a registered callback.
        /// </summary>
        /// <param name="type">Which spinner to draw; see <see cref="SpinnerType"/>.</param>
        /// <param name="radius">
        /// Widget radius in pixels (the FadeBars type uses it as row width).
        /// Small radii produce visibly coarse circles: ImGui reduces the
        /// segment count of circles and arcs as they shrink (an upstream
        /// design decision), and some spinner types derive sub-shapes from
        /// that count — at radius 12, Atom's electron dots render as
        /// five-sided polygons. Prefer about 20px or more for dot/arc-heavy
        /// types; sizes are forwarded verbatim (no clamping).
        /// </param>
        /// <param name="thickness">
        /// Line thickness in pixels (unused by FadeBars). Keep this at or
        /// below about one sixth of the radius for hand- and arc-heavy
        /// types: Clock's hands are butt-capped lines drawn at twice this
        /// width, so a thickness of radius/4 makes the short hand read as a
        /// rotating square.
        /// </param>
        /// <param name="tint">
        /// Optional color override; null uses the spinner's upstream default
        /// (white for most types, half-white for Clock and Pulsar backgrounds —
        /// see the <see cref="SpinnerType"/> value docs).
        /// </param>
        /// <param name="id">
        /// Optional ImGui ID for the widget (invisible; standard "##" semantics
        /// apply). Null uses a unique per-type default. Pass distinct ids when
        /// placing two spinners of the same type in one window.
        /// </param>
        public static void Spinner(SpinnerType type, float radius, float thickness, Color? tint = null, string id = null)
        {
            if (!IsAvailable)
            {
                return;
            }
            // Nullable<ImSpinnerColor> is a stack value type; null = "upstream
            // default color" resolved per spinner in ImSpinnerNative. The default
            // id path is allocation-free; a caller-provided id allocates one
            // short-lived UTF-8 buffer, the same convention as every facade label.
            ImSpinnerColor? spinnerTint = tint.HasValue
                ? new ImSpinnerColor(ToImVec4(tint.Value))
                : (ImSpinnerColor?)null;
            byte[] label = id == null ? s_spinnerDefaultIds[(int)type] : Label(id);
            switch (type)
            {
                case SpinnerType.RainbowMix:
                    ImSpinnerNative.SpinnerRainbowMix(label, radius, thickness, spinnerTint);
                    break;
                case SpinnerType.Ang8:
                    ImSpinnerNative.SpinnerAng8(label, radius, thickness, spinnerTint);
                    break;
                case SpinnerType.Clock:
                    ImSpinnerNative.SpinnerClock(label, radius, thickness, spinnerTint);
                    break;
                case SpinnerType.Pulsar:
                    ImSpinnerNative.SpinnerPulsar(label, radius, thickness, spinnerTint);
                    break;
                case SpinnerType.Atom:
                    ImSpinnerNative.SpinnerAtom(label, radius, thickness, spinnerTint);
                    break;
                case SpinnerType.DotsToBar:
                    ImSpinnerNative.SpinnerDotsToBar(label, radius, thickness, spinnerTint);
                    break;
                case SpinnerType.SwingDots:
                    ImSpinnerNative.SpinnerSwingDots(label, radius, thickness, spinnerTint);
                    break;
                case SpinnerType.DnaDots:
                    ImSpinnerNative.SpinnerDnaDots(label, radius, thickness, spinnerTint);
                    break;
                case SpinnerType.FadeBars:
                    ImSpinnerNative.SpinnerFadeBars(label, radius, thickness, spinnerTint);
                    break;
                case SpinnerType.MorphShape:
                    ImSpinnerNative.SpinnerMorphShape(label, radius, thickness, spinnerTint);
                    break;
                case SpinnerType.FlipTriangle:
                    ImSpinnerNative.SpinnerFlipTriangle(label, radius, thickness, spinnerTint);
                    break;
                case SpinnerType.FoldSquare:
                    ImSpinnerNative.SpinnerFoldSquare(label, radius, thickness, spinnerTint);
                    break;
                case SpinnerType.Pinwheel:
                    ImSpinnerNative.SpinnerPinwheel(label, radius, thickness, spinnerTint);
                    break;
                case SpinnerType.CornerSquares:
                    ImSpinnerNative.SpinnerCornerSquares(label, radius, thickness, spinnerTint);
                    break;
                case SpinnerType.SplitSquare:
                    ImSpinnerNative.SpinnerSplitSquare(label, radius, thickness, spinnerTint);
                    break;
            }
        }
    }
}
