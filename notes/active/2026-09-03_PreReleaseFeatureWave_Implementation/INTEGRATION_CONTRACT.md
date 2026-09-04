# Integration Contract: DearImGui-KSP Pre-Release Feature Wave Implementation
## Date: 2026-09-03
## Chunk Map Reference: `notes/active/2026-09-03_PreReleaseFeatureWave_Implementation/CHUNK_MAP.md`

### Execution Order
| Order | Chunk ID | Name | Why This Position | Parallel Group |
|-------|----------|------|-------------------|----------------|
| 1 | C2 | Binding foundation | All M1+ work consumes the binding pattern; lowest-level, compile-verifiable | - |
| 2 | C1 | Unity math types (D24) | Facade signature upgrade on top of stable bindings | - |
| 3 | C3 | Scope wrappers | Consumes C1 signatures + C2 bindings; completes M1 | - |
| 4 | C6 | Font settings + FontResolver | Produces the resolved-path contract C5 consumes; no native dependency | - |
| 5 | C4 | Native font export + v5 | Native side of the ABI; harness-verifiable before managed wiring | A |
| 6 | C7 | Bundle font assets | Pure asset copy, no code dependency; any time in M2 | A |
| 7 | C5 | Managed bridge + startup wiring | Last piece of M2: consumes C4 export + C6 resolver | - |
| 8 | C8 | Theme engine + presets | Core of M3; C9 composes on its gradient params | - |
| 9 | C9 | Gradient helpers + circular radio | Needs C8's active-preset/gradient contract | B |
| 10 | C10 | imgui_toggle end-to-end | Independent of C8/C9 (only M1 bindings); parallel-safe — disjoint files | B |
| 11 | C11 | ImPlot vendor + cimplot regen | Riskiest chunk; isolated so generator drift blocks nothing else | - |
| 12 | C12 | ImPlot context lifecycle | Needs vendored headers to compile | - |
| 13 | C13 | ImPlot managed wrapper | Consumes C12 context + cimplot exports | - |
| 14 | C14 | Tween engine | Zero native work; xUnit-gated before M5's native slices | - |
| 15 | C15 | Knobs + wheels | Native slice; sequential with C16 — both edit the 3 build scripts and `ExtensionShimsNative.cs` | - |
| 16 | C16 | Spinners | After C15 (shared build-script file lists) | - |
| 17 | C17 | ThemeDemo showcase tab | Consumes C9, C10, C14, C15, C16 | - |
| 18 | C18 | Telemetry foundation | Ring buffer + sampler + addon skeleton with tab slots | - |
| 19 | C19 | Graph panel | First real consumer of C13's plot API — isolated verification | - |
| 20 | C20 | Stage analyzer + panel | Consumes C18 contracts; disjoint files from C21 | C |
| 21 | C21 | Orbit panel | Consumes C18 contracts; disjoint files from C20 | C |
| 22 | C22 | Docs set A | Disjoint files from C23 | D |
| 23 | C23 | Docs set B | Disjoint files from C22 | D |
| 24 | C24 | XML-doc sweep + attribution | After both doc sets so the sweep sees final API shapes | - |
| 25 | C25 | Release packaging + D33 | Atomic; requires M1–M7 verified | - |

Milestones stay strictly sequential: no chunk of M(N) starts until M(N−1)'s verification gate passes.

### Stub / Scaffolding List
| Stub | Location | Replaced By | Remove In |
|------|----------|-------------|-----------|
| Placeholder tab content in TelemetryAddon ("Graphs/Stages/Orbit" tabs render a placeholder `Text` line each) | `DearImGuiKSPDemo/Telemetry/TelemetryAddon.cs` (C18) | Real panels | C19, C20, C21 respectively — each chunk replaces its own tab's placeholder |
| Bootstrap-session placeholder readmes | `Application/{Theming,Animation,Api}/README.md`, `Telemetry/README.md`, `src/shims/README.md`, `docs/*` stubs | Real files/docs | First chunk that lands real content in each folder removes that folder's placeholder (C8, C14, C3, C18, C10, C22/C23) |
| `vendor/PIN_RECORD.md` `_pending_` rows | `DearImGuiKSPNative/vendor/PIN_RECORD.md` | Real pins | C10 (toggle), C11 (implot/cimplot), C15 (knobs/wheels), C16 (spinner/cimspinner) |

No interface stubs needed anywhere else — chunk order guarantees every consumer's contract exists before the consumer starts.

### Inter-Chunk Contracts (Locked)
| Contract Element | Definition | Owner | Consumers |
|-------------------|------------|-------|-----------|
| Binding pattern | New extern declared in `Interop/ImGuiNative.cs` (DllImport, Cdecl, bool as I1) + safe internal wrapper in `ImGuiInternal.cs`; enum values cited to the exact header line | C2 | C1, C3, C8, C9, C10, C13, C15, C16 |
| `DearImGuiKSPNative_LoadFontFromFile` | `extern "C" int (const char* utf8Path, float sizePixels)`; 0 = success; called once before first NewFrame; native `GetVersion()` returns 5 | C4 | C5 |
| FontResolver result | Resolved absolute TTF path + pixel size, or fallback signal (null) meaning "load embedded default + log" | C6 | C5 |
| Scope wrappers | `ImGuiEx.Window(string) : IDisposable`; Dispose calls the matching End even on exception; one wrapper per Begin/End pair (Window, Child, Plot in M4) | C3 | all consumers, demo |
| ThemeEngine surface | `DearImGuiKSP.CurrentTheme` (read); active preset exposes gradient params (button top/bottom, window-bg top/bottom) for helpers/widgets | C8 | C9, C10, C17 |
| `ImGuiPlot` wrapper | `Begin(title, Vector2 size) : IDisposable`; `PlotLine(string label, ReadOnlySpan<float> ys, ...)` pins the span inside the call — zero copies, zero per-frame allocation | C13 | C19 |
| Tween API | `Tween.To(Action<float> set, float from, float to, float seconds, Ease ease) → TweenHandle { Cancel(), IsPlaying }`; Color overload; ticked by the library frame loop; paused on suspension | C14 | C17, C20 |
| RingBuffer | Fixed capacity (10k) `float` ring; `Push(float)`, `GetReadSpan()` or equivalent zero-alloc read; sampling continues while window closed | C18 | C19, C20, C21 |
| Sampler channels | altitude, dynamicPressurekPa, mainThrottle, geeForce — pushed once per frame in flight scene only | C18 | C19 |
| Telemetry tab slots | TelemetryAddon owns window + tab bar + toolbar; each panel class owns exactly its tab's rendering; panels degrade to `No active vessel.` placeholder | C18 | C19, C20, C21 |

### Build/Test Sequence
| After Chunk | Verification |
|-------------|--------------|
| C2 | `dotnet build` green; 59/59 tests pass |
| C1 | `dotnet build` green (CoreModule reference resolves); 59/59 tests pass |
| C3 | Build + tests green; in-game: demo window through scope wrappers; injected exception leaves stack closed — **M1 gate** |
| C6 | Build + tests green incl. new settings/font-normalization cases |
| C4 | All 3 native builds green; harness prints HARNESS PASS; native reports version 5 |
| C7 | Fonts + OFL.txt mirrored into game `GameData/DearImGuiKSP/Fonts/` after managed build |
| C5 | In-game: Plex Sans renders; missing TTF → ProggyClean + one log line; v4-native/v5-managed → version-mismatch popup path — **M2 gate** |
| C8 | Build + theme value-table tests green; "ksp" applied by default in-game; live switch to "dark" = exact stock ImGui dark |
| C9, C10 | In-game: gradient buttons/window bg, circular radios, animated toggles; user visual sign-off vs `UIStylingRef.png` at uiScale 1.0 — **M3 gate** |
| C11 | All 3 native builds green with vendored sources; `PIN_RECORD.md` rows filled incl. generator commit |
| C12 | Harness PASS with ImPlot context created/destroyed |
| C13 | In-game two-plot window; benchmark shows no managed-cost regression; zero per-frame alloc — **M4 gate** |
| C14 | Tween/easing xUnit green (incl. cancel, completion removal, suspension pause) |
| C15, C16 | Native builds green; knobs/wheels/spinners render in-game |
| C17 | All widgets + animations live in demo window — **M5 gate** |
| C18 | In-flight: sampler pushes channels; placeholder tabs render; no alloc at steady state |
| C19 | 2x2 rolling graphs with legends/tooltips at 60 Hz |
| C20, C21 | Stage/Δv panel (with `approx dV` label) + orbit radar live in flight; `No active vessel.` outside flight; full D16 gates — **M6 gate** |
| C22, C23 | Modder-from-zero dry run against docs alone |
| C24 | XML docs on every new public member; LICENSE aggregates MIT/0BSD/OFL — **M7 gate** |
| C25 | Zip installs into clean KSP and runs in D16 environment; D33 decision recorded — **M8 gate** |

### Rollback Plan
- Git checkpoint (commit or stash) before each chunk starts.
- If a chunk fails verification, revert only that chunk's files and re-run the previous chunk's verification to confirm a clean baseline.
- If a native-side chunk (C4, C11, C12, C15, C16) fails, the previous native DLL in `GameData/DearImGuiKSP/PluginData/` remains the known-good binary — rebuild it from the checkpoint rather than shipping a partial.
- If cimplot/cimspinner regeneration drifts (C11/C16), do not hand-patch generated output: either re-pin the generator or fall back to the documented hand-written ABI subset (spec §11), recorded as a plan deviation in IMPEDIMENTS.md.
- Downstream chunk discovers an upstream bug: stop, document in IMPEDIMENTS.md, patch upstream, re-verify upstream before resuming.
