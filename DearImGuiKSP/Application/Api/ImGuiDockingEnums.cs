namespace DearImGuiKSP
{
    /// <summary>
    /// Dock-node flags for <see cref="DearImGuiKSP.DockSpace(uint, UnityEngine.Vector2, ImGuiDockNodeFlags)"/>,
    /// <see cref="DearImGuiKSP.DockSpaceOverViewport(uint, ImGuiDockNodeFlags)"/>, and the
    /// DockBuilder methods; values match <c>ImGuiDockNodeFlags_</c> in imgui.h
    /// (imgui.h:1551-1565, imgui 1.92.9). Subset: the eight non-deprecated bits.
    /// The 1.90-renamed aliases (<c>NoSplit</c>, <c>NoDockingInCentralNode</c>,
    /// imgui.h:1564-1565) are intentionally not mirrored — use the modern names.
    /// Single source of truth: the Interop layer consumes these as <c>int</c>;
    /// do not duplicate this table.
    /// </summary>
    [System.Flags]
    public enum ImGuiDockNodeFlags
    {
        /// <summary>No flags (imgui.h:1553).</summary>
        None = 0,

        /// <summary>Don't display the dockspace node but keep it alive; windows docked into it won't be undocked (imgui.h:1554, 1 &lt;&lt; 0).</summary>
        KeepAliveOnly = 1 << 0,

        /// <summary>Disable docking over the Central Node, which is always kept empty (imgui.h:1556, 1 &lt;&lt; 2).</summary>
        NoDockingOverCentralNode = 1 << 2,

        /// <summary>
        /// Enable passthru dockspace: the dockspace renders no background over
        /// the Central Node when empty and lets inputs pass through (imgui.h:1557,
        /// 1 &lt;&lt; 3).
        /// </summary>
        PassthruCentralNode = 1 << 3,

        /// <summary>Disable other windows/nodes from splitting this node (imgui.h:1558, 1 &lt;&lt; 4).</summary>
        NoDockingSplit = 1 << 4,

        /// <summary>Disable resizing the node with the splitter/separators; useful with programmatic layouts (imgui.h:1559, 1 &lt;&lt; 5).</summary>
        NoResize = 1 << 5,

        /// <summary>The tab bar automatically hides when a single window is in the dock node (imgui.h:1560, 1 &lt;&lt; 6).</summary>
        AutoHideTabBar = 1 << 6,

        /// <summary>Disable undocking from this node (imgui.h:1561, 1 &lt;&lt; 7).</summary>
        NoUndocking = 1 << 7,
    }

    /// <summary>
    /// Cardinal direction for <see cref="DearImGuiKSP.DockBuilderSplitNode(uint, ImGuiDir, float, out uint, out uint)"/>;
    /// values match <c>ImGuiDir</c> in imgui.h (imgui.h:1617-1625, imgui 1.92.9).
    /// Single source of truth: the Interop layer consumes these as <c>int</c>;
    /// do not duplicate this table.
    /// </summary>
    public enum ImGuiDir
    {
        /// <summary>No direction (imgui.h:1619).</summary>
        None = -1,

        /// <summary>Left (imgui.h:1620).</summary>
        Left = 0,

        /// <summary>Right (imgui.h:1621).</summary>
        Right = 1,

        /// <summary>Up (imgui.h:1622).</summary>
        Up = 2,

        /// <summary>Down (imgui.h:1623).</summary>
        Down = 3,

        /// <summary>
        /// Sentinel equal to the number of directions; not a direction itself.
        /// Kept at the end of the table for 1:1 parity with <c>ImGuiDir</c>
        /// (imgui.h:1624).
        /// </summary>
        COUNT,
    }
}
