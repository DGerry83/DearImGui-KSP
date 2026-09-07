# Migrating from Unity IMGUI

If your mod draws windows with `OnGUI`/`GUILayout`, this page maps each IMGUI concept to its DearImGui-KSP equivalent and ports one small window end to end. The two models differ in one fundamental way: IMGUI runs your `OnGUI` for every **event** (layout, repaint, input) and you branch on `Event.current`; DearImGui-KSP calls your registered callback **once per frame**, unconditionally, and you declare the whole UI every time. No event handling, no `Repaint()` calls.

Prerequisites: [Getting Started](00-getting-started.md) for installation and dependency declaration, [API Fundamentals](10-api-fundamentals.md) for the registration/callback model.

## Concept mapping

| Unity IMGUI | DearImGui-KSP |
|---|---|
| `OnGUI()` called for every GUI event | A callback registered once with `DearImGuiKSP.DearImGuiKSP.Register(id, callback)`, invoked once per frame |
| Window rect field + `GUI.Window(id, rect, Func, title)` | `using (var w = DearImGuiKSP.ImGuiEx.Window("Title")) { if (w.Visible) { ... } }` |
| `GUILayout.Label(text)` | `DearImGuiKSP.DearImGuiKSP.Text(text)` |
| `GUILayout.Button(label)` | `DearImGuiKSP.DearImGuiKSP.Button(label)` — same "true on the click frame" return |
| `GUILayout.TextField(value)` / `GUI.TextField` | `DearImGuiKSP.DearImGuiKSP.InputText(label, ref value, capacity = 256)` |
| `GUILayout.HorizontalSlider(value, min, max)` | `DearImGuiKSP.DearImGuiKSP.SliderFloat(label, ref value, min, max)` |
| `GUIStyle` / `GUISkin` | The global theme (`ksp` or `dark`, set in the library's `settings.cfg`) + per-frame `PushStyleColor`/`PushStyleVar` or the `ImGuiEx.StyleColor`/`StyleVar` scopes — see [Theming](30-theming.md) |
| `GUILayout.BeginHorizontal` / `BeginVertical` | Layout is **vertical by default**; see "Layout" below — there is an honest gap here |
| `Event.current`, `Input.GetMouseButton`, hotControl juggling | Nothing. The library captures input and blocks click-through automatically — see "Input" below |
| Persistent window rect you save/load | Nothing to save: window positions are ImGui-managed for the session (drag by the title bar); note they are **not** persisted across sessions — the library disables the imgui.ini, so re-anchoring on next launch is consumer state if you need it |

## Side-by-side: one window ported

Before — Unity IMGUI:

```csharp
public class MyMod : MonoBehaviour
{
    private Rect _windowRect = new Rect(80f, 80f, 260f, 160f);
    private int _clickCount;
    private float _throttle;
    private string _vesselName = "untitled";

    private void OnGUI()
    {
        _windowRect = GUI.Window(
            GUIUtility.GetControlID(FocusType.Passive),
            _windowRect,
            DrawWindow,
            "My Mod");
    }

    private void DrawWindow(int id)
    {
        GUILayout.Label("Button clicked " + _clickCount + " time(s).");
        if (GUILayout.Button("Click me"))
        {
            _clickCount++;
        }
        GUILayout.Label("Throttle");
        _throttle = GUILayout.HorizontalSlider(_throttle, 0f, 1f);
        _vesselName = GUILayout.TextField(_vesselName);
        GUI.DragWindow();
    }
}
```

After — DearImGui-KSP:

```csharp
public class MyMod : MonoBehaviour
{
    private const string ConsumerId = "MyMod";
    private int _clickCount;
    private float _throttle;
    private string _vesselName = "untitled";

    private void Start()
    {
        if (!DearImGuiKSP.DearImGuiKSP.IsAvailable)
        {
            Debug.Log("[MyMod] DearImGui-KSP not available; staying on IMGUI.");
            return;
        }
        DearImGuiKSP.DearImGuiKSP.Register(ConsumerId, OnFrame);
    }

    private void OnDestroy()
    {
        DearImGuiKSP.DearImGuiKSP.Unregister(ConsumerId);
    }

    private void OnFrame()
    {
        using (var window = DearImGuiKSP.ImGuiEx.Window("My Mod"))
        {
            if (!window.Visible)
            {
                return;
            }
            DearImGuiKSP.DearImGuiKSP.Text("Button clicked " + _clickCount + " time(s).");
            if (DearImGuiKSP.DearImGuiKSP.Button("Click me"))
            {
                _clickCount++;
            }
            DearImGuiKSP.DearImGuiKSP.SliderFloat("Throttle", ref _throttle, 0f, 1f);
            DearImGuiKSP.DearImGuiKSP.InputText("Vessel name", ref _vesselName);
        }
    }
}
```

What changed and why:

- **The window rect is gone.** `ImGuiEx.Window` opens (or continues) a window by name; position and size are ImGui-managed for the session and the user drags the window by its title bar. By default the library also clamps windows to the viewport on resolution changes (`clampWindowsToViewport` in `GameData/DearImGuiKSP/settings.cfg`). One caveat: the library sets ImGui's ini filename to null, so window positions are **not** saved across sessions — if your mod must reopen where the player left it, capture the position yourself (or accept the default placement). Delete your rect save/load code either way.
- **State stays in fields; the callback is pure declaration.** Same fields, same widgets, same return-value-on-click-frame pattern. Everything is drawn every frame; visibility and clicks come back from the calls.
- **`GUI.Window`'s id** is replaced by the window title, which doubles as the ImGui identity. Keep the title stable.
- **No `GUI.DragWindow`** — dragging is built into the title bar.

If the library is unavailable, `Register` logs a warning and ignores the call; the `IsAvailable` check above lets you fall back to your old IMGUI path (or just do nothing) — which is also the recommended pattern for releasing a mod that supports both UI stacks during a transition.

## Layout: vertical-first, with a known gap

Widgets stack vertically by default — each call places its item below the previous one. There is **no public horizontal-layout helper yet**: `SameLine` exists only as an internal implementation detail (used by `InputText`), and a public layout-helper surface (SameLine et al.) may come in a later release. Until it lands:

- Put each logically-grouped control on its own line — the immediate-mode style reads fine that way, and it is what the demo does (its widget showcase stacks one labelled control per line).
- Use `DearImGuiKSP.DearImGuiKSP.SetCursorY(float y)` to add vertical space, and `DearImGuiKSP.DearImGuiKSP.Dummy(width, height)` where you need an explicit invisible spacer that grows the content bounds that grows the content bounds (required after a `SetCursorY` that extends a scroll region's range — ImGui asserts on a bare cursor move that grows parent boundaries). The manual-list-virtualization pattern built on these two calls (`GetScrollY` -> visible row range -> `SetCursorY` -> draw visible rows -> `SetCursorY(rowCount * rowHeight)` + `Dummy`) is documented on `BeginScrollRegion` in [API Fundamentals](10-api-fundamentals.md).

Do not try to fake columns with spaces in labels; wait for the layout helpers or stack vertically.

## Input: delete your event handling

Nothing to port. While a DearImGui-KSP window captures the mouse (hover) the library automatically locks camera/click-through controls and suppresses both Unity UI stacks beneath it — an invisible uGUI raycast blocker absorbs clicks, and an early-ordered OnGUI component grabs IMGUI hotControl so IMGUI windows below receive no input. An active text input additionally locks keyboard controls. Both mechanisms are inert when not capturing, so IMGUI mods are unaffected the rest of the time. You get all of this for free — there is no consumer-facing API for it. One known edge case: an IMGUI drag started outside an ImGui window keeps its hotControl until the mouse is released.

One text-editing limitation to know before you promise parity (recorded scope note): the library feeds navigation/edit keys and Ctrl only — no Shift/Alt modifiers. Ctrl+A works in input fields; Shift+Arrow and Shift+Home/End selection do not.

## Styling

`GUIStyle` overrides become theme plus style pushes. The global look (KSP-themed `ksp` or stock `dark`) is a library setting in `GameData/DearImGuiKSP/settings.cfg`, not code. Per-widget or per-section overrides use the style stack, preferably through the exception-safe scopes:

```csharp
using (DearImGuiKSP.ImGuiEx.StyleColor(DearImGuiKSP.ImGuiCol.Text, Color.red))
{
    DearImGuiKSP.DearImGuiKSP.Text("This line renders red.");
}
```

Colored accents without a scope: `DearImGuiKSP.DearImGuiKSP.TextColored(color, text)`, with public palette constants in `KspPalette` (e.g. `KspPalette.GreenLight`). See [Theming](30-theming.md) for the full style surface and the palette table.

## Checklist for a full port

1. Delete `OnGUI` window drawing; add one `Register` in `Start`, one `Unregister` in `OnDestroy`.
2. Convert each `GUILayout.*` call per the mapping table; keep state in fields, `ref`-pass values into widgets.
3. Delete rect bookkeeping, `GUI.DragWindow`, event branching, and input/hotControl code.
4. Replace `GUIStyle` tweaks with the theme + style pushes; use `TextColored` for accents.
5. Stack horizontally-grouped controls vertically for now (no public SameLine yet).
6. Gate the whole thing on `IsAvailable` with your chosen fallback.

Worked examples at full scale: `DearImGuiKSPDemo/DemoConsumer.cs` (registration, multiple windows from one callback, toolbar visibility toggle) and the demo's benchmark window, which deliberately keeps an IMGUI reference implementation (`BenchmarkUI.OnGUIReference`) for side-by-side comparison.

## Next

- [Troubleshooting](70-troubleshooting.md) — when the ported window "does nothing".
- [Plotting](40-plotting.md) and [Animation](50-animation.md) — capabilities IMGUI never gave you.
- Back to [API Fundamentals](10-api-fundamentals.md).
