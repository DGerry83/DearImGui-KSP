# Handoff: DearImGui-KSP Implementation

## Date: 2026-09-03 (plan implementation COMPLETE — Phases 0–5 done, session verdict CONTINUE)
## Resume with: nothing in this workflow — the plan is finished. Next work is post-plan: release packaging (D15), ISSUES #001/#002, deferred backlog (C5 OpenGL, unit tests, spec §3.4 items). Read FINAL_AUDIT.md in this folder for the full picture.

---

## 1. What this project is

**DearImGui-KSP** — a shared KSP 1.12.x mod library: modern, high-performance UI framework replacing Unity IMGUI for mods. Native C++ core (Dear ImGui 1.92.9 + cimgui, D3D11 backend, Unity render-thread injection) + managed C# wrapper (frame loop, consumer API, input locks, settings, lifecycle). Repo: `~\source\repos\DearImGui-KSP` (GitHub: `DGerry83/DearImGui-KSP`, private until release).

**Confirmed design spec**: `notes/finished/2026-07-29_DesignSpec_DearImGuiKSP_UI_Library/DESIGN_SPEC.md` (with `DECISION_LOG.md` D1–D20, `QUESTION_LOG.md` Q1–Q47, `RESEARCH_NOTES.md`). Read `AGENTS.md` at the repo root first — it encodes the workflow and hard constraints. A current-status snapshot lives at `notes/finished/2026-08-31_Status_DearImGuiKSP/STATUS_AND_NEXT_STEPS.md`.

## 2. Where we are in the workflow

Executing `PlanImplementation.md` (from `~\source\repos\FlyByWire\versions\v3\` — the current skill version; earlier chunks ran under v2). Session folder: `notes/finished/2026-07-29_DearImGuiKSP_PlanImplementation/` — contains `PLAN_DIGEST.md`, `CHUNK_MAP.md`, `INTEGRATION_CONTRACT.md`, `GATES.md` (frozen), `PROGRESS_LOG.md`, and per-chunk contracts (CHUNK_1…4, 6, 7, 8, 9, 10, 11, 12).

**Phase 3 (chunk execution) is complete.** All chunks C1–C15 done and verified in-game (C5 deferred per D20); Phases 4–5 closed 2026-09-03 with INTEGRATION_REPORT.md and FINAL_AUDIT.md; all gates G1–G6 PASS (AC2 deferred). Historical chunk status:

| Chunk | Result |
|-------|--------|
| C1 (M1: logger/composition/log line) | ✅ verified in-game |
| C2 (Unity export surface) | ✅ |
| C3 (imgui/cimgui + context host + atlas + harness) | ✅ harness PASS |
| C4 (D3D11 PoC — the kill-or-continue gate) | ✅ **AC1 PASS** in-game, Deferred+TUFX fine |
| C5 (OpenGL) | ⏸️ **DEFERRED (D20)** — needs a clean GL test environment; MVP is D3D11-only. Revisit before M6/C15 |
| C6 (cimgui interop, MVP widgets) | ✅ |
| C7 (public API + registry + frame loop) | ✅ |
| C8 (demo consumer + load-order proof) | ✅ **AC3 + AC4 PASS** in-game; includes ApplicationLauncher toolbar toggle (green 38×38 placeholder icon) |
| C9 (input capture + locks, incl. C9b input feeding) | ✅ **AC7 PASS** in-game (window fully interactive, camera locks on hover) |
| C10 (fault barrier) | ✅ **AC9 PASS** in-game (5-strike auto-disable verified; temp fault probe removed) |
| C11 (settings store/model/migration) | ✅ **AC12 PASS** in-game (after ConfigNode wrapper-node fix) |
| C12 (lifecycle state machine + game-event hooks) | ✅ **AC10 PASS** in-game (F2 suspend/resume, loading screens, resolution change) |
| **C13 (failure notifier + failure-mode tests, AC8) — NEXT** | ⬜ write CHUNK_13_CONTRACT.md, implement, user verifies in-game |
| C14 (torture-test benchmark, AC5/AC6) | ⬜ |
| C15 (full compatibility validation, AC11) | ⬜ |
| Phase 4 (stub removal/integration, G4), Phase 5 (final audit, G5) | ⬜ |

Milestones: **M1, M2, M3, M4 complete.** M5 needs only C13. M6 = C14+C15.

## 3. How each chunk runs (the established rhythm)

1. Lead writes `CHUNK_N_CONTRACT.md` in the session folder (scope, inputs, outputs, locked surfaces, exclusive file ownership, verification, rollback).
2. Implementation is delegated to a coder sub-agent with a detailed prompt (read contract + relevant files first, mandatory disagreement check — agents proceed when no disagreements, no git commits, raw-results report ending `STATUS: PASS|FAIL|INVALID`). Small chunks may be done directly by the lead.
3. **Lead owns the shared wiring files** — `Composition.cs`, `DearImGuiKSPAddon.cs`, `FrameLoopOrchestrator.cs` — so parallel agents never collide. Agents get exclusive ownership of their unit files only.
4. Lead spot-reviews key files (diffs), builds, updates `PROGRESS_LOG.md`, commits with message `CN: …` and pushes.
5. In-game verification (when a chunk requires it) is done by **the user**: they launch KSP (`the pinned KSP test instance`) and report what they see; the lead diagnoses from `KSP.log` (instance root) and `Player.log` (`%LOCALAPPDATA%\LocalLow\Squad\Kerbal Space Program`).
6. Never touch the KSP install beyond files this project deploys.

## 4. Build & deploy (all verified working)

```bash
cd DearImGuiKSPNative && cmd //c build.bat      # native DLL -> GameData/DearImGuiKSP/PluginData/
dotnet build DearImGui-KSP.slnx                  # managed; KSPBuildTools stages GameData/ and mirrors into ReformTestInstance
cd DearImGuiKSPNative && cmd //c build_harness.bat && build/harness.exe   # headless native smoke test, expect HARNESS PASS
```

- KSP install pin: `DearImGui-KSP.props.user` (gitignored) → `the pinned KSP test instance`.
- **Every managed build re-mirrors the repo's `GameData/DearImGuiKSP/settings.cfg` (defaults) over the instance copy** — re-apply test edits (e.g. `enabled = false`) after any build.
- cimgui/imgui sources: sibling clone `~\source\repos\cimgui` (imgui 1.92.9 pinned by submodule — **never update casually**).
- Layout rule (D19): managed DLLs → `GameData/DearImGuiKSP/Plugins/`, native DLL → `GameData/DearImGuiKSP/PluginData/` (native DLLs in the assembly scan path hang the game loader).

## 5. Hard-won technical facts (do not relearn)

- **Unity never calls `UnityPluginLoad` for a `LoadLibrary`'d plugin.** The D3D11 device is captured from a Unity-created `Texture2D.GetNativeTexturePtr()` → `texture->GetDevice()`. Device gate is managed-side: `SystemInfo.graphicsDeviceType`.
- **imgui 1.92.9 texture protocol**: set `ImGuiBackendFlags_RendererHasTextures` in `ContextInit` before the first `NewFrame`; never call `ImFontAtlas::Build()` — the backend uploads the font texture from `draw_data->Textures`.
- **Render flow**: addon `Update()` → `Orchestrator.RunFrame` → capture sample → locks → `BeginUiFrame` → consumer callbacks (via FaultBarrier) → `EndUiFrame`; a `WaitForEndOfFrame` coroutine issues `GL.IssuePluginEvent(RenderEventFunc, 0)`. Backend renders into whatever RT is bound at event time with full D3D11 state save/restore (Deferred/TUFX coexistence basis). Draw data produced on game thread, consumed on render thread — documented caveat, acceptable so far.
- **Input feeding (C9b)**: ImGui's IO must be fed every frame via `DearImGuiKSPNative_FeedFrameInput` (mouse pos Y-flipped, buttons 0–2, wheel, 13-key bit list, filtered UTF-8 chars) called by `NativeBridge.BeginUiFrame` **before** `BeginFrame`/`NewFrame`. Without it windows render but are inert and WantCapture* stay false.
- **Capture state is sampled pre-BeginUiFrame** (previous frame's IO) — spec §5.3 order.
- **cimgui interop**: variadic functions can't be P/Invoked — use `igTextUnformatted`; cimgui `bool` = `UnmanagedType.I1`; strings are null-terminated UTF-8 `byte[]`; implicit `[DllImport("DearImGuiKSPNative")]` resolves because the bridge `LoadLibrary`s first. The whole cimgui C API is exported — future widget bindings need no native changes.
- **ConfigNode file layout**: `ConfigNode.Load` returns a synthetic wrapper node named `root` (real node is a child — `root.GetNode("DEARIMGUIKSP_SETTINGS")`); `ConfigNode.Save` omits the node's own name/braces (save a wrapper with the named child). Knowledge Library: `NOTES/config-node-persistence.md`.
- **ApplicationLauncher toolbar**: verified pattern in `NOTES/ui-rendering-and-input.md` (AddModApplication signature, Ready check + onGUIApplicationLauncherReady, SetTrue(false), 38×38 icon).
- **Managed/native handshake**: `DearImGuiKSPNative_GetVersion()` currently returns **3** (v2 = GetIoCaptureState, v3 = FeedFrameInput); `ExpectedNativeVersion` in NativeBridge must match. Bump both in lockstep on any native surface change (invariant 4).

## 6. Locked contracts (do not break)

- **Public API (additive-only, invariant 2)**: `DearImGuiKSP.IsAvailable`, `Register(id, Action)`, `Unregister(id)`, `BeginWindow/EndWindow`, `Text`, `Button`, `SliderFloat`, `InputText` — in `DearImGuiKSP/Application/DearImGuiKSP.cs`.
  - `IsAvailable` = Running **or Suspended** (F2/loading is a pause — consumers must not tear down). False while Uninitialized/Initializing/Failed.
- **Native C ABI**: context host exports (init/shutdown/BeginFrame/EndFrame/GetFontAtlasPixels), device handoff `SetD3D11DeviceTexture`, `GetVersion`/`GetRenderEventFunc`, `GetIoCaptureState`, `FeedFrameInput`.
- **INativeBridge** (internal): `Initialize`, `GetIoSnapshot`, `BeginUiFrame`, `EndUiFrame`, `RebuildViewport` (documented no-op: per-frame DisplaySize, RT-bound backend, resolution-independent atlas), `Shutdown`.
- **IGameEventSource** (internal): `UiVisibilityChanged(bool)`, `LoadingChanged(bool)`, `ResolutionChanged(int,int)`, `Subscribe/Unsubscribe`.
- **Settings seam**: `SettingsModel` (values + clamping + `Changed` event + persist-on-change) over `ISettingsStore` (`Load`/`Save(LibrarySettings)`); `ILogger.Debug` gated by `SettingsModel.VerboseLogging` via provider.
- **Layer rules**: `DearImGuiKSP/Application/` + `DearImGuiKSP/Interop/` are Unity-free; only `DearImGuiKSP/Infrastructure/` touches KSP/Unity; native Core has zero game knowledge. Application↔Infrastructure wiring goes through Composition-assigned hooks (`DearImGuiKSP.Log`, `.Registry`, `.Lifecycle`).
- Five plan invariants in `PLAN_DIGEST.md`; frozen gates in `GATES.md` (G6 tracks AC1–AC12; AC1, AC3, AC4, AC7, AC9, AC10, AC12 PASS; AC2 deferred).

## 7. C13 brief (the next chunk)

Goal: failure notifier + failure-mode tests — **AC8**: each failure mode (missing/corrupt native DLL, version mismatch, unsupported graphics API, render-hook failure) → Failed state + one plain-language `PopupDialog` at main menu + `IsAvailable == false`.

- Skeletons awaiting implementation: `Infrastructure/FailureNotifier.cs`, `Application/Interfaces/IFailureNotifier.cs`; `NativeBridge.cs` handshake paths are listed as C13 files in CHUNK_MAP.
- String table is spec §7.1 (DK_FailTitle + three body strings, DK_LogPrefix); voice rules §7.3 (plain language, no stack traces; technical detail log-only under `[DearImGuiKSP]`). Failure UX: spec §5.4 and hard constraint in AGENTS.md.
- C12 already routes failures to `StateMachine.Fail(reason)` (log + terminal state); C13 adds the popup (once per session, at main menu) on entering Failed.
- Verification (AC8) needs each failure mode exercised in-game — likely by sabotaging the deployed instance (rename native DLL, edit version, `-force-glcore`) and restoring after; coordinate with the user.
- Remember INTEGRATION_CONTRACT: C13 is a Consumer-type chunk, Low complexity, depends on C12's Failed state.

## 8. Open follow-ups (ISSUES/ tracker, gitignored — see `ISSUES/TRACKER.md`)

- **#001 (P2)**: mouse clicks bleed through ImGui windows to KSP uGUI UI below. Root cause understood (ImGui isn't uGUI; EventSystem never sees the windows). Fix needs a research spike (raycast blocker panel vs. documented limitation). From C9b verification.
- **#002 (P3)**: window anchored top-left through resolution change; can end nearly off-screen (recoverable via visible corner). Note tension with spec §6.2 (window positions are consumer state) before implementing a library-side clamp.
- **C5 (OpenGL)** remains deferred per D20 — revisit before C15 compatibility validation.
- Phase 4 gate G4: integration build. PoC scaffolding already removed in C9; no other tracked stubs remain (INTEGRATION_CONTRACT stub table is all-closed).

## 9. Housekeeping notes

- Staged binaries under `GameData/**` are gitignored (kept out of commits since b5a9b37).
- `ISSUES/` is gitignored by design (evidence never touches history); schema in `ISSUES/README.md`, next ID #003.
- Environment cache: `notes/knowledge/ENVIRONMENT.md` (created during C11 onboarding).
- Commit cadence so far: one commit per chunk + one per in-game verification.
- Untracked `VisualReferenceMaterial/` folder exists at repo root — not created by this workflow; left alone.
- Knowledge Library rule: any new non-trivial "how does KSP do X" finding gets recorded in `~\source\repos\TOOLS\KSP Knowledge Library\NOTES\` with file:line citations (four notes so far: assembly-loading, ui-rendering-and-input, config-node-persistence, plus the D19 native-DLL finding).
