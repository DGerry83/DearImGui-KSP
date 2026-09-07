# HANDOFF — Opus Review Remediation (2026-09-07)

**Session:** `notes\active\2026-09-07_Bug_OpusReviewTriage\` (FlyByWire Bug/PlanImplementation hybrid run)
**For:** next session — finish Gate B3 confirmation, cleanup, Gate D release.
**Repo:** `C:\Users\Matt\source\repos\DearImGui-KSP`. Test install (auto-deployed on every managed build): `C:\SSDGames\DearImGui-KSP_TESTING`.

## Where things stand

ALL 16 contracted work packages are complete and committed. Build green, `dotnet test DearImGui-KSP.slnx` = **178/178**, working tree clean. The user's Claude-Opus review (~270 items) was triaged (TRIAGE_SWEEP.md), chunked into contracts (`notes\plans\opus-review-remediation-contracts.md`), executed per contract with one commit each, and verified in-game through Gates A/B/C/B2.

### Commit ledger (newest first)

| Commit | Contract | Content |
|---|---|---|
| f2738f4 | — | Stages/dV tab REMOVED from demo (user decision; closes G2-15, G3-34/35/40) |
| d667628 | C13c | IMGUI virtualization toggle: stash only on click passes (Repaint overwrote pending flip) |
| fcf0272 | — | Temporary verbose-gated `[MAPDIAG]` map-view lock heartbeat |
| f89c3a2 | C08b | Knob/Wheel native shims strip `##`/`###` for display (identity unchanged; PIN_RECORD updated) |
| 38965f2 | C12b | StageAnalyzer pooled-fuel + situation Isp (SUPERSEDED by f2738f4 removal) |
| 1d9fd5e / 4944a62 | C15/C16 | Docs housekeeping (33 doc items; AGENTS.md updated per repo rule) |
| e21c466 | C13b | IMGUI toggle stash-and-commit (SUPERSEDED by d667628) |
| 1639f80 | C11 | Packaging/metadata: settings.cfg out of zip, KSP 1.12 pins, raw AVC URL, demo License/Readme, gate fixes, DGerry83 metadata |
| f876f4b | C14 | Test hardening (32 new tests incl. ABI enum pins) + G3-20 residual closure |
| 594a703 / b744e7e | C12/C13 | Demo math fixes (orbit panel kept) / demo UI correctness |
| e4619cc | C08 | Widget correctness & label/ID family (13 items) |
| 4d7edeb / 5b6e71a / d9ecca2 | C06/C09/C10 | Debounced atomic settings writes; unscaled UI/tween timing + infra hygiene; dead IBM Plex Medium load dropped |
| 12cac8a | C07 | ABI enum/count/capacity validation guards |
| 99159be / f27cef3 | C04/C05 | Native rendering/diagnostics (gradient child retarget, ImGui error→KSP.log channel, uiScale 1px floor, D3D11 failure latch); build tooling (pin enforcement via check_build_env.bat, release PDBs, vcvars fallback) |
| 7ea11d0 / 79c03a0 | C01/C02 | Frame-boundary fault model (S1/S2/G2-05); tween fault containment (S3) |
| c359812 | — | Temporary F1-F4 fault-injection buttons in demo (STILL PRESENT — remove before release) |

### In-game gate results

- **Gate A PASS:** F1-F4 fault tests all contained (log-verified). G2-U1 unreachable (no scene-change-while-F2-hidden path) and G2-U2 disproven (gizmos blocked) → conditional contracts C17/C18 NOT scheduled.
- **Gate B+C:** items 1-6, 8-10, 12, 14, 15 passed; three fails fixed (knob `##`, IMGUI toggle, dV) — dV later removed entirely per user decision.
- **Gate B2:** knob label PASS; map-rotation NOT reproducible (diagnostic left armed); IMGUI toggle failed again → fixed properly in d667628 (C13c), needs one more user look.

## Remaining work (in order)

1. **Gate B3 (user, one quick look):** IMGUI reference window "Use Virtualization" toggle flips; telemetry window shows only Graphs + Orbit tabs. If the toggle STILL fails, read `DearImGuiKSPDemo\BenchmarkUI.cs:210-235` (the stash/commit block) and check KSP.log for IMGUI exceptions.
2. **Remove temporary test scaffolding** (single commit):
   - `DearImGuiKSPDemo\DemoConsumer.cs`: the F1-F4 fault-injection section — fields `FaultProbeId`/`_faultProbeRegistered`/`_pendingOutOfFrameProbe`/`_faultStatus`, the `Update()` method, `OnFaultProbeFrame()`, the button block at the end of `OnFrame`, and the class-doc line mentioning F1-F4. All marked "temporary"/"remove before release".
   - `DearImGuiKSP\Infrastructure\DearImGuiKSPAddon.cs`: `MapViewLockDiagnostic()` + its call in `Update()` + the four `_mapDiag*` fields.
   - `DearImGuiKSP\Application\InputCaptureTracker.cs`: the temporary `MouseCaptured` property (only consumer is the diagnostic).
   - Keep the pre-existing "Throw inside scope (test)" button — it predates this wave (M1 gate hook), not part of the removal.
3. **Gate D (release candidate):**
   - `cmd //c package_release.bat` end-to-end; inspect both zips (library: no settings.cfg, has License/Docs/Textures; demo: DLL+.version+License+Readme, named from demo version).
   - **Version sync check:** C11 added `VERSION` fields to the source `.version` templates — csproj `<Version>` and `.version` template must be bumped together. Both csproj + both `.version` → **1.0.1** (patch: no public API added; D36 lockstep managed+native). Demo's `KSPAssemblyDependencyEqualMajor` needs NO bump (major unchanged).
   - CHANGELOG 1.0.1 entry (this wave's user-facing fixes; docs agents flagged nothing blocking).
   - Full `dotnet build -c Release` + `DearImGuiKSPNative\build_release.bat` (PDB emitted in build\ — currently NOT shipped in the zip; decide consciously) + `dotnet test` (178/178 baseline).
   - Clean-KSP zip-install check per the D16 convention (as done for 1.0.0).
   - Update AGENTS.md "Current status" line after release.
4. **Session wrap-up:** move `notes\active\2026-09-07_Bug_OpusReviewTriage\` → `notes\finished\` per taxonomy.

## Decisions made — do not reopen

- INVALID/NOTE triage items are settled (see TRIAGE_SWEEP.md verdict tables); G3-U1/U2/U3 low-tier UNCERTAIN items are explicitly unscheduled.
- No PushFont/font-selection API (G3-10): dead IBM Plex Medium load dropped instead; font API is a potential 1.1.0 feature via DesignSpecRefinement. Same 1.1.0 bucket: a public style-metric/UiScale accessor (C13 recorded the gap; docs/20 now works around it).
- dV parity with stock is abandoned — requires stock's crossfeed/flow-priority simulation; findings preserved in KSP Knowledge Library `NOTES\stock-deltav-simulation.md` (+ `NOTES\mapview-camera-input.md` from the rotation investigation). Both files are in the separate KL repo, intentionally left uncommitted there.
- Native handshake is now version **7** (C04 added the diagnostics-drain export). Any future export addition → 8, lockstep managed+native.
- Release zip deliberately excludes settings.cfg (upgrades must not reset player settings); runtime tolerates its absence (defaults + first debounced save recreates).

## Pointers

- Contract specs: `notes\plans\opus-review-remediation-contracts.md` (status line is current).
- Per-contract records incl. §5.9 verdicts and gate evidence: `CHUNK_C01..C16_CONTRACT.md` in this folder.
- FlyByWire workflow assessment (delivered): `C:\Users\Matt\source\repos\FlyByWire\notes\2026-09-07_workflow-assessment-dearimgui-ksp-review.md`; seven new `notes\knowledge\` notes in this repo.
- C08 follow-up recorded in CHUNK_C08_CONTRACT.md: Toggle needed no patch (upstream correct); Knob/Wheel patched in vendor trees with PIN_RECORD entries.
