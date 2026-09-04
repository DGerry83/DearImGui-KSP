using System;
using UnityEngine;

namespace DearImGuiKSPDemo
{
    /// <summary>
    /// M5 widget showcase section (C17), folded into the existing demo window
    /// (spec §6.2): a row of animated spinners (C16), two rotary knobs of
    /// different variants (C15), horizontal + vertical rolling wheels (C15),
    /// and a tween demo (C14) — a float tween driving the Tick knob and a
    /// Color tween driving a TextColored header, both cancellable through
    /// their TweenHandles. Public facade API only; all state lives in fields,
    /// all labels are constants, and tween delegates are cached in fields, so
    /// the steady-state per-frame path allocates nothing beyond the shared
    /// ToUtf8 label conversion every facade call performs (PlotDemo
    /// precedent).
    /// </summary>
    internal sealed class ThemeDemo
    {
        private const string SectionHeaderText = "Widget showcase";
        private const string SpinnerCaptionText = "Spinners (animated by the native ImGui clock):";
        private const string SpinnerRainbowLabel = "RainbowMix";
        private const string SpinnerAng8Label = "Ang8";
        private const string SpinnerClockLabel = "Clock";
        private const string SpinnerAtomLabel = "Atom (tinted)";
        private const string TweenKnobLabel = "Tween-driven (Tick)";
        private const string WiperKnobLabel = "Draggable (WiperOnly)";
        private const string HorizontalWheelLabel = "Horizontal wheel";
        private const string VerticalWheelLabel = "Vertical wheel";
        private const string TweenHeaderText = "Tween demo";
        private const string PlayTweenLabel = "Play tween";
        private const string CancelTweenLabel = "Cancel tween";
        private const string TweenStatePlaying = "Tween: playing";
        private const string TweenStateIdle = "Tween: idle";

        private const float SpinnerRadius = 12f;
        private const float SpinnerThickness = 3f;
        private const float KnobMin = 0f;
        private const float KnobMax = 100f;
        private const float WheelMin = 0f;
        private const float WheelMax = 100f;
        private const float HorizontalWheelWidth = 120f;
        private const float HorizontalWheelHeight = 24f;
        private const float TweenSeconds = 2f;

        // Tween endpoints: the KSP palette pair the header color ping-pongs
        // through (Color32 -> Color implicit conversion, done once).
        private static readonly Color TweenColorA = DearImGuiKSP.Application.KspPalette.OrangeLight;
        private static readonly Color TweenColorB = DearImGuiKSP.Application.KspPalette.GreenLight;

        // Tint proving the Spinner Color? overload (conversion once, not per frame).
        private static readonly Color SpinnerTint = DearImGuiKSP.Application.KspPalette.GreenLight;

        // Widget state — persistent fields, never locals of the per-frame path.
        private float _tweenKnobValue;
        private float _wiperKnobValue = 35f;
        private float _horizontalWheelValue = 50f;
        private float _verticalWheelValue = 50f;
        private Color _tweenHeaderColor = TweenColorA;

        // Ping-pong direction flags: each Play click flips the target.
        private bool _tweenTowardHigh;
        private bool _colorTowardB;

        // Live tween handles (readonly structs — no boxing); Cancel on an
        // inert/completed handle is a no-op, so these are safe to call blind.
        private DearImGuiKSP.TweenHandle _floatTweenHandle;
        private DearImGuiKSP.TweenHandle _colorTweenHandle;

        // Tween setters cached once as delegate fields (method-group conversion
        // allocates a single delegate at construction; the per-frame path never
        // builds a closure).
        private readonly Action<float> _setTweenValue;
        private readonly Action<Color> _setTweenColor;

        /// <summary>
        /// Creates the showcase and caches the tween setter delegates.
        /// </summary>
        public ThemeDemo()
        {
            _setTweenValue = SetTweenKnobValue;
            _setTweenColor = SetTweenHeaderColor;
        }

        /// <summary>
        /// Content of the widget showcase section: spinner row, knobs, wheels,
        /// and the tween demo. Call inside an ImGuiEx.Window scope whose
        /// Visible is true (DemoConsumer calls it inside the existing demo
        /// window, after the M3 theme showcase section).
        /// </summary>
        internal void DrawImGui()
        {
            DearImGuiKSP.DearImGuiKSP.TextColored(
                DearImGuiKSP.Application.KspPalette.OrangeLight, SectionHeaderText);

            DrawSpinnerRow();
            DrawKnobs();
            DrawWheels();
            DrawTweenDemo();
        }

        // IMPEDIMENT I-C17-01: the contract asks for the spinner row "side by
        // side" via "SameLine/layout helpers available on the public facade",
        // but the facade exposes no public SameLine (it exists only as
        // internal ImGuiInternal.SameLine, used by InputText) and the chunk
        // may not touch library internals or change the library. The row is
        // therefore stacked vertically, one labelled spinner per line.
        private void DrawSpinnerRow()
        {
            DearImGuiKSP.DearImGuiKSP.Text(SpinnerCaptionText);
            DearImGuiKSP.DearImGuiKSP.Text(SpinnerRainbowLabel);
            DearImGuiKSP.DearImGuiKSP.Spinner(
                DearImGuiKSP.SpinnerType.RainbowMix, SpinnerRadius, SpinnerThickness);
            DearImGuiKSP.DearImGuiKSP.Text(SpinnerAng8Label);
            DearImGuiKSP.DearImGuiKSP.Spinner(
                DearImGuiKSP.SpinnerType.Ang8, SpinnerRadius, SpinnerThickness);
            DearImGuiKSP.DearImGuiKSP.Text(SpinnerClockLabel);
            DearImGuiKSP.DearImGuiKSP.Spinner(
                DearImGuiKSP.SpinnerType.Clock, SpinnerRadius, SpinnerThickness);
            DearImGuiKSP.DearImGuiKSP.Text(SpinnerAtomLabel);
            DearImGuiKSP.DearImGuiKSP.Spinner(
                DearImGuiKSP.SpinnerType.Atom, SpinnerRadius, SpinnerThickness, SpinnerTint);
        }

        private void DrawKnobs()
        {
            // The Tick knob is the tween demo's visible motion target; the
            // WiperOnly knob is the manual draggable of a different variant.
            DearImGuiKSP.DearImGuiKSP.Knob(
                TweenKnobLabel, ref _tweenKnobValue, KnobMin, KnobMax,
                0f, DearImGuiKSP.KnobVariant.Tick, 0f, DearImGuiKSP.KnobFlags.None, 10);
            DearImGuiKSP.DearImGuiKSP.Knob(
                WiperKnobLabel, ref _wiperKnobValue, KnobMin, KnobMax,
                0f, DearImGuiKSP.KnobVariant.WiperOnly, 0f, DearImGuiKSP.KnobFlags.None, 10);
        }

        private void DrawWheels()
        {
            // Elongated along the drag axis; the vertical wheel swaps the box.
            DearImGuiKSP.DearImGuiKSP.Wheel(
                HorizontalWheelLabel, ref _horizontalWheelValue, WheelMin, WheelMax,
                HorizontalWheelWidth, HorizontalWheelHeight,
                DearImGuiKSP.WheelOrientation.Horizontal);
            DearImGuiKSP.DearImGuiKSP.Wheel(
                VerticalWheelLabel, ref _verticalWheelValue, WheelMin, WheelMax,
                HorizontalWheelHeight, HorizontalWheelWidth,
                DearImGuiKSP.WheelOrientation.Vertical);
        }

        private void DrawTweenDemo()
        {
            // The Color tween drives this header's color (field -> TextColored
            // each frame; no formatting, no allocation).
            DearImGuiKSP.DearImGuiKSP.TextColored(_tweenHeaderColor, TweenHeaderText);

            if (DearImGuiKSP.DearImGuiKSP.Button(PlayTweenLabel))
            {
                // Cancel-before-restart: a re-click mid-flight restarts from
                // the current value instead of stacking tweens.
                _floatTweenHandle.Cancel();
                _colorTweenHandle.Cancel();

                _tweenTowardHigh = !_tweenTowardHigh;
                _floatTweenHandle = DearImGuiKSP.Tween.To(
                    _setTweenValue, _tweenKnobValue,
                    _tweenTowardHigh ? KnobMax : KnobMin,
                    TweenSeconds, DearImGuiKSP.Ease.QuadInOut);

                _colorTowardB = !_colorTowardB;
                _colorTweenHandle = DearImGuiKSP.Tween.To(
                    _setTweenColor, _tweenHeaderColor,
                    _colorTowardB ? TweenColorB : TweenColorA,
                    TweenSeconds, DearImGuiKSP.Ease.QuadInOut);
            }
            if (DearImGuiKSP.DearImGuiKSP.Button(CancelTweenLabel))
            {
                // Cancel is a no-op on completed/inert handles — safe to press
                // when nothing is playing; the knob keeps its mid-flight value.
                _floatTweenHandle.Cancel();
                _colorTweenHandle.Cancel();
            }
            DearImGuiKSP.DearImGuiKSP.Text(
                _floatTweenHandle.IsPlaying ? TweenStatePlaying : TweenStateIdle);
        }

        private void SetTweenKnobValue(float value)
        {
            _tweenKnobValue = value;
        }

        private void SetTweenHeaderColor(Color value)
        {
            _tweenHeaderColor = value;
        }
    }
}
