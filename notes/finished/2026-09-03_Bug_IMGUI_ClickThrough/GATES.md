# Frozen Acceptance Gates: ISSUES #003 — IMGUI (OnGUI) click bleed-through
## Frozen At: 2026-09-03 (frozen before any implementation)
## Source: [ARCHITECTURE_CONTRACT.md](ARCHITECTURE_CONTRACT.md)

| Gate ID | Criterion | Owner | Verdict | Evidence |
|---------|-----------|-------|---------|----------|
| G1 | With the demo ImGui window over the demo's IMGUI benchmark window: clicks, drags, and scroll over the ImGui window no longer reach the IMGUI window (its buttons inert, it does not drag/reposition); the ImGui window's own widgets work normally. Repeat with the ImGui window over Cinematic Shaders' menu: same result. | User (in-game) | **PASS** (2026-09-03, mechanism v2 + white-viewport fix) | user report |
| G2 | D16 regression: IMGUI menus (demo benchmark window, Cinematic Shaders) behave completely normally whenever NOT covered by an ImGui window — open, click, drag, type. Stock uGUI blocking from #001 still works (toolbar, save-load menu). Stock PopupDialogs unaffected. F2 hide + scene transitions leave nothing stuck (locks/shields released). | User (in-game) | **PASS** (2026-09-03) | user report |
| G3 | Keyboard: with an ImGui text field focused and the pointer moved off the window (keyboard captured, mouse not), typing does not reach an IMGUI text field behind; with nothing capturing, IMGUI text fields receive typing normally. | User (in-game) | **PASS** (2026-09-03, strong protocol: IMGUI field focused first, then ImGui field steals it) | user report |
| G4 | Mechanical: `dotnet build DearImGui-KSP.slnx` 0 warnings/0 errors; `dotnet test` all green incl. the updated InputCaptureTracker suite; native build untouched and still green; `Application/DearImGuiKSP.cs` public surface unchanged; no per-frame allocation added (transition-only calls). | Auditor | PASS | build 0/0; `Passed: 59, Failed: 0` (auditor re-run 2026-09-03); public API empty diff |
| G5 | On G1–G3 pass: spec amendment applied (§5.3 input policy incl. IMGUI event suppression, §10.4 note, §13 revision row, DECISION_LOG D23), README known-limitation updated, ISSUES #003 resolved and trackers synced. | Parent agent | PASS | doc diffs 2026-09-03 |

## Session Verdict
- **Verdict**: CONTINUE
- **Reason**: All gates PASS. v1 (Event.Use + forced script order) failed G1 in-game; mechanism v2 (GUIUtility hotControl/keyboardControl grab, order-independent) passed G1–G3; one self-inflicted white-viewport defect (blocker Image lost its transparent color during tint removal) found and fixed in the same cycle. G4 mechanical PASS; G5 applied.
