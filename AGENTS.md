# AGENTS.md — DearImGui-KSP

## What this project is

**DearImGui-KSP** is a shared KSP mod library providing a modern, high-performance UI framework as a drop-in replacement for Unity IMGUI. Other mods hard-depend on it via `KSPAssemblyDependencyEqualMajor("DearImGuiKSP", x, y)`.

- **Target**: KSP 1.12.x, Unity 2019.4.18f1, Mono x64, Windows-first, D3D11 + OpenGL Core (shipped 1.1.0, D37).
- **Structure**: `DearImGuiKSPNative.dll` (C++: Dear ImGui + cimgui + render backends) + `DearImGuiKSP.dll` (C#: KSP plugin, frame loop, input locks, public API).
- **Current status**: **1.1.0 released** (2026-09-08) — minor release adding the OpenGL renderer backend (imgui_impl_opengl3, embedded loader; original-plan C5, D37 queue item 1; `-force-glcore` supported alongside D3D11, handshake v8). Session `notes\finished\2026-09-08_Feature_OpenGLBackend\` (all gates G1–G7 PASS, incl. user-verified in-game D3D11 regression + GL validation). Prior: 1.0.1 patch (Opus-review remediation wave, session `notes\finished\2026-09-07_Bug_OpusReviewTriage\` — read its HANDOFF.md for the wave record and post-release deferred backlog). `package_release.bat` builds both release zips into `dist\` (library + separate demo, D7) and archives the versioned native PDB into `dist\symbols\` (PDBs deliberately stay out of the zips — ~20 MB compressed vs the 2 MB library zip, and KSP never loads them; crash reports are symbolicated dev-side). D35 records OpenGL post-release (stripped-down GL-capable test install ready and pinned as the default build target — `notes\knowledge\ENVIRONMENT.md`); D36 records the versioning paradigm (see Hard constraints); D37 sequences post-release graphics-API work (OpenGL shipped 1.1.0 → #011 docking next → Metal/Mac track → Vulkan back-burner pending user demand). Release stepping-stone: the demo's `KSPAssemblyDependencyEqualMajor` attribute must be bumped with every library major (ISSUES #016). Earlier history: implementation plan complete 2026-09-03 (M1–M6 verified, `notes\finished\2026-07-29_DearImGuiKSP_PlanImplementation\FINAL_AUDIT.md`); post-MVP follow-ups (ISSUES #001–#003); wave spec `notes\finished\2026-09-03_DesignSpec_Theming_Extensions_Showcase\DESIGN_SPEC.md` (D24–D34).

## Working on this repo — read these first

### Workflow (mandatory)

All work follows the FlyByWire workflow skill (v3) in:

- `~\source\repos\FlyByWire\versions\v3\README.md` — skill overview, template index, and shared rules. `SKILL.md` in that folder is the skill entry point (progressive loading sequence).
- **Start every task with `Router.md` in that folder** — it classifies the request and routes to the right template (BugfixPlanning, DesignSpecRefinement, ProjectBootstrap, etc.).
- `~\source\repos\FlyByWire\versions\v3\CORE_PROTOCOLS.md` — artifact taxonomy (`notes\active\`, `notes\knowledge\`, `notes\plans\`, `notes\indices\`), session naming, and shared engineering principles. All Markdown artifacts go in the taxonomy folders, never loose in `notes\`.
- v3 adds the Native Interop & Hot-Path Checklist (`versions\v3\reference\08-native-interop.md`), which applies to any chunk touching P/Invoke, native loading, or per-frame code.

The design of this project was produced by `DesignSpecRefinement.md`; bootstrap was produced by `ProjectBootstrap.md` (session `notes\finished\2026-07-29_NewProject_DearImGuiKSP\`: `PLANNING_WORKSHEET.md`, `IMPLEMENTATION_PLAN.md`, `PROJECT_SKELETON.md`, `GATES.md`). Implementation ran via `PlanImplementation.md` (session `notes\finished\2026-07-29_DearImGuiKSP_PlanImplementation\`), milestone by milestone; do not start milestone N until N-1 is verified.

## Repository Layout

- `DearImGuiKSP/` — managed library. `Application/` = orchestration + public API (no KSP APIs or engine lifecycle/scene APIs; `UnityEngine.CoreModule` math structs allowed in public signatures, D24; widget calls reach native code via the P/Invoke bindings in `Interop/`, all other seams via `Application/Interfaces/`); `Infrastructure/` = the **only** KSP/Unity-touching layer.
- `DearImGuiKSPNative/` — C++ Core (ImGui context, frame lifecycle, backends). Zero KSP/Unity knowledge. Sources come from a sibling clone of cimgui checked out next to this repo's root (the build scripts reference `..\..\cimgui`; pinned imgui 1.92.9), compiled in directly.
- `DearImGuiKSPDemo/` — demo/benchmark mod, separate install.
- `GameData/` — staging tree; mirrored into the game on every managed build.
- `tests/` — mirrors the layers (`Application.Tests` = real xUnit suite; Infrastructure/Core intentionally placeholders).
- `notes/` — artifact taxonomy (`active\`, `finished\`, `knowledge\`, `indices\`, `plans\`).
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
| `DECISION_LOG.md` | D1–D37: every design decision, alternatives, rationale. Check before reversing anything. |
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
- Versioning: SemVer `major.minor.patch` (major = breaking/finalized, minor = features, patch = fixes; components are integers with trailing reset, e.g. 1.3.12 is valid), managed+native DLLs released in lockstep, consumers guided to `KSPAssemblyDependencyEqualMajor` (D17, D36, spec §10.5). First public release is **1.0.0** — the 1.0+ stability policy applies from day one.
- Demo/example mod ships as a separate install (`GameData\DearImGuiKSPDemo\`), never inside the dependency package (D7).
