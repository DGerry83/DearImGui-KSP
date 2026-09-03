# Investigation Log: ISSUES #001 (click bleed-through), #002 (window anchor through resolution change), + unit-test backlog item (feature mode)

## Date: 2026-09-03
## Status: Phase 0 complete — user decisions received, spike done; proceeding to Phase 1
## Type: Bug (#001, #002) + Feature (unit tests; abbreviated scope/risk check)

Session folder: `notes/active/2026-09-03_Bug_ClickThrough_WindowAnchor/`
Backlog source: `notes/plans/2026-09-03_PostImplementation_Backlog_HANDOFF.md` (items 1–3 of the ordered backlog; styling and OpenGL deliberately excluded for now — styling details arrive from the user later in-session).

Root causes for both bugs are already confirmed in their ISSUES write-ups (Phase 0 detective work was done during C9b/C12 verification), so hypothesis testing is abbreviated per Router guidance; the remaining Phase 0 work is candidate enumeration and the design tensions each candidate raises.

---

## Item 1 — ISSUES #001 (P2): mouse clicks bleed through ImGui windows to KSP uGUI below

### Symptom Profile
- **Primary Symptom**: clicking a widget in a DearImGui-KSP window also activates the stock uGUI element behind it (e.g. KSC/menu buttons).
- **Reproduction**: demo window positioned over a stock UI element; click a demo widget → both receive the click. Verified at main menu, KSC, and flight (2026-08-31, C9b).
- **Conditions**: any ImGui window overlapping a uGUI canvas element. Camera/keyboard locks (C9, AC7) work correctly — the gap is uGUI pointer events only.
- **Frequency**: always, when overlap exists.

### Root Cause (confirmed)
- **Technical Cause**: ImGui windows are drawn natively and are not uGUI objects. Unity's `EventSystem`/`GraphicRaycaster` never sees them, so a click over an ImGui window still raycasts to the uGUI canvas below. KSP's `InputLockManager` `ControlTypes.GUI` lock (which the library correctly applies) gates *game* input, not uGUI canvas pointer dispatch.
- **Location**: architectural gap, not a code defect — input layer boundary between native-rendered ImGui and Unity uGUI. See `DearImGuiKSP/Application/InputCaptureTracker.cs`, `DearImGuiKSP/Infrastructure/InputLockGateway.cs`.
- **First Appearance**: present since first in-game build; found at C9b verification.
- **Corruption Risk**: none. Change risk: Medium (touches shared uGUI event space other mods also live in).

### Fix Candidates
- **Candidate A — invisible uGUI raycast-blocker**: a transparent full-screen `Graphic` (raycastTarget=true, alpha=0) on a library-owned Canvas sorted above KSP's UI canvases, enabled only while ImGui wants the mouse (`io.WantCaptureMouse`). Clicks over ImGui windows hit the blocker instead of stock UI.
  - Pros: uses uGUI's own dispatch rules; no EventSystem patching; scoped to "pointer is over an ImGui window" moments; reversible (disable the blocker).
  - Cons: needs a spike to confirm KSP's canvases all dispatch via `GraphicRaycaster` (UIMasterController's nine canvases) and that a higher-sort canvas reliably wins; full-screen blocker while hovering could swallow clicks that fall *outside* the actual window rect if enabled on WantCaptureMouse alone — may need window-rect-synced blockers instead; drag-from-window-outside edge case; must not interfere with other mods' uGUI overlays (Deferred/TUFX/etc.).
- **Candidate B — per-frame EventSystem suppression**: disable/gate `EventSystem.current` (or its input module) while `WantCaptureMouse`.
  - Pros: simple conceptually.
  - Cons: global hammer — suppresses *all* uGUI interaction including other mods' and stock UI the user may legitimately click while an ImGui window is merely hovered; risk of breaking mod coexistence (hard D5/D16 constraint); likely over-broad.
- **Candidate C — documented limitation**: accept and document.
  - Pros: zero risk. Cons: leaves a P2 UX defect in the flagship demo scenario; user has signaled they want it fixed (it's backlog item #1).

### Research spike required (before Phase 1 candidate selection)
1. Confirm every KSP uGUI canvas the user can click routes through `GraphicRaycaster` (Knowledge Library: UIMasterController has nine canvases + uiCamera — check for raycasters and canvas sort orders in the ILSpy dump).
2. Decide blocker granularity: full-screen-while-hovered vs per-window-rect blockers (needs per-frame window rects from ImGui — available via cimgui? verify).
3. Check coexistence: does an always-on-top transparent canvas break Deferred, TUFX, Scatterer, Parallax UI, or stock modal dialogs (which use their own canvases/locks)?
4. Non-uGUI click consumers (e.g. clicking parts/vessels in flight) — confirm these are already covered by existing `CAMERACONTROLS`/GUI locks, so the blocker only needs to fix uGUI.

### Spike results (2026-09-03, against the KSP Knowledge Library ILSpy dump; full note recorded at `KSP Knowledge Library/NOTES/ugui-click-blocking-and-canvas-sorting.md`)

1. **All stock clickable UI routes through `GraphicRaycaster`** (custom `KSPGraphicRaycaster`, `KSPGraphicRaycaster.cs:9-11`). No `PhysicsRaycaster` anywhere in the dump. Scroll and drag ride the same raycast results as clicks.
2. **Stock's own suppression pattern is input-lock-driven, not a blocker panel** — but it validates ours: `KSPGraphicRaycaster.Raycast` returns nothing when its lock mask intersects (`:74-118`); modals take `UI_DIALOGS` (`UIMasterController.cs:1729-1741`); `CanvasGroupInputLock.cs` drives `blocksRaycasts` from locks. Canvas precedent for going on top: TMPro dropdown uses `sortingOrder = 30000` (`TMP_Dropdown.cs:727`); Screen Space - Overlay or `overrideSorting`+high sortOrder both reliably win.
3. **Bonus coverage**: 44 call sites of `EventSystem.IsPointerOverGameObject()` gate world picking (part clicks `Part.cs:19450`, part-action windows `UIPartActionController.cs:1071`, flight camera wheel `FlightCamera.cs:1347-1358`, KSC buildings, editor picking, map). A blocker under the cursor makes this return true → the blocker also fixes click-through to *world* objects, not just uGUI.
4. **One real gap: main-menu 3D buttons** (`TextProButton3D`, collider + legacy `OnMouseX` physics messages, `TextProButton3D.cs:7-8`) bypass EventSystem entirely. Stock disables them via the `ControlTypes.MAIN_MENU` lock (`MainMenu.cs:1850-1893`). Fix must add `MAIN_MENU` to the mouse-capture lock mask.
5. **Granularity decision: full-screen-while-hovered is sufficient.** `io.WantCaptureMouse` stays true during active drags out of a window, so the blocker persists through drag-outs; no per-window-rect sync needed. No native changes required (capture state already computed managed-side).
6. Coexistence: blocker active during a stock modal is redundant-but-harmless (stock raycasters already inert via UI_DIALOGS). No rendering impact (alpha-0 graphic).

### User decisions (2026-09-03)
- **#001**: Candidate A approved (spike above now confirms feasibility; add `MAIN_MENU` lock for the 3D-button gap).
- **#002**: Candidate B — library clamp governed by a player-facing setting in `settings.cfg`, **default true (opt-out)**; §6.2 consumer-ownership rule stands unchanged for the API.
- **Unit tests**: xUnit; Application layer only this session; `SettingsStore`/Infrastructure test deferral must be documented.

---

## Item 2 — ISSUES #002 (P3): window anchored top-left through resolution change ends nearly off-screen

### Symptom Profile
- **Primary Symptom**: after lowering resolution, an ImGui window positioned near the old screen edge sits mostly outside the new viewport; only the top-left corner remains reachable (window is recoverable, hence P3).
- **Reproduction**: demo window near screen edge → Settings → lower resolution → window mostly off-screen. Verified 2026-08-31 (AC10 otherwise PASS).
- **Frequency**: always on resolution reduction with near-edge windows.

### Root Cause (confirmed)
- **Technical Cause**: ImGui window positions are pixel-absolute and owned by consumers; `DisplaySize` changes underneath them. Not a defect in the viewport path.
- **Design tension (must resolve before any implementation)**: spec §6.2 — "Consumers own window placement entirely; the library imposes no layout, anchoring, or sizing rules. Window positions are consumer state." A library-side clamp touches consumer-owned state.

### Fix Candidates
- **Candidate A — library-side clamp on resolution change**: on `ResolutionChanged`, clamp all window positions into the new viewport (e.g. via ImGui window list in the native core or per-frame managed clamp).
  - Pros: fixes it for every consumer at once; matches user expectation.
  - Cons: violates §6.2 as written — spec change required (user decision); needs window enumeration (native surface or cimgui — check handshake implications).
- **Candidate B — opt-in global assist**: same clamp, but governed by a library-owned setting in `settings.cfg` (library's own global config, spec §4.4), default on or off.
  - Pros: frames the clamp as *library policy the player controls* rather than the library editing consumer state silently; keeps §6.2's "consumers own placement" for API purposes.
  - Cons: still technically moves consumer windows; needs the same enumeration mechanism as A.
- **Candidate C — consumer helper**: offer an additive helper (e.g. `ClampWindowToViewport` or a position-persistence helper with clamping, already deferred in spec §3.4/§4.4); consumers opt in.
  - Pros: fully consistent with §6.2; additive-only API.
  - Cons: doesn't fix the demo out of the box unless the demo adopts it; every consumer must opt in.
- **Candidate D — document as expected immediate-mode behavior**: zero code. Cons: P3 UX wart remains.

---

## Item 3 — Unit tests (Feature; abbreviated scope/risk check)

### Scope
- External review finding 5 (2026-09-03), originally deferred by plan decision; user now wants tests before the public API grows further.
- `tests/` tree exists as placeholders: `Application.Tests`, `Core.Tests`, `Infrastructure.Tests` (READMEs state intent; framework choice explicitly deferred — "plain xUnit/NUnit works").
- **Primary targets** (Unity-free by design, pure C#): `LifecycleStateMachine`, `ConsumerRegistry`, `FaultBarrier`, `SettingsModel`, `InputCaptureTracker`, and `FrameLoopOrchestrator` seams in `DearImGuiKSP/Application/`.
- **Secondary target**: `SettingsStore` ConfigNode round-trip (`tests/Infrastructure.Tests`) — needs KSP assembly references (Assembly-CSharp); the C11 wrapper-node save/load asymmetry defect is the exact regression class to catch (round-trip test).
- **Core.Tests**: native layer already has a smoke harness (`build_harness.bat` → `harness.exe`); decide whether Core.Tests stays a placeholder or wraps the harness.
- All tested types are `internal` — needs `InternalsVisibleTo` (or equivalent) from `DearImGuiKSP.csproj`.
- Success criteria: `dotnet test` runs the Application suite (and Infrastructure SettingsStore round-trip if the KSP reference is practical) locally, catching state-machine/registry/fault-barrier/settings regressions without the game.

### Risks / decisions to make
- Framework choice (xUnit vs NUnit) — was deferred, now must be picked.
- `InternalsVisibleTo` adds a (build-time-only) visibility exception — acceptable, but record it in the contract.
- Infrastructure tests referencing KSP's Assembly-CSharp tie the test project to the pinned install path (`DearImGui-KSP.props.user` / `KSPBT_GameRoot`) — decide whether that's in scope now or deferred like Core.Tests.
- Keep layer purity: tests must not push Application toward Unity references.

---

## Data Loss / Risk Assessment (session-level)
- Corruption risk: none for all three items.
- Change risk: #001 Medium (shared uGUI event space); #002 Low–Medium (depends on candidate; clamp mechanism touches the frame loop); unit tests Low (additive, build-time only).

## Next step
User review of candidates and the open decisions:
1. #001: approve the research spike (Candidate A vs B vs C decided after spike, or pre-select).
2. #002: resolve the §6.2 tension — pick A/B/C/D (this is a spec-level call only the user can make).
3. Unit tests: framework pick (xUnit/NUnit), and whether Infrastructure `SettingsStore` tests are in scope this session.

**Phase 0 complete pending user sign-off on the above. Waiting for approval to proceed to Phase 1.**
