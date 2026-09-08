# API Fundamentals

> This is the reference for the programming model: registration, the frame
> callback, scopes, public types, availability rules, and the settings file.

Previous: [Getting Started](00-getting-started.md) -
Next: [Widget Catalog](20-widgets.md) - [Theming](30-theming.md)

Everything documented here is a member of the static class
`DearImGuiKSP.DearImGuiKSP` (namespace `DearImGuiKSP`), the `ImGuiEx` scope
factory class, or the `ImGuiDraw` / `ImGuiGradients` helper classes — unless
a different type is named explicitly. All examples compile against C# 7.3
(Unity 2019.4 / KSP 1.12.x).

## 1. Registration and the frame callback

```csharp
public static void Register(string id, Action callback)
public static bool Unregister(string id)
```

`Register` adds your per-frame callback; `Unregister` removes it. Callbacks
run **once per frame, in registration order**. That order decides only the
*initial* stacking of windows (a newly created window appears on top of
earlier ones); afterwards z-order follows focus, stock ImGui behavior —
clicking a window's title bar, body, or a widget raises it above the others.

**Widget calls are valid ONLY inside a callback invoked by the frame loop.**
Calling `Text`, `Button`, `ImGuiEx.Window`, style pushes, or any other
facade member from `Update`, `OnGUI`, a thread, or a delegate runs outside
the declaration phase and is a no-op (or returns false/0) — never an
exception. Declare everything inside the registered callback.

Multiple windows in one consumer are fine: one `Register` call, several
`ImGuiEx.Window` scopes in the callback, gated on your own visibility flags.

## 2. Availability: `IsAvailable`

```csharp
public static bool IsAvailable { get; }
```

`IsAvailable` is true when the library is initialized and either **Running**
or **Suspended**. It is false while Uninitialized/Initializing and
permanently false after an unrecoverable startup failure.

**Suspended is a pause, not a failure.** The library suspends itself on F2
(hiding the UI overlay) and during loading screens. When suspended:

- Your callback simply stops being invoked for a while.
- Do NOT tear down your UI, unregister, or null your state — the frame loop
  resumes automatically when the pause ends.

The only errors that throw are bad registration arguments
(`ArgumentNullException`): `Register` with a null **or empty** id or a null
callback, and `Unregister(null)` once the library is available (the registry
dictionary rejects a null key). Every other availability race or invalid
value is warn-and-ignore:

- `Register` while unavailable (or with a duplicate id): a warning line in
  the log, call ignored.
- `Unregister` while unavailable: warning, ignored, returns false.
- Any widget call while unavailable: silent no-op / false / zero.
- Invalid widget arguments never throw: an out-of-range style enum or
  spinner type or non-positive subplot dimensions are a logged no-op; an
  empty widget label is silently given a safe invisible ID instead of
  colliding with the window's own ID.

So the pattern is: check `IsAvailable` once at `Start`, and never write
defensive availability checks around individual widget calls inside your
callback — the callback only runs while available anyway.

If the library hits an unrecoverable startup failure, it self-disables for
the session and shows one plain-language popup at the main menu; technical
detail goes to the log under the `[DearImGuiKSP]` prefix.

## 3. Scopes: `ImGuiEx`

Every Begin/End and Push/Pop pair in the API has an exception-safe scope
guard in `ImGuiEx`. The factory begins immediately and returns a `readonly
struct` implementing `IDisposable`; `using` calls Dispose on every exit
path, including exceptions, so the ImGui stacks never leak even when your
code throws (the exception then reaches the fault barrier, section 7 below).

| Factory | Scope type | `Visible` semantics | Dispose behavior |
|---|---|---|---|
| `Window(string name, bool autoResize = false)` | `WindowScope` | false = window collapsed/clipped this frame | **Always** ends the window |
| `ScrollRegion(string id, float height)` | `ScrollRegionScope` | false = region clipped this frame | **Always** ends the region |
| `ScrollRegion(string id, Vector2 size)` | `ScrollRegionScope` | same; `size.x = 0` stretches to available width | Always ends the region |
| `TabBar(string id)` | `TabBarScope` | false = tab bar clipped | Ends the tab bar **only when visible** (ImGui requires End only after a successful Begin) |
| `TabItem(string label)` | `TabItemScope` | true = tab selected (draw its content) | Ends the tab **only when visible** |
| `StyleColor(ImGuiCol col, Color value)` | `StyleColorScope` | — | Pops exactly one style color |
| `StyleColor(ImGuiCol col, Color32 value)` | `StyleColorScope` | — | Pops exactly one style color |
| `StyleVar(ImGuiStyleVar var, float value)` | `StyleVarScope` | — | Pops exactly one style var |
| `StyleVar(ImGuiStyleVar var, Vector2 value)` | `StyleVarScope` | — | Pops exactly one style var |

Usage:

```csharp
using (var window = ImGuiEx.Window("MyMod"))
{
    if (window.Visible)
    {
        using (ImGuiEx.StyleColor(ImGuiCol.Text, new Color32(255, 198, 0, 255)))
        {
            DearImGuiKSP.DearImGuiKSP.Text("orange while inside this block");
        }
    }
}
```

Two rules:

- **The immediate-mode rule:** a scope must be disposed within the same
  frame/callback that created it. Never store a scope in a field or let it
  survive the callback — an unclosed window at end of frame trips ImGui's
  end-of-frame assert in a debug native build (the shipped release DLL
  compiles ImGui's asserts out). Either way the fault barrier force-closes
  any scopes a throwing callback left open, so the mistake cannot corrupt
  later consumers — but fix your code rather than relying on that.
- **Only dispose what a factory returned.** Do not `new` up scope structs
  yourself. A default `TabBarScope`/`TabItemScope` (or the plot scopes) ends
  nothing, but a default `WindowScope`/`ScrollRegionScope` ends a window or
  region you never began, and a default `StyleColorScope`/`StyleVarScope`
  pops a style entry you never pushed.

For raw Begin/End pairs (`BeginWindow`/`EndWindow`,
`BeginScrollRegion`/`EndScrollRegion`, `BeginTabBar`/`EndTabBar`,
`BeginTabItem`/`EndTabItem`) the same pairing rules apply and you must honor
them manually: `EndWindow`/`EndScrollRegion` always after their Begin, but
`EndTabBar`/`EndTabItem` only when the Begin returned true. The scopes exist
so you do not have to remember which is which — prefer them.

### Window sizing

Every window chooses one of two sizing models via the `autoResize` argument
of `ImGuiEx.Window` (or the matching parameter on the raw `BeginWindow`):

- **Default (`autoResize: false`)** — the window is user-resizable: drag the
  grip in the lower-right corner or any edge. When the content is taller or
  wider than the window, an automatic scrollbar appears so nothing is lost.
  This is the right choice for fixed-size views (plots, long lists) where the
  reader wants control.
- **Fit-to-content (`autoResize: true`)** — the window is resized to fit its
  content every frame. It grows when a collapsing section opens, shrinks when
  one closes, and reflows when the library's UI scale changes. The cost: the
  window is no longer user-resizable (the grip and edges are inactive), and
  scrollbars never appear because the window always fits. The title bar stays
  draggable in both modes.

## 4. Public types

The facade uses the Unity types you already know:

- `Vector2` = `UnityEngine.Vector2` (sizes, positions, cursor moves).
- `Color` = `UnityEngine.Color` (float RGBA — red, green, blue, alpha —
  components, 0-1 range).
- `Color32` = `UnityEngine.Color32` (byte components in the standard sRGB
  color space, 0-255 range).

Which one a member takes is deliberate and documented per member:

- `PushStyleColor(ImGuiCol, Color)` takes **0-1 floats**;
  `PushStyleColor(ImGuiCol, Color32)` takes **bytes** (normalized to 0-1
  internally). The two overloads differ only in component encoding — no
  color-space conversion happens either way. Both work; `Color32` is usually
  the convenient one.
- `Spinner(..., Color? tint, ...)` takes a **nullable `Color`** (0-1 floats).
- `ImGuiDraw` primitives and `ImGuiGradients` take **`Color32`**.
- `TextColored(Color32 color, string text)` takes **`Color32`**.

There is an implicit conversion from `Color32` to `Color` in Unity, so a
`KspPalette` constant (a `Color32`) works anywhere a `Color` is wanted.

## 5. Style push/pop and the style enums

```csharp
public static void PushStyleColor(ImGuiCol col, Color value)
public static void PushStyleColor(ImGuiCol col, Color32 value)
public static void PopStyleColor(int count = 1)
public static void PushStyleVar(ImGuiStyleVar var, float value)
public static void PushStyleVar(ImGuiStyleVar var, Vector2 value)
public static void PopStyleVar(int count = 1)
```

These override one style slot for everything drawn after the push, until the
matching pop — typically the end of your `using` block. Every push must be
paired with exactly one pop **before the end of the frame**; the
`ImGuiEx.StyleColor`/`StyleVar` scopes do the pairing for you. Use raw
push/pop only in shapes where `using` is awkward; never push inside a
conditional and pop outside it.

`ImGuiCol` is the full Dear ImGui color-slot table (`Text`, `WindowBg`,
`FrameBg`, `Button`, `CheckMark`, `PlotLines`, ... through `COUNT`) and
`ImGuiStyleVar` is the full style-variable table (`Alpha`,
`WindowRounding`, `FrameRounding`, `ItemSpacing`, ... through `COUNT`).
Whether a given `ImGuiStyleVar` takes a `float` or a `Vector2` is fixed by
ImGui (e.g. `FrameRounding` is a float, `ItemSpacing` is a Vector2);
passing the wrong type is a programming error — debug native builds assert
on it, while the shipped release DLL compiles that assert out, so get it
right rather than relying on the safety net. An out-of-range enum value
(including `COUNT`) is caught on the managed side as a logged no-op.
The members you will reach for most: `ImGuiCol.Text`, `ImGuiCol.FrameBg`,
`ImGuiCol.Button`, `ImGuiCol.CheckMark`, and `ImGuiStyleVar.FrameRounding`.

## 6. Immediate-mode IDs: labels, `##`, and the spinner `id` rule

Labels double as widget identity. Dear ImGui hashes the label string to
identify a widget, so:

- **Change a label and you get a new widget** — its state (scroll position,
  edit buffer, selection) resets. Keep labels stable across frames.
- **`"Label##id"` splits display text from identity:** the part after `##`
  is invisible in the UI but hashed. Use it for duplicate visible labels
  (`"Throttle##eng1"`, `"Throttle##eng2"`).
- **An invisible ID is `"##name"`, never `""`.** An empty string at window
  root hashes to the window's own ID and trips an ImGui assert (the library's
  own spinner bindings hit exactly this during development — an empty label
  asserted on window open). Every library widget guards this for
  you — but the rule applies to any API taking an `id`: give it a real,
  non-empty, unique-per-location string.

`Spinner` specifically: the default is a unique invisible per-type id, so
one spinner of each type per window "just works". Two spinners of the **same
type** in one window must be given distinct `id` arguments:

```csharp
DearImGuiKSP.DearImGuiKSP.Spinner(SpinnerType.Clock, 12f, 3f, id: "##clock_a");
DearImGuiKSP.DearImGuiKSP.Spinner(SpinnerType.Clock, 12f, 3f, id: "##clock_b");
```

## 7. The fault barrier

Your callback runs inside a per-consumer fault barrier
(`Application/FaultBarrier.cs`). If it throws:

- The exception is caught, logged as an error under `[DearImGuiKSP]` with
  your consumer id, and the frame continues.
- A consecutive-throw counter increments; a clean frame resets it to zero.
- At **5 consecutive throwing frames** the consumer is **auto-disabled for
  the rest of the session** (logged as an error) and its callback is no
  longer invoked.

This protects every other mod's UI (and the game) from a broken consumer.
During development, watch for the repeated error lines; in release, one
exceptional frame is survivable but five in a row means your UI is gone
until the next game restart.

## 8. settings.cfg

The library persists **only its own** global config at
`GameData/DearImGuiKSP/settings.cfg`. Consumer window state (positions,
visibility, values) belongs to consumers — persist it in your own config.

The file is a KSP `ConfigNode`:

```
DEARIMGUIKSP_SETTINGS
{
	formatVersion = 1
	uiScale = 1.0
	fontScale = 1.0
	theme = ksp
	font = IBMPlexSans
	verboseLogging = false
	enabled = true
	clampWindowsToViewport = true
	docking = true
}
```

Keys (with defaults from the library's config constants):

| Key | Type | Default | Meaning |
|---|---|---|---|
| `uiScale` | float | 1.0 | UI scale, clamped to 0.5-2.0 |
| `fontScale` | float | 1.0 | Font size scale, clamped to 0.5-2.0 |
| `theme` | string | `ksp` | Theme preset: `ksp` or `dark`; any other value falls back to `ksp` with one log line |
| `font` | string | `IBMPlexSans` | Font: `IBMPlexSans`, `ProggyClean`, or a `.ttf` filename placed in `GameData/DearImGuiKSP/Fonts/` (unknown names fall back to ProggyClean with a log line) |
| `verboseLogging` | bool | false | Extra library logging |
| `enabled` | bool | true | Master library enable switch — **file-only**: when false the library stays dormant for the whole session and the in-game settings panel never appears, so there is no in-game way to turn it back on; set it back to `true` here and restart KSP |
| `clampWindowsToViewport` | bool | true | Keep windows inside the screen on resolution changes |
| `docking` | bool | true | Window docking (ISSUES #011): allow windows to be docked together and into consumer-declared dockspaces; the player can toggle it live in the settings panel |
| `formatVersion` | int | 1 | Internal file-format marker; migrated forward on read |

These are user-facing options, not a per-mod API: your mod does not write
them. The file is read **once, at KSP startup** — it is not watched for
changes while the game runs. While running, the in-game settings panel is
the source of truth: panel edits apply in memory immediately (theme and UI
scale live; font and font scale on the next KSP start) and are written back
to settings.cfg about half a second after the changes settle, as one atomic
write. So a hand edit made mid-session takes effect only at the next KSP
start — and only if no panel change happens meanwhile, because the next
panel edit rewrites the whole file from the in-memory settings.

Next: [Widget Catalog](20-widgets.md) — every widget with real signatures
and minimal examples.
