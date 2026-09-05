# Getting Started with DearImGui-KSP

> Authored in milestone M7 of the pre-release feature wave (spec §8.2, D31).
> Audience: a KSP modder writing their first DearImGui-KSP window. No prior
> Dear ImGui experience is assumed.

DearImGui-KSP is a shared UI library: it renders your mod's windows with
Dear ImGui (vendored, native) styled to match KSP, instead of Unity IMGUI.
Your mod declares its UI once per frame through a C# callback; the library
handles rendering, input, and KSP integration.

Next: [API Fundamentals](10-api-fundamentals.md) -
[Widget Catalog](20-widgets.md) - [Theming](30-theming.md)

## 1. Install

The library ships as its own mod folder. Copy `GameData/DearImGuiKSP` from the
release zip into your KSP install's `GameData/`:

```
GameData/
  DearImGuiKSP/
    DearImGuiKSP.version        Addon Version Checker metadata
    Readme.txt
    License.txt
    Fonts/
      IBMPlexSans-Regular.ttf   Default font (IBM Plex Sans, OFL)
      IBMPlexSans-Medium.ttf
      OFL.txt                   Font license (required by the OFL)
    Plugins/
      DearImGuiKSP.dll          Managed assembly - the API you compile against
    PluginData/
      DearImGuiKSPNative.dll    Native DLL (Dear ImGui core + backends)
    settings.cfg                Library config (created/read at runtime)
```

Two rules:

- `DearImGuiKSPNative.dll` must stay in `PluginData/`. KSP only loads
  assemblies from `Plugins/`, and the library deliberately keeps the native
  DLL out of that folder so KSP never tries to load it directly; the library
  loads it itself from `PluginData/`.
- The managed and native DLLs are released **in lockstep** (design decision
  D17). A version mismatch at runtime is a startup failure. Never mix DLLs
  from different releases.

Your players install this folder once; many mods can depend on the same copy.

## 2. Declare the dependency

Tell KSP to refuse loading your mod if the library (or a compatible major
version of it) is missing, via an assembly attribute in your mod — typically
next to your `[KSPAddon]` class or in any one of your source files:

```csharp
[assembly: KSPAssemblyDependencyEqualMajor("DearImGuiKSP", 0, 1)]
```

The current library version is **0.1.0.0**, so the dependency reads
"major 0, minor 1". `KSPAssemblyDependencyEqualMajor` pins the major and minor:
KSP will not load your mod against an incompatible major. When the library
ships a new major version, bump this attribute in a matching release of your
mod (see the versioning rule, D17: managed and native DLLs always release
together, and consumers are expected to track the major version this way).

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
