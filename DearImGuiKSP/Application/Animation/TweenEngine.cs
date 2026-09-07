using System;
using System.Collections.Generic;
using DearImGuiKSP;
using Color = UnityEngine.Color;

namespace DearImGuiKSP.Application.Animation
{
    /// <summary>
    /// Delta-time-driven tween engine (C14, spec §5.4). Ticked once per frame by the
    /// frame loop before consumer callbacks, so tween setters see this frame's delta
    /// and consumers read fresh values the same frame. Suspension pauses for free:
    /// the frame loop does not run while the lifecycle is not Running.
    /// Live tweens live in one preallocated List of struct entries; removal is
    /// tombstone-then-sweep — a cancel or completion only sets a flag during the
    /// tick pass, and the compacting sweep runs after the pass, so a setter that
    /// cancels its own (or another) tween mid-tick cannot corrupt the iteration or
    /// skip/double-invoke a sibling. An empty tick is a single Count check; nothing
    /// here allocates per frame at zero (or any steady count of) live tweens.
    /// </summary>
    internal sealed class TweenEngine
    {
        private struct Entry
        {
            internal int Id;
            internal bool IsColor;
            internal bool Done;
            internal float From;
            internal float To;
            internal float Seconds;
            internal float Elapsed;
            internal Ease Ease;
            internal Color FromColor;
            internal Color ToColor;
            internal Action<float> FloatSetter;
            internal Action<Color> ColorSetter;
        }

        private const int InitialCapacity = 8;

        // Ids start at 1; 0 is reserved for the inert default(TweenHandle).
        private readonly List<Entry> _entries = new List<Entry>(InitialCapacity);
        private readonly Interfaces.ILogger _log;
        private int _nextId;
        private bool _ticking;

        internal TweenEngine(Interfaces.ILogger log = null)
        {
            _log = log;
        }

        internal TweenHandle StartFloat(Action<float> set, float from, float to, float seconds, Ease ease)
        {
            Entry entry = new Entry();
            entry.Id = ++_nextId;
            entry.From = from;
            entry.To = to;
            entry.Seconds = seconds;
            entry.Ease = ease;
            entry.FloatSetter = set;
            _entries.Add(entry);
            return new TweenHandle(this, entry.Id);
        }

        internal TweenHandle StartColor(Action<Color> set, Color from, Color to, float seconds, Ease ease)
        {
            Entry entry = new Entry();
            entry.Id = ++_nextId;
            entry.IsColor = true;
            entry.FromColor = from;
            entry.ToColor = to;
            entry.Seconds = seconds;
            entry.Ease = ease;
            entry.ColorSetter = set;
            _entries.Add(entry);
            return new TweenHandle(this, entry.Id);
        }

        /// <summary>
        /// Advances every live tween by <paramref name="deltaTime"/>, invoking each
        /// setter with the eased value; tweens reaching t = 1 get the exact target
        /// value and are removed. Safe against setters that cancel tweens mid-tick;
        /// a setter that throws is contained (S3): that tween is stopped and logged,
        /// siblings keep running, and the exception never escapes Tick.
        /// </summary>
        internal void Tick(float deltaTime)
        {
            List<Entry> entries = _entries;
            int count = entries.Count;
            if (count == 0)
            {
                return;
            }

            _ticking = true;
            try
            {
                for (int i = 0; i < count; i++)
                {
                    Entry entry = entries[i];
                    if (entry.Done)
                    {
                        continue;
                    }

                    entry.Elapsed += deltaTime;
                    // seconds <= 0 completes on the first tick instead of dividing by zero.
                    float t = entry.Seconds <= 0f ? 1f : Clamp01(entry.Elapsed / entry.Seconds);
                    float eased = EaseFunctions.Evaluate(entry.Ease, t);
                    entries[i] = entry;

                    try
                    {
                        if (entry.IsColor)
                        {
                            // t == 1 short-circuits to the exact target: component-wise
                            // lerp arithmetic need not reproduce `to` bit-for-bit.
                            Color value = t >= 1f
                                ? entry.ToColor
                                : Color.Lerp(entry.FromColor, entry.ToColor, eased);
                            entry.ColorSetter(value);
                        }
                        else
                        {
                            float f = t >= 1f ? entry.To : entry.From + (entry.To - entry.From) * eased;
                            entry.FloatSetter(f);
                        }
                    }
                    catch (Exception ex)
                    {
                        // S3: setters are consumer code running outside the fault
                        // barrier. Unguarded, a throw escapes Tick and the tween —
                        // never marked done — rethrows every frame, freezing the
                        // whole frame loop. Contain it: kill this tween only.
                        _log?.Error("Tween setter threw; the tween has been stopped. Exception: " + ex);
                        entry.Done = true;
                        entries[i] = entry;
                        continue;
                    }

                    // The setter may have cancelled this tween (tombstone in the
                    // list); completion merges with it, never overwrites it.
                    if (t >= 1f)
                    {
                        entry.Done = true;
                        entries[i] = entry;
                    }
                }
            }
            finally
            {
                _ticking = false;
            }

            // Sweep pass, backwards so RemoveAt does not shift unvisited entries.
            // Cancelled/never-started-in-this-tick entries may also sit here when
            // Cancel ran during the tick; all of them leave now.
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                if (entries[i].Done)
                {
                    entries.RemoveAt(i);
                }
            }
        }

        internal bool IsPlaying(int id)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                Entry entry = _entries[i];
                if (entry.Id == id)
                {
                    return !entry.Done;
                }
            }
            return false;
        }

        internal void Cancel(int id)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                Entry entry = _entries[i];
                if (entry.Id != id || entry.Done)
                {
                    continue;
                }
                if (_ticking)
                {
                    // Mid-tick: tombstone; the sweep pass removes it after the pass
                    // so list indices stay stable for the rest of the iteration.
                    entry.Done = true;
                    _entries[i] = entry;
                }
                else
                {
                    _entries.RemoveAt(i);
                }
                return;
            }
        }

        private static float Clamp01(float value)
        {
            if (value < 0f)
            {
                return 0f;
            }
            if (value > 1f)
            {
                return 1f;
            }
            return value;
        }
    }
}
