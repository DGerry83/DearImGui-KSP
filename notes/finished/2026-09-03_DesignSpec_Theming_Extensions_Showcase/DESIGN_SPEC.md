# Design Specification: DearImGui-KSP Pre-Release Feature Wave

Extends (does not replace) the base spec: `notes/finished/2026-07-29_DesignSpec_DearImGuiKSP_UI_Library/DESIGN_SPEC.md`. Everything not mentioned here stands unchanged. Decisions D24–D34 in this session's `DECISION_LOG.md`.

## 1. Overview

### 1.1 Project Name

**DearImGui-KSP pre-release feature wave** — theming, extensions, showcase, and documentation. No new assemblies; the wave lands in the existing `DearImGuiKSP.dll`, `DearImGuiKSPNative.dll`, and `DearImGuiKSPDemo.dll`.

### 1.2 One-Sentence Pitch

Make DearImGui-KSP look like it belongs in KSP by default, give it the widget depth (plots, knobs, wheels, toggles, spinners, gradients, animation) that a real mod UI needs, prove it with a flight telemetry showcase, and document the API well enough that a modder can adopt it without reading the demo's source.

### 1.3 Target Platform & Runtime

Unchanged from base spec §1.3: KSP 1.12.x, Unity 2019.4.18f1, Mono x64, Windows, D3D11 (OpenGL deferred, D20/D33).

### 1.4 Distribution

Unchanged structure (base §1.4), with three additions to the library zip: `Fonts/` (IBM Plex Sans + OFL.txt), `docs/` (modder documentation set), and updated README/LICENSE covering bundled-font and vendored-extension attribution.

---

## 2. Design Intent

### 2.1 Core Experience

- **Players** see mod UIs that visually belong next to stock KSP windows: blue-grey gradient buttons, rounded corners, circular radio buttons, KSP green/orange accent text, IBM Plex Sans.
- **Mod authors** get an idiomatic C# API (`using` scopes, `Vector2`/`Color`, spans, XML-doc'd members) with plot/knob/wheel/toggle/spinner widgets and tween helpers, plus docs that teach it.

### 2.2 Success Criteria

1. With `theme = "ksp"` (the default), the demo window visually approximates the stock KSP UI per `VisualReferenceMaterial/UIStylingRef.png` — gradients, rounding, palette — close enough that a screenshot sits naturally beside stock windows. Pixel-exactness is explicitly not required.
2. The telemetry showcase runs in flight in the full D16 environment: 2x2 live graphs, stage/Δv panel, orbital radar view, at no measurable FPS cost and zero per-frame managed allocation in the telemetry hot path.
3. A modder can follow `docs/` from zero to a working themed window with a plot, without opening the demo source.
4. All existing behavior regresses nowhere: D16 compatibility gates pass, the 59-test suite plus new tests stay green.

### 2.3 Inspiration & References

- `VisualReferenceMaterial/UIStylingRef.png` (stock Continue-Saved-Game dialog + Cinematic Shaders panel), `KSP_Theme_Palette_Notes.md` (palette), `ImGui_Extension_Project_Links.md` (extension shortlist).
- Extension repos and integration findings: `RESEARCH_NOTES.md` §2.

---

## 3. Scope

### 3.1 In Scope

- **KSP theme** as the library default ("ksp"), with "dark" retained (D25): palette mapping, rounded corners, circular radio buttons, gradient buttons and window backgrounds (D28), imgui_toggle-based animated toggles.
- **Bundled font**: IBM Plex Sans Regular + Medium with OFL.txt; `font` setting with ProggyClean fallback; handshake v5 (D29).
- **API ergonomics**: `Vector2`/`Color`/`Color32` public signatures (D24), `IDisposable` scope wrappers for Begin/End pairs, style push/pop bindings, widget bindings needed by the theme and showcase.
- **Extension integrations** (D27): ImPlot v1.0 (2D plots), imgui-knobs, imgui-wheels, imspinner, imgui_toggle.
- **C# easing/tween helper API** in Application (D27).
- **Telemetry showcase** inside DearImGuiKSPDemo (D30): 2x2 rolling graphs (altitude, dynamic pressure q, throttle, G-force) with legends and hover tooltips; stage/Δv panel with stacked bars, color-transitioning propellant meters, Isp/burn-time tooltips; 2D orbital radar (ImDrawList ellipse, live vessel marker, Ap/Pe/target markers, existing maneuver nodes display-only) plus orbital elements readout.
- **Modder documentation** in `docs/` (D31), shipped in the release zip.
- **Release packaging** (D15) after the above completes (D33).

### 3.2 Out of Scope

- ImGui docking branch (D26), implot3d (D27), OpenGL backend (D20/D33 — decision point after this wave), CKAN metadata, Linux/Mac.
- Glowing-radio activation effect (deferred sugar, per user).
- Precise stock-grade Δv simulation; maneuver-node creation/editing (D30).
- Consumer font-loading API (base §3.4 stays deferred — the bundled-font mechanism must not preclude it).
- Migrating any existing mod to the library.

Known limitations introduced by this wave (intentional partial coverage):

- 16-bit `ImDrawIdx` retained (D32): draw lists above 65,535 vertices (dense heatmaps, huge scatter sets) will assert/fail. Trigger and remedy (enable `RendererHasVtxOffset` or 32-bit indices) documented in `docs/`.
- Approximate Δv is labeled "approx" in the UI; it can diverge from stock/KER values for asparagus staging and complex fuel flows.

### 3.3 Minimum Viable Product (this wave)

Theme + fonts + ergonomics + ImPlot + the telemetry showcase's graph panel + core docs (getting-started, API fundamentals, theming). Knobs/wheels/spinners/toggles, the Δv and orbit panels, and the remaining docs complete the wave but are individually deferrable if scope must be cut.

### 3.4 Deferred / Future Work

- Glowing radio buttons; implot3d; docking branch; OpenGL (post-wave decision, D33); consumer font loading; window position-persistence helper; 32-bit index support (D32); CKAN; Linux/Mac.

---

## 4. Technical Architecture

### 4.1 Technology Stack (additions to base §4.1)

| Layer | Technology | Purpose |
|-------|------------|---------|
| Core (native) | + ImPlot v1.0, cimplot, imgui-knobs, imgui-wheels, imspinner+cimspinner, imgui_toggle — all compiled into `DearImGuiKSPNative.dll` | Widget implementations; C ABIs (generated for plot/spinner, thin hand shims for knobs/wheels/toggle). Zero KSP knowledge; imgui_internal use is safe because everything compiles against the pinned 1.92.9 tree. |
| Application (managed) | + Theme engine (D34), tween engine, extension widget wrappers, scope wrappers, style/draw-list helpers | Unity-lifecycle-free; **may reference UnityEngine.CoreModule for `Vector2`/`Color`/`Color32` only** (D24, amends D18). xUnit-tested where logic exists (tween math, theme value tables). |
| Interop (managed) | + New cimgui/cimplot/cimspinner externs and hand-shim delegates, hand-verified against headers | Same pattern as the existing 11 bindings. |
| Infrastructure (managed) | + Font path resolution, theme/font settings plumbing | The only KSP/Unity-lifecycle layer, unchanged in role. |
| Demo (managed) | + Telemetry showcase (flight-scene addon), theme/toggle/animation demos | Consumer of the public API only — it must never touch internals. |

Dependency direction unchanged: Infrastructure → Application → Core. The D24 amendment is deliberately narrow: Application uses Unity **math structs**, never engine lifecycle, scene, or object APIs.

### 4.2 Native Changes (handshake v4 → v5)

- New export `DearImGuiKSPNative_LoadFontFromFile(utf8 path, float sizePixels)` called by managed init before the first NewFrame; returns success/failure. Failure or absent setting → `AddFontDefault` (ProggyClean) + log line (D29). Atlas build stays lazy under the 1.92.9 texture protocol.
- Build scripts (`build.bat`, `build_release.bat`, `build_harness.bat`): explicit `cl` file lists gain `implot.cpp`, `implot_items.cpp`, `cimplot.cpp`, `cimspinner/*.cpp`, `imgui-knobs.cpp`, `imgui-wheels.cpp`, `imgui_toggle*.cpp`, plus the hand-shim translation units. Third-party sources are vendored under `DearImGuiKSPNative/vendor/` with a pin record (repo, tag/commit, date) — cimplot/cimspinner are **regenerated against the pinned cimgui clone** during vendor setup, not copied blind.
- ImPlot context: `ImPlot_CreateContext`/`DestroyContext` alongside the ImGui context in `ContextHost`.
- Version constants bump to 5 on both sides in lockstep (D17); `NativeBridge.cs` gains the new delegate.
- No 32-bit index / VtxOffset change (D32).

### 4.3 Managed API Shape (illustrative, not exhaustive)

- Scopes: `using (ImGuiEx.Window("Telemetry")) { ... }`, `using (ImGuiEx.Child("plots", size))`, `using (ImGuiPlot.Begin("Altitude", size)) { ... }` — every Begin/End pair gets an `IDisposable` wrapper whose Dispose closes the stack even on exceptions (fault-barrier friendly).
- Types: `Vector2`, `Color`/`Color32` in signatures; `ReadOnlySpan<float>`/`ReadOnlySpan<double>` plot data with pinning inside the wrapper (no copies, no per-frame allocation).
- Theme: `DearImGuiKSP.CurrentTheme` (read), style push/pop (`PushStyleColor`/`PushStyleVar`), gradient helpers (`GradientButton(label, top, bottom, size)`, `AddRectFilledGradientVertical(...)`), KSP theme applied globally by the library (D34).
- Widgets: `Knob`, `Wheel`, `Toggle`, `Spinner(type, ...)` (enum-dispatched over ~15 curated spinner types, not all ~590), `RadioButton`.
- Tween: `Tween.To(Action<float> set, float from, float to, float seconds, Ease ease)` → handle (`Cancel`, `IsPlaying`); Color overload; driven by the library frame loop; Application-layer, unit-tested.

### 4.4 Data Sources

- Telemetry reads stock flight state in the demo mod (consumer-side, per base §4.2 — the library never queries game state): `FlightGlobals.ActiveVessel` (altitude, `dynamicPressurekPa`, `geeForce`, `orbit`), `FlightInputHandler.state.mainThrottle`, stage engine/propellant data from vessel parts/modules, `patchedConicSolver.maneuverNodes` (display-only). Ring buffers (10k points, fixed capacity) for graph history — zero allocation at steady state.
- Knowledge-library check before flight-data implementation; new findings recorded in its `NOTES/`.

### 4.5 Update Cadence & Performance

Unchanged model (library frame loop). New hot paths — telemetry ring buffers, plot submission, tween tick — must hold the base budget (< 1 ms managed frame cost, no steady-state allocation); the benchmark window remains the regression instrument, and the showcase itself is a live 60 Hz proof.

### 4.6 Pattern Selection (additions to base §4.5)

| Problem / Concern | Selected Pattern | Justification |
|---|---|---|
| Named visual presets over a global style | Strategy (theme presets) | "ksp"/"dark" are interchangeable value tables + setup steps behind one apply operation; new themes add presets without touching frame logic. |
| Begin/End stack discipline must survive exceptions | RAII scope guards (`IDisposable`) | Immediate-mode pairs map directly to `using`; Dispose guarantees symmetry inside the fault barrier. |
| Vendored third-party widgets | Adapter (thin C ABI shims + C# wrappers) | Each extension's C++ API is adapted to our C-callable surface, then to idiomatic C#; upstream churn is absorbed at the vendor pin, not in consumer code. |
| Time-based value animation | Tween engine (update-loop driven) | Pure Application-layer math ticked by the existing frame loop; no native interop, fully unit-testable. |

---

## 5. Mechanics & Behavior

### 5.1 Theme Application

- Themes are named presets: value tables (colors per `ImGuiCol_*`, rounding, padding, border sizes) plus gradient parameters (button top/bottom, window-bg top/bottom).
- Applied at library startup after font load, and re-applied when `settings.cfg` changes `theme`. Consumer style push/pop composes on top per frame and is unaffected by theme switches.
- "dark" = the exact stock ImGui dark style (current behavior, regression-gated).

### 5.2 Font Loading

- Order at startup: resolve `font` setting → path under `GameData/DearImGuiKSP/Fonts/` → `LoadFontFromFile` before first NewFrame. Missing/corrupt TTF or `font = "ProggyClean"` → embedded default + one log line. Not a failure-mode trigger (§5.4 of base spec is untouched).
- `fontScale` applies as today; Medium weight is used where the theme wants emphasis (titles/headers), Regular elsewhere.

### 5.3 Extension Availability

Extensions are always-on once compiled in — no per-extension settings. Widget wrappers throw the standard consumer-fault path on misuse (caught by the fault barrier like any consumer error).

### 5.4 Tween Engine

- Ticks from the library frame loop with frame delta time; tweens are fire-and-forget with optional cancel handle; completed tweens are removed (no accumulation). Deterministic: driven solely by delta time and the easing function — no randomness (base §5.4 determinism holds).
- Library suspension (F2/loading) pauses tween ticks with the frame loop; no consumer callbacks run, consistent with base §5.2.

### 5.5 Telemetry Behavior

- Graphs: fixed-capacity ring buffers sampled once per frame while in flight scene; graphs render only while their window is open (sampling continues so history is warm on reopen).
- Stage panel recomputes on staging events / per second (not per frame); Δv approximation labeled in the UI.
- Orbit radar redraws per frame from `vessel.orbit`; degrades gracefully (panel shows "no active vessel") outside flight.

### 5.6 Edge Cases & Failure Modes (additions)

- Missing font file → ProggyClean fallback + log (§5.2).
- Telemetry with no active vessel / in non-flight scenes → panels idle with a placeholder line; no exceptions.
- Theme name unknown in settings → fall back to "ksp" + log.
- Extension vendor pin drift at build time → compile failure (fail loud at build, never at runtime).
- All base-spec failure modes (native missing, version mismatch incl. the new v5 check, unsupported API) unchanged.

---

## 6. User Experience & Interface

### 6.1 KSP Theme — Visual Design System

Palette source: `VisualReferenceMaterial/KSP_Theme_Palette_Notes.md` (RGB). All gradient/rounding values are starting points tuned in-game against `UIStylingRef.png`.

| Element | Spec |
|---------|------|
| Window background | Two-stop vertical gradient, top rgb(58,58,63) → bottom rgb(94,97,106); rounding 6 px; 1 px border rgb(30,32,38) |
| Title bar | Flat rgb(57,72,90); active title text off-white rgb(236,236,236) |
| Buttons | Two-stop vertical gradient, top rgb(102,114,135) → bottom rgb(57,72,90) (D28 ShadeVerts technique); rounding 4 px; hover lightened ~15%; active shifted toward KSP green; text off-white |
| Frame backgrounds (inputs, sliders, combo) | Flat rgb(57,72,90) darkened ~20%; rounding 4 px |
| Slider grab / scrollbar grab | rgb(102,114,135), hover → KSP light grey rgb(188,188,188); rounding 4 px |
| Radio buttons | Circular (stock `igRadioButton` geometry), active fill KSP light green rgb(181,252,0); glow deferred |
| Checkboxes / toggles | Animated imgui_toggle switches, on-state KSP light green, off-state frame grey; classic square checkbox remains available |
| Text | Default off-white rgb(236,236,236); secondary light grey rgb(188,188,188); on-dark-fill text rgb(58,58,63) where the palette demands it |
| Accents | Positive/confirm: light green rgb(181,252,0); strong positive text variant dark green rgb(51,230,51); warning: light orange rgb(255,198,0); critical/destructive: dark orange rgb(255,150,0) |
| Headers / selectables | Background rgb(57,72,90); hover rgb(102,114,135); rounding 4 px |
| Font | IBM Plex Sans Regular (body) / Medium (titles, headers); `fontScale` as today |

### 6.2 Showcase Layout

One flight-scene window (ApplicationLauncher toggle, consistent with the existing demo) using multi-panel layout (D26): tab bar with "Graphs" (2x2 plot grid), "Stages" (staging column + propellant meters), "Orbit" (radar + elements readout). Theme demos (toggles, knobs, wheels, spinners, gradient buttons) fold into the existing demo window rather than the flight window.

### 6.3 Animation & Timing

Tween engine with a standard easing set (linear, quad/cubic in/out, ease-in-out). Used by: toggle switches (native extension animates itself), showcase meter color transitions, spinner motion (extension-native). Frame-rate independent (delta-time driven).

### 6.4 Accessibility

- `uiScale`/`fontScale` unchanged; theme honors them.
- Colorblind note: green/orange accents always pair with text labels, never color-only meaning; documented in the theming guide.
- No audio, no motion-sensitivity concerns beyond spinners (static alternatives exist via classic widgets).

---

## 7. Content & Strings

Additions to the base string table (all log/prefix rules unchanged; no new popups):

| Context | English Text | Notes |
|---------|--------------|-------|
| Log: font fallback | `Font '<name>' not found or unreadable; using embedded default font.` | `[DearImGuiKSP]` prefix |
| Log: unknown theme | `Unknown theme '<name>'; using 'ksp'.` | |
| Showcase: no vessel | `No active vessel.` | Flight panels placeholder |
| Showcase: Δv label | `approx dV` | Approximation disclosure (D30) |
| Showcase: tab titles | `Graphs`, `Stages`, `Orbit` | |
| Docs file set | See §8.2 | Titles per D31 |

Voice/tone per base §7.3; docs are technical but plain, no emojis or symbol glyphs (D31).

---

## 8. Assets

### 8.1 Asset Inventory (additions)

| Asset ID | Type | File Path | Source | Format | Status |
|----------|------|-----------|--------|--------|--------|
| DK_FontPlexRegular | Font | `GameData/DearImGuiKSP/Fonts/IBMPlexSans-Regular.ttf` | Bundled (OFL) | Static TTF | Required |
| DK_FontPlexMedium | Font | `GameData/DearImGuiKSP/Fonts/IBMPlexSans-Medium.ttf` | Bundled (OFL) | Static TTF | Required |
| DK_FontOFL | License | `GameData/DearImGuiKSP/Fonts/OFL.txt` | Bundled | Text | Required (OFL obligation) |
| DK_VendoredSrc | Code | `DearImGuiKSPNative/vendor/` (+ pin record) | implot v1.0 tag, cimplot/cimspinner regenerated, knobs/wheels/toggle pinned commits | C/C++ source | Required |
| DK_Docs | Docs | `docs/` in repo and release zip | Authored in-repo | Markdown | Required |

### 8.2 Docs Set (D31)

`docs/00-getting-started.md` (dependency declaration, `KSPAssemblyDependencyEqualMajor`, first window), `10-api-fundamentals.md` (registration, frame callback, scopes, types, availability), `20-widgets.md` (catalog incl. knobs/wheels/toggles/spinners/radio), `30-theming.md` (themes, style push/pop, gradients, palette), `40-plotting.md` (ImPlot wrapper, spans, ring-buffer pattern), `50-animation.md` (tween API), `60-migration-from-imgui.md` (IMGUI-to-library mapping), `70-troubleshooting.md` (incl. the 16-bit index limit and its remedy).

### 8.3 Placeholder Policy

Theme gradient/rounding values are tunable constants, not placeholders. Nothing ships as placeholder except the pre-existing demo toolbar icon (already accepted).

---

## 9. Configuration

Additions to base §9.1 (`settings.cfg`, same persistence rules):

| Setting ID | Type | Default | Range | Description |
|------------|------|---------|-------|-------------|
| theme | String | **"ksp"** (was "dark") | "ksp", "dark" | Theme preset; unknown → "ksp" + log (D25) |
| font | String | "IBMPlexSans" | "IBMPlexSans", "ProggyClean", or a TTF filename placed in `Fonts/` | Default font; fallback ProggyClean + log (D29) |

Telemetry window state belongs to the demo mod (consumer state rule, base §4.4). No other new settings — extension behavior is not settings-gated (§5.3).

---

## 10. Compatibility & Distribution

- Hard/soft dependencies unchanged (base §10.1/§10.2).
- D16 environment regression gates apply to every milestone; imgui_toggle/wheels/knobs/spinners must not disturb IMGUI mods or the uGUI/IMGUI click-blocking work (ISSUES #001/#003 mechanisms stay inert-when-not-capturing).
- Handshake v5: managed and native release in lockstep (D17); a v4-native/v5-managed mismatch hits the existing version-mismatch failure path (tested via the retained v3 backup pattern).
- Release zip gains `Fonts/` + `docs/` (§1.4); LICENSE aggregates MIT (imgui, cimgui, implot, knobs, wheels, spinner), 0BSD (toggle), OFL (font) attributions.

---

## 11. Risks & Open Questions

| Risk | Mitigation |
|------|------------|
| imgui-wheels immaturity (days old, 1 star) | Vendor-and-pin; we own the code; ~6-function surface; drop from release if it misbehaves — no consumer depends on it yet |
| cimplot/cimspinner regeneration drift vs our pins | Regenerate during vendor setup against the pinned cimgui; record generator commit in the pin record; build fails loud on mismatch |
| `igShadeVerts*` is an imgui_internal export that could drift on future imgui bumps | We own the cimgui pin; gradient helpers fail at compile time, not runtime |
| Approximate Δv misleads players | Labeled "approx dV" in UI + docs; showcase framing, not a navigation tool |
| Theme constants diverge from stock look across resolutions/scales | In-game tuning pass against `UIStylingRef.png` at uiScale 1.0 before sign-off; user verifies |
| Pre-1.0 ImPlot v1.1 churn | Pin the v1.0 tag exactly; do not track master |

Open questions: none blocking. OpenGL before/after release is a decision point after this wave (D33).

---

## 12. Intended Milestone Order (for the implementation plan that follows)

Sequenced by dependency, not by user priority; the detailed plan, gates, and chunk contracts are produced in the follow-up planning session (bootstrap route, per Phase 7 routing):

1. **API ergonomics foundation** — D24 amendment, Vector2/Color signatures, scope wrappers, style push/pop bindings (everything else builds on these).
2. **Font pipeline** — native export, handshake v5, bundled Plex Sans, fallback.
3. **KSP theme** — preset engine, palette, gradients (D28), rounding, radio, imgui_toggle.
4. **ImPlot integration** — vendor + cimplot regen + C# wrapper (largest single chunk).
5. **Widget extensions** — knobs, wheels, spinners + tween engine.
6. **Telemetry showcase** — graphs, stages, orbit panels.
7. **Documentation** — docs/ set + XML-doc sweep + README/LICENSE attribution.
8. **Release packaging** (D15) + **OpenGL decision point** (D33).

---

## 13. Revision History

| Date | Author | Change |
|------|--------|--------|
| 2026-09-03 | Agent + User | Initial wave spec (Q1–Q11 answered; decisions D24–D34) |
| 2026-09-03 | User | Spec confirmed. Note on §6 (visual design system): the palette/gradient/rounding values are starting points — the user will verify the look in-game and §6.1 may be revised after that visual pass; hard to judge from the spec alone. |
