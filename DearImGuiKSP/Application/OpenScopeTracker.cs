using DearImGuiKSP.Interop;

namespace DearImGuiKSP.Application
{
    /// <summary>
    /// Closes one kind of ImGui/ImGui Plot scope during a fault unwind. The
    /// production implementation forwards to the native wrappers; tests supply
    /// a recorder.
    /// </summary>
    internal interface IScopeCloser
    {
        void EndWindow();
        void EndScrollRegion();
        void EndTabItem();
        void EndTabBar();
        void EndPlot();
        void EndSubplots();
        void PopStyleColor();
        void PopStyleVar();
    }

    /// <summary>
    /// Per-frame count of facade scopes a consumer has opened but not closed
    /// (G2-05). Facade Begin*/Push* calls increment and their End*/Pop* pairs
    /// decrement, so a balanced consumer nets to zero. The frame loop resets the
    /// counts when a frame opens; when a consumer callback throws, the fault
    /// barrier unwinds whatever it left open so later consumers in the same
    /// frame are not mis-parented into its windows or styled by its leftovers.
    /// Counts are plain internal statics (the facade wiring-hook pattern);
    /// only the frame loop thread touches them.
    /// </summary>
    internal static class OpenScopeTracker
    {
        internal static int Windows;
        internal static int ScrollRegions;
        internal static int TabBars;
        internal static int TabItems;
        internal static int Plots;
        internal static int Subplots;
        internal static int StyleColors;
        internal static int StyleVars;

        /// <summary>Total open scopes across all kinds (diagnostics/tests).</summary>
        internal static int TotalOpen =>
            Windows + ScrollRegions + TabBars + TabItems + Plots + Subplots + StyleColors + StyleVars;

        /// <summary>Zeroes all counts; called by the frame loop when a frame opens.</summary>
        internal static void Reset()
        {
            Windows = 0;
            ScrollRegions = 0;
            TabBars = 0;
            TabItems = 0;
            Plots = 0;
            Subplots = 0;
            StyleColors = 0;
            StyleVars = 0;
        }

        /// <summary>
        /// Closes every tracked open scope, innermost first: style stacks are
        /// per-window and must pop before their window ends; plots live inside
        /// subplot cells; tab items inside tab bars; child regions before their
        /// parent window.
        /// </summary>
        internal static void Unwind(IScopeCloser closer)
        {
            while (StyleVars > 0) { StyleVars--; closer.PopStyleVar(); }
            while (StyleColors > 0) { StyleColors--; closer.PopStyleColor(); }
            while (Plots > 0) { Plots--; closer.EndPlot(); }
            while (Subplots > 0) { Subplots--; closer.EndSubplots(); }
            while (TabItems > 0) { TabItems--; closer.EndTabItem(); }
            while (TabBars > 0) { TabBars--; closer.EndTabBar(); }
            while (ScrollRegions > 0) { ScrollRegions--; closer.EndScrollRegion(); }
            while (Windows > 0) { Windows--; closer.EndWindow(); }
        }
    }

    /// <summary>IScopeCloser over the native Interop wrappers.</summary>
    internal sealed class NativeScopeCloser : IScopeCloser
    {
        internal static readonly NativeScopeCloser Instance = new NativeScopeCloser();

        private NativeScopeCloser() { }

        public void EndWindow() { ImGuiInternal.EndWindow(); }
        public void EndScrollRegion() { ImGuiInternal.EndScrollRegion(); }
        public void EndTabItem() { ImGuiInternal.EndTabItem(); }
        public void EndTabBar() { ImGuiInternal.EndTabBar(); }
        public void EndPlot() { ImPlotNative.EndPlot(); }
        public void EndSubplots() { ImPlotNative.EndSubplots(); }
        public void PopStyleColor() { ImGuiInternal.PopStyleColor(1); }
        public void PopStyleVar() { ImGuiInternal.PopStyleVar(1); }
    }
}
