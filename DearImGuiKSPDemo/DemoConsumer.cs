using KSP.UI.Screens;
using UnityEngine;

namespace DearImGuiKSPDemo
{
    /// <summary>
    /// Demo consumer for DearImGui-KSP — ships as a SEPARATE install (GameData/DearImGuiKSPDemo, D7)
    /// so users installing the library as a dependency get no demo UI.
    /// Hosts the example window (AC3): text, a button with click feedback, a slider, and an
    /// input field, all declared per frame through the public C# API, plus the AC5 benchmark
    /// window (naive vs virtualized 1000-item list) with its IMGUI reference window (D10),
    /// and the C13/M4 ImPlot proof window (two live line plots fed by ring buffers),
    /// plus the M5 widget showcase section (C17: spinners, knobs, wheels, and a
    /// cancellable tween demo, drawn by ThemeDemo inside the main window),
    /// plus the ISSUES #011 docking showcase: a dockspace hosted inside the
    /// main window with a DockBuilder layout for the plot/benchmark windows
    /// (whichever of them is visible docks in — a single window fills the
    /// dockspace, both share a split layout; both hidden = no dockspace),
    /// plus the 1.3.0 Row/Combo/Tooltip showcase: the G2/G3/G4 in-game manual
    /// test page (selector and label+control rows, a per-state-tinted 4×4
    /// camera-slot grid built with Row() in loops, a 64-item scrolling combo,
    /// an out-of-range-tolerant combo, hover tooltips, and rows composed
    /// inside a ScrollRegion).
    /// All Begin/End pairs are declared through ImGuiEx scopes (C3), including one
    /// "Throw inside scope (test)" fault-barrier test hook.
    /// Persistent once-addon (MainMenu, once: true) that DontDestroyOnLoads itself —
    /// KSP does not do that automatically for once-addons (the library's own
    /// entry point works the same way). One instance lives for the whole session,
    /// so window visibility choices survive scene loads instead of resetting, and
    /// the toolbar button registers one-shot: the ApplicationLauncher persists
    /// across scenes and carries mod buttons with it, so the per-scene add/remove
    /// cycle stock does not reliably honor (ISSUES #015) is never exercised —
    /// no duplicate or dead green buttons accumulate.
    /// Toggled via an ApplicationLauncher toolbar button (green placeholder icon).
    /// </summary>
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public sealed class DemoConsumer : MonoBehaviour
    {
        private const string ConsumerId = "DearImGuiKSPDemo";

        // Window titles are process-global ImGui identities (a window is
        // docked by its exact title), so the three scopes and the DockBuilder
        // layout share these constants.
        private const string MainWindowTitle = "DearImGui-KSP Demo";
        private const string PlotWindowTitle = "DearImGui-KSP Plots";
        private const string BenchmarkWindowTitle = "DearImGui-KSP Benchmark";

        // Stable ID for the demo dockspace (any non-zero uint works; ImGui
        // hashes strings the same way, an explicit constant is just clearer).
        private const uint DemoDockspaceId = 0xD0C1A11;

        // Fixed height for the in-window dockspace. It must be explicit
        // because the main window is AlwaysAutoResize: a zero size would ask
        // imgui for the "remaining" content region, which is ~0 at the end of
        // an auto-fitted window's content, so the dockspace would clamp to a
        // 4px strip every frame (imgui.cpp DockSpace size clamp, :20833-20836)
        // and stay invisible. Zero width still fills the fitted content width.
        private const float DockspaceHeight = 300f;

        private ApplicationLauncherButton _toolbarButton;
        private bool _registered;
        private bool _windowVisible = true;
        // Aux windows: the plot window defaults visible (cheap), the benchmark
        // window is opt-in — it and its IMGUI reference window are the perf
        // showcase and have no business appearing unasked. Because this addon
        // persists across scenes, these choices survive scene loads; whichever
        // aux windows are visible dock into the in-window dockspace.
        private bool _benchmarkVisible;
        private bool _plotVisible = true;
        // Docking showcase layout state: which DockBuilder layout (if any) is
        // currently applied to the in-window dockspace. ImGui discards a dock
        // node tree on any frame its dockspace is not submitted, so this only
        // ever tracks trees this code believes are alive.
        private int _dockLayoutState;
        private const int DockLayoutNone = 0;
        private const int DockLayoutPlotOnly = 1;
        private const int DockLayoutBenchmarkOnly = 2;
        private const int DockLayoutBoth = DockLayoutPlotOnly | DockLayoutBenchmarkOnly;
        private BenchmarkUI _benchmark;
        private PlotDemo _plotDemo;
        private ThemeDemo _themeDemo;
        private int _clickCount;
        private float _sliderValue = 0.5f;
        private string _inputText = "edit me";

        // M3 tuning-pass showcase: the styled gradient buttons pull their stops
        // from the active theme preset (KspPalette is public, but the demo no
        // longer needs its own copies — the style overload reads the preset).
        private bool _themeToggle = true;
        private int _radioChoice;

        // --- 1.3.0 Row/Combo/Tooltip showcase (G2/G3/G4 in-game test page) ---
        private const string LayoutSectionHeaderText = "Row / combo / tooltip showcase (1.3.0)";
        private const string SelectorPrevLabel = "←";
        private const string SelectorNextLabel = "→";
        private const string SelectorTooltipText = "Next camera\n(wraps around the 64-item list)";
        private const string PairRowLabelText = "Pair row:";
        private const string PairRowSliderId = "##pairrowslider";
        private const string PairRowSliderTooltip = "Tooltip on a slider inside a Row";
        private const string GridCaptionText =
            "Camera slots (click cycles empty → assigned → live → empty):";
        private const int GridRows = 4;
        private const int GridCols = 4;
        private const int GridCellCount = GridRows * GridCols;
        private const int TooltipCellIndex = 0;
        private const string GridCellTooltipText =
            "Camera slot 01 — multi-line tooltip\nState tint: green = assigned, orange = live.";
        private const float GridRowSpacing = 4f;   // Row(float): explicit, unscaled pixels
        private const int SlotEmpty = 0;
        private const int SlotAssigned = 1;
        private const int SlotLive = 2;
        private const int CameraItemCount = 64;    // 50+ items so the combo popup must scroll
        private const int OrphanedIndexSeed = CameraItemCount + 7; // deliberately out of range
        private const string CameraComboLabel = "Camera combo";
        private const string OrphanedComboLabel = "Recovered combo";
        private const string OrphanedRebreakLabel = "Re-orphan the index (test)";
        private const string OrphanedRebreakTooltip =
            "Seeds the combo index beyond the list again:\npreview shows \"(none)\", nothing throws.";
        private const string EmptyComboLabel = "Empty combo (disabled)";
        private const string EmptyComboTooltip =
            "Bound to an empty list: disabled \"(none)\" chrome.\nThe popup never opens and the call never throws.";
        private const string ScrollRegionCaptionText = "Rows inside a ScrollRegion:";
        private const string ScrollRegionId = "rowscrollregion";
        private const float ScrollRegionHeight = 64f;

        // Generated lists, built once (steady-state per-frame path allocates
        // nothing here — the ThemeDemo precedent).
        private static readonly string[] CameraItems = BuildCameraItems();
        private static readonly string[] CellLabels = BuildCellLabels();
        private static readonly string[] EmptyItems = new string[0];

        // Per-state slot tints, alpha < 255 so the buttons read as tints.
        // Assigned = KSP green, live = KSP orange (the CinematicRecorder
        // camera-slot mirror the FR asks the grid to stand in for).
        private static readonly Color32 SlotAssignedTint =
            WithAlpha(DearImGuiKSP.Application.KspPalette.GreenDark, 120);
        private static readonly Color32 SlotAssignedTintHover =
            WithAlpha(DearImGuiKSP.Application.KspPalette.GreenDark, 185);
        private static readonly Color32 SlotAssignedTintActive =
            WithAlpha(DearImGuiKSP.Application.KspPalette.GreenLight, 230);
        private static readonly Color32 SlotLiveTint =
            WithAlpha(DearImGuiKSP.Application.KspPalette.OrangeDark, 150);
        private static readonly Color32 SlotLiveTintHover =
            WithAlpha(DearImGuiKSP.Application.KspPalette.OrangeDark, 210);
        private static readonly Color32 SlotLiveTintActive =
            WithAlpha(DearImGuiKSP.Application.KspPalette.OrangeLight, 255);

        private int _selectorIndex;
        private float _pairSliderValue = 0.5f;
        private readonly int[] _slotStates = new int[GridCellCount];
        private int _cameraComboIndex;
        // Seeded beyond the list on purpose: the combo preview must show
        // "(none)" and never throw until the user makes a real selection.
        private int _orphanedComboIndex = OrphanedIndexSeed;
        private int _emptyComboIndex;

        private void Awake()
        {
            // KSP does not DontDestroyOnLoad once-addons automatically (KSP
            // Knowledge Library, assembly-loading notes) — the library's own
            // entry point does this too.
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (!DearImGuiKSP.DearImGuiKSP.IsAvailable)
            {
                Debug.Log("[DearImGuiKSPDemo] DearImGui-KSP not available; demo disabled.");
                return;
            }
            DearImGuiKSP.DearImGuiKSP.Register(ConsumerId, OnFrame);
            _registered = true;
            Debug.Log("[DearImGuiKSPDemo] Registered with DearImGui-KSP.");

            _benchmark = new BenchmarkUI();
            _plotDemo = new PlotDemo();
            _themeDemo = new ThemeDemo();

            GameEvents.onGUIApplicationLauncherReady.Add(OnLauncherReady);
            if (ApplicationLauncher.Ready)
            {
                OnLauncherReady();
            }
        }

        private void OnDestroy()
        {
            GameEvents.onGUIApplicationLauncherReady.Remove(OnLauncherReady);
            if (_toolbarButton != null && ApplicationLauncher.Instance != null)
            {
                ApplicationLauncher.Instance.RemoveModApplication(_toolbarButton);
                _toolbarButton = null;
            }
            // Only when Start actually registered: OnDestroy runs at session end
            // (once-addon), and Unregister on a consumer the library never knew
            // logs a spurious warning whenever the library is unavailable/dormant (G3-41).
            if (_registered)
            {
                _registered = false;
                DearImGuiKSP.DearImGuiKSP.Unregister(ConsumerId);
            }
        }

        private void OnLauncherReady()
        {
            if (_toolbarButton != null)
            {
                return;
            }
            _toolbarButton = ApplicationLauncher.Instance.AddModApplication(
                OnToolbarOn, OnToolbarOff,
                null, null, null, null,
                ApplicationLauncher.AppScenes.ALWAYS,
                MakePlaceholderIcon());
            _toolbarButton.SetTrue(false); // window starts visible; reflect it without firing the callback
        }

        private void OnToolbarOn()
        {
            _windowVisible = true;
        }

        private void OnToolbarOff()
        {
            _windowVisible = false;
        }

        // Solid-green 38x38 placeholder icon (stock toolbar icon size).
        private static Texture2D MakePlaceholderIcon()
        {
            Texture2D tex = new Texture2D(38, 38, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[38 * 38];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.green;
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        // Per-frame UI declaration — the only place widget calls are valid.
        // All Begin/End pairs run through ImGuiEx scopes (C3): Dispose closes the
        // stack on every exit path, including exceptions (the fault barrier test hook).
        private void OnFrame()
        {
            if (!_windowVisible)
            {
                // The dock node tree only lives while the dockspace is submitted,
                // so a hidden main window discards the built layout too.
                _dockLayoutState = DockLayoutNone;
                return;
            }
            // noDocking: this window hosts the dockspace, and docking a
            // dockspace host is unsupported upstream (the official dockspace
            // demo marks its host window ImGuiWindowFlags_NoDocking,
            // imgui_demo.cpp:10698-10700). A docked host acquires the
            // ChildWindow flag (imgui.cpp:21544), and dock-node re-parenting
            // can then trip the recoverable "Must call EndChild() and not
            // End()!" error at its End() (imgui.cpp:8908-8909). Other windows
            // can still be dropped into the dockspace region inside it.
            using (var window = DearImGuiKSP.ImGuiEx.Window(MainWindowTitle, autoResize: true, noDocking: true))
            {
                if (window.Visible)
                {
                    DearImGuiKSP.DearImGuiKSP.Text("Hello from the demo consumer!");
                    DearImGuiKSP.DearImGuiKSP.Text("Button clicked " + _clickCount + " time(s).");

                    if (DearImGuiKSP.DearImGuiKSP.Button("Click me"))
                    {
                        _clickCount++;
                    }

                    DearImGuiKSP.DearImGuiKSP.SliderFloat("Slider", ref _sliderValue, 0f, 1f);
                    DearImGuiKSP.DearImGuiKSP.InputText("Input", ref _inputText);

                    // MVP has no checkbox — button-toggle is the pattern.
                    if (DearImGuiKSP.DearImGuiKSP.Button(_benchmarkVisible
                        ? "Hide benchmark window"
                        : "Show benchmark window"))
                    {
                        _benchmarkVisible = !_benchmarkVisible;
                    }
                    if (DearImGuiKSP.DearImGuiKSP.Button(_plotVisible
                        ? "Hide plot window"
                        : "Show plot window"))
                    {
                        _plotVisible = !_plotVisible;
                    }

                    // C3 test hook (M1 gate): throwing inside a using scope must leave
                    // the style stack symmetric — the scope's Dispose pops the pushed
                    // color during unwind, then the fault barrier logs the exception.
                    if (DearImGuiKSP.DearImGuiKSP.Button("Throw inside scope (test)"))
                    {
                        using (DearImGuiKSP.ImGuiEx.StyleColor(
                            DearImGuiKSP.ImGuiCol.Text, Color.red))
                        {
                            throw new System.InvalidOperationException(
                                "[DearImGuiKSPDemo] Intentional throw inside a scope (test hook).");
                        }
                    }

                    // M3 tuning-pass theme showcase: a consumer-choice colored
                    // header (the theme never auto-colors text), the two themed
                    // gradient button styles, an animated toggle, and circular
                    // radios. The window background gradient is applied
                    // natively by the theme.
                    DearImGuiKSP.DearImGuiKSP.TextColored(
                        DearImGuiKSP.Application.KspPalette.GreenLight, "Theme showcase");
                    if (DearImGuiKSP.ImGuiGradients.GradientButton(
                        "Primary gradient", new Vector2(180f, 28f), DearImGuiKSP.GradientButtonStyle.Primary))
                    {
                        _clickCount++;
                    }
                    if (DearImGuiKSP.ImGuiGradients.GradientButton(
                        "Secondary gradient", new Vector2(180f, 28f), DearImGuiKSP.GradientButtonStyle.Secondary))
                    {
                        _clickCount++;
                    }
                    DearImGuiKSP.DearImGuiKSP.Toggle(
                        "Animated toggle", ref _themeToggle, DearImGuiKSP.ToggleFlags.Animated);
                    DearImGuiKSP.DearImGuiKSP.RadioButton("Radio option 1", ref _radioChoice, 0);
                    DearImGuiKSP.DearImGuiKSP.RadioButton("Radio option 2", ref _radioChoice, 1);

                    // M5 widget showcase (C17): spinner row, knobs, wheels, and
                    // the tween demo — additive to the M3 section above.
                    _themeDemo.DrawImGui();

                    // 1.3.0 Row/Combo/Tooltip showcase — additive section inside
                    // this autoResize window (the grid G2 checks stays here).
                    DrawLayoutShowcase();

                    // ISSUES #011 docking showcase. The dockspace is hosted
                    // inside this window rather than over the whole viewport:
                    // DockSpace is a strict native no-op while the library's
                    // docking setting is off (it returns 0 and submits
                    // nothing), whereas DockSpaceOverViewport would keep
                    // painting its fullscreen host window. So when docking is
                    // off the windows simply float, with no broken layout and
                    // no log noise.
                    // The dockspace is declared whenever at least one aux
                    // window is visible — never while both are hidden, because
                    // an empty dock node would render ImGui's "Debug##Default"
                    // fallback window. Each visibility combo gets its own
                    // DockBuilder layout. The curated API has no RemoveNode, so
                    // a combo change cannot reshape a live tree: the dockspace
                    // is skipped for one frame (ImGui discards a dock node
                    // tree on any frame its dockspace is not submitted) and
                    // the new layout applies to the fresh tree next frame.
                    int desiredDockLayout =
                        (_plotVisible ? DockLayoutPlotOnly : DockLayoutNone) |
                        (_benchmarkVisible ? DockLayoutBenchmarkOnly : DockLayoutNone);
                    if (desiredDockLayout == DockLayoutNone)
                    {
                        // No aux windows: no dockspace, no node tree.
                        _dockLayoutState = DockLayoutNone;
                    }
                    else if (_dockLayoutState != DockLayoutNone && desiredDockLayout != _dockLayoutState)
                    {
                        // The combo changed while an old-layout tree is alive:
                        // skip this frame so ImGui discards it; the new layout
                        // is applied to the fresh tree on the next frame.
                        _dockLayoutState = DockLayoutNone;
                    }
                    else
                    {
                        // Width fills the content region; the height is
                        // explicit (see DockspaceHeight) because Vector2.zero
                        // has no "remaining region" to fill in an
                        // AlwaysAutoResize window.
                        uint dockspaceId = DearImGuiKSP.DearImGuiKSP.DockSpace(
                            DemoDockspaceId, new Vector2(0f, DockspaceHeight));
                        if (dockspaceId == 0)
                        {
                            // Docking setting off: nothing was submitted, so
                            // no tree exists — force a fresh layout apply if
                            // the player turns docking back on mid-session.
                            _dockLayoutState = DockLayoutNone;
                        }
                        else if (_dockLayoutState == DockLayoutNone)
                        {
                            // Fresh node tree (first show, post-combo-change,
                            // or docking just toggled on): apply the layout
                            // for the current visibility combo.
                            ApplyDockLayout(dockspaceId, desiredDockLayout);
                            _dockLayoutState = desiredDockLayout;
                        }
                    }
                }
                else
                {
                    // Collapsed main window: the dockspace is not submitted
                    // this frame either, so the node tree is discarded and the
                    // layout must be re-applied when the window is expanded.
                    _dockLayoutState = DockLayoutNone;
                }
            }
            DrawBenchmarkWindow();
            DrawPlotWindow();
        }

        // One-time DockBuilder layout (ISSUES #011), applied only to a FRESH
        // dock node tree — the caller guarantees any previous tree was
        // discarded by skipping the dockspace for a frame first
        // (DockBuilderSplitNode asserts on an already-split node). The curated
        // API has no DockSpace node flag, so the upstream RemoveNode/AddNode
        // recipe does not apply: DockSpace above already created the dockspace
        // node this frame, and DockBuilderSplitNode operates directly on a
        // live node. Windows are docked by their exact titles (their
        // process-global ImGui identities) — the same constants the window
        // scopes use.
        private static void ApplyDockLayout(uint dockspaceId, int layout)
        {
            if (layout == DockLayoutBoth)
            {
                // Region below the widgets: plots on top (60%), benchmark below (40%).
                DearImGuiKSP.DearImGuiKSP.DockBuilderSplitNode(
                    dockspaceId, DearImGuiKSP.ImGuiDir.Down, 0.4f,
                    out uint benchmarkNode, out uint plotsNode);
                DearImGuiKSP.DearImGuiKSP.DockBuilderDockWindow(PlotWindowTitle, plotsNode);
                DearImGuiKSP.DearImGuiKSP.DockBuilderDockWindow(BenchmarkWindowTitle, benchmarkNode);
            }
            else
            {
                // Single visible aux window: dock it into the root node so it
                // fills the whole dockspace.
                DearImGuiKSP.DearImGuiKSP.DockBuilderDockWindow(
                    layout == DockLayoutPlotOnly ? PlotWindowTitle : BenchmarkWindowTitle,
                    dockspaceId);
            }
            DearImGuiKSP.DearImGuiKSP.DockBuilderFinish(dockspaceId);
        }

        // 1.3.0 manual test page (FR-1..FR-3; the in-game half of G2/G3/G4):
        // selector and label+control Row examples, the per-state-tinted 4×4
        // camera-slot grid, the 64-item combo, the out-of-range combo, and
        // hover tooltips — all inside this autoResize window; the last block
        // additionally composes rows inside a ScrollRegion. The ←/→ selector
        // labels stand in for the work item's ◀/▶ because the bundled Plex
        // font has no glyphs for U+25C0/U+25B6 (font probe on file).
        private void DrawLayoutShowcase()
        {
            DearImGuiKSP.DearImGuiKSP.TextColored(
                DearImGuiKSP.Application.KspPalette.GreenLight, LayoutSectionHeaderText);

            // FR-1 selector row: prev/next buttons flank a label; inside a
            // Row scope the three widgets share one line. Tooltip attaches
            // to the "next" button (the item declared immediately before it;
            // a tooltip is not a layout item, so the row spacing is unaffected).
            using (DearImGuiKSP.ImGuiEx.Row())
            {
                if (DearImGuiKSP.DearImGuiKSP.Button(SelectorPrevLabel))
                {
                    _selectorIndex = (_selectorIndex + CameraItemCount - 1) % CameraItemCount;
                }
                DearImGuiKSP.DearImGuiKSP.Text(CameraItems[_selectorIndex]);
                if (DearImGuiKSP.DearImGuiKSP.Button(SelectorNextLabel))
                {
                    _selectorIndex = (_selectorIndex + 1) % CameraItemCount;
                }
                DearImGuiKSP.DearImGuiKSP.Tooltip(SelectorTooltipText);
            }

            // FR-1 label+control pair row: a plain label and a slider on one
            // line (the slider's visible label is empty; "##" keeps its id).
            using (DearImGuiKSP.ImGuiEx.Row())
            {
                DearImGuiKSP.DearImGuiKSP.Text(PairRowLabelText);
                DearImGuiKSP.DearImGuiKSP.SliderFloat(
                    PairRowSliderId, ref _pairSliderValue, 0f, 1f);
                DearImGuiKSP.DearImGuiKSP.Tooltip(PairRowSliderTooltip);
            }

            // FR-1 camera-slot grid (the CinematicRecorder use case): 4 rows
            // of 4 cells built with Row() in loops; ## ids give every cell a
            // unique identity and ImGuiEx.StyleColor scopes tint the button
            // per state (empty keeps the theme default).
            DearImGuiKSP.DearImGuiKSP.Text(GridCaptionText);
            for (int row = 0; row < GridRows; row++)
            {
                using (DearImGuiKSP.ImGuiEx.Row(GridRowSpacing))
                {
                    for (int col = 0; col < GridCols; col++)
                    {
                        DrawGridCell(row * GridCols + col);
                    }
                }
            }

            // FR-2 scrolling combo: 64 generated items force the popup to
            // scroll; the current selection is echoed as text below.
            DearImGuiKSP.DearImGuiKSP.Combo(
                CameraComboLabel, ref _cameraComboIndex, CameraItems);
            DearImGuiKSP.DearImGuiKSP.Text(
                "Selected: " + CameraItems[_cameraComboIndex]);

            // FR-2 out-of-range tolerance: the index is seeded beyond the
            // list, so the preview shows "(none)" and the ref is left
            // untouched — no exception. The button re-seeds it after a real
            // selection repairs it.
            DearImGuiKSP.DearImGuiKSP.Combo(
                OrphanedComboLabel, ref _orphanedComboIndex, CameraItems);
            // Index echo makes the repair visible: 71 ("(none)") until a real
            // selection writes a valid index.
            DearImGuiKSP.DearImGuiKSP.Text(
                "Recovered index: " + _orphanedComboIndex);
            if (DearImGuiKSP.DearImGuiKSP.Button(OrphanedRebreakLabel))
            {
                _orphanedComboIndex = OrphanedIndexSeed;
            }
            DearImGuiKSP.DearImGuiKSP.Tooltip(OrphanedRebreakTooltip);

            // FR-2 empty-list path: the genuinely DISABLED "(none)" combo —
            // its popup never opens. Distinct from the recovered combo above,
            // which is interactive by design (selection repairs the index).
            DearImGuiKSP.DearImGuiKSP.Combo(
                EmptyComboLabel, ref _emptyComboIndex, EmptyItems);
            DearImGuiKSP.DearImGuiKSP.Tooltip(EmptyComboTooltip);

            // Composition edge case: rows inside a fixed-height ScrollRegion
            // (the region is skipped while its begin reports not visible).
            DearImGuiKSP.DearImGuiKSP.Text(ScrollRegionCaptionText);
            using (var region = DearImGuiKSP.ImGuiEx.ScrollRegion(
                ScrollRegionId, ScrollRegionHeight))
            {
                if (region.Visible)
                {
                    using (DearImGuiKSP.ImGuiEx.Row())
                    {
                        DearImGuiKSP.DearImGuiKSP.Button("Alpha##scrollrow");
                        DearImGuiKSP.DearImGuiKSP.Button("Beta##scrollrow");
                        DearImGuiKSP.DearImGuiKSP.Button("Gamma##scrollrow");
                    }
                    using (DearImGuiKSP.ImGuiEx.Row())
                    {
                        DearImGuiKSP.DearImGuiKSP.Text("a second row");
                        DearImGuiKSP.DearImGuiKSP.Button("Delta##scrollrow");
                    }
                }
            }
        }

        // One camera-slot cell, tinted per state. Assigned = KSP green,
        // live = KSP orange; the empty state keeps the stock themed button.
        private void DrawGridCell(int index)
        {
            int state = _slotStates[index];
            if (state == SlotAssigned)
            {
                using (DearImGuiKSP.ImGuiEx.StyleColor(
                    DearImGuiKSP.ImGuiCol.Button, SlotAssignedTint))
                using (DearImGuiKSP.ImGuiEx.StyleColor(
                    DearImGuiKSP.ImGuiCol.ButtonHovered, SlotAssignedTintHover))
                using (DearImGuiKSP.ImGuiEx.StyleColor(
                    DearImGuiKSP.ImGuiCol.ButtonActive, SlotAssignedTintActive))
                {
                    DrawGridCellButton(index);
                }
            }
            else if (state == SlotLive)
            {
                using (DearImGuiKSP.ImGuiEx.StyleColor(
                    DearImGuiKSP.ImGuiCol.Button, SlotLiveTint))
                using (DearImGuiKSP.ImGuiEx.StyleColor(
                    DearImGuiKSP.ImGuiCol.ButtonHovered, SlotLiveTintHover))
                using (DearImGuiKSP.ImGuiEx.StyleColor(
                    DearImGuiKSP.ImGuiCol.ButtonActive, SlotLiveTintActive))
                {
                    DrawGridCellButton(index);
                }
            }
            else
            {
                DrawGridCellButton(index);
            }
        }

        // The cell button itself plus its click cycle (empty → assigned →
        // live → empty; only one slot can be live, so a new live slot demotes
        // the previously live one to assigned). Cell 0 carries the grid's
        // multi-line tooltip.
        private void DrawGridCellButton(int index)
        {
            if (DearImGuiKSP.DearImGuiKSP.Button(CellLabels[index]))
            {
                int state = _slotStates[index];
                if (state == SlotLive)
                {
                    _slotStates[index] = SlotEmpty;
                }
                else if (state == SlotAssigned)
                {
                    for (int i = 0; i < GridCellCount; i++)
                    {
                        if (_slotStates[i] == SlotLive)
                        {
                            _slotStates[i] = SlotAssigned;
                        }
                    }
                    _slotStates[index] = SlotLive;
                }
                else
                {
                    _slotStates[index] = SlotAssigned;
                }
            }
            if (index == TooltipCellIndex)
            {
                DearImGuiKSP.DearImGuiKSP.Tooltip(GridCellTooltipText);
            }
        }

        // 64 generated camera names — 50+ items so the combo popup scrolls.
        private static string[] BuildCameraItems()
        {
            var items = new string[CameraItemCount];
            for (int i = 0; i < items.Length; i++)
            {
                items[i] = "Camera " + (i + 1).ToString("00");
            }
            return items;
        }

        // "01".."16" visible labels with ##camslotNN ids: uniform two-character
        // labels keep the grid cells the same width and every cell identity
        // unique within the window.
        private static string[] BuildCellLabels()
        {
            var labels = new string[GridCellCount];
            for (int i = 0; i < labels.Length; i++)
            {
                string number = (i + 1).ToString("00");
                labels[i] = number + "##camslot" + number;
            }
            return labels;
        }

        private static Color32 WithAlpha(Color32 color, byte alpha)
        {
            return new Color32(color.r, color.g, color.b, alpha);
        }

        // Third window in the same registered callback — one consumer ID, one
        // registration. Skipped entirely (no window scope opened) while hidden.
        private void DrawPlotWindow()
        {
            if (!_plotVisible || _plotDemo == null)
            {
                return;
            }
            using (var window = DearImGuiKSP.ImGuiEx.Window(PlotWindowTitle))
            {
                if (window.Visible)
                {
                    _plotDemo.DrawImGui();
                }
            }
        }

        // Second window in the same registered callback — still one consumer ID,
        // one registration. Skipped entirely (no window scope opened) while hidden.
        private void DrawBenchmarkWindow()
        {
            if (!_benchmarkVisible || _benchmark == null)
            {
                return;
            }
            using (var window = DearImGuiKSP.ImGuiEx.Window(BenchmarkWindowTitle))
            {
                if (window.Visible)
                {
                    _benchmark.DrawImGui();
                }
            }
        }

        // Unity IMGUI callback — hosts the IMGUI reference window so the AC5
        // side-by-side comparison shows alongside the benchmark window.
        private void OnGUI()
        {
            if (!_windowVisible || !_benchmarkVisible || _benchmark == null)
            {
                return;
            }
            _benchmark.OnGUIReference();
        }
    }
}
