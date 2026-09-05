# Plotting with ImPlot

DearImGui-KSP ships a line-plot wrapper over a vendored [ImPlot](https://github.com/epezent/implot) (pinned at the v1.0 tag). The wrapper follows the same immediate-mode discipline as every other widget: you declare the plot and its line series **each frame**, inside a registered callback. All calls are in the `DearImGuiKSP` namespace and are safe to leave in your code when the library is unavailable — they no-op.

Prerequisites: you already know how to register a per-frame callback and open a window — see [Getting Started](00-getting-started.md) and [API Fundamentals](10-api-fundamentals.md). The `Vector2` in the signatures below is `UnityEngine.Vector2`.

## A minimal plot

```csharp
using UnityEngine;

private static readonly Vector2 PlotSize = new Vector2(340f, 130f);
private float _speed;

// Inside your registered callback, inside a window scope:
using (var plot = DearImGuiKSP.ImGuiPlot.Begin("Airspeed", PlotSize))
{
    if (plot.Visible)
    {
        DearImGuiKSP.ImGuiPlot.PlotLine("m/s", _samples.OldestFirst);
    }
}
```

Signature:

```csharp
public static PlotScope ImGuiPlot.Begin(string title, Vector2 size)
```

`Begin` returns a scope struct (a `readonly struct`, so `using` does not box). Two things to know about it:

- `plot.Visible` mirrors ImPlot's BeginPlot result. When it is false — the plot is collapsed or clipped this frame, or the library is unavailable — **skip the plot's content** but let the scope dispose normally. The scope's `Dispose` only calls ImPlot's EndPlot when the matching BeginPlot succeeded (ImPlot's pairing rule: calling EndPlot without a successful BeginPlot asserts).
- The scope must be disposed within the same frame/callback that created it. Never store it in a field.

`PlotLine` draws one line series inside the current plot:

```csharp
public static void ImGuiPlot.PlotLine(string label, System.ReadOnlySpan<float> values)
public static void ImGuiPlot.PlotLine(string label, System.ReadOnlySpan<double> values)
```

The x-values are implicit: point i is plotted at `x = i`. Only line series are exposed by this wrapper; there are no bar/scatter/heatmap APIs. An empty span is a no-op (the native call is skipped entirely), so a not-yet-filled buffer is fine.

The `title` and `label` strings double as the plot's ImPlot identity, so keep them stable across frames. Prefix with `##` to hide the visible text while keeping the identity: `"##telemetry_graphs"` shows nothing but still identifies the plot (the demo's telemetry grid does exactly this).

## Zero-copy data: what the span overloads promise

`PlotLine` reads your buffer **in place**. It pins the span's backing storage for the duration of the call only — no copy, no pinning handle, no boxing. That gives you the one rule the signature cannot enforce:

> The span's backing storage must stay alive and unmodified for the duration of the `PlotLine` call.

In practice: pass a span over an array you own (a field), and don't write to that array while the call is in flight. Because `PlotLine` consumes the buffer synchronously and never retains it, "the call" is exactly the single line — after it returns, the buffer is yours again. What you must **not** do is hand over a span whose backing array gets resized, collected, or mutated by another thread during that call. Do not pass spans over `List<T>` internals that can realloc; use fixed-capacity arrays.

The data path allocates nothing per frame. The only managed allocation on a `PlotLine` call is a short-lived null-terminated UTF-8 buffer for the label — which is why you should pass a constant label string (every string literal is one). Never build label strings per frame.

A span over a plain array is implicit, so `PlotLine("m/s", _array)` compiles as long as you pass the whole array. For a partial window use `new System.ReadOnlySpan<float>(_array, start, count)`.

## Subplot grids

`BeginSubplots` opens a grid of evenly sized cells; each cell is a normal plot begun with `ImGuiPlot.Begin`:

```csharp
public static SubplotScope ImGuiPlot.BeginSubplots(string title, int rows, int cols, Vector2 size)
```

```csharp
private static readonly Vector2 GridSize = new Vector2(520f, 360f);

using (var grid = DearImGuiKSP.ImGuiPlot.BeginSubplots("##telemetry_graphs", 2, 2, GridSize))
{
    if (grid.Visible)
    {
        DrawCell("##alt", "Altitude (m)", _altitude);
        DrawCell("##dynp", "Dyn pressure (kPa)", _dynPressure);
        DrawCell("##thr", "Throttle", _throttle);
        DrawCell("##g", "G-force", _gForce);
    }
}

private static void DrawCell(string title, string label, RingBuffer ring)
{
    using (var plot = DearImGuiKSP.ImGuiPlot.Begin(title, Vector2.zero))
    {
        if (plot.Visible)
        {
            DearImGuiKSP.ImGuiPlot.PlotLine(label, ring.OldestFirst);
        }
    }
}
```

Cells are laid out row-major. One ImPlot quirk matters here: **inside a subplot context the size argument of `Begin` is ignored** (the cells share the grid size evenly), so `Vector2.zero` is the conventional argument — do not bother computing a cell size.

## Hover queries

Two calls let you read the mouse position in the plot's coordinate system. Both are only valid between a successful `Begin` and the scope's `Dispose`:

```csharp
public static bool ImGuiPlot.IsPlotHovered()
public static Vector2 ImGuiPlot.GetPlotMousePos()
```

`GetPlotMousePos` returns `x`/`y` as the current axes' data values. Combine the two so the value is consumed only while the cursor is inside the plotting area:

```csharp
using (var plot = DearImGuiKSP.ImGuiPlot.Begin("##alt", Vector2.zero))
{
    if (!plot.Visible)
    {
        return;
    }
    DearImGuiKSP.ImGuiPlot.PlotLine("Altitude (m)", ring.OldestFirst);
    if (ring.Count > 0 && DearImGuiKSP.ImGuiPlot.IsPlotHovered())
    {
        Vector2 pos = DearImGuiKSP.ImGuiPlot.GetPlotMousePos();
        DearImGuiKSP.DearImGuiKSP.Text(
            string.Format("Altitude: {0:0.###} (sample {1:0})", pos.y, pos.x));
    }
}
```

This is the exact pattern the demo's `GraphPanel` uses for its per-cell hover readout. Note the allocation trade-off it makes deliberately: `string.Format` runs only while the user hovers a cell with data — the unhovered steady-state path allocates nothing.

## The ring-buffer pattern

Live graphs almost always mean a rolling history of samples. The pattern the library expects is a **fixed-capacity ring buffer preallocated once**, exposed oldest-to-newest as a span over a reused scratch array, so feeding the plot each frame costs one small in-place copy and zero allocation:

```csharp
// A minimal ring buffer — enough for one rolling line series.
private sealed class RingBuffer
{
    private readonly float[] _samples;
    private readonly float[] _ordered;
    private int _head;  // index of the OLDEST sample; full once _count == capacity
    private int _count;

    public RingBuffer(int capacity)
    {
        _samples = new float[capacity];
        _ordered = new float[capacity];
    }

    // Oldest-to-newest as a span over the reused scratch array.
    // Valid until the next Push; consumed synchronously by PlotLine each frame.
    public System.ReadOnlySpan<float> OldestFirst
    {
        get
        {
            int capacity = _samples.Length;
            for (int i = 0; i < _count; i++)
            {
                int idx = _head + i;
                _ordered[i] = _samples[idx >= capacity ? idx - capacity : idx];
            }
            return new System.ReadOnlySpan<float>(_ordered, 0, _count);
        }
    }

    public void Push(float value)
    {
        int capacity = _samples.Length;
        if (_count < capacity)
        {
            _samples[(_head + _count) % capacity] = value;
            _count++;
        }
        else
        {
            _samples[_head] = value;
            _head = _head + 1 == capacity ? 0 : _head + 1;
        }
    }
}
```

Usage: `Push` one sample per frame (from your callback or a sampler you already run per frame), then pass `OldestFirst` to `PlotLine`.

```csharp
private readonly RingBuffer _frameMs = new RingBuffer(240);

// Per frame, inside the registered callback:
_frameMs.Push(Time.unscaledDeltaTime * 1000f);
using (var plot = DearImGuiKSP.ImGuiPlot.Begin("Frame time (ms/frame)", PlotSize))
{
    if (plot.Visible)
    {
        DearImGuiKSP.ImGuiPlot.PlotLine("ms/frame", _frameMs.OldestFirst);
    }
}
```

This is the demo `PlotDemo` class almost verbatim (`DearImGuiKSPDemo/PlotDemo.cs`) — two 240-sample rings feeding two plots, nothing allocated on the steady-state path.

For a fuller implementation, look at the demo's telemetry ring (`DearImGuiKSPDemo/Telemetry/RingBuffer.cs`): 10,000-sample capacity with a windowed read, `LatestOldestFirst(int maxSamples)`, which copies only the newest `maxSamples` samples into the scratch array. That windowed read is the performance tool described next — reuse the file in your own mod if you like (it has no dependencies beyond `System`).

## Performance: plot cost scales with point count

Per-frame plot cost grows with the number of points you submit — both the scratch-copy pass in your ring and ImPlot's line segment submission are O(point count). The demo's telemetry tab learned this the hard way: plotting the full 10,000-sample ring every frame grew frame cost to about 7 ms late in a flight and shrank back to zero on revert. The fix shipped in the demo: **plot a rolling window**, not the whole history.

Rules of thumb:

- **Use a rolling window.** The demo plots the newest 1200 samples per cell — about 20 seconds at 60 Hz sampling (`GraphPanel.WindowSamples`). Keep the full history recorded in the ring if you want it, but submit only the window to `PlotLine` (`ring.LatestOldestFirst(1200)`). Cost becomes constant regardless of how long the player has been flying.
- **Auto-fit is fine at rolling-window sizes.** The 1200-point window keeps ImPlot's segment submission cheap and the auto-fitted axes readable; you do not need to manage axis limits yourself. (What made the full-buffer graphs unreadable was never auto-fit — it was the frame cost.)
- **Hover text is the one sanctioned allocation.** A `string.Format` readout line like the one above runs only while hovered; keep it to one line and only for the hovered cell.
- **Regression-test with the demo's benchmark window.** The demo mod ships a naive-vs-virtualized 1000-item benchmark window (`DearImGuiKSPDemo/BenchmarkUI.cs`) used as the standing performance instrument. If you suspect your plots cost too much, compare against it.

Also keep the 16-bit vertex limit in mind for very dense content — see [Troubleshooting](70-troubleshooting.md).

## Next

- [Animation](50-animation.md) — tween values into your plots' source buffers.
- [Migrating from Unity IMGUI](60-migration-from-imgui.md) — porting an IMGUI graph window.
- [Troubleshooting](70-troubleshooting.md) — the 16-bit index limit and other failure modes.
- Back to [API Fundamentals](10-api-fundamentals.md) / [Widget catalog](20-widgets.md).
