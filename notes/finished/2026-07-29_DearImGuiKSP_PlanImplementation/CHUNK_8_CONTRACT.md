# Chunk Contract: Demo Example Window + Load-Order Proof

## Plan: DearImGui-KSP Implementation
## Date: 2026-08-31
## Chunk ID: C8
## Advances Milestone: M3

### Scope

- First real consumer of the C7 public API: `DearImGuiKSPDemo/DemoConsumer.cs` renders an example window exercising every MVP widget — text, button (with click feedback), slider bound to a field, input field bound to a string.
- Proves **AC3** (all MVP widgets via the C# API, zero Unity IMGUI) and **AC4** (`KSPAssemblyDependencyEqualMajor` load order) in-game.
- Updates the class XML doc: remove the `TODO(milestone 3)` line (now done); keep the milestone-6 benchmark TODO.

### Inputs (must exist before starting)

- C7: public facade locked — `DearImGuiKSP.IsAvailable`, `Register(id, Action)`, `Unregister(id)`, `BeginWindow/EndWindow`, `Text`, `Button`, `SliderFloat`, `InputText` (`DearImGuiKSP/Application/DearImGuiKSP.cs`).
- C1–C4: working build/deploy + render path (AC1 PASS in-game).
- `DearImGuiKSPDemo/Properties/AssemblyInfo.cs` already declares `[assembly: KSPAssemblyDependencyEqualMajor("DearImGuiKSP", 0, 1)]` — unchanged by this chunk.
- Demo ships as a separate install (`GameData/DearImGuiKSPDemo/`, D7); KSPBuildTools mirrors it on build.

### Outputs (must be created/changed)

- Modified: `DearImGuiKSPDemo/DemoConsumer.cs`:
  - `Start()`: check `DearImGuiKSP.IsAvailable`; if false, log `"[DearImGuiKSPDemo] DearImGui-KSP not available; demo disabled."` and bail. Else `DearImGuiKSP.Register("DearImGuiKSPDemo", OnFrame)` and log a registered line.
  - `OnFrame()` (the registered callback): `BeginWindow("DearImGui-KSP Demo")` → `Text`, `Button` (click increments a counter shown via `Text`), `SliderFloat` bound to a private float field, `InputText` bound to a private string field → `EndWindow()` (unconditionally, per the API contract).
  - `OnDestroy()`: `DearImGuiKSP.Unregister("DearImGuiKSPDemo")`.
  - Replace the skeleton `Awake()` log line; no other MonoBehaviour events needed.
- Nothing else. No changes to the library, native code, csproj, or GameData.

### Constraints

- **Zero Unity IMGUI** (AC3): no `OnGUI`, no `GUILayout`, no `GUI.` calls anywhere in the demo.
- **Additive-only public API (invariant 2)**: consume the C7 surface exactly as locked; if the API proves insufficient, STOP with IMPEDIMENTS.md — do not modify the library from this chunk.
- net48 / C# language level per existing project (no newer language features).
- Widget calls only inside the registered callback — this is the locked frame-loop rule; the demo must model correct usage for consumers.
- Window will be **non-interactive** until C9 (input locks) — expected, not a defect. Do not add input handling here.
- No git commits — the lead commits after review (established rhythm, HANDOFF §3).

### Verification

- `dotnet build DearImGui-KSP.slnx` — 0 errors, 0 warnings (gate G3).
- Native untouched (no rebuild needed).
- **User verifies in-game** (ReformTestInstance, D16 environment):
  - AC3: demo window visible in every scene (`KSPAddon.Startup.EveryScene`), showing text, a button with click feedback, a slider, and an input field. Widgets render correctly; window not yet interactive (C9 pending).
  - AC4: KSP.log shows the demo loading after the library, with no `KSPAssemblyDependency` warnings from the loader.
  - Library init lines (`[DearImGuiKSP]`) unchanged from C7.

### Rollback

- `git checkout -- DearImGuiKSPDemo/DemoConsumer.cs`
