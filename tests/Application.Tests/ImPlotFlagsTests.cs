using DearImGuiKSP.Interop;
using Xunit;

namespace Application.Tests
{
    /// <summary>
    /// ImPlot flag value pins (C13, M4 ImPlot integration): the internal
    /// <see cref="ImPlotFlags"/> subset must stay bit-identical to the vendored
    /// <c>ImPlotFlags_</c> enum in <c>DearImGuiKSPNative/vendor/implot/implot.h</c>
    /// (enum at line 160) — a drifted flag would silently change plot behavior
    /// in-game. Same precedent as the C8 ThemePresetsTests anchors.
    /// </summary>
    public class ImPlotFlagsTests
    {
        [Fact]
        public void ImPlotFlags_Subset_MatchesVendoredImPlotHeader()
        {
            Assert.Equal(0, (int)ImPlotFlags.None);
            Assert.Equal(1 << 0, (int)ImPlotFlags.NoTitle);   // implot.h:162
            Assert.Equal(1 << 1, (int)ImPlotFlags.NoLegend);  // implot.h:163
            Assert.Equal(1 << 4, (int)ImPlotFlags.NoMenus);   // implot.h:166
        }
    }
}
