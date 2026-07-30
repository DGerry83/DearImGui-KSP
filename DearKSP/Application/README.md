# Application layer (managed)

Use cases, orchestration, and the public consumer API. Depends on **Core (native)** only through the interfaces in `Interfaces/` — this folder must never reference KSP or Unity APIs directly.

Components (see `IMPLEMENTATION_PLAN.md` §3):

- `DearKSP.cs` — public facade consumed by other mods (`IsAvailable`, registration, style access).
- `ConsumerRegistry.cs` — tracks registered consumers in registration order with per-consumer fault state.
- `FrameLoopOrchestrator.cs` — the per-frame sequence: input → capture → locks → callbacks → ImGui frame → native handoff.
- `InputCaptureTracker.cs` — computes mouse/keyboard capture from ImGui IO each frame.
- `LifecycleStateMachine.cs` — `Uninitialized → Initializing → Running`, plus `Suspended` and `Failed`.
- `FaultBarrier.cs` — catches consumer exceptions; auto-disables at 5 consecutive throwing frames.
- `SettingsModel.cs` — in-memory settings snapshot with change notifications.
- `Interfaces/` — the seams Infrastructure implements (`ISettingsStore`, `INativeBridge`, `IInputLockGateway`, `IGameEventSource`, `IFailureNotifier`, `ILogger`).
