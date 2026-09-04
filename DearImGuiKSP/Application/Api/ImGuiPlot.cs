using System;
using DearImGuiKSP.Interop;
using Vector2 = UnityEngine.Vector2;

namespace DearImGuiKSP
{
    /// <summary>
    /// Public plot API over vendored ImPlot (spec §4.3; chunk C13, M4). Declares a
    /// plot and its line series each frame inside a registered callback, the same
    /// immediate-mode discipline as the rest of the facade. All calls no-op when
    /// <see cref="DearImGuiKSP.IsAvailable"/> is false.
    /// </summary>
    /// <remarks>
    /// Allocation story (§5.9 hot-path discipline): <see cref="PlotLine(string, ReadOnlySpan{float})"/>
    /// pins the caller's buffer in place with <c>fixed</c> (no copies, no pinning
    /// handles, no boxing) and passes it to native code for the duration of the
    /// call only. Its only managed allocation is the null-terminated UTF-8 label
    /// buffer (the same ToUtf8 convention every facade call uses since C9; the C10
    /// toggle wrapper documents it identically) — label strings are short-lived
    /// and never retained. Pass constant labels; the data path itself allocates
    /// nothing per frame.
    /// </remarks>
    public static class ImGuiPlot
    {
        /// <summary>
        /// Begins a plot and returns a scope that ends it. Only valid inside a
        /// registered callback, and only line series drawn through
        /// <see cref="PlotLine(string, ReadOnlySpan{float})"/> /
        /// <see cref="PlotLine(string, ReadOnlySpan{double})"/> are supported by
        /// this chunk.
        /// </summary>
        /// <param name="title">Plot title; also its ImPlot identity (double-hash to hide).</param>
        /// <param name="size">Plot size in pixels.</param>
        /// <returns>
        /// A scope whose <see cref="PlotScope.Visible"/> mirrors the ImPlot
        /// BeginPlot result — when false, skip the plot's content for this frame.
        /// Dispose ends the plot only when BeginPlot returned true (ImPlot's
        /// pairing rule), and runs on every exit path, including when the body
        /// throws. Inert (Visible false, no native state) when the library is
        /// unavailable.
        /// </returns>
        /// <remarks>
        /// Must be disposed within the same frame/callback (immediate-mode rule).
        /// </remarks>
        public static PlotScope Begin(string title, Vector2 size)
        {
            if (!DearImGuiKSP.IsAvailable)
            {
                return default(PlotScope);
            }
            bool visible = ImPlotNative.BeginPlot(title, new ImVec2(size.x, size.y), ImPlotFlags.None);
            return new PlotScope(visible);
        }

        /// <summary>
        /// Draws a line series (float32 y-values, x = index * 1 + 0) inside the
        /// current plot. Only valid between a successful
        /// <see cref="Begin(string, Vector2)"/> and its scope's Dispose.
        /// </summary>
        /// <param name="label">Series label for the legend; also its ImPlot identity.</param>
        /// <param name="values">
        /// Y-values read in place: pinned for the duration of the call, never
        /// copied, never retained. The span's backing storage must stay alive and
        /// unmodified for the call.
        /// </param>
        /// <remarks>
        /// No-op when the library is unavailable or <paramref name="values"/> is
        /// empty (the native call is skipped entirely). See the class remarks for
        /// the label UTF-8 allocation story.
        /// </remarks>
        public static unsafe void PlotLine(string label, ReadOnlySpan<float> values)
        {
            if (!DearImGuiKSP.IsAvailable || values.IsEmpty)
            {
                return;
            }
            // Unity 2019.4's mscorlib predates the C# 7.3 GetPinnableReference
            // pattern, so span pinning goes through Mono's DangerousGetPinnableReference.
            // IsEmpty is checked above, so the reference is never taken on an empty span.
            ref float first = ref values.DangerousGetPinnableReference();
            fixed (float* p = &first)
            {
                ImPlotNative.PlotLine(label, p, values.Length, 1.0, 0.0);
            }
        }

        /// <summary>
        /// Draws a line series (float64 y-values, x = index * 1 + 0) inside the
        /// current plot. Only valid between a successful
        /// <see cref="Begin(string, Vector2)"/> and its scope's Dispose.
        /// </summary>
        /// <param name="label">Series label for the legend; also its ImPlot identity.</param>
        /// <param name="values">
        /// Y-values read in place: pinned for the duration of the call, never
        /// copied, never retained. The span's backing storage must stay alive and
        /// unmodified for the call.
        /// </param>
        /// <remarks>
        /// No-op when the library is unavailable or <paramref name="values"/> is
        /// empty (the native call is skipped entirely). See the class remarks for
        /// the label UTF-8 allocation story.
        /// </remarks>
        public static unsafe void PlotLine(string label, ReadOnlySpan<double> values)
        {
            if (!DearImGuiKSP.IsAvailable || values.IsEmpty)
            {
                return;
            }
            // Unity 2019.4's mscorlib predates the C# 7.3 GetPinnableReference
            // pattern, so span pinning goes through Mono's DangerousGetPinnableReference.
            // IsEmpty is checked above, so the reference is never taken on an empty span.
            ref double first = ref values.DangerousGetPinnableReference();
            fixed (double* p = &first)
            {
                ImPlotNative.PlotLine(label, p, values.Length, 1.0, 0.0);
            }
        }

        /// <summary>
        /// Scope guard pairing one <see cref="Begin(string, Vector2)"/> with at most
        /// one ImPlot EndPlot. Obtain it from <see cref="Begin(string, Vector2)"/>;
        /// do not construct it directly (a default instance has no matching Begin —
        /// dispose only what a factory returned).
        /// </summary>
        public readonly struct PlotScope : IDisposable
        {
            private readonly bool _visible;
            private readonly bool _begun;

            internal PlotScope(bool visible)
            {
                _visible = visible;
                _begun = visible;
            }

            /// <summary>
            /// The BeginPlot result captured when this scope was created: false when
            /// the plot is collapsed/clipped (or the library is unavailable) — skip
            /// the plot's content for this frame. Dispose ends the plot only when
            /// BeginPlot returned true.
            /// </summary>
            public bool Visible
            {
                get { return _visible; }
            }

            /// <summary>
            /// Ends the plot. Called once by <c>using</c> on every exit path,
            /// including when the body throws; calls ImPlot EndPlot only when the
            /// matching BeginPlot returned true (ImPlot's pairing rule — EndPlot
            /// without a successful BeginPlot asserts). No-ops when the library
            /// became unavailable or for a default (factory-untouched) instance.
            /// </summary>
            public void Dispose()
            {
                if (_begun && DearImGuiKSP.IsAvailable)
                {
                    ImPlotNative.EndPlot();
                }
            }
        }
    }
}
