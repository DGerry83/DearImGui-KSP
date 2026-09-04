using System;
using System.Runtime.InteropServices;

namespace DearImGuiKSP.Interop
{
    /// <summary>
    /// Blittable mirror of cimgui's <c>ImColor_c</c> (<c>struct { ImVec4_c Value; }</c>,
    /// cimgui.h:1348-1351), which is what the generated cimspinner ABI passes by value
    /// for every <c>const ImColor</c> parameter (I-07 discipline: small structs by value,
    /// mirrored field-for-field). 16 bytes (4 floats), sequential default layout —
    /// safe on Win64 Cdecl.
    /// </summary>
    internal struct ImSpinnerColor
    {
        public ImVec4 Value;

        public ImSpinnerColor(ImVec4 value)
        {
            Value = value;
        }
    }

    /// <summary>
    /// Raw + safe P/Invoke surface for cimspinner (chunk C16, M5 spinner widgets).
    /// Same seam as <see cref="ImPlotNative"/> (C13): implicit
    /// <c>[DllImport("DearImGuiKSPNative")]</c> resolves against the module NativeBridge
    /// already LoadLibrary'd. Only the Ex variants of the 15 curated spinners are bound
    /// (the short variants omit color; Ex carries the full parameter list, with the
    /// upstream C++ defaults applied as constants inside each safe wrapper). cimspinner
    /// <c>bool</c> is the C++ 1-byte bool, so it marshals as
    /// <see cref="UnmanagedType.I1"/>; <c>size_t</c> marshals as <see cref="UIntPtr"/>.
    /// C9 owns ImGuiNative.cs / ImGuiInternal.cs, so this file is self-contained and
    /// layering-clean: it knows nothing of the facade — the public SpinnerType enum
    /// dispatch lives in the facade (C16 contract), which calls the safe wrappers
    /// below. No string ever enters this layer: the native label parameter is a
    /// shared static empty buffer (see EmptyLabel below), so the whole path is
    /// allocation-free.
    /// </summary>
    internal static class ImSpinnerNative
    {
        private const string Dll = "DearImGuiKSPNative";

        // Upstream ImSpinner color constants (vendor/imspinner/imspinner.h:49-50).
        private static readonly ImSpinnerColor White = new ImSpinnerColor(new ImVec4(1f, 1f, 1f, 1f));
        private static readonly ImSpinnerColor HalfWhite = new ImSpinnerColor(new ImVec4(1f, 1f, 1f, 0.5f));

        // Shared label for the no-label path. Spinners use the label only for a
        // state-free ImGuiID (vendor/imspinner/imspinner.h:110) — they hold no per-ID
        // state, so an empty shared ID is layout-safe and keeps the path allocation-free.
        private static readonly byte[] EmptyLabel = { 0 };

        // --- Curated spinner externs (15), each cited to vendor/cimspinner ---
        // Common parameter mapping (contract C16): label = shared empty ID (see above);
        // radius/thickness are forwarded by the safe wrappers below (FadeBars forwards
        // radius as its width parameter w); every ImColor comes from the nullable tint
        // (null = the upstream default color for that spinner, applied in the wrapper);
        // every other bespoke upstream argument is a wrapper constant equal to the
        // upstream C++ default cited at each wrapper.

        // CIMSPINNER_API void SpinnerRainbowMixEx(const char *label, float radius, float thickness, const ImColor color, float speed, float ang_min, float ang_max, int arcs, int mode);
        // (vendor/cimspinner/cimspinner.h:125)
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern void SpinnerRainbowMixEx(
            [In] byte[] label, float radius, float thickness, ImSpinnerColor color,
            float speed, float ang_min, float ang_max, int arcs, int mode);

        // CIMSPINNER_API void SpinnerAng8Ex(const char *label, float radius, float thickness, const ImColor color, const ImColor bg, float speed, float angle, int mode, float rkoef);
        // (vendor/cimspinner/cimspinner.h:131)
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern void SpinnerAng8Ex(
            [In] byte[] label, float radius, float thickness, ImSpinnerColor color, ImSpinnerColor bg,
            float speed, float angle, int mode, float rkoef);

        // CIMSPINNER_API void SpinnerClockEx(const char *label, float radius, float thickness, const ImColor color, const ImColor bg, float speed);
        // (vendor/cimspinner/cimspinner.h:137)
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern void SpinnerClockEx(
            [In] byte[] label, float radius, float thickness, ImSpinnerColor color, ImSpinnerColor bg,
            float speed);

        // CIMSPINNER_API void SpinnerPulsarEx(const char *label, float radius, float thickness, const ImColor bg, float speed, bool sequence, float angle, int mode);
        // (vendor/cimspinner/cimspinner.h:139)
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern void SpinnerPulsarEx(
            [In] byte[] label, float radius, float thickness, ImSpinnerColor bg,
            float speed, [MarshalAs(UnmanagedType.I1)] bool sequence, float angle, int mode);

        // CIMSPINNER_API void SpinnerAtomEx(const char *label, float radius, float thickness, const ImColor color, float speed, int elipses);
        // (vendor/cimspinner/cimspinner.h:262)
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern void SpinnerAtomEx(
            [In] byte[] label, float radius, float thickness, ImSpinnerColor color,
            float speed, int elipses);

        // CIMSPINNER_API void SpinnerDotsToBarEx(const char *label, float radius, float thickness, float offset_k, const ImColor color, float speed, size_t dots);
        // (vendor/cimspinner/cimspinner_dots.h:91)
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern void SpinnerDotsToBarEx(
            [In] byte[] label, float radius, float thickness, float offset_k, ImSpinnerColor color,
            float speed, UIntPtr dots);

        // CIMSPINNER_API void SpinnerSwingDotsEx(const char *label, float radius, float thickness, const ImColor color, float speed);
        // (vendor/cimspinner/cimspinner_dots.h:131)
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern void SpinnerSwingDotsEx(
            [In] byte[] label, float radius, float thickness, ImSpinnerColor color, float speed);

        // CIMSPINNER_API void SpinnerDnaDotsEx(const char *label, float radius, float thickness, const ImColor color, float speed, int lt, float delta, bool mode);
        // (vendor/cimspinner/cimspinner_dots.h:133)
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern void SpinnerDnaDotsEx(
            [In] byte[] label, float radius, float thickness, ImSpinnerColor color,
            float speed, int lt, float delta, [MarshalAs(UnmanagedType.I1)] bool mode);

        // CIMSPINNER_API void SpinnerFadeBarsEx(const char *label, float w, const ImColor color, float speed, size_t bars, bool scale);
        // (vendor/cimspinner/cimspinner_bars.h:79)
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern void SpinnerFadeBarsEx(
            [In] byte[] label, float w, ImSpinnerColor color, float speed,
            UIntPtr bars, [MarshalAs(UnmanagedType.I1)] bool scale);

        // CIMSPINNER_API void SpinnerMorphShapeEx(const char *label, float radius, float thickness, const ImColor color, float speed, int mode);
        // (vendor/cimspinner/cimspinner_shapes.h:54)
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern void SpinnerMorphShapeEx(
            [In] byte[] label, float radius, float thickness, ImSpinnerColor color, float speed, int mode);

        // CIMSPINNER_API void SpinnerFlipTriangleEx(const char *label, float radius, float thickness, const ImColor color, float speed, int mode);
        // (vendor/cimspinner/cimspinner_shapes.h:56)
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern void SpinnerFlipTriangleEx(
            [In] byte[] label, float radius, float thickness, ImSpinnerColor color, float speed, int mode);

        // CIMSPINNER_API void SpinnerFoldSquareEx(const char *label, float radius, float thickness, const ImColor color, float speed, int mode);
        // (vendor/cimspinner/cimspinner_shapes.h:58)
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern void SpinnerFoldSquareEx(
            [In] byte[] label, float radius, float thickness, ImSpinnerColor color, float speed, int mode);

        // CIMSPINNER_API void SpinnerPinwheelEx(const char *label, float radius, float thickness, const ImColor color, float speed, int mode);
        // (vendor/cimspinner/cimspinner_shapes.h:60)
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern void SpinnerPinwheelEx(
            [In] byte[] label, float radius, float thickness, ImSpinnerColor color, float speed, int mode);

        // CIMSPINNER_API void SpinnerCornerSquaresEx(const char *label, float radius, float thickness, const ImColor color, float speed, int mode);
        // (vendor/cimspinner/cimspinner_shapes.h:62)
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern void SpinnerCornerSquaresEx(
            [In] byte[] label, float radius, float thickness, ImSpinnerColor color, float speed, int mode);

        // CIMSPINNER_API void SpinnerSplitSquareEx(const char *label, float radius, float thickness, const ImColor color, float speed, int mode);
        // (vendor/cimspinner/cimspinner_shapes.h:64)
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern void SpinnerSplitSquareEx(
            [In] byte[] label, float radius, float thickness, ImSpinnerColor color, float speed, int mode);

        // --- Safe wrappers: one per curated spinner, upstream defaults baked in ---
        // tint null = the upstream default color for that spinner. All defaults cited
        // to the vendored imspinner headers.

        /// <summary>SpinnerRainbowMix (imspinner.h:331; upstream defines no color/speed defaults — 2.8f is the library-standard speed; sweep 0..2π, 1 arc, mode 0).</summary>
        internal static void SpinnerRainbowMix(float radius, float thickness, ImSpinnerColor? tint)
        {
            SpinnerRainbowMixEx(EmptyLabel, radius, thickness, tint ?? White, 2.8f, 0f, 6.2831855f, 1, 0);
        }

        /// <summary>SpinnerAng8 (imspinner.h:387: bg white, speed 2.8f, angle π, mode 0, rkoef 0.5f).</summary>
        internal static void SpinnerAng8(float radius, float thickness, ImSpinnerColor? tint)
        {
            SpinnerAng8Ex(EmptyLabel, radius, thickness, tint ?? White, White, 2.8f, 3.1415927f, 0, 0.5f);
        }

        /// <summary>SpinnerClock (imspinner.h:484: bg half-white, speed 2.8f).</summary>
        internal static void SpinnerClock(float radius, float thickness, ImSpinnerColor? tint)
        {
            SpinnerClockEx(EmptyLabel, radius, thickness, tint ?? White, HalfWhite, 2.8f);
        }

        /// <summary>SpinnerPulsar (imspinner.h:497: this spinner's only color is its bg ring; default half-white, speed 2.8f, sequence on, angle 0, mode 0).</summary>
        internal static void SpinnerPulsar(float radius, float thickness, ImSpinnerColor? tint)
        {
            SpinnerPulsarEx(EmptyLabel, radius, thickness, tint ?? HalfWhite, 2.8f, true, 0f, 0);
        }

        /// <summary>SpinnerAtom (imspinner.h:2535: speed 2.8f, 3 ellipses).</summary>
        internal static void SpinnerAtom(float radius, float thickness, ImSpinnerColor? tint)
        {
            SpinnerAtomEx(EmptyLabel, radius, thickness, tint ?? White, 2.8f, 3);
        }

        /// <summary>SpinnerDotsToBar (imspinner_dots.h:88: offset_k 0.5f — no upstream default, the upstream demo value (imspinner_demo.h:279); speed 2.8f, 5 dots).</summary>
        internal static void SpinnerDotsToBar(float radius, float thickness, ImSpinnerColor? tint)
        {
            SpinnerDotsToBarEx(EmptyLabel, radius, thickness, 0.5f, tint ?? White, 2.8f, new UIntPtr(5));
        }

        /// <summary>SpinnerSwingDots (imspinner_dots.h:606: speed 2.8f).</summary>
        internal static void SpinnerSwingDots(float radius, float thickness, ImSpinnerColor? tint)
        {
            SpinnerSwingDotsEx(EmptyLabel, radius, thickness, tint ?? White, 2.8f);
        }

        /// <summary>SpinnerDnaDots (imspinner_dots.h:636: speed 2.8f, 8 rungs, delta 0.5f, mode off).</summary>
        internal static void SpinnerDnaDots(float radius, float thickness, ImSpinnerColor? tint)
        {
            SpinnerDnaDotsEx(EmptyLabel, radius, thickness, tint ?? White, 2.8f, 8, 0.5f, false);
        }

        /// <summary>SpinnerFadeBars (imspinner_bars.h:20: radius forwarded as w — this spinner has no thickness parameter; speed 2.8f, 3 bars, no scale).</summary>
        internal static void SpinnerFadeBars(float radius, float thickness, ImSpinnerColor? tint)
        {
            SpinnerFadeBarsEx(EmptyLabel, radius, tint ?? White, 2.8f, new UIntPtr(3), false);
        }

        /// <summary>SpinnerMorphShape (imspinner_shapes.h:22: speed 1f, mode 0).</summary>
        internal static void SpinnerMorphShape(float radius, float thickness, ImSpinnerColor? tint)
        {
            SpinnerMorphShapeEx(EmptyLabel, radius, thickness, tint ?? White, 1f, 0);
        }

        /// <summary>SpinnerFlipTriangle (imspinner_shapes.h:97: speed 1f, mode 0).</summary>
        internal static void SpinnerFlipTriangle(float radius, float thickness, ImSpinnerColor? tint)
        {
            SpinnerFlipTriangleEx(EmptyLabel, radius, thickness, tint ?? White, 1f, 0);
        }

        /// <summary>SpinnerFoldSquare (imspinner_shapes.h:127: speed 1f, mode 0).</summary>
        internal static void SpinnerFoldSquare(float radius, float thickness, ImSpinnerColor? tint)
        {
            SpinnerFoldSquareEx(EmptyLabel, radius, thickness, tint ?? White, 1f, 0);
        }

        /// <summary>SpinnerPinwheel (imspinner_shapes.h:183: speed 1f, mode 0).</summary>
        internal static void SpinnerPinwheel(float radius, float thickness, ImSpinnerColor? tint)
        {
            SpinnerPinwheelEx(EmptyLabel, radius, thickness, tint ?? White, 1f, 0);
        }

        /// <summary>SpinnerCornerSquares (imspinner_shapes.h:225: speed 1f, mode 0).</summary>
        internal static void SpinnerCornerSquares(float radius, float thickness, ImSpinnerColor? tint)
        {
            SpinnerCornerSquaresEx(EmptyLabel, radius, thickness, tint ?? White, 1f, 0);
        }

        /// <summary>SpinnerSplitSquare (imspinner_shapes.h:263: speed 1f, mode 0).</summary>
        internal static void SpinnerSplitSquare(float radius, float thickness, ImSpinnerColor? tint)
        {
            SpinnerSplitSquareEx(EmptyLabel, radius, thickness, tint ?? White, 1f, 0);
        }
    }
}
