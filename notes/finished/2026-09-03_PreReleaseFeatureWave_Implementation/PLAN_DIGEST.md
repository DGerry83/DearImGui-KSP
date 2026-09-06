# Plan Digest: DearImGui-KSP Pre-Release Feature Wave Implementation
## Date: 2026-09-03
## Source Plan: `notes/active/2026-09-03_NewProject_PreReleaseFeatureWave/IMPLEMENTATION_PLAN.md`

Session type token: PlanName = `PreReleaseFeatureWave`. This session executes the plan
produced by the bootstrap session `notes/active/2026-09-03_NewProject_PreReleaseFeatureWave/`.

### Goals (one-line summary each)
1. API ergonomics foundation — Vector2/Color signatures, IDisposable scope wrappers, style push/pop bindings (M1).
2. Font pipeline — bundled IBM Plex Sans, `LoadFontFromFile` export, handshake v5, ProggyClean fallback (M2).
3. KSP theme — preset engine, palette, gradients, circular radios, animated toggles; "ksp" default (M3).
4. ImPlot integration — vendor + cimplot regen + context + C# wrapper (M4).
5. Widget extensions + tween — knobs, wheels, spinners, C# tween engine (M5).
6. Telemetry showcase — Graphs/Stages/Orbit flight-scene window in the demo mod (M6).
7. Documentation — 8-file docs/ set, XML-doc sweep, LICENSE attribution (M7).
8. Release packaging + OpenGL decision point (M8, D33).

### Structural Sections (plan milestones)
| Section | Summary | Files/Records Touched | Risk Level |
|---------|---------|----------------------|------------|
| M1 Ergonomics | Public API shape change (D24); all later work builds on it | `DearImGuiKSP.cs`, `Application/Api/ImGuiEx.cs`, `Interop/ImGuiNative.cs`, `ImGuiInternal.cs`, csproj (CoreModule ref) | Med — touches the public facade |
| M2 Fonts | First managed→native path string; handshake bump | `DearImGuiKSPNative.cpp`, `ContextHost.cpp/.h`, `NativeBridge.cs`, `INativeBridge.cs`, `FontResolver.cs`, settings, `Fonts/` assets | Med — lockstep version bump both sides |
| M3 Theme | Global style application + gradient helpers + toggle shim | `Application/Theming/*`, `Api/ImGuiGradients.cs`, `vendor/imgui_toggle`, `src/shims/imgui_toggle_shim.*`, build scripts | Med — visual sign-off gate |
| M4 ImPlot | Largest single chunk: vendor setup + cimplot regen + context lifecycle + wrapper | `vendor/{implot,cimplot}`, `PIN_RECORD.md`, `ContextHost`, `Interop/ImPlotNative.cs`, `Api/ImGuiPlot.cs`, build scripts | High — generator drift risk, span pinning hot path |
| M5 Widgets+tween | Shims + wrappers + C# tween engine | `vendor/{imgui-knobs,imgui-wheels,imspinner,cimspinner}`, `src/shims/*`, `Interop/*Native.cs`, `Api/ImGuiWidgets.cs`, `Application/Animation/*`, `ThemeDemo.cs` | Med — wheels immaturity |
| M6 Showcase | Demo-only, public API only | `DearImGuiKSPDemo/Telemetry/*` | Med — D16 gates, zero-alloc hot path |
| M7 Docs | Markdown docs set + XML-doc sweep | `docs/*`, XML docs, README, LICENSE | Low |
| M8 Packaging | Release zip + D33 decision | release artifacts, DECISION_LOG | Low |

### Files Requiring Changes
| File | Change Type | Plan Section | Depends On |
|------|-------------|--------------|------------|
| `DearImGuiKSP/DearImGuiKSP.cs` | Modify (extend public facade) | M1, M3–M5 | — |
| `DearImGuiKSP/Application/Api/*.cs` (4 files) | Add | M1/M3/M4 | — |
| `DearImGuiKSP/Interop/ImGuiNative.cs`, `ImGuiInternal.cs` | Modify | M1, M3 | — |
| `DearImGuiKSP/DearImGuiKSP.csproj` | Modify (UnityEngine.CoreModule ref, D24) | M1 | — |
| `DearImGuiKSPNative/src/ContextHost.cpp/.h` | Modify | M2, M4 | — |
| `DearImGuiKSPNative/src/DearImGuiKSPNative.cpp` | Modify (v5, LoadFontFromFile) | M2 | — |
| `DearImGuiKSP/Infrastructure/NativeBridge.cs`, `Application/Interfaces/INativeBridge.cs` | Modify | M2 | — |
| `DearImGuiKSP/Infrastructure/FontResolver.cs` | Add | M2 | — |
| `DearImGuiKSP/Infrastructure/SettingsStore.cs`, `Application/SettingsModel.cs`, `LibraryConfig.cs` | Modify | M2, M3 | — |
| `GameData/DearImGuiKSP/Fonts/` (2 TTF + OFL.txt) | Add (assets) | M2 | — |
| `DearImGuiKSP/Application/Theming/*.cs` (3 files) | Add | M3 | M1 |
| `DearImGuiKSPNative/vendor/**`, `PIN_RECORD.md` | Add (vendored sources) | M3 (toggle), M4, M5 | — |
| `DearImGuiKSPNative/src/shims/*.cpp/.h` | Add | M3 (toggle), M5 | vendor |
| `DearImGuiKSPNative/build*.bat` (3 files) | Modify (file lists) | M3, M4, M5 | vendor |
| `DearImGuiKSP/Interop/ImPlotNative.cs`, `ImSpinnerNative.cs`, `ExtensionShimsNative.cs` | Add | M4, M5 | native exports |
| `DearImGuiKSP/Application/Animation/*.cs` (3 files) | Add | M5 | M1 |
| `DearImGuiKSPDemo/ThemeDemo.cs`, `Telemetry/*.cs` (7 files) | Add | M5, M6 | M1–M5 |
| `tests/Application.Tests/Animation/*`, `Theming/*`, `SettingsModelTests.cs` | Add/Modify | M2, M3, M5 | respective chunks |
| `docs/*.md` (8 stubs → full) | Modify | M7 | M1–M6 |
| `README.md`, `LICENSE` | Modify (attribution) | M7 | vendor pins |

### New Records / Assets
| Asset ID | Type | Purpose | Defined In Plan Section |
|----------|------|---------|------------------------|
| DK_FontPlexRegular / DK_FontPlexMedium | Font (TTF) | Default UI font | §8.1 (spec) |
| DK_FontOFL | License text | OFL obligation | §8.1 |
| DK_VendoredSrc | Vendored code + pin record | Extension widgets | §8.1, §4.2 |
| DK_Docs | Markdown docs | Modder onboarding | §8.2 |
| settings.cfg `font` key | Config | Font selection | §9 |
| settings.cfg `theme` default "ksp" | Config | Theme default (D25) | §9 |

### External Dependencies
| Dependency | Required? | How Verified |
|------------|-----------|--------------|
| Sibling cimgui clone (`~/source/repos/cimgui`, imgui 1.92.9) | Yes | Present — `imgui/imgui.h` confirmed |
| IBM Plex Sans static TTFs (`~/source/repos/Fonts/IBM_Plex_Sans/static/`) | Yes | Present — Regular + Medium confirmed |
| ImPlot v1.0 tag, imgui-knobs, imgui-wheels, imspinner+cimspinner, imgui_toggle | Yes (M3–M5) | To be fetched/vendored at M3–M5; pins recorded in `vendor/PIN_RECORD.md` |
| cimplot/cimspinner generators | Yes (M4/M5) | Run against pinned cimgui at vendor setup; generator commit recorded |
| dotnet SDK / cl.exe | Yes | dotnet 10.0.301 verified; cl.exe assumed per environment cache |
| Baseline build + tests | Yes | `dotnet build` green (0 warnings); `dotnet test` 59/59 passed (2026-09-03) |

### Risk Flags
- **Handshake v5 lockstep (M2)**: managed `ExpectedNativeVersion` and native `GetVersion()` must bump together; mismatch path must be verified with a mismatched pair, not assumed.
- **cimplot/cimspinner regeneration (M4/M5)**: generator drift vs pins is the wave's highest technical risk; build must fail loud, and the fallback (hand-written ABI subset) is documented.
- **imgui-wheels immaturity (M5)**: droppable pre-release if it misbehaves (spec §11).
- **M3 visual sign-off**: palette constants are starting values (spec §13); theme milestone is not verified until the user confirms the look in-game.
- **Hot-path allocation (M4/M6)**: plot submission and telemetry sampling must hold zero steady-state managed allocation; benchmark window is the regression instrument.
- **D16 environment gates (every milestone)**: no disturbance to Deferred/TUFX/Scatterer/Parallax/Cinematic mods or the ISSUES #001/#003 click-blocking mechanisms.

### Open Questions
- None blocking. All spec questions resolved (D24–D34). OpenGL timing is decided at M8 by design (D33).

---

## Plan-Specific Invariants (filled for this codebase — always enforced)

1. **D16 compatibility**: no change may break Deferred, TUFX, Scatterer, Parallax, Cinematic Shaders, Cinematic Recorder, or IMGUI mods; the uGUI/IMGUI click-blocking mechanisms (ISSUES #001/#003) stay inert-when-not-capturing.
2. **Lockstep versioning (D17)**: managed and native version constants bump together; a mismatch must hit the existing version-mismatch failure path, never limp along.
3. **Layering**: Infrastructure → Application → Core direction holds; Application references UnityEngine.CoreModule for `Vector2`/`Color`/`Color32` **only** (D24); the demo consumes the public API only — never internals.
4. **Failure handling**: unrecoverable startup failure → session-permanent self-disable + one plain-language popup; technical detail in the log under `[DearImGuiKSP]` only; font/theme degradation is log-only, never a failure-mode trigger.
5. **Hot-path budget**: < 1 ms managed frame cost, zero steady-state per-frame managed allocation in telemetry/plot/tween paths (CORE_PROTOCOLS §5.9 checklist applies to every chunk touching them).
6. **Docs discipline**: XML `///` docs on every new public member; no emojis or symbol glyphs in docs (D31); library persists only its own `settings.cfg` — consumer state belongs to consumers.
