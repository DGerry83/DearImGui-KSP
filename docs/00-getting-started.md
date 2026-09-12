# Getting Started with DearImGui-KSP

DearImGui-KSP is a shared UI library: it renders your mod's windows with
Dear ImGui (a bundled native library) styled to match KSP, instead of Unity
IMGUI. Your mod declares its UI once per frame through a C# callback; the
library handles rendering, input, and KSP integration.

Next: [API Fundamentals](10-api-fundamentals.md) -
[Widget Catalog](20-widgets.md) - [Theming](30-theming.md)

## 1. Install

The library ships as its own mod folder, in its own release zip, separate
from any mod that uses it. Players copy `GameData/DearImGuiKSP` from the
library's release zip into their KSP install's `GameData/`:

```
GameData/
  DearImGuiKSP/
    DearImGuiKSP.version        Addon Version Checker metadata
    Readme.txt
    License.txt
    Fonts/
      IBMPlexSans-Regular.ttf   Default font (IBM Plex Sans, OFL)
      OFL.txt                   Font license (the SIL Open Font License requires shipping it)
    Textures/
      toolbar.png               Toolbar icon for the settings window button
    Plugins/
      DearImGuiKSP.dll          Managed assembly - the programming interface you compile against
    PluginData/
      DearImGuiKSPNative.dll    Native DLL (Dear ImGui core + backends)
    Docs/                       This documentation set (00-70) plus CHANGELOG.md
```

`settings.cfg` (the library config) is deliberately **not** in the zip — so
installing an upgrade never resets your saved settings. The library creates
it on the first settings change; until then it runs on its defaults.

Two rules:

- `DearImGuiKSPNative.dll` must stay in `PluginData/`. KSP's assembly loader
  scans all of `GameData/` recursively for DLLs — skipping only folders named
  `PluginData` — and tries to load every DLL it finds as a managed assembly.
  A native DLL in the scan path stalls the game very early in loading, so the
  library deliberately keeps its native DLL where the loader never looks and
  loads it itself from `PluginData/`. (`Plugins/` for the managed DLL is
  convention, not a requirement of the scan.)
- The managed and native DLLs are released **in lockstep** — always together,
  as a matched pair. A version mismatch at runtime is a startup failure.
  Never mix DLLs from different releases.

**Player settings:** the library adds its own button to the KSP toolbar (in
every scene). It opens the "DearImGui-KSP Settings" window, where the theme
(ksp / dark) and the overall UI scale change immediately, verbose logging
toggles on the spot, and the font plus font scale are saved but only take
effect on the next KSP start. Each scale slider has a small type-in box
beside it for entering an exact value. The button uses `Textures/toolbar.png`,
which ships in this folder (a generated grey placeholder is the fallback if
the file is missing).

**What the UI scale covers:** it scales the library's style-driven metrics
(window and frame padding, item spacing, rounding) and font rendering. It
deliberately does **not** rescale sizes a mod passes to widgets in pixels —
spinner radius and thickness, knob or wheel size, explicit plot or button
sizes keep exactly the pixels asked for. A mod that wants those to follow the
player's scale can read the configured value and multiply them itself.

**Who installs this:** your players, from the library's own release — not
you, from your mod's download. DearImGui-KSP is a shared library: players
install this folder once, and every mod that depends on the library uses that
same single copy.

Do **not** copy `DearImGuiKSP.dll` or `DearImGuiKSPNative.dll` into your own
mod's download, and do not ask players to drop those DLLs in by hand. Declare
the dependency (next section) and point players at the library's release
instead. Bundled copies drift out of sync and then fail the startup version
check; one shared copy keeps every mod on the same, lockstep-managed version
of the managed and native pair.

## 2. Declare the dependency

Tell KSP to refuse loading your mod if the library (or a compatible major
version of it) is missing, via an assembly attribute in your mod — typically
next to your `[KSPAddon]` class or in any one of your source files:

```csharp
[assembly: KSPAssemblyDependencyEqualMajor("DearImGuiKSP", 1, 3)]
```

The current library version is **1.3.0**, so the dependency reads
"major 1, minor 3". Despite the name, `KSPAssemblyDependencyEqualMajor` pins
only the **major**: the library's major must equal yours, and its minor must
be **equal or higher** than the one you declare (the declared minor is a
minimum, not a pin) — so a mod built against 1.0 loads fine against library
1.1, but KSP refuses to load it against any 2.x. When the library ships a new
major version, bump this attribute in a matching release of your mod. Managed
and native DLLs always release together; this attribute is how your mod
tracks the library's major version.

Because of this attribute, the `IsAvailable` check below is a safety net for
edge cases (library self-disabled at startup, scene transitions), not the
normal path — if the library is entirely absent, KSP skips your mod first.

## 3. Your first window

The whole programming model in four steps:

1. **Check availability** with `DearImGuiKSP.DearImGuiKSP.IsAvailable`.
2. **Register** a callback with `Register(id, callback)`. The id must be
   unique across all mods (use your mod's name). The callback runs once per
   frame, in registration order.
3. **Declare your UI inside the callback** — widget calls are only valid
   there. Wrap your window in `ImGuiEx.Window` (an exception-safe scope).
4. **Unregister** with `Unregister(id)` when your object is destroyed.

A complete minimal consumer:

```csharp
using System;
using UnityEngine;
using DearImGuiKSP;

namespace MyMod
{
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public sealed class MyModUi : MonoBehaviour
    {
        private const string ConsumerId = "MyMod";

        private bool _registered;
        private bool _windowVisible = true;
        private float _throttle;

        private void Start()
        {
            if (!DearImGuiKSP.DearImGuiKSP.IsAvailable)
            {
                Debug.Log("[MyMod] DearImGui-KSP not available; UI disabled.");
                return;
            }
            DearImGuiKSP.DearImGuiKSP.Register(ConsumerId, OnFrame);
            _registered = true;
        }

        private void OnDestroy()
        {
            if (_registered)
            {
                DearImGuiKSP.DearImGuiKSP.Unregister(ConsumerId);
                _registered = false;
            }
        }

        // Per-frame UI declaration - the ONLY place widget calls are valid.
        private void OnFrame()
        {
            if (!_windowVisible)
            {
                return;
            }
            using (var window = ImGuiEx.Window("MyMod"))
            {
                if (window.Visible)
                {
                    DearImGuiKSP.DearImGuiKSP.Text("Hello from MyMod!");
                    DearImGuiKSP.DearImGuiKSP.SliderFloat(
                        "Throttle", ref _throttle, 0f, 1f);
                }
            }
        }
    }
}
```

Build that against `GameData/DearImGuiKSP/Plugins/DearImGuiKSP.dll` (plus the
KSP/Unity assemblies), drop your DLL in `GameData/MyMod/Plugins/`, and the
"MyMod" window appears in-game.

A few things to note before you go further:

- **All widget state lives in your fields**, not in the UI. Every frame your
  callback re-declares the whole window from current state; the library diffs
  it against last frame. There is no `OnGUI`, no retained widget tree, no
  event handlers.
- **`window.Visible` false means "collapsed/clipped this frame"** — skip the
  content, but the scope still ends the window correctly on dispose.
- **If your callback throws**, the library's fault barrier catches it, logs
  it, and only after **5 consecutive throwing frames** disables your consumer
  for the rest of the session. One stray exception will not kill your UI —
  but fix your logs.

From here: [API Fundamentals](10-api-fundamentals.md) explains the
registration model, scopes, types, and the settings file in detail.
