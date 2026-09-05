using System;
using UnityEngine;
using Color32 = UnityEngine.Color32;

namespace DearImGuiKSPDemo.Telemetry
{
    /// <summary>
    /// Stages tab content (spec §5.5, §6.2, §6.3; chunk C20, M6): per stage a
    /// tweened propellant meter plus an inline readout line (approx dV / approx
    /// Isp / burn time), then a stacked summary bar of per-stage approx-dV
    /// proportions. All ImGui calls go through the library's public API only
    /// (<see cref="DearImGuiKSP.DearImGuiKSP"/>, <see cref="DearImGuiKSP.ImGuiDraw"/>,
    /// <see cref="DearImGuiKSP.ImGuiGradients"/>, <see cref="DearImGuiKSP.Tween"/>).
    /// </summary>
    /// <remarks>
    /// Allocation story (§5.9 hot-path discipline): the per-frame path allocates
    /// nothing — the panel reads the analyzer's immutable snapshot, every readout
    /// string is preformatted when a NEW snapshot reference appears (recompute
    /// cadence, ~1 Hz, the same "may allocate" class as the analyzer), the meter
    /// and summary-bar geometry is struct math, and the tween setters are cached
    /// once at construction. Every figure is labeled "approx" (spec §3.2; D31
    /// ASCII "dV", no symbol glyphs). Only called when the Stages tab is visible;
    /// the addon owns the "No active vessel." degradation (C18 tab-slot contract)
    /// and the panel repeats it so the tab slot stays self-contained.
    /// </remarks>
    internal sealed class StagePanel
    {
        private const string NoVesselText = "No active vessel.";
        private const string NoStagesText = "No stage data.";

        // Meter geometry (pixels). Fixed width keeps the per-frame path free of
        // content-region queries; the tab is wide enough for the telemetry window.
        private const float MeterWidth = 360f;
        private const float MeterHeight = 14f;
        private const float MeterRounding = 3f;
        private const float SummaryHeight = 10f;
        private const float SummaryWidth = 360f;

        // Fraction-to-color mapping thresholds: green (well stocked) -> orange
        // (running down) -> red (nearly dry), spec §6.3.
        private const float OrangeBelow = 0.5f;
        private const float RedBelow = 0.2f;

        // Meter fill tween: fast enough to settle within the analyzer's 1 s
        // recompute cadence, slow enough to read as a transition.
        private const float ColorTweenSeconds = 0.45f;

        // Fixed tween-slot pool: stage rows beyond the pool snap to their target
        // color instead of tweening (noted graceful degradation; 16 slots covers
        // any plausible KSP stack). Slots are per display row, not per stage id.
        private const int TweenSlots = 16;

        private static readonly Color32 MeterBackground = new Color32(30, 32, 38, 255);
        private static readonly Color FillGreen = new Color32(140, 220, 60, 255);
        private static readonly Color FillOrange = new Color32(255, 170, 40, 255);
        private static readonly Color FillRed = new Color32(235, 75, 60, 255);

        private readonly StageAnalyzer _analyzer;

        // Tween-slot state: the cached setter writes the tweened color into its
        // own slot (one closure per slot, allocated once at construction). Kept
        // as UnityEngine.Color because Tween.To's color overload takes
        // Action<Color>; conversions to Color32 at draw time are implicit.
        private readonly Color[] _meterColors = new Color[TweenSlots];
        private readonly Action<Color>[] _setMeterColors = new Action<Color>[TweenSlots];
        private readonly DearImGuiKSP.TweenHandle[] _meterTweens = new DearImGuiKSP.TweenHandle[TweenSlots];

        // Render cache, rebuilt only when a new snapshot reference appears.
        private StageRow[] _rows = new StageRow[0];
        private string _totalLabel = "Total approx dV: 0 m/s";
        private StageAnalyzer.Snapshot _lastSnapshot;

        /// <summary>One cached display row: preformatted label + fraction-derived geometry.</summary>
        private struct StageRow
        {
            public string Label;
            public float Fraction;
            public float SegmentWidth;
        }

        /// <summary>
        /// Creates the panel and caches the tween setter delegates (one per slot,
        /// a single allocation each at construction — never per frame).
        /// </summary>
        public StagePanel(StageAnalyzer analyzer)
        {
            _analyzer = analyzer;
            for (int i = 0; i < TweenSlots; i++)
            {
                int slot = i;
                _setMeterColors[i] = delegate(Color value) { _meterColors[slot] = value; };
                _meterColors[i] = FillGreen;
            }
        }

        /// <summary>
        /// Content of the Stages tab. Call inside a tab item scope whose Visible is
        /// true; handles the no-vessel / no-stage degradation itself.
        /// </summary>
        public void DrawImGui()
        {
            StageAnalyzer.Snapshot snapshot = _analyzer.Current;
            if (snapshot == null || !snapshot.HasVessel)
            {
                DearImGuiKSP.DearImGuiKSP.Text(NoVesselText);
                return;
            }
            if (!object.ReferenceEquals(snapshot, _lastSnapshot))
            {
                RebuildCache(snapshot);
            }
            if (_rows.Length == 0)
            {
                DearImGuiKSP.DearImGuiKSP.Text(NoStagesText);
                return;
            }

            for (int i = 0; i < _rows.Length; i++)
            {
                DearImGuiKSP.DearImGuiKSP.Text(_rows[i].Label);
                DrawMeter(_rows[i].Fraction, MeterColorAt(i));
            }

            DearImGuiKSP.DearImGuiKSP.Text(_totalLabel);
            DrawSummaryBar();
        }

        // Rebuilds the render cache for a fresh snapshot: preformatted readout
        // strings (the only allocations here, at recompute cadence), summary-bar
        // segment widths, and the per-slot color tweens toward the new
        // fraction-driven target. Runs only on snapshot-reference change.
        private void RebuildCache(StageAnalyzer.Snapshot snapshot)
        {
            _lastSnapshot = snapshot;
            StageAnalyzer.StageInfo[] stages = snapshot.Stages;
            if (_rows.Length != stages.Length)
            {
                _rows = new StageRow[stages.Length];
            }

            double total = snapshot.TotalApproxDeltaV;
            for (int i = 0; i < stages.Length; i++)
            {
                StageAnalyzer.StageInfo stage = stages[i];
                _rows[i].Fraction = (float)stage.PropellantFraction;
                _rows[i].SegmentWidth = total > 0.0
                    ? (float)(stage.ApproxDeltaV / total) * SummaryWidth
                    : 0f;
                _rows[i].Label = stage.HasEngines
                    ? string.Format(
                        "Stage {0}: approx dV {1:0} m/s, approx Isp {2:0} s, burn {3:0.0} s",
                        stage.Stage, stage.ApproxDeltaV, stage.ApproxIsp, stage.BurnTimeSeconds)
                    : string.Format("Stage {0}: no engines", stage.Stage);

                Color target = ColorForFraction(_rows[i].Fraction);
                if (i < TweenSlots)
                {
                    _meterTweens[i].Cancel();
                    _meterTweens[i] = DearImGuiKSP.Tween.To(
                        _setMeterColors[i], _meterColors[i], target,
                        ColorTweenSeconds, DearImGuiKSP.Ease.QuadInOut);
                }
                // Rows beyond the tween pool keep no slot state; MeterColorAt
                // resolves them straight from the fraction — snap, never throws.
            }

            _totalLabel = string.Format("Total approx dV: {0:0} m/s", total);
        }

        // One meter: reserve the layout slot, paint the background, then the
        // fraction-proportional gradient fill in the (possibly tweened) color.
        // Struct math only — nothing here allocates.
        private static void DrawMeter(float fraction, Color color)
        {
            Vector2 origin = DearImGuiKSP.ImGuiDraw.GetCursorScreenPos();
            DearImGuiKSP.ImGuiDraw.Dummy(MeterWidth, MeterHeight);
            DearImGuiKSP.ImGuiDraw.AddRectFilled(
                origin,
                new Vector2(origin.x + MeterWidth, origin.y + MeterHeight),
                MeterBackground,
                MeterRounding);
            if (fraction > 0f)
            {
                DearImGuiKSP.ImGuiGradients.AddRectFilledGradientVertical(
                    origin,
                    new Vector2(origin.x + MeterWidth * fraction, origin.y + MeterHeight),
                    color,
                    Darken(color),
                    MeterRounding);
            }
        }

        // Stacked summary bar: one rect per stage, width proportional to its
        // share of total approx dV, colors matching the meters.
        private void DrawSummaryBar()
        {
            Vector2 origin = DearImGuiKSP.ImGuiDraw.GetCursorScreenPos();
            DearImGuiKSP.ImGuiDraw.Dummy(SummaryWidth, SummaryHeight);
            float x = origin.x;
            for (int i = 0; i < _rows.Length; i++)
            {
                float w = _rows[i].SegmentWidth;
                if (w <= 0f)
                {
                    continue;
                }
                DearImGuiKSP.ImGuiDraw.AddRectFilled(
                    new Vector2(x, origin.y),
                    new Vector2(x + w, origin.y + SummaryHeight),
                    MeterColorAt(i));
                x += w;
            }
        }

        private Color MeterColorAt(int row)
        {
            return row < TweenSlots ? _meterColors[row] : ColorForFraction(_rows[row].Fraction);
        }

        // Green -> orange -> red as the fraction drops (spec §6.3).
        private static Color ColorForFraction(float fraction)
        {
            if (fraction < RedBelow)
            {
                return FillRed;
            }
            if (fraction < OrangeBelow)
            {
                return FillOrange;
            }
            return FillGreen;
        }

        // Gradient stop math on structs: bottom stop at 70% brightness.
        private static Color Darken(Color c)
        {
            return new Color(c.r * 0.7f, c.g * 0.7f, c.b * 0.7f, c.a);
        }
    }
}
