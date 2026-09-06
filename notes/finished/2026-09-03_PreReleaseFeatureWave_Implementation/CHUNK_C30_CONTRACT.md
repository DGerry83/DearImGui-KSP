# Chunk Contract: Spinner Geometry Investigation + Large-Size Demo Rendering
## Plan: DearImGui-KSP Pre-Release Feature Wave
## Date: 2026-09-04
## Chunk ID: C30
## Advances Milestone: Wave polish (pre-M8) — ISSUES #006 (escalated P1)

### Scope
ISSUES #006 was escalated P3→P1: beyond the known color limits, the user sees
GEOMETRY defects — Atom's "electrons" have corners instead of being circles,
and one of Clock's hands is a bare square rotating in the center. The demo
renders spinners at radius 12f, so a prime hypothesis is small-radius
tessellation (ImGui reduces circle segment counts at small radii; a 2px
"circle" is a diamond). The other possibility is a real defect in the generated
cimspinner binding or our draw path. Read the issue file first:
`ISSUES/KNOWNLIMIT-[DearImGuiKSP]_SpinnerAesthetics-LOCAL-2026-09-04/`.

1. **Demo: large-size spinner rendering** (`DearImGuiKSPDemo/ThemeDemo.cs`).
   Add a "Spinners (large)" block to the ThemeDemo window rendering the same
   four showcased types (RainbowMix, Ang8, Clock, Atom) at ~3-4x the current
   radius (and thickness scaled proportionally) inside a CollapsingHeader
   (C28 landed — use it, `defaultOpen: true`). Purpose: the user takes zoomed
   screenshots to examine shapes up close. Keep the existing small row
   untouched.
2. **Static geometry investigation** — trace how Atom's electrons and Clock's
   hands are drawn in the GENERATED binding (`vendor/cimspinner/cimspinner*.cpp`
   — generated, never edited) and upstream (`vendor/imspinner/imspinner.h`,
   lines already cited in the issue file: Atom ~2568, RainbowMix 345; find the
   Clock/Atom draw calls). Determine which ImDrawList primitives they use
   (AddCircle/AddCircleFilled/AddLine thick/PathStroke etc.), what segment
   counts apply at radius 12f vs 36-48f (see ImGui's
   `_CalcCircleAutoSegmentCount` / `style.CircleTessellationMaxError` in the
   pinned imgui_draw.cpp), and whether anything in OUR stack (generated binding
   arg marshaling, the D3D11 backend, the gradient pass) could degrade the
   shapes. Write dated findings into the #006 issue file Investigation Log.
3. **Resolution decision + implementation** — based on findings:
   - If it's small-radius tessellation (likely): resolution is sizing guidance,
     not code surgery — set sensible demo sizes, and add a size note to
     `Spinner` XML docs + `docs/20-widgets.md` (very small radii produce
     low-segment circles by design upstream; recommend >= ~16-20px radius for
     dot/arc-heavy types). Optionally consider a conservative doc'd minimum —
     do NOT clamp silently in the wrapper (surprising behavior); guidance only.
   - If it's a real binding/rendering defect: fix in OUR code (never patch
     `vendor/` — byte-identical rule), same chunk.
   - If a type can't be made to look right at any size: trim it from the
     curated `SpinnerType` set (edit OUR wrapper + docs; do not touch vendor).
   - Full cut of Spinner from 0.1.0 is the user's stated fallback — only if
     the investigation shows the rendering path is unsound, not merely
     size-sensitive. Record the decision + rationale in the issue file and in
     the chunk report; the orchestrator relays it to the user for sign-off.

### Inputs (must exist before starting)
- The #006 issue file; `Api/DearImGuiKSP.Spinner.cs` (curated 15 + docs),
  `Interop/ImSpinnerNative.cs`, `DearImGuiKSPDemo/ThemeDemo.cs` (C28 sections).
- Pinned sources: `vendor/cimspinner/*.cpp`, `vendor/imspinner/imspinner.h`,
  sibling `..\..\cimgui\imgui\imgui_draw.cpp` (tessellation).

### Outputs (must be created/changed)
- `ThemeDemo.cs`: large spinner block (CollapsingHeader, defaultOpen).
- #006 issue file: investigation findings + decision recorded.
- Whatever the decision requires: docs/XML-doc size guidance, wrapper trims,
  or a real fix in our code.
- NO vendor edits. NO spec/process refs or emojis in docs.

### Constraints
- Zero-alloc steady state (facade label convention only).
- If the decision trims `SpinnerType` values: that's a public-API change —
  acceptable pre-release, but the demo, docs, and XML docs must all be updated
  in the same chunk, and tests stay green.
- 3 native builds stay 0 errors IF native is touched (ideally untouched).

### Verification
- `dotnet build DearImGui-KSP.slnx` 0/0; `dotnet test DearImGui-KSP.slnx` 90/90
  (adjust if SpinnerType trim changes a test).
- Native builds + harness PASS if native was touched (expected: untouched).
- In-game (USER's gate, report readiness): large spinners render; zoomed
  screenshot check of Atom electrons and Clock hands at both sizes.

### Rollback
- `git checkout -- DearImGuiKSPDemo DearImGuiKSP/Application/Api docs/`;
  restore any trimmed enum values.
