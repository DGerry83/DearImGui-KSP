# Chunk Contract: C02 — Tween fault containment
## Plan: Opus Review Remediation (`notes\plans\opus-review-remediation-contracts.md`)
## Date: 2026-09-07
## Chunk ID: C02
## Advances Milestone: Post-1.0.0 remediation wave (severe item S3 + G3-07 code half)

### Scope
- **S3**: `TweenEngine.Tick` wraps each setter invocation in try/catch — a throwing setter is logged and its tween stopped (tombstoned for the post-tick sweep); sibling tweens keep running and the exception never escapes Tick. `TweenEngine` gains an optional `ILogger` constructor param; `Composition` passes the real logger.
- **G3-07 (code half)**: the immediate baseline `set(from)` in both `Tween.To` overloads is guarded — a throw is contained and logged, and the tween is not started (inert handle returned). XML docs updated to match. Doc half (O25, docs/50) is scheduled in C16.

### Inputs
- C01 complete (committed 7ea11d0); triage verdicts S3/G3-07 VALID.

### Outputs
- Changed: `Application/Animation/TweenEngine.cs`, `Application/Animation/Tween.cs`, `Infrastructure/Composition.cs`.
- New: `tests/Application.Tests/Animation/TweenFaultTests.cs` (3 tests).

### Constraints
- Public API surface unchanged (patch class, D36).
- §5.9 native-interop checklist applies (per-frame tick):
  - **Process-global state**: none new.
  - **Hot-path allocation**: none new (catch path only allocates the log string on failure).
  - **Resource-acquisition symmetry**: N/A (no acquisition), but the containment restores the frame loop's liveness guarantee — a throwing setter no longer skips `EndUiFrame` (C01 finally) nor rethrows forever.

### Verification
- `dotnet build` + `dotnet test` green: 110/110 (new: throwing setter killed after one call, siblings unaffected; zero-duration throwing tween swept; baseline throw contained, tween not started, engine stays usable).
- In-game: gate A (user) — throwing tween setter no longer freezes consumer UIs.

### Rollback
- Revert the three changed files; delete the new test file.
