# Implementation Plan: DearImGui-KSP Pre-Release Feature Wave

Session: `notes/active/2026-09-03_NewProject_PreReleaseFeatureWave/`
Design spec: `notes/finished/2026-09-03_DesignSpec_Theming_Extensions_Showcase/DESIGN_SPEC.md` (confirmed 2026-09-03; D24–D34)
Extends — never replaces — the base plan: `notes/finished/2026-07-29_NewProject_DearImGuiKSP/IMPLEMENTATION_PLAN.md` and the verified M1–M6 codebase.

---

## 1. Overview

### 1.1 Project Name
DearImGui-KSP pre-release feature wave — theming, extensions, showcase, documentation.

### 1.2 Description
Make DearImGui-KSP look like it belongs in KSP by default (KSP theme + IBM Plex Sans), add the widget depth a real mod UI needs (ImPlot, knobs, wheels, toggles, spinners, gradients, tweens), prove it with an in-flight telemetry showcase in the demo mod, and ship a docs set that takes a modder from zero to a themed, plotting window without reading demo source. No new assemblies — everything lands in the existing `DearImGuiKSP.dll`, `DearImGuiKSPNative.dll`, and `DearImGuiKSPDemo.dll`.

### 1.3 Target Platform & Runtime
- OS: Windows 10+ (Windows-first; Linux/Mac out of scope)
- Runtime: KSP 1.12.x, Unity 2019.4.18f1, Mono x64, .NET Framework 4.8 (managed), C++ via `cl.exe` (native)
- Distribution: KSP mod (GameData folders; release zip gains `Fonts/` + `docs/`)
- Graphics: D3D11 only this wave (OpenGL decision point at M8, D33)

### 1.4 Language & Dependencies
- Primary language: C# (managed), C++ (native core)
- Core framework/engine: Unity 2019.4.18f1 (game host), KSP API, KSPBuildTools 1.1.1 (build/deploy)
- External libraries (new this wave, all vendored under `DearImGuiKSPNative/vendor/` with pin record):
  - ImPlot v1.0 tag (MIT) + cimplot regenerated against pinned cimgui — 2D plotting
  - imgui-knobs (MIT, pinned commit) — knob widgets
  - imgui-wheels (MIT, pinned commit) — wheel widgets
  - imspinner (MIT) + cimspinner regenerated — spinner widgets
  - imgui_toggle (0BSD, pinned commit) — animated toggles
  - IBM Plex Sans Regular + Medium static TTFs (OFL) — bundled fonts
- Existing pins (unchanged): imgui 1.92.9 via sibling cimgui clone, compiled in directly (not vendored)
- Forbidden/restricted: ImGui docking branch (D26), implot3d (D27), HImGuiAnimation (D27 — rejected, real bugs), 32-bit indices / VtxOffset (D32), FreeType, any new assembly or consumer-facing font API (§3.4)

### 1.5 Scope
- **In scope**: KSP theme as default (D25) with "dark" retained; bundled Plex Sans + `font` setting + handshake v5 (D29); API ergonomics — Vector2/Color/Color32 signatures (D24), IDisposable scope wrappers, style push/pop; ImPlot + knobs + wheels + spinners + imgui_toggle (D27); C# tween engine; gradient helpers via existing ShadeVerts export (D28); telemetry showcase in the demo mod (D30); docs set in `docs/` (D31); release packaging after all of the above (D33).
- **Out of scope**: docking branch, implot3d, OpenGL backend, CKAN, Linux/Mac, glowing radios, stock-grade Δv, maneuver-node editing, consumer font-loading API, migrating any existing mod, 32-bit index support.
- **MVP**: theme + fonts + ergonomics + ImPlot + telemetry graph panel + core docs (getting-started, API fundamentals, theming) — milestones M1–M4 plus the graph half of M6 and the core half of M7.
- **Deferred**: glowing radio buttons; implot3d; docking; OpenGL (post-wave decision); consumer font loading; window position-persistence helper; 32-bit indices; CKAN; Linux/Mac.

---

## 2. State Model (Step 1 Output)

### Entity: ThemePreset
- Identity: name ("ksp" / "dark")
- Lifetime: transient (static value tables in code); only the name persists in `settings.cfg`
- Attributes:
  - colors: per-`ImGuiCol_*` value table (`Color32`-backed)
  - frameRounding / windowRounding / grabRounding: float
  - framePadding / windowPadding / itemSpacing: `Vector2`
  - windowBorderSize / frameBorderSize: float
  - buttonGradientTop / buttonGradientBottom: `Color32`
  - windowBgGradientTop / windowBgGradientBottom: `Color32`
- Relationships: applied to global ImGui style by ThemeEngine; selected by SettingsModel's `Theme`

### Entity: LibrarySettings (existing, extended)
- Identity: singleton record
- Lifetime: persistent (`settings.cfg` via ISettingsStore)
- Attributes:
  - Font: string, default `"IBMPlexSans"` (new)
  - Theme: string, default changes `"dark"` → `"ksp"`
  - UiScale, FontScale, VerboseLogging, Enabled, ClampWindowsToViewport (unchanged)
- Relationships: read by ThemeEngine, FontResolver, frame loop

### Entity: FontRequest
- Identity: resolved font name
- Lifetime: transient (startup only)
- Attributes:
  - path: absolute TTF path under `GameData/DearImGuiKSP/Fonts/`
  - sizePixels: float (base size × fontScale)
  - weight: Regular / Medium
- Relationships: produced by FontResolver; consumed by NativeBridge.LoadFontFromFile

### Entity: Tween
- Identity: handle id (int, monotonic)
- Lifetime: transient (removed on completion or cancel)
- Attributes:
  - setter: `Action<float>`
  - from / to / durationSeconds / elapsed: float
  - ease: Ease
  - playing: bool
- Relationships: owned by TweenEngine; ticked from FrameLoopOrchestrator

### Entity: TweenHandle (public)
- Identity: handle id
- Lifetime: transient
- Attributes: Cancel(), IsPlaying (query against TweenEngine)

### Entity: TelemetrySample / RingBuffer
- Identity: implicit (ring index)
- Lifetime: transient, rolling, fixed capacity 10,000 points per channel
- Attributes: values: preallocated `float[]`; head index; count
- Relationships: written by TelemetrySampler; read by GraphPanel as `ReadOnlySpan<float>`

### Entity: StageSnapshot
- Identity: stage index
- Lifetime: transient (recomputed on staging events / once per second)
- Attributes: approxDeltaV, isp, burnTimeSeconds, propellant fractions (per stage)
- Relationships: produced by StageAnalyzer from vessel parts/modules; rendered by StagePanel

### Entity: OrbitSnapshot
- Identity: active vessel
- Lifetime: transient (per frame)
- Attributes: apoapsis, periapsis, eccentricity, inclination, semiMajorAxis, trueAnomaly, maneuver node list (display-only)
- Relationships: read from `vessel.orbit`; rendered by OrbitPanel

### Entity: VendorPinRecord
- Identity: extension name
- Lifetime: persistent (in-repo `vendor/PIN_RECORD.md`)
- Attributes: repo URL, tag/commit, pin date, generator commit (cimplot/cimspinner)

### Value Objects / Enums
- `Ease` — Linear, QuadIn, QuadOut, QuadInOut, CubicIn, CubicOut, CubicInOut (pure functions, unit-tested)
- `SpinnerType` — ~15 curated imspinner variants
- `ThemeName` — string-backed "ksp" / "dark"; unknown → "ksp" + log
- `KspPalette` — static readonly `Color32` constants per spec §6.1 (tunable; revised after in-game visual pass, spec §13)
- ImPlot flag subsets — axis/location/line-style enums hand-verified against `implot.h` (same discipline as existing ImGui flag enums in `ImGuiNative.cs`)
- `KnobVariant`, wheel-mode enum — the small public surface of each extension

---

## 3. Component Responsibilities (Step 2 Output)

### Component: ContextHost (extend)
- Responsibility: Owns the ImGui and ImPlot contexts and their frame lifecycle, including loading a font from file before the first NewFrame.
- Layer: Native Core
- Type: System
- Collaborators: BackendD3D11, DearImGuiKSPNative.cpp exports
- State access: write — native font atlas, contexts

### Component: ExtensionShims (new translation units)
- Responsibility: Expose imgui-knobs, imgui-wheels, and imgui_toggle as flat C-callable functions.
- Layer: Native Core
- Type: Adapter
- Collaborators: vendored extension sources
- State access: none

### Component: ImGuiNative / ImGuiInternal (extend)
- Responsibility: Marshal new cimgui (style/draw-list/radio), cimplot, cimspinner, and shim externs into safe internal C# calls with UTF-8 handling.
- Layer: Interop (managed)
- Type: Adapter
- Collaborators: native exports
- State access: none

### Component: NativeBridge (extend)
- Responsibility: Carry the new `LoadFontFromFile` delegate and enforce handshake version 5.
- Layer: Infrastructure
- Type: Adapter
- Collaborators: INativeBridge, ContextHost
- State access: none

### Component: FontResolver (new)
- Responsibility: Resolve the `font` setting to a TTF path under `Fonts/`, or signal ProggyClean fallback.
- Layer: Infrastructure
- Type: Service
- Collaborators: LibraryConfig, SettingsModel, KSPUtil path roots
- State access: read — LibrarySettings.Font

### Component: ThemeEngine (new)
- Responsibility: Apply a named ThemePreset value table to the global ImGui style and hold gradient parameters for the frame.
- Layer: Application
- Type: Service (Strategy)
- Collaborators: ThemePresets, ImGuiInternal (style push), SettingsModel.Changed
- State access: write — global ImGui style (via bindings)

### Component: TweenEngine (new)
- Responsibility: Advance live tweens by frame delta time and invoke their setters.
- Layer: Application
- Type: System
- Collaborators: FrameLoopOrchestrator (tick source), Ease
- State access: write — Tween entities

### Component: ImGuiEx (new, public)
- Responsibility: Guarantee Begin/End symmetry via IDisposable scope wrappers that close the stack even on exception.
- Layer: Application (public API)
- Type: RAII guards
- Collaborators: DearImGuiKSP facade, ImGuiInternal
- State access: none

### Component: ImGuiPlot / ImGuiWidgets / ImGuiGradients (new, public)
- Responsibility: Present plots, knobs, wheels, toggles, spinners, radio buttons, and gradient helpers as idiomatic public C# methods.
- Layer: Application (public API)
- Type: Facade extensions
- Collaborators: ImGuiInternal
- State access: none

### Component: SettingsModel / SettingsStore (extend)
- Responsibility: Persist and normalize the `font` key and the changed `theme` default.
- Layer: Application / Infrastructure
- Type: Service / Repository
- Collaborators: ISettingsStore, ConfigNode
- State access: write — LibrarySettings

### Component: TelemetrySampler (new, demo)
- Responsibility: Read stock flight state into fixed-capacity ring buffers once per frame.
- Layer: Demo (consumer)
- Type: System
- Collaborators: FlightGlobals, FlightInputHandler, RingBuffer
- State access: write — ring buffers

### Component: StageAnalyzer (new, demo)
- Responsibility: Compute approximate per-stage Δv, Isp, and burn time from vessel parts and engine modules.
- Layer: Demo (consumer)
- Type: Service
- Collaborators: vessel parts/modules
- State access: read — vessel state

### Component: GraphPanel / StagePanel / OrbitPanel (new, demo)
- Responsibility: Render the Graphs, Stages, and Orbit showcase tabs from sampled data using only the public API.
- Layer: Demo (consumer)
- Type: Presenter
- Collaborators: RingBuffer/StageSnapshot/OrbitSnapshot, DearImGuiKSP public API
- State access: read — demo state

### Component: ThemeDemo (new, demo)
- Responsibility: Demonstrate toggles, knobs, wheels, spinners, and gradient buttons inside the existing demo window.
- Layer: Demo (consumer)
- Type: Presenter
- Collaborators: DearImGuiKSP public API
- State access: none

---

## 4. Data Flow Diagram (Step 3 Output)

```
[settings.cfg] --(theme/font strings)--> SettingsStore --(LibrarySettings)--> SettingsModel
   |                                                |
   |                                                +--(font name)--> FontResolver --(TTF path, size)--> NativeBridge.LoadFontFromFile
   |                                                |                                                      |
   |                                                |                                                      +--> [native atlas; failure → AddFontDefault + log]
   |                                                |
   |                                                +--(theme name)--> ThemeEngine --(style colors/vars)--> [global ImGui style]
   |
[FrameLoopOrchestrator] --(deltaTime)--> TweenEngine --(eased value)--> consumer setter --> [widget state; paused on suspension]
   |
   +--(frame)--> TelemetrySampler --(altitude/q/throttle/G floats)--> RingBuffer --(ReadOnlySpan<float>, pinned)--> ImGuiPlot.PlotLine --> [cimplot → draw list → screen]
   |
   +--(staging event / 1 s)--> StageAnalyzer --(StageSnapshot)--> StagePanel --> [stacked bars, meters, "approx dV" tooltips → screen]
   |
   +--(vessel.orbit)--> OrbitSnapshot --> OrbitPanel --(ImDrawList primitives)--> [radar ellipse, vessel/Ap/Pe/target/node markers → screen]

[consumer callback] --(using ImGuiEx.Window(...))--> scope wrapper --(Dispose: End on exit or exception)--> [stack closed inside FaultBarrier]
```

Splitting arrows: frame-loop fan-out (tween/telemetry/panels) uses direct calls — all consumers live in one frame path; no event bus warranted.

---

## 5. Interface Definitions (Step 4 Output)

### Interface: INativeBridge (existing, extended)
- Defined in layer: Application/Interfaces
- Implemented by: NativeBridge (Infrastructure)
- Consumed by: startup wiring (DearImGuiKSPAddon/Composition), FrameLoopOrchestrator
- Methods (new only):
  - `LoadFontFromFile(string utf8Path, float sizePixels) -> bool`: loads a TTF into the native atlas before first NewFrame; false → fallback to embedded default + log
  - Version expectation bumps 4 → 5 (mismatch → existing version-mismatch failure path)

### Interface: ISettingsStore (existing, unchanged shape)
- Defined in layer: Application/Interfaces
- Implemented by: SettingsStore (Infrastructure)
- Consumed by: SettingsModel
- Methods: unchanged; the `font` key travels through the existing string get/set plumbing. `theme` default change is a constants change (`LibraryConfig.DefaultTheme`), not a schema change — `SettingsFormatVersion` stays 1, with `NormalizeTheme` mapping unknown/absent values to "ksp".

### Interface: INativeWidgetBindings — not introduced
- Not applicable: new widget externs follow the existing internal DllImport pattern in `ImGuiNative.cs` (same as the current 11 bindings). Creating an interface seam for them would be an abstraction with a single consumer (YAGNI); testability is unaffected because bindings are pure marshalling, and all wave logic (theme tables, tween math) sits above them and is unit-tested directly.

### Boundary: Demo ↔ library (existing public API)
- The showcase consumes the public facade and the new public wrappers only — never internals (spec §4.1). Verified at M6 by grep: no `internal` access from DearImGuiKSPDemo.

---

## 6. Pattern Selection (Step 5 Output)

| Problem / Concern | Selected Pattern | Justification |
|---|---|---|
| Named visual presets over a global style | Strategy (theme presets) | "ksp"/"dark" are interchangeable value tables behind one apply operation (D34); "Use When" met: multiple algorithms, one context |
| Begin/End stack discipline must survive exceptions | RAII scope guards (`IDisposable`) | Immediate-mode pairs map directly to `using`; Dispose guarantees symmetry inside the fault barrier |
| Vendored third-party widgets | Adapter (thin C ABI shims + C# wrappers) | Upstream churn absorbed at the vendor pin, not consumer code |
| Time-based value animation | Tween engine (update-loop driven) | Pure Application-layer math; no native interop; fully unit-testable (D27) |
| Rolling telemetry history, zero allocation | Ring buffer (fixed capacity) | Steady-state writes are array slots; reads are spans pinned inside the wrapper |
| Gradient buttons/window backgrounds | None (direct calls to existing cimgui exports, D28) | `igShadeVertsLinearColorGradientKeepAlpha` already exported; a texture pipeline would be needless machinery |
| Font/theme degradation | Graceful fallback + single log line | Spec §5.2/§5.6: never triggers the failure path |

---

## 7. Project Structure (Step 6 Output)

New and changed files only; all M1–M6 files stand.

```
DearImGuiKSP/
├── Application/
│   ├── Theming/
│   │   ├── ThemeEngine.cs
│   │   ├── ThemePresets.cs
│   │   └── KspPalette.cs
│   ├── Animation/
│   │   ├── Ease.cs
│   │   ├── Tween.cs
│   │   └── TweenEngine.cs
│   ├── Api/
│   │   ├── ImGuiEx.cs
│   │   ├── ImGuiPlot.cs
│   │   ├── ImGuiWidgets.cs
│   │   └── ImGuiGradients.cs
│   ├── SettingsModel.cs              # + font key, theme default, real NormalizeTheme
│   └── Interfaces/INativeBridge.cs   # + LoadFontFromFile
├── Interop/
│   ├── ImGuiNative.cs                # + style/draw-list/radio externs
│   ├── ImPlotNative.cs               # NEW
│   ├── ImSpinnerNative.cs            # NEW
│   ├── ExtensionShimsNative.cs       # NEW
│   └── ImGuiInternal.cs              # + safe wrappers for all new externs
├── Infrastructure/
│   ├── NativeBridge.cs               # + delegate, ExpectedNativeVersion = 5
│   ├── FontResolver.cs               # NEW
│   └── SettingsStore.cs              # + font key
├── LibraryConfig.cs                  # + font/theme constants, FontsDir
├── DearImGuiKSP.cs                   # + public widget/theme/tween surface (facade)
DearImGuiKSPNative/
├── vendor/                           # NEW
│   ├── PIN_RECORD.md
│   ├── implot/    (implot.cpp, implot_items.cpp, implot.h, implot_internal.h @ v1.0)
│   ├── cimplot/   (cimplot.cpp/.h — regenerated vs pinned cimgui)
│   ├── imgui-knobs/
│   ├── imgui-wheels/
│   ├── imspinner/
│   ├── cimspinner/ (regenerated vs pinned cimgui)
│   └── imgui_toggle/
├── src/
│   ├── ContextHost.cpp/.h            # + LoadFontFromFile, ImPlot_CreateContext/DestroyContext
│   ├── DearImGuiKSPNative.cpp        # GetVersion() → 5; LoadFontFromFile export
│   └── shims/
│       ├── imgui_knobs_shim.cpp/.h
│       ├── imgui_wheels_shim.cpp/.h
│       └── imgui_toggle_shim.cpp/.h
├── build.bat / build_release.bat / build_harness.bat   # + new TUs and /I vendor paths
DearImGuiKSPDemo/
├── Telemetry/
│   ├── TelemetryAddon.cs             # [KSPAddon(Flight)], tab bar, toolbar button
│   ├── TelemetrySampler.cs
│   ├── RingBuffer.cs
│   ├── StageAnalyzer.cs
│   ├── GraphPanel.cs
│   ├── StagePanel.cs
│   └── OrbitPanel.cs
└── ThemeDemo.cs                      # folds into the existing demo window
tests/Application.Tests/
├── Animation/TweenEngineTests.cs     # NEW
├── Theming/ThemePresetsTests.cs      # NEW
└── SettingsModelTests.cs             # + font/theme cases (existing suite stays green)
GameData/DearImGuiKSP/
└── Fonts/                            # IBMPlexSans-Regular.ttf, IBMPlexSans-Medium.ttf, OFL.txt
docs/
├── 00-getting-started.md
├── 10-api-fundamentals.md
├── 20-widgets.md
├── 30-theming.md
├── 40-plotting.md
├── 50-animation.md
├── 60-migration-from-imgui.md
└── 70-troubleshooting.md
```

| File Path | Contains | Responsibility |
|---|---|---|
| `Application/Theming/ThemeEngine.cs` | Component | Apply named preset to global ImGui style; gradient params |
| `Application/Theming/ThemePresets.cs` | Entity | "ksp"/"dark" value tables |
| `Application/Theming/KspPalette.cs` | Value object | Spec §6.1 palette constants (tunable) |
| `Application/Animation/Ease.cs` | Value object | Easing enum + pure functions |
| `Application/Animation/Tween.cs` | Entity | Tween factory + public handle |
| `Application/Animation/TweenEngine.cs` | Component | Delta-time tick, completion removal, suspension pause |
| `Application/Api/ImGuiEx.cs` | Component | IDisposable scope wrappers (Window/Child/Plot) |
| `Application/Api/ImGuiPlot.cs` | Component | Public plot API over cimplot, span pinning |
| `Application/Api/ImGuiWidgets.cs` | Component | Public Knob/Wheel/Toggle/Spinner/RadioButton |
| `Application/Api/ImGuiGradients.cs` | Component | GradientButton, AddRectFilledGradientVertical (D28) |
| `Interop/ImPlotNative.cs` | Component | cimplot externs + verified enums |
| `Interop/ImSpinnerNative.cs` | Component | cimspinner externs |
| `Interop/ExtensionShimsNative.cs` | Component | knobs/wheels/toggle shim externs |
| `Infrastructure/FontResolver.cs` | Component | font setting → TTF path, fallback signal |
| `src/shims/*.cpp` | Component | Flat C ABI over vendored C++ extensions |
| `vendor/PIN_RECORD.md` | Entity | Provenance pins + generator commits |
| `Telemetry/*.cs` | Components | Sampler, ring buffer, analyzer, three panels (demo) |
| `ThemeDemo.cs` | Component | Widget/animation demo (demo window) |

Dependency direction check: Application references UnityEngine.CoreModule **for `Vector2`/`Color`/`Color32` only** (D24, amends D18); no lifecycle/scene APIs. Demo references the library's public surface only. Native Core has zero KSP/Unity knowledge. No Core→Application→Infrastructure violation introduced.

---

## 8. Dependencies & Error Handling (Step 7 Output)

### External Dependencies
| Library / API | Version | Purpose | Risk Level |
|---|---|---|---|
| ImPlot | v1.0 tag (exact pin) | 2D plotting | Med |
| cimplot | regenerated vs pinned cimgui | ImPlot C ABI | Med |
| imgui-knobs | pinned commit | Knobs | Low |
| imgui-wheels | pinned commit | Wheels | Med (immature; droppable) |
| imspinner + cimspinner | pinned / regenerated | Spinners | Low |
| imgui_toggle | pinned commit | Animated toggles | Low-Med |
| IBM Plex Sans (Regular, Medium) | OFL static TTFs | Default font | Low |
| KSP flight APIs (`FlightGlobals`, `vessel.orbit`, engine modules) | 1.12.x | Telemetry | Low |
| imgui / cimgui (existing) | 1.92.9 pinned | Base UI | Low (unchanged) |

### Error Handling Strategy
| Failure Scenario | Detection | Response | User Notification |
|---|---|---|---|
| Font file missing/corrupt | `LoadFontFromFile` returns false / path unresolved | Embedded ProggyClean fallback | One log line: `Font '<name>' not found or unreadable; using embedded default font.` |
| Unknown theme in settings | NormalizeTheme | Fall back to "ksp" | One log line: `Unknown theme '<name>'; using 'ksp'.` |
| Handshake mismatch (v4 native / v5 managed) | Version check at bridge init | Existing version-mismatch failure path (session-permanent disable + popup) | Existing popup flow (tested via retained v3 backup pattern) |
| No active vessel / non-flight scene | FlightGlobals checks in demo | Panels idle with `No active vessel.` placeholder | In-window text only |
| Vendor pin drift | Compile-time | Build fails loud (never runtime) | Build error |
| Consumer misuse of widget wrappers | Exception in consumer callback | Existing fault barrier (auto-disable at 5 consecutive failures) | Existing behavior |
| 16-bit index overflow (dense plots) | ImGui assert | Documented known limit; remedy (VtxOffset/32-bit) in `docs/70-troubleshooting.md` | Docs only |

### Logging
- Target: Unity log via `[DearImGuiKSP]` prefix (unchanged)
- Level: errors always; debug gated by `verboseLogging` (unchanged)
- Format: plain text (unchanged); no new popups this wave

---

## 9. Milestones (Step 8 Output)

Strictly sequential per spec §12; do not start milestone N until N−1 is verified. Chunk groups map to PlanImplementation groups G1–G8.

| # | Milestone | Components Implemented | Verification Method | Success Criteria |
|---|---|---|---|---|
| 1 | API ergonomics foundation | ImGuiEx scopes, Vector2/Color signatures (D24), style push/pop + draw-list/radio externs and wrappers | `dotnet build`, xUnit suite, in-game demo | Demo window renders through the new API; scope Dispose closes the stack on an injected exception; existing 59 tests green |
| 2 | Font pipeline | `LoadFontFromFile` native export + NativeBridge delegate, handshake v5 both sides, FontResolver, bundled Plex Sans + OFL.txt, `font` setting | Native builds (3 scripts), harness, in-game | Plex Sans visible at startup; missing TTF → ProggyClean + single log line; mismatched v4 native → existing version-mismatch popup path |
| 3 | KSP theme | ThemeEngine, ThemePresets, KspPalette, gradient helpers, circular radio buttons, imgui_toggle (shim + wrapper), `theme` default "ksp" | xUnit (value tables, normalization), in-game visual pass vs `VisualReferenceMaterial/UIStylingRef.png` at uiScale 1.0 | Default look approximates stock KSP (user sign-off; pixel-exact not required, spec §13); "dark" is byte-identical stock ImGui dark (regression); live re-apply on settings change |
| 4 | ImPlot integration | vendor/ + PIN_RECORD, cimplot regen, ImPlot context lifecycle, ImGuiPlot wrapper with span pinning | Native builds, harness, in-game, benchmark window | Two-plot window renders live data; zero per-frame managed allocation in plot submission; benchmark shows no managed-cost regression |
| 5 | Widget extensions + tween | knobs/wheels/spinners shims + wrappers, Ease/Tween/TweenEngine, ThemeDemo tab | xUnit (easing math, completion, cancel, suspension pause), in-game demo | All widgets render and animate in the demo window; tween tests green; suspension (F2/loading) pauses ticks |
| 6 | Telemetry showcase | TelemetryAddon, TelemetrySampler, RingBuffer, StageAnalyzer, GraphPanel, StagePanel, OrbitPanel | In-flight acceptance in the full D16 environment | 2x2 graphs + stage/Δv + orbit radar live at no measurable FPS cost; zero steady-state allocation in the telemetry hot path; `No active vessel.` placeholder outside flight; `approx dV` label present; D16 compatibility gates pass; ISSUES #001/#003 mechanisms remain inert-when-not-capturing |
| 7 | Documentation | docs/ 8-file set, XML-doc sweep on all new public members, README/LICENSE attribution (MIT/0BSD/OFL) | Modder-from-zero dry run against docs alone | Docs reach a working themed window with a plot without opening demo source; no emojis/symbol glyphs (D31); LICENSE aggregates all attributions |
| 8 | Release packaging + OpenGL decision point | Release zip (Fonts/ + docs/), lockstep version bump, D33 workload assessment and decision record | Clean-KSP install of the packaged zip; decision record written | Zip installs and runs in the D16 environment; OpenGL before/after-release decision recorded in DECISION_LOG |

---

## 10. Open Questions / Assumptions

- [Assumption: cimplot and cimspinner generators run cleanly against the pinned cimgui at vendor-setup time; if generation drifts, the fallback is hand-writing the needed subset of the C ABI (recorded as a risk in spec §11).]
- [Assumption: IBM Plex Sans static TTFs from `C:\Users\Matt\source\repos\Fonts\IBM_Plex_Sans` are OFL-licensed as researched; OFL.txt ships beside them.]
- [Assumption: KSP flight-data APIs behave per `RESEARCH_NOTES.md` §5; the knowledge library's `NOTES/` is checked before M6 implementation and new findings are recorded there.]
- [Assumption: spec §6.1 palette/gradient/rounding constants are starting values; the M3 in-game visual pass (user verifies) may revise them, per spec §13.]
- [Question (resolved at M8, not blocking): OpenGL before or after release — D33 decision point with an honest workload assessment.]

---

## Plan Checklist (Agent Self-Verification)

- [x] Every entity in Section 2 has a clear identity and lifetime classification
- [x] Every component in Section 3 has a single-responsibility description without "and"/"or"
- [x] Every cross-boundary dependency in Section 5 has an interface in Core or Application (or an explicit not-applicable justification)
- [x] Dependency direction (Core ← Application ← Infrastructure) is never violated in Section 7
- [x] Every pattern in Section 6 has a stated justification matching its "Use When" condition
- [x] Every milestone in Section 9 has an observable, testable success criterion
- [x] No God Classes, no global mutable state, no deep inheritance trees
- [x] The MVP in Section 1.5 is achievable with Milestones 1–4 plus the graph half of M6 and the core half of M7
