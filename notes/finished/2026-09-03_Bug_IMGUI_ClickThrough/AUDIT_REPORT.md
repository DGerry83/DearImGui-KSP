# Audit Report: ISSUES #003 — IMGUI (OnGUI) click bleed-through
## Date: 2026-09-03
## Type: Bugfix

### Gate Verdicts
| Gate ID | Criterion | Verdict | Evidence |
|---------|-----------|---------|----------|
| G1 | Mouse/drag/scroll no longer reach IMGUI windows beneath (demo benchmark window + Cinematic Shaders menu); ImGui widgets normal | (pending — user in-game) | — |
| G2 | D16 regression: IMGUI mods normal when not covered; #001 uGUI blocking intact; dialogs/F2/scene transitions clean | (pending — user in-game) | — |
| G3 | Keyboard bleed blocked while keyboard-captured; IMGUI text fields normal otherwise | (pending — user in-game) | — |
| G4 | Mechanical: build 0/0, tests green, native untouched, public API unchanged, no per-frame allocation | **PASS** | `Build succeeded. 0 Warning(s) 0 Error(s)`; `Passed! Failed: 0, Passed: 59` (auditor re-run); `DearImGuiKSP.cs` empty diff; tracker diff is bool-compares + transition-only calls |
| G5 | On pass: spec amendment + D23 + README + ISSUES #003 closure | (pending — conditional on G1–G3) | — |

### Session Verdict
- **Verdict**: CONTINUE (mechanical gates green; final closure conditional on user's in-game G1–G3, then G5 doc application)

### Frozen Gates Integrity
- [x] GATES.md frozen before implementation (Phase 1, 2026-09-03); criteria unmodified since

### Invariant Check Results
- [x] Public interfaces preserved — `Application/DearImGuiKSP.cs` empty diff; new interface is internal
- [x] Shared state shape stable — no renames/removals
- [x] Language/runtime compliance — net48; no native change (handshake stays v4)
- [x] Minimal change — 182 insertions/13 deletions across 6 files + 2 new files; matches contract table (plus the one flagged extra ctor call-site fix in FrameLoopOrchestratorTests.cs, ruled ACCEPT)
- [x] Layered dependency direction preserved — `Event.current`/OnGUI confined to Infrastructure; Application gained an interface only
- [x] Native interop & hot-path checklist — tracker additions are transition-only bool compares (zero allocation); eater OnGUI is per-event bool checks; eater created only post-init-success (failed init → no eater); ReleaseAll clears both shields (F2/loading verified by trace at Composition.WireLifecycle handlers)

### Principles & Anti-Patterns Check
- [x] SRP: blocker = uGUI raycasts; eater = IMGUI events; tracker = transition mapping
- [x] Dependency inversion: tracker depends on `IImguiEventEaterGateway`
- [x] No global mutable state beyond the established composition-root singletons
- [x] Magic numbers named (`ExecutionOrder = -32000` const with rationale comment)
- [x] Safety property documented on the class: inert when not shielded; Layout/Repaint never eaten

### Bugfix Verification
- [x] Root cause addressed at the actual mechanism (event-queue starvation), not masked
- [ ] Reproduction no longer triggers — pending user in-game run (G1–G3)
- [x] No collateral damage — untouched paths verified by diff: uGUI blocker, lock mask, clamp, public API, native
- [x] Similar bugs checked (sweep below)
- [x] Before/after evidence in PROGRESS_LOG.md

### Similar Bugs Sweep
- **Pattern searched**: other consumers of `InputCaptureState` / capture transitions needing the new shield (grep across `DearImGuiKSP/`). **Findings**: `InputCaptureTracker` remains the only consumer; no parallel path. Keyboard-capture was previously only consumed by the lock mask — now also drives the keyboard shield (covered).

### Violations Found
- None.

### Recommendations — user in-game checklist (G1–G3)
1. **G1**: demo ImGui window over the demo's IMGUI benchmark window → clicks, drag of the IMGUI title bar, and scroll must do nothing to the IMGUI window; ImGui widgets work normally. Repeat over Cinematic Shaders' menu.
2. **G2**: with no overlap, the IMGUI benchmark window and Cinematic Shaders' menu must behave exactly as before (open/click/drag/type); stock toolbar + save-load menu still blocked under ImGui windows (#001 regression); PopupDialogs normal; F2 hide/show and a scene change leave nothing stuck.
3. **G3**: focus an ImGui text field, move the pointer OFF the window, type → nothing reaches an IMGUI text field behind. Then click an IMGUI text field with no ImGui capture → typing works normally.
4. Note: verboseLogging tint from the #001 diagnostics is still available if anything looks off (`verboseLogging = true`; re-apply after rebuilds).
- On PASS: parent applies G5 (spec §5.3/§10.4/§13 amendment, DECISION_LOG D23, README limitation update, ISSUES #003 resolution) and closes the session.

---

## Addendum — Mechanism v2 rework after G1 FAIL (2026-09-03)

**User run (v1)**: G1 FAIL (both IMGUI windows still receive clicks), G2 PASS (no regression), G3 PASS (flagged possibly vacuous — IMGUI text field may never have held `keyboardControl` focus).

**Why v1 failed**: `Event.Use()` only starves later OnGUI handlers if ours runs first in that dispatch; `[DefaultExecutionOrder]` across runtime-loaded mod assemblies proved unreliable.

**Mechanism v2 (order-independent)**: while shielded, the eater grabs IMGUI's own capture primitives on every OnGUI pass (incl. Layout/Repaint, which run every frame during hover — long before any click's MouseDown): `GUIUtility.hotControl = ShieldControlId` (mouse) and `GUIUtility.keyboardControl = ShieldControlId` (keyboard). IMGUI's per-control filter (`Event.GetTypeForControl`) then reports `Used` to every control except the hot one — the same mechanism IMGUI uses to block controls behind an active drag. Releases are idempotent and only-if-ours, so a foreign capture is never clobbered and a stuck grab cannot deaden IMGUI after capture ends. v1's `Use()` retained as belt-and-suspenders.

**Also in the rework**: the #001 diagnostic red tint removed from `PointerBlockerGateway` at user request (verbose-gated Debug logs retained).

**Auditor re-verification (v2)**: build 0/0; 59/59 tests (v2 changes no tested logic — tracker/interface untouched); public API empty diff; release trace inspected (every unshield path releases both captures iff ours). G1–G3 remain with the user; note G3 needs the stronger protocol this run (focus the IMGUI text field FIRST, confirm typing lands, then click into an ImGui text field and type — the IMGUI field must stop receiving input).

---

## Addendum 2 — white-viewport defect in v2 (2026-09-03, user run)

**User report**: mechanism v2 blocks clicks correctly, BUT the viewport turned pure white whenever the pointer entered an ImGui window (ImGui/IMGUI windows still visible on top). G1 not ratable, G2/G3 previously passed, G3 re-check deferred by user until fixed.

**Root cause (auditor)**: the v2 tint removal over-deleted — `PointerBlockerGateway.CreateBlocker` no longer assigned `image.color`, and a uGUI `Image` defaults to **opaque white**. The full-screen blocker therefore rendered as a white overlay exactly when active (mouse entering an ImGui window). ImGui (native, end-of-frame) and IMGUI (OnGUI Repaint) both draw after uGUI, so they stayed visible. Verbose logging would not have helped (the tint feature itself was the deleted code path); identified by code inspection.

**Fix**: one line — `image.color = new Color(1f, 1f, 1f, 0f)` restored at creation with a comment (raycast hits are alpha-independent). Build 0/0, 59/59 tests. Pending user re-run for G1–G3.
