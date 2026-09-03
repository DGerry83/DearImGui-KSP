# AGENTS.md — DearImGui-KSP

## What this project is

**DearImGui-KSP** is a shared KSP mod library providing a modern, high-performance UI framework as a drop-in replacement for Unity IMGUI. Other mods hard-depend on it via `KSPAssemblyDependencyEqualMajor("DearImGuiKSP", x, y)`.

- **Target**: KSP 1.12.x, Unity 2019.4.18f1, Mono x64, Windows-first, D3D11 (OpenGL deferred post-MVP, D20).
- **Structure**: `DearImGuiKSPNative.dll` (C++: Dear ImGui + cimgui + render backends) + `DearImGuiKSP.dll` (C#: KSP plugin, frame loop, input locks, public API).
- **Current status**: implementation plan complete (2026-09-03) — all milestones M1–M6 verified in-game, all gates PASS, session verdict CONTINUE. See `notes\finished\2026-07-29_DearImGuiKSP_PlanImplementation\FINAL_AUDIT.md`. Post-MVP follow-ups landed 2026-09-03 (`notes\finished\2026-09-03_Bug_ClickThrough_WindowAnchor\`): ISSUES #001 (uGUI click bleed-through) and #002 (resolution-change window clamp, handshake v4) resolved; xUnit Application test suite live; IMGUI click-through documented as KNOWNLIMIT #003. Next: styling/theming work (spec §3.4), release packaging (D15, user-deferred), OpenGL (backlog).

## Working on this repo — read these first

### Workflow (mandatory)

All work follows the FlyByWire workflow skill (v3) in:

- `~\source\repos\FlyByWire\versions\v3\README.md` — skill overview, template index, and shared rules. `SKILL.md` in that folder is the skill entry point (progressive loading sequence).
- **Start every task with `Router.md` in that folder** — it classifies the request and routes to the right template (BugfixPlanning, DesignSpecRefinement, ProjectBootstrap, etc.).
- `~\source\repos\FlyByWire\versions\v3\CORE_PROTOCOLS.md` — artifact taxonomy (`notes\active\`, `notes\knowledge\`, `notes\plans\`, `notes\indices\`), session naming, and shared engineering principles. All Markdown artifacts go in the taxonomy folders, never loose in `notes\`.
- v3 adds the Native Interop & Hot-Path Checklist (`versions\v3\reference\08-native-interop.md`), which applies to any chunk touching P/Invoke, native loading, or per-frame code.

The design of this project was produced by `DesignSpecRefinement.md`; bootstrap was produced by `ProjectBootstrap.md` (session `notes\finished\2026-07-29_NewProject_DearImGuiKSP\`: `PLANNING_WORKSHEET.md`, `IMPLEMENTATION_PLAN.md`, `PROJECT_SKELETON.md`, `GATES.md`). Implementation ran via `PlanImplementation.md` (session `notes\finished\2026-07-29_DearImGuiKSP_PlanImplementation\`), milestone by milestone; do not start milestone N until N-1 is verified.

## Repository Layout

- `DearImGuiKSP/` — managed library. `Application/` = Unity-free orchestration + public API (depends on Core only via interfaces); `Infrastructure/` = the **only** KSP/Unity-touching layer.
- `DearImGuiKSPNative/` — C++ Core (ImGui context, frame lifecycle, backends). Zero KSP/Unity knowledge. Sources come from the sibling clone `~\source\repos\cimgui` (pinned imgui 1.92.9), compiled in directly.
- `DearImGuiKSPDemo/` — demo/benchmark mod, separate install.
- `GameData/` — staging tree; mirrored into the game on every managed build.
- `tests/` — mirrors the layers (`Application.Tests` = real xUnit suite; Infrastructure/Core intentionally placeholders).
- `notes/` — artifact taxonomy (`active\`, `finished\`, `archive\`, `knowledge\`, `indices\`, `plans\`).
- `ISSUES/` — local issue tracker (gitignored). Schema and workflow in `ISSUES/README.md`; file new issues per its naming convention and keep `TRACKER.md` in sync.

## Build & Test Commands

- Managed: `dotnet build DearImGui-KSP.slnx` (Debug) / `-c Release`. Requires `DearImGui-KSP.props.user` pinning `KSPBT_GameRoot` (gitignored; each machine pins its own KSP test instance).
- Native: `cd DearImGuiKSPNative; build.bat` (debug) / `build_release.bat` (release) — plain `cl.exe`, no CMake/vcxproj.
- Test: `dotnet test DearImGui-KSP.slnx` (xUnit Application-layer suite; Infrastructure/Core deferred — see `tests/*/README.md`). In-game acceptance per milestone ACs (`notes\finished\2026-07-29_DearImGuiKSP_PlanImplementation\FINAL_AUDIT.md`).

### Design artifacts (this repo)

Authoritative design record, in `notes\finished\2026-07-29_DesignSpec_DearImGuiKSP_UI_Library\`:

| File | Contents |
|------|----------|
| `DESIGN_SPEC.md` | **The spec.** Confirmed by the user 2026-07-29. Build from this. |
| `DECISION_LOG.md` | D1–D22: every design decision, alternatives, rationale. Check before reversing anything. |
| `QUESTION_LOG.md` | Q1–Q47: the user's answers that the spec is built from. |
| `RESEARCH_NOTES.md` | KSP API findings, precedents, compatibility concerns, open gaps. |

### KSP Knowledge Library (external, read-only ground truth)

- `~\source\repos\TOOLS\KSP Knowledge Library\README.md` — how to use it.
- An ILSpy dump of KSP's `Assembly-CSharp.dll` with search indexes. **Check its `NOTES\` folder first** before researching any "how does the game do X" question — and record any new non-trivial finding there (with `file:line` citations) so it is never researched twice.
- Already captured from this project: `NOTES\assembly-loading-and-dependencies.md`, `NOTES\ui-rendering-and-input.md`, `NOTES\ugui-click-blocking-and-canvas-sorting.md`.

## Hard constraints (from the spec — do not regress)

- Compatibility with **Deferred** is a hard requirement; must also not break TUFX, Scatterer, Parallax, Cinematic Shaders, Cinematic Recorder, or IMGUI mods (D5, D16).
- Failure handling: unrecoverable startup failure → session-permanent self-disable, one plain-language `PopupDialog` at main menu, technical detail in the log under `[DearImGuiKSP]` only (§5.4, §7 of the spec).
- Library persists only its own global config (`GameData\DearImGuiKSP\settings.cfg`); consumer window state belongs to consumers.
- Versioning: SemVer, managed+native DLLs released in lockstep, consumers guided to `KSPAssemblyDependencyEqualMajor` (D17, spec §10.5).
- Demo/example mod ships as a separate install (`GameData\DearImGuiKSPDemo\`), never inside the dependency package (D7).
