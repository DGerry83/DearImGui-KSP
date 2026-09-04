# Frozen Gates: DearImGui-KSP Pre-Release Feature Wave Bootstrap
## Date Frozen: 2026-09-03
## Session: `notes/active/2026-09-03_NewProject_PreReleaseFeatureWave/`

Adaptation note (recorded at freeze time, part of the frozen record): this wave bootstraps
onto an existing, verified codebase. G4 and G5 are therefore judged against the
pre-existing skeleton from the 2026-07-29 bootstrap plus this session's delta skeleton
(`PROJECT_SKELETON.md`), not against a greenfield `src/` tree.

| Gate ID | Criterion | Evidence Required | Verdict |
|---------|-----------|-------------------|---------|
| G1 | `DESIGN_SPEC.md` exists and is complete (scope, architecture + patterns, UI/UX, assets/strings, compatibility) | File reviewed — `notes/finished/2026-09-03_DesignSpec_Theming_Extensions_Showcase/DESIGN_SPEC.md`, all sections present, user-confirmed 2026-09-03 | PASS |
| G2 | `PLANNING_WORKSHEET.md` covers Steps 1–8 | Worksheet reviewed — all 8 steps filled, grounded in the current codebase (handshake v4 sites, 11 existing externs, build-script file lists) | PASS |
| G3 | `IMPLEMENTATION_PLAN.md` follows `05-output-format.md` | Plan reviewed — sections 1–10 populated, self-verification checklist passed, 8 sequential milestones with observable criteria | PASS |
| G4 | Layered project skeleton exists (Core/Application/Infrastructure separation + tests) | Directory tree inspected — pre-existing layers verified (`DearImGuiKSP/Application|Infrastructure|Interop`, `DearImGuiKSPNative/src`, `tests/`); wave delta created: `Application/{Theming,Animation,Api}/`, `DearImGuiKSPDemo/Telemetry/`, `src/shims/`, `vendor/PIN_RECORD.md`, `docs/` stubs | PASS |
| G5 | `AGENTS.md` and `README.md` created | Files reviewed — root `AGENTS.md` and `README.md` exist and are current for this wave; no changes required | PASS |

**Session Verdict**: CONTINUE

GATES.md is frozen. Post-freeze modification renders the affected gate INVALID (per `07-frozen-gates.md`).
