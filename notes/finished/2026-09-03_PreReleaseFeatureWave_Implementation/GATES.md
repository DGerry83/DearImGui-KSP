# Frozen Gates: DearImGui-KSP Pre-Release Feature Wave Implementation
## Date Frozen: 2026-09-03
## Session: `notes/active/2026-09-03_PreReleaseFeatureWave_Implementation/`
## Frozen By: Agent (on user approval to enter Phase 3)

Project-specific gate additions relative to the template are criteria 6–9, derived from
the plan-specific invariants in `PLAN_DIGEST.md`.

| Gate ID | Criterion | Evidence Required | Verdict |
|---------|-----------|-------------------|---------|
| G1 | Plan digest and chunk map reflect the actual plan file | PLAN_DIGEST.md and CHUNK_MAP.md reviewed | PASS (authored from the plan this session; Phase 0/1 reports) |
| G2 | All chunk contracts define verifiable inputs, outputs, and rollback | CHUNK_*_CONTRACT.md files reviewed as created | PASS (all C1–C36 contracts authored pre-chunk with Scope/Inputs/Outputs/Constraints/Verification/Rollback) |
| G3 | Each implemented chunk compiles/builds with 0 errors; the 59-test xUnit baseline stays green (grows only by added tests) | Build log + test output per chunk | PASS (build 0/0 every chunk; suite grew 59 → 99 by added tests only; final state 99/99) |
| G4 | Integration build passes after stubs removed and wiring connected: `dotnet build DearImGui-KSP.slnx` + all 3 native build scripts + harness | Full build logs | PASS (final state: managed 0/0, all 3 native builds 0 errors, harness 60/61 checks PASS) |
| G5 | Plan coverage check confirms every plan section is implemented or explicitly deferred | FINAL_AUDIT.md coverage table | PASS (CHUNK_MAP C1–C36 all Done in PROGRESS_LOG; every deferral explicit and user-approved — #006/#010/#011/#012/#013 + backlog list) |
| G6 | Handshake v5 lands in lockstep: native `GetVersion()` = 5 and managed `ExpectedNativeVersion` = 5 in the same verified state; a mismatched pair hits the existing version-mismatch failure path | M2 verification evidence (in-game mismatch test) | PASS (compile-time: C4/C5 direct-call evidence; in-game: v4-native/v5-managed popup confirmed by user 2026-09-04; later bumped to v6 in lockstep at C31) |
| G7 | Hot-path budget held: zero steady-state per-frame managed allocation in tween tick, plot submission, and telemetry sampling; benchmark window shows no managed-cost regression | M4/M5/M6 verification evidence | PASS (no-alloc source reviews per chunk; M4/M5/M6 in-game user-confirmed no managed-cost regression; ~0.24–0.33 ms managed frame cost in the verboseLogging run) |
| G8 | D16 compatibility undisturbed at every milestone that touches rendering/input: Deferred, TUFX, Scatterer, Parallax, Cinematic Shaders/Recorder, IMGUI mods; ISSUES #001/#003 mechanisms inert-when-not-capturing | In-game acceptance notes per milestone | PASS (every milestone gate user-verified in the full D16 environment incl. the M8 clean-install zip test; no interop regressions reported) |
| G9 | Docs discipline: XML `///` docs on every new public member; no emojis or symbol glyphs in `docs/`; LICENSE aggregates MIT/0BSD/OFL | M7 verification evidence | PASS (C24: XML-doc sweep clean, emoji sweep clean, README credits aggregate all 8 attributions; C26 purge greps 0 hits; M7 dry run PASS) |

**Session Verdict**: PASS (2026-09-05) — all gates green, M1–M8 VERIFIED by user in-game, wave complete.

GATES.md is frozen as of user approval to enter Phase 3 (2026-09-03). Post-freeze
modification renders the affected gate INVALID per `07-frozen-gates.md`.
