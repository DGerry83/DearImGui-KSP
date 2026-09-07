using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace DearImGuiKSPDemo
{
    /// <summary>
    /// AC5 torture test (spec §4.3, D10): naive vs virtualized 1000-item list on the
    /// Dear ImGui side, plus a self-contained IMGUI reference window porting
    /// IMGUI_Helper's PerformanceTab so the side-by-side comparison needs no
    /// IMGUI_Helper install. The ImGui virtualized path uses only the public
    /// BeginScrollRegion/GetScrollY/SetCursorY API — it is a consumer of the public
    /// surface, not a special case.
    ///
    /// The item strings are pre-built once in the ctor; the steady-state list draw
    /// allocates nothing per row. Only the FPS/declaration readout lines format
    /// strings, which is demo code, not a library hot path.
    /// </summary>
    public sealed class BenchmarkUI
    {
        // Row pitch of the IMGUI reference list. The reference window renders with
        // Unity's GUI skin (KSP's own UI scale), not the library's uiScale/fontScale.
        private const float ImguiRowHeight = 22f;
        private const float ListViewHeight = 200f;
        private const int ItemCount = 1000;

        private const string ResizeNote = "Auto-sizing off — drag the corner or edge to resize.";

        // Rolling-average window for the declaration-cost readouts.
        private const int RollingWindowFrames = 60;

        // Unique large integer per the IMGUI_Helper window-ID convention (prevents input fighting).
        private const int ImguiReferenceWindowId = 826371;

        private readonly List<string> _items = new List<string>(ItemCount);

        // Independent toggles: AC5 toggles each side separately to compare paths.
        private bool _virtualized = true;
        private bool _imguiVirtualized = true;
        // Last value returned by the IMGUI reference toggle, stashed on every OnGUI
        // pass and committed to _imguiVirtualized only during Layout (G3-33).
        private bool _pendingImguiVirtualized;

        private Vector2 _imguiScrollPos;
        private Rect _imguiWindowRect = new Rect(60f, 60f, 380f, 420f);

        // FPS readout: exponential moving average of unscaled delta time.
        private float _fpsEma = -1f;

        // Row pitch of the Dear ImGui virtualized list, measured live from the
        // declared row geometry (see DrawImGuiVirtualizedList). The public API
        // exposes no style metrics (no CalcTextSize/TextLineHeight/UiScale
        // accessor — G2-12), so a hard-coded pitch cannot follow uiScale,
        // fontScale, or the active font; seeded with the stock-style pitch and
        // corrected every frame after the first row is declared.
        private float _rowHeight = 22f;

        // Rolling 60-frame declaration-cost averages (ms), one per side.
        private readonly Stopwatch _imGuiWatch = new Stopwatch();
        private readonly Stopwatch _imguiWatch = new Stopwatch();
        private float _imGuiSampleSum;
        private int _imGuiSampleCount;
        private float _imguiSampleSum;
        private int _imguiSampleCount;
        private float _imGuiAvgMs = -1f;
        private float _imguiAvgMs = -1f;

        // IMGUI runs OnGUI several times per frame (Layout, Repaint, input passes) and
        // does real layout work in all of them, so per-pass timing undercounts badly.
        // Accumulate every pass and publish the per-frame total when the frame changes.
        private int _imguiFrame = -1;
        private float _imguiFrameMs;

        public BenchmarkUI()
        {
            for (int i = 0; i < ItemCount; i++)
            {
                _items.Add(string.Format("Telemetry Entry {0:D4}: {1:F4}", i, Random.value));
            }
        }

        /// <summary>
        /// Content of the Dear ImGui benchmark window. Times its own declaration cost
        /// and feeds the 60-frame rolling average behind the readout line.
        /// </summary>
        public void DrawImGui()
        {
            UpdateFpsEma();

            _imGuiWatch.Restart();
            DrawImGuiContent();
            _imGuiWatch.Stop();
            AddSample((float)_imGuiWatch.Elapsed.TotalMilliseconds,
                ref _imGuiSampleSum, ref _imGuiSampleCount, ref _imGuiAvgMs);
        }

        /// <summary>
        /// Unity IMGUI callback body: declares the draggable "IMGUI Reference" window.
        /// Every OnGUI pass is timed and summed per frame (IMGUI does layout work in
        /// Layout/Repaint/input passes alike, so timing any single pass undercounts).
        /// </summary>
        public void OnGUIReference()
        {
            _imguiWatch.Restart();

            _imguiWindowRect = GUILayout.Window(
                ImguiReferenceWindowId, _imguiWindowRect, DrawImguiReferenceWindow, "IMGUI Reference");

            _imguiWatch.Stop();

            if (Time.frameCount != _imguiFrame)
            {
                if (_imguiFrame >= 0)
                {
                    AddSample(_imguiFrameMs,
                        ref _imguiSampleSum, ref _imguiSampleCount, ref _imguiAvgMs);
                }
                _imguiFrame = Time.frameCount;
                _imguiFrameMs = 0f;
            }
            _imguiFrameMs += (float)_imguiWatch.Elapsed.TotalMilliseconds;
        }

        private void DrawImGuiContent()
        {
            DearImGuiKSP.DearImGuiKSP.TextColored(
                DearImGuiKSP.Application.KspPalette.TextLightGrey, ResizeNote);
            DearImGuiKSP.DearImGuiKSP.Text(FpsReadoutLine());
            DearImGuiKSP.DearImGuiKSP.Text(string.Format(
                "ImGui declaration: {0} ms (60-frame avg)", FormatMs(_imGuiAvgMs)));
            // IMGUI's dominant cost is Unity-internal rendering AFTER OnGUI returns,
            // so script timing undercounts it; the frame-time line is the honest metric.
            DearImGuiKSP.DearImGuiKSP.Text(string.Format(
                "IMGUI OnGUI script time: {0} ms (see note in IMGUI window)", FormatMs(_imguiAvgMs)));

            // MVP has no checkbox — button-toggle is the pattern; the label shows the mode.
            if (DearImGuiKSP.DearImGuiKSP.Button(_virtualized
                ? "Mode: Virtualized (click for naive)"
                : "Mode: Naive (click for virtualized)"))
            {
                _virtualized = !_virtualized;
            }

            // Fixed-height scrolling child region via the C3 scope API; Dispose ends
            // the region even when it is clipped (Visible false).
            using (var list = DearImGuiKSP.ImGuiEx.ScrollRegion("benchmarkList", ListViewHeight))
            {
                if (list.Visible)
                {
                    if (_virtualized)
                    {
                        DrawImGuiVirtualizedList();
                    }
                    else
                    {
                        for (int i = 0; i < _items.Count; i++)
                        {
                            DearImGuiKSP.DearImGuiKSP.Text(_items[i]);
                        }
                    }
                }
            }
        }

        // Manual virtualization on the public API: reserve the full scroll range with
        // SetCursorY, draw only the rows intersecting the viewport. Rows come from the
        // pre-built list verbatim — no per-row string building. The row pitch is the
        // style-driven line advance measured from the first declared row each frame:
        // the cursor's screen Y before and after one Text item is that advance, and
        // the scroll offset shifts both readings equally within a frame, so the delta
        // holds at any scroll position. The public API exposes no style metrics, so
        // this is the only scale-correct source (G2-12); the seed only matters until
        // the first row is declared. The trailing Dummy is required: ImGui asserts
        // when SetCursorY extends parent boundaries without a following item
        // (imgui.cpp ErrorCheckUsingSetCursorPosToExtendParentBoundaries).
        private void DrawImGuiVirtualizedList()
        {
            float scrollY = DearImGuiKSP.DearImGuiKSP.GetScrollY();
            float rowHeight = _rowHeight;

            int firstVisible = Mathf.FloorToInt(scrollY / rowHeight);
            if (firstVisible < 0)
            {
                firstVisible = 0;
            }
            else if (firstVisible >= _items.Count)
            {
                firstVisible = _items.Count - 1;
            }
            int visibleCount = Mathf.CeilToInt(ListViewHeight / rowHeight) + 2;
            int lastVisible = Mathf.Min(_items.Count, firstVisible + visibleCount);

            DearImGuiKSP.DearImGuiKSP.SetCursorY(firstVisible * rowHeight);
            Vector2 before = DearImGuiKSP.ImGuiDraw.GetCursorScreenPos();
            DearImGuiKSP.DearImGuiKSP.Text(_items[firstVisible]);
            Vector2 after = DearImGuiKSP.ImGuiDraw.GetCursorScreenPos();
            float measured = after.y - before.y;
            if (measured > 0f && !float.IsNaN(measured) && !float.IsInfinity(measured))
            {
                _rowHeight = measured;
            }
            for (int i = firstVisible + 1; i < lastVisible; i++)
            {
                DearImGuiKSP.DearImGuiKSP.Text(_items[i]);
            }
            DearImGuiKSP.DearImGuiKSP.SetCursorY(_items.Count * _rowHeight);
            DearImGuiKSP.DearImGuiKSP.Dummy(0f, 0f);
        }

        private void DrawImguiReferenceWindow(int windowId)
        {
            GUILayout.Label("Naive vs Virtualized List (1000 items)");

            bool newVirtualized = GUILayout.Toggle(_imguiVirtualized, "Use Virtualization");
            // IMGUI lays out in the Layout pass and replays the SAME control tree in
            // the Repaint/input passes: flipping the branch mid-pass lays out a
            // different control count and the next Repaint throws "Getting control
            // N's position in a group with only M controls" (G3-33). Toggle returns
            // the flipped value ONLY on the click's input-event pass — every other
            // pass returns the passed-in state — so the stash updates only when the
            // returned value differs from the current state (a Repaint pass would
            // otherwise overwrite the pending flip before the next Layout). The
            // commit to the tree-driving state happens during Layout, keeping all
            // passes of a frame consistent while clicks still land.
            if (newVirtualized != _imguiVirtualized)
            {
                _pendingImguiVirtualized = newVirtualized;
            }
            if (Event.current.type == EventType.Layout &&
                _pendingImguiVirtualized != _imguiVirtualized)
            {
                _imguiVirtualized = _pendingImguiVirtualized;
                _imguiScrollPos = Vector2.zero; // reset scroll when switching modes
            }

            GUILayout.Label(FpsReadoutLine());
            GUILayout.Label(string.Format(
                "IMGUI OnGUI script time: {0} ms (60-frame avg)", FormatMs(_imguiAvgMs)));
            GUILayout.Label("Note: IMGUI's real cost is Unity-internal rendering after");
            GUILayout.Label("OnGUI returns — watch the frame-time line, not script time.");
            GUILayout.Space(4f);

            if (_imguiVirtualized)
            {
                GUILayout.Label("Virtualized: only ~12 items are actually drawn.");
                DrawImguiVirtualizedList();
            }
            else
            {
                GUILayout.Label("Naive: all 1000 GUILayout items processed every frame.");
                GUILayout.Label("Try scrolling — notice the stutter.");
                DrawImguiNaiveList();
            }

            GUI.DragWindow();
        }

        // IMGUI_Helper PerformanceTab's virtualized path, verbatim in spirit: reserve
        // the total height with one GetRect, then draw only the visible rows at
        // explicit rects so IMGUI skips layout for the other ~988 items.
        private void DrawImguiVirtualizedList()
        {
            _imguiScrollPos = GUILayout.BeginScrollView(_imguiScrollPos, GUILayout.Height(ListViewHeight));

            float totalHeight = _items.Count * ImguiRowHeight;
            Rect viewRect = GUILayoutUtility.GetRect(0f, totalHeight);

            int firstVisible = Mathf.FloorToInt(_imguiScrollPos.y / ImguiRowHeight);
            if (firstVisible < 0)
            {
                firstVisible = 0;
            }
            int visibleCount = Mathf.CeilToInt(ListViewHeight / ImguiRowHeight) + 2;
            int lastVisible = Mathf.Min(_items.Count, firstVisible + visibleCount);

            for (int i = firstVisible; i < lastVisible; i++)
            {
                Rect rowRect = new Rect(viewRect.x, viewRect.y + (i * ImguiRowHeight), viewRect.width, ImguiRowHeight);
                GUI.Label(rowRect, _items[i]);
            }

            GUILayout.EndScrollView();
        }

        // IMGUI_Helper PerformanceTab's naive path: 1000 GUILayout.Label calls inside a
        // scroll view — IMGUI runs layout for the entire collection every frame.
        private void DrawImguiNaiveList()
        {
            _imguiScrollPos = GUILayout.BeginScrollView(_imguiScrollPos, GUILayout.Height(ListViewHeight));
            for (int i = 0; i < _items.Count; i++)
            {
                GUILayout.Label(_items[i]);
            }
            GUILayout.EndScrollView();
        }

        private void UpdateFpsEma()
        {
            float dt = Time.unscaledDeltaTime;
            // The first sampled frame can report dt == 0; seeding the EMA with it
            // prints Infinity for that frame and the 0.05 lerp takes dozens of
            // frames to wash out (G4-02). Non-finite deltas poison the EMA
            // permanently (G3-39 family). Skip degenerate frames entirely.
            if (dt <= 0f || float.IsNaN(dt) || float.IsInfinity(dt))
            {
                return;
            }
            _fpsEma = _fpsEma < 0f ? dt : Mathf.Lerp(_fpsEma, dt, 0.05f);
        }

        // Both windows share the FPS line; valid only once the EMA has a real sample.
        private string FpsReadoutLine()
        {
            return _fpsEma > 0f
                ? string.Format("FPS: {0:0.0}  ({1:0.0} ms frame)", 1f / _fpsEma, _fpsEma * 1000f)
                : "FPS: --  (-- ms frame)";
        }

        // Accumulates samples; every RollingWindowFrames samples the average is
        // published and the accumulator resets.
        private static void AddSample(float ms, ref float sum, ref int count, ref float avg)
        {
            sum += ms;
            count++;
            if (count >= RollingWindowFrames)
            {
                avg = sum / count;
                sum = 0f;
                count = 0;
            }
        }

        private static string FormatMs(float avg)
        {
            return avg < 0f ? "collecting..." : string.Format("{0:0.000}", avg);
        }
    }
}
