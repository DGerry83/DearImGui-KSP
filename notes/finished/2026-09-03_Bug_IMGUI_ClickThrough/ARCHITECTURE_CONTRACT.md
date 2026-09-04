# Architecture Contract: ISSUES #003 — IMGUI (OnGUI) click bleed-through
## Date: 2026-09-03
## Type: Bugfix (reopened KNOWNLIMIT #003 at user direction)
## Related Investigation: [INVESTIGATION_LOG.md](INVESTIGATION_LOG.md)

### Change Specifics
- **Root Cause**: Unity IMGUI (`OnGUI`) reads mouse/keyboard events straight from the engine's event queue — outside EventSystem raycasts (so the #001 uGUI blocker can't help) and outside `InputLockManager` (so capture locks can't help). No KSP mod precedent blocks this (ClickThroughBlocker verified: cooperative-only for IMGUI; never calls `Event.current.Use()`).
- **Trigger**: any IMGUI window (other mods', demo benchmark window) positioned under a DearImGui-KSP window.
- **Data Loss Risk**: none.
- **Spec oversight**: mod-IMGUI input was never addressed (§5.3 covers InputLockManager only; §10.4 covers IMGUI rendering coexistence only). Amended in this session (see Files to Modify — docs).
- **Feature Scope**: while the library captures the mouse (hover/drag over an ImGui window) or the keyboard (active text field), IMGUI windows beneath receive nothing. When not capturing, IMGUI behavior is bit-identical to today.

### Fix Strategy (Candidate A — user approved 2026-09-03)
- **Approach**: an always-active, DontDestroyOnLoad MonoBehaviour forced to the front of script execution order (`[DefaultExecutionOrder(-32000)]`, named constant). Its `OnGUI` calls `Event.current.Use()` on mouse events (`e.isMouse || e.type == EventType.ScrollWheel`) while the library's mouse-capture flag is set, and on keyboard events (`e.isKey`) while the keyboard-capture flag is set. Later `OnGUI` handlers (all IMGUI mods) then see the event as `Used` and ignore it. **Layout/Repaint events are never touched** (eating them would break all IMGUI rendering). Flags are driven transition-only by `InputCaptureTracker` through a new Application-layer interface — the same plumbing as the #001 blocker.
- **Main technical risk**: `DefaultExecutionOrder` ordering of `OnGUI` across mods is documented-but-not-absolute. In practice no D16 mod forces early order. If in-game verification shows clicks still passing, the evidence will be identical to the pre-fix state and the fallback is an ordering investigation — no rollback harm. The eater is inert when not capturing, so worst case = today's behavior.
- **Accepted edge case** (documented): an IMGUI drag that *starts* outside an ImGui window and crosses it may keep its `GUIUtility.hotControl` capture mid-drag.
- **Rollback**: remove the two tracker calls (or set flags never) — behavior identical to pre-fix.
- **Similar Code Search**: capture-state consumers (only InputCaptureTracker — swept in the #001 session, still true).

### Structural Invariants
- Public API (`Application/DearImGuiKSP.cs`) unchanged — no new public members.
- Layer rules: Application gets an interface only; UnityEngine/Event API contact is Infrastructure-only; native untouched (no handshake change, stays v4).
- Failure UX / lifecycle: eater GameObject is created in `WireLifecycle` (only after successful init); suspend/fail paths release capture via `ReleaseAll`, which clears the eater flags — nothing eats while F2-hidden/loading/failed.
- D5/D16: inert-when-not-capturing is the safety property; IMGUI-mod regression gate is mandatory.
- Hot-path: tracker additions are transition-only bool compares (zero per-frame allocation); OnGUI eating is per-event bool checks.
- No assembly/version bump this session (behavior fix, API unchanged; version policy applied at release packaging per D17).

### Files to Modify
| File | Change Type | Invariants Applied | Risk | Lines (Est.) |
|------|-------------|--------------------|------|--------------|
| `DearImGuiKSP/Application/Interfaces/IImguiEventEaterGateway.cs` | Add | Unity-free interface; XML doc clarifies IMGUI = Unity OnGUI | Low | ~20 |
| `DearImGuiKSP/Infrastructure/ImguiEventEaterGateway.cs` | Add | MonoBehaviour + interface impl; `[DefaultExecutionOrder]`; DontDestroyOnLoad handled at creation site | Medium | ~70 |
| `DearImGuiKSP/Application/InputCaptureTracker.cs` | Modify | Third gateway; transition-only mouse AND keyboard shield calls; `ReleaseAll` clears both | Low | ~25 |
| `DearImGuiKSP/Infrastructure/Composition.cs` | Modify | Create eater GameObject in `WireLifecycle`; inject into tracker | Low | ~10 |
| `tests/Application.Tests/TestDoubles.cs` | Modify | Fake eater gateway (records call sequence) | Low | ~15 |
| `tests/Application.Tests/InputCaptureTrackerTests.cs` | Modify | New cases: keyboard-shield transitions, ReleaseAll clears both shields, mouse+keyboard independence | Low | ~60 |
| `DESIGN_SPEC.md` §5.3 / §10.4 / §13, `DECISION_LOG.md` (D23) | Modify (docs) | Spec oversight amendment | Low | ~15 |
| `README.md` (Known limitations) | Modify (docs) | Remove the IMGUI limitation on gate pass | Low | ~3 |
| `ISSUES` #003 file + trackers | Modify (docs) | Resolve on in-game pass | Low | — |

### Sub-Agent Scopes
- Single scope, single agent: eater component + tracker + wiring + tests. Docs (spec/D23/README/ISSUES) are applied by the parent on gate pass (they record a verified behavior change, not speculative text).
- No milestones file: change is small and single-layer; one in-game verification round.

---

## Addendum — Mechanism v2 after G1 FAIL (2026-09-03 user run)

**Result**: clicks still reached both IMGUI windows; G2 (no regression) PASS; G3 (keyboard) PASS but flagged possibly vacuous (if the IMGUI text field never held `GUIUtility.keyboardControl` focus, the eater was never exercised).

**Why v1 failed**: `Event.current.Use()` only starves later OnGUI handlers *if our OnGUI runs first in that dispatch* — and `[DefaultExecutionOrder]` on OnGUI across runtime-loaded mod assemblies proved insufficient/unreliable. No absolute ordering guarantee exists from our position.

**Mechanism v2 (order-independent)**: while shielded, the eater grabs IMGUI's own capture primitives during Layout/Repaint passes (which run every OnGUI cycle, frames before any click's MouseDown arrives):
- Mouse: `GUIUtility.hotControl = <our control id>` — IMGUI's per-control event filter (`Event.GetTypeForControl`) then reports `Used` to every control except the hot one. This is how IMGUI itself implements "dragging a slider blocks the buttons behind it"; it does not depend on OnGUI dispatch order.
- Keyboard: `GUIUtility.keyboardControl = <our id>` — steals IMGUI keyboard focus from any focused IMGUI text field.
- Release: when unshielded, reset each to 0 **iff currently ours** (every pass, idempotent) — the safety property (inert when not capturing) is preserved.
- Keep `Event.Use()` from v1 as belt-and-suspenders for the case where our OnGUI does run first.
- Our control id: a named constant unlikely to collide with mods' sequential `GetControlID` values.

**Risk delta**: grabbing hotControl while shielded starves ALL IMGUI controls — intended (shielded ⇔ pointer over an ImGui window / active ImGui text field). The release-on-unshield-every-pass guard prevents a stuck grab from permanently deadening IMGUI. D16 regression gate G2 remains mandatory.

**Files delta vs v1**: `ImguiEventEaterGateway.cs` rewritten (same class/interface/tracker wiring unchanged); the #001 diagnostic red tint in `PointerBlockerGateway.cs` is removed at user request (no longer needed); its now-unused `Func<bool> verboseLogging` ctor param goes with it (Composition updated). Tests unchanged in scope (tracker logic unchanged).
