# Chunk Contract: C13 — Demo UI/benchmark correctness
## Plan: Opus Review Remediation (`notes\plans\opus-review-remediation-contracts.md`)
## Date: 2026-09-07
## Chunk ID: C13
## Advances Milestone: Post-1.0.0 remediation wave (G2-12, G3-33, G3-36, G3-39, G3-41 (= G4-03), G4-02)

### Scope
Theme: demo-only UI/benchmark correctness. Demo files only — the library was not touched, no public API added (G2-12's gap is recorded below, not papered over). The temporary fault-injection section in DemoConsumer.cs (F1–F4, needed for the in-game gate) was left byte-identical.

- **G2-12** (`BenchmarkUI.cs`): the Dear ImGui virtualized list's row pitch is no longer a hard-coded 22 px constant. It is now measured live each frame from the first declared row: the cursor's screen Y (`ImGuiDraw.GetCursorScreenPos`, public) before and after one `Text` item is exactly the style-driven line advance, and the scroll offset shifts both readings equally within a frame, so the delta is correct at any scroll position. The pitch follows uiScale, fontScale, and the active font (including ProggyClean) with one frame of lag after a settings change. **Public API gap (recorded, not fixed per contract):** the public surface genuinely exposes no style metrics — no CalcTextSize/TextLineHeight, no FramePadding read, no UiScale/FontScale accessor (`SettingsModel.UiScale` is internal; docs/20 O2 already tracks the missing UiScale accessor). The measurement trick is the best the current public API allows; the IMGUI reference list keeps a constant (`ImguiRowHeight`, renamed from `RowHeight`) because that window renders with Unity's GUI skin, which KSP scales independently of the library's uiScale. The docs/20 RowHeight recommendation (docs/20-widgets.md:423) remains for contract C16.
- **G3-33** (`BenchmarkUI.cs`): the IMGUI reference window's virtualization toggle now applies the new state only during `EventType.Layout`. Toggling mid-pass (a click is an input event) previously laid out a different control count than the Layout pass had built, and the next Repaint threw the one-frame "Getting control N's position in a group with only M controls" ArgumentException. One-frame visual lag on the toggle, standard IMGUI practice.
- **G3-36** (`TelemetrySampler.cs`): the four channel rings are cleared whenever `FlightGlobals.ActiveVessel` changes identity — vessel switch, switch to no vessel, and scene change all reset, so two vehicles' traces never concatenate in one history. Implemented as a per-frame `ReferenceEquals` against the last sampled vessel with ring recreation (fresh `RingBuffer` instances) on change; the panels read through the sampler's properties every frame, so they pick up the new instances with no re-wiring and no event subscription lifecycle. Deviation from the item text's suggestion: GameEvents vessel events were not used — the reference comparison covers the same transitions (including the to-null case events don't uniformly cover) with less plumbing; `RingBuffer.cs` has no `Clear()` and was left untouched (out of C13's file list).
- **G3-39** (`PlotDemo.cs`): `Tick` skips frames whose `unscaledDeltaTime` is `<= 0`, NaN, or Infinity before touching the EMA or the rings. Previously a single degenerate delta seeded Inf/NaN into the `Mathf.Lerp` EMA, from which the `-1f` sentinel could never recover (every later frame re-lerped the poisoned value); the frame-time ring also ingested `dt * 1000` = 0/Inf/NaN.
- **G3-41** (`DemoConsumer.cs`): `OnDestroy` now calls `Unregister` only when `Start` actually registered (`_registered` flag). Previously every scene's `OnDestroy` called `Unregister` unconditionally; with the library unavailable/dormant that logged the facade's "Unregister ignored: not available" warning once per scene, spuriously.
- **G4-02** (`BenchmarkUI.cs`): `UpdateFpsEma` rejects `dt <= 0`/NaN/Infinity samples instead of seeding the EMA with them. A zero first-frame delta used to print `Infinity` for that frame AND leave a near-zero EMA that the 0.05 lerp washed out only over dozens of frames (sweep's PARTIAL correction: the inflation persisted, not one frame). The shared `FpsReadoutLine` helper shows `FPS: --` until the EMA holds a real sample; both the ImGui and IMGUI windows use it.

### Inputs
- Triage sweep verdicts (`TRIAGE_SWEEP.md`): G2-12 VALID (BenchmarkUI.cs:21), G3-33 VALID (classic Layout/Repaint mismatch), G3-36 VALID (TelemetrySampler.cs:51), G3-39 VALID (unrecoverable Inf/NaN), G3-41/G4-03 VALID (spurious per-scene warning), G4-02 PARTIAL-upgraded (persists dozens of frames).
- Public API surface survey for G2-12: facade + ImGuiEx + ImGuiDraw + ImGuiGradients + ImGuiPlot public members enumerated; no style-metric accessor exists.

### Outputs
- Changed: `DearImGuiKSPDemo/BenchmarkUI.cs`, `DearImGuiKSPDemo/Telemetry/TelemetrySampler.cs`, `DearImGuiKSPDemo/PlotDemo.cs`, `DearImGuiKSPDemo/DemoConsumer.cs`.
- New: `notes\active\2026-09-07_Bug_OpusReviewTriage\CHUNK_C13_CONTRACT.md` (this record).
- No new members on any public surface; the demo's changes are internal to its own classes.

### Constraints
- §5.9 native-interop/hot-path checklist verdicts for the per-frame demo paths:
  - **G2-12 (row-pitch measurement):** adds exactly two `GetCursorScreenPos` P/Invokes and a handful of float ops per frame to the already-virtualized path; zero managed allocation (Vector2 struct reads; the measured value is an instance field). The first visible row is declared once — measurement reuses the real row, not an extra item. Verdict: no added steady-state allocation; per-frame native call count strictly bounded.
  - **G3-33 (toggle guard):** one `Event.current.type` comparison per OnGUI pass; no allocation, no native calls. Verdict: negligible.
  - **G3-36 (vessel-change clear):** steady state is one `ReferenceEquals` per frame; ring recreation (4 × 80 KB of preallocated arrays) happens only on the rare vessel-change event, never per frame. Verdict: no steady-state cost; allocation is event-driven and replaces data the old rings held anyway.
  - **G3-39 / G4-02 (dt guards):** three float comparisons per frame per window; the skipped frame simply doesn't push a sample (ring windows show a one-frame plateau on a degenerate delta, never a poisoned series). Verdict: no allocation, no added calls.
  - **Process-global state added:** none. `_rowHeight`, `_sampledVessel`, `_registered` are per-instance demo fields on the single frame-loop thread. Verdict: clean.
  - **Resource-acquisition symmetry:** no Begin/End pairing touched; the measurement sits between the existing `BeginScrollRegion`/`EndScrollRegion` scope exactly like the row draws it replaces. Verdict: N/A, symmetry preserved.
- Scope discipline held: library projects untouched; `Telemetry/OrbitPanel.cs`, `Telemetry/StageAnalyzer.cs`, `Telemetry/RingBuffer.cs`, `Telemetry/GraphPanel.cs`, `Telemetry/StagePanel.cs`, `Telemetry/TelemetryAddon.cs` untouched (C12's + untracked files); the temporary F1–F4 fault-injection section in DemoConsumer.cs left verbatim for the later in-game gate; no new dependencies.
- Known gap recorded (G2-12): the public API cannot report style metrics; the live measurement is the best available public accessor. If the library ever exposes CalcTextSize/TextLineHeight/UiScale (SemVer minor), the demo should switch to it — C16's docs/20 RowHeight note should describe whichever mechanism survives.

### Verification
- `dotnet build DearImGui-KSP.slnx` green: **0 errors, 0 warnings** (Debug; demo project included).
- No demo test project exists (correctness by review + in-game gate per contract); no library tests were added or affected.
- Review passes: BenchmarkUI virtualized list re-read end-to-end (scroll math, firstVisible clamp, measurement guard, trailing Dummy); IMGUI toggle guard re-read against the Layout/Repaint replay model; TelemetrySampler clear path re-read against GraphPanel's per-frame property reads; PlotDemo/BenchmarkUI dt guards re-read for sentinel recovery; DemoConsumer flag set/cleared symmetrically in Start/OnDestroy.

### Gate C (in-game) check items
- Benchmark window at non-1.0 uiScale AND non-1.0 fontScale (library settings panel): virtualized ImGui list rows stay evenly spaced with no overlap/clipping while scrolling (also try the ProggyClean/default font setting); row pitch tracks the scale live after a mid-session change.
- IMGUI Reference window: spam-click "Use Virtualization" — no ArgumentException in the log (KSP.log / Alt-F12), toggle still flips within a frame.
- Telemetry window Graphs tab: fly, switch to another vessel (or recover then switch), confirm the graphs reset instead of concatenating the two vehicles' traces; return to a scene with no vessel and confirm no stale trace remains.
- Scene changes (menu → flight → tracking station → menu) with the demo installed: KSP.log shows no "Unregister ignored" warnings from `DearImGuiKSPDemo`.
- Benchmark/plot FPS readouts: no `Infinity`/`NaN`/inflated FPS on the first frames after scene load; both readouts agree.

### Rollback
- `git checkout -- DearImGuiKSPDemo/BenchmarkUI.cs DearImGuiKSPDemo/Telemetry/TelemetrySampler.cs DearImGuiKSPDemo/PlotDemo.cs DearImGuiKSPDemo/DemoConsumer.cs`; delete this contract record.

---

## Addendum C13b (2026-09-07, Gate C follow-up): IMGUI toggle non-responsive

**Defect in the C13 G3-33 fix:** applying the toggle's new value only when `Event.current.type == EventType.Layout` fixed the Layout/Repaint mismatch but made the toggle dead — toggle clicks register on input events (MouseUp), and those passes never satisfied the Layout condition, so `_imguiVirtualized` never changed.

**Fix (`BenchmarkUI.cs` only):** standard IMGUI stash-and-commit. The toggle's returned value is stashed into `_pendingImguiVirtualized` on every OnGUI pass (a click's input-event pass is where it actually flips); the commit to `_imguiVirtualized` — the state driving the control tree — happens only during a Layout pass. A click therefore lands one frame later, at the start of a frame, and every pass of that frame (Layout, Repaint, input) sees one consistent branch, so the G3-33 guarantee (no mid-pass tree change → no ArgumentException) holds. Verified: `dotnet build DearImGui-KSP.slnx` 0 errors, 0 warnings.

**Gate C re-check:** click "Use Virtualization" in the IMGUI reference window — the mode flips within a frame AND repeated spam-clicking still produces no "Getting control N's position..." ArgumentException in the log.
