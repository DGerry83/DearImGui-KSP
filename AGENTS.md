# AGENTS.md — Dear KSP

## What this project is

**Dear KSP** is a shared KSP mod library providing a modern, high-performance UI framework as a drop-in replacement for Unity IMGUI. Other mods hard-depend on it via `KSPAssemblyDependencyEqualMajor("DearKSP", x, y)`.

- **Target**: KSP 1.12.x, Unity 2019.4.18f1, Mono x64, Windows-first, D3D11 primary + OpenGL secondary.
- **Structure**: `DearKSPNative.dll` (C++: Dear ImGui + cimgui + render backends) + `DearKSP.dll` (C#: KSP plugin, frame loop, input locks, public API).
- **Current status**: bootstrap complete (2026-07-29). Confirmed spec + implementation plan exist; skeleton compiles; implementation proceeds milestone-by-milestone via `PlanImplementation.md`.

## Working on this repo — read these first

### Workflow (mandatory)

All work follows the meta-prompt workflow in:

- `C:\Users\Matt\source\repos\META-PROMPTS\TEMPLATES\README.md` — template index and shared rules.
- **Start every task with `Router.md` in that folder** — it classifies the request and routes to the right template (BugfixPlanning, DesignSpecRefinement, ProjectBootstrap, etc.).
- `C:\Users\Matt\source\repos\META-PROMPTS\TEMPLATES\CORE_PROTOCOLS.md` — artifact taxonomy (`notes\active\`, `notes\knowledge\`, `notes\plans\`, `notes\indices\`), session naming, and shared engineering principles. All Markdown artifacts go in the taxonomy folders, never loose in `notes\`.

The design of this project was produced by `DesignSpecRefinement.md`; bootstrap was produced by `ProjectBootstrap.md` (session `notes\active\2026-07-29_NewProject_DearKSP\`: `PLANNING_WORKSHEET.md`, `IMPLEMENTATION_PLAN.md`, `PROJECT_SKELETON.md`, `GATES.md`). Implementation proceeds via `PlanImplementation.md`, milestone by milestone; do not start milestone N until N-1 is verified.

## Repository Layout

- `DearKSP/` — managed library. `Application/` = Unity-free orchestration + public API (depends on Core only via interfaces); `Infrastructure/` = the **only** KSP/Unity-touching layer.
- `DearKSPNative/` — C++ Core (ImGui context, frame lifecycle, backends). Zero KSP/Unity knowledge. Sources come from the sibling clone `C:\Users\Matt\source\repos\cimgui` (pinned imgui 1.92.9), compiled in directly.
- `DearKSPDemo/` — demo/benchmark mod, separate install.
- `GameData/` — staging tree; mirrored into the game on every managed build.
- `tests/` — mirrors the layers (placeholders).
- `notes/` — artifact taxonomy (`active\`, `finished\`, `archive\`, `knowledge\`, `indices\`, `plans\`).

## Build & Test Commands

- Managed: `dotnet build DearKSP.slnx` (Debug) / `-c Release`. Requires `DearKSP.props.user` pinning `KSPBT_GameRoot` (gitignored; currently `C:\SSDGames\ReformTestInstance`).
- Native: `cd DearKSPNative; build.bat` (debug) / `build_release.bat` (release) — plain `cl.exe`, no CMake/vcxproj.
- Test: in-game acceptance per `IMPLEMENTATION_PLAN.md` §9 milestones; unit tests deferred to milestone 3+.

### Design artifacts (this repo)

Authoritative design record, in `notes\active\2026-07-29_DesignSpec_DearKSP_UI_Library\`:

| File | Contents |
|------|----------|
| `DESIGN_SPEC.md` | **The spec.** Confirmed by the user 2026-07-29. Build from this. |
| `DECISION_LOG.md` | D1–D18: every design decision, alternatives, rationale. Check before reversing anything. |
| `QUESTION_LOG.md` | Q1–Q47: the user's answers that the spec is built from. |
| `RESEARCH_NOTES.md` | KSP API findings, precedents, compatibility concerns, open gaps. |

### KSP Knowledge Library (external, read-only ground truth)

- `C:\Users\Matt\source\repos\TOOLS\KSP Knowledge Library\README.md` — how to use it.
- An ILSpy dump of KSP's `Assembly-CSharp.dll` with search indexes. **Check its `NOTES\` folder first** before researching any "how does the game do X" question — and record any new non-trivial finding there (with `file:line` citations) so it is never researched twice.
- Already captured from this project: `NOTES\assembly-loading-and-dependencies.md`, `NOTES\ui-rendering-and-input.md`.

## Hard constraints (from the spec — do not regress)

- Compatibility with **Deferred** is a hard requirement; must also not break TUFX, Scatterer, Parallax, Cinematic Shaders, Cinematic Recorder, or IMGUI mods (D5, D16).
- Failure handling: unrecoverable startup failure → session-permanent self-disable, one plain-language `PopupDialog` at main menu, technical detail in the log under `[DearKSP]` only (§5.4, §7 of the spec).
- Library persists only its own global config (`GameData\DearKSP\settings.cfg`); consumer window state belongs to consumers.
- Versioning: SemVer, managed+native DLLs released in lockstep, consumers guided to `KSPAssemblyDependencyEqualMajor` (D17, spec §10.5).
- Demo/example mod ships as a separate install (`GameData\DearKSPDemo\`), never inside the dependency package (D7).
