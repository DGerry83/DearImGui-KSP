using System;
using DearImGuiKSP;
using Xunit;

namespace Application.Tests
{
    /// <summary>
    /// N13/T19 ABI ordinal pins: the hand-mirrored managed tables
    /// (<see cref="ImGuiCol"/>, <see cref="ImGuiStyleVar"/>) cross the ABI as raw
    /// <c>int</c>s into style slots in the native DLL, where the release build's
    /// /DNDEBUG has compiled out every range assert — a drifted ordinal silently
    /// recolors/restyles the wrong slot. The expected name tables below were
    /// mechanically extracted from the pinned native header
    /// <c>C:\Users\Matt\source\repos\cimgui\imgui\imgui.h</c>
    /// (IMGUI_VERSION "1.92.9", IMGUI_VERSION_NUM 19290 — the sibling clone the
    /// native core compiles from, per DearImGuiKSPNative/vendor/PIN_RECORD.md),
    /// from the <c>enum ImGuiCol_</c> (imgui.h:1839-1912) and
    /// <c>enum ImGuiStyleVar_</c> (imgui.h:1922-1970) blocks. Regenerate the
    /// arrays from the header when the pin moves; do not hand-edit to match the
    /// C# enum (that would be the tautology this test exists to prevent).
    /// </summary>
    public class StyleEnumPinTests
    {
        // imgui.h @ 1.92.9, enum ImGuiCol_ in declaration order (COUNT = 63 slots).
        private static readonly string[] ExpectedImGuiCol =
        {
            "Text", "TextDisabled", "WindowBg", "ChildBg", "PopupBg", "Border",
            "BorderShadow", "FrameBg", "FrameBgHovered", "FrameBgActive", "TitleBg",
            "TitleBgActive", "TitleBgCollapsed", "MenuBarBg", "ScrollbarBg",
            "ScrollbarGrab", "ScrollbarGrabHovered", "ScrollbarGrabActive", "CheckMark",
            "CheckboxSelectedBg", "SliderGrab", "SliderGrabActive", "Button",
            "ButtonHovered", "ButtonActive", "Header", "HeaderHovered", "HeaderActive",
            "Separator", "SeparatorHovered", "SeparatorActive", "ResizeGrip",
            "ResizeGripHovered", "ResizeGripActive", "InputTextCursor", "TabHovered",
            "Tab", "TabSelected", "TabSelectedOverline", "TabDimmed",
            "TabDimmedSelected", "TabDimmedSelectedOverline", "DockingPreview",
            "DockingEmptyBg", "PlotLines", "PlotLinesHovered", "PlotHistogram",
            "PlotHistogramHovered", "TableHeaderBg", "TableBorderStrong",
            "TableBorderLight", "TableRowBg", "TableRowBgAlt", "TextLink",
            "TextSelectedBg", "TreeLines", "DragDropTarget", "DragDropTargetBg",
            "UnsavedMarker", "NavCursor", "NavWindowingHighlight", "NavWindowingDimBg",
            "ModalWindowDimBg", "COUNT",
        };

        // imgui.h @ 1.92.9, enum ImGuiStyleVar_ in declaration order (COUNT = 45 slots).
        private static readonly string[] ExpectedImGuiStyleVar =
        {
            "Alpha", "DisabledAlpha", "WindowPadding", "WindowRounding",
            "WindowBorderSize", "WindowMinSize", "WindowTitleAlign", "ChildRounding",
            "ChildBorderSize", "PopupRounding", "PopupBorderSize", "FramePadding",
            "FrameRounding", "FrameBorderSize", "ItemSpacing", "ItemInnerSpacing",
            "IndentSpacing", "CellPadding", "ScrollbarSize", "ScrollbarRounding",
            "ScrollbarPadding", "GrabMinSize", "GrabRounding", "ImageRounding",
            "ImageBorderSize", "TabRounding", "TabBorderSize", "TabMinWidthBase",
            "TabMinWidthShrink", "TabBarBorderSize", "TabBarOverlineSize",
            "TableAngledHeadersAngle", "TableAngledHeadersTextAlign", "TreeLinesSize",
            "TreeLinesRounding", "MenuItemRounding", "SelectableRounding",
            "DragDropTargetRounding", "ButtonTextAlign", "SelectableTextAlign",
            "SeparatorSize", "SeparatorTextBorderSize", "SeparatorTextAlign",
            "SeparatorTextPadding", "DockingSeparatorSize", "COUNT",
        };

        [Fact]
        public void ImGuiCol_NamesAndDeclarationOrder_MatchHeader19290()
        {
            // Enum.GetNames sorts by ordinal; the table is sequential from 0, so
            // order equality pins every ordinal AND the member set. A renamed,
            // inserted, removed, or reordered member fails here.
            Assert.Equal(ExpectedImGuiCol, Enum.GetNames(typeof(ImGuiCol)));
            Assert.Equal(63, (int)ImGuiCol.COUNT);
            Assert.Equal("COUNT", Enum.GetName(typeof(ImGuiCol), 63));
        }

        [Fact]
        public void ImGuiStyleVar_NamesAndDeclarationOrder_MatchHeader19290()
        {
            Assert.Equal(ExpectedImGuiStyleVar, Enum.GetNames(typeof(ImGuiStyleVar)));
            Assert.Equal(45, (int)ImGuiStyleVar.COUNT);
            Assert.Equal("COUNT", Enum.GetName(typeof(ImGuiStyleVar), 45));
        }
    }
}
