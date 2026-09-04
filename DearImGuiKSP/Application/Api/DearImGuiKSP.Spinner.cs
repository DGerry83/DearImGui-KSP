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
        /// Constants: speed 2.8f, 3 ellipses.
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
        /// <summary>
        /// Draws an animated spinner (vendored dalerank/imspinner) of the given
        /// type. Spinners indicate ongoing work; they animate themselves natively
        /// from the ImGui clock and have no interaction or state. Only valid
        /// inside a registered callback.
        /// </summary>
        /// <param name="type">Which spinner to draw; see <see cref="SpinnerType"/>.</param>
        /// <param name="radius">Widget radius in pixels (the FadeBars type uses it as row width).</param>
        /// <param name="thickness">Line thickness in pixels (unused by FadeBars).</param>
        /// <param name="tint">
        /// Optional color override; null uses the spinner's upstream default
        /// (white for most types, half-white for Clock and Pulsar backgrounds —
        /// see the <see cref="SpinnerType"/> value docs).
        /// </param>
        public static void Spinner(SpinnerType type, float radius, float thickness, Color? tint = null)
        {
            if (!IsAvailable)
            {
                return;
            }
            // Nullable<ImSpinnerColor> is a stack value type; null = "upstream
            // default color" resolved per spinner in ImSpinnerNative (hot-path
            // checklist Check 2 — zero per-frame allocation, pure value-type
            // enum dispatch).
            ImSpinnerColor? spinnerTint = tint.HasValue
                ? new ImSpinnerColor(ToImVec4(tint.Value))
                : (ImSpinnerColor?)null;
            switch (type)
            {
                case SpinnerType.RainbowMix:
                    ImSpinnerNative.SpinnerRainbowMix(radius, thickness, spinnerTint);
                    break;
                case SpinnerType.Ang8:
                    ImSpinnerNative.SpinnerAng8(radius, thickness, spinnerTint);
                    break;
                case SpinnerType.Clock:
                    ImSpinnerNative.SpinnerClock(radius, thickness, spinnerTint);
                    break;
                case SpinnerType.Pulsar:
                    ImSpinnerNative.SpinnerPulsar(radius, thickness, spinnerTint);
                    break;
                case SpinnerType.Atom:
                    ImSpinnerNative.SpinnerAtom(radius, thickness, spinnerTint);
                    break;
                case SpinnerType.DotsToBar:
                    ImSpinnerNative.SpinnerDotsToBar(radius, thickness, spinnerTint);
                    break;
                case SpinnerType.SwingDots:
                    ImSpinnerNative.SpinnerSwingDots(radius, thickness, spinnerTint);
                    break;
                case SpinnerType.DnaDots:
                    ImSpinnerNative.SpinnerDnaDots(radius, thickness, spinnerTint);
                    break;
                case SpinnerType.FadeBars:
                    ImSpinnerNative.SpinnerFadeBars(radius, thickness, spinnerTint);
                    break;
                case SpinnerType.MorphShape:
                    ImSpinnerNative.SpinnerMorphShape(radius, thickness, spinnerTint);
                    break;
                case SpinnerType.FlipTriangle:
                    ImSpinnerNative.SpinnerFlipTriangle(radius, thickness, spinnerTint);
                    break;
                case SpinnerType.FoldSquare:
                    ImSpinnerNative.SpinnerFoldSquare(radius, thickness, spinnerTint);
                    break;
                case SpinnerType.Pinwheel:
                    ImSpinnerNative.SpinnerPinwheel(radius, thickness, spinnerTint);
                    break;
                case SpinnerType.CornerSquares:
                    ImSpinnerNative.SpinnerCornerSquares(radius, thickness, spinnerTint);
                    break;
                case SpinnerType.SplitSquare:
                    ImSpinnerNative.SpinnerSplitSquare(radius, thickness, spinnerTint);
                    break;
            }
        }
    }
}
