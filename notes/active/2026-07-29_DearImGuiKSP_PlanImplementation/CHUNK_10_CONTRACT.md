# Chunk Contract: Fault Barrier + Auto-Disable

## Plan: DearImGui-KSP Implementation
## Date: 2026-08-31
## Chunk ID: C10
## Advances Milestone: M4 (parallel group A — runs concurrently with C9, C11)

### Scope

- Real per-consumer fault isolation (spec §5.3): a throwing consumer callback is caught, logged, and skipped for that frame; after **5 consecutive throwing frames** (`LibraryConfig.ConsumerFailureThreshold`) the consumer is auto-disabled; other consumers are unaffected.
- Replaces the placeholder try/catch in the frame loop — but the orchestrator swap itself is lead wiring, not this chunk.

### Inputs (must exist before starting)

- C7: `Application/ConsumerRegistry.cs` — `ConsumerRegistration` already carries `Enabled` and `ConsecutiveFailureCount` fields (marked `TODO(C10)`).
- C7: `Application/FrameLoopOrchestrator.cs` — placeholder try/catch marked `TODO(C10)` (read for the log-line style; do not modify).
- `LibraryConfig.ConsumerFailureThreshold = 5` (spec §5.3).
- Spec §7.2: the auto-disable notice is log-only — consumer name, exception, and disable notice under `[DearImGuiKSP]`.
- `Application/Interfaces/ILogger.cs` — the logging interface (Error/Warn/Info/Debug).

### Outputs (must be created/changed) — exclusive file ownership

- `Application/FaultBarrier.cs` — implement:
  ```csharp
  internal sealed class FaultBarrier
  {
      internal FaultBarrier(ILogger log);
      // Invokes one consumer's callback with fault isolation.
      internal void Invoke(ConsumerRegistry.ConsumerRegistration consumer);
  }
  ```
  Behavior (locked):
  - `consumer.Enabled == false` → return immediately.
  - Success → reset `ConsecutiveFailureCount` to 0 (the threshold counts *consecutive* frames).
  - Exception → increment the count, log `Error` with the consumer id and the exception; when the count reaches `LibraryConfig.ConsumerFailureThreshold`, set `Enabled = false` and log a second `Error` stating the consumer has been auto-disabled after N consecutive throwing frames (name + exception + notice, per §7.2).
- `Application/ConsumerRegistry.cs` — remove the now-stale `TODO(C10)` comment on `ConsecutiveFailureCount` and update the class doc comment's "inert until the FaultBarrier lands" sentence. No behavioral change.

**Explicitly NOT owned by this chunk** (lead wires after group A): `FrameLoopOrchestrator.cs`, `Composition.cs`, `DearImGuiKSPAddon.cs`. Do not touch them.

### Constraints

- Application stays Unity-free — `FaultBarrier` references only `System`, `ILogger`, and `ConsumerRegistry`.
- No new public API (invariant 2 — additive-only public surface untouched).
- Deterministic (spec §5.4): counting logic only, no timers/randomness.
- No git commits — the lead commits after review.

### Verification

- `dotnet build DearImGui-KSP.slnx` — 0 errors, 0 warnings (gate G3).
- Code-reviewable behavior table: thrower with threshold 5 → frames 1–4 logged/skipped, frame 5 disables; a succeeding frame mid-sequence resets the count; a second consumer is invoked normally throughout.
- AC9 (fault-injection consumer auto-disabled after 5 throwing frames, others unaffected) is user-verified in-game after the lead wires the barrier and adds a temporary fault-injection consumer to the demo — not this chunk's build-time gate.

### Rollback

- `git checkout -- DearImGuiKSP/Application/FaultBarrier.cs DearImGuiKSP/Application/ConsumerRegistry.cs`
