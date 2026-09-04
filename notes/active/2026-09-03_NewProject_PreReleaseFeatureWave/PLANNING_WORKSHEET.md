# Planning Worksheet: DearImGui-KSP Pre-Release Feature Wave
## Date: 2026-09-03
## Design Spec Reference: `notes/finished/2026-09-03_DesignSpec_Theming_Extensions_Showcase/DESIGN_SPEC.md` (D24–D34)

This wave extends the existing codebase; it does not create a new project. Steps below name only what the wave *adds or changes* — everything not mentioned stands as built in the M1–M6 implementation (`notes/finished/2026-07-29_DearImGuiKSP_PlanImplementation/FINAL_AUDIT.md`).

### Step 1 — Core Entities and State
| Entity | Identity | Attributes | Relationships | Lifetime |
|--------|----------|------------|---------------|----------|
| ThemePreset | Name ("ksp" / "dark") | Value table: colors per `ImGuiCol_*`, rounding, padding, border sizes; gradient params (button top/bottom, window-bg top/bottom) | Applied to global ImGui style by ThemeEngine; selected by `theme` setting | Transient (code constant tables); only the name persists in `settings.cfg` |
| LibrarySettings (existing, extended) | Singleton record | + `Font` (string, default "IBMPlexSans"); `Theme` default changes "dark"→"ksp" | Persisted via ISettingsStore; consumed by ThemeEngine, font pipeline | Persistent (`settings.cfg`) |
| FontRequest | Resolved font name | TTF path (under `GameData/DearImGuiKSP/Fonts/`), size in pixels, weight (Regular/Medium) | Produced by Infrastructure font resolution; consumed by native `LoadFontFromFile` | Transient (startup only) |
| Tween | Handle id | Setter `Action<float>`, from, to, duration (s), Ease, elapsed, IsPlaying | Owned by TweenEngine; ticked from frame loop | Transient (removed on completion) |
| TweenHandle | Handle id (public) | Cancel(), IsPlaying | Consumer-facing view of a Tween | Transient |
| TelemetrySample | Implicit (ring index) | float value + sample time per channel (altitude, q, throttle, G) | Owned by RingBuffer; rendered by GraphPanel | Transient (rolling, fixed 10k capacity) |
| StageSnapshot | Stage index | Approx Δv, Isp, burn time, propellant fractions per stage | Produced by StageAnalyzer; rendered by StagePanel | Transient (recomputed on staging / per second) |
| OrbitSnapshot | Vessel | ApA/PeA, eccentricity, inclination, semi-major axis, true anomaly, maneuver node list | Produced from `vessel.orbit`; rendered by OrbitPanel | Transient (per frame) |
| VendorPinRecord | Extension name | Repo, tag/commit, date, generator commit (cimplot/cimspinner) | Documents `DearImGuiKSPNative/vendor/` provenance | Persistent (in-repo file) |

#### Value Objects / Enums
- `Ease` — Linear, QuadIn/Out/InOut, CubicIn/Out/InOut.
- `SpinnerType` — ~15 curated imspinner variants (enum-dispatched, not the full ~590).
- `ThemeName` — "ksp", "dark" (string-backed; unknown → "ksp" + log).
- `KspPalette` — static readonly `Color32` constants per spec §6.1 (gradient/rounding values are tunable constants, revised after in-game visual pass).
- ImPlot enums — axis/location/line-style flag subsets needed by graph panel; hand-verified against `implot.h`.
- Knob/wheel variant enums — the small surface of each extension (~2 / ~6 functions).

### Step 2 — Behaviors and Responsibilities
| Component | Single-Sentence Responsibility | Command / Query / Both | Layer |
|-----------|-------------------------------|------------------------|-------|
| ContextHost (extend) | Owns the ImGui and ImPlot contexts and frame lifecycle, including loading a font from file before first NewFrame | Both | Native Core |
| ExtensionShims (new TUs) | Expose imgui-knobs / imgui-wheels / imgui_toggle as flat C-callable functions | Query (draw + value in/out) | Native Core |
| ImGuiNative / ImGuiInternal (extend) | Marshal new cimgui/cimplot/cimspinner/shim externs into safe internal C# calls | Both | Interop (managed) |
| NativeBridge (extend) | Carry the new `LoadFontFromFile` delegate and enforce handshake v5 | Command | Infrastructure |
| FontResolver (new) | Resolve the `font` setting to a TTF path under `Fonts/` with ProggyClean fallback signal | Query | Infrastructure |
| ThemeEngine (new) | Apply a named ThemePreset value table to the global ImGui style, including gradient state | Command | Application |
| TweenEngine (new) | Advance live tweens by frame delta time and invoke their setters | Command | Application |
| ImGuiEx scopes (new) | Guarantee Begin/End symmetry via IDisposable wrappers that close on exception | Both | Application (public API) |
| Widget wrappers (new) | Present Knob/Wheel/Toggle/Spinner/RadioButton/Plot as idiomatic public C# methods | Query | Application (public API) |
| Gradient helpers (new) | Draw two-stop vertical gradients via AddRectFilled + ShadeVerts post-shade | Query | Application (public API) |
| SettingsStore / SettingsModel (extend) | Persist and normalize the new `font` key and the changed `theme` default | Both | Infrastructure / Application |
| TelemetrySampler (new, demo) | Read stock flight state into fixed-capacity ring buffers once per frame | Command | Demo (consumer) |
| StageAnalyzer (new, demo) | Compute approximate per-stage Δv/Isp/burn-time from vessel parts on staging events | Query | Demo (consumer) |
| GraphPanel / StagePanel / OrbitPanel (new, demo) | Render the three showcase tabs from sampled data using only the public API | Query | Demo (consumer) |
| ThemeDemo (new, demo) | Demonstrate toggles, knobs, wheels, spinners, and gradient buttons in the existing demo window | Query | Demo (consumer) |

### Step 3 — Data Flow
```
[settings.cfg] --(theme/font strings)--> SettingsStore --(LibrarySettings)--> SettingsModel
        |                                          |
        |                                          +--(font name)--> FontResolver --(TTF path)--> NativeBridge.LoadFontFromFile --> [native font atlas]
        |                                          |
        |                                          +--(theme name)--> ThemeEngine --(style values)--> [global ImGui style]
        |
[frame loop] --(deltaTime)--> TweenEngine --(interpolated value)--> consumer setter --> [widget state]
        |
        +--(deltaTime)--> TelemetrySampler --(samples)--> RingBuffer --(ReadOnlySpan<float>)--> Plot wrapper --> [cimplot --> draw list --> screen]
        |
        +--(staging event / 1s timer)--> StageAnalyzer --(StageSnapshot)--> StagePanel --> [screen]
        |
        +--(vessel.orbit)--> OrbitSnapshot --> OrbitPanel (ImDrawList ellipse/markers) --> [screen]

[consumer using-scope] --(Begin)--> ImGuiEx scope wrapper --(Dispose on exit/exception)--> [End; stack closed inside fault barrier]
```

### Step 4 — Boundaries and Interfaces
| Interface | Defined In | Implemented By | Consumed By | Purpose |
|-----------|------------|----------------|-------------|---------|
| INativeBridge (existing, extend) | Application/Interfaces | NativeBridge (Infrastructure) | FrameLoopOrchestrator / startup wiring | + `LoadFontFromFile(utf8 path, float sizePixels) → bool`; expected version 4 → 5 |
| ISettingsStore (existing, unchanged shape) | Application/Interfaces | SettingsStore (Infrastructure) | SettingsModel | Gains `font` key via existing string plumbing; `theme` default change only |
| INativeWidgetBindings — **not introduced** | — | — | — | New widget externs follow the existing ImGuiNative DllImport pattern (direct, internal), same as the current 11 bindings; no new interface boundary warranted (YAGNI) |
| Demo ↔ library boundary (existing) | Public API | DearImGuiKSP static facade | Demo panels/sampler | Showcase consumes public API only — never internals (spec §4.1) |

### Step 5 — Pattern Selection
| Problem / Concern | Selected Pattern | Justification |
|---|---|---|
| Named visual presets over a global style | Strategy (theme presets) | Spec §4.6 (D34): "ksp"/"dark" are interchangeable value tables behind one apply operation |
| Begin/End stack discipline must survive exceptions | RAII scope guards (`IDisposable`) | Spec §4.6: Dispose guarantees symmetry inside the fault barrier |
| Vendored third-party widgets | Adapter (thin C ABI shims + C# wrappers) | Spec §4.6: upstream churn absorbed at the vendor pin |
| Time-based value animation | Tween engine (update-loop driven) | Spec §4.6 (D27): pure Application math, unit-testable, no native interop |
| Rolling telemetry history without allocation | Ring buffer (fixed 10k capacity) | Spec §4.4/§4.5: zero steady-state allocation; spans pinned inside the wrapper |
| Gradient rendering | None beyond existing cimgui exports (D28) | `igShadeVertsLinearColorGradientKeepAlpha` already exported; C#-only |
| Font/theme fallback | Graceful degradation + log line | Spec §5.2/§5.6: never a failure-mode trigger |

### Step 6 — Project Layout (new/changed files only)
```
DearImGuiKSP/
├── Application/
│   ├── Theming/
│   │   ├── ThemeEngine.cs          # preset application to global style
│   │   ├── ThemePresets.cs         # "ksp" and "dark" value tables
│   │   └── KspPalette.cs           # Color32 constants (spec §6.1, tunable)
│   ├── Animation/
│   │   ├── Ease.cs                 # easing enum + functions (pure math)
│   │   ├── Tween.cs                # Tween.To(...) factory + TweenHandle
│   │   └── TweenEngine.cs          # delta-time tick, completion removal
│   ├── Api/
│   │   ├── ImGuiEx.cs              # scope wrappers (Window/Child/Plot), style push/pop
│   │   ├── ImGuiPlot.cs            # ImPlot wrapper: Begin/PlotLine (spans + pinning)
│   │   ├── ImGuiWidgets.cs         # Knob, Wheel, Toggle, Spinner, RadioButton
│   │   └── ImGuiGradients.cs       # GradientButton, AddRectFilledGradientVertical
│   ├── SettingsModel.cs            # font key, theme default "ksp", NormalizeTheme real
│   └── LibraryConfig.cs            # font/theme constants, paths
├── Interop/
│   ├── ImGuiNative.cs              # + style/draw-list/radio externs (cimgui)
│   ├── ImPlotNative.cs             # cimplot externs
│   ├── ImSpinnerNative.cs          # cimspinner externs
│   ├── ExtensionShimsNative.cs     # knobs/wheels/toggle shim externs
│   └── ImGuiInternal.cs            # safe wrappers for all of the above
├── Infrastructure/
│   ├── NativeBridge.cs             # LoadFontFromFile delegate, ExpectedNativeVersion 5
│   ├── FontResolver.cs             # font setting → TTF path, fallback signal
│   └── SettingsStore.cs            # + font key
├── LibraryConfig.cs                # FontDefaults, FontsDir
DearImGuiKSPNative/
├── vendor/                         # NEW: implot v1.0, cimplot (regen), imgui-knobs,
│   │                               # imgui-wheels, imspinner, cimspinner (regen),
│   │                               # imgui_toggle — each with pin record
│   ├── PIN_RECORD.md               # repo, tag/commit, date, generator commit
├── src/
│   ├── ContextHost.cpp/.h          # + LoadFontFromFile, ImPlot create/destroy
│   ├── DearImGuiKSPNative.cpp      # GetVersion() → 5, LoadFontFromFile export
│   └── shims/
│       ├── imgui_knobs_shim.cpp/.h
│       ├── imgui_wheels_shim.cpp/.h
│       └── imgui_toggle_shim.cpp/.h
├── build.bat / build_release.bat / build_harness.bat   # + new TU file lists
DearImGuiKSPDemo/
├── Telemetry/
│   ├── TelemetryAddon.cs           # KSPAddon(Flight), registration, toolbar
│   ├── TelemetrySampler.cs         # flight-state reads into ring buffers
│   ├── RingBuffer.cs               # fixed-capacity float history
│   ├── StageAnalyzer.cs            # approximate per-stage Δv/Isp/burn time
│   ├── GraphPanel.cs               # 2x2 rolling plots, legends, tooltips
│   ├── StagePanel.cs               # stacked bars, meters, tooltips, "approx dV"
│   └── OrbitPanel.cs               # ImDrawList radar ellipse, markers, readout
└── ThemeDemo.cs                    # toggles/knobs/wheels/spinners/gradients in demo window
tests/Application.Tests/
├── Animation/TweenEngineTests.cs   # easing math, completion, cancel, pause
├── Theming/ThemePresetsTests.cs    # value tables, normalize/unknown→ksp
└── (existing 59 tests stay green; settings tests gain font/theme cases)
GameData/DearImGuiKSP/
└── Fonts/                          # IBMPlexSans-Regular.ttf, IBMPlexSans-Medium.ttf, OFL.txt
docs/                               # 00-getting-started … 70-troubleshooting (8 files, spec §8.2)
```

### Step 7 — Dependencies and Risks
| Dependency | Version | Purpose | Risk Level |
|---|---|---|---|
| ImPlot (epezent) | v1.0 tag, pinned | 2D plotting | Med — pin exact tag, do not track master |
| cimplot | regenerated vs pinned cimgui | C ABI for ImPlot | Med — generator drift; record generator commit, build fails loud |
| imgui-knobs (altschuler) | pinned commit | Knob widget | Low — 2 functions, 1.92.9 guards present |
| imgui-wheels (Engineer162) | pinned commit | Wheel widget | Med — immature repo; we own the pin; droppable pre-release |
| imspinner + cimspinner | pinned / regenerated | Spinner widgets | Low — curated ~15-type enum dispatch only |
| imgui_toggle (cmdwtf) | pinned commit | Animated toggles | Low-Med — expose scalar/flag overloads only |
| IBM Plex Sans | OFL, static Regular+Medium TTFs | Default font | Low — OFL.txt bundled (obligation) |
| `igShadeVertsLinearColorGradientKeepAlpha` | pinned cimgui export | Gradients (D28) | Low — compile-time failure on drift, never runtime |
| KSP flight APIs | 1.12.x | Telemetry reads | Low — knowledge-library check first; findings recorded in its NOTES/ |

Error handling (spec §5.6): missing font → ProggyClean + one log line; unknown theme → "ksp" + log; no active vessel → placeholder line; vendor pin drift → compile failure; handshake v4/v5 mismatch → existing version-mismatch failure path. Logging: `[DearImGuiKSP]` prefix, Debug level gated by verboseLogging (unchanged).

Native interop & hot-path checkpoints (CORE_PROTOCOLS §5.9) apply to: handshake v5 / LoadFontFromFile (Check 1 — no process-global mutation expected; Check 3 — font load failure path releases nothing extra, atlas stays consistent), telemetry sampling + plot submission + tween tick (Check 2 — zero steady-state allocation, pinned spans, ring buffers). Recorded per-chunk in implementation contracts.

### Step 8 — Verification Checkpoints (Sequential Milestones)

| # | Milestone | Components | Verification | Success Criteria | PlanImplementation Chunk Group |
|---|---|---|---|---|---|
| 1 | API ergonomics foundation | ImGuiEx scopes, Vector2/Color signatures, style push/pop, draw-list/radio externs | `dotnet build` + xUnit + in-game demo | Demo window redrawn through new API; scopes close on injected exception; 59 tests green | G1 |
| 2 | Font pipeline | LoadFontFromFile export+delegate, handshake v5, FontResolver, bundled Plex Sans + OFL | Native build (3 scripts) + harness + in-game | Plex Sans renders at startup; missing TTF → ProggyClean + log line; v4-native/v5-managed → version-mismatch popup path | G2 |
| 3 | KSP theme | ThemeEngine, presets, KspPalette, gradient helpers, circular radio, imgui_toggle (native+wrapper) | xUnit (value tables) + in-game visual pass vs `UIStylingRef.png` at uiScale 1.0 | Default "ksp" approximates stock look (user sign-off; pixel-exact not required); "dark" = exact stock ImGui dark (regression); settings switch re-applies live | G3 |
| 4 | ImPlot integration | vendor/ + PIN_RECORD, cimplot regen, ImPlot context lifecycle, ImGuiPlot wrapper | Native build + harness + in-game | 2-plot window renders live data via spans with zero per-frame alloc (benchmark window shows no cost regression) | G4 |
| 5 | Widget extensions + tween | knobs/wheels/spinners shims+wrappers, TweenEngine, ThemeDemo | xUnit (tween/ease) + in-game demo tab | All widgets render/animate in demo window; tween tests green; suspension pauses tweens | G5 |
| 6 | Telemetry showcase | TelemetryAddon, sampler, StageAnalyzer, three panels | In-flight acceptance in full D16 environment | 2x2 graphs + stage/Δv + orbit radar at no measurable FPS cost; zero per-frame alloc in hot path; placeholders outside flight; D16 gates pass | G6 |
| 7 | Documentation | docs/ 8-file set, XML-doc sweep, README/LICENSE attribution | Doc walkthrough (modder-from-zero dry run) | Docs reach a working themed window with a plot without opening demo source; XML docs on every new public member; LICENSE aggregates MIT/0BSD/OFL | G7 |
| 8 | Release packaging + OpenGL decision point | Release zip (Fonts/ + docs/), version bump, D33 assessment | Packaged install into clean KSP + decision record | Zip installs and runs in the D16 environment; OpenGL before/after-release decision recorded | G8 |

Milestones are strictly sequential (spec §12 dependency order). M1–M5 = the wave MVP plus widget extensions; per spec §3.3, knobs/wheels/spinners, Δv/orbit panels, and non-core docs are individually deferrable if scope must be cut.
