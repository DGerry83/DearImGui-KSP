# Handoff: Pre-Release Feature Wave Implementation — WAVE COMPLETE (M1–M8 VERIFIED)
## Session: `notes/finished/2026-09-03_PreReleaseFeatureWave_Implementation/` (moved from active on completion)
## Written: 2026-09-05, final update 2026-09-05 (M8 gate retry PASS — #015/#016 resolved; README rewritten user-facing per user; 1.0.0 zips final; GitHub publishing is the user's own step)

## Where we are

M1–M7 **VERIFIED**; wave polish C26–C36 all Done with all gates PASSED;
**C25 (M8 packaging) is Done in code**: version set to **1.0.0** (user's
call — D36 records the paradigm: major = breaking, minor = features,
patch = fixes, integers with trailing reset),
`package_release.bat` produces both zips in `dist/` (verified: manifests,
versions, no PDBs, License.txt + Docs + CHANGELOG included), user's MIT
`LICENSE.txt` committed and wired to ship as `GameData/.../License.txt`,
cimplot's upstream MIT LICENSE vendored, and **D35 (OpenGL ships
post-release) recorded** in the design DECISION_LOG. The first M8 gate
attempt surfaced two release-blocking defects — **#015** (library toolbar
button main-menu-only; fixed by converting `LibraryPanelToolbar` to a
Composition-owned plain class with one-shot persistent registration) and
**#016** (demo refused to load: its `KSPAssemblyDependencyEqualMajor` was
still `(0, 1)` after the major bump — D17 working as designed; bumped to
`(1, 0)`). Both fixed (M8-FIX), zips repackaged and re-verified.
Remaining: the M8 user gate retry — install both fixed zips into a clean
KSP in the D16 environment. Full state lives in this folder — read
`PROGRESS_LOG.md` first, then `CHUNK_MAP.md` and `INTEGRATION_CONTRACT.md`.

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
| `6634a09` | M7 retrospective: friction dispositions; wave-polish C26–C30 planned (#006 escalated, #008–#011 filed) |
| `a30d076` | C26: docs polish (strip headers, separate-install clarity, spec-ref purge, plain-language skim) |
| `38db86a` | C27: #008 collapse-arrow fix; **#004 P0 flicker root-caused + fixed** (SRWLOCK frame guard vs render thread); #009 characterized |
| `f4637ba` | C27 gate PASS: #004/#008 resolved, #009 not-a-defect (#012 spun off); verboseLogging run-through → C25 |
| `0f12612` | C28: public CollapsingHeader + ThemeDemo sections + docs |
| `838aadd` | C29: throttle dial (KnobVariant.Wiper) two-way bound to vessel throttle + docs binding example |
| `c3fcacb` | C30: spinner geometry = upstream small-size behavior; large-size demo block + sizing guidance |
| `d90270d` | C28–C30 gates PASS (user); #006 geometry closed as sizing issue, aesthetics post-release |
| `86fa455` | C31: library control panel (theme/uiScale live, font+fontScale restart, verbose toggle) + handshake **v6** + visible resize grips + user toolbar icon |
| `f2e73e2` | C32: grip-color exclusion in gradient pass, slider type-in boxes, uiScale scope docs |
| `2b8d854`/`5f9d562` | C31 gate PARTIAL → C32 gate PASS (user) |
| `dfcc600` | C33: public Window(autoResize) fit-to-content; demo/telemetry/panel auto-fit; benchmark/plot user-resizable w/ note; sizing-model docs |
| `3120803` | C34: scrollbar-color exclusion (#014); command-0 decoration audit (2 more casualties found → inclusion flip recommended) |
| `472db3a` | C35: gradient pass flipped to **inclusion filter** (bg/title/menu/border only) — decoration casualty class closed; harness checks 60/61 |
| `46f249b` | C32 gate PASS + C33–C35 logged done |
| `723c3fd` | C33–C35 gates PASS (user); **#014 resolved+archived**; C36: plot auto-fit default (6 demo plots, new public `SetupAxesAutoFit()`) + settings-panel fixed widths |
| `cf5e3d2` | C36 follow-up: uiScale hint split to two lines; verboseLogging KSP.log inspected — clean |
| `a8bbdb7` | C25 contract: release packaging + D35 OpenGL record |
| `92f35ec` | C25: 0.2.0.0 bump, `package_release.bat`, both zips built+verified, CHANGELOG, user's MIT LICENSE.txt wired to ship, cimplot LICENSE vendored, D35 recorded |
| `2f70586` | C25: **1.0.0** everywhere (D36), zips repackaged + re-verified |
| `cf187d1` | M8-FIX: #016 demo EqualMajor(0,1)→(1,0); #015 toolbar → persistent one-shot registration; zips repackaged (tests 99/99) |

### Verified state
- `dotnet build DearImGui-KSP.slnx` 0/0; `dotnet test` **99/99**; native
  harness **HARNESS PASS** (60/61 checks after C35 — grip/scrollbar/
  title-button/border-highlight survival checks all green); all 3 native
  builds 0 errors (1 tolerated C4190 from generated cimspinner code).
  Handshake is **v6** — managed and native must both be this build; native
  DLLs mirror into the game install (`C:\SSDGames\ReformTestInstance`) on build.
- In-game (all user-confirmed through C32): everything through M7 plus
  flicker gone (#004), collapse arrow visible (#008), z-order confirmed
  working as designed (#009), collapsible sections, throttle dial two-way
  binding, zoomed spinners smooth (sizing issue, beautification deferred),
  control panel (theme radios, uiScale/fontScale sliders, verbose toggle,
  icon button), cfg persistence.
- C31/C32 gate notes: performance unchanged post-flicker-fix; uiScale does
  not scale gradient buttons/spinners/knobs/wheels (documented scope =
  style metrics + font rendering); sliders get explicit type-in boxes
  (C32); resize grip exclusion (C32) made grips visible again.

## What's next — nothing in this wave; it is COMPLETE

M8 gate retry PASSED 2026-09-05 (user): library toolbar icon in all
scenes, demo loads with button + windows everywhere, no regressions.
#015/#016 resolved+archived. README rewritten user-facing (old dev-facing
version archived in this folder as `README_archive_2026-09-05.md`);
shipped `GameData/DearImGuiKSP/Readme.txt` rewritten to match. Final
1.0.0 zips in `dist\`. Remaining is outside this session: **GitHub
publishing is the user's own step** (recommendation delivered: one release
carrying both zips, annotated tag + `gh release create --notes-from-tag`;
full CI build rejected — proprietary KSP reference assemblies + pinned
cimgui sibling clone can't live on a runner). Post-release threads (all
recorded): the user's stripped-down GL-capable install unblocks D35
OpenGL work; deferred backlog below. Process follow-up carried forward:
the demo's EqualMajor attribute is a release stepping-stone — bump it with
every future library major.

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
6. ISSUES tracker is gitignored — `ISSUES/TRACKER.md` Next ID: **#017**
   (resolved+archived: #004 P0 flicker, #005, #007, #008, #009 not-a-defect,
   #014 P1 scrollbar; open: #006 P3 spinner aesthetics, #010 P3 plot
   auto-fit, #011 P3 docking, #012 P3 focus highlight, #013 P3 live font
   atlas rebuild; **In Progress pending M8 gate retry: #015 P1 toolbar scene
   persistence, #016 P1 demo EqualMajor block** — both fixed in code,
   resolve only on user confirmation).

## Active gotchas / open threads

- **ISSUES #014** (P1): **RESOLVED + archived 2026-09-05** (user gate PASS).
  C34 patched the gradient pass, then
  the C34 audit found 2 more casualties of the same class (title-bar button
  hover bg, edge-resize highlight), so C35 flipped the pass to an
  **inclusion filter**: shades only verts matching resolved bg
  (WindowBg/ChildBg/PopupBg), TitleBg/TitleBgActive, MenuBarBg, Border;
  everything else keeps theme colors. Audit finding #13 corrected by the
  agent (integer-thickness border highlight uses baked textured lines,
  already UV-gated). The whole exclusion whack-a-mole class is closed.
- **ISSUES #006** (KNOWNLIMIT, P3): spinner aesthetics — RainbowMix hue comes from
  the tint's HSV (white tint → grey arc, no rainbow); Atom's electron dots are
  hardcoded RGB upstream. Geometry was explained as upstream small-size
  behavior (C30); aesthetics deferred post-release by user.
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
- Deferred post-release backlog (all recorded): #006 spinner aesthetics,
  #010 plot auto-fit + content-region query, #011 docking, #012 focus
  highlight, #013 live font rebuild, public layout helpers (SameLine),
  tooltip primitive, mod/user control of rolling plot window size.

## First action on resume

None — this session is complete (2026-09-05). This folder now lives under
`notes/finished/` and stands as the wave's implementation record. For
post-release work (D35 OpenGL, the deferred backlog in "Active gotchas /
open threads"), start a new session per the FlyByWire Router.
