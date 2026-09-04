using System;
using DearImGuiKSP.Application.Animation;
using Color = UnityEngine.Color;

namespace DearImGuiKSP
{
    /// <summary>
    /// Public tween API (C14, spec §4.3, §5.4). Tween values are advanced by the
    /// library's frame loop — driven solely by delta time and the easing curve, so
    /// a tween's trajectory is deterministic and suspension (F2/loading) pauses it
    /// automatically. Tweens started while the library is unavailable are ignored:
    /// availability races never throw.
    /// </summary>
    public static class Tween
    {
        // Wiring hook (C14): assigned by Infrastructure.Composition.WireApplicationFacade,
        // the same lazy-singleton pattern as DearImGuiKSP.ThemeEngine.
        internal static TweenEngine Engine { get; set; }

        /// <summary>
        /// Starts a float tween from <paramref name="from"/> to <paramref name="to"/>
        /// over <paramref name="seconds"/>, invoking <paramref name="set"/> with the
        /// eased value each frame. <paramref name="set"/> is invoked once immediately
        /// with <paramref name="from"/> (the t = 0 baseline); the final invocation is
        /// exactly <paramref name="to"/>. A non-positive duration completes on the
        /// first frame after the baseline call.
        /// </summary>
        /// <param name="set">Per-frame value callback.</param>
        /// <param name="from">Value at t = 0; delivered to <paramref name="set"/> immediately.</param>
        /// <param name="to">Value at t = 1; delivered exactly on completion.</param>
        /// <param name="seconds">Duration in seconds; zero or negative completes on the first tick.</param>
        /// <param name="ease">Easing curve applied to the normalized progress.</param>
        /// <returns>
        /// A handle for <see cref="TweenHandle.Cancel"/> and <see cref="TweenHandle.IsPlaying"/>.
        /// Inert (never playing, cancel is a no-op) when the library is not available.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="set"/> is null.</exception>
        public static TweenHandle To(Action<float> set, float from, float to, float seconds, Ease ease)
        {
            if (set == null)
            {
                throw new ArgumentNullException("set");
            }
            TweenEngine engine = Engine;
            if (engine == null || !DearImGuiKSP.IsAvailable)
            {
                DearImGuiKSP.Log?.Warn("Tween.To(...) ignored: DearImGui-KSP is not available.");
                return default(TweenHandle);
            }
            set(from);
            return engine.StartFloat(set, from, to, seconds, ease);
        }

        /// <summary>
        /// Starts a color tween from <paramref name="from"/> to <paramref name="to"/>
        /// over <paramref name="seconds"/>, invoking <paramref name="set"/> with the
        /// RGBA-lerped value each frame (<c>Color.Lerp</c> semantics over the eased t).
        /// <paramref name="set"/> is invoked once immediately with <paramref name="from"/>
        /// (the t = 0 baseline); the final invocation is exactly <paramref name="to"/>.
        /// A non-positive duration completes on the first frame after the baseline call.
        /// </summary>
        /// <param name="set">Per-frame value callback.</param>
        /// <param name="from">Value at t = 0; delivered to <paramref name="set"/> immediately.</param>
        /// <param name="to">Value at t = 1; delivered exactly on completion.</param>
        /// <param name="seconds">Duration in seconds; zero or negative completes on the first tick.</param>
        /// <param name="ease">Easing curve applied to the normalized progress.</param>
        /// <returns>
        /// A handle for <see cref="TweenHandle.Cancel"/> and <see cref="TweenHandle.IsPlaying"/>.
        /// Inert (never playing, cancel is a no-op) when the library is not available.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="set"/> is null.</exception>
        public static TweenHandle To(Action<Color> set, Color from, Color to, float seconds, Ease ease)
        {
            if (set == null)
            {
                throw new ArgumentNullException("set");
            }
            TweenEngine engine = Engine;
            if (engine == null || !DearImGuiKSP.IsAvailable)
            {
                DearImGuiKSP.Log?.Warn("Tween.To(...) ignored: DearImGui-KSP is not available.");
                return default(TweenHandle);
            }
            set(from);
            return engine.StartColor(set, from, to, seconds, ease);
        }
    }

    /// <summary>
    /// Handle for a live tween returned by <c>Tween.To</c> (C14). A readonly struct —
    /// no boxing — with an internal identity: consumers can only receive handles,
    /// never forge or inspect them. The default value is an inert handle
    /// (<see cref="IsPlaying"/> false, <see cref="Cancel"/> a no-op).
    /// </summary>
    public readonly struct TweenHandle
    {
        private readonly TweenEngine _engine;
        private readonly int _id;

        internal TweenHandle(TweenEngine engine, int id)
        {
            _engine = engine;
            _id = id;
        }

        /// <summary>
        /// True while the tween is live on the engine. False once the tween has
        /// completed or been cancelled, and for inert handles.
        /// </summary>
        public bool IsPlaying => _engine != null && _engine.IsPlaying(_id);

        /// <summary>
        /// Stops the tween: no further setter invocations. A no-op for an already
        /// completed, already cancelled, or inert handle. Never throws.
        /// </summary>
        public void Cancel()
        {
            if (_engine != null)
            {
                _engine.Cancel(_id);
            }
        }
    }
}
