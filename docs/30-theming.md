# Theming

> Authored in milestone M7 of the pre-release feature wave (spec §8.2, D31).
> Sources: `DearImGuiKSP/Application/Theming/{ThemePresets,KspPalette,
> ThemeEngine}.cs`. How the library styles your UI, and how to restyle it.

Previous: [Widget Catalog](20-widgets.md) - Next:
[Plotting](40-plotting.md)

## 1. The two presets

The library ships two theme presets:

| Name | What it is |
|---|---|
| `ksp` | **The default.** KSP UI palette (blue-grey title bars and button gradients, dark grey window background, off-white text, KSP green/orange accents), IBM Plex Sans, 4-6 px rounding. Window backgrounds and gradient buttons use two-stop vertical gradients drawn natively each frame. |
| `dark` | The **exact stock ImGui dark style** — a zero-override marker, applied as ImGui's own `StyleColorsDark()` reset plus nothing. Retained as a regression baseline; under it the native gradient pass is disabled so it stays byte-exact stock. |

Read the active preset at runtime:

```csharp
public static string CurrentTheme { get; }   // "ksp" or "dark"
```

`CurrentTheme` is **read-only** — there is no API to switch themes from a
mod, and that is deliberate: theme choice belongs to the user.

## 2. Changing the theme

The theme lives in the library's `settings.cfg`:

```
DEARIMGUIKSP_SETTINGS
{
	theme = ksp
}
```

Valid values are `ksp` and `dark` (any case). Any other value falls back to
`ksp` with a single log line (`Unknown theme '<name>'; using 'ksp'.`).
A change takes effect at the **start of the next UI frame** — the frame loop
re-applies the theme there, never mid-callback, so a switch can never catch
a window half-drawn.

Everything else about the look — window background gradients, button
gradient stops, rounding — is fixed by the preset. For local, per-widget
variation use style push/pop (below), and for signature buttons use the
gradient helpers (below). There is no API to author new presets.

## 3. Local overrides: style push/pop

Style overrides are the escape hatch for "this one label in orange":

```csharp
using (DearImGuiKSP.ImGuiEx.StyleColor(
    DearImGuiKSP.ImGuiCol.Text, new Color32(255, 198, 0, 255)))
{
    DearImGuiKSP.DearImGuiKSP.Text("warning");
}
```

`ImGuiEx.StyleColor` / `ImGuiEx.StyleVar` scopes pair the push with its pop
automatically — including when the body throws. The raw facade calls
(`PushStyleColor`, `PopStyleColor`, `PushStyleVar`, `PopStyleVar`) exist too;
every push must be popped before the end of the frame, so prefer the scopes.
See [API Fundamentals](10-api-fundamentals.md#5-style-pushpop-and-the-style-enums)
for the enum tables and the float-vs-Vector2 rule.

The slots most useful for local theming: `ImGuiCol.Text`, `ImGuiCol.FrameBg`,
`ImGuiCol.Button`, `ImGuiCol.CheckMark` (radio fill / checkbox tick),
`ImGuiCol.PlotLines`, plus `ImGuiStyleVar.FrameRounding` and
`ImGuiStyleVar.ItemSpacing`.

## 4. Gradient helpers

Two-stop vertical gradients are a KSP visual signature. The public surface:

```csharp
// On ImGuiGradients:
public static void AddRectFilledGradientVertical(
    Vector2 min, Vector2 max, Color32 top, Color32 bottom, float rounding)
public static bool GradientButton(string label, Color32 top, Color32 bottom, Vector2 size)
public static bool GradientButton(string label, Vector2 size, GradientButtonStyle style)
```

`AddRectFilledGradientVertical` fills a screen-coordinate rect with a
vertical gradient from `top` (at `min`) to `bottom` (at `max`), preserving
the corner `rounding` and each vertex's alpha. Use it for custom gauges,
bars, and panels (see the `ImGuiDraw` canvas pattern in
[Widget Catalog](20-widgets.md#imguidraw-custom-drawing)). It renders
identically in every theme.

`GradientButton` is documented with the other buttons in
[Widget Catalog](20-widgets.md#button): the explicit-color overload takes
your own stops; the `GradientButtonStyle` overload takes the preset's
`Primary` (blue-grey) or `Secondary` (plain grey) stops — and because a
gradient button is an explicit consumer call, both styles render with the
KSP palette stops in every theme, including "dark".

## 5. KspPalette: the KSP palette constants

`DearImGuiKSP.Application.KspPalette` exposes the tuned KSP palette as
public `Color32` constants, so your accents can match the library exactly.
The theme **never** auto-colors your text — these are opt-in, e.g. via
`TextColored(KspPalette.GreenLight, "...")`.

| Constant | sRGB bytes | Role |
|---|---|---|
| `WindowBgTop` | 94, 97, 106 | Window background gradient, top stop |
| `WindowBgBottom` | 58, 58, 63 | Window background gradient, bottom stop |
| `BorderDark` | 30, 32, 38 | 1 px window border |
| `TitleBar` | 57, 72, 90 | Title bar (flat) |
| `ButtonGradientTop` | 102, 114, 135 | Primary button gradient, top stop; also slider/scrollbar grab |
| `ButtonGradientBottom` | 57, 72, 90 | Primary button gradient, bottom stop |
| `ButtonSecondaryGradientTop` | 135, 143, 158 | Secondary button gradient, top stop |
| `ButtonSecondaryGradientBottom` | 69, 77, 92 | Secondary button gradient, bottom stop |
| `ButtonHover` | 125, 135, 153 | Button hover (top lightened ~15%) |
| `ButtonActive` | 126, 155, 95 | Button held (shifted toward KSP green) |
| `FrameBg` | 58, 58, 63 | Frame background (inputs, sliders) |
| `FrameBgHovered` | 88, 88, 92 | Frame background, hovered |
| `FrameBgActive` | 117, 117, 121 | Frame background, held |
| `TextOffWhite` | 236, 236, 236 | Default text |
| `TextLightGrey` | 188, 188, 188 | Secondary text |
| `TextOnDarkFill` | 58, 58, 63 | Text sitting on dark infill |
| `GreenLight` | 181, 252, 0 | KSP light green accent (checkbox tick, radio fill, active mix) |
| `GreenDark` | 51, 230, 51 | KSP dark green accent |
| `OrangeLight` | 255, 198, 0 | KSP light orange accent (typed InputText text) |
| `OrangeDark` | 255, 150, 0 | KSP dark orange accent |

`KspPalette` is in the `DearImGuiKSP.Application` namespace — add
`using DearImGuiKSP.Application;` or fully qualify it. All constants are
`Color32`; Unity's implicit conversion covers `Color` parameters
(e.g. the `Spinner` tint).

## 6. How the preset maps onto widgets

Under `ksp`, knowing the mapping helps you pick push/pop slots:

- Text: `TextOffWhite`; disabled text: `TextLightGrey`.
- Window background: a two-stop gradient (`WindowBgTop` to
  `WindowBgBottom`) drawn natively over the flat `WindowBg` slot.
- Title bar / headers: flat `TitleBar` blue-grey.
- Buttons: gradient stops above; `ButtonHover`/`ButtonActive` are the
  hover/held targets used by themed controls.
- Sliders and scrollbars grab: `ButtonGradientTop`.
- Radio fill / checkbox tick: `GreenLight` via `ImGuiCol.CheckMark`.
- InputText typed text: `OrangeLight` (under the `ksp` theme only; "dark"
  keeps stock single-color InputText rendering).

Under `dark`, every slot is exactly ImGui's stock dark value; gradient
drawing (window background and gradient-button backgrounds via the themed
overload) still uses the KSP constants because both are explicit drawing
paths, not theme slots.

## 7. Accessibility

Per the design spec (§6.4):

- **Never encode meaning in color alone.** The KSP green/orange accents
  always pair with a text label — keep that rule in your own UI: an accent
  state must also be readable as text ("GO", "ARMED", "off"), not just as a
  hue change.
- Users who need larger UI have `uiScale` and `fontScale` (0.5-2.0) in
  `settings.cfg`; themes honor both.
- Animated spinners are the only continuously-moving element; static
  alternatives for "busy" indication exist in the classic widgets (e.g. a
  `Text` status line) if your audience prefers no motion.

Next: [Plotting](40-plotting.md).
