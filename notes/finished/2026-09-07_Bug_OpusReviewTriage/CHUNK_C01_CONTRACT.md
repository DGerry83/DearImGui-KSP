# Chunk Contract: C01 — Frame-boundary fault model
## Plan: Opus Review Remediation (`notes\plans\opus-review-remediation-contracts.md`)
## Date: 2026-09-07
## Chunk ID: C01
## Advances Milestone: Post-1.0.0 remediation wave (severe items S1, S2 + G2-05)

### Scope
- **S1**: `FrameLoopOrchestrator.RunFrame` iterates a registry snapshot and wraps the frame in try/finally so `EndUiFrame()` (native SRWLOCK release) always runs; `ConsumerRegistry` gains a lazily-refreshed `OrderedSnapshot` (dirty on register/unregister); mid-frame registration changes apply from the next frame. `InputCaptureTracker` switches to the snapshot too (similar-bugs sweep: the only other live-list iteration).
- **S2**: facade gains `FrameOpen` (set by the orchestrator between BeginUiFrame/EndUiFrame) and internal `CanDeclareUi = IsAvailable && FrameOpen`; every widget guard across the facade and Api partials switches to it. Register/Unregister/Tween stay on the session-level `IsAvailable`. Out-of-frame widget calls become the documented safe no-op instead of a native null-deref.
- **G2-05**: new `OpenScopeTracker` (Application) counts facade Begin/End and Push/Pop pairs per frame; `FaultBarrier` unwinds open scopes via `DearImGuiKSP.UnwindOpenScopes()` when a consumer throws, so later consumers are not mis-parented. Unwind order: style vars, style colors, plots, subplots, tab items, tab bars, scroll regions, windows.

### Inputs
- Triage sweep verdicts S1/S2/G2-05 (`notes\active\2026-09-07_Bug_OpusReviewTriage\TRIAGE_SWEEP.md`), all VALID.
- Existing tests: FrameLoopOrchestratorTests, FaultBarrierTests, ConsumerRegistryTests patterns.

### Outputs
- Changed: `Application/FrameLoopOrchestrator.cs`, `Application/ConsumerRegistry.cs`, `Application/FaultBarrier.cs`, `Application/InputCaptureTracker.cs`, `Application/DearImGuiKSP.cs`, `Application/Api/DearImGuiKSP.{Header,Knob,Radio,Spinner,TextColored,Toggle,Wheel}.cs`, `Application/Api/ImGuiDraw.cs`, `Application/Api/ImGuiGradients.cs`, `Application/Api/ImGuiPlot.cs`.
- New: `Application/OpenScopeTracker.cs` (tracker + `IScopeCloser` + native closer), `tests/Application.Tests/FrameBoundaryTests.cs`.
- New members: `DearImGuiKSP.FrameOpen` (internal), `DearImGuiKSP.CanDeclareUi` (internal), `DearImGuiKSP.UnwindOpenScopes(IScopeCloser)` (internal), `ConsumerRegistry.OrderedSnapshot` (internal).

### Constraints
- Public API surface unchanged (all new members internal; D17/D36 versioning: this is a patch-class fix, no API break).
- §5.9 native-interop checklist applies (per-frame code + native lock symmetry):
  - **Process-global state**: `FrameOpen` + scope counters are process-static, scoped-and-restored — FrameOpen cleared in `finally`, counters reset at each frame open. Verdict: acceptable, documented here.
  - **Hot-path allocation**: registry snapshot array allocated only on register/unregister (dirty flag); steady-state frames allocate nothing new; guard adds two bool reads per widget call. Verdict: zero new steady-state allocation.
  - **Resource-acquisition symmetry**: `EndUiFrame` now in `finally` — the native SRWLOCK has a release on every exit path, including consumer-thrown and registry-mutation paths. Verdict: symmetry restored (this was the S1 bug).

### Verification
- `dotnet build DearImGui-KSP.slnx` green; `dotnet test` green including new FrameBoundaryTests:
  - Register/Unregister inside a callback does not throw; EndUiFrame still called; changes apply next frame.
  - Out-of-frame widget call is a no-op (would throw DllNotFound if the guard failed).
  - Unwind closes scopes innermost-first per the order above (recorder IScopeCloser).
  - FrameOpen false after RunFrame; CanDeclareUi true only during callbacks.
- No native source changes; native harness sanity not required (managed-only chunk).
- In-game: gate A (user) — misbehaving consumer no longer hangs/CTDs.

### Gate A result (2026-09-07, user in-game, verbose logging on)
- PASS. Temporary demo fault hooks (commit c359812, remove before release): F1 registered a new consumer from inside OnFrame — probe window appeared, no freeze (KSP.log 09:33:57); F2 fired a widget call from Update outside any callback — silent no-op, no CTD (log 09:34:08). Only F1's register direction was exercised in-game; unregister shares the same snapshot path and is unit-tested. The 3 NREs in the session log are Scatterer/Parallax/stock scene-teardown noise, unrelated.

### Rollback
- Revert the listed files (`git checkout -- <files>`); delete the two new files.
