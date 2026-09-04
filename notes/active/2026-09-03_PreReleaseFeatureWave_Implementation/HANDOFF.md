# Handoff: Pre-Release Feature Wave Implementation — Resume at M5
## Session: `notes/active/2026-09-03_PreReleaseFeatureWave_Implementation/`
## Written: 2026-09-04 (paused by user after M4 verification)

## Where we are

Milestones M1–M4 **VERIFIED** (in-game, user-confirmed). M5–M8 remain.
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

### Verified state
- `dotnet build DearImGui-KSP.slnx` 0/0; `dotnet test` **76/76**; native harness
  **HARNESS PASS** (incl. gradient + ImPlot context checks); all 3 native builds 0 errors.
- In-game: Plex Sans 18px default, ksp theme default (gradient windows/buttons,
  circular radios with rim, animated toggles, grey secondary buttons, orange input
  text), font fallback + v4/v5 mismatch popup paths, demo two-plot window live,
  benchmark no-regression, fault-barrier scope-exception test.

## What's next — M5 (widgets + tween), chunks C14–C17

Order per `INTEGRATION_CONTRACT.md`: **C14 → C15 → C16 → C17** (C15/C16 serialized —
both edit the 3 native build scripts + `ExtensionShimsNative.cs`).

- **C14** — C# tween engine (`Application/Animation/`, ~3 files). Zero native work;
  xUnit-gated. API per spec §4.3: `Tween.To(Action<float> set, from, to, seconds,
  Ease)` → handle with `Cancel`/`IsPlaying`; Color overload; driven by the library
  frame loop. Acceptance: tween/easing xUnit green incl. cancel, completion
  removal, suspension pause.
- **C15** — imgui-knobs (altschuler, MIT) + imgui-wheels (Engineer162, MIT —
  immature, knowingly accepted spec §11; droppable pre-release if it misbehaves).
  Vendor + PIN_RECORD rows + hand shims in `src/shims/` + facade methods on the
  `DearImGuiKSP` partial (same pattern as C10's `DearImGuiKSP.Toggle.cs`).
- **C16** — imspinner (dalerank, header-only) + cimspinner regeneration against the
  pinned cimgui clone (same generator drill as C11 — canonical **gcc** path, LuaJIT;
  see I-06). `Spinner(type, ...)` enum-dispatched over ~15 curated types (spec §4.3),
  NOT all ~590.
- **C17** — ThemeDemo showcase tab in the demo mod consuming C9/C10/C14/C15/C16.
  M5 gate: all widgets + animations live in-game, user sign-off.

Then M6 (telemetry showcase C18–C21; C20/C21 parallel-safe, C19 isolated as first
real ImGuiPlot consumer), M7 (docs C22–C24), M8 (packaging C25 + D33 OpenGL
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

Write `CHUNK_C14_CONTRACT.md` (tween engine — see plan
`notes/active/2026-09-03_NewProject_PreReleaseFeatureWave/IMPLEMENTATION_PLAN.md`
§2 "Entity: Tween/TweenHandle", §3 "Component: TweenEngine", spec §4.3) and
dispatch its coder sub-agent, per the per-chunk loop above.
