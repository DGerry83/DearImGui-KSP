# Impediments: Window Docking (ISSUES #011)

## IMP-001 (Scope C, MATERIAL — work stopped pending ruling)

**Conflict:** The Scope C task instruction "declare a viewport dockspace" and
"the showcase must degrade gracefully when docking is disabled (setting off):
the three windows simply render floating as before — no broken layout, no
errors" cannot both be satisfied with the landed Scope B public API.

**Evidence (vendored imgui 1.92.9, `cimgui/imgui/imgui.cpp` in the sibling
cimgui clone):**

1. `ImGui::DockSpaceOverViewport` (imgui.cpp:20893-20921) unconditionally
   submits a fullscreen host window (`WindowOverViewport_*`,
   `NoTitleBar|NoResize|NoMove|NoDocking|...`, themed `WindowBg`, mouse input
   enabled) around its `DockSpace` call. Only `ImGui::DockSpace` early-outs
   when `ImGuiConfigFlags_DockingEnable` is off (imgui.cpp:20786-20789,
   `return 0` before any submission). Therefore a per-frame
   `DockSpaceOverViewport` in the demo, with the library `docking` setting
   OFF, paints a fullscreen theme-background overlay over the game and eats
   all mouse input — the exact opposite of "windows simply render floating
   as before". No flag combination avoids this (`PassthruCentralNode` only
   drops the background, not input capture; `KeepAliveOnly` hides docked
   windows).
2. When docking is OFF, `DockSpace` never creates the dock node, so a one-time
   `DockBuilderSplitNode` on that ID hits `IM_ASSERT(node != NULL)`
   (imgui.cpp:21176-21179) → recoverable error → diagnostics buffer → KSP.log —
   violating "no errors".
3. The demo has **no public way to read the `docking` setting** and gate
   itself: `LibrarySettings.Docking` (Application/LibrarySettings.cs:17) and
   `LibraryConfig.DefaultDocking` (LibraryConfig.cs:48) are `internal`;
   SettingsModel is internal; the public facade exposes only `CurrentTheme`
   and `IsAvailable` (DearImGuiKSP.cs:74,82). Scope C is forbidden from
   modifying `DearImGuiKSP\` beyond the single optional theme item, and
   Scope B is closed.

**Note:** the task's parenthetical "(DockBuilder calls are gated no-ops
outside frame callbacks by design...)" is true only of the facade's
`CanDeclareUi` gate — it does NOT hold for the docking-disabled case.
ARCHITECTURE_CONTRACT.md:47 itself says only "dockspace + one-time
dock-builder layout" (no "viewport"), so the contract text supports the
workaround below.

**Proposed resolutions (parent ruling required):**

- **R1 (recommended; fully within Scope C):** Host the dockspace *inside* the
  main demo window via `DockSpace(id, Vector2.zero)` (a perfect native no-op
  when docking is off — returns 0, submits nothing) and gate the one-time
  DockBuilder layout on the non-zero return value of that per-frame
  `DockSpace` call. Docking OFF: zero visual change, layout never applied, no
  warnings; toggled ON mid-session: layout applies on the next frame.
  Docking ON: plots/benchmark dock into a region under the main window's
  widgets (one-time split + dock-by-title + Finish). Deviation: the dockspace
  is not viewport-covering; the docs would teach this in-window pattern.
- **R2 (requires reopening Scope B / parent action):** expose a public
  docking-state query (e.g. `DearImGuiKSP.IsDockingEnabled`) on the facade;
  then the demo keeps a true per-frame `DockSpaceOverViewport` and skips it
  when off. Out of Scope C's allowed file set.

**Secondary finding (non-blocking, docs-relevant):** the curated
`ImGuiDockNodeFlags` subset intentionally omits the `DockSpace` bit (1<<10),
so the upstream-canonical recipe `RemoveNode(id); AddNode(id, flags|
DockSpace); ...` is not expressible — `AddNode` would recreate the node as a
*floating* node (imgui.cpp:21016-21041). The working recipe with the curated
subset is: let `DockSpace`/`DockSpaceOverViewport` create the node, then
`DockBuilderSplitNode` directly on the live node (`SplitNode` accepts any
existing node, imgui.cpp:21166-21194), `DockBuilderDockWindow` by exact
title, `DockBuilderFinish`. Docs must teach this recipe, not the upstream one.

**Status (2026-09-08, resolved):** Parent ruled **D1 ACCEPT — proceed with R1** (R2 rejected, YAGNI) and **D2 ACCEPT**. Implemented per R1: demo hosts the dockspace in-window via `DockSpace(id, Vector2.zero)` with the one-time DockBuilder layout gated on its non-zero return; docs teach the curated split-live-node recipe. Build 0 err/0 warn, tests 191/191. Details in PROGRESS_LOG.md (Scope C). One G5 follow-up noted there: main window is `autoResize: true` while hosting the dock region — confirm in-game the size feedback settles when both windows are docked.
Verified agreeing items (facade signatures, enum ordinals, noDocking overloads,
`docking` default true, 191-test baseline, the three demo window titles,
`ImGuiCol.DockingPreview`/`DockingEmptyBg` slots at ImGuiStyleEnums.cs:96,98,
docs settings table at 10-api-fundamentals.md:271-282) — no other
disagreements.
