using UnityEngine;

namespace DearImGuiKSPDemo
{
    /// <summary>
    /// C13/M4 ImPlot proof window: two live line plots drawn through the public
    /// <see cref="DearImGuiKSP.ImGuiPlot"/> API only — rolling frame time (ms) and
    /// smoothed FPS, one series per plot, both axes auto-fitting every frame
    /// (<see cref="DearImGuiKSP.ImGuiPlot.SetupAxesAutoFit"/>). Both series are fed once per frame from
    /// fixed-capacity ring buffers (preallocated in the constructor), so the
    /// steady-state per-frame path allocates no managed memory: ring writes and the
    /// oldest-first copy are in-place, plot labels are constants, and PlotLine pins
    /// the buffer without copying (its only allocation is the shared ToUtf8 label
    /// convention every facade call uses).
    /// </summary>
    public sealed class PlotDemo
    {
        private const int Capacity = 240;

        private const string FrameMsTitle = "Frame time (ms/frame)";
        private const string FrameMsLabel = "ms/frame";
        private const string FpsTitle = "Smoothed FPS";
        private const string FpsLabel = "fps";
        private const string ResizeNote = "Auto-sizing off — drag the corner or edge to resize.";
        private static readonly Vector2 PlotSize = new Vector2(340f, 130f);

        private readonly RingBuffer _frameMs = new RingBuffer(Capacity);
        private readonly RingBuffer _fps = new RingBuffer(Capacity);

        // Exponential moving average of instantaneous FPS (same 0.05 lerp as the
        // AC5 benchmark window, so the two windows agree).
        private float _fpsEma = -1f;

        /// <summary>
        /// Content of the plot window: samples the frame clock, then declares the
        /// two plots and their line series. Call inside an ImGuiEx.Window scope
        /// whose Visible is true.
        /// </summary>
        public void DrawImGui()
        {
            Tick();

            DearImGuiKSP.DearImGuiKSP.TextColored(
                DearImGuiKSP.Application.KspPalette.TextLightGrey, ResizeNote);
            using (var frameMsPlot = DearImGuiKSP.ImGuiPlot.Begin(FrameMsTitle, PlotSize))
            {
                if (frameMsPlot.Visible)
                {
                    DearImGuiKSP.ImGuiPlot.SetupAxesAutoFit();
                    DearImGuiKSP.ImGuiPlot.PlotLine(FrameMsLabel, _frameMs.OldestFirst);
                }
            }
            using (var fpsPlot = DearImGuiKSP.ImGuiPlot.Begin(FpsTitle, PlotSize))
            {
                if (fpsPlot.Visible)
                {
                    DearImGuiKSP.ImGuiPlot.SetupAxesAutoFit();
                    DearImGuiKSP.ImGuiPlot.PlotLine(FpsLabel, _fps.OldestFirst);
                }
            }
        }

        // One sample per frame into both rings; no string building, no allocation.
        private void Tick()
        {
            float dt = Time.unscaledDeltaTime;
            _frameMs.Push(dt * 1000f);
            _fpsEma = _fpsEma < 0f ? 1f / dt : Mathf.Lerp(_fpsEma, 1f / dt, 0.05f);
            _fps.Push(_fpsEma);
        }

        /// <summary>
        /// Fixed-capacity ring of float samples with a reused oldest-first scratch
        /// array, so exposing the series as a span costs one in-place copy and no
        /// allocation. Preallocated once; never grows.
        /// </summary>
        private sealed class RingBuffer
        {
            private readonly float[] _samples;
            private readonly float[] _ordered;
            private int _head;  // index of the OLDEST sample; ring is full once _count == capacity
            private int _count;

            public RingBuffer(int capacity)
            {
                _samples = new float[capacity];
                _ordered = new float[capacity];
            }

            /// <summary>
            /// The samples oldest-to-newest as a span over a reused scratch array.
            /// Valid until the next Push; consumed synchronously by PlotLine each
            /// frame, which is the only reader.
            /// </summary>
            public System.ReadOnlySpan<float> OldestFirst
            {
                get
                {
                    int capacity = _samples.Length;
                    for (int i = 0; i < _count; i++)
                    {
                        int idx = _head + i;
                        _ordered[i] = _samples[idx >= capacity ? idx - capacity : idx];
                    }
                    return new System.ReadOnlySpan<float>(_ordered, 0, _count);
                }
            }

            public void Push(float value)
            {
                int capacity = _samples.Length;
                if (_count < capacity)
                {
                    _samples[(_head + _count) % capacity] = value;
                    _count++;
                }
                else
                {
                    _samples[_head] = value;
                    _head = _head + 1 == capacity ? 0 : _head + 1;
                }
            }
        }
    }
}
