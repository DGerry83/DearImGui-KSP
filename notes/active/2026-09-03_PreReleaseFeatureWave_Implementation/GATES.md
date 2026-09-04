# Frozen Gates: DearImGui-KSP Pre-Release Feature Wave Implementation
## Date Frozen: 2026-09-03
## Session: `notes/active/2026-09-03_PreReleaseFeatureWave_Implementation/`
## Frozen By: Agent (on user approval to enter Phase 3)

Project-specific gate additions relative to the template are criteria 6–9, derived from
the plan-specific invariants in `PLAN_DIGEST.md`.

| Gate ID | Criterion | Evidence Required | Verdict |
|---------|-----------|-------------------|---------|
| G1 | Plan digest and chunk map reflect the actual plan file | PLAN_DIGEST.md and CHUNK_MAP.md reviewed | PASS (authored from the plan this session; Phase 0/1 reports) |
| G2 | All chunk contracts define verifiable inputs, outputs, and rollback | CHUNK_*_CONTRACT.md files reviewed as created | Pending (evaluated per chunk) |
| G3 | Each implemented chunk compiles/builds with 0 errors; the 59-test xUnit baseline stays green (grows only by added tests) | Build log + test output per chunk | Pending |
| G4 | Integration build passes after stubs removed and wiring connected: `dotnet build DearImGui-KSP.slnx` + all 3 native build scripts + harness | Full build logs | Pending |
| G5 | Plan coverage check confirms every plan section is implemented or explicitly deferred | FINAL_AUDIT.md coverage table | Pending |
| G6 | Handshake v5 lands in lockstep: native `GetVersion()` = 5 and managed `ExpectedNativeVersion` = 5 in the same verified state; a mismatched pair hits the existing version-mismatch failure path | M2 verification evidence (in-game mismatch test) | Pending |
| G7 | Hot-path budget held: zero steady-state per-frame managed allocation in tween tick, plot submission, and telemetry sampling; benchmark window shows no managed-cost regression | M4/M5/M6 verification evidence | Pending |
| G8 | D16 compatibility undisturbed at every milestone that touches rendering/input: Deferred, TUFX, Scatterer, Parallax, Cinematic Shaders/Recorder, IMGUI mods; ISSUES #001/#003 mechanisms inert-when-not-capturing | In-game acceptance notes per milestone | Pending |
| G9 | Docs discipline: XML `///` docs on every new public member; no emojis or symbol glyphs in `docs/`; LICENSE aggregates MIT/0BSD/OFL | M7 verification evidence | Pending |

**Session Verdict**: Pending (evaluated at Phase 5)

GATES.md is frozen as of user approval to enter Phase 3 (2026-09-03). Post-freeze
modification renders the affected gate INVALID per `07-frozen-gates.md`.
