using System;
using DearImGuiKSP.Application;
using DearImGuiKSP.Application.Interfaces;
using DearImGuiKSP.Interop;

namespace DearImGuiKSP
{
    /// <summary>
    /// Public consumer-facing API for DearImGui-KSP (spec §5.4, §6.1; locked in C7).
    /// Consumers check <see cref="IsAvailable"/> before registering their UI, then
    /// declare their ImGui widgets each frame inside a registered callback.
    /// Widget calls are only valid inside a callback invoked by the frame loop;
    /// calling them from anywhere else is a no-op/undefined, never an exception.
    /// </summary>
    public static class DearImGuiKSP
    {
        // Wiring hooks (C7/C12): Application cannot see Infrastructure.Composition, so
        // Composition assigns these at startup — the logger in Awake, the registry
        // alongside it, and the lifecycle state machine after it is created.
        internal static ILogger Log { get; set; }
        internal static ConsumerRegistry Registry { get; set; }
        internal static LifecycleStateMachine Lifecycle { get; set; }

        /// <summary>
        /// True when the library is initialized and either running or temporarily suspended.
        /// Suspended (F2 hide or loading screen) is a pause, not a failure — consumers
        /// should NOT tear down their UI; the frame loop resumes automatically.
        /// </summary>
        public static bool IsAvailable =>
            Lifecycle != null &&
            (Lifecycle.State == LifecycleState.Running || Lifecycle.State == LifecycleState.Suspended);

        /// <summary>
        /// Registers a per-frame UI declaration callback. Callbacks run once per
        /// frame, in registration order (the MVP z-order, spec §5.3), and are the
        /// only place widget calls are valid.
        /// </summary>
        /// <param name="id">Unique consumer identifier (e.g. your mod's name).</param>
        /// <param name="callback">Invoked every frame while the library is running.</param>
        /// <exception cref="ArgumentNullException"><paramref name="id"/> is null/empty or <paramref name="callback"/> is null.</exception>
        /// <remarks>
        /// A duplicate <paramref name="id"/> is logged as a warning and ignored.
        /// Calling while <see cref="IsAvailable"/> is false logs a warning and is
        /// ignored — availability races never throw.
        /// </remarks>
        public static void Register(string id, Action callback)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException("id");
            }
            if (callback == null)
            {
                throw new ArgumentNullException("callback");
            }
            if (!IsAvailable)
            {
                Log?.Warn("Register('" + id + "') ignored: DearImGui-KSP is not available.");
                return;
            }
            if (!Registry.TryRegister(id, callback))
            {
                Log?.Warn("Register('" + id + "') ignored: id already registered.");
            }
        }

        /// <summary>
        /// Removes a previously registered consumer callback.
        /// </summary>
        /// <param name="id">The identifier passed to <see cref="Register"/>.</param>
        /// <returns>True when the consumer was registered and has been removed.</returns>
        /// <remarks>
        /// Calling while <see cref="IsAvailable"/> is false logs a warning, is
        /// ignored, and returns false — availability races never throw.
        /// </remarks>
        public static bool Unregister(string id)
        {
            if (!IsAvailable)
            {
                Log?.Warn("Unregister('" + id + "') ignored: DearImGui-KSP is not available.");
                return false;
            }
            return Registry.Unregister(id);
        }

        /// <summary>
        /// Begins an ImGui window. Only valid inside a registered callback.
        /// </summary>
        /// <param name="name">Window title; also its ImGui identity.</param>
        /// <returns>
        /// False when the window is collapsed/clipped — <see cref="EndWindow"/> is
        /// still required. Also false when called while unavailable.
        /// </returns>
        public static bool BeginWindow(string name)
        {
            if (!IsAvailable)
            {
                return false;
            }
            return ImGuiInternal.BeginWindow(name);
        }

        /// <summary>
        /// Ends the current window. Always required after <see cref="BeginWindow"/>,
        /// regardless of its return value. Only valid inside a registered callback.
        /// </summary>
        public static void EndWindow()
        {
            if (!IsAvailable)
            {
                return;
            }
            ImGuiInternal.EndWindow();
        }

        /// <summary>
        /// Draws unformatted text. Only valid inside a registered callback.
        /// Null renders as an empty string.
        /// </summary>
        public static void Text(string text)
        {
            if (!IsAvailable)
            {
                return;
            }
            ImGuiInternal.Text(text);
        }

        /// <summary>
        /// Draws an auto-sized button. Only valid inside a registered callback.
        /// </summary>
        /// <returns>True on the frame the button is clicked; false when unavailable.</returns>
        public static bool Button(string label)
        {
            if (!IsAvailable)
            {
                return false;
            }
            return ImGuiInternal.Button(label);
        }

        /// <summary>
        /// Draws a float slider. Only valid inside a registered callback.
        /// </summary>
        /// <returns>
        /// True when the value changed this frame; <paramref name="value"/> is
        /// updated in place. False when unavailable.
        /// </returns>
        public static bool SliderFloat(string label, ref float value, float min, float max)
        {
            if (!IsAvailable)
            {
                return false;
            }
            return ImGuiInternal.SliderFloat(label, ref value, min, max);
        }

        /// <summary>
        /// Draws a single-line text input. Only valid inside a registered callback.
        /// </summary>
        /// <param name="capacity">
        /// Buffer size in bytes, including the NUL terminator; edited text is
        /// truncated to capacity-1 UTF-8 bytes.
        /// </param>
        /// <returns>
        /// True when the user edited the text this frame; <paramref name="value"/>
        /// is updated in place. False when unavailable.
        /// </returns>
        public static bool InputText(string label, ref string value, int capacity = 256)
        {
            if (!IsAvailable)
            {
                return false;
            }
            return ImGuiInternal.InputText(label, ref value, capacity);
        }

        /// <summary>
        /// Begins a fixed-height, bordered scrolling child region. Only valid inside a
        /// registered callback. The manual-virtualization pattern for large lists:
        /// call <see cref="GetScrollY"/> to read the current scroll offset, compute the
        /// visible row range from it, <see cref="SetCursorY"/> to
        /// <c>firstVisibleRow * rowHeight</c>, draw only the visible rows, then
        /// <see cref="SetCursorY"/> to <c>rowCount * rowHeight</c> followed by
        /// <see cref="Dummy"/> so the scrollable range legitimately covers the full list
        /// (ImGui requires an item, not a bare cursor move, to grow content bounds).
        /// </summary>
        /// <param name="id">Region identifier; also its ImGui identity.</param>
        /// <param name="height">Region height in pixels.</param>
        /// <returns>
        /// False when the region is clipped — <see cref="EndScrollRegion"/> is
        /// still required. Also false when called while unavailable.
        /// </returns>
        public static bool BeginScrollRegion(string id, float height)
        {
            if (!IsAvailable)
            {
                return false;
            }
            return ImGuiInternal.BeginScrollRegion(id, height);
        }

        /// <summary>
        /// Ends the current scrolling child region. Always required after
        /// <see cref="BeginScrollRegion"/>, regardless of its return value.
        /// Only valid inside a registered callback.
        /// </summary>
        public static void EndScrollRegion()
        {
            if (!IsAvailable)
            {
                return;
            }
            ImGuiInternal.EndScrollRegion();
        }

        /// <summary>
        /// Current vertical scroll offset of the active region/window, in pixels.
        /// Only valid inside a registered callback.
        /// </summary>
        /// <returns>The scroll offset; 0 when unavailable.</returns>
        public static float GetScrollY()
        {
            if (!IsAvailable)
            {
                return 0f;
            }
            return ImGuiInternal.GetScrollY();
        }

        /// <summary>
        /// Sets the vertical cursor position within the active region/window, in local
        /// coordinates. Only valid inside a registered callback.
        /// </summary>
        public static void SetCursorY(float y)
        {
            if (!IsAvailable)
            {
                return;
            }
            ImGuiInternal.SetCursorY(y);
        }

        /// <summary>
        /// Submits an invisible item of the given size, advancing the cursor and growing
        /// the window's content bounds. Only valid inside a registered callback.
        /// Required after a <see cref="SetCursorY"/> that extends a region's scrollable
        /// range — ImGui asserts when a bare cursor move grows parent boundaries.
        /// </summary>
        public static void Dummy(float width, float height)
        {
            if (!IsAvailable)
            {
                return;
            }
            ImGuiInternal.Dummy(width, height);
        }
    }
}
