# Chunk Contract: Scrollbar Visibility Fix (#014) + Decoration Audit
## Plan: DearImGui-KSP Pre-Release Feature Wave
## Date: 2026-09-05
## Chunk ID: C34
## Advances Milestone: Wave polish (pre-M8) — ISSUES #014

### Scope
User report: no vertical scrollbar has ever appeared under the ksp theme, even
on overflowing windows. Root cause already identified (issue file:
`ISSUES/VANILLA-[DearImGuiKSP]_ScrollbarGradient-LOCAL-2026-09-05/`) — third
casualty of the gradient pass repainting command-0 decoration verts, after the
collapse arrow (#008, C27) and the resize grip (C32).

1. **Fix** — extend the exclusion filter in `ApplyWindowBgGradient`
   (`DearImGuiKSPNative/src/ContextHost.cpp`) to also skip verts whose full
   RGBA matches `GetColorU32` of `ImGuiCol_ScrollbarBg`,
   `ImGuiCol_ScrollbarGrab`, `ImGuiCol_ScrollbarGrabHovered`,
   `ImGuiCol_ScrollbarGrabActive` (four cached U32s per pass next to the
   existing text/grip exclusions; zero alloc).
2. **Harness** — extend the survival checks (20/21 and 40-43 pattern): a
   vert of each scrollbar color in command 0 survives the pass byte-unchanged.
   Drive hovered/active via `DearImGuiKSPNative_FeedFrameInput` if practical
   (C32 did this for the grip); idle-only checks are acceptable for grab
   states if driving a real scrollbar hover headless proves fiddly — note
   which.
3. **Decoration audit (so this is the last one)** — enumerate every solid-fill
   primitive ImGui emits into a window's command 0 (pinned imgui.cpp:
   `Begin`/`RenderWindowDecorations`/`UpdateWindowManualResize`, plus the menu
   bar path) and classify each: gradient-shaded (intended: bg fill, title-bar
   fill, menu-bar fill, borders) vs excluded (text-colored prims, grips,
   scrollbars). For anything user-visible still shaded, judge: is its current
   rendering correct/intended? Write the table into the #014 issue file. If
   the audit finds another user-visible casualty, STOP and flag it as an
   impediment — do not grow the fix ad hoc.
4. **Issue file** — dated Investigation Log entry with fix + audit; leave
   status Open pending the user's in-game gate.

### Inputs
- `ContextHost.cpp` (current exclusion block + comments), `harness_main.cpp`
  (existing survival checks), pinned `..\..\cimgui\imgui\imgui.cpp` and
  `imgui_widgets.cpp` (Scrollbar colors: grab uses
  `ImGuiCol_ScrollbarGrab*` — verify exact color slots in the pinned source).

### Outputs
- `ContextHost.cpp`, `harness_main.cpp`; #014 issue file updates.
- No managed changes expected. No new export → NO handshake bump. No new TU →
  build scripts untouched. vendor/ untouched.

### Constraints
- The M3-approved look changes ONLY in that scrollbars become visible in the
  theme's ScrollbarGrab colors; all other shading byte-identical (existing
  byte-exact harness checks must keep passing).
- 3 native builds 0 errors (1 tolerated C4190).

### Verification
- 3 native builds 0 errors; harness PASS incl. new checks; `dotnet build` 0/0;
  `dotnet test` 99/99 (should be untouched).
- In-game (USER gate, report readiness): benchmark window's 1000-item list
  shows a visible scrollbar under the ksp theme (idle + hover + drag states);
  the rest of the theme unchanged; dark preset regression glance (gradient
  off there — must be byte-exact stock).

### Rollback
- `git checkout -- DearImGuiKSPNative/src DearImGuiKSPNative/harness`; rebuild.
