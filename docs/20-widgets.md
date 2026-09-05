# Widget Catalog

> Every widget below shows its real signature (from `DearImGuiKSP/Application/
> DearImGuiKSP.cs` and `DearImGuiKSP/Application/Api/*.cs`) and one minimal
> working example. All widget calls are only valid inside a registered
> per-frame callback (see [API Fundamentals](10-api-fundamentals.md)).

Previous: [API Fundamentals](10-api-fundamentals.md) -
Next: [Theming](30-theming.md)

General conventions:

- Widgets **stack vertically**, one per line, in declaration order. There is
  **no public `SameLine` yet** (it exists only inside the library) — for
  side-by-side layouts see [ImGuiDraw](#imguidraw-custom-drawing) and
  [Layout](#layout-dummy--setcursory), and the "Known layout gaps" note below.
- Value-returning widgets report "changed this frame" and update your `ref`
  value in place; returns are `false` when the library is unavailable.
- Labels double as widget IDs — see the immediate-mode ID rules in
  [API Fundamentals](10-api-fundamentals.md#6-immediate-mode-ids-labels--and-the-spinner-id-rule).

## Text

```csharp
public static void Text(string text)
public static void TextColored(Color32 color, string text)
```

`Text` draws unformatted text in the theme's text color. `TextColored`
draws it in an explicit color — the consumer-choice pattern for accents and
colored headers. The theme **never** auto-colors your text; opting in is
always your decision. `null` renders as an empty string.

```csharp
DearImGuiKSP.DearImGuiKSP.Text("Stage 1 ready.");
DearImGuiKSP.DearImGuiKSP.TextColored(
    DearImGuiKSP.Application.KspPalette.GreenLight, "GO");
```

## Button

```csharp
public static bool Button(string label)
public static bool GradientButton(string label, Color32 top, Color32 bottom, Vector2 size)
public static bool GradientButton(string label, Vector2 size, GradientButtonStyle style)
```

`Button` is an auto-sized stock button. `GradientButton` (on `ImGuiGradients`)
is the KSP theme's signature button: a two-stop vertical gradient rect with a
centered label. Interaction is stock ImGui button behavior — hover lightens
both stops ~15%, holding shifts both stops toward the KSP light green.

- The explicit-color overload takes the two gradient stops as sRGB bytes.
- The themed overload takes a `GradientButtonStyle`: `Primary` (signature
  blue-grey) or `Secondary` (plain grey). Gradient buttons are an explicit
  consumer call, so both styles render with the KSP palette stops in **every**
  theme, including "dark".
- In the `size` arguments, a component `<= 0` fits that dimension to the
  label plus padding, so `new Vector2(0f, 28f)` auto-fits the width.

```csharp
if (DearImGuiKSP.DearImGuiKSP.Button("Stage"))
{
    StageVessel();
}
if (DearImGuiKSP.ImGuiGradients.GradientButton(
    "Launch", new Vector2(160f, 30f), DearImGuiKSP.GradientButtonStyle.Primary))
{
    LaunchVessel();
}
```

## SliderFloat

```csharp
public static bool SliderFloat(string label, ref float value, float min, float max)
```

A float slider. Returns true when the value changed this frame; `value` is
updated in place.

```csharp
private float _throttle;
// inside the callback:
DearImGuiKSP.DearImGuiKSP.SliderFloat("Throttle", ref _throttle, 0f, 1f);
```

## InputText

```csharp
public static bool InputText(string label, ref string value, int capacity = 256)
```

A single-line text field. Returns true when the user edited the text this
frame; `value` is updated in place. `capacity` is the buffer size **in bytes,
including the NUL terminator** — edited text is truncated to `capacity - 1`
UTF-8 bytes. The default of 256 is fine for most fields; raise it for long
free-text entries.

Under the "ksp" theme the label is drawn separately in off-white and only
the typed text renders in the KSP light orange (ImGui colors an InputText's
label with the same color as its contents, so the library pushes the orange
just for the field). Under "dark" the stock single-call rendering is kept.

```csharp
private string _vesselName = "Untitled";
// inside the callback:
DearImGuiKSP.DearImGuiKSP.InputText("Name", ref _vesselName, 64);
```

## Toggle (the KSP checkbox)

```csharp
public static bool Toggle(string label, ref bool value)
public static bool Toggle(string label, ref bool value, ToggleFlags flags)
```

An animated toggle switch — the KSP theme's replacement for a checkbox.
Returns true when the value changed this frame; `value` is updated in place.
Combine `ToggleFlags` values for the second overload:

| Flag | Effect |
|---|---|
| `None` (0) | Plain toggle |
| `Animated` (1 << 0) | Animates the knob between states |
| `BorderedFrame` (1 << 3) | Border on the toggle frame |
| `BorderedKnob` (1 << 4) | Border on the knob |
| `ShadowedFrame` (1 << 5) | Shadow under the frame |
| `ShadowedKnob` (1 << 6) | Shadow under the knob |
| `A11y` (1 << 8) | Draws on/off glyphs indicating state |
| `Bordered` | Shorthand for `BorderedFrame \| BorderedKnob` |
| `Shadowed` | Shorthand for `ShadowedFrame \| ShadowedKnob` |

```csharp
private bool _autoStage = true;
// inside the callback:
DearImGuiKSP.DearImGuiKSP.Toggle(
    "Auto-stage", ref _autoStage,
    DearImGuiKSP.ToggleFlags.Animated | DearImGuiKSP.ToggleFlags.Bordered);
```

## RadioButton

```csharp
public static bool RadioButton(string label, ref bool value)
public static bool RadioButton(string label, ref int value, int option)
```

A circular radio button in KSP style (with a light-grey rim so the ring
stays readable against the window background gradient). Clicking selects it.
The selected fill color is the theme's `ImGuiCol.CheckMark` slot (KSP light
green under "ksp").

For a mutually-exclusive option set, use the `int` overload with a group of
buttons bound to one field and distinct `option` values. Returns true on the
frame the button is clicked.

```csharp
private int _guidanceMode; // 0 = surface, 1 = orbit
// inside the callback:
DearImGuiKSP.DearImGuiKSP.RadioButton("Surface", ref _guidanceMode, 0);
DearImGuiKSP.DearImGuiKSP.RadioButton("Orbit", ref _guidanceMode, 1);
```

## Knob

```csharp
public static bool Knob(string label, ref float value, float min, float max)
public static bool Knob(string label, ref float value, float min, float max,
    float speed, KnobVariant variant, float size, KnobFlags flags, int steps, string format = null)
public static bool Knob(string label, ref int value, int min, int max)
public static bool Knob(string label, ref int value, int min, int max,
    float speed, KnobVariant variant, float size, KnobFlags flags, int steps, string format = null)
```

A rotary knob (float and int overloads). The simple overloads default to a
`Tick` variant sized to four lines of the current font (about 72 px at the
18 px base font, uiScale 1.0) with a `"%.3f"` (`"%i"`) inline input.

Full-overload arguments: `speed` is the drag sensitivity (`0f` selects the
upstream default `(max - min) / 250`); `size` is the widget size in pixels
(`0f` sizes from the font); `format` is a printf-style format for the inline
input (`null` = `"%.3f"` / `"%i"`); `steps` is the tick count used by the
`Stepped` variant.

`KnobVariant` values (visual styles): `Tick`, `Dot`, `Wiper`, `WiperOnly`,
`WiperDot`, `Stepped`, `Space`.

`KnobFlags` values (behavior): `None`, `NoTitle` (hide the label above),
`NoInput` (hide the inline drag-scalar), `ValueTooltip` (tooltip with the
formatted value while hovered), `DragHorizontal`, `DragVertical`,
`Logarithmic` (requires a range not spanning zero), `AlwaysClamp`.

```csharp
private float _mixture = 50f;
// inside the callback:
DearImGuiKSP.DearImGuiKSP.Knob("Mixture", ref _mixture, 0f, 100f);
DearImGuiKSP.DearImGuiKSP.Knob(
    "Trim", ref _mixture, 0f, 100f,
    0f, DearImGuiKSP.KnobVariant.WiperOnly, 48f,
    DearImGuiKSP.KnobFlags.ValueTooltip, 10);
```

## Wheel

```csharp
public static bool Wheel(string label, ref float value, float min, float max)
public static bool Wheel(string label, ref float value, float min, float max,
    float sizeX, float sizeY, WheelOrientation orientation,
    float speed = 1f, string format = null, bool invertColors = false)
public static bool Wheel(string label, ref int value, int min, int max)
public static bool Wheel(string label, ref int value, int min, int max,
    float sizeX, float sizeY, WheelOrientation orientation,
    float speed = 1f, string format = null, bool invertColors = false)
```

A barrel-shaped rolling-wheel input (float and int overloads; the int
version has detent resistance between integer steps). The simple overloads
default to a horizontal wheel 96 x 22 px with a `"%.2f"` (`"%d"`) value
label. The wheel rolls along its orientation axis, so pick a layout box
elongated along the drag axis: wide for `Horizontal`, tall for `Vertical`.

`WheelOrientation`: `Horizontal` (0), `Vertical` (1). `speed` is the drag
sensitivity multiplier (upstream default 1.0). `format` is printf-style
(`null` = `"%.2f"` / `"%d"`). `invertColors` swaps the wheel's color profile
against the current theme.

```csharp
private float _apTrim;
private int _headingBug = 90;
// inside the callback:
DearImGuiKSP.DearImGuiKSP.Wheel("AP trim", ref _apTrim, -100f, 100f);
DearImGuiKSP.DearImGuiKSP.Wheel(
    "Heading bug", ref _headingBug, 0, 360,
    24f, 120f, DearImGuiKSP.WheelOrientation.Vertical);
```

## Spinner

```csharp
public static void Spinner(SpinnerType type, float radius, float thickness,
    Color? tint = null, string id = null)
```

An animated activity spinner: ongoing-work indication with no interaction
and no state. Spinners animate themselves natively from the ImGui clock —
there is no tween or timer to drive. `radius` is the widget radius in pixels
(the `FadeBars` type uses it as the row width instead); `thickness` is the
line thickness in pixels (unused by `FadeBars`). `tint` is an optional
`Color` override (`null` uses the spinner's upstream default — white for
most types, half-white backgrounds for `Clock` and `Pulsar`). `id` is an
optional invisible ImGui ID.

The library binds a **curated subset of 15 types** (the upstream imspinner
build enables only these at compile time):

| Value | Visual | Value | Visual |
|---|---|---|---|
| `RainbowMix` | Rotating rainbow arc | `SwingDots` | Two dots swinging on a pivot |
| `Ang8` | Eight-tick activity arc | `DnaDots` | DNA double-helix |
| `Clock` | Clock face, sweeping hand | `FadeBars` | Row of fading vertical bars |
| `Pulsar` | Fading pulse ring sequence | `MorphShape` | Shape morphing through corners |
| `Atom` | Atom with orbiting electrons | `FlipTriangle` | Triangle flipping around its axis |
| `DotsToBar` | Dots collapsing into a bar | `FoldSquare` | Square folding through its diagonal |
| | | `Pinwheel` | Pinwheel of rotating blades |
| `CornerSquares` | Four corner squares chasing | `SplitSquare` | Square splitting into a mirrored pair |

**ID rule:** the default is a unique invisible per-type ID, so one spinner of
each type per window works with no extra arguments. Two spinners of the same
type in one window must be given distinct `id` values (standard `"##"`
semantics; never pass an empty string — an empty ID at window root trips an
ImGui assert):

```csharp
// different types: no ids needed
DearImGuiKSP.DearImGuiKSP.Spinner(DearImGuiKSP.SpinnerType.Clock, 12f, 3f);
DearImGuiKSP.DearImGuiKSP.Spinner(DearImGuiKSP.SpinnerType.Ang8, 12f, 3f);

// same type twice: distinct ids
DearImGuiKSP.DearImGuiKSP.Spinner(DearImGuiKSP.SpinnerType.Clock, 12f, 3f, id: "##clock_a");
DearImGuiKSP.DearImGuiKSP.Spinner(DearImGuiKSP.SpinnerType.Clock, 12f, 3f, id: "##clock_b");

// tinted (Color, not Color32 - a KspPalette Color32 converts implicitly)
DearImGuiKSP.DearImGuiKSP.Spinner(
    DearImGuiKSP.SpinnerType.Atom, 12f, 3f,
    DearImGuiKSP.Application.KspPalette.GreenLight);
```

**Known tint quirks** (confirmed upstream behavior of the vendored spinner
library, not binding bugs):

- `RainbowMix` derives its hue from the tint's HSV saturation. The default
  white tint has saturation 0, so the arc renders grey/white — pass a
  saturated `tint` to get an actual rainbow.
- `Atom` hardcodes its electron dots to red/green/blue upstream; the tint
  colors only the orbit ellipses.

## CollapsingHeader

```csharp
public static bool CollapsingHeader(string label, bool defaultOpen = false)
```

A collapsible section header inside a window — the group-level equivalent of
minimizing a whole window. Returns true while the section is open; draw the
section's content inside the `if`:

```csharp
if (DearImGuiKSP.DearImGuiKSP.CollapsingHeader("Guidance", defaultOpen: true))
{
    DearImGuiKSP.DearImGuiKSP.SliderFloat("Gain", ref _gain, 0f, 2f);
    DearImGuiKSP.DearImGuiKSP.Button("Calibrate");
}
```

This is a single call, not a Begin/End pair — there is no scope to dispose.
`defaultOpen` is only consulted when the header has no stored state yet:
section open/closed state lives per window, keyed by the label's ID (the
usual `"##"` disambiguation rules apply), and it is **not** persisted across
sessions. Every launch starts from `defaultOpen` again — the same contract as
window positions.

## Tabs: TabBar / TabItem

Use the `ImGuiEx` scopes (see [API Fundamentals](10-api-fundamentals.md#3-scopes-imguiex)):

```csharp
using (var tabBar = DearImGuiKSP.ImGuiEx.TabBar("pages"))
{
    if (tabBar.Visible)
    {
        using (var tab = DearImGuiKSP.ImGuiEx.TabItem("Overview"))
        {
            if (tab.Visible)
            {
                DrawOverview();
            }
        }
        using (var tab = DearImGuiKSP.ImGuiEx.TabItem("Settings"))
        {
            if (tab.Visible)
            {
                DrawSettings();
            }
        }
    }
}
```

A `TabItem` scope's `Visible` is true only when that tab is selected — gate
the tab's content on it (tabs are non-closable). Tab bars are only valid
inside a window.

## Scroll regions

```csharp
public static bool BeginScrollRegion(string id, float height)
public static bool BeginScrollRegion(string id, Vector2 size)
public static void EndScrollRegion()
public static float GetScrollY()
public static void SetCursorY(float y)
```

`BeginScrollRegion` starts a fixed-height (or sized; `size.x = 0` stretches
to available width), bordered, scrolling child region; prefer the
`ImGuiEx.ScrollRegion` scope, whose Dispose always ends the region.

For **large lists**, virtualize manually instead of drawing every row:

1. Call `GetScrollY()` and compute the visible row range from it.
2. `SetCursorY(firstVisibleRow * rowHeight)` to jump to the first visible row.
3. Draw only the visible rows.
4. `SetCursorY(rowCount * rowHeight)` followed by `Dummy(...)` so the
   scrollable range legitimately covers the full list — ImGui requires an
   actual item (not a bare cursor move) to grow content bounds.

```csharp
private const float RowHeight = 22f;
// inside the callback:
using (var region = DearImGuiKSP.ImGuiEx.ScrollRegion("vessels", 200f))
{
    if (region.Visible)
    {
        float scroll = DearImGuiKSP.DearImGuiKSP.GetScrollY();
        int first = (int)(scroll / RowHeight);
        int count = _vessels.Count;
        int last = Math.Min(first + 10, count);
        DearImGuiKSP.DearImGuiKSP.SetCursorY(first * RowHeight);
        for (int i = first; i < last; i++)
        {
            DearImGuiKSP.DearImGuiKSP.Text(_vessels[i].vesselName);
        }
        DearImGuiKSP.DearImGuiKSP.SetCursorY(count * RowHeight);
        DearImGuiKSP.DearImGuiKSP.Dummy(1f, 1f);
    }
}
```

## Layout: Dummy / SetCursorY

```csharp
public static void Dummy(float width, float height)
public static void Dummy(Vector2 size)
public static void SetCursorY(float y)
```

`Dummy` submits an invisible item of the given size: it advances the cursor
and grows the window's content bounds, reserving blank vertical space.
`SetCursorY` sets the absolute cursor Y within the active window/region.
Together they are the layout toolkit — vertical rhythm via `Dummy`, jump-to
via `SetCursorY` — and `Dummy` is also the required partner of the
cursor-anchor custom-drawing pattern below.

## ImGuiDraw: custom drawing

`ImGuiDraw` exposes draw-list primitives in **screen coordinates** on the
current window's draw list. The sanctioned pattern: query
`GetCursorScreenPos()` as the anchor, reserve a canvas with `Dummy`, then
draw at and around the anchor. All colors are `Color32`.

```csharp
public static Vector2 GetCursorScreenPos()
public static void AddLine(Vector2 p1, Vector2 p2, Color32 color, float thickness = 1f)
public static void AddCircle(Vector2 center, float radius, Color32 color, float thickness = 1f)
public static void AddCircleFilled(Vector2 center, float radius, Color32 color)
public static void AddEllipse(Vector2 center, Vector2 radii, Color32 color, float rotation = 0f, float thickness = 1f)
public static void AddEllipseFilled(Vector2 center, Vector2 radii, Color32 color, float rotation = 0f)
public static void AddRectFilled(Vector2 min, Vector2 max, Color32 color, float rounding = 0f)
public static void AddText(Vector2 pos, Color32 color, string text)
public static void Dummy(float width, float height)
```

Minimal example — a horizontal divider line across a 200 px-wide canvas:

```csharp
Vector2 origin = DearImGuiKSP.ImGuiDraw.GetCursorScreenPos();
DearImGuiKSP.ImGuiDraw.Dummy(200f, 8f);
DearImGuiKSP.ImGuiDraw.AddLine(
    origin, origin + new Vector2(200f, 0f),
    new Color32(94, 97, 106, 255), 1f);
```

Rules:

- Draw within the same callback — and the same window scope — that queried
  the cursor, and never retain coordinates across frames (windows move).
- Everything lands on the current window's draw list; outside a window scope
  the calls no-op.
- `AddText` encodes its string to UTF-8 on every call; keep it off the
  per-frame hot path or cache at the call site.

For filled two-stop **vertical gradient rects** (e.g. your own gauge
backgrounds), use `ImGuiGradients.AddRectFilledGradientVertical(min, max,
top, bottom, rounding)` — same screen-coordinate convention; details in
[Theming](30-theming.md).

## Known layout gaps

- **No public `SameLine`.** Widgets stack vertically; the only ways to place
  things side by side today are the `ImGuiDraw` cursor-anchor + `Dummy`
  canvas pattern above, or a rolling `Wheel`/`Knob` designed to sit in a
  sized box. A public same-line helper is a post-release candidate.
- There is no public checkbox, combo, tree, menu, table, or drag widget; the
  catalog above plus [Theming](30-theming.md) helpers is the full public
  surface of this release. (Plotting lives in the sibling doc
  [Plotting](40-plotting.md).)

Next: [Theming](30-theming.md).
