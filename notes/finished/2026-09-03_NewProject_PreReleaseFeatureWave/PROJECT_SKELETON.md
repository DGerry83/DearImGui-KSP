# Project Skeleton: DearImGui-KSP Pre-Release Feature Wave
## Date: 2026-09-03
## Session: `notes/active/2026-09-03_NewProject_PreReleaseFeatureWave/`

This wave extends an existing, verified codebase — the full layered skeleton,
`AGENTS.md`, `README.md`, build/test wiring, and GameData staging were created in the
2026-07-29 bootstrap (`notes/finished/2026-07-29_NewProject_DearImGuiKSP/PROJECT_SKELETON.md`)
and stand unchanged. This document records the **delta skeleton**: only the directories
and scaffolding files this wave adds. No implementation code was written.

## Directories and files created

| Path | Contents now | Real contents arrive in |
|------|--------------|--------------------------|
| `DearImGuiKSP/Application/Theming/README.md` | Placeholder readme (project placeholder convention, cf. `tests/Core.Tests/`) | M3: ThemeEngine, ThemePresets, KspPalette |
| `DearImGuiKSP/Application/Animation/README.md` | Placeholder readme | M5: Ease, Tween, TweenEngine |
| `DearImGuiKSP/Application/Api/README.md` | Placeholder readme | M1–M5: ImGuiEx, ImGuiPlot, ImGuiWidgets, ImGuiGradients |
| `DearImGuiKSPDemo/Telemetry/README.md` | Placeholder readme (public-API-only rule recorded) | M6: addon, sampler, ring buffer, analyzer, three panels |
| `DearImGuiKSPNative/src/shims/README.md` | Placeholder readme | M3/M5: knobs/wheels/toggle C ABI shims |
| `DearImGuiKSPNative/vendor/PIN_RECORD.md` | Pin-record template, all rows `_pending_` | M4 (toggle possibly M3): vendored trees + pins |
| `docs/00-getting-started.md` … `docs/70-troubleshooting.md` | 8 title stubs with planned contents | M7: full docs set (spec §8.2, D31) |

## Deliberately not created

- **Managed/native source files** (`ThemeEngine.cs`, shims, etc.) — creating empty
  `.cs`/`.cpp` files would add noise and risk stale placeholders; each milestone's
  chunk creates its real files per `IMPLEMENTATION_PLAN.md` §7.
- **`GameData/DearImGuiKSP/Fonts/`** — the GameData tree is mirrored into the game on
  every managed build, so a placeholder there would ship to the player. M2 creates the
  folder when it copies the real TTFs + OFL.txt.
- **Interop files (`ImPlotNative.cs`, …)** — created with the bindings they contain (M4/M5).

## Existing skeleton confirmation (gate G4/G5 evidence)

- Layered structure present: `DearImGuiKSP/Application|Infrastructure|Interop`,
  `DearImGuiKSPNative/src`, `tests/Application.Tests` (+ documented placeholders for
  Core/Infrastructure tests), `GameData/` staging.
- `AGENTS.md` (root, current — workflow, layout, build/test commands, hard constraints)
  and `README.md` exist and cover the project; no changes required for this wave.
- Build/test commands unchanged: `dotnet build DearImGui-KSP.slnx`,
  `cd DearImGuiKSPNative && ./build.bat` (or `build_release.bat`), `dotnet test DearImGui-KSP.slnx`.
