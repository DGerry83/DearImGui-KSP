# Status & Next Steps — DearImGui-KSP (2026-08-31)

Snapshot of where the project stands against `DESIGN_SPEC.md` and `IMPLEMENTATION_PLAN.md`, taken after the project rename. Sources: `notes/active/2026-07-29_DearImGuiKSP_PlanImplementation/` (PROGRESS_LOG, HANDOFF, CHUNK_MAP, GATES) and a spot-check of the code on 2026-08-31.

## Recent event: project rename (2026-08-31)

Repo/project renamed **Dear KSP → DearImGui-KSP** (full rename: assemblies, namespaces, GameData folders, log prefix `[DearImGuiKSP]`, dependency key, docs). Identifiers use `DearImGuiKSP` (no hyphen — C# rules); repo/solution/prose use `DearImGui-KSP`. Managed and native builds verified post-rename (0 errors); old deployed copies removed from the test instance. Safe because nothing has shipped — post-release this would be a breaking change.

## Milestone status (IMPLEMENTATION_PLAN §9)

| Milestone | Chunks | Status |
|---|---|---|
| M1 Build pipeline + deployment | C1 | **DONE** — in-game `[DearImGuiKSP]` startup line verified (commit fbbb33e) |
| M2 Render-injection PoC | C2–C4 | **DONE (D3D11)** — AC1 PASS in-game with Deferred+TUFX (commit 225ecb6). C5 OpenGL backend **deferred** per D20 (commit 55bec8e) |
| M3 Consumer API + core widgets | C6–C8 | **DONE** — C6 interop, C7 facade/registry/frame loop, C8 demo consumer: **AC3 + AC4 PASS in-game 2026-08-31** (window renders all MVP widgets, toolbar toggle, correct load order) |
| M4 Input locking + fault isolation | C9, C10 | **IMPLEMENTED (group A, 2026-08-31)** — C9 capture+locks (handshake v2, PoC scaffolding removed), C10 fault barrier. Awaiting user in-game verification: AC7, AC9 (temp fault probe in demo) |
| M5 Settings + lifecycle + failure UX | C11–C13 | **C11 DONE (group A)** — settings store/model/migration + verbose-logging gate + kill switch. Awaiting user in-game verification: AC12. C12/C13 not started |
| M6 Benchmark + compatibility | C14, C15 | Not started |

## Code state (verified against notes)

- Implemented: public API (`Application/DearImGuiKSP.cs`), `ConsumerRegistry`, `FrameLoopOrchestrator`, `Interop/` bindings, `NativeBridge` (with C7-amended `BeginUiFrame`/`EndUiFrame` split), addon + `Composition`, logger, native `ContextHost` + D3D11 backend.
- Skeletons awaiting their chunks (as expected): `InputCaptureTracker`, `FaultBarrier`, `LifecycleStateMachine`, `SettingsModel`, `InputLockGateway`, `GameEventHooks`, `SettingsStore`, `FailureNotifier`.
- `GameData/DearImGuiKSP/settings.cfg` exists, matches spec §9.1 defaults.

## Open items / known discrepancies

- **Gates** (`PlanImplementation/GATES.md`): G1 PASS; G2–G6 PENDING. AC1, AC3, AC4 passed; AC2 deferred (OpenGL); AC5–AC12 pending. Next practical blocker: G2 contracts for C9/C10/C11 before parallel group A starts.
- ~~**Dead PoC scaffolding**~~ — **closed in C9 (2026-08-31)**: `s_DemoWindowVisible`, the `SetDemoWindowVisible` export, and the `ShowDemoWindow` call removed from `ContextHost.cpp`; native handshake bumped to v2.
- **README.md**: refreshed 2026-08-31 (status line, `DearImGui-KSP.slnx` build command, props.user filename). Rest of the README verified accurate.
- **OpenGL backend (C5)** remains deferred per D20 — revisit before M6 compatibility validation.

## Next steps (in planned order)

1. ~~C8~~ — **DONE 2026-08-31** (AC3/AC4 PASS; includes user-requested ApplicationLauncher toolbar toggle with green placeholder icon — toggle itself still unverified in-game).
2. ~~Parallel group A~~ — **IMPLEMENTED 2026-08-31** (C9 input capture + locks, C10 fault barrier, C11 settings). Contracts: CHUNK_9/10/11_CONTRACT.md. **User verifies in-game**: AC7 (locks on hover/text focus, release cleanly), AC9 (temp `DearImGuiKSPDemoFaultProbe` auto-disabled after 5 throwing frames, demo unaffected — remove probe after PASS), AC12 (settings round-trip, migration, `enabled=false` dormancy, `verboseLogging=true` debug lines).
3. **C12** lifecycle state machine + game-event hooks → **C13** failure notifier.
4. **C14** torture-test benchmark → **C15** full compatibility validation (Deferred, TUFX, Scatterer, Parallax, Cinematic Shaders, Cinematic Recorder, IMGUI mods).
5. Phase 4/5 gates: G4 integration build, G5 `FINAL_AUDIT.md` coverage table.
