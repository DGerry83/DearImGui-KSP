# Opus Review Remediation — Chunk Contract Plan

**Date:** 2026-09-07
**Source:** `notes\active\2026-09-07_Bug_OpusReviewTriage\TRIAGE_SWEEP.md` (triaged Claude Opus review)
**Status:** Plan only — no code changes. Contracts below are work packages for one or more future FlyByWire sessions (`BugfixPlanning.md` for the P0 severe seam, `PlanImplementation.md` for the batched waves).
**Scope rule:** Only WORK items plus the two UNCERTAIN mediums (G2-U1/G2-U2) appear here. INVALID/NOTE items get no contracts and are not re-litigated. G3-U1/U2/U3 (low-tier UNCERTAIN) are not scheduled; record any future confirmation in the triage session, not here.

**Coverage:** 100 distinct WORK items (3 severe + 16 medium + 46 low + 2 nit + 33 docs; G3-19 merged into G3-08; G4-03/04/05/06 are duplicates of G3-41/31/32/46) + 2 verification-gated UNCERTAIN items = **102 items across 16 contracts + 2 conditional contracts**.

## Summary table

| ID | Title | Items | Risk (severity / exposure) | §5.9 checklist | Parallelizable with |
|----|-------|------:|----------------------------|:---:|---------------------|
| C01 | Frame-boundary fault model (severe) | 3 | P0 / Critical Path | Yes | Nothing (P0, single-agent) |
| C02 | Tween fault containment (severe) | 2 | P0 / Critical Path | Yes | Nothing (after C01) |
| C03 | In-game verification: G2-U1, G2-U2 | 2 | No code / — | No | With C01–C02 (user-side) |
| C04 | Native rendering & diagnostics | 4 | P2 / Standard | Yes | C05, C06, C09, C11 |
| C05 | Native build tooling hardening | 3 | P3 / Standard | No | C04, C06, C09, C11 |
| C06 | Settings write-amplification family | 5 | P2 / Standard | Yes | C04, C05, C09, C11 |
| C07 | ABI enum/count validation family | 6 | P1 / Critical Path | Yes | Batch-1 set (not C08) |
| C08 | Widget correctness & label/ID family | 13 | P2 / Standard | Yes | C09, C12, C13 (after C07) |
| C09 | Infrastructure hygiene & timing | 7 | P2 / Standard | Yes | C04, C05, C06, C07, C11 |
| C10 | Font reachability decision (G3-10) | 1 | P2 / Critical Path (if API added) | No | Any post-C01 slot |
| C11 | Packaging & release metadata | 9 | P3 / Presentational | No | C04, C05, C06, C09 |
| C12 | Demo flight-dynamics math | 6 | P2 / Standard (demo) | Yes | C08, C13, C14 |
| C13 | Demo UI/benchmark correctness | 6 | P2 / Standard (demo) | Yes | C08, C12, C14 |
| C14 | Test hardening | 1 (+T-series) | P3 / Standard | No | Any |
| C15 | Docs housekeeping A (root + 00/10) | 19 | P3 / Presentational | No | C16 (after code deps) |
| C16 | Docs housekeeping B (20–70 + layer READMEs) | 15 | P3 / Presentational | No | C15 (after code deps) |
| C17 | (conditional) F2-hidden resync fix | 1 | P2 / Standard | No | After C03 confirms G2-U1 |
| C18 | (conditional) Mouse-lock mask bits fix | 1 | P2 / Standard | No | After C03 confirms G2-U2 |

---

## C01 — Frame-boundary fault model (severe)

- **Items:** S1, S2, G2-05
- **What:** Iterate a snapshot (or deferred register/unregister queue) in `RunFrame` and wrap the frame in try/finally so `EndUiFrame()` always runs (S1); add a frame-open flag set between BeginUiFrame/EndUiFrame and gate facade widgets on it, making out-of-frame calls safe no-ops as documented (S2); save/restore ImGui stack state (window stack, ID stack) in FaultBarrier so a throwing consumer cannot mis-parent later consumers (G2-05).
- **Files:** `DearImGuiKSP/Application/FrameLoopOrchestrator.cs`, `DearImGuiKSP/Application/ConsumerRegistry.cs`, `DearImGuiKSP/Application/FaultBarrier.cs`, `DearImGuiKSP/DearImGuiKSP.cs` (facade gate), likely `tests/Application.Tests/` additions.
- **Risk:** P0 / Critical Path — frame loop, public facade gating, consumer registry shape. CORE_PROTOCOLS §6: P0 → single agent, sequential, no parallelization.
- **§5.9 applies:** per-frame code + resource-acquisition symmetry (native SRWLOCK held BeginFrame→EndFrame; the skipped-EndFrame deadlock is the bug). Record all three verdicts.
- **Dependencies:** none (first contract executed).
- **Verification:** `dotnet build DearImGui-KSP.slnx` + `dotnet test` (new tests: register/unregister-inside-callback does not throw and EndUiFrame still runs; out-of-frame widget call is a no-op) + native debug build harness sanity + **in-game gate A** (user: throwing/misbehaving consumer no longer hangs or CTDs).
- **Rollback:** revert the listed files.

## C02 — Tween fault containment (severe)

- **Items:** S3, G3-07 (code half)
- **What:** Guard each tween setter invocation in `TweenEngine.Tick` (catch → log → kill/auto-complete that tween), mirroring FaultBarrier semantics (S3); guard the immediate baseline `set(from)` in `Tween.To` (G3-07 code; its doc half O25 is in C16).
- **Files:** `DearImGuiKSP/Application/TweenEngine.cs`, `DearImGuiKSP/Application/Tween.cs`, `tests/Application.Tests/`.
- **Risk:** P0 / Critical Path — per-frame engine driving every consumer's animation.
- **§5.9 applies:** per-frame tick.
- **Dependencies:** C01 (both touch `FrameLoopOrchestrator.cs` region — S3's throw path runs through `RunFrame:88`; sequential per P0 policy).
- **Verification:** build + dotnet test (throwing setter kills only that tween; other tweens keep running; baseline-set throw contained) + **in-game gate A** alongside C01.
- **Rollback:** revert the listed files.

## C03 — In-game verification: G2-U1 / G2-U2

- **Items:** G2-U1, G2-U2
- **What:** Verification-only contract, no code. User (or a scripted in-game session) checks: (U1) hide UI with F2, change scene, confirm whether the library stays Suspended for the session; (U2) open an ImGui window over maneuver-node gizmos in map view and test whether legacy collider input passes through. Outcome recorded in the triage session folder; verdict decides whether C17/C18 are scheduled or the items close as NOTE.
- **Files touched:** none (notes only).
- **Risk:** none.
- **Dependencies:** none — schedule early (uses the user's game time; results gate C17/C18).
- **Verification:** in-game only. Combine with gate A to save a game session.

## C04 — Native rendering & diagnostics

- **Items:** G2-01, G2-04, G3-01, G3-03
- **What:** Retarget the gradient pass so child-window decorations render into the child's draw list (G2-01); install an ImGui ErrorCallback routing diagnostics to KSP.log instead of red debug overlays (G2-04); clamp/round scaled 1px style sizes so uiScale < 1.0 doesn't zero them (G3-01); log D3D11 backend bring-up failure once and stop per-frame retry spam / RendererHasTextures flapping (G3-03).
- **Files:** `DearImGuiKSPNative/src/ContextHost.cpp`, `DearImGuiKSPNative/src/BackendD3D11.cpp`.
- **Risk:** P2 / Standard — native internals, no ABI change.
- **§5.9 applies:** per-frame native code; record allocation + symmetry verdicts.
- **Dependencies:** C01–C02 verified (shared game build). Parallel-safe with C05/C06/C09/C11 (disjoint files).
- **Verification:** `cd DearImGuiKSPNative && ./build.bat` + native harness run; visual in-game check in gate B (gradients on child windows, uiScale 0.5 borders visible, error tooltip path).

## C05 — Native build tooling hardening

- **Items:** G2-02, G2-03, G3-02
- **What:** Enforce the cimgui/imgui pin per PIN_RECORD's fail-loud rule in the build scripts (G2-02); emit PDBs for release native builds (G2-03); vcvars64 path fallback + errorlevel checks in all three build scripts (G3-02).
- **Files:** `DearImGuiKSPNative/build.bat`, `build_release.bat`, `build_harness.bat`.
- **Risk:** P3 / Standard — build scripts only.
- **§5.9:** no.
- **Dependencies:** none beyond git baseline. Parallel-safe (disjoint from C04's src files).
- **Verification:** run all three scripts; confirm PDB emitted next to release DLL and pin-mismatch fails loud.

## C06 — Settings write-amplification family

- **Items:** G3-08, G3-09, G3-14, G3-16, G3-43 (+ merged G3-19)
- **What:** One shared fix: debounce settings.cfg writes (dirty-flag + save on change-settle / frame exit), make the save atomic (temp-file + replace), and move the write outside the held frame lock — closes the four per-frame rewrite items. Add the documented `font` key to the shipped default settings.cfg (G3-43).
- **Files:** `DearImGuiKSP/Application/SettingsModel.cs`, `DearImGuiKSP/Application/ThemeEngine.cs`, `DearImGuiKSP/Infrastructure/SettingsStore.cs`, `DearImGuiKSP/Infrastructure/LibraryControlPanel.cs`, `GameData/DearImGuiKSP/settings.cfg`.
- **Risk:** P2 / Standard — internal persistence path; public config file format unchanged (hard constraint: library persists only its own settings.cfg).
- **§5.9 applies:** per-frame slider path; hot-path allocation + process-global (file I/O) verdicts required.
- **Dependencies:** none; parallel-safe with C04/C05/C09/C11. Do **not** run parallel with C07 (both may touch `DearImGuiKSP.cs` region via panel glue — check at dispatch; default sequential after C07 if overlap confirmed).
- **Verification:** build + dotnet test + in-game gate B (drag uiScale slider; one settings.cfg write after settle, theme still live-updates).

## C07 — ABI enum/count validation family

- **Items:** G2-10, G3-18, G3-22, G3-23, G3-24, G3-30
- **What:** Theme 1 of the sweep: the release native build compiles out ImGui/ImPlot asserts (`/DNDEBUG`), so managed must validate before crossing the ABI. Honor caller capacity in InputText (G2-10) and make the seed copy UTF-8-boundary-safe (G3-18, same `ImGuiInternal.cs:144` region); clamp/validate BeginSubplots rows/cols (G3-22); reject `COUNT` and out-of-range ImGuiCol/ImGuiStyleVar before forwarding (G3-23, G3-24); add AlwaysClamp to SliderFloat (G3-30).
- **Files:** `DearImGuiKSP/ImGuiInternal.cs`, `DearImGuiKSP/ImGuiPlot.cs`, `DearImGuiKSP/ImGuiStyleEnums.cs`, `DearImGuiKSP/DearImGuiKSP.cs`.
- **Risk:** P1 / Critical Path — public widget entry points, UB-class bugs. Guards must preserve the no-throw widget contract (clamp + no-op + log, not exceptions).
- **§5.9 applies:** per-frame widget-call paths.
- **Dependencies:** C01 (shares `DearImGuiKSP.cs` facade gate region). **C08 must run after C07** (shared files: `ImGuiInternal.cs`, `DearImGuiKSP.cs`).
- **Verification:** build + dotnet test (new guard tests: OOB enum no-ops, rows/cols clamped, capacity honored, multi-byte UTF-8 not split). Native harness optional.

## C08 — Widget correctness & label/ID family

- **Items:** G2-07, G2-08, G2-09, G2-11, G3-17, G3-20, G3-21, G3-25, G3-26, G3-29, G3-31, G3-32, G4-01
- **What:** Fix RadioButton(ref bool) never assigning its ref (G2-07); strip `##` ID suffixes in GradientButton and ksp-theme InputText labels (G2-08, G2-09); document + detect process-global window-title collisions across mods (G2-11); Spinner out-of-range type → no-op instead of IndexOutOfRange (G3-17); guard empty label/id collision as docs promise (G3-20); respect dark theme's byte-exact-stock guarantee for RadioButton rim (G3-21); correct ImGuiDraw.AddText XML doc (G3-25); hoist GradientButton double-measure (G3-26); remove double allocation in ToUtf8 (G3-29); fix ImSpinner ABI comment citations (G3-31, G3-32); extend Spinner empty-ID guard test to `""` (G4-01, ISSUES #005).
- **Files:** `DearImGuiKSP/DearImGuiKSP.Radio.cs`, `DearImGuiKSP/ImGuiGradients.cs`, `DearImGuiKSP/DearImGuiKSP.cs`, `DearImGuiKSP/DearImGuiKSP.Spinner.cs`, `DearImGuiKSP/ImGuiInternal.cs`, `DearImGuiKSP/ImGuiDraw.cs`, `DearImGuiKSP/Interop/ImSpinnerNative.cs`, `tests/Application.Tests/`.
- **Risk:** P2 / Standard — widget internals; no public signature changes.
- **§5.9 applies:** per-frame measure/encode paths (G3-26, G3-29) — allocation verdict required.
- **Dependencies:** **after C07** (shared `ImGuiInternal.cs` / `DearImGuiKSP.cs`). Parallel-safe with C09, C12, C13, C14.
- **Verification:** build + dotnet test (Radio ref assignment, label stripping, empty-id guard, spinner no-op) + spot-check in gate B.

## C09 — Infrastructure hygiene & timing

- **Items:** G2-06, G3-06, G3-11, G3-12, G3-13, G3-15, G3-47
- **What:** Use unscaled time for io.DeltaTime and tween clock so tweens don't freeze on pause and ImGui timing isn't 4x fast under physics warp (G2-06); don't start the render pump coroutine after failed init + remove per-frame WaitForEndOfFrame alloc (G3-12); reuse the ApplyLocks HashSet instead of allocating per frame (G3-13); fix stale/misleading comment and log text (G3-06, G3-11, G3-15, G3-47).
- **Files:** `DearImGuiKSP/Infrastructure/DearImGuiKSPAddon.cs`, `DearImGuiKSP/Infrastructure/InputLockGateway.cs`, `DearImGuiKSP/Infrastructure/NativeBridge.cs`, `DearImGuiKSP/Interop/INativeBridge.cs`, `DearImGuiKSP/Interop/ImGuiNative.cs`, `DearImGuiKSP/Application/TweenEngine.cs` (clock source only — coordinate with C02 if that contract is still open).
- **Risk:** P2 / Standard — Infrastructure layer only (the KSP/Unity-touching layer), timing behavior change is user-visible (tweens on pause) — call out in gate notes.
- **§5.9 applies:** per-frame coroutine/lock paths.
- **Dependencies:** C02 for the TweenEngine clock touch (sequence after, or split the clock line to C02's follow-up). Otherwise parallel-safe with batch 1.
- **Verification:** build + dotnet test + in-game gate B (pause → tweens freeze; warp → UI timing correct).

## C10 — Font reachability decision (G3-10)

- **Items:** G3-10
- **What:** Decide: expose a minimal font-selection/PushFont API so the atlas-loaded IBM Plex Medium is reachable (public API addition → SemVer minor, D17/D36 lockstep applies) **or** drop the dead atlas load. Record the decision in the triage session before implementing.
- **Files:** `DearImGuiKSP/Application/FontResolver.cs` and, if the API route is chosen, facade + Interop additions; otherwise just the dead-load removal.
- **Risk:** P2 / Critical Path if API added; P3 / Standard if load dropped.
- **§5.9:** no (font loading is gated pre-first-frame).
- **Dependencies:** none; any slot after C01.
- **Verification:** build + dotnet test; if API added, in-game check of font switch in gate B.

## C11 — Packaging & release metadata

- **Items:** G2-16, G3-04, G3-05, G3-37, G3-38, G3-42, G3-44, G3-45, G3-46 (= G4-06)
- **What:** Stop shipping settings.cfg in the release zip (G2-16 — upgrades must not reset player settings); pin shipped .version KSP_VERSION min/max to 1.12.x (G3-04, G3-38); point AVC URL at a raw .version URL (G3-05); include License.txt/Readme.txt in the demo zip (G3-37); fix release-gate step 4 License.txt check and unreachable docs check (G3-42); make Compress-Archive errors fail the packaging run (G3-44); name the demo zip from the demo's own version (G3-45); fix AssemblyCopyright/Authors/Company metadata (G3-46).
- **Files:** `package_release.bat`, `DearImGuiKSP/DearImGuiKSP.csproj`, `DearImGuiKSP/DearImGuiKSP.version`, `DearImGuiKSPDemo/DearImGuiKSPDemo.csproj`, `DearImGuiKSPDemo/DearImGuiKSPDemo.version`, `GameData/` staging.
- **Risk:** P3 / Presentational — release tooling and metadata; no runtime code.
- **§5.9:** no.
- **Dependencies:** none; parallel-safe with C04/C05/C06/C09. Run before the final release-candidate gate.
- **Verification:** `package_release.bat` end-to-end; inspect both zips (no settings.cfg, license present, correct names); error-injection test that a failed archive step exits non-zero.

## C12 — Demo flight-dynamics math

- **Items:** G2-13, G2-14, G2-15, G3-34, G3-35, G3-40
- **What:** Fix radar ellipse rotation/degrees-fed-to-Cos (G2-13); remove double radians→degrees on inclination (G2-14); include upper-stage propellant in stage dV masses (G2-15); handle multi-mode engines correctly in thrust/Isp/mass-flow (G3-34); exclude locked tanks from usable propellant (G3-35); use unscaled time for the stage recompute cadence (G3-40).
- **Files:** `DearImGuiKSPDemo/Telemetry/OrbitPanel.cs`, `DearImGuiKSPDemo/Telemetry/StageAnalyzer.cs`.
- **Risk:** P2 / Standard — demo only, capped medium; demo teaches by example (sweep theme 7) so correctness matters for consumers copying it.
- **§5.9 applies:** per-frame telemetry paths.
- **Dependencies:** none vs library contracts; parallel-safe with C08, C13, C14. Demo stays a separate install (D7) — never mixed into library contracts.
- **Verification:** build + **in-game gate C** (flight scene: orbit panel ellipse matches map view, stage dV sanity against stock/KER, locked-tank and multi-mode vessels).

## C13 — Demo UI/benchmark correctness

- **Items:** G2-12, G3-33, G3-36, G3-39, G3-41 (= G4-03), G4-02
- **What:** Derive virtualized-list RowHeight from uiScale/fontScale instead of hard-coded 22px (G2-12; docs recommendation fixed in C16); guard the virtualization-toggle Layout/Repaint mismatch (G3-33); clear telemetry rings on active-vessel change (G3-36); guard PlotDemo FPS EMA against dt=0/Inf/NaN (G3-39); only Unregister in OnDestroy when registered (G3-41); fix benchmark FPS inflation after dt=0 first frame (G4-02).
- **Files:** `DearImGuiKSPDemo/BenchmarkUI.cs`, `DearImGuiKSPDemo/Telemetry/TelemetrySampler.cs`, `DearImGuiKSPDemo/PlotDemo.cs`, `DearImGuiKSPDemo/DemoConsumer.cs`.
- **Risk:** P2 / Standard — demo only.
- **§5.9 applies:** per-frame demo paths.
- **Dependencies:** none vs library; disjoint files from C12 → parallel-safe.
- **Verification:** build + in-game gate C (scene change with UI hidden — no warning spam; vessel switch clears rings; benchmark toggling stable).

## C14 — Test hardening

- **Items:** G3-27 + the T-series NOTE block (sweep line: "Collectively they argue for one test-hardening contract"), including ABI enum pin tests (N13/T18/T23 theme).
- **What:** Add unit tests for the pure helpers (Pack/Lerp/Lighten, FailureText.BodyFor, KindForInitResult); add pin tests locking the hand-mirrored enum ordinals against the pinned imgui 1.92.9 headers (cross-cutting theme 5); adopt the T-series test-quality improvements (incl. AdoptTopSortOrder extraction).
- **Files:** `tests/Application.Tests/` (new files), `tests/*/README.md` freshness where touched (README content itself is C15/C16).
- **Risk:** P3 / Standard — tests only.
- **§5.9:** no.
- **Dependencies:** none; parallel-safe with everything after C01. Land after C07/C08 so pin tests cover the new guards.
- **Verification:** `dotnet test DearImGui-KSP.slnx` green.

## C15 — Docs housekeeping A

- **Items:** G5-M1, G5-M2, O1, O7, O10, O11, O12, O13, O14, O15, O16, O17, O18, O19, O20, O21, M28, M29, T13 (19 items)
- **What:** Correct the stale/false claims in root docs and the getting-started/fundamentals guides: CHANGELOG 1.0.0 feature claims (M1, O7), settings.cfg liveness (M2), README build recipe + credits + licence claim (O1, O10, O11), docs/00 install tree, loader rule, toolbar wording, KSPAssemblyDependencyEqualMajor semantics (O12–O15), docs/10 stacking-order, error list, compiled-out assert, destructive disposals, color-space labels, enabled=false file-only (O16–O21), Application/README + AGENTS.md layering staleness (M28, M29 — **AGENTS.md update required per repo rule**), Application.Tests README (T13).
- **Files:** `README.md`, `CHANGELOG.md`, `AGENTS.md`, `docs/00-getting-started.md`, `docs/10-api-fundamentals.md`, `DearImGuiKSP/Application/README.md`, `tests/Application.Tests/README.md`.
- **Risk:** P3 / Presentational.
- **§5.9:** no.
- **Dependencies:** after C06 (M2 must describe the debounced behavior) and C09 (M28/M29 layering text). Parallel-safe with C16 (disjoint files).
- **Verification:** markdown review diff; claim-by-claim cross-check against code (the sweep's citations are the checklist).

## C16 — Docs housekeeping B

- **Items:** G5-M3, M13, O2, O22, O23, O24, O25, O26, G3-28, I28, N17, I41, A26, T27, RowHeight doc (docs/20:423) (15 items)
- **What:** docs/70 fixes (false 16-bit index limit M3; in-session recovery vs restart M13); docs/20 widget fixes (UiScale accessor claim O2, spinner-count attribution O22, RowHeight recommendation); docs/30 theming contradictions + live panel/font restart asymmetry (O23, O24, G3-28 label-side difference); docs/50 broken link (O25 — pairs with C02's G3-07 code fix); docs/60 non-compiling mapping table (O26); uiScale/fontScale multiply-not-independent (I28, incl. `FontResolver.cs:57` comment); native README (N17), Infrastructure README (I41), **new** `Interop/README.md` with caller-direction rule (A26), Infrastructure.Tests README (T27). Optional add-on: one `notes\knowledge\` note recording the latent/unreachable class (sweep theme 6) so NOTEs don't get re-litigated.
- **Files:** `docs/20-widgets.md`, `docs/30-theming.md`, `docs/50-animation.md`, `docs/60-migration-from-imgui.md`, `docs/70-troubleshooting.md`, `DearImGuiKSPNative/README.md`, `DearImGuiKSP/Infrastructure/README.md`, `DearImGuiKSP/Interop/README.md` (new), `tests/Infrastructure.Tests/README.md`, `DearImGuiKSP/Application/FontResolver.cs` (comment line only), optionally `notes/knowledge/latent-items-register.md`.
- **Risk:** P3 / Presentational.
- **§5.9:** no.
- **Dependencies:** after C02 (O25), C13 (RowHeight doc), C10 (I28 font comment). Parallel-safe with C15.
- **Verification:** markdown review diff; link check on docs/50.

## C17 — (conditional) F2-hidden resync fix

- **Items:** G2-U1
- **Schedule only if C03 confirms** the library stays Suspended after scene change with UI hidden.
- **What:** Resync `_uiHidden` on scene load (stock scene-load reshow behavior), per the verified mechanism.
- **Files:** `DearImGuiKSP/Infrastructure/Composition.cs`.
- **Risk:** P2 / Standard. **§5.9:** no.
- **Verification:** in-game reproduction of the C03 test → library resumes after scene change.

## C18 — (conditional) Mouse-lock mask bits fix

- **Items:** G2-U2
- **Schedule only if C03 confirms** maneuver gizmos stay live under ImGui windows.
- **What:** Add MAP_UI/MANNODE bits to the mouse-capture lock mask.
- **Files:** `DearImGuiKSP/Infrastructure/InputLockGateway.cs`.
- **Risk:** P2 / Standard. **§5.9:** no.
- **Verification:** in-game reproduction of the C03 test → gizmos blocked under ImGui windows.

---

## Recommended execution order

Sequencing rules applied: P0 first and single-agent (CORE_PROTOCOLS §5.4/§6); file-disjoint contracts may run in parallel, max 2–3 concurrent sub-agents (BugfixPlanning Phase 2); dependent file sets run sequentially; demo contracts never mix with library contracts (D7 release scope).

1. **C01 → C02** (sequential, single agent each). These are the same consumer-code/frame-boundary seam but touch different subsystems; the P0 policy forbids parallelizing them.
2. **Gate A (in-game, user):** verify C01+C02 fixes **and run C03's two experiments** in the same game session. User-verified milestone per AGENTS.md convention (same as M1–M8 gating).
   - If G2-U1/G2-U2 confirm → insert C17/C18 into wave 2 (both are small Infrastructure edits; C18 shares `InputLockGateway.cs` with C09 → run C18 inside or after C09, not parallel).
3. **Wave 2 (parallel, 2–3 agents max, library internals):** dispatch in pairs/triples from {C04, C05, C06, C07, C09, C10, C11} — all mutually file-disjoint except: C08 waits for C07; if C06↔C07 overlap on `DearImGuiKSP.cs` is confirmed at dispatch, sequence C06 after C07.
4. **C08** (after C07 completes — shared `ImGuiInternal.cs`/`DearImGuiKSP.cs`).
5. **Gate B (in-game, user):** visual/behavioral checks for C04, C06, C07, C08, C09, C10 (gradients, uiScale 0.5, slider-drag writes, pause/warp timing, guards no-throw). One session.
6. **Wave 3 (parallel):** C12 ∥ C13 ∥ C14 (demo pair + tests; disjoint from each other and from library).
7. **Gate C (in-game, user):** demo verification — flight scene for C12, scene/vessel switching and benchmark UI for C13.
8. **C15 ∥ C16** (docs housekeeping, parallel; last, so docs describe final behavior).
9. **Gate D (release candidate):** `package_release.bat` dry-run exercising C11, clean-KSP zip-install check per the D16 convention, full `dotnet build DearImGui-KSP.slnx -c Release` + `build_release.bat` + `dotnet test`. Then version/changelog decision per D36 (batch ships as 1.0.x patch or 1.1.0 if C10 adds API — SemVer lockstep managed+native).

**Fastest safe path if the user wants minimal game sessions:** merge gates B and C by scheduling wave 3 before gate B (C12/C13 don't depend on the library waves); then gates are A (severe + U-verification), B+C combined (all behavior), D (release).

## Notes for the executing session

- Each contract above maps to a `CHUNK_N_CONTRACT.md` artifact (template: FlyByWire v3 `reference\templates\CHUNK_N_CONTRACT.md`) created at dispatch time in the executing session's `notes\active\` folder; this file is the CHUNK_MAP-equivalent.
- Frozen gates (GATES.md) for the remediation session should be derived per contract from the Verification fields above.
- The executing session should be a Bug-type FlyByWire session for wave 1 (C01–C02, full Phase 0 detective already satisfied by the triage sweep's verified root causes — reference, don't redo) and a PlanImplementation-style run for waves 2+.
- Nothing here touches `ISSUES\` — per user direction, the triage sweep document remains the record.
