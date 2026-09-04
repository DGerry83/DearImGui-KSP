namespace DearImGuiKSP
{
    /// <summary>
    /// Style-color identifiers for <see cref="DearImGuiKSP.PushStyleColor(ImGuiCol, Color)"/>;
    /// values match <c>ImGuiCol_</c> in imgui.h (imgui.h:1839-1912, sequential from Text = 0).
    /// Full table — theme presets apply every slot. Single source of truth: the Interop
    /// layer consumes these as <c>int</c>; do not duplicate this table.
    /// </summary>
    public enum ImGuiCol
    {
        /// <summary>Text color (imgui.h:1841).</summary>
        Text = 0,
        /// <summary>Disabled text color (imgui.h:1842).</summary>
        TextDisabled,
        /// <summary>Background of normal windows (imgui.h:1843).</summary>
        WindowBg,
        /// <summary>Background of child windows (imgui.h:1844).</summary>
        ChildBg,
        /// <summary>Background of popups, menus, tooltips (imgui.h:1845).</summary>
        PopupBg,
        /// <summary>Border color (imgui.h:1846).</summary>
        Border,
        /// <summary>Border shadow (imgui.h:1847).</summary>
        BorderShadow,
        /// <summary>Background of checkbox, radio button, plot, slider, text input (imgui.h:1848).</summary>
        FrameBg,
        /// <summary>Hovered frame background (imgui.h:1849).</summary>
        FrameBgHovered,
        /// <summary>Active frame background (imgui.h:1850).</summary>
        FrameBgActive,
        /// <summary>Title bar (imgui.h:1851).</summary>
        TitleBg,
        /// <summary>Title bar when focused (imgui.h:1852).</summary>
        TitleBgActive,
        /// <summary>Title bar when collapsed (imgui.h:1853).</summary>
        TitleBgCollapsed,
        /// <summary>Menu bar background (imgui.h:1854).</summary>
        MenuBarBg,
        /// <summary>Scrollbar background (imgui.h:1855).</summary>
        ScrollbarBg,
        /// <summary>Scrollbar grab (imgui.h:1856).</summary>
        ScrollbarGrab,
        /// <summary>Hovered scrollbar grab (imgui.h:1857).</summary>
        ScrollbarGrabHovered,
        /// <summary>Active scrollbar grab (imgui.h:1858).</summary>
        ScrollbarGrabActive,
        /// <summary>Checkbox tick and radio button circle (imgui.h:1859).</summary>
        CheckMark,
        /// <summary>Checkbox background when selected (imgui.h:1860).</summary>
        CheckboxSelectedBg,
        /// <summary>Slider grab (imgui.h:1861).</summary>
        SliderGrab,
        /// <summary>Active slider grab (imgui.h:1862).</summary>
        SliderGrabActive,
        /// <summary>Button background (imgui.h:1863).</summary>
        Button,
        /// <summary>Hovered button background (imgui.h:1864).</summary>
        ButtonHovered,
        /// <summary>Active button background (imgui.h:1865).</summary>
        ButtonActive,
        /// <summary>Header background (collapsing header, tree node, selectable, menu item) (imgui.h:1866).</summary>
        Header,
        /// <summary>Hovered header background (imgui.h:1867).</summary>
        HeaderHovered,
        /// <summary>Active header background (imgui.h:1868).</summary>
        HeaderActive,
        /// <summary>Separator (imgui.h:1869).</summary>
        Separator,
        /// <summary>Hovered separator (imgui.h:1870).</summary>
        SeparatorHovered,
        /// <summary>Active separator (imgui.h:1871).</summary>
        SeparatorActive,
        /// <summary>Resize grip in window corners (imgui.h:1872).</summary>
        ResizeGrip,
        /// <summary>Hovered resize grip (imgui.h:1873).</summary>
        ResizeGripHovered,
        /// <summary>Active resize grip (imgui.h:1874).</summary>
        ResizeGripActive,
        /// <summary>InputText cursor/caret (imgui.h:1875).</summary>
        InputTextCursor,
        /// <summary>Tab background when hovered (imgui.h:1876).</summary>
        TabHovered,
        /// <summary>Tab background when tab bar is focused and tab is unselected (imgui.h:1877).</summary>
        Tab,
        /// <summary>Tab background when tab bar is focused and tab is selected (imgui.h:1878).</summary>
        TabSelected,
        /// <summary>Tab horizontal overline when tab bar is focused and tab is selected (imgui.h:1879).</summary>
        TabSelectedOverline,
        /// <summary>Tab background when tab bar is unfocused and tab is unselected (imgui.h:1880).</summary>
        TabDimmed,
        /// <summary>Tab background when tab bar is unfocused and tab is selected (imgui.h:1881).</summary>
        TabDimmedSelected,
        /// <summary>Tab horizontal overline when tab bar is unfocused and tab is selected (imgui.h:1882).</summary>
        TabDimmedSelectedOverline,
        /// <summary>Preview overlay color when docking (imgui.h:1883).</summary>
        DockingPreview,
        /// <summary>Background of empty docking nodes (imgui.h:1884).</summary>
        DockingEmptyBg,
        /// <summary>Plot lines (imgui.h:1885).</summary>
        PlotLines,
        /// <summary>Hovered plot lines (imgui.h:1886).</summary>
        PlotLinesHovered,
        /// <summary>Plot histogram (imgui.h:1887).</summary>
        PlotHistogram,
        /// <summary>Hovered plot histogram (imgui.h:1888).</summary>
        PlotHistogramHovered,
        /// <summary>Table header background (imgui.h:1889).</summary>
        TableHeaderBg,
        /// <summary>Table outer and header borders (imgui.h:1890).</summary>
        TableBorderStrong,
        /// <summary>Table inner borders (imgui.h:1891).</summary>
        TableBorderLight,
        /// <summary>Table row background, even rows (imgui.h:1892).</summary>
        TableRowBg,
        /// <summary>Table row background, odd rows (imgui.h:1893).</summary>
        TableRowBgAlt,
        /// <summary>Hyperlink color (imgui.h:1894).</summary>
        TextLink,
        /// <summary>Selected text inside an InputText (imgui.h:1895).</summary>
        TextSelectedBg,
        /// <summary>Tree node hierarchy outlines (imgui.h:1896).</summary>
        TreeLines,
        /// <summary>Rectangle border highlighting a drop target (imgui.h:1897).</summary>
        DragDropTarget,
        /// <summary>Rectangle background highlighting a drop target (imgui.h:1898).</summary>
        DragDropTargetBg,
        /// <summary>Unsaved document marker (imgui.h:1899).</summary>
        UnsavedMarker,
        /// <summary>Keyboard/gamepad navigation cursor color (imgui.h:1900).</summary>
        NavCursor,
        /// <summary>Highlight window during Ctrl+Tab windowing (imgui.h:1901).</summary>
        NavWindowingHighlight,
        /// <summary>Dim background during Ctrl+Tab windowing (imgui.h:1902).</summary>
        NavWindowingDimBg,
        /// <summary>Dim background behind a modal window (imgui.h:1903).</summary>
        ModalWindowDimBg,
        /// <summary>
        /// Sentinel equal to the number of style-color slots; not a color itself.
        /// Kept at the end of the table for 1:1 parity with <c>ImGuiCol_</c> (imgui.h:1904).
        /// </summary>
        COUNT,
    }

    /// <summary>
    /// Style-variable identifiers for <see cref="DearImGuiKSP.PushStyleVar(ImGuiStyleVar, float)"/>;
    /// values match <c>ImGuiStyleVar_</c> in imgui.h (imgui.h:1922-1970, sequential from Alpha = 0).
    /// Full table — sequential enum, cheap to verify whole. Whether a slot takes a float or a
    /// <see cref="UnityEngine.Vector2"/> is defined by the style table in imgui.h; passing the
    /// wrong type is an ImGui assert, not a managed exception. Single source of truth: the
    /// Interop layer consumes these as <c>int</c>; do not duplicate this table.
    /// </summary>
    public enum ImGuiStyleVar
    {
        /// <summary>Global alpha (float) (imgui.h:1925).</summary>
        Alpha = 0,
        /// <summary>Additional alpha multiplier for disabled widgets (float) (imgui.h:1926).</summary>
        DisabledAlpha,
        /// <summary>Padding within a window (Vector2) (imgui.h:1927).</summary>
        WindowPadding,
        /// <summary>Radius of window corners rounding (float) (imgui.h:1928).</summary>
        WindowRounding,
        /// <summary>Thickness of border around windows (float) (imgui.h:1929).</summary>
        WindowBorderSize,
        /// <summary>Minimum window size (Vector2) (imgui.h:1930).</summary>
        WindowMinSize,
        /// <summary>Alignment for title bar text (Vector2) (imgui.h:1931).</summary>
        WindowTitleAlign,
        /// <summary>Radius of child window corners rounding (float) (imgui.h:1932).</summary>
        ChildRounding,
        /// <summary>Thickness of border around child windows (float) (imgui.h:1933).</summary>
        ChildBorderSize,
        /// <summary>Radius of popup window corners rounding (float) (imgui.h:1934).</summary>
        PopupRounding,
        /// <summary>Thickness of border around popup/tooltip windows (float) (imgui.h:1935).</summary>
        PopupBorderSize,
        /// <summary>Padding within a frame (Vector2) (imgui.h:1936).</summary>
        FramePadding,
        /// <summary>Radius of frame corners rounding (float) (imgui.h:1937).</summary>
        FrameRounding,
        /// <summary>Thickness of border around frames (float) (imgui.h:1938).</summary>
        FrameBorderSize,
        /// <summary>Horizontal and vertical spacing between widgets/lines (Vector2) (imgui.h:1939).</summary>
        ItemSpacing,
        /// <summary>Horizontal and vertical spacing between elements of a composite widget (Vector2) (imgui.h:1940).</summary>
        ItemInnerSpacing,
        /// <summary>Horizontal indentation of tree node/label-less widgets (float) (imgui.h:1941).</summary>
        IndentSpacing,
        /// <summary>Padding within a table cell (Vector2) (imgui.h:1942).</summary>
        CellPadding,
        /// <summary>Width of the vertical scrollbar/height of the horizontal scrollbar (float) (imgui.h:1943).</summary>
        ScrollbarSize,
        /// <summary>Radius of scrollbar corners rounding (float) (imgui.h:1944).</summary>
        ScrollbarRounding,
        /// <summary>Spacing between scrollbar and widget (float) (imgui.h:1945).</summary>
        ScrollbarPadding,
        /// <summary>Minimum width/height of a grab box for slider/scrollbar (float) (imgui.h:1946).</summary>
        GrabMinSize,
        /// <summary>Radius of grabs corners rounding (float) (imgui.h:1947).</summary>
        GrabRounding,
        /// <summary>Radius of image corners rounding (float) (imgui.h:1948).</summary>
        ImageRounding,
        /// <summary>Thickness of border around images (float) (imgui.h:1949).</summary>
        ImageBorderSize,
        /// <summary>Radius of upper corners of a tab (float) (imgui.h:1950).</summary>
        TabRounding,
        /// <summary>Thickness of border around tabs (float) (imgui.h:1951).</summary>
        TabBorderSize,
        /// <summary>Minimum width of a tab (float) (imgui.h:1952).</summary>
        TabMinWidthBase,
        /// <summary>Shrink factor of a tab (float) (imgui.h:1953).</summary>
        TabMinWidthShrink,
        /// <summary>Thickness of the tab bar separator (float) (imgui.h:1954).</summary>
        TabBarBorderSize,
        /// <summary>Thickness of the tab bar overline (float) (imgui.h:1955).</summary>
        TabBarOverlineSize,
        /// <summary>Angle of angled headers in a table (float, radians) (imgui.h:1956).</summary>
        TableAngledHeadersAngle,
        /// <summary>Text alignment of angled headers in a table (Vector2) (imgui.h:1957).</summary>
        TableAngledHeadersTextAlign,
        /// <summary>Thickness of tree node hierarchy outlines (float) (imgui.h:1958).</summary>
        TreeLinesSize,
        /// <summary>Radius of tree node outline corners rounding (float) (imgui.h:1959).</summary>
        TreeLinesRounding,
        /// <summary>Radius of menu item corners rounding (float) (imgui.h:1960).</summary>
        MenuItemRounding,
        /// <summary>Radius of selectable corners rounding (float) (imgui.h:1961).</summary>
        SelectableRounding,
        /// <summary>Radius of drag-drop target corners rounding (float) (imgui.h:1962).</summary>
        DragDropTargetRounding,
        /// <summary>Alignment of button text when button is larger than text (Vector2) (imgui.h:1963).</summary>
        ButtonTextAlign,
        /// <summary>Alignment of selectable text (Vector2) (imgui.h:1964).</summary>
        SelectableTextAlign,
        /// <summary>Thickness of a separator line (float) (imgui.h:1965).</summary>
        SeparatorSize,
        /// <summary>Thickness of the border around a separator text (float) (imgui.h:1966).</summary>
        SeparatorTextBorderSize,
        /// <summary>Alignment of separator text (Vector2) (imgui.h:1967).</summary>
        SeparatorTextAlign,
        /// <summary>Padding of separator text (Vector2) (imgui.h:1968).</summary>
        SeparatorTextPadding,
        /// <summary>Thickness of the docking separator (float) (imgui.h:1969).</summary>
        DockingSeparatorSize,
        /// <summary>
        /// Sentinel equal to the number of style-variable slots; not a variable itself.
        /// Kept at the end of the table for 1:1 parity with <c>ImGuiStyleVar_</c> (imgui.h:1970).
        /// </summary>
        COUNT,
    }
}
