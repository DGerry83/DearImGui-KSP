using System;
using Color = UnityEngine.Color;
using Color32 = UnityEngine.Color32;
using Vector2 = UnityEngine.Vector2;

namespace DearImGuiKSP
{
    /// <summary>
    /// Exception-safe scope guards over the facade's Begin/End and Push/Pop pairs
    /// (spec §6.1 ergonomics; C3). Each factory pushes/begins immediately and returns
    /// a <c>readonly struct</c> whose <see cref="IDisposable.Dispose"/> calls the
    /// matching End/Pop exactly once — including when the <c>using</c> body throws,
    /// because Dispose runs during stack unwinding, before the exception reaches the
    /// fault barrier (Application/FaultBarrier.cs). Using over a struct does not box.
    /// </summary>
    /// <remarks>
    /// Immediate-mode rule: a scope must be disposed within the same frame/callback
    /// that created it. Holding a scope across frames leaves the ImGui window/style
    /// stack open and triggers ImGui's end-of-frame assert. All factories and Dispose
    /// paths route through the public facade, which no-ops when the library is
    /// unavailable — so Dispose is always safe, never an exception source.
    /// </remarks>
    public static class ImGuiEx
    {
        /// <summary>
        /// Begins a window and returns a scope that ends it. Only valid inside a
        /// registered callback.
        /// </summary>
        /// <param name="name">Window title; also its ImGui identity.</param>
        /// <returns>
        /// A scope whose <see cref="WindowScope.Visible"/> mirrors the facade's
        /// BeginWindow result — when false, skip the window's content for this frame.
        /// Dispose always ends the window, even when unused or when the body throws.
        /// </returns>
        /// <remarks>
        /// Must be disposed within the same frame/callback (immediate-mode rule).
        /// </remarks>
        public static WindowScope Window(string name)
        {
            return new WindowScope(DearImGuiKSP.BeginWindow(name));
        }

        /// <summary>
        /// Begins a fixed-height scrolling region and returns a scope that ends it.
        /// Only valid inside a registered callback.
        /// </summary>
        /// <param name="id">Region identifier; also its ImGui identity.</param>
        /// <param name="height">Region height in pixels.</param>
        /// <returns>
        /// A scope whose <see cref="ScrollRegionScope.Visible"/> mirrors the facade's
        /// BeginScrollRegion result — when false, skip the region's content for this
        /// frame. Dispose always ends the region, even when unused or on exception.
        /// </returns>
        /// <remarks>
        /// Must be disposed within the same frame/callback (immediate-mode rule).
        /// </remarks>
        public static ScrollRegionScope ScrollRegion(string id, float height)
        {
            return new ScrollRegionScope(DearImGuiKSP.BeginScrollRegion(id, height));
        }

        /// <summary>
        /// Begins a scrolling region of the given size and returns a scope that ends
        /// it. Only valid inside a registered callback.
        /// </summary>
        /// <param name="id">Region identifier; also its ImGui identity.</param>
        /// <param name="size">Region size in pixels; x = 0 stretches to available width.</param>
        /// <returns>
        /// A scope whose <see cref="ScrollRegionScope.Visible"/> mirrors the facade's
        /// BeginScrollRegion result — when false, skip the region's content for this
        /// frame. Dispose always ends the region, even when unused or on exception.
        /// </returns>
        /// <remarks>
        /// Must be disposed within the same frame/callback (immediate-mode rule).
        /// </remarks>
        public static ScrollRegionScope ScrollRegion(string id, Vector2 size)
        {
            return new ScrollRegionScope(DearImGuiKSP.BeginScrollRegion(id, size));
        }

        /// <summary>
        /// Pushes a style color and returns a scope that pops exactly one entry.
        /// Only valid inside a registered callback.
        /// </summary>
        /// <param name="col">The style slot to override (see <see cref="DearImGuiKSP.ImGuiCol"/>).</param>
        /// <param name="value">The color; components are linear RGBA in 0–1 range.</param>
        /// <returns>
        /// A scope whose Dispose pops exactly one style-color entry — on the success
        /// path and on exception alike — keeping the stack symmetric for the frame.
        /// </returns>
        /// <remarks>
        /// Must be disposed within the same frame/callback (immediate-mode rule).
        /// </remarks>
        public static StyleColorScope StyleColor(ImGuiCol col, Color value)
        {
            DearImGuiKSP.PushStyleColor(col, value);
            return default(StyleColorScope);
        }

        /// <summary>
        /// Pushes a style color and returns a scope that pops exactly one entry.
        /// Only valid inside a registered callback.
        /// </summary>
        /// <param name="col">The style slot to override (see <see cref="DearImGuiKSP.ImGuiCol"/>).</param>
        /// <param name="value">The color; components are sRGB bytes in 0–255 range.</param>
        /// <returns>
        /// A scope whose Dispose pops exactly one style-color entry — on the success
        /// path and on exception alike — keeping the stack symmetric for the frame.
        /// </returns>
        /// <remarks>
        /// Must be disposed within the same frame/callback (immediate-mode rule).
        /// </remarks>
        public static StyleColorScope StyleColor(ImGuiCol col, Color32 value)
        {
            DearImGuiKSP.PushStyleColor(col, value);
            return default(StyleColorScope);
        }

        /// <summary>
        /// Pushes a float style variable and returns a scope that pops exactly one
        /// entry. Only valid inside a registered callback.
        /// </summary>
        /// <param name="var">The style slot to override (see <see cref="DearImGuiKSP.ImGuiStyleVar"/>).</param>
        /// <param name="value">The new value for the slot.</param>
        /// <returns>
        /// A scope whose Dispose pops exactly one style-variable entry — on the
        /// success path and on exception alike — keeping the stack symmetric for
        /// the frame.
        /// </returns>
        /// <remarks>
        /// Must be disposed within the same frame/callback (immediate-mode rule).
        /// </remarks>
        public static StyleVarScope StyleVar(ImGuiStyleVar var, float value)
        {
            DearImGuiKSP.PushStyleVar(var, value);
            return default(StyleVarScope);
        }

        /// <summary>
        /// Pushes a 2D style variable and returns a scope that pops exactly one entry.
        /// Only valid inside a registered callback.
        /// </summary>
        /// <param name="var">The style slot to override (see <see cref="DearImGuiKSP.ImGuiStyleVar"/>).</param>
        /// <param name="value">The new value for the slot.</param>
        /// <returns>
        /// A scope whose Dispose pops exactly one style-variable entry — on the
        /// success path and on exception alike — keeping the stack symmetric for
        /// the frame.
        /// </returns>
        /// <remarks>
        /// Must be disposed within the same frame/callback (immediate-mode rule).
        /// </remarks>
        public static StyleVarScope StyleVar(ImGuiStyleVar var, Vector2 value)
        {
            DearImGuiKSP.PushStyleVar(var, value);
            return default(StyleVarScope);
        }

        /// <summary>
        /// Scope guard pairing a facade <see cref="DearImGuiKSP.BeginWindow"/> with
        /// exactly one <see cref="DearImGuiKSP.EndWindow"/>. Obtain it from
        /// <see cref="ImGuiEx.Window"/>; do not construct it directly (a default
        /// instance has no matching Begin — dispose only what a factory returned).
        /// </summary>
        public readonly struct WindowScope : IDisposable
        {
            private readonly bool _visible;

            internal WindowScope(bool visible)
            {
                _visible = visible;
            }

            /// <summary>
            /// The Begin result captured when this scope was created: false when the
            /// window is collapsed/clipped (or the library is unavailable) — skip the
            /// window's content for this frame. Dispose ends the window regardless.
            /// </summary>
            public bool Visible
            {
                get { return _visible; }
            }

            /// <summary>
            /// Ends the window. Called once by <c>using</c> on every exit path,
            /// including when the body throws. Routes through the facade, which
            /// no-ops when the library is unavailable.
            /// </summary>
            public void Dispose()
            {
                DearImGuiKSP.EndWindow();
            }
        }

        /// <summary>
        /// Scope guard pairing a facade <see cref="DearImGuiKSP.BeginScrollRegion"/>
        /// with exactly one <see cref="DearImGuiKSP.EndScrollRegion"/>. Obtain it from
        /// <see cref="ImGuiEx.ScrollRegion(string, float)"/> or
        /// <see cref="ImGuiEx.ScrollRegion(string, Vector2)"/>; do not construct it
        /// directly (a default instance has no matching Begin — dispose only what a
        /// factory returned).
        /// </summary>
        public readonly struct ScrollRegionScope : IDisposable
        {
            private readonly bool _visible;

            internal ScrollRegionScope(bool visible)
            {
                _visible = visible;
            }

            /// <summary>
            /// The Begin result captured when this scope was created: false when the
            /// region is clipped (or the library is unavailable) — skip the region's
            /// content for this frame. Dispose ends the region regardless.
            /// </summary>
            public bool Visible
            {
                get { return _visible; }
            }

            /// <summary>
            /// Ends the scrolling region. Called once by <c>using</c> on every exit
            /// path, including when the body throws. Routes through the facade, which
            /// no-ops when the library is unavailable.
            /// </summary>
            public void Dispose()
            {
                DearImGuiKSP.EndScrollRegion();
            }
        }

        /// <summary>
        /// Scope guard pairing one facade PushStyleColor with exactly one
        /// <see cref="DearImGuiKSP.PopStyleColor(int)"/>. Obtain it from
        /// <see cref="ImGuiEx.StyleColor(ImGuiCol, Color)"/> or
        /// <see cref="ImGuiEx.StyleColor(ImGuiCol, Color32)"/>; a default instance
        /// pops nothing pushed by this library — dispose only what a factory returned.
        /// </summary>
        public readonly struct StyleColorScope : IDisposable
        {
            /// <summary>
            /// Pops exactly one style-color entry. Called once by <c>using</c> on
            /// every exit path, including when the body throws, so the stack stays
            /// symmetric for the frame. Routes through the facade, which no-ops when
            /// the library is unavailable.
            /// </summary>
            public void Dispose()
            {
                DearImGuiKSP.PopStyleColor(1);
            }
        }

        /// <summary>
        /// Scope guard pairing one facade PushStyleVar with exactly one
        /// <see cref="DearImGuiKSP.PopStyleVar(int)"/>. Obtain it from
        /// <see cref="ImGuiEx.StyleVar(ImGuiStyleVar, float)"/> or
        /// <see cref="ImGuiEx.StyleVar(ImGuiStyleVar, Vector2)"/>; a default instance
        /// pops nothing pushed by this library — dispose only what a factory returned.
        /// </summary>
        public readonly struct StyleVarScope : IDisposable
        {
            /// <summary>
            /// Pops exactly one style-variable entry. Called once by <c>using</c> on
            /// every exit path, including when the body throws, so the stack stays
            /// symmetric for the frame. Routes through the facade, which no-ops when
            /// the library is unavailable.
            /// </summary>
            public void Dispose()
            {
                DearImGuiKSP.PopStyleVar(1);
            }
        }
    }
}
