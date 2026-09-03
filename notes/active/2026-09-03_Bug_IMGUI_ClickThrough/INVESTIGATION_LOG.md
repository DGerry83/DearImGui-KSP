# Investigation Log: ISSUES #003 — IMGUI (OnGUI) click bleed-through
## Date: 2026-09-03
## Status: Phase 0 complete — root cause confirmed, precedent mined; awaiting user approval of the fix approach
## Type: Bug (reopened KNOWNLIMIT #003 → user wants it fixed)

Session folder: `notes/active/2026-09-03_Bug_IMGUI_ClickThrough/`
Reopens: `ISSUES/KNOWNLIMIT-[DearImGuiKSP]_IMGUI-LOCAL-2026-09-03/` (#003).

### Symptom Profile
- **Primary Symptom**: clicks/drags over a DearImGui-KSP (ImGui) window pass through to Unity IMGUI (`OnGUI`) windows beneath — confirmed with the demo's IMGUI benchmark window and Cinematic Shaders' menu (user, 2026-09-03, G1 run 3).
- **Stock uGUI is already correctly blocked** (ISSUES #001 fix: uGUI raycast blocker + MAIN_MENU lock — verified same run).
- **Frequency**: always, when an IMGUI window sits under an ImGui window.

### Root Cause (confirmed)
- **Technical Cause**: IMGUI processes mouse input straight from Unity's event queue inside `OnGUI` — entirely outside EventSystem/GraphicRaycaster and outside `InputLockManager`. Neither the uGUI blocker (#001) nor control locks can reach it.
- **Location**: architectural gap; the library currently feeds nothing into the IMGUI event path.

### Spec oversight (user-flagged, confirmed)
The design spec never addresses mod-IMGUI *input*:
- §5.3 "Input locking" covers `InputLockManager` (stock/game input) only.
- §10.3 covers IMGUI *rendering* coexistence ("an ImGui overlay drawn after everything does not intersect the game's IMGUI frame") — input is unaddressed.
→ The spec must be amended as part of this fix (§5.3 input policy, §10.3, Revision History §13, new decision-log entry).

### Precedent research: ClickThroughBlocker (`C:\Users\Matt\source\repos\ClickThroughBlocker`, incl. upstream chambm Harmony PR #29)
- **Classic CTB is strictly cooperative** — mods replace `GUILayout.Window` with `ClickThruBlocker.GUILayoutWindow` and take `InputLockManager`/`EditorLogic` locks on hover (`ClickThroughBlocker.cs:105-126`, `FocusLock.cs:23-26`). Those locks gate *stock/game* input only. Useless against a non-cooperating IMGUI window.
- **The universal Harmony layer does NOT eat IMGUI events either** — it postfix-tracks all `GUI.Window`/`GUILayout.Window` rects (`HarmonyPatches.cs:136-149`) and suppresses uGUI raycasts + the PAW click coroutine + takes stock locks when hovered (`HarmonyPatches.cs:155-186`, `:48-72`). Zero use of `Event.current.Use()` or `GUIUtility.hotControl` anywhere in the codebase. **No KSP mod precedent blocks IMGUI-to-IMGUI clicks.**
- **The adaptable insight**: Unity dispatches each `Event.current` through every MonoBehaviour's `OnGUI` in script-execution order; a handler running *first* that calls `Event.current.Use()` starves all later handlers (the event's type becomes `Used`, inert to IMGUI controls). CTB's architecture leaves exactly this piece open. We don't even need CTB's rect tracking — the library already knows capture state (`InputCaptureState.MouseCaptured/KeyboardCaptured`).

### Fix Candidates
- **Candidate A — early-ordered OnGUI event eater** (recommended): a library MonoBehaviour with `[DefaultExecutionOrder(<very negative>)]`, created with the existing pointer-blocker lifecycle, whose `OnGUI` calls `Event.current.Use()` for mouse events (MouseDown/Up/Drag/ScrollWheel/MouseMove) while `MouseCaptured`, and keyboard events while `KeyboardCaptured`. Never touches Layout/Repaint events (that would break all IMGUI rendering).
  - Pros: unilateral — no cooperation needed from IMGUI mods; small (~60 lines, Infrastructure-only); reuses the existing capture-state plumbing; gated identically to the #001 blocker (only while hovering/capturing).
  - Cons: script-execution-order guarantees our OnGUI runs first *in practice* (nothing stops another mod from also forcing early order — acceptable); an IMGUI drag that *started* outside our window and crosses it keeps its `GUIUtility.hotControl` and may continue (documented edge); must be verified against the D16 IMGUI mods.
- **Candidate B — CTB-style cooperative wrappers**: offer consumers... — irrelevant; our consumers don't use IMGUI at all, and the offending windows belong to *other* mods. Rejected.
- **Candidate C — remain documented limitation**: rejected by the user (this session exists to fix it).

### Data Loss / Risk Assessment
- Corruption risk: none. Change risk: **Medium** — touches the shared IMGUI event stream; D16 IMGUI-mod regression checks are mandatory gates.
- **Crucial safety property**: when not capturing, the eater does nothing (zero behavior change for IMGUI mods in the normal case).

### Next step
User approval of Candidate A + the spec amendment → Phase 1 (contract, gates incl. D16 IMGUI regression checks).
