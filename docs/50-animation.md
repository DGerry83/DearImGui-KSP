# Animation with Tweens

The `Tween` static class animates float and `UnityEngine.Color` values over time: you give it a setter, a from/to pair, a duration, and an easing curve, and the library's frame loop invokes the setter with the eased value each frame. Tweens are fire-and-forget — you do not tick them yourself — with an optional handle for cancellation and status. The engine is pure C# (no native interop) and allocates nothing per frame.

Prerequisites: [Getting Started](00-getting-started.md) and [API Fundamentals](10-api-fundamentals.md). For coloring animated output, see [Theming](30-theming.md).

## Starting a tween

```csharp
public static TweenHandle Tween.To(Action<float> set, float from, float to, float seconds, Ease ease)
public static TweenHandle Tween.To(Action<Color> set, Color from, Color to, float seconds, Ease ease)
```

Behavior, exactly as implemented:

- The setter is invoked **once immediately** with `from` (the t = 0 baseline) — before `To` returns.
- The setter is then invoked each frame with the eased value; the **final invocation is exactly `to`** (short-circuited, not the last lerp step).
- `seconds` is the duration in seconds. Zero or negative completes on the first frame after the baseline call.
- The color overload applies `Color.Lerp(from, to, easedT)` component-wise, with the same exact-endpoint guarantee.
- A null setter throws `ArgumentNullException` — the only way `Tween.To` throws.
- If the library is not available, `To` logs a warning and returns an **inert handle**: `IsPlaying` is false and `Cancel` is a no-op. Availability races never throw.

## The handle

```csharp
public readonly struct TweenHandle
{
    public bool IsPlaying { get; }   // true while the tween is live on the engine
    public void Cancel();           // stop; never throws; no-op on completed/inert handles
}
```

The handle is a readonly struct — storing it in a field and checking it costs nothing (no boxing, no allocation). `Cancel()` from inside the setter itself is safe. Completed and cancelled tweens are removed from the engine automatically; nothing accumulates across frames.

## The easing set

```csharp
public enum Ease
{
    Linear,     // constant rate; eased value is t itself
    QuadIn,     // quadratic acceleration; t squared
    QuadOut,    // quadratic deceleration
    QuadInOut,  // quadratic accelerate then decelerate
    CubicIn,    // cubic acceleration; t cubed
    CubicOut,   // cubic deceleration
    CubicInOut, // cubic accelerate then decelerate
}
```

Every curve is exact at both endpoints (t = 0 yields `from`, t = 1 yields `to`), so any curve chains cleanly into the next tween.

## Frame-loop semantics

- Tweens are ticked **once per frame by the library's frame loop, before consumer callbacks run**, using frame delta time. The values you read in your registered callback this frame already include this frame's tween step. A tween's trajectory is deterministic: delta time plus the easing curve, nothing else.
- **Suspension pauses tweens for free.** While the library is suspended (F2 hide, loading screens) the frame loop does not run, so no setter invocations happen and no tween time elapses. When the library resumes, the tween continues from where it froze. You do not need to react to suspension.
- **Fire-and-forget is the norm.** If you never need to cancel, you can ignore the returned handle entirely.
- **Restart pattern:** call `Cancel()` on the old handle before starting the replacement tween so a re-trigger mid-flight restarts from the current value instead of stacking two tweens on the same field.

## Allocation guidance

The tween engine itself allocates nothing per frame (live tweens live in a preallocated list of struct entries). Your side should match it:

- **Cache the setter delegate in a field.** A method-group conversion allocates a single delegate at construction; assigning it once in your constructor keeps the per-frame path (and every `Tween.To` call) free of closure building:

```csharp
private readonly System.Action<float> _setLevel;

public MyConsumer()
{
    _setLevel = SetLevel;   // one delegate, created once
}

private void SetLevel(float value)
{
    _level = value;
}
```

- A non-capturing lambda also converts once and is fine; a lambda that captures allocates a closure per call — avoid those on a repeated trigger path.
- `TweenHandle` fields are safe to call blind: `Cancel()` on a default/completed handle is a defined no-op.

## Minimal example: animating a knob

A self-contained consumer section that tweens a knob from 0 to 100 over 2 seconds when a button is pressed, with a cancel button and a live status line. State lives in fields; the tween drives the same field the knob writes, so the knob and the tween never fight:

```csharp
using System;
using UnityEngine;

private const float KnobMin = 0f;
private const float KnobMax = 100f;
private const float TweenSeconds = 2f;

private float _knobValue;                                  // tween target and knob state
private DearImGuiKSP.TweenHandle _tweenHandle;             // default handle is inert
private readonly Action<float> _setKnobValue;              // cached setter delegate

public MyConsumer()
{
    _setKnobValue = SetKnobValue;                          // allocate the delegate once
}

// Inside your registered callback, inside a window scope:
if (DearImGuiKSP.DearImGuiKSP.Button("Play tween"))
{
    _tweenHandle.Cancel();                                 // restart cleanly if mid-flight
    _tweenHandle = DearImGuiKSP.Tween.To(
        _setKnobValue, _knobValue, KnobMax, TweenSeconds, DearImGuiKSP.Ease.QuadInOut);
}
if (DearImGuiKSP.DearImGuiKSP.Button("Cancel tween"))
{
    _tweenHandle.Cancel();                                 // no-op when nothing is playing
}
DearImGuiKSP.DearImGuiKSP.Knob("Animated", ref _knobValue, KnobMin, KnobMax);
DearImGuiKSP.DearImGuiKSP.Text(
    _tweenHandle.IsPlaying ? "Tween: playing" : "Tween: idle");

private void SetKnobValue(float value)
{
    _knobValue = value;
}
```

The knob is `Knob(string label, ref float value, float min, float max)` — the simple overload; see the [Widget catalog](20-widgets.md) for the full variant/flags surface. The demo's `ThemeDemo` class (`DearImGuiKSPDemo/ThemeDemo.cs`) shows the ping-pong version of this (play flips the target between the min and max each press, plus a parallel `Color` tween driving a `TextColored` header), and `DearImGuiKSPDemo/Telemetry/StagePanel.cs` uses color tweens for propellant-meter transitions. Spinners and toggles, by contrast, animate themselves natively — no tweens needed for those; see the [Widget catalog](20-widgets.md).

## Next

- [Migrating from Unity IMGUI](60-migration-from-imgui.md) — replacing hand-rolled IMGUI animation timers.
- [Troubleshooting](70-troubleshooting.md) — what "Tween.To ignored" in the log means.
- Back to [Plotting](40-plotting.md) or [Theming](30-theming.md).
