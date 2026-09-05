# Handoff: Pre-Release Feature Wave Implementation — M1–M7 VERIFIED, M8 remains
## Session: `notes/active/2026-09-03_PreReleaseFeatureWave_Implementation/`
## Written: 2026-09-04 (M7 docs gate PASSED via sandboxed dry run)

## Where we are

M1–M7 **VERIFIED** (M7 gate: modder-from-zero dry run by an agent sandboxed to
`docs/` + KSP KB only — PASS, zero API misuses on post-hoc audit). Only M8
(C25: packaging + D33) remains. NOTE: ISSUES #004 (UI flicker) is now **P0** —
investigate before M8.
Full state lives in this folder — read `PROGRESS_LOG.md` first, then
`CHUNK_MAP.md` and `INTEGRATION_CONTRACT.md`.

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
- In-game (all user-confirmed through M6): Plex Sans 18px, ksp theme, font
  fallback + v4/v5 popup paths, two-plot window, benchmark no-regression,
  fault-barrier test, spinner/knob/wheel widgets + tween play/cancel/F2-pause,
  telemetry window (graphs/stages/orbit tabs) in flight.
- M6 gate items verified in flight (record): Graphs tab — 2x2 rolling plots
  (altitude/q/throttle/G) animating at 60 Hz, legends present, hover readout line
  under the grid; Stages tab — per-stage propellant meters with color transitions
  on staging, "approx dV" labels, Isp/burn-time readouts; Orbit tab — live radar
  ellipse + vessel/Ap/Pe markers (+ target/node markers when applicable),
  elements readout; main menu → all tabs show `No active vessel.`; no measurable
  FPS cost; D16 environment intact (Deferred etc. unaffected); click-through
  protections still inert-when-not-capturing. Known cosmetic limits: graphs grid
  fixed 520x360, hover readout is a text line not a floating tooltip, spinner
  aesthetics (#006).

## What's next — M8 (C25), preceded by #004 P0 investigation

**Before packaging**: investigate ISSUES **#004 (P0)** flicker — worse in flight,
especially under time warp, no reliable repro; suspects listed in the issue
file (gradient pass window filtering, render-event pump timing vs game frame,
deltaTime source under warp). Also: user has friction points about demo-consumer
ergonomics / docs browsing to triage (raised at the M7 gate, details pending).
Then **M8 = C25** (release packaging + D33 OpenGL decision point; gate: zip
installs into a clean KSP in the full D16 environment, D33 recorded in
DECISION_LOG). C25 follow-ups already assigned: vendor upstream cimplot's MIT
LICENSE into `vendor/cimplot/` (C24 found none ships there); version is 0.1.0.0
from the csproj `<Version>`.

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
6. ISSUES tracker is gitignored — `ISSUES/TRACKER.md` Next ID: **#008**
   (#005 and #007 resolved+archived, #006 KNOWNLIMIT open, #004 P0 open).

## Active gotchas / open threads

- **ISSUES #004** (UNCONFIRMED, **P0** — escalated at M6 gate): UI flicker,
  worse in flight and especially under time warp, more panels open may
  correlate; no reliable repro. Investigate before M8 packaging. Suspects in
  the issue file: gradient pass window filtering, render-event pump timing vs
  game frame, deltaTime source under warp.
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

M7 is VERIFIED. Next work, in order: (1) triage the user's friction points
(demo-consumer ergonomics / docs browsing — details pending from the user);
(2) investigate ISSUES #004 (P0 flicker) before packaging; (3) on the user's
"proceed", write `CHUNK_C25_CONTRACT.md` (read spec §10 + the plan's M8 row
first; DECISION_LOG entry format per
`notes/finished/2026-07-29_DesignSpec_DearImGuiKSP_UI_Library/DECISION_LOG.md`).
