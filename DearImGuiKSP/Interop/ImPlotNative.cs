using System;
using System.Runtime.InteropServices;

namespace DearImGuiKSP.Interop
{
    /// <summary>
    /// Plot flags for <see cref="ImPlotNative.BeginPlot"/>; values match
    /// <c>ImPlotFlags_</c> in the vendored implot <c>implot.h</c> (typedef int,
    /// implot.h:86, enum at implot.h:160). Only the values the wrapper needs;
    /// the showcase chunk (C19) can extend the subset when it needs more.
    /// </summary>
    [Flags]
    internal enum ImPlotFlags
    {
        /// <summary>Default (implot.h:161, ImPlotFlags_None = 0).</summary>
        None = 0,

        /// <summary>Hides the plot title (implot.h:162, ImPlotFlags_NoTitle = 1 &lt;&lt; 0).</summary>
        NoTitle = 1 << 0,

        /// <summary>Hides the legend (implot.h:163, ImPlotFlags_NoLegend = 1 &lt;&lt; 1).</summary>
        NoLegend = 1 << 1,

        /// <summary>The user cannot open context (right-click) menus (implot.h:166, ImPlotFlags_NoMenus = 1 &lt;&lt; 4).</summary>
        NoMenus = 1 << 4,
    }

    /// <summary>
    /// Subplot flags for <see cref="ImPlotNative.BeginSubplots"/>; values match
    /// <c>ImPlotSubplotFlags_</c> in the vendored implot <c>implot.h</c> (typedef int,
    /// implot.h:88, enum at implot.h:199-212). Only the values the wrapper needs;
    /// extend the subset when more are required.
    /// </summary>
    [Flags]
    internal enum ImPlotSubplotFlags
    {
        /// <summary>Default (implot.h:200, ImPlotSubplotFlags_None = 0).</summary>
        None = 0,

        /// <summary>Hides the subplot title (implot.h:201, NoTitle = 1 &lt;&lt; 0; also hidden by "##" prefixes).</summary>
        NoTitle = 1 << 0,

        /// <summary>Hides the legend (implot.h:202, NoLegend = 1 &lt;&lt; 1; only applicable with ShareItems).</summary>
        NoLegend = 1 << 1,

        /// <summary>The user cannot open context (right-click) menus (implot.h:203, NoMenus = 1 &lt;&lt; 2).</summary>
        NoMenus = 1 << 2,

        /// <summary>Resize splitters between subplot cells are not provided (implot.h:204, NoResize = 1 &lt;&lt; 3).</summary>
        NoResize = 1 << 3,

        /// <summary>Subplot edges are not aligned vertically/horizontally (implot.h:205, NoAlign = 1 &lt;&lt; 4).</summary>
        NoAlign = 1 << 4,

        /// <summary>Items across all subplots share one legend (implot.h:206, ShareItems = 1 &lt;&lt; 5).</summary>
        ShareItems = 1 << 5,

        /// <summary>Link the y-axis limits of all plots in each row (implot.h:207, LinkRows = 1 &lt;&lt; 6).</summary>
        LinkRows = 1 << 6,

        /// <summary>Link the x-axis limits of all plots in each column (implot.h:208, LinkCols = 1 &lt;&lt; 7).</summary>
        LinkCols = 1 << 7,

        /// <summary>Link the x-axis limits in every plot (implot.h:209, LinkAllX = 1 &lt;&lt; 8).</summary>
        LinkAllX = 1 << 8,

        /// <summary>Link the y-axis limits in every plot (implot.h:210, LinkAllY = 1 &lt;&lt; 9).</summary>
        LinkAllY = 1 << 9,

        /// <summary>Subplots are added in column-major order (implot.h:211, ColMajor = 1 &lt;&lt; 10).</summary>
        ColMajor = 1 << 10,
    }

    /// <summary>
    /// Axis flags for <see cref="ImPlotNative.SetupAxesAutoFit"/>; values match
    /// <c>ImPlotAxisFlags_</c> in the vendored implot <c>implot.h</c> (typedef int,
    /// implot.h:87, enum at implot.h:175). Only the values the wrapper needs;
    /// extend the subset when more are required.
    /// </summary>
    [Flags]
    internal enum ImPlotAxisFlags
    {
        /// <summary>Default (implot.h:176, ImPlotAxisFlags_None = 0).</summary>
        None = 0,

        /// <summary>Axis auto-fits to data extents every frame (implot.h:188, ImPlotAxisFlags_AutoFit = 1 &lt;&lt; 11).</summary>
        AutoFit = 1 << 11,
    }

    /// <summary>
    /// Blittable mirror of cimplot's <c>ImPlotPoint_c</c> (<c>struct</c>,
    /// vendor/cimplot/cimplot.h:837-840), the by-value return of
    /// <c>ImPlot_GetPlotMousePos</c> (cimplot.h:1345). Two sequential doubles
    /// (Pack 8), 16 bytes, returned by value, Cdecl — the I-07 struct-by-value ABI
    /// discipline applied to a return instead of a parameter.
    /// </summary>
    internal struct ImPlotPoint
    {
        public double X;    // cimplot.h:838
        public double Y;    // cimplot.h:839
    }

    /// <summary>
    /// Blittable mirror of cimplot's <c>ImPlotSpec_c</c> (<c>struct</c>,
    /// vendor/cimplot/cimplot.h:856-875), the item-style tail parameter of the
    /// v1.0 <c>ImPlot_PlotLine_*</c> family. Field-for-field identical in
    /// declaration order, so the sequential layout (default Pack 8) matches the
    /// native struct on Win64: 144 bytes, passed by value, Cdecl.
    /// </summary>
    internal struct ImPlotSpec
    {
        public ImVec4 LineColor;            // cimplot.h:858
        public IntPtr LineColors;           // cimplot.h:859
        public float LineWeight;            // cimplot.h:860
        public ImVec4 FillColor;            // cimplot.h:861
        public IntPtr FillColors;           // cimplot.h:862
        public float FillAlpha;             // cimplot.h:863
        public int Marker;                  // cimplot.h:864 (ImPlotMarker, typedef int, implot.h:118)
        public float MarkerSize;            // cimplot.h:865
        public IntPtr MarkerSizes;          // cimplot.h:866
        public ImVec4 MarkerLineColor;      // cimplot.h:867
        public IntPtr MarkerLineColors;     // cimplot.h:868
        public ImVec4 MarkerFillColor;      // cimplot.h:869
        public IntPtr MarkerFillColors;     // cimplot.h:870
        public float Size;                  // cimplot.h:871
        public int Offset;                  // cimplot.h:872
        public int Stride;                  // cimplot.h:873
        public int Flags;                   // cimplot.h:874 (ImPlotItemFlags, typedef int; None = 0, implot.h:253-254)
    }

    /// <summary>
    /// Raw + safe P/Invoke surface for cimplot (chunk C13, M4 ImPlot integration).
    /// Same seam as <see cref="ImGuiNative"/> and <see cref="ExtensionShimsNative"/>:
    /// implicit <c>[DllImport("DearImGuiKSPNative")]</c> resolves against the module
    /// NativeBridge already LoadLibrary'd. cimplot <c>bool</c> is the same 1-byte
    /// C++ bool as cimgui, so returns use <see cref="UnmanagedType.I1"/>. C9 owns
    /// ImGuiNative.cs / ImGuiInternal.cs; ID-bearing titles/labels route through
    /// <see cref="ImGuiInternal.ToIdUtf8"/> (C14, G3-20 closure: the docs/10 §6
    /// empty-label guard applies to plot identity too).
    /// </summary>
    internal static class ImPlotNative
    {
        private const string Dll = "DearImGuiKSPNative";

        // Mirror of C++ ImPlot::ImPlotSpec()'s field defaults (implot.h:515-532).
        // The AUTO sentinels are load-bearing: a zeroed spec would draw a transparent
        // 0-weight line (LineColor (0,0,0,0), LineWeight 0) and read every point
        // through Stride 0 instead of sizeof(T). Read-only: passed by value, never
        // retained by native code.
        private static readonly ImPlotSpec DefaultSpec = new ImPlotSpec
        {
            LineColor = new ImVec4(0f, 0f, 0f, -1f),        // IMPLOT_AUTO_COL = ImVec4(0,0,0,-1) (implot.h:74)
            LineColors = IntPtr.Zero,
            LineWeight = 1f,
            FillColor = new ImVec4(0f, 0f, 0f, -1f),        // IMPLOT_AUTO_COL (implot.h:74)
            FillColors = IntPtr.Zero,
            FillAlpha = 1f,
            Marker = -2,                                    // ImPlotMarker_None (implot.h:439)
            MarkerSize = 4f,
            MarkerSizes = IntPtr.Zero,
            MarkerLineColor = new ImVec4(0f, 0f, 0f, -1f),  // IMPLOT_AUTO_COL (implot.h:74)
            MarkerLineColors = IntPtr.Zero,
            MarkerFillColor = new ImVec4(0f, 0f, 0f, -1f),  // IMPLOT_AUTO_COL (implot.h:74)
            MarkerFillColors = IntPtr.Zero,
            Size = 4f,
            Offset = 0,
            Stride = -1,                                    // IMPLOT_AUTO: sizeof(T) at consumption (implot.h:72, 531)
            Flags = 0,                                      // ImPlotItemFlags_None (implot.h:254)
        };

        // CIMGUI_API bool ImPlot_BeginPlot(const char* title_id, const ImVec2_c size, ImPlotFlags flags);
        // (vendor/cimplot/cimplot.h:1015)
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool ImPlot_BeginPlot([In] byte[] title_id, ImVec2 size, int flags);

        // CIMGUI_API void ImPlot_EndPlot(void);
        // (vendor/cimplot/cimplot.h:1016)
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern void ImPlot_EndPlot();

        // CIMGUI_API void ImPlot_PlotLine_FloatPtrInt(const char* label_id, const float* values, int count, double xscale, double xstart, const ImPlotSpec_c spec);
        // (vendor/cimplot/cimplot.h:1040)
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static unsafe extern void ImPlot_PlotLine_FloatPtrInt(
            [In] byte[] label_id, float* values, int count, double xscale, double xstart, ImPlotSpec spec);

        // CIMGUI_API void ImPlot_PlotLine_doublePtrInt(const char* label_id, const double* values, int count, double xscale, double xstart, const ImPlotSpec_c spec);
        // (vendor/cimplot/cimplot.h:1041)
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static unsafe extern void ImPlot_PlotLine_doublePtrInt(
            [In] byte[] label_id, double* values, int count, double xscale, double xstart, ImPlotSpec spec);

        // CIMGUI_API bool ImPlot_BeginSubplots(const char* title_id,int rows,int cols,const ImVec2_c size,ImPlotSubplotFlags flags,float* row_ratios,float* col_ratios);
        // (vendor/cimplot/cimplot.h:1017) — the v1.0 signature carries the two ratio
        // tail pointers; the C++ defaults are nullptr (vendor/implot/implot.h:828-829),
        // so the safe wrapper passes IntPtr.Zero for both.
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool ImPlot_BeginSubplots(
            [In] byte[] title_id, int rows, int cols, ImVec2 size, int flags, IntPtr row_ratios, IntPtr col_ratios);

        // CIMGUI_API void ImPlot_EndSubplots(void);
        // (vendor/cimplot/cimplot.h:1018)
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern void ImPlot_EndSubplots();

        // CIMGUI_API ImPlotPoint_c ImPlot_GetPlotMousePos(ImAxis x_axis,ImAxis y_axis);
        // (vendor/cimplot/cimplot.h:1345) — returns ImPlotPoint_c BY VALUE (16 bytes,
        // two doubles, cimplot.h:837-840); ImAxis is typedef int (cimplot.h:35), and
        // -1 is IMPLOT_AUTO "use the current axes" (vendor/implot/implot.h:72).
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern ImPlotPoint ImPlot_GetPlotMousePos(int x_axis, int y_axis);

        // CIMGUI_API bool ImPlot_IsPlotHovered(void);
        // (vendor/cimplot/cimplot.h:1347) — the no-argument variant, true when the
        // mouse is within the current plot's plotting area (vendor/implot/implot.h:1128).
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool ImPlot_IsPlotHovered();

        // CIMGUI_API void ImPlot_SetupAxes(const char* x_label,const char* y_label,ImPlotAxisFlags x_flags,ImPlotAxisFlags y_flags);
        // (vendor/cimplot/cimplot.h:1030) — labels passed NULL (the C++ defaults,
        // implot.h:888: no axis labels).
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern void ImPlot_SetupAxes([In] byte[] x_label, [In] byte[] y_label, int x_flags, int y_flags);

        /// <summary>
        /// Begins a plot. The title is encoded per call through
        /// <see cref="ImGuiInternal.ToIdUtf8"/> (it is ImPlot identity: null/empty
        /// gets the invisible sentinel instead of an empty-string ID); plot titles
        /// are short-lived and the buffer is not retained.
        /// </summary>
        /// <returns>The ImPlot BeginPlot result (false = plot collapsed/clipped).</returns>
        internal static bool BeginPlot(string title, ImVec2 size, ImPlotFlags flags)
        {
            return ImPlot_BeginPlot(ImGuiInternal.ToIdUtf8(title), size, (int)flags);
        }

        /// <summary>Ends the current plot. Call exactly once per BeginPlot that returned true.</summary>
        internal static void EndPlot()
        {
            ImPlot_EndPlot();
        }

        /// <summary>
        /// Draws a y-series line plot from an already-pinned buffer (the caller pins
        /// with <c>fixed</c>; the pin releases at scope exit, nothing native is
        /// retained). The series label is encoded per call through
        /// <see cref="ImGuiInternal.ToIdUtf8"/> (legend identity: null/empty gets the
        /// invisible sentinel); series labels are short-lived and the buffer is
        /// not retained. Passes the library-wide default <see cref="ImPlotSpec"/>
        /// (AUTO colors, AUTO stride = sizeof(float)).
        /// </summary>
        internal static unsafe void PlotLine(string label, float* values, int count, double xscale, double xstart)
        {
            ImPlot_PlotLine_FloatPtrInt(ImGuiInternal.ToIdUtf8(label), values, count, xscale, xstart, DefaultSpec);
        }

        /// <summary>
        /// Double-precision variant of the float <see cref="PlotLine(string, float*, int, double, double)"/>.
        /// Same pinning and label-encoding contract; default spec's AUTO stride resolves
        /// to sizeof(double).
        /// </summary>
        internal static unsafe void PlotLine(string label, double* values, int count, double xscale, double xstart)
        {
            ImPlot_PlotLine_doublePtrInt(ImGuiInternal.ToIdUtf8(label), values, count, xscale, xstart, DefaultSpec);
        }

        /// <summary>
        /// Begins a subplot grid. The title is encoded per call through
        /// <see cref="ImGuiInternal.ToIdUtf8"/> (subplot identity, same rule as
        /// <see cref="BeginPlot"/>); titles are short-lived and the buffer is not
        /// retained. The row/column ratio pointers of
        /// the v1.0 signature (cimplot.h:1017) are passed null, i.e. evenly sized cells
        /// (the C++ defaults, implot.h:828-829).
        /// </summary>
        /// <returns>The ImPlot BeginSubplots result (false = grid collapsed/clipped).</returns>
        internal static bool BeginSubplots(string title, int rows, int cols, ImVec2 size, ImPlotSubplotFlags flags)
        {
            return ImPlot_BeginSubplots(ImGuiInternal.ToIdUtf8(title), rows, cols, size, (int)flags, IntPtr.Zero, IntPtr.Zero);
        }

        /// <summary>Ends the current subplot grid. Call exactly once per BeginSubplots that returned true.</summary>
        internal static void EndSubplots()
        {
            ImPlot_EndSubplots();
        }

        /// <summary>
        /// The current plot's mouse position in plot coordinates. Both axes are passed
        /// as -1 (IMPLOT_AUTO, implot.h:72), i.e. the current axes. Only meaningful
        /// between a successful BeginPlot and its EndPlot.
        /// </summary>
        internal static ImPlotPoint GetPlotMousePos()
        {
            return ImPlot_GetPlotMousePos(-1, -1);
        }

        /// <summary>
        /// True while the mouse hovers the current plot's plotting area. Only meaningful
        /// between a successful BeginPlot and its EndPlot.
        /// </summary>
        internal static bool IsPlotHovered()
        {
            return ImPlot_IsPlotHovered();
        }

        /// <summary>
        /// Sets both axes of the current plot to auto-fit to data extents every frame
        /// (ImPlotAxisFlags_AutoFit, implot.h:188) with no axis labels (NULL labels,
        /// the C++ defaults implot.h:888). Only meaningful between a successful
        /// BeginPlot and its EndPlot; ImPlot applies SetupAxes per frame.
        /// </summary>
        internal static void SetupAxesAutoFit()
        {
            ImPlot_SetupAxes(null, null, (int)ImPlotAxisFlags.AutoFit, (int)ImPlotAxisFlags.AutoFit);
        }
    }
}
