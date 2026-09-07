using UnityEngine;

namespace DearImGuiKSPDemo.Telemetry
{
    /// <summary>
    /// Graphs tab content (spec §5.5, §6.2; chunk C19, M6): a 2x2 ImPlot subplot
    /// grid fed by the C18 sampler's four ring buffers — altitude (m), dynamic
    /// pressure (kPa), main throttle (0–1), and g-force — one rolling line series per
    /// cell. All ImGui calls go through the library's public API only.
    /// </summary>
    /// <remarks>
    /// Allocation story (§5.9 hot-path discipline): the steady-state unhovered path
    /// allocates nothing per frame — the series spans come from the ring buffers'
    /// reused scratch arrays (C18 mechanism, one in-place copy), every title and
    /// series label is a constant, <see cref="DearImGuiKSP.ImGuiPlot.PlotLine(string, System.ReadOnlySpan{float})"/>
    /// pins the buffer in place (its only allocation is the shared ToUtf8 label
    /// convention), the hover capture is a struct assignment, and the per-cell draw
    /// is a static method (no closure). The one documented exception is the hover
    /// readout line: while the cursor is inside a cell, one
    /// <see cref="string.Format(string, object[])"/> builds its text — user-driven,
    /// bounded to one hovered cell at a time, nothing on the unhovered path.
    /// The readout renders as a <c>Text</c> line under the grid rather than a floating
    /// tooltip because the public API exposes no tooltip/overlay-text primitive.
    /// A throttle dial under the grid is the input-direction counterpart to the
    /// throttle graph cell: a Wiper-variant Knob bound two-way to
    /// <c>FlightInputHandler.state.mainThrottle</c> (seed a local each frame, write
    /// back only when the knob reports a change). Its percent readout is a cached
    /// string reformatted only when the value moves (preformat-on-change
    /// pattern); the steady-state unhovered, undragged path still allocates nothing.
    /// Empty cells (first frames, before the ring has samples) draw empty axes:
    /// <see cref="DearImGuiKSP.ImGuiPlot.PlotLine(string, System.ReadOnlySpan{float})"/>
    /// no-ops on an empty span, and the hover readout is suppressed until the cell
    /// has data. Only called when a vessel is active — the addon owns the
    /// "No active vessel." degradation (C18 tab-slot contract).
    /// </remarks>
    internal sealed class GraphPanel
    {
        private const string SubplotId = "##telemetry_graphs";
        private const string AltitudeTitle = "##tele_alt";
        private const string AltitudeLabel = "Altitude (m)";
        private const string DynPressureTitle = "##tele_dynp";
        private const string DynPressureLabel = "Dyn pressure (kPa)";
        private const string ThrottleTitle = "##tele_thr";
        private const string ThrottleLabel = "Throttle";
        private const string ThrottleDialId = "Throttle##input";
        private const string ThrottleReadoutFormat = "Throttle: {0:0}%";
        private const string GForceTitle = "##tele_g";
        private const string GForceLabel = "G-force";

        // Fixed grid size (cells share it evenly — the ratio pointers stay null).
        // The cell-size argument of ImGuiPlot.Begin is ignored inside a subplot
        // context (implot.h:818-821); Vector2.zero is the conventional value.
        private static readonly Vector2 GridSize = new Vector2(520f, 360f);
        private static readonly Vector2 IgnoredCellSize = Vector2.zero;

        // Throttle dial geometry: compact enough to sit under the grid without
        // restructuring the tab.
        private const float ThrottleDialSize = 48f;

        // Rolling plot window (ISSUES #007): plotting the full 10k-sample ring let
        // per-frame cost grow with total recorded data (~7 ms by late flight); a
        // 1200-sample window (~20 s at 60 Hz sampling) keeps ImPlot's segment
        // submission and the scratch copy constant-cost, and keeps auto-fit cheap
        // and readable. History depth is unaffected — the full ring stays recorded.
        private const int WindowSamples = 1200;

        private readonly TelemetrySampler _sampler;

        // Throttle readout cache: formatted only when the value changes (drag or
        // keyboard input), never per frame (preformat-on-change pattern).
        private float _lastReadoutThrottle = -1f;
        private string _throttleReadout = "Throttle: 0%";

        public GraphPanel(TelemetrySampler sampler)
        {
            _sampler = sampler;
        }

        /// <summary>
        /// Content of the Graphs tab: declares the 2x2 grid and the four rolling
        /// series. Call inside a tab item scope whose Visible is true and only when a
        /// vessel is active (the addon owns the no-vessel placeholder).
        /// </summary>
        public void DrawImGui()
        {
            HoverReadout hover = default(HoverReadout);
            using (var grid = DearImGuiKSP.ImGuiPlot.BeginSubplots(SubplotId, 2, 2, GridSize))
            {
                if (grid.Visible)
                {
                    DrawCell(AltitudeTitle, AltitudeLabel, _sampler.Altitude, ref hover);
                    DrawCell(DynPressureTitle, DynPressureLabel, _sampler.DynamicPressurekPa, ref hover);
                    DrawCell(ThrottleTitle, ThrottleLabel, _sampler.MainThrottle, ref hover);
                    DrawCell(GForceTitle, GForceLabel, _sampler.GeeForce, ref hover);
                }
            }

            // Throttle dial under the grid: reads and writes the game's throttle
            // (seed local from game state, write back only when the knob returns
            // changed), so keyboard throttle changes move the dial and drags take
            // effect in-game.
            DrawThrottleDial();

            // Hover-only readout: one formatted line for the single hovered cell.
            // This string.Format is the documented exception to the no-alloc rule —
            // it runs only while the user hovers a cell that has data.
            if (hover.Label != null)
            {
                DearImGuiKSP.DearImGuiKSP.Text(
                    string.Format("{0}: {1:0.###} (sample {2:0})", hover.Label, hover.Y, hover.X));
            }
        }

        // Two-way throttle dial bound to FlightInputHandler.state.mainThrottle:
        // seed a local from the game state each frame so keyboard input (Z/X)
        // is reflected, and write back only when Knob reports a change (a user
        // drag). The ref target is the local, never the game field. The percent
        // readout is reformatted only when the value moved. No-ops without a
        // FlightCtrlState, mirroring the sampler's null guard.
        private void DrawThrottleDial()
        {
            FlightCtrlState state = FlightInputHandler.state;
            if (state == null)
            {
                return;
            }
            float throttle = state.mainThrottle;
            if (DearImGuiKSP.DearImGuiKSP.Knob(
                ThrottleDialId,
                ref throttle,
                0f,
                1f,
                0f,
                DearImGuiKSP.KnobVariant.Wiper,
                ThrottleDialSize,
                DearImGuiKSP.KnobFlags.None,
                10))
            {
                state.mainThrottle = throttle;
            }
            if (throttle != _lastReadoutThrottle)
            {
                _lastReadoutThrottle = throttle;
                _throttleReadout = string.Format(ThrottleReadoutFormat, throttle * 100f);
            }
            DearImGuiKSP.DearImGuiKSP.Text(_throttleReadout);
        }

        // One grid cell: a plot plus its ring-fed series, with hover capture. Static
        // and ref-parameterized so the per-frame path builds no closure and allocates
        // nothing; the hover capture is a struct assignment, valid only when Label
        // was set (a cell with no data never captures).
        private static void DrawCell(string title, string label, RingBuffer ring, ref HoverReadout hover)
        {
            using (var plot = DearImGuiKSP.ImGuiPlot.Begin(title, IgnoredCellSize))
            {
                if (!plot.Visible)
                {
                    return;
                }
                DearImGuiKSP.ImGuiPlot.SetupAxesAutoFit();
                DearImGuiKSP.ImGuiPlot.PlotLine(label, ring.LatestOldestFirst(WindowSamples));
                if (ring.Count > 0 && DearImGuiKSP.ImGuiPlot.IsPlotHovered())
                {
                    Vector2 pos = DearImGuiKSP.ImGuiPlot.GetPlotMousePos();
                    hover.Label = label;
                    hover.X = pos.x;
                    hover.Y = pos.y;
                }
            }
        }

        // Latest hovered-cell capture. A struct (no allocation); Label == null means
        // no cell is hovered this frame. Set only from the hovered path in DrawCell.
        private struct HoverReadout
        {
            public string Label;
            public float X;
            public float Y;
        }
    }
}
