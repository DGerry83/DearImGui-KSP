namespace DearImGuiKSPDemo.Telemetry
{
    /// <summary>
    /// Fixed-capacity (10,000 samples) ring of float telemetry history, preallocated
    /// once and never grown (spec §5.5; C18). Writes are a single array slot
    /// (<see cref="Push"/>); reads are exposed oldest-to-newest as a
    /// <see cref="System.ReadOnlySpan{float}"/> over a reused scratch array — the
    /// wrapped copy is in place, so feeding <see cref="DearImGuiKSP.ImGuiPlot.PlotLine(string, System.ReadOnlySpan{float})"/>
    /// each frame costs one O(n) copy and zero managed allocation. The mechanism is
    /// the same one PlotDemo's ring uses (C13), lifted here so the telemetry panels
    /// share it: the live samples wrap at the head index, so the scratch pass copies
    /// <c>_samples[head..] ++ _samples[..head]</c> into linear order; PlotLine pins
    /// the scratch buffer and reads it synchronously, before any next Push.
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
        /// until the next Push; consumed synchronously by PlotLine each frame, which
        /// is the only reader.
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
