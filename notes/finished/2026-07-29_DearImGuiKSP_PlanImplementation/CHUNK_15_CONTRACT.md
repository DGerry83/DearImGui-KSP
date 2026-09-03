# Chunk Contract: Full Compatibility Environment Validation

## Plan: DearImGui-KSP Implementation
## Date: 2026-09-03
## Chunk ID: C15
## Advances Milestone: M6 (final chunk)

### Scope

- **Test-only chunk** — no code changes (CHUNK_MAP, INTEGRATION_CONTRACT). Full compatibility validation of the D16 environment with the library active: **AC11** — Deferred, TUFX, Scatterer, Parallax, Cinematic Shaders, Cinematic Recorder, and an IMGUI mod run without visual or functional regressions.
- This is the final gate before Phase 4/5; a failure here blocks release explicitly rather than silently (CHUNK_MAP decision).

### Inputs (must exist before starting)

- All prior chunks verified in-game: AC1, AC3, AC4, AC5, AC6, AC7, AC8, AC9, AC10, AC12 PASS (AC2 deferred with C5 per D20).
- ReformTestInstance carries the full D16 mod set (Deferred, TUFX, Scatterer, Parallax, Cinematic Shaders, Cinematic Recorder, IMGUI mods) plus DearImGuiKSP + DearImGuiKSPDemo at the current build (`3fa257a`).
- The demo mod now doubles as the IMGUI coexistence fixture via its IMGUI reference window (C14).

### Test matrix (user-driven, one session is fine; note what was covered)

For each scene below — main menu, flight (with Scatterer/Parallax-visible terrain and atmosphere), VAB/SPH editor, tracking station:

1. **Rendering coexistence**: scene renders normally — no visual corruption, flicker, missing post-processing, or broken planet/atmosphere/terrain effects (Deferred + TUFX + Scatterer + Parallax).
2. **Library windows**: demo window and benchmark window render on top correctly in every scene; toolbar toggle works; windows interactive (hover locks camera, text field editable).
3. **IMGUI coexistence**: the IMGUI reference window (and any other IMGUI mod UI, e.g. stock alarm/cheat dialogs) renders and interacts normally alongside ImGui windows.
4. **Cinematic Recorder/Shaders**: if practical, run a short capture or toggle cinematic shaders; no crashes, hooks coexist.
5. **Lifecycle re-check in full env**: F2 hide/show, a scene transition through a loading screen, and a resolution change — library windows pause/resume/re-render correctly (AC10 behaviors under the full mod stack).
6. **Logs**: after the session, KSP.log and Player.log contain no new exceptions, repeated warnings, or error spam attributable to DearImGuiKSP or to the other mods that wasn't present before the library was installed.

### Constraints

- No code changes. If a regression is found, halt: file it in `ISSUES/` (next ID #003), document in PROGRESS_LOG, and fix upstream before resuming (INTEGRATION_CONTRACT rollback rule).
- Never touch the KSP install beyond files this project deploys.

### Verification

- **AC11 (user, in-game)**: every matrix row above reported clean (or explicitly noted as pre-existing behavior unrelated to the library). Lead cross-checks KSP.log/Player.log afterward.

### Rollback

- Nothing to revert (test-only). A failed AC11 blocks Phase 4 gate G4 and returns to the user for triage.
