# Chunk Map: DearImGui-KSP Pre-Release Feature Wave Implementation
## Date: 2026-09-03
## Digest Reference: `notes/active/2026-09-03_PreReleaseFeatureWave_Implementation/PLAN_DIGEST.md`

### Chunk List
| ID | Name | Type | Files/Records | Est. Complexity | Milestone | Status |
|----|------|------|---------------|-----------------|-----------|--------|
| C1 | Unity math types in Application (D24) | Foundation | `DearImGuiKSP.csproj`, `DearImGuiKSP.cs` (signature upgrade) | Low | M1 | Pending |
| C2 | Binding foundation: style push/pop, draw-list, radio externs | Foundation | `Interop/ImGuiNative.cs`, `Interop/ImGuiInternal.cs` | Med | M1 | Pending |
| C3 | Scope wrappers (IDisposable Begin/End guards) | Vertical slice | `Application/Api/ImGuiEx.cs`, facade wiring | Med | M1 | Pending |
| C4 | Native font export + handshake v5 (native side) | Foundation | `src/ContextHost.cpp/.h`, `src/DearImGuiKSPNative.cpp` | Med | M2 | Pending |
| C5 | Managed bridge: LoadFontFromFile delegate + v5 + startup wiring | Consumer | `Infrastructure/NativeBridge.cs`, `Application/Interfaces/INativeBridge.cs`, `Composition.cs`, `DearImGuiKSPAddon.cs` | Med | M2 | Pending |
| C6 | Font settings + FontResolver + fallback | Consumer | `SettingsStore.cs`, `SettingsModel.cs`, `LibraryConfig.cs`, `Infrastructure/FontResolver.cs`, settings tests | Med | M2 | Pending |
| C7 | Bundle font assets | Vertical slice | `GameData/DearImGuiKSP/Fonts/` (2 TTF + OFL.txt) | Low | M2 | Pending |
| C8 | Theme engine + presets + palette + theme default | Vertical slice | `Application/Theming/*.cs`, `SettingsModel.cs`, `LibraryConfig.cs`, orchestrator wiring, theme tests | High | M3 | Pending |
| C9 | Gradient helpers + circular radio | Consumer | `Application/Api/ImGuiGradients.cs`, radio wrapper | Med | M3 | Pending |
| C10 | imgui_toggle end-to-end (vendor, shim, wrapper) | Vertical slice | `vendor/imgui_toggle/`, `src/shims/imgui_toggle_shim.*`, `Interop/ExtensionShimsNative.cs`, build scripts | Med | M3 | Pending |
| C11 | ImPlot vendor setup + cimplot regen | Foundation | `vendor/{implot,cimplot}/`, `PIN_RECORD.md`, 3 build scripts | High | M4 | Pending |
| C12 | ImPlot context lifecycle | Consumer | `src/ContextHost.cpp/.h` | Low | M4 | Pending |
| C13 | ImPlot managed wrapper (spans + pinning) | Consumer | `Interop/ImPlotNative.cs`, `Application/Api/ImGuiPlot.cs`, demo proof window | High | M4 | Pending |
| C14 | Tween engine | Vertical slice | `Application/Animation/{Ease,Tween,TweenEngine}.cs`, `TweenEngineTests.cs`, frame-loop hook | Med | M5 | Pending |
| C15 | Knobs + wheels end-to-end | Vertical slice | `vendor/{imgui-knobs,imgui-wheels}/`, `src/shims/*`, wrappers | Med | M5 | Pending |
| C16 | Spinners end-to-end (cimspinner regen, curated enum) | Vertical slice | `vendor/{imspinner,cimspinner}/`, `Interop/ImSpinnerNative.cs`, spinner wrapper | Med | M5 | Pending |
| C17 | ThemeDemo showcase tab | Consumer | `DearImGuiKSPDemo/ThemeDemo.cs`, `DemoConsumer.cs` | Med | M5 | Pending |
| C18 | Telemetry foundation: ring buffer + sampler + addon skeleton | Foundation | `Telemetry/{RingBuffer,TelemetrySampler,TelemetryAddon}.cs` | Med | M6 | Pending |
| C19 | Graph panel (2x2 rolling plots) | Consumer | `Telemetry/GraphPanel.cs` | Med | M6 | Pending |
| C20 | Stage analyzer + stage panel | Consumer | `Telemetry/{StageAnalyzer,StagePanel}.cs` | High | M6 | Pending |
| C21 | Orbit panel (radar + elements readout) | Consumer | `Telemetry/OrbitPanel.cs` | High | M6 | Pending |
| C22 | Docs set A: getting-started, api-fundamentals, widgets, theming | Vertical slice | `docs/00,10,20,30` | Med | M7 | Pending |
| C23 | Docs set B: plotting, animation, migration, troubleshooting | Vertical slice | `docs/40,50,60,70` | Med | M7 | Pending |
| C24 | XML-doc sweep + README/LICENSE attribution | Cleanup | all new public members, `README.md`, `LICENSE` | Low | M7 | Pending |
| C25 | Release packaging + D33 OpenGL decision record | Integration | release zip, `DECISION_LOG.md` | Med | M8 | Pending |

### Dependency Graph
```
M1:  C2 ──► C1 ──► C3
M2:  C4 ──► C5 ──► C6        C7 (independent of C4–C6; pure asset copy)
M3:  C8 ──► C9               C10 (independent of C8/C9 except M1 bindings)
M4:  C11 ─► C12 ─► C13
M5:  C14 (independent within M5)   C15, C16 (independent of each other)
     C9, C10, C14, C15, C16 ──► C17
M6:  C18 ──► C19 (needs C13)     C18 ──► C20, C21 (C20/C21 parallel-safe)
M7:  C22, C23 (parallel-safe) ──► C24
M8:  C25 (needs M1–M7 verified)
```
- Arrow = hard dependency ("must be completed before").
- Milestones remain strictly sequential per spec §12: no M(N) chunk starts until M(N−1) is verified, even where intra-wave parallelism is possible (e.g., C14 has no technical dependency on M4).

### Interface Contracts Between Chunks
| From | To | Contract | Rationale |
|------|----|----------|-----------|
| C2 | C1, C3, C8, C9, C10 | Binding pattern: extern in `ImGuiNative.cs` + safe wrapper in `ImGuiInternal.cs`, enum values cited to imgui.h | Single seam for all new cimgui surface |
| C1 | C3, C8, C13, C15–C17 | `Vector2`/`Color`/`Color32` in public signatures; Application references CoreModule only | D24 amendment scope |
| C3 | all consumers | `using (ImGuiEx.Window(name)) { }` — Dispose closes stack on exception | Fault-barrier-friendly symmetry |
| C4 | C5 | `DearImGuiKSPNative_LoadFontFromFile(utf8 path, float sizePixels) → int` (0 = ok); `GetVersion() = 5` | Lockstep handshake (D17/D29) |
| C6 | C5 | FontResolver result: resolved absolute TTF path or fallback signal | Fallback decision lives in one place |
| C8 | C9, C10, C17 | ThemeEngine exposes active preset + gradient params (`DearImGuiKSP.CurrentTheme` read API) | Consumers compose on global style |
| C13 | C19 | `using (ImGuiPlot.Begin(title, size)) { ImGuiPlot.PlotLine(label, ReadOnlySpan<float>, ...) }` | Zero-alloc plot submission |
| C14 | C17, C20 | `Tween.To(Action<float>, from, to, seconds, Ease) → handle (Cancel, IsPlaying)` | Fire-and-forget animation |
| C18 | C19, C20, C21 | RingBuffer fixed-capacity API (push + span read); sampler channel set (altitude, q, throttle, G) | Warm history, zero steady-state alloc |

### Milestone Mapping
| Milestone | Chunks Advancing It | Verification Gate |
|-----------|---------------------|-------------------|
| M1 Ergonomics | C1, C2, C3 | Build green; 59 tests green; demo window via new API in-game; scope Dispose on injected exception |
| M2 Fonts | C4, C5, C6, C7 | Native builds (3 scripts) + harness; Plex Sans in-game; missing TTF → fallback + log; v4-native/v5-managed → mismatch popup |
| M3 Theme | C8, C9, C10 | Theme value-table tests green; in-game visual pass vs `UIStylingRef.png` (user sign-off); "dark" regression; live theme switch |
| M4 ImPlot | C11, C12, C13 | Native builds + harness; two-plot in-game window; zero per-frame alloc (benchmark) |
| M5 Widgets+tween | C14, C15, C16, C17 | Tween tests green; widgets animate in demo; suspension pauses tweens |
| M6 Showcase | C18, C19, C20, C21 | In-flight D16 acceptance: 60 Hz graphs, stage/Δv, orbit radar; zero-alloc hot path; placeholders outside flight |
| M7 Docs | C22, C23, C24 | Modder-from-zero dry run; XML docs complete; LICENSE aggregates MIT/0BSD/OFL |
| M8 Packaging | C25 | Clean-KSP install of zip; D33 decision recorded |

### Cross-Cutting Concerns
- Plan-specific invariants from `PLAN_DIGEST.md` apply to every chunk (D16 compatibility, lockstep versioning, layering/D24 scope, failure handling, hot-path budget, docs discipline).
- Native interop & hot-path checklist (CORE_PROTOCOLS §5.9) mandatory for: C4, C5, C11, C12, C13, C18, C19.
- Every native TU addition touches **3 build scripts** (`build.bat`, `build_release.bat`, `build_harness.bat`) — never just one.
- cimplot/cimspinner are **regenerated against the pinned cimgui**, never copied blind; generator commits recorded in `PIN_RECORD.md`.
- XML `///` docs on every new public member in the same chunk that creates it (not deferred to C24; C24 is the sweep).
- Existing 59-test suite must stay green after every chunk.

### Chunking Decisions & Rationale
- **C1 vs C2 split**: signature upgrade (facade-only) is low-risk and compile-verifiable; new externs are the error-prone part (hand-verified enum/marshal work) — isolating them keeps diffs reviewable.
- **C4/C5 split at the ABI boundary**: native and managed sides of handshake v5 are verified differently (harness vs in-game mismatch test); the export signature is the frozen contract between them.
- **C7 standalone**: pure asset copy with its own check (files mirror into the game), independent of code.
- **C8 is the biggest managed chunk** (engine + presets + palette + default change) but is one cohesive concern: "apply a named style table." Splitting presets from the engine would force a half-built engine.
- **C10/C15/C16 are vertical slices per extension**: vendor → shim → wrapper in one chunk each, because the ABI decisions are per-extension and partial slices leave dead files.
- **C14 (tween) has zero native work** — deliberately placed first in M5 so its xUnit verification gates the milestone's riskier native chunks.
- **C20/C21 parallel-safe**: disjoint files, both consume C18's stable contracts; C19 is separated because it is the first real consumer of C13's plot API and deserves isolated verification.
- **C22/C23 split by doc file** for parallelism; C24 sequenced after both so the sweep covers final API shapes.
- **C25 is the only M8 chunk**: packaging is atomic (a partial zip is worse than none) and ends with the D33 decision record.
