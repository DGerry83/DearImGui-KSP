# Chunk Contract: Gradient Pass — Flip to Inclusion Filter (decoration casualty class kill)
## Plan: DearImGui-KSP Pre-Release Feature Wave
## Date: 2026-09-05
## Chunk ID: C35
## Advances Milestone: Wave polish (pre-M8) — C34 audit follow-up

### Scope
The gradient pass (`ApplyWindowBgGradient`, `DearImGuiKSPNative/src/ContextHost.cpp`)
repaints every solid-fill vert in a window's command 0 to the window gradient.
Three exclusion patches (C27 Text, C32 grips, C34 scrollbars) fixed individual
casualties; the C34 audit found two more (title-bar button hover/held
background — ButtonHovered/ButtonActive; resize-edge highlight —
SeparatorHovered/SeparatorActive) and recommended flipping to an inclusion
filter. This chunk makes that flip.

1. **Flip the filter.** Shade ONLY verts whose full RGBA matches one of the
   intended fills (cached per pass via `GetColorU32`):
   - the window bg color actually used for this window (the pass already picks
     WindowBg vs ChildBg per window — ContextHost.cpp ~314: use THAT resolved
     color so child windows keep working),
   - `ImGuiCol_TitleBg`, `ImGuiCol_TitleBgActive` (title-bar fill, either
     focus state),
   - `ImGuiCol_MenuBarBg`,
   - `ImGuiCol_Border` (and `ImGuiCol_BorderShadow` IF the pinned 1.92.9 border
     path emits it — verify in imgui.cpp; include only if real).
   Everything else keeps its theme color. This fixes the two audited
   casualties and forecloses the whole class. Remove the now-unneeded
   exclusion lists (Text/grip/scrollbar) — the inclusion set replaces them.
2. **Prove no visual regression to the intended look.** Per the C34 audit, the
   intended-shaded set is exactly: bg fill, title-bar fill, menu-bar fill,
   outer border. Harness must show those verts shaded byte-identically to the
   pre-flip behavior (reuse/keep the existing gradient checks: stops applied,
   alpha preserved, disabled-pass byte-exact), AND every known decoration
   primitive surviving: Text prims, grips (idle/hover/active), scrollbars
   (idle/hover/active), PLUS new checks for the two new casualties:
   ButtonHovered-colored vert and SeparatorHovered-colored vert in command 0
   survive byte-unchanged. Keep the existing per-color harness windows; add
   what's needed.
3. **Issue files** — #014 Investigation Log: dated entry recording the flip
   (status stays Open pending the user's in-game gate, which now covers
   scrollbars AND the flip). Note in the entry that the exclusion-era casualty
   list is closed by construction.

### Inputs
- `ContextHost.cpp` current pass (post-C34), `harness_main.cpp` (checks 16,
  20/21, 40-43, 50-59), the C34 audit table in the #014 issue file.
- Pinned `..\..\cimgui\imgui\imgui.cpp` for exact decoration color slots.

### Outputs
- `ContextHost.cpp` (inclusion filter), `harness_main.cpp` (regression +
  survival checks). No managed changes. No new export/TU → no handshake bump,
  build scripts untouched, vendor/ untouched.

### Constraints
- The intended M3 look must be byte-identical for the shaded set; everything
  else renders in true theme colors (that IS the fix).
- Zero alloc; same single pass; 3 native builds 0 errors (1 tolerated C4190).
- If the audit table turns out wrong (e.g. some intended-shaded prim uses a
  color outside the inclusion set), STOP — impediment, don't guess-add colors.

### Verification
- 3 native builds 0 errors; harness PASS incl. all old + new checks;
  `dotnet build` 0/0; `dotnet test` 99/99 (untouched).
- In-game (USER gate, report readiness): full theme eyeball; then the chrome
  checklist — collapse arrow visible + hover bg, grip visible/hover/drag,
  scrollbar visible/hover/drag on the benchmark list, edge-resize highlight on
  window borders, list text and button gradients unchanged.

### Rollback
- `git checkout -- DearImGuiKSPNative/src DearImGuiKSPNative/harness` (C34
  state is committed); rebuild.
