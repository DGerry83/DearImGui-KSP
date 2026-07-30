# Handoff: Dear KSP Implementation

## Date: 2026-07-29 (end of session)
## Resume with: chunk C8 of the PlanImplementation workflow

---

## 1. What this project is

**Dear KSP** — a shared KSP 1.12.x mod library: modern, high-performance UI framework replacing Unity IMGUI for mods. Native C++ core (Dear ImGui 1.92.9 + cimgui, D3D11 backend, Unity render-thread injection) + managed C# wrapper (frame loop, consumer API, input locks, settings). Repo: `C:\Users\Matt\source\repos\Dear_KSP` (GitHub: `DGerry83/Dear_KSP`, private until release).

**Confirmed design spec**: `notes/active/2026-07-29_DesignSpec_DearKSP_UI_Library/DESIGN_SPEC.md` (with `DECISION_LOG.md` D1–D20, `QUESTION_LOG.md` Q1–Q47, `RESEARCH_NOTES.md`). Read `AGENTS.md` at the repo root first — it encodes the workflow and hard constraints.

## 2. Where we are in the workflow

Executing `PlanImplementation.md` (from `C:\Users\Matt\source\repos\META-PROMPTS\TEMPLATES\`). Session folder: `notes/active/2026-07-29_DearKSP_PlanImplementation/` — contains `PLAN_DIGEST.md`, `CHUNK_MAP.md`, `INTEGRATION_CONTRACT.md`, `GATES.md` (frozen), `PROGRESS_LOG.md`, and per-chunk contracts.

**Phase 3 (chunk execution) is underway.** Status:

| Chunk | Result |
|-------|--------|
| C1 (M1: logger/composition/log line) | ✅ verified in-game |
| C2 (Unity export surface) | ✅ |
| C3 (imgui/cimgui + context host + atlas + harness) | ✅ harness PASS |
| C4 (D3D11 PoC — the kill-or-continue gate) | ✅ **AC1 PASS in-game**: demo window renders, full fps, Deferred+TUFX fine |
| C5 (OpenGL) | ⏸️ **DEFERRED (D20)** — needs a clean GL test environment; MVP is D3D11-only |
| C6 (cimgui interop, MVP widgets) | ✅ |
| C7 (public API + registry + frame loop) | ✅ |
| **C8 (demo consumer + load-order proof) — NEXT** | ⬜ write CHUNK_8_CONTRACT.md, implement, user verifies in-game (AC3, AC4) |
| C9–C11 (parallel group A: input locks, fault barrier, settings) | ⬜ |
| C12–C13 (lifecycle/state machine, failure popup) | ⬜ |
| C14–C15 (torture-test benchmark, full compat validation) | ⬜ |
| Phase 4 (stub removal/integration), Phase 5 (final audit) | ⬜ |

## 3. How each chunk runs (the established rhythm)

1. Lead writes `CHUNK_N_CONTRACT.md` in the session folder (scope, inputs, outputs, locked surfaces, verification, rollback).
2. Implementation is delegated to a coder sub-agent with a detailed prompt (read contract + relevant files first, mandatory disagreement check, no git commits, raw-results report ending `STATUS: PASS|FAIL|INVALID`). Small chunks may be done directly by the lead.
3. Lead spot-reviews key files, fixes nits, updates `PROGRESS_LOG.md`, commits with message `CN: …` and pushes.
4. In-game verification (when a chunk requires it) is done by **the user**: they launch KSP (`C:\SSDGames\ReformTestInstance`) and report what they see; the lead diagnoses from `KSP.log` (instance root) and `Player.log` (`%LOCALAPPDATA%LocalLow\Squad\Kerbal Space Program`).
5. Never touch the KSP install beyond files this project deploys.

## 4. Build & deploy (both verified working)

```bash
cd DearKSPNative && cmd //c build.bat      # native DLL -> GameData/DearKSP/PluginData/
dotnet build DearKSP.slnx                   # managed; KSPBuildTools stages GameData/ and mirrors into ReformTestInstance
cd DearKSPNative && cmd //c build_harness.bat && build/harness.exe   # headless native smoke test, expect HARNESS PASS
```

- KSP install pin: `DearKSP.props.user` (gitignored) → `C:\SSDGames\ReformTestInstance`.
- cimgui/imgui sources: sibling clone `C:\Users\Matt\source\repos\cimgui` (imgui 1.92.9 pinned by submodule — **never update casually**).
- Layout rule (D19): managed DLLs → `GameData/DearKSP/Plugins/`, native DLL → `GameData/DearKSP/PluginData/` (native DLLs in the assembly scan path hang the game loader).

## 5. Hard-won technical facts (do not relearn)

- **Unity never calls `UnityPluginLoad` for a `LoadLibrary`'d plugin.** The D3D11 device is captured from a Unity-created `Texture2D.GetNativeTexturePtr()` → `texture->GetDevice()` (CinematicRecorderNative's pattern). Device gate is managed-side: `SystemInfo.graphicsDeviceType`.
- **imgui 1.92.9 texture protocol**: set `ImGuiBackendFlags_RendererHasTextures` in `ContextInit` before the first `NewFrame`; never call `ImFontAtlas::Build()` — the backend uploads the font texture from `draw_data->Textures`. Violating this = the exact assert in `imgui_draw.cpp:2820`.
- **Render flow**: addon `Update()` → `Orchestrator.RunFrame` → `BeginUiFrame` → consumer callbacks → `EndUiFrame`; a `WaitForEndOfFrame` coroutine issues `GL.IssuePluginEvent(RenderEventFunc, 0)`; the backend renders into whatever RT is bound at event time, with imgui's full D3D11 state save/restore (the Deferred/TUFX coexistence basis). Draw data produced on game thread, consumed on render thread — documented caveat, acceptable so far.
- **cimgui interop**: variadic functions (`igText`) can't be P/Invoked — use `igTextUnformatted`; cimgui `bool` = `UnmanagedType.I1`; strings are null-terminated UTF-8 `byte[]`; implicit `[DllImport("DearKSPNative")]` resolves because the bridge `LoadLibrary`s first.
- The whole cimgui C API is exported from our DLL — future bindings need no native changes.

## 6. Locked contracts (do not break)

- **Public API (additive-only, invariant 2)**: `DearKSP.IsAvailable`, `Register(id, Action)`, `Unregister(id)`, `BeginWindow/EndWindow`, `Text`, `Button`, `SliderFloat`, `InputText` — in `DearKSP/Application/DearKSP.cs`.
- **Native C ABI**: context host six exports (C3) + device handoff `DearKSPNative_SetD3D11DeviceTexture` + `GetVersion`/`GetRenderEventFunc` (C4).
- **INativeBridge** (internal): `Initialize`, `GetIoSnapshot`, `BeginUiFrame`, `EndUiFrame`, `RebuildViewport`, `Shutdown` (C7 amendment split SubmitFrame — documented).
- **Layer rules**: `DearKSP/Application/` + `DearKSP/Interop/` are Unity-free; only `DearKSP/Infrastructure/` touches KSP/Unity; native Core has zero game knowledge. Application↔Infrastructure wiring goes through Composition-assigned hooks (`DearKSP.Log`, `DearKSP.Registry`, `SetAvailable`).
- Five plan invariants in `PLAN_DIGEST.md`; frozen gates in `GATES.md` (G6 tracks AC1–AC12; AC1 PASS, AC2 deferred).

## 7. C8 brief (the next chunk)

Goal: prove the consumer model end-to-end — AC3 (demo window with all MVP widgets via the C# API, zero IMGUI) and AC4 (EqualMajor load order).

- `DearKSPDemo/DemoConsumer.cs` already: `KSPAddon(EveryScene, false)`, logs a skeleton line; its `Properties/AssemblyInfo.cs` declares `KSPAssemblyDependencyEqualMajor("DearKSP", 0, 1)`.
- It should: check `DearKSP.IsAvailable` in `Start()` (log + bail if false), `Register("DearKSPDemo", OnFrame)`, and in the callback render a window using every MVP widget (text, button with click feedback, slider bound to a field, input field bound to a string), `Unregister` in `OnDestroy`.
- Verification: user launches KSP → demo window visible and correct in every scene; KSP.log shows demo loading after the library (loader's topological sort handles order — just confirm no dependency warnings). Window is still non-interactive until C9 (input locks) — set that expectation with the user.
- C8 is small enough to implement directly or delegate; either is consistent with precedent.

## 8. Tracked stubs awaiting later chunks (INTEGRATION_CONTRACT.md)

- `VerboseLoggingStub = true` in `DearKSPLogger` → C11 (settings).
- `GetIoSnapshot` returns defaults → C9; per-consumer try/catch placeholder → C10; `RebuildViewport` no-op → C12.
- Failure popup + `Failed` state → C13. `IsAvailable` is currently a simple flag; C12 gates it behind the state machine.

## 9. Housekeeping notes

- Staged binaries under `GameData/**` are gitignored (kept out of commits since b5a9b37).
- Commit cadence so far: one commit per chunk + one per in-game verification.
- Knowledge Library rule: any new non-trivial "how does KSP do X" finding gets recorded in `C:\Users\Matt\source\repos\TOOLS\KSP Knowledge Library\NOTES\` with file:line citations (two notes already exist from this project, plus the D19 native-DLL finding appended).
