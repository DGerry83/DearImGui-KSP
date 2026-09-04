using System;
using System.Runtime.InteropServices;
using System.Text;

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
    /// ImGuiNative.cs / ImGuiInternal.cs, so this file is self-contained (its own
    /// ToUtf8).
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

        /// <summary>
        /// Begins a plot. The label is encoded to a null-terminated UTF-8 buffer per
        /// call (same ToUtf8 convention as <c>ImGuiInternal</c>/<c>ExtensionShimsNative</c>);
        /// plot titles are short-lived and the buffer is not retained.
        /// </summary>
        /// <returns>The ImPlot BeginPlot result (false = plot collapsed/clipped).</returns>
        internal static bool BeginPlot(string title, ImVec2 size, ImPlotFlags flags)
        {
            return ImPlot_BeginPlot(ToUtf8(title), size, (int)flags);
        }

        /// <summary>Ends the current plot. Call exactly once per BeginPlot that returned true.</summary>
        internal static void EndPlot()
        {
            ImPlot_EndPlot();
        }

        /// <summary>
        /// Draws a y-series line plot from an already-pinned buffer (the caller pins
        /// with <c>fixed</c>; the pin releases at scope exit, nothing native is
        /// retained). The label is encoded to a null-terminated UTF-8 buffer per call
        /// (same ToUtf8 convention); series labels are short-lived and the buffer is
        /// not retained. Passes the library-wide default <see cref="ImPlotSpec"/>
        /// (AUTO colors, AUTO stride = sizeof(float)).
        /// </summary>
        internal static unsafe void PlotLine(string label, float* values, int count, double xscale, double xstart)
        {
            ImPlot_PlotLine_FloatPtrInt(ToUtf8(label), values, count, xscale, xstart, DefaultSpec);
        }

        /// <summary>
        /// Double-precision variant of the float <see cref="PlotLine(string, float*, int, double, double)"/>.
        /// Same pinning and label-encoding contract; default spec's AUTO stride resolves
        /// to sizeof(double).
        /// </summary>
        internal static unsafe void PlotLine(string label, double* values, int count, double xscale, double xstart)
        {
            ImPlot_PlotLine_doublePtrInt(ToUtf8(label), values, count, xscale, xstart, DefaultSpec);
        }

        // Null-terminated UTF-8. Null becomes "\0" (empty string).
        // Duplicated from ImGuiInternal (C9-owned; not editable here) — C10 precedent.
        private static byte[] ToUtf8(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return new byte[] { 0 };
            }
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            byte[] terminated = new byte[bytes.Length + 1];
            for (int i = 0; i < bytes.Length; i++)
            {
                terminated[i] = bytes[i];
            }
            terminated[bytes.Length] = 0;
            return terminated;
        }
    }
}
