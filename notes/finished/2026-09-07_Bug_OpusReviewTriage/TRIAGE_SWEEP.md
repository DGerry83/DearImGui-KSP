# TRIAGE SWEEP — External (Claude Opus) Review, Organized & Assessed

**Session:** `notes\active\2026-09-07_Bug_OpusReviewTriage\`
**Date:** 2026-09-07
**Source:** Friend-commissioned Claude Opus review (~270 items: bugs, architecture/design concerns, review items). Received verbatim from the user; the review's severity grades are its own verifier grades.
**Method:** The 3 severe items were verified directly by the main agent. All remaining items were verified by an 8-area read-only sub-agent swarm against the cited code (one hop allowed), then classified. The review's "recorded in notes" claims were checked against `ISSUES\TRACKER.md` and `notes\`.

## Verdict / class legend

- **VALID** — mechanism confirmed in code as described. **PARTIAL** — real but materially mis-described. **INVALID** — claim wrong, unreachable, or already handled. **UNCERTAIN** — hinges on runtime behavior not statically checkable (says what).
- **WORK** — should be changed/fixed. **NOTE** — deliberate decision, accepted tradeoff, latent/unreachable, or observation not warranting change.
- Duplicates are merged into the primary item (`= Xn`).

## Summary stats

| Group | Items (after dedup) | WORK | NOTE | INVALID | UNCERTAIN |
|---|---|---|---|---|---|
| 1. Severe | 3 | 3 | 0 | 0 | 0 |
| 2. Medium (non-docs) | 25 | 16 | 5 | 2 | 2 |
| 3. Low (non-docs) | ~90 | 47 | ~40 | 1 | 3 |
| 4. Nit (non-docs) | ~70 | 6 | ~62 | 2 | 0 |
| 5. Documentation | ~45 | ~33 | ~11 | 1 | 0 |

---

## GROUP 1 — SEVERE (fix first; all VALID, all WORK)

All three sit at the same seam: the boundary between consumer-mod code and the shared frame.

### S1 — Register/Unregister inside a consumer's own callback hard-hangs KSP
- **Verified:** `FrameLoopOrchestrator.RunFrame` iterates `_registry.Ordered` (`FrameLoopOrchestrator.cs:112`), which returns the **live** `List<>` (`ConsumerRegistry.cs:33`). A consumer calling `Register`/`Unregister` inside its callback mutates the list; the enumerator's next `MoveNext` throws `InvalidOperationException` **outside** the FaultBarrier (the barrier wraps `_faultBarrier.Invoke(consumer)`, not the enumerator). `EndUiFrame()` (`:116`) has no try/finally and is skipped. Native side confirmed (`ContextHost.cpp:129`): the frame guard holds the SRWLOCK from BeginFrame to EndFrame — a skipped EndFrame deadlocks the game thread **and** Unity's render thread.
- **Review fix:** try/finally around the frame + iterate a snapshot (or deferred register/unregister queue).

### S2 — Widget call from outside a frame callback crashes KSP to desktop
- **Verified:** `IsAvailable` (`DearImGuiKSP.cs:43`) is a session predicate (Running || Suspended), not a frame-open predicate. Every facade widget gate uses it, so a call from e.g. a consumer's own Update reaches cimgui with no open frame/current window; the release DLL is built `/DNDEBUG`, so ImGui derefs null instead of asserting. Docs promise a safe no-op ("no-op/undefined, never an exception", facade class doc).
- **Review fix:** a frame-open flag (set by the orchestrator between BeginUiFrame/EndUiFrame) in the gate. Subsumed items: the review's `DearImGuiKSP.cs:43` bug entry is this same item.

### S3 — Throwing tween setter permanently freezes every consumer's UI
- **Verified:** `TweenEngine.Tick` (`TweenEngine.cs:110,115`) invokes setters with no exception guard (the try/finally only resets `_ticking`). The exception escapes through `RunFrame` (`FrameLoopOrchestrator.cs:88`, before BeginUiFrame — no native lock held, so no hang) to the addon's Update; the tween is never marked Done and **rethrows every frame forever**, starving the whole frame loop. Setters are the one consumer code path outside the FaultBarrier.
- **Review fix:** guard each setter invocation (catch → log → auto-complete/kill that tween), mirroring FaultBarrier semantics.

---

## GROUP 2 — MEDIUM-TIER (non-docs)

### WORK

| ID | Item | Location | Verdict note |
|---|---|---|---|
| G2-01 | Gradient pass mis-targets child windows (child decorations render into parent's draw list) | `ContextHost.cpp:405` | VALID; confirmed against imgui.cpp:8602 |
| G2-02 | cimgui/imgui pin not enforced — no commit verification despite PIN_RECORD's fail-loud rule | `build_release.bat:10` | PARTIAL (fail-loud rule governs vendor trees; over-graded but real gap) |
| G2-03 | Release native build emits no PDB — shipped-DLL crash addresses unsymbolisable | `build_release.bat:10` | VALID |
| G2-04 | No ImGui ErrorCallback: stock error tooltips paint red debug UI over the game; diagnostics never reach KSP.log | `ContextHost.cpp:79` | VALID |
| G2-05 | FaultBarrier restores no ImGui stack state — a throwing consumer mis-parents later consumers' widgets that frame | `FaultBarrier.cs:34` | VALID; only ImGuiEx using-scope consumers protected |
| G2-06 | io.DeltaTime and tweens use scaled `Time.deltaTime` — tweens freeze on pause, ImGui timing 4x fast under physics warp | `DearImGuiKSPAddon.cs:80` | VALID |
| G2-07 | RadioButton(string, ref bool) never assigns its ref parameter | `DearImGuiKSP.Radio.cs:23` | VALID; contradicts XML doc + docs/20 |
| G2-08 | GradientButton draws raw label — `##` ID suffixes render visibly, text mis-centred | `ImGuiGradients.cs:170` | VALID |
| G2-09 | ksp-theme InputText renders `##id` tails as visible text; theme-dependent ImGui ID (= A8) | `DearImGuiKSP.cs:211` | VALID |
| G2-10 | InputText ignores caller capacity; buf_size is the shared buffer's grown length (= A11) | `ImGuiInternal.cs:144` | VALID |
| G2-11 | Window titles are process-global identity across all mods — no namespacing, detection, or documentation | `DearImGuiKSP.cs:115` | VALID |
| G2-12 | Demo: virtualized-list RowHeight hard-coded 22px desyncs under uiScale/fontScale; docs recommend same constant | `BenchmarkUI.cs:21` | VALID; doc tie-in in Group 5 |
| G2-13 | Demo: radar ellipse rotated by argPe while markers drawn periapsis-aligned — worse: argPe (degrees) fed to Math.Cos | `OrbitPanel.cs:191` | VALID |
| G2-14 | Demo: inclination readout multiplies already-degrees `Orbit.inclination` by 180/pi | `OrbitPanel.cs:356` | VALID (KSP source: Orbit.cs:619/2618) |
| G2-15 | Demo: stage dV rocket equation omits upper-stage propellant from both masses | `StageAnalyzer.cs:187` | VALID; only labeled "approx". **CLOSED BY FEATURE REMOVAL 2026-09-07 (user decision):** after two fix rounds (C12, C12b) the demo's demo-scale dV math still diverged from stock's crossfeed-simulated values; the Stages/dV tab, StagePanel and StageAnalyzer were removed from the demo rather than chase parity (the findings live on in the KSP Knowledge Library `NOTES\stock-deltav-simulation.md`). G3-34/G3-35/G3-40 closed by the same removal |
| G2-16 | Release zip ships settings.cfg — extracting an upgrade silently resets player settings | `package_release.bat:49` | VALID |

### UNCERTAIN — need in-game verification before scheduling

**Resolved 2026-09-07 (Gate A, user in-game):** G2-U1 **closed — unreachable in practice** (there is no stock UX path to change scene while the UI is F2-hidden; latent only, C17 not scheduled). G2-U2 **closed — claim disproven** (maneuver-node gizmos do NOT receive clicks through an ImGui window in map view; the current lock mask suffices, C18 not scheduled).

| ID | Item | Location | Resolution |
|---|---|---|---|
| G2-U1 | F2-hidden state never resyncs on scene change → library silently Suspended for the session | `Composition.cs:185` | UNREACHABLE: no way to switch scenes without UI (user, Gate A 2026-09-07). Latent-only; no contract |
| G2-U2 | Mouse-capture lock mask omits MAP_UI/MANNODE bits — legacy collider input (maneuver gizmos) live under ImGui windows | `InputLockGateway.cs:81` | DISPROVEN: gizmos blocked correctly under ImGui windows (user, Gate A 2026-09-07). No contract |

### NOTE (valid but not work)

| ID | Item | Location | Rationale |
|---|---|---|---|
| G2-N1 | settings.cfg lives in GameData (upgrades reset it; MM parses it) | `LibraryConfig.cs:16` | Deliberate recorded decision (spec §4.4 / AGENTS constraint) |
| G2-N2 | Intermittently throwing consumers never auto-disabled; full stack trace every throwing frame | `FaultBarrier.cs:42` | Consecutive-failure reset is documented design (docs/70); review over-graded |
| G2-N3 | Level-polled mouse buttons drop sub-16ms clicks | `NativeBridge.cs:253` | Real but nit-grade impact; over-graded |
| G2-N4 | Consumer printf format strings reach native varargs unvalidated (Knob/Wheel) | `DearImGuiKSP.Knob.cs:122` | Standard ImGui varargs tradeoff; over-graded |
| G2-N5 | Public PlotLine takes KSP-mscorlib ReadOnlySpan | `ImGuiPlot.cs:157` | KSPBuildTools consumers unaffected; over-graded |

### INVALID (claim disproven)

| ID | Item | Verdict |
|---|---|---|
| G2-X1 | "ImGuiMod_Ctrl/Shift/Alt/Super never submitted" (`ContextHost.cpp:185`) | INVALID — Ctrl **is** fed (LeftCtrl/RightCtrl bits); docs disclose the Shift/Alt limitation |
| G2-X2 | "Blocker out-sorted: overlay raycasters compared by sortingOrder first" (`PointerBlockerGateway.cs:137`) | INVALID — RaycastComparer compares sorting **layer** before order; code is correct, claim reverses it |

---

## GROUP 3 — LOW-TIER (non-docs)

### WORK

| ID | Item | Location | Note |
|---|---|---|---|
| G3-01 | uiScale < 1.0 truncates every 1px style size to zero (borders/separators/caret vanish); MinScale 0.5 reachable | `ContextHost.cpp:505` | ScaleAllSizes ImTrunc; upstream warns itself |
| G3-02 | All three native build scripts hard-code one vcvars64 path, no fallback/errorlevel check | `build_release.bat:8` | |
| G3-03 | D3D11 backend bring-up failure silent, retried every frame, clears RendererHasTextures | `BackendD3D11.cpp:68` | |
| G3-04 | Shipped .version inherits KSPBuildTools 1.8–1.12 defaults — advertises compatibility never had | `DearImGuiKSP.csproj:26` | GameData .version KSP_VERSION_MIN 1.8 |
| G3-05 | AVC URL points at repo HTML page; KSP-AVC needs a raw .version URL | `DearImGuiKSP.version:3` | |
| G3-06 | Stale `INativeBridge.RebuildViewport` doc text advertises unfinished C12 work (shipped intentional no-op) | `INativeBridge.cs:55` | XML doc comment fix |
| G3-07 | docs/50 claims null setter is the only Tween.To throw; immediate baseline `set(from)` unguarded (`Tween.cs:50,84`) | `Tween.cs:50` | Code guard + doc fix (see Group 5) |
| G3-08 | Slider drag rewrites settings.cfg from scratch every changed frame — no debounce | `SettingsModel.cs:44` | Shared fix with G3-09, G3-19, G3-20 |
| G3-09 | uiScale drag rebuilds/re-applies whole theme + rewrites settings.cfg every changed frame | `ThemeEngine.cs:65` | |
| G3-10 | IBM Plex Medium atlas-loaded but unreachable — no PushFont API anywhere | `FontResolver.cs:50` (= I26) | DECIDED 2026-09-07: drop the dead load. Adding a font-selection/PushFont API is a SemVer-minor public feature (D17/D36 lockstep) and out of scope for a patch-class remediation wave; if ever wanted it goes through DesignSpecRefinement as 1.1.0 work. C10 implements the removal |
| G3-11 | NativeBridge class doc claims all native funcs GetProcAddress-bound; Interop's 87 DllImports contradict | `NativeBridge.cs:13` | Comment fix |
| G3-12 | Render pump coroutine starts even after failed init; per-frame WaitForEndOfFrame alloc (= I51) | `DearImGuiKSPAddon.cs:62` | Two trivial fixes |
| G3-13 | ApplyLocks allocates a fresh HashSet<string> every frame even when nothing captures | `InputLockGateway.cs:20` | Already queued: fix backlog P1 |
| G3-14 | Settings panel writes settings.cfg to disk every slider-drag frame, inside the held frame lock | `LibraryControlPanel.cs:105` | Shared fix with G3-08 |
| G3-15 | Stale log line: GraphicsApi failure still says "milestone 2 PoC", promises GL "arrives in C5" | `NativeBridge.cs:134` | |
| G3-16 | Non-atomic synchronous settings.cfg rewrite per slider frame | `SettingsStore.cs:127` | Shared fix with G3-08 |
| G3-17 | Spinner throws IndexOutOfRange on out-of-range SpinnerType (breaks no-throw widget contract) | `DearImGuiKSP.Spinner.cs:213` | FaultBarrier catches, but contract broken |
| G3-18 | InputText seed copy clamps on raw bytes — can split a UTF-8 sequence into persisted U+FFFD | `ImGuiInternal.cs:144` | |
| G3-19 | (merged into G3-08) | | |
| G3-20 | Empty-string label/id silently collides with window's own ImGui ID; docs promise a guard | `ImGuiInternal.cs:710` | Only Spinner guards today |
| G3-21 | RadioButton always draws KSP-palette rim — breaks dark theme's byte-exact-stock guarantee | `DearImGuiKSP.Radio.cs:24` | |
| G3-22 | BeginSubplots forwards unvalidated rows/cols; ImPlot's only guard compiled out by /DNDEBUG → UB | `ImGuiPlot.cs:85` (= A33) | |
| G3-23 | Public COUNT enum member; PushStyleColor forwards unchecked — OOB write past style.Colors, no guard even in debug | `ImGuiStyleEnums.cs:141` (= A35) | |
| G3-24 | PushStyleVar forwards unchecked ImGuiStyleVar (COUNT included) — GetStyleVarInfo OOB in release | `DearImGuiKSP.cs:458` (= A37) | Same guard family as G3-23 |
| G3-25 | ImGuiDraw.AddText XML doc calls pos a baseline anchor; ImDrawList::AddText treats top-left | `ImGuiDraw.cs:175` | XML doc fix |
| G3-26 | GradientButton measures label twice per frame in fit-to-label path | `ImGuiGradients.cs:169` | |
| G3-27 | Pure helpers Pack/Lerp/Lighten, FailureText.BodyFor, KindForInitResult have no tests | `ImGuiGradients.cs:250` | All testable |
| G3-28 | InputText label drawn LEFT under ksp theme, RIGHT under dark — undocumented layout difference | `DearImGuiKSP.cs:211` | Doc fix component in Group 5 |
| G3-29 | ToUtf8 allocates two byte arrays per label on per-widget per-frame path | `ImGuiInternal.cs:708` | |
| G3-30 | SliderFloat omits AlwaysClamp — type-in entry reachable NOW (Ctrl is forwarded; review's "blocked by missing Ctrl merge" was wrong) | `ImGuiInternal.cs:68` | PARTIAL, upgraded to live issue |
| G3-31 | ImSpinnerColor doc cites cimgui ImColor_c; actual cimspinner ABI type is C++ imgui::ImColor | `ImSpinnerNative.cs:7` | Comment fix |
| G3-32 | Stale citation: SpinnerAng8 defaults at imspinner.h:410, not :387 | `ImSpinnerNative.cs:167` | Comment fix |
| G3-33 | Demo: toggling IMGUI reference virtualization mid-pass throws one-frame layout ArgumentException | `BenchmarkUI.cs:183` | Classic Layout/Repaint mismatch |
| G3-34 | Demo: multi-mode engines — both modes summed into stage thrust/Isp/mass flow | `StageAnalyzer.cs:191` | |
| G3-35 | Demo: locked tanks (flowState false) counted as usable propellant | `StageAnalyzer.cs:225` | |
| G3-36 | Demo: telemetry rings never cleared on active-vessel change | `TelemetrySampler.cs:51` | |
| G3-37 | Demo: release zip ships only DLL + .version, no License.txt/Readme.txt | `DearImGuiKSPDemo.csproj:38` | |
| G3-38 | Demo: neither .version emits KSP_VERSION — AVC/CKAN cannot gate to 1.12.x | `DearImGuiKSPDemo.version:1` | Related to G3-04 |
| G3-39 | Demo: PlotDemo FPS EMA divides by unguarded dt; Inf/NaN seeds unrecoverable | `PlotDemo.cs:68` | |
| G3-40 | Demo: stage recompute cadence uses scaled Time.time — high-warp rails it to per-frame recompute | `StageAnalyzer.cs:132` | Agent flags review **under-graded** this |
| G3-41 | Demo: Unregister called unconditionally in OnDestroy — spurious warning per scene when library dormant | `DemoConsumer.cs:70` | |
| G3-42 | Release gate step 4 omits License.txt; docs\*.md check unreachable by construction | `package_release.bat:43` | |
| G3-43 | Shipped settings.cfg omits documented 'font' key | `GameData/DearImGuiKSP/settings.cfg:6` | Loader defaults to IBMPlexSans; impact nil but doc-promised |
| G3-44 | Compress-Archive non-terminating per-entry errors exit 0 — errorlevel guard can report success with truncated zip | `package_release.bat:61` | |
| G3-45 | Demo zip named from library's `<Version>`, not demo's own version property | `package_release.bat:62` | Latent until demo revs independently |
| G3-46 | AssemblyCopyright "DGerry" vs LICENSE/README "DGerry83"; Authors/Company unset | `DearImGuiKSP.csproj:12` | (review graded nit; one-word metadata fix, listed here with the packaging batch) |
| G3-47 | Stale load-order comment in ImGuiNative.cs (SetDllDirectory rationale — restored immediately after load) | `ImGuiNative.cs:102` | Comment fix |

### UNCERTAIN — runtime behavior not statically checkable

| ID | Item | Location | What's missing |
|---|---|---|---|
| G3-U1 | Viewport clamp lacks zero-size guard (0x0 Screen → all windows at origin) | `FrameLoopOrchestrator.cs:95` | Whether Screen ever reports 0x0 in KSP |
| G3-U2 | NaN uiScale/fontScale passes ClampScale + native guard, poisons style | `SettingsModel.cs:167` | Whether KSP ConfigNode parses "NaN" as float NaN |
| G3-U3 | Settings floats written InvariantCulture, read culture-sensitively | `SettingsStore.cs:142` | ConfigNode's internal culture behavior; knowledge library silent |

### INVALID

| ID | Item | Verdict |
|---|---|---|
| G3-X1 | "Facade statics race under xUnit parallel test classes" (`DearImGuiKSP.cs:24`) | INVALID — no test touches facade statics; suite builds own instances |

### NOTE (valid, no action) — compact list

Previous-frame capture sampling is spec-locked (M3); one stale-capture frame on resume self-corrects (M4); tweens lack consumer ownership — subsumed by S3 fix (M10); Tween.To-while-Suspended baseline edge (M11); English-only UI is spec-verbatim (M12, spec §7.1); ThemeEngine concrete seam is deliberate (M14); zero-window full frame keeps capture sampling uniform (M15); per-consumer lock IDs cosmetic (M16); unused interface seams IGameEventSource/IFailureNotifier (M17); Fail() pre-frame only, locks never held (M19, backlog P1); KspPalette namespace leak, API frozen (M20); uiScale independent of GameSettings.UI_SCALE, undocumented divergence (M21); spec's KSPAssemblyDependency claim vs deliberate csproj disable (M22); tween-id wrap unreachable (M23); Entry copy cost negligible (M24); ILogger level check (M25, = M37); KSPBuildTools auto-discovery is dev-machine-only (M32); additive CopyToGame (M33); stopwatch scope/unconditional timing trivial (M35, M36); duplicate _blocked/_mouseShielded (M38); InputCaptureTracker flag-cache latent (M39); rejected-theme flag drain latent (M40); discarded native return codes acknowledged in code (M41); preset table rebuild rare+small (M42); InternalsVisibleTo harmless unstrong-named (M44).
ContextShutdown no D3D11 backend shutdown — unreachable caller, benign at exit (N4); TexUvWhitePixel snapshot — mid-frame repack unreachable, fonts gated pre-first-frame (N5, PARTIAL); no device-loss/TDR handling — latent (N9); five exports mutate outside frame lock — game-thread-only today, latent (N10); frame lock stalls render thread for whole UI build — deliberate ISSUES #004 design (N11); gradient skip on style-divergent windows — C35 inclusion-filter tradeoff (N12); hand-mirrored enum tables uncoupled from headers (N13, see G3 test-pins theme); native builds /W1 no /WX /utf-8 (N19); SRWLOCK zero harness coverage (N21); harness coverage gaps + one confirmed internal defect :157 (N22, PARTIAL); backend comment overstates HS/DS/CS restore (N23); dead ImGui_ImplDX11_Init guard in 1.92.9 (N25); no RT-vs-DisplaySize diagnostic (N26); no mouse-cursor integration (N27); no AddFocusEvent, stale hover on alt-tab (N28); gradient pass rescans child geometry vs O(windows) comment (N29); collapsed-window flat title bars — known ISSUES #008 (N30); gradient stop alphas silently discarded (N31); FontGlobalScale obsolete but still applied upstream (N32); demo TUs in shipped DLL link-required (N33).
Stale pre-scene-change frame re-render — one ordering edge only (I6, PARTIAL); lock-gateway cache trust vs external ClearControlLocks — latent (I7); no focus/cursor-lock handling — latent (I8); Logger→Settings→Store recursion — no Debug on load path today, latent (I9); Fail() never releases locks — unreachable today (I10, = I45); control-panel id unreserved, TryRegister discarded — harmless first-wins (I11); per-consumer lock fan-out N×onInputLocksModified (I13, = I31); gateway write-only, no modal-All-lock gate (I14); KEYBOARDINPUT locks broader than spec text (I15); LibraryControlPanel reaches into Interop (I16); 87 lazily-bound DllImports unverified by handshake (I17); handshake constant hand-copied, no cross-check (I18, = I34); no IME support (I19); Composition static locator accepted shape (I20); concrete NativeBridge members absent from INativeBridge (I21); render-event id literal 0 lives in DearImGuiKSPAddon.cs:138 not NativeBridge.cs:132 (I23, PARTIAL); suspend-release rule in untested lambdas (I24); FailureNotifier Notify unguarded — unlikely at menu (I27); font path built outside try — addon catch-all covers (I29); custom TTF unrepresentable in panel (I33); horizontal wheel + buttons 3/4 never fed (I36); inputString buffer chain allocs only when non-empty (I37, PARTIAL); blocker sort snapshot at transition (I38); full canvas scan per capture re-entry — deliberate tradeoff (I39, = I40); missing formatVersion treated as current (I42); culture read/write — see G3-U3 (I44); WireLifecycle lambdas called once by design (I46); ResolutionChanged→no-op RebuildViewport vestigial (I47); OnDestroy teardown asymmetric, session-end only (I48); PanelToolbar constructed at quit — harmless (I49); SetActive frame lag — runtime ordering (I50, UNCERTAIN); no re-init guard needed today (I52); FailureNotifier bare _logger (I53); ShieldControlId comment wrong about IMGUI id generation — sentinel still safe (I54); always-active OnGUI deliberate, two bool checks (I55); keyboardControl non-restore mirrors documented clobber (I56); toolbar texture IS passed to AddModApplication, not destroyed at quit (I57, PARTIAL); SetDllDirectory(null) comment overstates (I58); frame lock scope narrow — input/theme/clamp mutate outside but not draw data (I59, ISSUES #004); mousePosition read twice (I60); height−y vs (height−1)−y one-pixel edge shift (I61); overrideSorting no-op on root canvas (I62); newer settings.cfg silently downgraded (I63); migration write-back persists raw values (I64); garbage settings values default silently (I65).
PlotScope _begun redundant (A40); facade class==namespace locked C7 (A17); no theme-changed event (A19); over-pop clamped by ImGui itself (A20); ToUtf8 triplication deliberate per C9 (A21); own-ABI exports bypass checked binding — unreachable given lockstep (A23); Knob variant no default — silent no-draw (A27); ImGuiEx unconditional Dispose matches pairing rules (A29); NaN geometry args garbage-in (A30); InputText capacity no high clamp — OOM inside barrier (A43); InputTextCore throwaway encode (A44); implicit sequential layout is C# default, fine (A45).
Demo: csproj conventions duplicated deliberately (D9); benchmark whole-frame metric "the honest metric" per comments (D11); ImGui readout lacks native-render caveat (D12); IsAvailable-sampled-once log noise deliberate gate (D15); per-scene visibility reset (D16); toolbar master-switch suppresses sub-windows (D17); hover "sample N" window-relative (D19); throttle knob no THROTTLE lock check (D20); graph x-axis comment already qualifies "~20 s at 60 Hz" (D21, PARTIAL); recompute-while-closed is deliberate warm-history caching (D23, PARTIAL); strings-in-timed-region acknowledged demo code (D24); placeholder texture 5.7KB/scene leak (D27); click-count string per frame (D28); 4800-float copy is documented #007 fix (D29); OldestFirst deliberately kept documented (D30); per-row meter tween slots documented (D31); spinner radius/4 ratio known ISSUES #006 deferred (D32).
Tests: all T-items NOTE — individually small test-quality gaps (T1–T27; T1/T6 dup). Collectively they argue for one test-hardening contract — see chunking plan. T13 (tests README) is filed in Group 5.
Build: .gitignore gaps mostly covered transitively (B4, PARTIAL); no-CI is deliberate (B6, recorded in wave PROGRESS_LOG); zip lacks upstream MIT texts but spec doesn't require them (B7, PARTIAL — policy choice); no global.json (B10, nit).

---

## GROUP 4 — NIT-TIER (non-docs)

### WORK (6)

| ID | Item | Location | Note |
|---|---|---|---|
| G4-01 | Spinner empty-ID guard tests only null — `id: ""` bypasses documented guard | `DearImGuiKSP.Spinner.cs:213` | Already ISSUES #005 |
| G4-02 | Demo: benchmark FPS readout inflated (not one-frame) after dt=0 first frame | `BenchmarkUI.cs:116` | PARTIAL — persists dozens of frames |
| G4-03 | Demo: Unregister unconditional in OnDestroy → spurious per-scene warning when dormant | `DemoConsumer.cs:70` | |
| G4-04 | ImSpinnerColor doc cites wrong ABI type (ImColor_c vs C++ ImColor) | `ImSpinnerNative.cs:7` | = G3-31, batched with comment fixes |
| G4-05 | SpinnerAng8 stale line citation | `ImSpinnerNative.cs:167` | = G3-32 |
| G4-06 | AssemblyCopyright "DGerry" vs "DGerry83"; Authors/Company unset | `DearImGuiKSP.csproj:12` | = G3-46, batched with packaging |

### INVALID (2)

- "First backend bring-up marks atlas Destroyed → one frame null SRV" (`BackendD3D11.cpp:70`) — INVALID: RenderDrawData processes textures first, same frame; no Destroyed marking on init.
- "Frame-guard comments mislabel draw data / block-cost thread" (`ContextHost.cpp:30`) — INVALID: comment accurately describes hazard and scope (ISSUES #004).

### NOTE (~60, all VALID/PARTIAL observations not warranting change)

Native: N14 (order contract documented, harness-proven), N19, N20 (dup), N21, N33, N34 (ordering IS documented, :52-56), N36 (malloc-failure-only leak path), N37 (dead export, no callers), N38 (latent), N39 (PARTIAL — comment literally accurate), N40 (redundant dark re-apply), N41 (unreachable entry points deliberate), N42 (null-policy divergence undocumented).
Managed: M23–M26, M34–M42, M44 (see Group 3 NOTE list for one-liners).
Infrastructure: I45–I65 range minus items promoted above; all latent/cosmetic/harmless (see Group 3 NOTE list).
API: A24, A25, A26 (Interop README → Group 5), A40, A43, A44, A45.
Demo: D10, D24, D27–D32.
Tests: T20–T27 range (T27's README half → Group 5; AdoptTopSortOrder extraction suggestion noted for test-hardening contract).
Build: B10 (no global.json).

---

## GROUP 5 — DOCUMENTATION UPDATES (all tiers)

### Medium-tier doc fixes

| ID | Item | Location | Verdict |
|---|---|---|---|
| G5-M1 | CHANGELOG 1.0.0 advertises ID-scope helpers and tree widgets that don't exist | `CHANGELOG.md:5` (= O8) | VALID WORK |
| G5-M2 | Docs present settings.cfg as live-editable; read once at startup, overwritten by panel | `docs/10-api-fundamentals.md:267` | VALID WORK |
| G5-M3 | docs/70 "16-bit index limit" is false — shipped imgui_impl_dx11 sets RendererHasVtxOffset | `docs/70-troubleshooting.md:81` | VALID WORK (D32 record) |

### Low/nit-tier doc fixes (WORK)

- README build recipe: `cd DearImGuiKSPNative` persists, next line's slnx path wrong — `README.md:116` (O1)
- Widget doc says multiply by UI scale, but no public UiScale accessor exists — `docs/20-widgets.md:340` (O2)
- CHANGELOG: in-game panel "adjusts window clamping" — no such control — `CHANGELOG.md:5` (O7)
- README credits omit cimplot copyright holder (Victor Bombí 2020) — `README.md:135` (O10)
- vendor/cimspinner has no LICENSE; README's blanket per-tree licence claim false — `README.md:149` (O11)
- Install-tree listing omits Textures/ and Docs/ — `docs/00-getting-started.md:17` (O12)
- Inverted loader rule: KSP scans all GameData, excludes PluginData — `docs/00:36` (O13)
- Stale "toolbar icon not shipped" wording — `docs/00:49` (O14)
- KSPAssemblyDependencyEqualMajor mis-stated: minor is a minimum, not a pin — `docs/00:83` (O15)
- Registration order is initial stacking only, focus overrides — `docs/10:23` (O16, resolved #009)
- "Only errors that throw" list omits empty-id Register / Unregister(null) — `docs/10:53` (O17, = A41)
- End-of-frame assert promised but compiled out by /DNDEBUG — `docs/10:110` (O18)
- "Only dispose what a factory returned" illustrated with inert scopes, omits four destructive ones — `docs/10:111` (O19)
- Color/Color32 PushStyleColor labeled linear vs sRGB — no conversion exists — `docs/10:150` (O20)
- Settings table never says enabled=false is file-only, no in-game re-enable — `docs/10:262` (O21)
- 15-spinner subset blamed on upstream config; vendored config enables 26 (binding-layer curation) — `docs/20:284` (O22)
- Theming §6 contradicts §1/ThemeEngine: window-bg gradient disabled under "dark" — `docs/30:149` (O23)
- Theming doc omits live settings panel + font/fontScale restart asymmetry — `docs/30:160` (O24)
- Animation doc links to nonexistent 'Tween.To ignored' troubleshooting entry — `docs/50:129` (O25; pair with G3-07 code fix)
- docs/60 mapping table uses `DearImGuiKSP.Text(...)` single-qualified — never compiles — `docs/60:13` (O26)
- Native README claims nonexistent opengl32 link; install layout omits Textures/ — `DearImGuiKSPNative/README.md:15` (N17, PARTIAL: ENVIRONMENT.md was not stale)
- Application/README.md + AGENTS.md still state pre-D24 layering rules; stale inverted frame order; omit Animation/Theming/Api folders — `Application/README.md:3,9` (M28, M29)
- Docs describe uiScale/fontScale as independent knobs; they multiply — `docs` + `FontResolver.cs:57` comment (I28)
- Infrastructure README misstates NativeBridge as owning all P/Invoke decls; omits five components — `Infrastructure/README.md:9` (I41)
- Interop/ has no README / caller-direction rule, unlike sibling layers — A26
- Application.Tests README stale coverage set; falsely claims no Unity assemblies loaded — T13
- Infrastructure.Tests README deferral list omits several gateways — T27
- Docs prescribe game restart for auto-disable recovery; Unregister+Register recovers in-session — M13 (also consider API observability, deferred)
- Doc components of code items: RowHeight 22px recommendation (`docs/20-widgets.md:423`, pair with G2-12); theme label-side difference (pair with G3-28)

### Doc items closed as NOTE/INVALID

- AGENTS.md notes/archive folder name + cimgui path — line-ref off, minor (O6, NOTE)
- README "fraction of a millisecond" — wave run did include full busy demo (O9, PARTIAL NOTE)
- "You will not corrupt other consumers" — doc explicitly credits using-scope disposal; raw pairs not claimed safe (O27, INVALID)
- Plot scopes unlisted in ImGuiEx guard claim (O28, NOTE); Spinner-only default IDs (O29, NOTE); IMGUI blocking attribution vestigial (O30, PARTIAL, #003); handshake log line quote omission (O31, NOTE)

---

## CROSS-CUTTING THEMES (input for the FlyByWire workflow assessment)

1. **NDEBUG-stripped upstream asserts shift validation responsibility to the managed layer** — G2-10, G3-22/23/24/30, O18. Recurring: the release native build compiles out ImGui/ImPlot asserts, so any unvalidated enum/count/size forwarded across the ABI is UB, not an assert.
2. **Consumer-code-at-the-frame-boundary fault model** — S1, S2, S3, G2-05, M10. The FaultBarrier covers callback bodies only; setters, registration calls, and stack-state restoration around a throw were all outside it.
3. **Per-frame write amplification family** — G3-08/09/14/16 (settings.cfg rewritten per slider-drag frame, non-atomic, inside the frame lock). One debounce/atomic-save fix closes four items.
4. **Docs drift family** — ~33 doc fixes, mostly small stale-claim corrections; several promise behavior the code doesn't have (guards, asserts, disposals).
5. **Hand-mirrored constants across the ABI without pins** — handshake version (I18), ImGuiCol/StyleVar ordinals (N13/T19), render-event id literal (I23), enum pin tests (T18/T23). No build-time cross-check anywhere.
6. **Latent/unreachable class** — a large share of NOTEs are "real but unreachable today" (N4, N9, N10, I9, I10, A23, I52…). Worth a single knowledge note on how to record these so they don't get re-litigated.
7. **Demo/reference code teaches by example** — demo bugs (D-series) matter because consumers copy the demo; docs recommending the same bad constant (G2-12) amplify it.

## Items already recorded in project notes/ISSUES (do not re-file)

ISSUES #003–#009 region: N11/N35 (#004), N30 (#008), O16 (#009), I59 (#004), A39 (#005), D29/D30/D32 (#007/#006), D32 (#006), O5 (D32), M9 (spec §4.4), M12 (spec §7.1), M19 (backlog P1), I30 (fix backlog P1), B6/B7 (wave PROGRESS_LOG), O30 (#003). Per user direction, none of the review items are being added to ISSUES\ — this document is the record.
