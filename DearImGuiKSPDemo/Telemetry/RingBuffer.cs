namespace DearImGuiKSPDemo.Telemetry
{
    /// <summary>
    /// Fixed-capacity (10,000 samples) ring of float telemetry history, preallocated
    /// once and never grown (spec §5.5; C18). Writes are a single array slot
    /// (<see cref="Push"/>); reads are exposed oldest-to-newest as a
    /// <see cref="System.ReadOnlySpan{float}"/> over a reused scratch array — the
    /// wrapped copy is in place, so feeding <see cref="DearImGuiKSP.ImGuiPlot.PlotLine(string, System.ReadOnlySpan{float})"/>
    /// each frame costs one O(window) copy and zero managed allocation. The mechanism
    /// is the same one PlotDemo's ring uses (C13), lifted here so the telemetry panels
    /// share it: the live samples wrap at the head index, so the scratch pass copies
    /// the window tail into linear order; PlotLine pins the scratch buffer and reads
    /// it synchronously, before any next Push. Reads are WINDOWED
    /// (<see cref="LatestOldestFirst"/>): plotting the full ring scales per-frame
    /// cost with total recorded data (ISSUES #007), so the Graphs tab plots a rolling
    /// window while the full 10k history depth stays recorded underneath.
    /// </summary>
    internal sealed class RingBuffer
    {
        /// <summary>Locked contract capacity (spec §6.2): 10k samples per channel.</summary>
        public const int Capacity = 10000;

        private readonly float[] _samples;
        private readonly float[] _ordered;
        private int _head;  // index of the OLDEST sample; ring is full once _count == Capacity
        private int _count;

        public RingBuffer()
        {
            _samples = new float[Capacity];
            _ordered = new float[Capacity];
        }

        /// <summary>
        /// The samples oldest-to-newest as a span over a reused scratch array. Valid
        /// until the next Push. Superseded for rendering by
        /// <see cref="LatestOldestFirst"/> (ISSUES #007 — full-ring plotting scales
        /// per-frame cost with flight length); kept for whole-history consumers.
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

        /// <summary>Current sample count, up to <see cref="Capacity"/>.</summary>
        public int Count
        {
            get { return _count; }
        }

        /// <summary>
        /// The most recent <paramref name="maxSamples"/> samples oldest-to-newest as
        /// a span over the reused scratch array (fewer when the ring holds less).
        /// This is the ROLLING-WINDOW read the Graphs tab uses (ISSUES #007): plotting
        /// the full ring cost O(capacity) per frame in both the scratch copy and
        /// ImPlot's segment submission, growing to ~7 ms as the flight filled the
        /// rings — the window keeps cost constant. Valid until the next Push;
        /// consumed synchronously by PlotLine each frame.
        /// </summary>
        public System.ReadOnlySpan<float> LatestOldestFirst(int maxSamples)
        {
            int capacity = _samples.Length;
            int n = _count < maxSamples ? _count : maxSamples;
            int first = _head + _count - n;  // logical start, may be negative
            if (first < 0)
            {
                first += capacity;
            }
            for (int i = 0; i < n; i++)
            {
                int idx = first + i;
                _ordered[i] = _samples[idx >= capacity ? idx - capacity : idx];
            }
            return new System.ReadOnlySpan<float>(_ordered, 0, n);
        }

        /// <summary>Appends one sample; an array-slot write, no allocation.</summary>
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
