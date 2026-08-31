# Chunk Contract: Consumer API — Facade, Registry, Frame Loop

## Plan: DearImGui-KSP Implementation
## Date: 2026-07-29
## Chunk ID: C7
## Advances Milestone: M3

### Scope

- The **public consumer API**: `DearImGuiKSP.IsAvailable`, `DearImGuiKSP.Register(id, callback)`, `DearImGuiKSP.Unregister(id)`, and public widget methods delegating to the C6 interop surface.
- `ConsumerRegistry` (Application): ordered registration with per-consumer fault state fields.
- `FrameLoopOrchestrator` (Application): the real per-frame sequence — native BeginFrame → consumer callbacks in registration order → native EndFrame — driven by the addon.
- PoC scaffold removal: the `_setDemoWindowVisible(1)` line marked `POC-SCAFFOLD(C7)` in `NativeBridge.Initialize()`, and the hard-coded `IsAvailable => false` stub.

### Inputs (must exist before starting)

- C4: working bridge + render path (AC1 PASS).
- C6: `DearImGuiKSP.Interop.ImGuiInternal` safe widget surface.

### Outputs (must be created/changed)

- Modified: `DearImGuiKSP/Application/DearImGuiKSP.cs` — real facade (public).
- Modified: `DearImGuiKSP/Application/ConsumerRegistry.cs` — real implementation.
- Modified: `DearImGuiKSP/Application/FrameLoopOrchestrator.cs` — real implementation.
- Modified: `DearImGuiKSP/Application/Interfaces/INativeBridge.cs` — `SubmitFrame()` **split into `BeginUiFrame(w,h,dt)` + `EndUiFrame()`** (documented contract amendment; interface is internal, no external consumers exist yet).
- Modified: `DearImGuiKSP/Infrastructure/NativeBridge.cs` — implement the split; remove the POC-SCAFFOLD demo-window line; stop reading `Screen`/`Time` (Unity values now come from the addon via parameters).
- Modified: `DearImGuiKSP/Infrastructure/Composition.cs` — wire registry + orchestrator.
- Modified: `DearImGuiKSP/Infrastructure/DearImGuiKSPAddon.cs` — `Update()` calls the orchestrator with `Screen.width/height` and `Time.deltaTime`; coroutine unchanged.

### Public API (locked — additive-only from here, invariant 2)

```csharp
public static class DearImGuiKSP
{
    public static bool IsAvailable { get; }                 // bridge initialized; later: state machine Running
    public static void Register(string id, Action callback) // per-frame UI declaration, called in registration order
    public static bool Unregister(string id)                // true if removed
    public static bool BeginWindow(string name)             // false = collapsed/clipped; EndWindow still required
    public static void EndWindow()
    public static void Text(string text)
    public static bool Button(string label)                 // true on click frame
    public static bool SliderFloat(string label, ref float value, float min, float max)
    public static bool InputText(string label, ref string value, int capacity = 256)
}
```

Behavior rules:
- `Register`: `ArgumentNullException` on null/empty id or null callback; duplicate id → logged warning, ignored.
- `Register`/`Unregister`/widget calls when unavailable: widgets no-op (return false), `Register` logs a warning and ignores. No exceptions for availability races.
- Widget methods delegate to `ImGuiInternal`; they are only valid inside a consumer callback (frame-loop gating documented in XML docs; misuse = no-op/undefined, not crash — do what `ImGuiInternal` already does, don't add machinery).

### Frame loop sequence (locked)

1. Addon `Update()` → `orchestrator.RunFrame(Screen.width, Screen.height, Time.deltaTime)`.
2. Orchestrator: skip when bridge not initialized. Else: `BeginUiFrame` → iterate registry in order, invoke each enabled consumer's callback (single try/catch + log per consumer as a placeholder — full FaultBarrier replaces this in C10, mark `TODO(C10)`) → `EndUiFrame`.
3. `IssuePluginEvent` coroutine unchanged (Infrastructure).

### Constraints

- Application stays Unity-free: `RunFrame` takes floats; `Screen`/`Time` are read only in the addon.
- No input locking (C9), no fault counting (C10), no settings (C11), no state machine (C12) — placeholders marked, not built.
- Demo mod untouched (C8).
- XML docs on every public member — these are the consumer-facing docs (Q42).

### Verification

- `dotnet build DearImGuiKSP.slnx` — 0 errors, 0 warnings.
- Native untouched (no rebuild needed); harness unaffected.
- Deploy; in-game verification is C8's (demo window returns via the API). Optionally note in KSP.log that init lines are unchanged.

### Rollback

- `git checkout -- DearImGuiKSP/`.
