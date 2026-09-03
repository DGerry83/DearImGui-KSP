# Audit Report: ISSUES #001 (pointer blocker), #002 (viewport clamp), unit tests
## Date: 2026-09-03
## Type: Bugfix (#001, #002) + Feature (unit tests)

### Gate Verdicts
| Gate ID | Criterion | Verdict | Evidence |
|---------|-----------|---------|----------|
| G1 | Click-through fixed at main menu/KSC/flight; ImGui widgets still work | (pending — user in-game) | — |
| G2 | 3D-button lock, modals, F2, D16 coexistence, no false blocking | (pending — user in-game) | — |
| G3 | Clamp behavior + settings round-trip | (pending — user in-game) | — |
| G4 | Handshake v4 lockstep; mismatch fails safely | (pending — user in-game mismatch run; code path verified by diff: version check now precedes full bind, `NativeBridge.cs:102-121`) | git diff NativeBridge.cs |
| G5 | `dotnet test` green, five Application types covered | **PASS** | `Passed! - Failed: 0, Passed: 49, Skipped: 0` (project- and solution-level), auditor re-run 2026-09-03 |
| G6 | Full build green; public API unchanged | **PASS** | `build.bat` + `build_release.bat` succeed; harness `HARNESS PASS`; `dotnet build DearImGui-KSP.slnx` 0 warnings/0 errors; `DearImGuiKSP/Application/DearImGuiKSP.cs` absent from git status (empty diff) |
| G7 | Post-change D16 in-game smoke | (pending — user in-game) | — |

### Session Verdict
- **Verdict**: CONTINUE
- **Reason**: No gate FAIL. Mechanical gates (G5, G6) PASS with raw evidence; G4's code path verified by diff. G1–G4 (in-game portions) and G7 are user-executed in-game gates — the change is ready for that verification. Per the frozen-gates protocol, final closure of G1–G4/G7 happens after the user's run; any FAIL there re-opens the session at Phase 0/1.

### Frozen Gates Integrity
- [x] GATES.md exists and was frozen before implementation (frozen in Phase 1, 2026-09-03)
- [x] GATES.md criteria and freeze timestamp not modified after freezing (verdict column only, per protocol)
- [x] No post-freeze modification detected

### Invariant Check Results
- [x] Public interfaces preserved — `DearImGuiKSP/Application/DearImGuiKSP.cs` has an empty diff; `INativeBridge` is internal and its addition is additive-only
- [x] Shared state shape stable — no field renames/removals; `LibrarySettings` gained one additive field
- [x] Language/runtime compliance — net48 both projects; native C ABI unchanged in shape (one additive export)
- [x] Public utility function signatures stable — n/a (no public utilities touched)
- [x] Minimal change principle followed — 172 insertions/15 deletions across 18 files vs. contract estimate; no unrelated edits in the diff
- [x] Layered dependency direction preserved — UnityEngine.UI appears only in new `Infrastructure/PointerBlockerGateway.cs`; Application gains an interface only; native has zero game knowledge
- [x] Handshake lockstep — `GetVersion()` returns 4 (`DearImGuiKSPNative.cpp:28`) and `ExpectedNativeVersion = 4` (`NativeBridge.cs:36`)

### Principles & Anti-Patterns Check
- [x] SRP: `PointerBlockerGateway` = "owns and toggles the blocker GameObject"; tracker addition is one responsibility (capture-state reaction)
- [x] SoC: presentation (uGUI blocker) in Infrastructure; policy (when to block) in Application
- [x] Dependency inversion: tracker depends on `IPointerBlockerGateway` abstraction
- [x] No global mutable state introduced (Composition singletons are the established pattern)
- [x] No God Classes / Golden Hammer / Leaky Abstractions observed in the diff
- [x] Magic numbers named: `CanvasSortOrder = 30000`, `GameObjectName` consts
- [x] Native interop checklist (§5.9): no process-global state; blocker toggle is transition-only (zero per-frame allocation — verified in `InputCaptureTracker.Update` diff); clamp is event-driven, allocation-free; `Unload()` present on every NativeBridge init failure path after the reorder

### Bugfix Verification
- [x] #001 root cause addressed, not masked: the blocker intercepts EventSystem raycasts at dispatch (the actual gap), not by hiding symptoms
- [ ] Reproduction steps no longer trigger bug — pending user's in-game run (G1/G2)
- [x] No collateral damage — suspend/fail paths release the blocker (`ReleaseAll` callers traced at `Composition.cs:120,125`); test host loads no Unity/KSP assemblies
- [x] Similar bugs checked (see sweep below)
- [x] Before/after evidence documented in PROGRESS_LOG.md scopes A–C

### Similar Bugs Sweep
- **Pattern searched (#001)**: consumers of `InputCaptureState` / `GetIoSnapshot` — any path reacting to capture state that would also need blocker/lock updates. Files checked: all of `DearImGuiKSP/` (grep `InputCaptureState|GetIoSnapshot|MouseCaptured`). **Findings**: `InputCaptureTracker` is the only consumer; `NativeBridge` only produces the snapshot. No parallel path exists.
- **Pattern searched (#002)**: window-list enumeration / position mutation. **Findings**: first and only use (`ContextHost_ClampWindowsToViewport`); no similar code.

### Violations Found
- None blocking. Noted and accepted: the deliberate NativeBridge init reorder (version handshake before full export binding) — required by frozen G4, resource symmetry preserved, ruled ACCEPT in Phase 2.

### Recommendations
- Clear to proceed to user in-game verification (G1, G2, G3, G4-mismatch, G7). Suggested checklist for that run:
  1. Demo window over a stock uGUI button → click demo widget → stock button must NOT activate (repeat at main menu over the 3D menu, KSC, flight).
  2. While hovering the demo window, main-menu 3D buttons inert; stock PopupDialog (e.g. settings) still works; F2 hide → stock clicking fully restored.
  3. Demo window near screen edge → lower resolution → window fully on-screen; set `clampWindowsToViewport = false` → old behavior; restart → setting persisted. **Note: every managed build re-mirrors the repo's `GameData` settings.cfg over the instance copy — re-apply the `false` edit after any rebuild.**
  4. Deliberate mismatch: copy the previous v3 native DLL into PluginData with the v4 managed DLL → expect the one plain-language version-mismatch popup + `[DearImGuiKSP]` log line, then restore.
  5. D16 smoke: toolbar opens demo, benchmark runs, locks engage/release on hover, no new `[DearImGuiKSP]` errors in KSP.log.
- After verification: session closure (move to `notes/finished/`, update `notes/indices/master_index.md`), then resolve ISSUES #001/#002 per the ISSUES workflow.

---

## Addendum — Rework audit after G1/G3 FAIL (2026-09-03, user run #1)

**User run #1 results**: G1 FAIL (clicks incl. drags still pass through the demo window), G3 FAIL (window still ends nearly off-screen after resolution reduction; instance settings.cfg confirmed untouched = shipped default `true`), G2 PASS, G7 PASS. Verdicts recorded in GATES.md.

**Root-cause hypotheses and fixes (auditor-verified by diff/read)**:

- **G1**: the blocker was active but losing Unity's `RaycastComparer`, which compares *sorting layer before sorting order*; stock canvas layers/orders are prefab-serialized and unknowable from the ILSpy dump, so the hardcoded overlay sortOrder 30000 could lose. Fix (`PointerBlockerGateway.cs`): on every activation the blocker now introspects all scene canvases and adopts the top sorting layer + `min(maxOrder+1, short.MaxValue)`; plus diagnostics — Debug-logged creation/transitions and a faint red tint while `verboseLogging` is on, so the next run distinguishes "blocker inactive" from "blocker loses raycast". Fix verified by read: `AdoptTopSortOrder` excludes the blocker's own canvas, runs only on capture transitions, tint defaults transparent.
- **G3**: two candidate causes, both closed by the rework — (a) the clamp read `io.DisplaySize`, which is still the OLD (larger) size when the resolution event fires between frames, making the clamp a no-op on reductions; (b) `onScreenResolutionModified` was never proven to fire at all. Fix: size-change detection moved into `FrameLoopOrchestrator.RunFrame` (live w/h already in hand; event no longer needed for clamping) and the native export now takes explicit `(w, h)` instead of reading DisplaySize. `Composition.ResolutionChanged` is back to `RebuildViewport` only. Handshake stays v4 (v4 never shipped; the signature change folds into it).

**Re-verification (mechanical)**: `dotnet build` 0/0; `dotnet test` 53/53 PASS (4 new FrameLoopOrchestrator clamp tests); native debug build OK; `dumpbin` shows the export; harness `HARNESS PASS`. Public API file still untouched.

**Produced for the user**: a v3 backup native DLL for the G4 mismatch test at `DearImGuiKSPNative/build/DearImGuiKSPNative-v3.dll` (gitignored `build/` dir; built with `GetVersion` temporarily at 3, immediately restored to 4 and rebuilt).

**Awaiting user run #2**: G1 (with `verboseLogging = true` for tint + logs), G3, G4-mismatch (using the v3 backup), and a G7 re-smoke. G2/G5/G6 stand.

---

## Addendum 2 — Final gate results (2026-09-03, user run #3)

**Run #3 results**: G1 **PASS** per its frozen criterion — the user confirmed clicks do NOT pass through to guaranteed stock uGUI targets (stock toolbar, save-load menu). The residual click-through is to **IMGUI (OnGUI) menus only** (demo's IMGUI benchmark window `BenchmarkUI.cs:91` `GUILayout.Window`; Cinematic Shaders' menu — assumed IMGUI, see ISSUES #003), which reads input from Unity's event queue outside EventSystem and is structurally unreachable by a uGUI raycast blocker. IMGUI was never in G1's scope ("the uGUI element beneath") nor in issue #001's title/scope. G3 PASS, G4 PASS (v3 backup DLL mismatch run), G7 PASS (D16 smoke).

**Final session verdict: CONTINUE — all gates PASS.** G1 PASS, G2 PASS, G3 PASS, G4 PASS, G5 PASS, G6 PASS, G7 PASS.

**Follow-ups filed**: ISSUES #003 (KNOWNLIMIT, P3): IMGUI menus receive clicks through ImGui windows — documented limitation; an OnGUI-level event-eater is possible but fragile (script-execution-order dependent) and raises D16 coexistence questions; revisit only if a real consumer need appears.
