using System;
using System.Collections.Generic;
using DearImGuiKSP.Application;
using DearImGuiKSP.Application.Interfaces;
using DearImGuiKSP.Interop;
using Color = UnityEngine.Color;
using Color32 = UnityEngine.Color32;
using Vector2 = UnityEngine.Vector2;

namespace DearImGuiKSP
{
    /// <summary>
    /// Public consumer-facing API for DearImGui-KSP (spec §5.4, §6.1; locked in C7).
    /// Consumers check <see cref="IsAvailable"/> before registering their UI, then
    /// declare their ImGui widgets each frame inside a registered callback.
    /// Widget calls are only valid inside a callback invoked by the frame loop;
    /// calling them from anywhere else is a safe no-op, never an exception (the
    /// frame-open gate, C01: widget guards require an open frame, not just an
    /// available session).
    /// </summary>
    public static partial class DearImGuiKSP
    {
        // Wiring hooks (C7/C12): Application cannot see Infrastructure.Composition, so
        // Composition assigns these at startup — the logger in Awake, the registry
        // alongside it, and the lifecycle state machine after it is created.
        // ThemeEngine (C8) joins the same pattern for <see cref="CurrentTheme"/>.
        internal static ILogger Log { get; set; }
        internal static ConsumerRegistry Registry { get; set; }
        internal static LifecycleStateMachine Lifecycle { get; set; }
        internal static Application.ThemeEngine ThemeEngine { get; set; }

        /// <summary>
        /// Id of the consumer whose callback is currently running (set by
        /// FaultBarrier around each invocation; null between callbacks). Backs
        /// the G2-11 window-title collision warning's attribution.
        /// </summary>
        internal static string CurrentConsumerId { get; set; }

        /// <summary>
        /// True only between the frame loop's BeginUiFrame and EndUiFrame — the
        /// window in which widget calls reach native code (S2). Set by
        /// FrameLoopOrchestrator; cleared in a finally so it can never latch.
        /// </summary>
        internal static bool FrameOpen { get; set; }

        /// <summary>
        /// Widget-call gate: the session is available AND a frame is currently open.
        /// Widget calls outside a registered callback are safe no-ops as documented;
        /// <see cref="IsAvailable"/> alone is a session predicate and must not be
        /// used to gate native widget calls.
        /// </summary>
        internal static bool CanDeclareUi => IsAvailable && FrameOpen;

        /// <summary>
        /// Closes every facade scope the faulting consumer left open (G2-05), in
        /// innermost-first order. Called by FaultBarrier from inside the frame;
        /// no-op when no frame is open. Tests inject a recorder closer.
        /// </summary>
        internal static void UnwindOpenScopes(Application.IScopeCloser closer = null)
        {
            if (!FrameOpen)
            {
                return;
            }
            Application.OpenScopeTracker.Unwind(closer ?? Application.NativeScopeCloser.Instance);
        }

        /// <summary>
        /// Name of the currently applied theme preset: "ksp" (the default) or
        /// "dark" (exact stock ImGui dark). Read-only; change the theme via the
        /// library's settings.cfg <c>theme</c> key (spec §9, D25) — the new theme
        /// applies at the start of the next UI frame.
        /// </summary>
        public static string CurrentTheme =>
            ThemeEngine != null ? ThemeEngine.CurrentThemeName : LibraryConfig.DefaultTheme;

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
        /// ignored — availability races never throw. Registration from inside a
        /// consumer callback is allowed and applies from the next frame (the frame
        /// loop iterates a snapshot, S1).
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
        /// Unregistering from inside a consumer callback is allowed and applies
        /// from the next frame (the frame loop iterates a snapshot, S1).
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
        /// <param name="name">
        /// Window title; also its ImGui identity. Titles are PROCESS-GLOBAL:
        /// they are shared by every mod using this library (and any other ImGui
        /// user) in the KSP process, with no namespacing — two mods beginning
        /// the same title share one window identity, so position, collapse
        /// state, and focus bleed across them. When two different registered
        /// consumers begin the same title, the library logs one warning per
        /// title (G2-11; detection only, the window still begins). Prefix
        /// titles with your mod name to keep them unique. Standard ImGui
        /// <c>##</c>/<c>###</c> suffix rules apply.
        /// </param>
        /// <param name="autoResize">
        /// Opt in to fit-to-content sizing (<c>ImGuiWindowFlags_AlwaysAutoResize</c>,
        /// imgui.h:1225): the window is resized to its content every frame. While
        /// enabled, the resize grip and edges are inactive (the window is not
        /// user-resizable) and no scrollbars appear because the window always fits;
        /// the title bar stays draggable.
        /// </param>
        /// <returns>
        /// False when the window is collapsed/clipped — <see cref="EndWindow"/> is
        /// still required. Also false when called while unavailable.
        /// </returns>
        public static bool BeginWindow(string name, bool autoResize = false)
        {
            if (!CanDeclareUi)
            {
                return false;
            }
            WarnOnWindowTitleCollision(name);
            // End is required even when Begin returns false, so count unconditionally.
            bool visible = ImGuiInternal.BeginWindow(
                name, autoResize ? ImGuiWindowFlags.AlwaysAutoResize : ImGuiWindowFlags.None);
            OpenScopeTracker.Windows++;
            return visible;
        }

        /// <summary>
        /// Ends the current window. Always required after <see cref="BeginWindow"/>,
        /// regardless of its return value. Only valid inside a registered callback.
        /// </summary>
        public static void EndWindow()
        {
            if (!CanDeclareUi)
            {
                return;
            }
            ImGuiInternal.EndWindow();
            OpenScopeTracker.Windows--;
        }

        /// <summary>
        /// Draws unformatted text. Only valid inside a registered callback.
        /// Null renders as an empty string.
        /// </summary>
        public static void Text(string text)
        {
            if (!CanDeclareUi)
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
            if (!CanDeclareUi)
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
            if (!CanDeclareUi)
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
        /// <remarks>
        /// Under the "ksp" theme the label is drawn separately in the theme's
        /// off-white and only the typed text renders in the KSP light orange —
        /// ImGui colors an InputText's label with the same Col_Text as its
        /// contents, so the widget runs with a hidden-label ID and Col_Text
        /// pushed to orange for that call. The hidden ID is built with a
        /// mid-string "###" (ImHashStr reset), so the widget's ImGui identity
        /// equals the stock single-call identity in EVERY theme (G2-09):
        /// switching themes mid-edit does not drop focus, and <c>##</c> suffixes
        /// never render as visible text. Under the "dark" theme the stock
        /// single-call path is kept byte-identical.
        /// </remarks>
        public static bool InputText(string label, ref string value, int capacity = 256)
        {
            if (!CanDeclareUi)
            {
                return false;
            }
            if (ThemeEngine == null ||
                string.Equals(ThemeEngine.CurrentThemeName, LibraryConfig.DarkThemeName, StringComparison.Ordinal))
            {
                return ImGuiInternal.InputText(label, ref value, capacity);
            }

            Text(StripIdSuffix(label));
            ImGuiInternal.SameLine();
            PushStyleColor(ImGuiCol.Text, KspPalette.OrangeLight);
            bool edited = ImGuiInternal.InputTextWithHiddenLabel(label, ref value, capacity);
            PopStyleColor();
            return edited;
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
            if (!CanDeclareUi)
            {
                return false;
            }
            // End is required even when Begin returns false, so count unconditionally.
            bool visible = ImGuiInternal.BeginScrollRegion(id, height);
            OpenScopeTracker.ScrollRegions++;
            return visible;
        }

        /// <summary>
        /// Ends the current scrolling child region. Always required after
        /// <see cref="BeginScrollRegion"/>, regardless of its return value.
        /// Only valid inside a registered callback.
        /// </summary>
        public static void EndScrollRegion()
        {
            if (!CanDeclareUi)
            {
                return;
            }
            ImGuiInternal.EndScrollRegion();
            OpenScopeTracker.ScrollRegions--;
        }

        /// <summary>
        /// Current vertical scroll offset of the active region/window, in pixels.
        /// Only valid inside a registered callback.
        /// </summary>
        /// <returns>The scroll offset; 0 when unavailable.</returns>
        public static float GetScrollY()
        {
            if (!CanDeclareUi)
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
            if (!CanDeclareUi)
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
            if (!CanDeclareUi)
            {
                return;
            }
            ImGuiInternal.Dummy(width, height);
        }

        /// <summary>
        /// Submits an invisible item of the given size, advancing the cursor and growing
        /// the window's content bounds. Only valid inside a registered callback.
        /// Convenience overload of <see cref="Dummy(float, float)"/>; see that member
        /// for the virtualization pattern this supports.
        /// </summary>
        public static void Dummy(Vector2 size)
        {
            if (!CanDeclareUi)
            {
                return;
            }
            ImGuiInternal.Dummy(new ImVec2(size.x, size.y));
        }

        /// <summary>
        /// Begins a bordered scrolling child region of the given size (width 0 stretches
        /// to the available width). Only valid inside a registered callback.
        /// Convenience overload of <see cref="BeginScrollRegion(string, float)"/>; see
        /// that member for the manual-virtualization pattern this supports.
        /// </summary>
        /// <param name="id">Region identifier; also its ImGui identity.</param>
        /// <param name="size">Region size in pixels; x = 0 stretches to available width.</param>
        /// <returns>
        /// False when the region is clipped — <see cref="EndScrollRegion"/> is
        /// still required. Also false when called while unavailable.
        /// </returns>
        public static bool BeginScrollRegion(string id, Vector2 size)
        {
            if (!CanDeclareUi)
            {
                return false;
            }
            // End is required even when Begin returns false, so count unconditionally.
            bool visible = ImGuiInternal.BeginScrollRegion(id, new ImVec2(size.x, size.y));
            OpenScopeTracker.ScrollRegions++;
            return visible;
        }

        /// <summary>
        /// Begins a tab bar. Only valid inside a registered callback, inside a window.
        /// </summary>
        /// <param name="id">Tab bar identifier; also its ImGui identity.</param>
        /// <returns>
        /// False when the tab bar is clipped — <see cref="EndTabBar"/> must NOT be
        /// called then (imgui.h:965). Also false when called while unavailable.
        /// Use <c>ImGuiEx.TabBar</c> (C18) for exception-safe pairing.
        /// </returns>
        public static bool BeginTabBar(string id)
        {
            if (!CanDeclareUi)
            {
                return false;
            }
            // End is paired only with a Begin that returned true (imgui.h:965).
            bool open = ImGuiInternal.BeginTabBar(id);
            if (open)
            {
                OpenScopeTracker.TabBars++;
            }
            return open;
        }

        /// <summary>
        /// Ends the current tab bar. Only call after a <see cref="BeginTabBar"/> that
        /// returned true (imgui.h:965). Only valid inside a registered callback.
        /// </summary>
        public static void EndTabBar()
        {
            if (!CanDeclareUi)
            {
                return;
            }
            ImGuiInternal.EndTabBar();
            OpenScopeTracker.TabBars--;
        }

        /// <summary>
        /// Begins a non-closable tab inside the current tab bar. Only valid inside a
        /// registered callback, after a successful <see cref="BeginTabBar"/>.
        /// </summary>
        /// <param name="label">Tab label; also its ImGui identity.</param>
        /// <returns>
        /// True when the tab is selected (draw its content this frame). False when
        /// unselected/clipped — <see cref="EndTabItem"/> must NOT be called then
        /// (imgui.h:967). Also false when called while unavailable.
        /// Use <c>ImGuiEx.TabItem</c> (C18) for exception-safe pairing.
        /// </returns>
        public static bool BeginTabItem(string label)
        {
            if (!CanDeclareUi)
            {
                return false;
            }
            // End is paired only with a Begin that returned true (imgui.h:967).
            bool selected = ImGuiInternal.BeginTabItem(label);
            if (selected)
            {
                OpenScopeTracker.TabItems++;
            }
            return selected;
        }

        /// <summary>
        /// Ends the current tab. Only call after a <see cref="BeginTabItem"/> that
        /// returned true (imgui.h:967). Only valid inside a registered callback.
        /// </summary>
        public static void EndTabItem()
        {
            if (!CanDeclareUi)
            {
                return;
            }
            ImGuiInternal.EndTabItem();
            OpenScopeTracker.TabItems--;
        }

        /// <summary>
        /// Pushes an RGBA color onto the style-color stack, affecting all widgets drawn
        /// after this call until <see cref="PopStyleColor"/> (typically the end of the
        /// frame). Only valid inside a registered callback. Every Push must be paired
        /// with exactly one Pop before the end of the frame — use
        /// <c>ImGuiEx.StyleColor</c> (C3) for exception-safe pairing.
        /// </summary>
        /// <param name="col">The style slot to override (see <see cref="DearImGuiKSP.ImGuiCol"/>).
        /// Out-of-range values (including <see cref="ImGuiCol.COUNT"/>) are a logged no-op.</param>
        /// <param name="value">The color; components are linear RGBA in 0–1 range.</param>
        public static void PushStyleColor(ImGuiCol col, Color value)
        {
            if (!CanDeclareUi)
            {
                return;
            }
            if (!IsValidStyleColor(col))
            {
                return;
            }
            ImGuiInternal.PushStyleColor((int)col, ToImVec4(value));
            OpenScopeTracker.StyleColors++;
        }

        /// <summary>
        /// Pushes an RGBA color onto the style-color stack, affecting all widgets drawn
        /// after this call until <see cref="PopStyleColor"/> (typically the end of the
        /// frame). Only valid inside a registered callback. Byte components are
        /// normalized to 0–1 floats; every Push must be paired with exactly one Pop
        /// before the end of the frame.
        /// </summary>
        /// <param name="col">The style slot to override (see <see cref="DearImGuiKSP.ImGuiCol"/>).
        /// Out-of-range values (including <see cref="ImGuiCol.COUNT"/>) are a logged no-op.</param>
        /// <param name="value">The color; components are sRGB bytes in 0–255 range.</param>
        public static void PushStyleColor(ImGuiCol col, Color32 value)
        {
            if (!CanDeclareUi)
            {
                return;
            }
            if (!IsValidStyleColor(col))
            {
                return;
            }
            ImGuiInternal.PushStyleColor((int)col, ToImVec4(value));
            OpenScopeTracker.StyleColors++;
        }

        /// <summary>
        /// Pops <paramref name="count"/> entries from the style-color stack. Only valid
        /// inside a registered callback. Every <see cref="PushStyleColor(ImGuiCol, Color)"/>
        /// must be paired with exactly one Pop before the end of the frame.
        /// </summary>
        /// <param name="count">Number of entries to pop; must not exceed the pushed depth.</param>
        public static void PopStyleColor(int count = 1)
        {
            if (!CanDeclareUi)
            {
                return;
            }
            ImGuiInternal.PopStyleColor(count);
            OpenScopeTracker.StyleColors -= count;
        }

        /// <summary>
        /// Pushes a float style variable (e.g. rounding, border size, spacing) onto the
        /// style stack. Only valid inside a registered callback. Every Push must be
        /// paired with exactly one Pop before the end of the frame.
        /// </summary>
        /// <param name="var">The style slot to override (see <see cref="DearImGuiKSP.ImGuiStyleVar"/>).
        /// Out-of-range values (including <see cref="ImGuiStyleVar.COUNT"/>) are a logged no-op.</param>
        /// <param name="value">The new value for the slot.</param>
        public static void PushStyleVar(ImGuiStyleVar var, float value)
        {
            if (!CanDeclareUi)
            {
                return;
            }
            if (!IsValidStyleVar(var))
            {
                return;
            }
            ImGuiInternal.PushStyleVar((int)var, value);
            OpenScopeTracker.StyleVars++;
        }

        /// <summary>
        /// Pushes a 2D style variable (e.g. padding, alignment) onto the style stack.
        /// Only valid inside a registered callback. Every Push must be paired with
        /// exactly one Pop before the end of the frame.
        /// </summary>
        /// <param name="var">The style slot to override (see <see cref="DearImGuiKSP.ImGuiStyleVar"/>).
        /// Out-of-range values (including <see cref="ImGuiStyleVar.COUNT"/>) are a logged no-op.</param>
        /// <param name="value">The new value for the slot.</param>
        public static void PushStyleVar(ImGuiStyleVar var, Vector2 value)
        {
            if (!CanDeclareUi)
            {
                return;
            }
            if (!IsValidStyleVar(var))
            {
                return;
            }
            ImGuiInternal.PushStyleVar((int)var, new ImVec2(value.x, value.y));
            OpenScopeTracker.StyleVars++;
        }

        /// <summary>
        /// Pops <paramref name="count"/> entries from the style-variable stack. Only valid
        /// inside a registered callback. Every <see cref="PushStyleVar(ImGuiStyleVar, float)"/>
        /// must be paired with exactly one Pop before the end of the frame.
        /// </summary>
        /// <param name="count">Number of entries to pop; must not exceed the pushed depth.</param>
        public static void PopStyleVar(int count = 1)
        {
            if (!CanDeclareUi)
            {
                return;
            }
            ImGuiInternal.PopStyleVar(count);
            OpenScopeTracker.StyleVars -= count;
        }

        // Pure struct math, no heap allocation (hot-path checklist Check 2).
        private static ImVec4 ToImVec4(Color color)
        {
            return new ImVec4(color.r, color.g, color.b, color.a);
        }

        // Pure struct math: byte components normalized to 0–1 floats, no heap allocation.
        private static ImVec4 ToImVec4(Color32 color)
        {
            const float scale = 1f / 255f;
            return new ImVec4(color.r * scale, color.g * scale, color.b * scale, color.a * scale);
        }

        // ImGui label ID-suffix rules (imgui.cpp FindRenderedTextEnd): everything
        // from the first "##" on is identity, not display — "###" included, since
        // "###id" contains "##" at its start. Returns the label itself (no
        // allocation) when there is no suffix.
        internal static string StripIdSuffix(string label)
        {
            if (string.IsNullOrEmpty(label))
            {
                return label;
            }
            int marker = label.IndexOf("##", StringComparison.Ordinal);
            return marker >= 0 ? label.Substring(0, marker) : label;
        }

        // Character count of <see cref="StripIdSuffix(string)"/> without
        // allocating the substring (the reused-buffer encoder's form, G2-09).
        internal static int StripIdSuffixLength(string label)
        {
            if (string.IsNullOrEmpty(label))
            {
                return 0;
            }
            int marker = label.IndexOf("##", StringComparison.Ordinal);
            return marker >= 0 ? marker : label.Length;
        }

        // G2-11: window titles are process-global ImGui identities shared by
        // every mod in the process (no namespacing — spec §5.3 leaves scoping to
        // consumers). Minimal detection only: the first registered consumer to
        // begin a given title is recorded as its owner; when a DIFFERENT consumer
        // later begins the same title, one warning is logged per title (both
        // windows then share one ImGui identity — state, position, and focus
        // bleed across the two mods). Not a gate: the window still begins.
        // Process-static, mutated only on the single frame-loop thread.
        private static readonly Dictionary<string, string> s_windowTitleOwners =
            new Dictionary<string, string>(StringComparer.Ordinal);
        private static readonly HashSet<string> s_windowTitleWarnings =
            new HashSet<string>(StringComparer.Ordinal);

        // G2-11 collision bookkeeping, split out so tests can drive it without a
        // native frame (BeginWindow itself P/Invokes). Null titles are not
        // tracked (the anonymous-window case).
        internal static void WarnOnWindowTitleCollision(string name)
        {
            if (name == null)
            {
                return;
            }
            string owner;
            if (!s_windowTitleOwners.TryGetValue(name, out owner))
            {
                s_windowTitleOwners.Add(name, CurrentConsumerId);
                return;
            }
            if (string.Equals(owner, CurrentConsumerId, StringComparison.Ordinal))
            {
                return;
            }
            if (s_windowTitleWarnings.Add(name))
            {
                Log?.Warn(
                    "BeginWindow('" + name + "'): window title already in use by consumer '" +
                    (owner ?? "<unknown>") + "' (now also '" + (CurrentConsumerId ?? "<unknown>") +
                    "'). Window titles are process-global ImGui identities shared by all mods; " +
                    "both windows now share one identity. Prefix the title with your mod name to disambiguate.");
            }
        }

        // G3-23: the release native build compiles out ImGui's idx bounds assert
        // (/DNDEBUG), so an out-of-range slot (including the public COUNT sentinel)
        // would index past style.Colors natively. Reject before crossing the ABI;
        // the log string allocates only on the rejection path.
        private static bool IsValidStyleColor(ImGuiCol col)
        {
            if (col >= 0 && col < ImGuiCol.COUNT)
            {
                return true;
            }
            Log?.Warn("PushStyleColor ignored: ImGuiCol " + (int)col + " is out of range.");
            return false;
        }

        // G3-24: same as IsValidStyleColor — GetStyleVarInfo(idx) is unchecked in
        // release, so COUNT and out-of-range vars are rejected before the ABI.
        private static bool IsValidStyleVar(ImGuiStyleVar var)
        {
            if (var >= 0 && var < ImGuiStyleVar.COUNT)
            {
                return true;
            }
            Log?.Warn("PushStyleVar ignored: ImGuiStyleVar " + (int)var + " is out of range.");
            return false;
        }
    }
}
