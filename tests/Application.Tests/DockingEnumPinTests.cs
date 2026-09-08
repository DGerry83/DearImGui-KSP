using DearImGuiKSP;
using Xunit;

namespace Application.Tests
{
    /// <summary>
    /// Docking enum value pins (ISSUES #011): the public
    /// <see cref="ImGuiDockNodeFlags"/> and <see cref="ImGuiDir"/> mirror
    /// <c>ImGuiDockNodeFlags_</c> (imgui.h:1551-1565) and <c>ImGuiDir</c>
    /// (imgui.h:1617-1625) of the pinned native header
    /// <c>C:\Users\Matt\source\repos\cimgui\imgui\imgui.h</c>
    /// (IMGUI_VERSION "1.92.9", IMGUI_VERSION_NUM 19290 — the sibling clone the
    /// native core compiles from, per DearImGuiKSPNative/vendor/PIN_RECORD.md).
    /// The values cross the ABI as raw <c>int</c>s into the native DLL, where
    /// the release build's /DNDEBUG has compiled out every range assert — a
    /// drifted value silently changes docking behavior. Explicit-value pattern
    /// (ImPlotFlagsTests), because ImGuiDockNodeFlags_ carries duplicate-value
    /// 1.90 aliases (NoSplit, NoDockingInCentralNode, imgui.h:1564-1565) that
    /// make the Enum.GetNames order-equality pattern unreliable; the aliases are
    /// deliberately not mirrored in the public enum. Regenerate from the header
    /// when the pin moves; do not hand-edit to match the C# enum.
    /// </summary>
    public class DockingEnumPinTests
    {
        [Fact]
        public void ImGuiDockNodeFlags_Subset_MatchesPinnedHeader()
        {
            Assert.Equal(0, (int)ImGuiDockNodeFlags.None);                       // imgui.h:1553
            Assert.Equal(1 << 0, (int)ImGuiDockNodeFlags.KeepAliveOnly);         // imgui.h:1554
            Assert.Equal(1 << 2, (int)ImGuiDockNodeFlags.NoDockingOverCentralNode); // imgui.h:1556
            Assert.Equal(1 << 3, (int)ImGuiDockNodeFlags.PassthruCentralNode);   // imgui.h:1557
            Assert.Equal(1 << 4, (int)ImGuiDockNodeFlags.NoDockingSplit);        // imgui.h:1558
            Assert.Equal(1 << 5, (int)ImGuiDockNodeFlags.NoResize);              // imgui.h:1559
            Assert.Equal(1 << 6, (int)ImGuiDockNodeFlags.AutoHideTabBar);        // imgui.h:1560
            Assert.Equal(1 << 7, (int)ImGuiDockNodeFlags.NoUndocking);           // imgui.h:1561
        }

        [Fact]
        public void ImGuiDir_Table_MatchesPinnedHeader()
        {
            Assert.Equal(-1, (int)ImGuiDir.None);   // imgui.h:1619
            Assert.Equal(0, (int)ImGuiDir.Left);    // imgui.h:1620
            Assert.Equal(1, (int)ImGuiDir.Right);   // imgui.h:1621
            Assert.Equal(2, (int)ImGuiDir.Up);      // imgui.h:1622
            Assert.Equal(3, (int)ImGuiDir.Down);    // imgui.h:1623
            Assert.Equal(4, (int)ImGuiDir.COUNT);   // imgui.h:1624
        }
    }
}
