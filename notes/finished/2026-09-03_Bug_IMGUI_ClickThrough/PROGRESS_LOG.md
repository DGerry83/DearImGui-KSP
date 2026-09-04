# Progress Log: ISSUES #003 — IMGUI (OnGUI) click bleed-through
## Session: notes/active/2026-09-03_Bug_IMGUI_ClickThrough/

- 2026-09-03 — Phase 1 complete: PLANNING_WORKSHEET.md, ARCHITECTURE_CONTRACT.md (Candidate A approved), GATES.md frozen (G1–G5, incl. D16 IMGUI-mod regression gate G2).

## Implementation (2026-09-03)

Implementation complete per ARCHITECTURE_CONTRACT.md. Raw build/test output below (verbatim).

### Files changed

- **Add** `DearImGuiKSP/Application/Interfaces/IImguiEventEaterGateway.cs` — internal interface, house XML-doc style, no Unity types. `SetMouseShielded(bool)` / `SetKeyboardShielded(bool)`; doc states "IMGUI" = Unity legacy immediate-mode GUI (OnGUI), not Dear ImGui.
- **Add** `DearImGuiKSP/Infrastructure/ImguiEventEaterGateway.cs` — internal sealed MonoBehaviour implementing the interface. `[DefaultExecutionOrder(ExecutionOrder)]` with `private const int ExecutionOrder = -32000` (comment: forces our OnGUI before other mods' so `Event.current.Use()` starves their IMGUI windows; documented-but-not-absolute ordering, D16-verified). Two private bool fields set by the interface methods. `OnGUI`: `Event e = Event.current;` — early-returns on Layout/Repaint/Used; if mouse-shielded and (`e.isMouse` || `e.type == EventType.ScrollWheel`) → `e.Use()`; else if keyboard-shielded and `e.isKey` → `e.Use()`. Class XML doc covers mechanism, ISSUES #003 rationale, inert-when-not-shielded safety property, and the accepted hotControl mid-drag edge case.
- **Modify** `DearImGuiKSP/Application/InputCaptureTracker.cs` — third ctor param `IImguiEventEaterGateway`; new `_mouseShielded`/`_keyboardShielded` fields; `Update(...)` calls `SetMouseShielded(state.MouseCaptured)` / `SetKeyboardShielded(state.KeyboardCaptured)` transition-only (bool compares, zero per-frame allocation); `ReleaseAll()` forces both false (transition-guarded). Existing `_blocked`/uGUI-blocker path untouched — eater mouse flag mirrors it but is a separate call (blocker = uGUI raycasts, eater = IMGUI events).
- **Modify** `DearImGuiKSP/Infrastructure/Composition.cs` — new lazy `ImguiEventEater` singleton property (`GameObject "DearImGuiKSP.ImguiEventEater"`, DontDestroyOnLoad, AddComponent); injected into `new InputCaptureTracker(LockGateway, PointerBlocker, ImguiEventEater, Registry)`; `WireLifecycle()` forces creation (`_ = ImguiEventEater;`) so the eater exists before frames run and regardless of first capture. Eater creation lives in WireLifecycle → failed-init path (DearImGuiKSPAddon.Start else-branch) creates no eater.
- **Modify** `tests/Application.Tests/TestDoubles.cs` — `FakeImguiEventEaterGateway` recording `MouseCalls`/`KeyboardCalls` sequences.
- **Modify** `tests/Application.Tests/InputCaptureTrackerTests.cs` — ctor calls fixed (all 8 existing tests + helper); 6 new cases: mouse-shield transition-only; keyboard-shield engages on KeyboardCaptured alone without mouse; mouse/keyboard independence across interleaved transitions; ReleaseAll forces both shields false; ReleaseAll when not shielded touches nothing; re-engage after ReleaseAll.
- **Modify** `tests/Application.Tests/FrameLoopOrchestratorTests.cs` — mechanical ctor-call fix only (extra `FakeImguiEventEaterGateway()` arg; call site not listed in the contract's Files table — flagged to parent, required by the signature change).

### Raw build output (final run)

```
  DearImGuiKSP -> C:\Users\Matt\source\repos\DearImGui-KSP\DearImGuiKSP\bin\Debug\net48\DearImGuiKSP.dll
  DearImGuiKSPDemo -> C:\Users\Matt\source\repos\DearImGui-KSP\DearImGuiKSPDemo\bin\Debug\net48\DearImGuiKSPDemo.dll
  Application.Tests -> C:\Users\Matt\source\repos\DearImGui-KSP\tests\Application.Tests\bin\Debug\net48\Application.Tests.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:02.20
```

### Raw test output

```
Test run for C:\Users\Matt\source\repos\DearImGui-KSP\tests\Application.Tests\bin\Debug\net48\Application.Tests.dll (.NETFramework,Version=v4.8)
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:    59, Skipped:     0, Total:    59, Duration 140 ms - Application.Tests.dll (net48)
```

(Pre-fix suite: 53 tests; +6 new InputCaptureTracker cases.)

### git diff --stat

```
 DearImGuiKSP/Application/InputCaptureTracker.cs    |  32 +++++-
 DearImGuiKSP/Infrastructure/Composition.cs         |  26 ++++-
 tests/Application.Tests/FrameLoopOrchestratorTests.cs |   3 +-
 tests/Application.Tests/InputCaptureTrackerTests.cs  | 122 ++++++++++++++++++--
 tests/Application.Tests/TestDoubles.cs             |  10 ++
```

Plus untracked new files: `DearImGuiKSP/Application/Interfaces/IImguiEventEaterGateway.cs`, `DearImGuiKSP/Infrastructure/ImguiEventEaterGateway.cs`.

### "Do not fix what is not broken" — untouched-path evidence

`git diff --stat` contains NO changes to: `DearImGuiKSP/Infrastructure/PointerBlockerGateway.cs` (uGUI blocker), `DearImGuiKSP/Infrastructure/InputLockGateway.cs` (lock-mask computation), `DearImGuiKSP/Application/FrameLoopOrchestrator.cs` (clamp logic), `DearImGuiKSP/Application/DearImGuiKSP.cs` (public surface), `DearImGuiKSP/LibraryConfig.cs`, native sources. Handshake stays v4; no assembly/version bump.

### Trace by inspection (G4-adjacent)

- F2-hide / loading: `Composition.WireLifecycle` handlers `UiVisibilityChanged`/`LoadingChanged` → `CaptureTracker.ReleaseAll()` → both shields forced false (transition-guarded) → eater eats nothing while hidden/loading.
- Failed state: eater created only via `ImguiEventEater` (forced in `WireLifecycle`); `WireLifecycle` runs only on `BridgeInitResult == NativeBridge.InitOk` (DearImGuiKSPAddon.Start:38) → failed init creates no eater.
- Hot-path (per 08-native-interop.md): Check 1 process-global state — none (in-process MonoBehaviour flags only). Check 2 allocation — Update() additions are two bool compares + two interface calls only on transitions; OnGUI eating is per-event bool checks, no allocation. Check 3 resource symmetry — eater GameObject is session-lived (DontDestroyOnLoad), created only post-init-success; no partial-failure path creates it.

### Phase 0 disagreement list

No blocking disagreements; contract matches files. Non-blocking notes:
1. `tests/Application.Tests/FrameLoopOrchestratorTests.cs:43` is a third `InputCaptureTracker` ctor call site not listed in the contract's Files to Modify table; it required the same mechanical fix (done).
2. Contract row for `Composition.cs` says "Create eater GameObject in WireLifecycle; inject into tracker" — implemented as a lazy `ImguiEventEater` property (composition-root house style per the same contract sentence) forced in `WireLifecycle`, satisfying both "eager before frames" and "inject into tracker".
3. Contract's doc rows (DESIGN_SPEC/D23/README/ISSUES) are excluded from this scope per instructions (parent applies on G5 gate pass).

STATUS: COMPLETE

---

## Mechanism v2 rework (2026-09-03)

G1 FAIL evidence: user in-game run — clicks still reached both IMGUI windows under the ImGui window. v1 (`Event.current.Use()` from an early-ordered OnGUI) starves later handlers only if our OnGUI runs first in that dispatch; `[DefaultExecutionOrder]` across runtime-loaded mod assemblies proved unreliable. Rework per contract "## Addendum — Mechanism v2 after G1 FAIL".

### Phase 0 disagreement list (v2 addendum vs files)

No blocking disagreements found — contract matches files:
- `ImguiEventEaterGateway.cs` — class/interface/`[DefaultExecutionOrder(-32000)]` all as the addendum requires ("same class/interface/tracker wiring unchanged"); only the OnGUI body rewritten.
- `PointerBlockerGateway.cs:39` — ctor was exactly `(ILogger log, System.Func<bool> verboseLogging)`; tint at `:29-30`/`:86`; doc tint sentence at `:20-21`. Addendum's removal spec matches.
- `Composition.cs:70` — constructed with `(Logger, () => Settings.VerboseLogging)`; addendum's "drop the lambda argument" matches.
- Non-blocking observation: the v2 grab must be placed before the Layout/Repaint early return so it runs on every pass — an implementation detail of the rewrite, not a file/contract conflict.

### Changes

1. **`DearImGuiKSP/Infrastructure/ImguiEventEaterGateway.cs`** — OnGUI body rewritten. On EVERY pass (before the Layout/Repaint early return): `if (_mouseShielded) GUIUtility.hotControl = ShieldControlId; else if (GUIUtility.hotControl == ShieldControlId) GUIUtility.hotControl = 0;` and the same pair for `_keyboardShielded`/`GUIUtility.keyboardControl`. Then the v1 eating retained: mouse events (`e.isMouse || ScrollWheel`) while mouse-shielded, keyboard events (`e.isKey`) while keyboard-shielded → `e.Use()`; Layout/Repaint/Used never Use()d. New named const `ShieldControlId = int.MaxValue - 1024` — `GUIUtility.GetControlID` ids are sequential per event starting near zero, so even control-heavy mods stay orders of magnitude below it; 0 means "none" so it cannot be ours. Class XML doc rewritten for v2: GetTypeForControl per-control filter mechanism (how IMGUI itself blocks controls behind a drag; order-independent because the grab lands in Layout passes), why Use()-alone failed (dispatch ordering across runtime-loaded assemblies unreliable), release-guard safety property (reset iff ours, every pass, idempotent), retained hotControl mid-drag edge case (a control already hot when the shield goes up is clobbered; drag cancels mid-way).
2. **`DearImGuiKSP/Infrastructure/PointerBlockerGateway.cs`** — removed `VerboseTint`/`InvisibleTint` statics, `_verboseLogging` field, ctor param, the per-call tint assignment, and the now-unused `_image` field + its `GetComponent<Image>()` (the Image component itself stays in `CreateBlocker` — it is the raycast target). `ILogger` param and both `Debug` logs kept. Class doc tint sentence replaced with "Debug-logged (verbose-gated)".
3. **`DearImGuiKSP/Infrastructure/Composition.cs`** — `PointerBlocker` now `new PointerBlockerGateway(Logger)`; summary comment updated ("G1 rework: logger"). Grep sweep: no remaining `VerboseTint`/`InvisibleTint`/`_verboseLogging` in `DearImGuiKSP/` (the `SettingsModel._verboseLogging` hits are the unrelated settings field — untouched); `Composition.cs:70` is the only construction site; tests use `FakePointerBlockerGateway`.

### Raw outputs

Baseline (pre-change) build:
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```
Baseline (pre-change) test:
```
Passed!  - Failed:     0, Passed:    59, Skipped:     0, Total:    59, Duration: 217 ms - Application.Tests.dll (net48)
```
Post-change build:
```
Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:03.28
```
Post-change test (raw summary line):
```
Passed!  - Failed:     0, Passed:    59, Skipped:     0, Total:    59, Duration: 132 ms - Application.Tests.dll (net48)
```

### "Do not fix what is not broken" — untouched-path evidence

v2 wrote exactly three source files (file mtimes, run window 19:47:00–19:47:54 EDT 2026-09-03, `date` at 19:48:25):
- `DearImGuiKSP/Application/InputCaptureTracker.cs` — mtime 19:27:53 (v1 session, predates v2 run) — UNTOUCHED
- `DearImGuiKSP/Application/Interfaces/IImguiEventEaterGateway.cs` — mtime 19:27:21 — UNTOUCHED
- `tests/Application.Tests/InputCaptureTrackerTests.cs` (19:29:16), `TestDoubles.cs`, `FrameLoopOrchestratorTests.cs` — UNTOUCHED
- `DearImGuiKSP/Application/DearImGuiKSP.cs` (public API) — mtime 14:58:48 — UNTOUCHED
- `DearImGuiKSP/Infrastructure/InputLockGateway.cs` (lock mask), `FrameLoopOrchestrator.cs` (clamp) — UNTOUCHED
- `DearImGuiKSPNative/src/*` — mtimes 18:47–18:48 — UNTOUCHED; handshake stays v4
- GATES.md — NOT edited (frozen)

### Trace by inspection (shielded→unshielded release)

Every release path funnels through `InputCaptureTracker` transition calls (unchanged): the interface setters flip the eater's flags, and the release happens lazily on the eater's own next OnGUI pass — with the flag false, `else if (GUIUtility.hotControl == ShieldControlId) GUIUtility.hotControl = 0;` fires iff the grab is still ours (idempotent; a foreign hotControl is never clobbered). Same for `keyboardControl`. `ReleaseAll` (F2 hide via `UiVisibilityChanged`, loading via `LoadingChanged`, both in `Composition.WireLifecycle`, unchanged) clears both shields → both grabs released on the next pass. Failed-init path: `WireLifecycle` runs only on successful bridge init, and the eater exists only via `ImguiEventEater` forced there → no eater on failure. Hot-path: the OnGUI additions are bool compares + int property sets against a const; zero per-frame allocation.

STATUS: COMPLETE

---

## Session closure (2026-09-03)

User in-game verification, final run: **G1 PASS, G2 PASS, G3 PASS**. G4 (regression trace by inspection) and G5 (docs applied) recorded earlier in GATES.md. All gates PASS — session verdict **CONTINUE**.

Closure actions:

- Spec amended: DESIGN_SPEC.md §5.3 (input-locking bullet now covers uGUI blocker + IMGUI suppression, both inert when not capturing; hover mask corrected to `CAMERACONTROLS|GUI|MAIN_MENU`), §10.4 (IMGUI coexistence behavior while capturing + retained mid-drag edge case), §13 revision row added.
- DECISION_LOG.md: D23 appended (GUIUtility capture grab; v1 Event.Use/ordering failure recorded; limitation-acceptance alternative rejected by user). D22 row annotated as superseded by D23.
- README.md: "Known limitations" section removed (its only content was #003); status line updated.
- AGENTS.md: status line updated (ISSUES #001–#003 resolved; 59 tests); DECISION_LOG range updated to D1–D23.
- ISSUES #003: status Resolved, investigation log + resolution written; row moved TRACKER.md → ARCHIVED_TRACKER.md (Next ID stays #004; active table empty).
- Debug tint removed from `PointerBlockerGateway` in the v2 rework (user-requested once no longer needed); white-viewport defect (alpha-0 color line dropped during tint removal) fixed and verified same cycle.

Session moving to `notes/finished/`.

STATUS: CLOSED (CONTINUE)
