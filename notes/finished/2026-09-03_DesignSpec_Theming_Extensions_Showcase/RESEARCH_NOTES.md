# Research Notes — Pre-Release Feature Wave (Theming, Extensions, Showcase, Docs)

Session: `notes/active/2026-09-03_DesignSpec_Theming_Extensions_Showcase/`
Date: 2026-09-03

## 1. User's ask (verbatim scope, 2026-09-03)

1. KSP-vanilla-look default theme: gradients on widgets, rounded corners (buttons, round-corner rects), circular radio buttons; glowing-radio effect explicitly deferred as sugar. Palette from `VisualReferenceMaterial/KSP_Theme_Palette_Notes.md` + `UIStylingRef.png`. IBM Plex Sans as default font (`C:\Users\Matt\source\repos\Fonts\IBM_Plex_Sans`, OFL), Noto Sans as optional fallback. Close-to-stock, not pixel-exact.
2. Showcase telemetry mod: (a) live 60 Hz rolling graphs (Altitude, dynamic pressure q, throttle, G-force) in a 2x2 grid with legends + hover tooltips; (b) stage & delta-v breakdown with stacked bars, color-transitioning progress meters, Isp/burn-time tooltips; (c) 2D orbital radar view (custom ImDrawList vector drawing: orbit ellipse, vessel position, Ap/Pe/target markers) + orbital elements. Intent: showcase 60 Hz graphs, custom vector maps, complex dockable layouts. Warrants ImPlot integration + wheels/knobs/spinners.
3. Animations + 3D plotting = stretch goals, evaluated after the above lands. Explicit anti-over-spec directive: every feature needs a real use case today.
4. Modder-facing API documentation beyond the demo mod; idiomatic C# (IDisposable/using blocks, strong types Vector2/Color); XML docs on all public members (already true); no emojis/symbols in docs.
5. Release is POST all of this. OpenGL before/after release decided by honest workload assessment.

## 2. Extension research (web, 2026-09-03)

| Extension | License | Structure | C bindings | Verdict |
|---|---|---|---|---|
| ImPlot (epezent) | MIT | 2 .cpp + 2 .h; uses imgui_internal | **cimplot exists** (cimgui org, same Lua generator, ~792 exports, tracks implot master) | MEDIUM-low: pin **v1.0 tag** (imgui 1.92 fixes land in v0.17+; v1.0 ~Jan 2026); regenerate cimplot against our pinned cimgui; compile `implot.cpp`+`implot_items.cpp`+`cimplot.cpp` in |
| imgui-knobs (altschuler) | MIT | 2 files; imgui_internal | none — 2 public functions, trivial hand shim | LOW: upstream already carries 1.92.9 compat guards |
| imgui-wheels (Engineer162) | MIT | 2 files; heavy imgui_internal | none — 6 functions, trivial shim | MEDIUM: **repo created 2026-08-03, 1 star, zero adoption** — vendor-and-pin, we own it; uses `IM_ARRAYSIZE` alias (deprecated 1.92.6, still present in 1.92.9) |
| imspinner (dalerank) | MIT | header-only, 7 headers; imgui_internal | **official cimspinner subfolder** (~590 exports, cimgui conventions, added 2026-08) | LOW: compile `cimspinner/*.cpp` in; actively maintained, 1.92.x-current |
| imgui_toggle (cmdwtf) | 0BSD | 4 .cpp + 5 .h; imgui_internal | none — ~6 overloads, trivial shim | LOW-MEDIUM: actively maintained (1.92.8 fixes merged 2026-07); expose only scalar/flag overloads |
| HImGuiAnimation (Half-People) | Apache-2.0 | 2 files, **zero imgui dependency** | none; pointer/`void*` callback API | **Do NOT bind.** ~150 LOC with real bugs (static delta_time buffer shared across instances, frame truncation); abandoned since 2024-04. Reimplement the concept in C# (~100 lines: tween manager + easing enum) — Unity-free Application layer, unit-testable, idiomatic `Action<float>` callbacks |
| implot3d (brenocq) | MIT | 4-6 files; imgui_internal | cimplot3d exists (cimgui org) | MEDIUM: pre-1.0 (v0.4), **self-declared breaking changes between versions**; requires 32-bit `ImDrawIdx` or `RendererHasVtxOffset` (we have neither — see §3) |
| Gradients (imgui #4722) | n/a (patterns) | ocornut design thread + reference impl | n/a | LOW: `ImDrawList_AddRectFilledMultiColor` and **`igShadeVertsLinearColorGradientKeepAlpha` are already exported by our pinned cimgui** (cimgui.cpp:2482, cimgui.h:5595). Correct technique for rounded+gradient: AddRectFilled then ShadeVerts post-shade. Pure C# P/Invoke, zero native changes |

## 3. Current-state findings (local, 2026-09-03)

- **Managed binding surface is narrow**: only 11 cimgui externs hand-written in `Interop/ImGuiNative.cs` (not the "full API" — that claim covers native DLL exports only). Every new widget needs: extern in `ImGuiNative.cs` + wrapper in `ImGuiInternal.cs` + public API method. Enum values hand-verified against imgui.h.
- **No imgui_internal bindings yet**; cimgui exports only some internals (ShadeVerts yes; ButtonBehavior/RenderTextClipped per cimgui pin — verify at implementation time).
- **Public API is primitives-only** (string/int/float/bool); no Vector2/Color/ImVec2/ImVec4 in signatures. `Application/` is Unity-free by design (D18) — adding Unity types is an architectural decision, not a detail.
- **No IDisposable/using wrappers** — consumers call `BeginWindow`/`EndWindow` manually (`DemoConsumer.cs:102-128`).
- **Font pipeline**: atlas built natively (`ContextHost.cpp:49`, `AddFontDefault` = embedded ProggyClean), imgui 1.92.9 texture protocol (lazy atlas, GPU upload from `draw_data->Textures`). **No `AddFontFromFileTTF` anywhere; no string/path mechanism managed→native** (only input chars cross). TTF loading = new native export + **handshake v5**.
- **Native build**: single `cl` line, explicit file list in `build.bat:6` / `build_release.bat:6` / `build_harness.bat:6`. Adding extensions = appending paths (3 files).
- **`ImDrawIdx` is 16-bit; `RendererHasVtxOffset` not set.** Dense implot heatmaps can assert; implot3d meshes will break. Fix = define `IMGUI_USE_32BIT_INDICES` or set VtxOffset in BackendD3D11 (native change, low risk, but touches the locked renderer).
- **Native plugin binding is LoadLibrary/GetProcAddress + delegates** (`NativeBridge.cs`), not P/Invoke; growing the surface touches: native export + version consts both sides (v4 → v5) + delegate field + `INativeBridge.cs` + build scripts.
- **Demo consumer shape**: one `[KSPAddon(EveryScene)]` MonoBehaviour, `IsAvailable` check, `Register(id, OnFrame)`, per-frame window declaration, ApplicationLauncher toggle. A telemetry mod slots in identically (likely `KSPAddon.Startup.Flight`).
- XML docs already complete on all public members (user requirement already satisfied; must not regress as API grows).

## 4. KSP palette (from VisualReferenceMaterial, gitignored local)

Light green 181,252,0 / dark green 51,230,51; light orange 255,198,0 / dark orange 255,150,0; main fill grey 94,97,106; light grey text 188,188,188; dark grey text 58,58,63; off-white 236,236,236; button gradient dark(bottom) 57,72,90 → light(top) 102,114,135.

## 5. Telemetry data sources (KSP API, to confirm at implementation)

- Altitude: `FlightGlobals.ActiveVessel.altitude` / `terrainAltitude`. q: `vessel.dynamicPressurekPa`. Throttle: `FlightInputHandler.state.mainThrottle`. G: `vessel.geeForce`.
- Orbit: `vessel.orbit` (ApA/PeA, eccentricity, inclination, semiMajorAxis, true anomaly → 2D ellipse projection is consumer-side math).
- Stage Δv: precise staging simulation (stock `FuelFlowSimulation`) is a large subsystem (cf. Kerbal Engineer); a simplified per-stage propellant/engine sum is the realistic showcase scope — needs a user scope decision.
- Knowledge library check before implementation: `NOTES/ui-rendering-and-input.md` exists; flight-data APIs likely need new notes entries.

## 6. Gaps requiring user decisions

See QUESTION_LOG.md — consolidated Phase 1/2 question batch (theme default vs option, Unity-type policy in Application layer, docking interpretation, telemetry mod packaging & Δv depth, toggle scope, font weights/bundling, docs format, implot3d/animation verdicts presented as recommendations).
