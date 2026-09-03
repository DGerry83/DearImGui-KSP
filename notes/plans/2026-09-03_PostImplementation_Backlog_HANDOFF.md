# Handoff: DearImGui-KSP — Post-Implementation Planning Session

## Date: 2026-09-03
## Purpose of the next session: create a **detailed plan** (not yet implementation) for the ordered post-MVP backlog below. The user will provide additional details in-session, especially for the styling work.

---

## 1. Where the project stands

**DearImGui-KSP implementation plan is COMPLETE** (all milestones M1–M6, gates G1–G6 PASS, session verdict CONTINUE, 2026-09-03). The library works in-game in the full D16 mod environment on D3D11.

Read first, in this order:

1. `AGENTS.md` (repo root) — workflow, layout, hard constraints, current build/test commands. **Start the session with the FlyByWire `Router.md`** per the mandatory workflow.
2. `notes/finished/2026-07-29_DearImGuiKSP_PlanImplementation/FINAL_AUDIT.md` — the full implementation record: coverage table, invariants, deviations, and the known-limitations list this backlog comes from.
3. `notes/finished/2026-07-29_DesignSpec_DearImGuiKSP_UI_Library/DESIGN_SPEC.md` (+ `DECISION_LOG.md` D1–D20) — the confirmed spec. Check it before designing anything; several backlog items touch spec-deferred areas (§3.4).
4. `ISSUES/TRACKER.md` + the two issue folders (gitignored, local-only) — the actual issue write-ups.

## 2. The ordered backlog (user-set priority, 2026-09-03)

Tackle in this order; OpenGL only after everything else:

1. **ISSUES #001 (P2) — mouse clicks bleed through ImGui windows to KSP uGUI below.** Root cause understood: ImGui isn't uGUI, so KSP's EventSystem never sees the windows. Needs a research spike first (candidate approaches: invisible uGUI raycast-blocker panel synced to window rects vs. documented limitation). The demo mod's benchmark window + IMGUI reference window make a good repro fixture. See `ISSUES/VANILLA-[DearImGuiKSP]-LOCAL-2026-08-31/`.
2. **ISSUES #002 (P3) — windows anchored top-left through resolution changes can end nearly off-screen** (recoverable via a visible corner). **Tension with spec §6.2**: window positions are consumer state — a library-side clamp may be out of bounds; the plan must resolve that design question before any implementation. See `ISSUES/VANILLA-[DearImGuiKSP]_WindowAnchor-LOCAL-2026-08-31/`.
3. **Unit tests** — external review 2026-09-03 finding 5, originally deferred by plan decision; user now wants them. The `tests/` tree exists as placeholders (`Application.Tests`, `Core.Tests`, `Infrastructure.Tests` with READMEs stating intent). The Application layer is Unity-free by design (`LifecycleStateMachine`, `ConsumerRegistry`, `FaultBarrier`, `SettingsModel` are pure C#). `SettingsStore` ConfigNode parsing needs KSP assembly references — the C11 wrapper-node defect is the class of regression to catch. All types are `internal` — plan for `InternalsVisibleTo` or similar. Framework choice was explicitly deferred ("plain xUnit/NUnit works" per the test READMEs).
4. **Styling/theming work** — user-driven, details to come in-session. Relates to the local-only `VisualReferenceMaterial/` folder at the repo root (**gitignored, not in history** — read it from disk: `ImGui_Extension_Project_Links.md`, `KSP_Theme_Palette_Notes.md`, `UIStylingRef.png`). Spec §3.4 already defers: KSP-flavored theme, additional themes, styling presets, built-in settings window (theme picker, UI scale), consumer font loading, HiDPI polish. Expect the plan to pull some subset of these forward.
5. **OpenGL backend (C5/AC2, deferred per D20)** — only after 1–4. Needs a clean GL test environment (CinematicShaders/CinematicRecorder fail under GL, so the main test instance is unactionable for this). The GL backend design (imgui_impl_opengl3, embedded loader, 1.92.9 texture protocol) is documented and ready in the design session's research notes.

**Deferred by the user**: release packaging (D15) — do not plan it now.

## 3. Hard constraints that still bind any new plan

- Public API is additive-only within a major version (invariant 2, D17); current surface in `DearImGuiKSP/Application/DearImGuiKSP.cs`.
- Layer rules: `Application/` + `Interop/` stay Unity-free; only `Infrastructure/` touches KSP/Unity; native has zero game knowledge.
- Managed/native handshake lockstep (currently v3); bump both sides on any native surface change. The full cimgui API is already exported — new widget bindings need no native changes.
- Compatibility with Deferred (hard), TUFX, Scatterer, Parallax, Cinematic Shaders, Cinematic Recorder, IMGUI mods (D5/D16) — regression checks belong in every plan.
- Failure UX rules (spec §5.4/§7) and the locked contracts in the implementation session's `INTEGRATION_CONTRACT.md` §"Locked" remain in force.
- `notes/` taxonomy: new session artifacts go in `notes/active/YYYY-MM-DD_<SessionName>/`; completed sessions move to `notes/finished/`.
- Never commit `ISSUES/` or `VisualReferenceMaterial/` (both gitignored by design).

## 4. Build & verify quick reference

```bash
cd DearImGuiKSPNative && cmd //c build.bat      # native debug DLL
dotnet build DearImGui-KSP.slnx                  # managed; mirrors GameData/ into the pinned test instance
cd DearImGuiKSPNative && cmd //c build_harness.bat && build/harness.exe   # native smoke test, expect HARNESS PASS
```

- The KSP test instance is pinned via the gitignored `DearImGui-KSP.props.user` (`KSPBT_GameRoot`).
- Every managed build re-mirrors the repo's default `GameData/DearImGuiKSP/settings.cfg` over the instance copy — re-apply test edits after any build.
- In-game verification is done by the user; diagnose from `KSP.log` (instance root) and `Player.log` (`%LOCALAPPDATA%\LocalLow\Squad\Kerbal Space Program`).

## 5. Known accepted gaps (don't re-litigate unless the plan touches them)

- `INativeBridge.Shutdown` is never called (process-lifetime addon; deliberate no-op — see INTEGRATION_REPORT.md).
- Shift/Alt modifiers are not fed to ImGui (documented in spec §3.2); Ctrl works.
- Draw-data tearing caveat (game thread produces, render thread consumes) — accepted with a documented double-buffering plan.
- Demo toolbar icon is a green placeholder (intentional).
