# Chunk Contract: Collapse-Arrow Fix (#008) + Flicker Investigation (#004) + Z-Order First Look (#009)
## Plan: DearImGui-KSP Pre-Release Feature Wave
## Date: 2026-09-04
## Chunk ID: C27
## Advances Milestone: Wave polish (pre-M8) — P0/P1 hygiene before packaging

### Scope
Three related items, all in the window-rendering / frame-loop area. Read the
issue files first (they carry the knowns):
`ISSUES/VANILLA-[DearImGuiKSP]_CollapseArrowGradient-LOCAL-2026-09-04/`,
`ISSUES/UNCONFIRMED-[DearImGuiKSP]_UIFlicker-LOCAL-2026-09-04/`,
`ISSUES/UNCONFIRMED-[DearImGuiKSP]_WindowZOrder-LOCAL-2026-09-04/`.

1. **#008 fix (the concrete deliverable)** — the window-bg gradient pass
   (`DearImGuiKSPNative/src/ContextHost.cpp`, `ApplyWindowBgGradient`, lines
   ~291-366) repaints every solid-fill (white-pixel-UV) vertex in each expanded
   window's first draw command to the window gradient. That recolors the
   title-bar collapse arrow (a solid triangle drawn with the Text color),
   making it invisible against the title bar while expanded. Goal: **the arrow
   (and any other Text-colored primitive verts that merge into command 0 —
   bullets, checkmarks, tree arrows) must keep their colors; the bg fill and
   title-bar fill must keep the gradient shading exactly as the M3-approved
   visuals show; glyph verts stay untouched (existing harness check 16).**
   Candidate approach: extend the existing white-UV filter with a color match —
   shade only verts whose original color is the window bg fill color or one of
   the title-bar fill colors (TitleBg/TitleBgActive), leave everything else.
   IMPORTANT unknown to resolve first: determine what else currently merges
   into command 0 in practice (buttons with C9 gradients? radio rims?
   scrollbar/border fills?) by reading the draw flow, because narrowing the
   filter must not change the user-approved M3 look. If button gradients are
   applied at consumer draw time and their verts merge into command 0, the
   window pass may already be a latent hazard for them — check and note.
   Add/extend harness read-back checks (harness_main.cpp already has gradient
   checks incl. code 16 glyph check): add a check that a Text-colored
   solid-fill primitive vert placed in command 0 survives the pass unchanged,
   and that bg-fill verts are still shaded.

2. **#004 investigation (P0 flicker)** — intermittent whole-UI flicker/vanish
   for 1-2 frames; worse in flight, especially under time warp; no reliable
   repro. This is primarily a static-analysis chunk (no in-game access):
   - Suspects from the issue file: (a) this same gradient pass's window
     filtering (per-frame `window->Active` predicate vs windows that were
     submitted late/skipped), (b) render-event pump timing vs the game frame
     (the GL.IssuePluginEvent / render thread path in Infrastructure), (c)
     deltaTime source under time warp (what clock drives ImGui's io.DeltaTime
     and does it go pathological under warp?).
   - Trace each suspect in code and record findings in the issue file's
     Investigation Log (dated entry). Look specifically for frame-edge
     conditions where a window could be drawn one frame and skipped the next,
     or where the whole draw list could be empty/stale for a frame.
   - If a root cause is found with confidence: fix it (same discipline as any
     chunk). If not: add **verbose-gated diagnostics** (verboseLogging pattern
     already exists — check how it's read) that would let the user's next
     in-game session capture the event (e.g. log once per occurrence when the
     submitted window count or draw-list size changes discontinuously
     frame-to-frame). Diagnostics must be zero-cost when verbose logging is off.

3. **#009 first look** — static characterization only: trace how window order
   is decided each frame (registration order of consumers? ImGui focus stack?
   anything in FrameLoopOrchestrator/Composition that reorders?) and confirm
   there is no flag or mechanism suppressing ImGui's default
   bring-to-front-on-click. Write findings into the issue file Investigation
   Log, including a precise list of what the user should test in-game
   (click body vs title bar, drag, hover) to characterize the observed fixed
   ordering. NO code change for #009 in this chunk.

### Inputs (must exist before starting)
- The three issue files (paths above).
- `DearImGuiKSPNative/src/ContextHost.cpp`, `harness/harness_main.cpp`,
  `DearImGuiKSP/Application/FrameLoopOrchestrator.cs`,
  `DearImGuiKSP/Infrastructure/` (render pump, NativeBridge).
- M3-FIX history in PROGRESS_LOG.md (the white-UV filter's origin).

### Outputs (must be created/changed)
- `ContextHost.cpp` gradient-pass fix + `harness_main.cpp` new/extended checks.
- Issue-file Investigation Log entries for #004, #008, #009 (dated 2026-09-04).
- If #004 root cause not found: verbose-gated diagnostic additions (minimal).
- All 3 native build scripts unchanged unless a new TU is added (then all 3).

### Constraints
- Vendored sources are byte-identical to upstream pins — never patch
  `vendor/`; fixes go in our `src/` or managed code.
- Hot-path discipline (CORE_PROTOCOLS §5.9): no per-frame allocation or extra
  passes on the steady path; diagnostics zero-cost when verbose is off.
- The M3-approved visual look must not change: bg/title gradients, list text,
  radio rims all render as before. If the filter change alters any visible
  shading beyond restoring the arrow, STOP and flag as an impediment.
- 3 native builds must stay 0 errors (1 tolerated C4190 from cimspinner).

### Verification
- `cd DearImGuiKSPNative && build.bat && build_release.bat && build_harness.bat`
  all 0 errors; `build/harness.exe` → HARNESS PASS including the new checks.
- `dotnet build DearImGui-KSP.slnx` 0/0; `dotnet test` 90/90.
- In-game confirmation (arrow visible expanded+collapsed; flicker diagnostics
  or fix) is the USER's gate after the chunk — report readiness, do not claim
  in-game verification yourself.

### Rollback
- `git checkout -- DearImGuiKSPNative/src DearImGuiKSPNative/harness` (and any
  managed files touched) — pre-chunk state is committed.
