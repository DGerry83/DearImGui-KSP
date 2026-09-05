# Handoff: Pre-Release Feature Wave Implementation — M6 gate pending
## Session: `notes/active/2026-09-03_PreReleaseFeatureWave_Implementation/`
## Written: 2026-09-04 (paused for user M6 in-flight gate)

## Where we are

M1–M5 **VERIFIED**; M6 chunks C18–C21 all **Done and committed** — waiting on the
user's in-flight M6 gate. M7 (docs) and M8 (packaging) remain.
Full state lives in this folder — read `PROGRESS_LOG.md` first (chunk table,
milestone table, decisions), then `CHUNK_MAP.md` and `INTEGRATION_CONTRACT.md`.

### Commits (on `main`; user pushes to remote themselves)
| Commit | Content |
|--------|---------|
| `e702313` | Session bootstrap (plan digest, gates, chunk map, integration contract) |
| `c09fa42` | M1 (C1–C3: Vector2/Color facade, ImGuiEx scopes) |
| `8a7f628`…`60f49b4` | M2 (C6 settings, C4 font export v5, C7 assets, C5 wiring, I-03 fix, 18px font) |
| `7cf14ae`…`02fdd05` | M3 (C8 theme engine, C10 imgui_toggle, C9 gradients+radio, M3 tune pass) |
| `fee6070` | M3 gate fixes: gradient pass white-UV filter (list-text bug), radio rim |
| `964a68b` | C11: ImPlot v1.0 vendored + cimplot regenerated (I-06 accepted) |
| `7a7588c` | C12: ImPlot context lifecycle (harness checks 18/19) |
| `eb4879f` | C13: managed ImPlot wrapper + demo two-plot window (I-07 accepted) |
| `4b9ad41` | M4 gate VERIFIED |
| `602000e`, `8b823b9` | C14 contract + tween engine (tests 90/90) |
| `24bacdf`, `c117866` | C15 contract + knobs/wheels vendored, shimmed, wrapped |
| `55beb69`, `9a1ed51` | C16 contract + imspinner/cimspinner + Spinner wrapper (I-08 accepted) |
| `a96d64c` + `c722490` | C17 contract + ThemeDemo showcase (I-C17-01 accepted) |
| `dad7666` | M5-FIX: spinner empty-ID assert (ISSUES #005 resolved); M5 gate VERIFIED |
| `9b7049f`, `0f18770` | C18 contract + telemetry foundation (tab bindings, RingBuffer, sampler, addon skeleton) |
| `591f8c0`, `5a533c7` | C19 contract + graph panel (subplots scope, hover readout) |
| `978139c`, `b098aad` | C20 contract + ImGuiDraw surface + stage analyzer/panel (C20/C21 serialized) |
| `6735c56`, `45de285` | C21 contract + orbit panel (M6 gate pending) |

### Verified state
- `dotnet build DearImGui-KSP.slnx` 0/0; `dotnet test` **90/90**; native harness
  **HARNESS PASS**; all 3 native builds 0 errors (1 tolerated C4190 from generated
  cimspinner code). New bindings proven via export spot-checks (c18/c19/c20
  verify scripts) — no native rebuild was needed for M6 (cimgui/cimplot compiled
  in whole).
- In-game (all user-confirmed through M5): Plex Sans 18px, ksp theme, font
  fallback + v4/v5 popup paths, two-plot window, benchmark no-regression,
  fault-barrier test, spinner/knob/wheel widgets + tween play/cancel/F2-pause.
- **M6 gate checklist for the user** (FLIGHT scene; new toolbar button, blue
  icon, telemetry window starts hidden): Graphs tab — 2x2 rolling plots
  (altitude/q/throttle/G) animating at 60 Hz, legends present, hover readout line
  under the grid; Stages tab — per-stage propellant meters with color transitions
  on staging, "approx dV" labels, Isp/burn-time readouts; Orbit tab — live radar
  ellipse + vessel/Ap/Pe markers (+ target/node markers when applicable),
  elements readout; main menu → all tabs show `No active vessel.`; no measurable
  FPS cost; D16 environment intact (Deferred etc. unaffected); click-through
  protections still inert-when-not-capturing. Known cosmetic limits: graphs grid
  fixed 520x360, hover readout is a text line not a floating tooltip, spinner
  aesthetics (#006).

## What's next — M7 (docs), chunks C22–C24

After the user confirms the M6 gate in-flight: **C22/C23** (parallel-safe, disjoint
doc files per spec §8.2) then **C24** (XML-doc sweep + LICENSE attribution
aggregation: MIT x4 / 0BSD / OFL). Then M8 (C25: release packaging + D33 OpenGL
decision point).

## How this session runs (conventions the next agent must keep)

1. **Per-chunk loop**: write `CHUNK_Cn_CONTRACT.md` in this folder (template: any
   existing CHUNK_C*_CONTRACT.md — Scope/Inputs/Outputs/Constraints/Verification/
   Rollback) → commit it → dispatch a coder sub-agent whose prompt points at the
   contract + pattern files → rule on its IMPEDIMENTS entries → update
   `PROGRESS_LOG.md` → commit the chunk. **Per-milestone commits** (user-approved).
2. **User checkpoint cadence**: the user says "proceed" between chunks/groups and
   performs ALL in-game verification at milestone gates. Never mark a milestone
   VERIFIED without the user's in-game confirmation.
3. **Frozen artifacts**: `GATES.md` criteria must not change (filling verdicts is
   fine). Settled decisions in PROGRESS_LOG "Decisions Made" — don't reopen.
4. **Build/test commands**: managed `dotnet build DearImGui-KSP.slnx` (mirrors
   GameData into the game install `C:\SSDGames\ReformTestInstance`); native
   `cd DearImGuiKSPNative && build.bat` / `build_release.bat` / `build_harness.bat`
   (+ run `build/harness.exe`); tests `dotnet test DearImGui-KSP.slnx`.
   Any new native TU goes in all 3 scripts. dumpbin fails under MSYS — use the
   PowerShell PE-export parser (`DearImGuiKSPNative/build/c*_verify.ps1`).
5. **Never commit** `notes/plans/2026-09-03_DearImGui-KSP_Fix_Backlog.md`
   (untracked, belongs to another session).
6. ISSUES tracker is gitignored — `ISSUES/TRACKER.md` Next ID: **#007**
   (#005 resolved+archived, #006 KNOWNLIMIT open).

## Active gotchas / open threads

- **ISSUES #004** (UNCONFIRMED, P2): occasional 1–2 frame UI flicker/disappear,
  incl. on rapid button clicks. Filed only; not investigated. Per-frame window
  filtering is a suspect.
- **ISSUES #006** (KNOWNLIMIT, P3): spinner aesthetics — RainbowMix hue comes from
  the tint's HSV (white tint → grey arc, no rainbow); Atom's electron dots are
  hardcoded RGB upstream. Deferred pre-release by user at M5 gate; resolution
  candidates in the issue file.
- **I-01…I-08** all accepted in `IMPEDIMENTS.md` — notably I-06 (cimplot generator
  gcc-canonical; `implot_demo.cpp` link-required), I-07 (cimplot v1.0 ABI takes
  `ImPlotSpec_c` struct; span pinning needs `DangerousGetPinnableReference` on
  Unity 2019.4 mscorlib), I-08 (cimspinner generator is Ruby `genCimSpinner.rb`,
  byte-identical proven; curated 15 = upstream config-enabled subset).
- cimplot/cimspinner regeneration provenance: scratch clones live under
  `%TEMP%\c11_scratch` (may be gone); generator commits are in `vendor/PIN_RECORD.md`.
- Native DLLs for mismatch testing stashed at `DearImGuiKSPNative/build/native_v4.dll`
  / `native_v5.dll`.
- Vendored sources are byte-identical to upstream pins — never patch in place;
  drift → IMPEDIMENTS.md, not silent fixes.
- The benchmark "declaration ms" times only the benchmark window's own content
  (by design, for the IMGUI comparison); FPS line is the whole-game metric.

## First action on resume

If the user has confirmed the M6 gate: mark M6 VERIFIED in PROGRESS_LOG.md, then
write `CHUNK_C22_CONTRACT.md` + `CHUNK_C23_CONTRACT.md` (docs sets A/B per spec
§8.2 — 00-getting-started, 10-api-fundamentals, 20-widgets, 30-theming vs
40-plotting, 50-animation, 60-migration-from-imgui, 70-troubleshooting) and
dispatch both coder sub-agents in parallel, per the per-chunk loop above.
If the M6 gate found defects: file per `ISSUES/README.md` (Next ID: #007), fix
forward in a patch chunk, re-verify.
