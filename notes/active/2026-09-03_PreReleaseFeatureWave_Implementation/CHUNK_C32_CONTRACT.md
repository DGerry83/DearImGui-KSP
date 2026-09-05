# Chunk Contract: C31 Gate Fixes — Grip Visibility, Slider Type-In, uiScale Docs
## Plan: DearImGui-KSP Pre-Release Feature Wave
## Date: 2026-09-04
## Chunk ID: C32
## Advances Milestone: Wave polish (pre-M8) — C31 in-game gate findings

### Scope
The user's C31 in-game pass found three defects. All causes already grounded
or strongly suspected by the orchestrator — verify, fix, re-verify.

**A. Resize grip STILL invisible (hover/drag included).** Root cause confirmed
statically: the grip is drawn by `RenderWindowDecorations` as a solid-fill
`PathFillConvex(col)` (pinned imgui.cpp:7728-7743) into `window->DrawList`
during `Begin` — i.e. into command 0, white-pixel UV — so `ApplyWindowBgGradient`
(ContextHost.cpp) repaints it to the window gradient; at the bottom-right corner
t≈1, i.e. the bottom stop = the window bg color right there. Invisible by
construction; the C31 preset colors never stood a chance. The C27 Text-color
exclusion fixed the collapse arrow identically. Fix: extend the exclusion
filter in `ApplyWindowBgGradient` to also skip verts whose full RGBA matches
`GetColorU32` of `ImGuiCol_ResizeGrip`, `ImGuiCol_ResizeGripHovered`,
`ImGuiCol_ResizeGripActive` (three cached U32s per pass, compare per
solid-fill vert, zero alloc). Harness: extend the existing primitive-survival
check — a grip-colored vert in command 0 must survive the pass byte-unchanged,
all three state colors.

**B. Slider type-in missing.** Sliders already pass `ImGuiSliderFlags.None`
(ctrl+click temp-input is on ImGui-side), but it doesn't reach the user —
do NOT chase the temp-input path; implement the user's requested UX: a numeric
**type-in box next to each slider** in the library control panel
(`Infrastructure/LibraryControlPanel.cs`, uiScale + fontScale sliders).
Internal-only addition: wrap `igInputFloat` (cimgui — verify exact signature
against `..\..\cimgui\cimgui.h`) in Interop per the existing extern+wrapper
pattern, place it right of the slider via the internal SameLine (M3-TUNE
already added it). Two-way: typing sets the value (clamped 0.5–2.0 by the
SettingsModel setters already), dragging updates the box. Allocation while the
panel is OPEN is acceptable (user-driven); closed panel stays zero-cost.
Behavior note: uiScale type-in applies live; fontScale type-in persists for
restart — same semantics as the sliders, just a second input path.

**C. uiScale scope must be documented.** User observation: uiScale does not
scale gradient buttons, spinners, knobs, wheels. Explanation to encode: uiScale
scales style-driven metrics (padding, spacing, rounding) and font rendering
(ScaleAllSizes + FontGlobalScale); widgets whose size the CONSUMER passes in
pixels (spinner radius/thickness, knob/wheel size, explicit plot/button sizes)
keep exactly the pixels asked for — scaling them is the consumer's choice
(e.g. multiply by the configured scale). Update: the panel note in
`docs/00-getting-started.md` (what uiScale does/doesn't cover) and the
`docs/20-widgets.md` sizing guidance (one sentence cross-ref). No spec refs,
no emojis.

### Inputs (must exist before starting)
- `DearImGuiKSPNative/src/ContextHost.cpp` (`ApplyWindowBgGradient`, the C27
  Text exclusion), `harness/harness_main.cpp` (checks 20/21 pattern).
- `DearImGuiKSP/Infrastructure/LibraryControlPanel.cs`,
  `Interop/ImGuiNative.cs` / `ImGuiInternal.cs` (slider/SameLine/InputText
  patterns), `Application/SettingsModel.cs` (clamping setters).

### Outputs (must be created/changed)
- `ContextHost.cpp` exclusion extension; `harness_main.cpp` grip-color checks.
- `Interop` InputFloat extern+wrapper; `LibraryControlPanel.cs` type-in boxes.
- Docs edits (00 + 20).
- NO handshake bump (no new native export — the filter change is internal;
  InputFloat is an existing cimgui symbol — verify presence via a verify
  script, c28/c31 pattern). If InputFloat is somehow not exported, STOP —
  impediment.

### Constraints
- The M3-approved look changes only in that the grip becomes visible (with
  C31's colors) — bg/title/border/scrollbar shading must stay identical
  (harness byte-checks already exist; keep them passing).
- Zero-alloc steady state with panel closed.
- 3 native builds 0 errors (1 tolerated C4190); no new TU expected (build
  scripts untouched).

### Verification
- 3 native builds 0 errors; harness PASS incl. new grip checks.
- `dotnet build` 0/0; `dotnet test` 99/99 (or more if a test is added).
- In-game (USER gate, report readiness): grip visible bottom-right, brightens
  on hover, dragging it resizes; both sliders accept typed values (uiScale
  live-applies, fontScale persists for restart); no other visual change.

### Rollback
- `git checkout --` touched files; rebuild; harness PASS.
