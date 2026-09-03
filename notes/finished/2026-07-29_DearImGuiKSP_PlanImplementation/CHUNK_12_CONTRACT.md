# Chunk Contract: Lifecycle State Machine + Game-Event Hooks

## Plan: DearImGui-KSP Implementation
## Date: 2026-08-31
## Chunk ID: C12
## Advances Milestone: M5

### Scope

- Real lifecycle state machine (spec §5.2): `Uninitialized → Initializing → Running`, plus `Suspended` (F2 UI hide, loading screens) and terminal `Failed`.
- `GameEventHooks` translating GameEvents into library signals (spec §5.2, §5.4).
- `IsAvailable` moves behind the state machine (HANDOFF §8 tracked item); `RebuildViewport` no-op resolved and documented.
- The failure *popup* is C13 — `Fail()` here is log + terminal state only.

### Inputs (must exist before starting)

- C7 facade + frame loop (`DearImGuiKSP.cs`, `FrameLoopOrchestrator.cs`); C9/C10/C11 wired (group A).
- GameEvents verified against the KSP Knowledge Library dump (`index/skeletons/GameEvents.cs`): `onHideUI`/`onShowUI` (EventVoid, :389/:390), `onGameSceneLoadRequested` (:253) / `onLevelWasLoadedGUIReady` (:256) (EventData\<GameScenes\>), `onScreenResolutionModified` (EventData\<int,int\>, :258).
- Skeletons: `Application/LifecycleStateMachine.cs`, `Application/Interfaces/IGameEventSource.cs`, `Infrastructure/GameEventHooks.cs`.

### Locked designs

**`Application/LifecycleStateMachine.cs`:**
```csharp
internal enum LifecycleState { Uninitialized, Initializing, Running, Suspended, Failed }

internal sealed class LifecycleStateMachine
{
    internal LifecycleStateMachine(ILogger log);
    internal LifecycleState State { get; }        // starts Uninitialized
    internal bool IsRunning { get; }              // State == Running

    internal void MarkInitializing();             // Uninitialized → Initializing
    internal void MarkRunning();                  // Initializing → Running
    internal void SetUiVisible(bool visible);     // Running ↔ Suspended
    internal void SetLoading(bool loading);       // Running ↔ Suspended
    internal void Fail(string reason);            // any state → Failed (terminal)
}
```
Behavior rules:
- Suspended is computed from two latched flags (`_uiHidden`, `_loading`): enter Suspended from Running when either sets; return to Running when both clear. Flags latched even while not Running so a mid-load F2 behaves correctly on resume.
- Illegal transitions (e.g. `MarkRunning` from Failed, `SetUiVisible` before Running): `Warn` log, no state change. `Fail` from `Failed`: ignored. Every real transition logs `Debug` (from → to); `Fail` additionally logs `Error` with the reason.
- Deterministic; no timers/threads.

**`Application/Interfaces/IGameEventSource.cs`:**
```csharp
internal interface IGameEventSource
{
    event Action<bool> UiVisibilityChanged;   // false = hidden (F2), true = shown
    event Action<bool> LoadingChanged;        // true = scene load requested, false = scene GUI ready
    event Action<int, int> ResolutionChanged; // GameEvents.onScreenResolutionModified
    void Subscribe();
    void Unsubscribe();
}
```

**`Infrastructure/GameEventHooks.cs`**: implements the above over GameEvents (`onHideUI`/`onShowUI`, `onGameSceneLoadRequested`/`onLevelWasLoadedGUIReady`, `onScreenResolutionModified`). Subscribe/Unsubscribe idempotent; raises nothing before Subscribe. Assumed initial state: UI visible, not loading.

**`Application/DearImGuiKSP.cs`** (facade amendment — internal mechanics only, public signature unchanged):
- New internal hook: `internal static LifecycleStateMachine Lifecycle { get; set; }`
- `IsAvailable` becomes `Lifecycle != null && (Lifecycle.State == Running || Lifecycle.State == Suspended)` — suspended means temporarily paused, NOT unavailable; consumers must not tear down on F2. XML doc updated to say so.
- Remove `_isAvailable` / `SetAvailable` (internal hooks superseded by the state machine; no external consumers exist).

**`Infrastructure/NativeBridge.cs`**: `RebuildViewport` stays a managed no-op — justify in the comment: ImGui `DisplaySize` is set every `BeginFrame`, the backend draws into whatever RT is bound at render-event time, and the font atlas is resolution-independent, so there is nothing to rebuild; add a `Debug` log so resolution changes are visible under verboseLogging. Remove the TODO(C12).

### Outputs — exclusive file ownership

- Agent owns: `Application/LifecycleStateMachine.cs`, `Application/Interfaces/IGameEventSource.cs`, `Infrastructure/GameEventHooks.cs`, `Application/DearImGuiKSP.cs`, `Infrastructure/NativeBridge.cs` (comment/log only).
- **Lead wires after the agent returns** (agent must NOT touch): `Composition.cs` (StateMachine + GameEventHooks singletons, facade hook assignment, hook→state-machine subscription, ResolutionChanged→RebuildViewport), `DearImGuiKSPAddon.cs` (MarkInitializing/MarkRunning/Fail in Start, kill switch stays dormant at Uninitialized, render pump gated on IsRunning), `FrameLoopOrchestrator.cs` (guard becomes lifecycle `IsRunning`; ctor gains the state machine).

### Constraints

- Application stays Unity-free (state machine references only `System` + `ILogger`).
- Public API surface unchanged (invariant 2) — `IsAvailable` semantics amend internal docs only.
- `Failed` means no rendering and no consumer callbacks (spec §5.2) — enforced via the orchestrator/pump guards (lead wiring), not by the state machine itself.
- No git commits — the lead commits after review.

### Verification

- `dotnet build DearImGui-KSP.slnx` — 0 errors, 0 warnings (gate G3). Native untouched except a comment — no rebuild strictly needed, harmless if run.
- **AC10 (user, in-game)**: F2 hides the demo window and pauses it (button counter frozen), F2 restores; no library UI during scene loading screens; changing resolution in KSP settings keeps the demo window rendering correctly at the new resolution.

### Rollback

- `git checkout -- DearImGuiKSP/Application/LifecycleStateMachine.cs DearImGuiKSP/Application/Interfaces/IGameEventSource.cs DearImGuiKSP/Application/DearImGuiKSP.cs DearImGuiKSP/Infrastructure/GameEventHooks.cs DearImGuiKSP/Infrastructure/NativeBridge.cs`
