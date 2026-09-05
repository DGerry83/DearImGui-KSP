# Troubleshooting

Symptom-first fixes for the failure modes a modder or player is likely to hit. For API usage questions, start with [API Fundamentals](10-api-fundamentals.md); for install and dependency declaration, [Getting Started](00-getting-started.md).

## Reading the log

Every library log line is written to `KSP.log` (KSP's standard log at the install root) with the `[DearImGuiKSP]` prefix. Warn/error/info lines are always on; additional `[DearImGuiKSP] [debug] ...` diagnostics require `verboseLogging = true` in `GameData/DearImGuiKSP/settings.cfg`. Players can also flip that switch live in the "DearImGui-KSP Settings" window (the library's own toolbar button). When reporting a bug, attach the log with verbose logging on.

## "DearImGui-KSP — Startup Failed" popup at the main menu

The library hit an unrecoverable startup failure. This is **session-permanent by design**: the library self-disables, shows exactly one plain-language popup, and puts the technical detail in the log. Restarting KSP retries from scratch; there is no in-session recovery. The popup body tells you which of the four failure kinds it was, and the matching log line just above it has the specifics:

| Popup body says | Failure kind | Log shows | Fix |
|---|---|---|---|
| "native component is missing or corrupt" | `NativeComponent` | LoadLibrary/Win32 errors, missing exports, context/texture init codes | Reinstall the library package intact: `DearImGuiKSP/Plugins/DearImGuiKSP.dll` and `DearImGuiKSP/PluginData/DearImGuiKSPNative.dll` must both be present |
| "components are from different versions" | `VersionMismatch` | handshake line (below) | Reinstall **both** DLLs from one release — see next section |
| "this graphics API is not supported" | `GraphicsApi` | the detected `GraphicsDeviceType` | The library currently requires D3D11. OpenGL support is not shipped yet; it may come in a later release |
| (same as native component) | `RenderHook` | "GetRenderEventFunc returned a null pointer" | Treat as native component: reinstall; report if it persists |

Your mod is unaffected code-wise: `DearImGuiKSP.IsAvailable` is false, your `Register` call is ignored with a warning, and any IMGUI fallback you built (see [Migration](60-migration-from-imgui.md)) takes over.

## Version mismatch between the DLLs

Managed and native DLLs always release together in lockstep — never mix DLLs
from different releases. A startup version handshake enforces this
(currently expected version **6** on the managed side). A stale or partially
copied native DLL fails the handshake before anything else runs, with this
log line:

```
[DearImGuiKSP] Native/managed handshake mismatch: expected version 6, DearImGuiKSPNative reported <n>.
```

Fix: copy both DLLs from the same release zip. `DearImGuiKSPNative.dll` belongs in `GameData/DearImGuiKSP/PluginData/` (not next to the managed DLL); a missing native DLL is the `NativeComponent` failure instead. Consumers: declaring `KSPAssemblyDependencyEqualMajor("DearImGuiKSP", x, y)` keeps KSP's loader from mixing a different managed major with your build (see [Getting Started](00-getting-started.md)).

## Font fallback

If the configured font cannot be found or read at startup, the library falls back to the embedded default font (ProggyClean) — a degraded look, not a failure — and logs exactly one line:

```
[DearImGuiKSP] Font '<name>' not found or unreadable; using embedded default font.
```

Fix: place the TTF under `GameData/DearImGuiKSP/Fonts/` and set `font = <filename>` (or `font = IBMPlexSans`, the shipped default; `font = ProggyClean` selects the embedded font deliberately) in `settings.cfg`. Related: an unknown `theme` value falls back to `ksp` with `Unknown theme '<name>'; using 'ksp'.` — valid theme names are `ksp` and `dark`.

## "My widget calls do nothing"

Three causes, in order of likelihood:

1. **The calls run outside a registered callback.** Widget calls are only valid inside the `Action` you passed to `DearImGuiKSP.Register`; from anywhere else they silently no-op (never throw). If your drawing code is invoked from `Update`, a timer, or an event handler, move it into the registered callback.
2. **The library is not available.** When `IsAvailable` is false (startup failure above, or the game is still loading), every call no-ops. Check `IsAvailable` before registering and don't draw from elsewhere.
3. **The UI is suspended.** During F2-hide and loading screens the frame loop stops — no callbacks run, nothing renders. This is a pause, not a failure; callbacks resume automatically. Keep your state in fields so the next callback reconstructs the window.

Also check your scope guard: content after `if (!window.Visible) return;` is skipped on frames the window is collapsed or clipped — that is normal, not a malfunction.

## "Register ignored" warnings

```
[DearImGuiKSP] Register('<id>') ignored: DearImGui-KSP is not available.
[DearImGuiKSP] Register('<id>') ignored: id already registered.
```

The first is cause 1/2 above. The second means a consumer with that id is already registered — ids must be unique per session. Typical cause: registering from both `Start` and a scene-change path without unregistering, or two instances of your addon. Guard with `Unregister` in `OnDestroy` before re-registering, or make registration idempotent. `Unregister` on an unknown id simply returns false.

## "Consumer '<id>' threw an exception ... auto-disabled"

Your callback threw. The fault barrier catches it, logs it, and skips only your consumer for that frame; after **5 consecutive throwing frames** your consumer is auto-disabled for the session (other consumers are unaffected). The log line names your consumer id and carries the exception. Note the useful property: because `using` scopes dispose during exception unwinding, an exception thrown inside a window/style scope still leaves the ImGui stack symmetric — you will not corrupt other consumers, just get yourself disabled. Fix the exception, restart the game.

## Empty-ID assert: "Cannot have an empty ID at the root of a window"

Any ImGui item needs a non-empty ID; at window root an empty label resolves to the window's own ID and trips an ItemAdd assert. The library's own spinner bindings hit this during development and now ship per-type invisible default IDs — the lesson generalizes to you:

- "Invisible ID" means a `##` prefix (`"##my_section"`), never an empty string.
- Widget labels double as IDs; rename a label and you get a new identity (lost state such as scroll positions is expected).
- Place two spinners of the **same type** in one window and you must pass distinct `id` values to `Spinner(...)`, which exists for exactly this.

Build nuance: a **debug** native build (`build.bat`) shows the assert dialog when this fires; the shipped **release** DLL (`build_release.bat`, `/DNDEBUG`) compiles asserts out, so the same bug degrades silently into wrong identity/behavior instead of a clear stop. Develop against the debug build so you see asserts; never treat release silence as correctness.

## The 16-bit index limit

Dear ImGui indexes draw-list vertices with a 16-bit type, so **one draw list may hold at most 65,535 vertices**; exceeding it asserts (debug builds) or renders corrupted geometry. There is no escape hatch in this library — 32-bit indices (`RendererHasVtxOffset`) were evaluated and deliberately deferred until a demonstrated need.

For context: a 10,000-point line is about 20k vertices — far under the limit. What approaches it is one window/region stuffed with very dense content. Remedies, in order of preference:

- **Plot a rolling window instead of full history** (see [Plotting](40-plotting.md)) — fewer points, constant cost, and it keeps you far from the limit.
- **Split dense content across windows or scroll regions** — the limit is per draw list, and separate windows/regions get their own.
- Downsample deliberately (e.g. plot every Nth sample) rather than rendering data no one can read at that density anyway.

## Performance

- **Plots cost per point.** Per-frame plot cost scales with submitted point count; the demo's full-buffer telemetry graphs grew to ~7 ms/frame late in flight before the rolling-window fix (1200 samples, ~20 s at 60 Hz, plotted per cell). Keep your plotted windows bounded; auto-fit is cheap at those sizes.
- **Hover readouts allocate on hover.** A `string.Format` readout under a hovered plot is the demo's one sanctioned per-frame allocation — user-driven, bounded to one hovered cell. Do not format strings on the unhovered path; pass label literals.
- **The benchmark window is the regression instrument.** The demo mod ships a naive-vs-virtualized 1000-item benchmark window (`BenchmarkUI`) used as the standing performance gate. If you suspect your UI is slow, reproduce with it and compare; the library targets no measurable FPS impact with a few typical windows and under 1 ms managed frame cost.
- **Window count and scopes.** A few windows from one registered callback is the intended pattern (the demo draws three from one registration). Scopes are structs — `using` them costs nothing; do not pool or cache them across frames.

## Known limitations (honest list)

- **Intermittent whole-UI flicker** (known open issue): rarely the entire library UI flickers or disappears for a frame or two — reported worse in flight and especially under time warp; no reliable repro yet. Under active investigation; when it is understood this list will be updated.
- **Spinner tints are partially upstream-inherent**: `SpinnerType.RainbowMix` derives its hue from the tint's saturation — with the default white tint it renders grey and never cycles; pass a saturated `Color` as the tint to get the rainbow. `SpinnerType.Atom` hardcodes its electron dots to red/green/blue; the tint colors only the ellipses. Both are vendor behavior, not binding bugs; spinner rendering is under review before release.
- **No OpenGL**: D3D11 only; OpenGL support may come in a later release.
- **Text input modifiers**: navigation/edit keys and Ctrl only — Ctrl+A works; Shift+Arrow / Shift+Home/End selection does not.
- **No public horizontal layout helper** (SameLine et al.): layout is vertical-first for now; a public layout surface is a possible later addition. Use `SetCursorY`/`Dummy` for spacing (see [Migration](60-migration-from-imgui.md)).
- **Library-owned settings only**: the library persists its own `settings.cfg` (and deliberately writes no imgui.ini); per-consumer window positions and state are yours to keep.

## Next

- [Plotting](40-plotting.md), [Animation](50-animation.md), [Migration](60-migration-from-imgui.md) for the feature docs.
- [Getting Started](00-getting-started.md) for install/dependency basics.
