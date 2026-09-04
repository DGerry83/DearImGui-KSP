# Handoff: Pre-Release Feature Wave Implementation — M5 gate pending
## Session: `notes/active/2026-09-03_PreReleaseFeatureWave_Implementation/`
## Written: 2026-09-04 (paused for user M5 in-game gate)

## Where we are

M1–M4 **VERIFIED**; M5 chunks C14–C17 all **Done and committed** — waiting on the
user's in-game M5 gate. M6–M8 remain.
Full state lives in this folder — read `PROGRESS_LOG.md` first (chunk table,
milestone table, decisions), then `CHUNK_MAP.md` (remaining chunks/dependencies)
and `INTEGRATION_CONTRACT.md` (locked inter-chunk contracts, per-chunk acceptance).

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
| `a96d64c` + C17 commit | C17 contract + ThemeDemo showcase (I-C17-01 accepted) |

### Verified state
- `dotnet build DearImGui-KSP.slnx` 0/0; `dotnet test` **90/90**; native harness
  **HARNESS PASS**; all 3 native builds 0 errors (1 tolerated C4190 from generated
  cimspinner code).
- In-game (M1–M4 evidence): Plex Sans 18px default, ksp theme default, font
  fallback + v4/v5 mismatch popup paths, demo two-plot window, benchmark
  no-regression, fault-barrier scope-exception test.
- **M5 gate checklist for the user** (demo window, main window "Widget showcase"
  section): spinner row animating (4 types, one green-tinted); Tick knob is
  tween-driven / WiperOnly knob draggable; horizontal + vertical wheels draggable;
  "Play tween" animates the knob 0↔100 and the section header orange↔green over
  2 s, ping-pongs on re-click; "Cancel tween" freezes mid-flight; F2 suspension
  pauses tween motion (resume continues). Note: spinner row is stacked vertically
  (no public SameLine — accepted, I-C17-01).

## What's next — M6 (telemetry showcase), chunks C18–C21

After the user confirms the M5 gate in-game: **C18 → C19 → C20/C21** (C20/C21
parallel-safe; C19 isolated as the first real ImGuiPlot consumer). Contracts per
INTEGRATION_CONTRACT: C18 ring buffer + sampler + addon skeleton with placeholder
tabs; C19 graph panel (2x2 rolling plots); C20 stage analyzer/panel; C21 orbit
panel. M6 gate: in-flight acceptance in the full D16 environment.

Then M7 (docs C22–C24; C22/C23 parallel-safe), M8 (packaging C25 + D33 OpenGL
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
6. ISSUES tracker is gitignored — `ISSUES/TRACKER.md` Next ID: **#005**.

## Active gotchas / open threads

- **ISSUES #004** (UNCONFIRMED, P2): occasional 1–2 frame UI flicker/disappear,
  incl. on rapid button clicks. Filed only; not investigated. Per-frame window
  filtering is a suspect.
- **I-01…I-07** all accepted in `IMPEDIMENTS.md` — notably I-06 (cimplot generator
  must run gcc-canonical; `implot_demo.cpp` is link-required) and I-07 (cimplot
  v1.0 ABI takes `ImPlotSpec_c` struct, AUTO sentinels load-bearing; span pinning
  needs `DangerousGetPinnableReference` on Unity 2019.4 mscorlib).
- cimplot/cimspinner regeneration provenance: scratch clones live under
  `%TEMP%\c11_scratch` (may be gone); generator commits are in `vendor/PIN_RECORD.md`.
- Native DLLs for mismatch testing stashed at `DearImGuiKSPNative/build/native_v4.dll`
  / `native_v5.dll`.
- Vendored sources are byte-identical to upstream pins — never patch in place;
  drift → IMPEDIMENTS.md, not silent fixes.
- The benchmark "declaration ms" times only the benchmark window's own content
  (by design, for the IMGUI comparison); FPS line is the whole-game metric.

## First action on resume

If the user has confirmed the M5 gate: mark M5 VERIFIED in PROGRESS_LOG.md, then
write `CHUNK_C18_CONTRACT.md` (telemetry foundation — plan
`notes/active/2026-09-03_NewProject_PreReleaseFeatureWave/IMPLEMENTATION_PLAN.md`
§2 "Entity: TelemetrySample/RingBuffer", §3 "Component: TelemetrySampler", spec §5.5)
and dispatch its coder sub-agent, per the per-chunk loop above.
If the M5 gate found defects: file per `ISSUES/README.md` (Next ID: #005), fix
forward in a patch chunk, re-verify.
