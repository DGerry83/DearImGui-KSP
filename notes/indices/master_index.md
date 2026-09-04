# Master Index — DearImGui-KSP workflow sessions

Created 2026-09-03 (first indexed session). Newest first.

| Session | Type | Status | Summary |
|---------|------|--------|---------|
| `notes/finished/2026-09-03_DesignSpec_Theming_Extensions_Showcase/` | DesignSpec | Complete (spec confirmed) | Pre-release feature wave spec: KSP default theme (gradients via ShadeVerts, rounding, IBM Plex Sans bundled), extension integrations (ImPlot v1.0/cimplot, knobs, wheels vendored, cimspinner, imgui_toggle; C# tween engine; implot3d + docking deferred), telemetry showcase in demo mod, docs/ set, release gating (D24–D34). §6.1 visual values subject to in-game tuning pass. Next: bootstrap-route implementation plan. |
| `notes/finished/2026-09-03_Bug_IMGUI_ClickThrough/` | Bug | Complete (CONTINUE, all gates PASS) | ISSUES #003 IMGUI click bleed-through fixed via GUIUtility `hotControl`/`keyboardControl` capture grab (`ImguiEventEaterGateway`, D23); v1 Event.Use approach failed in-game; debug tint removed, white-viewport defect fixed same cycle; spec amended (§5.3, §10.4, §13); test suite at 59. |
| `notes/finished/2026-09-03_Bug_ClickThrough_WindowAnchor/` | Bug + Feature | Complete (CONTINUE, all gates PASS) | Post-MVP backlog items 1–3: ISSUES #001 uGUI click bleed-through fixed (invisible raycast blocker + MAIN_MENU lock); ISSUES #002 resolution-change window clamp fixed (opt-out `clampWindowsToViewport`, handshake v4); xUnit Application test suite (53 tests). IMGUI click-through residual filed as KNOWNLIMIT #003. |
| `notes/finished/2026-07-29_DearImGuiKSP_PlanImplementation/` | PlanExecution | Complete | M1–M6 implementation, all gates PASS (FINAL_AUDIT.md). |
| `notes/finished/2026-07-29_NewProject_DearImGuiKSP/` | NewProject | Complete | Project bootstrap: planning worksheet, implementation plan, skeleton, gates. |
| `notes/finished/2026-07-29_DesignSpec_DearImGuiKSP_UI_Library/` | DesignSpec | Complete | Confirmed design spec + decision log (D1–D22), question log (Q1–Q47), research notes. |
