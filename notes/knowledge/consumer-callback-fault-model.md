# Consumer Callback Fault Model — Every External Invocation Point Needs Containment

**Source:** Opus review triage 2026-09-07, severe items S1–S3 (+ G2-05, M10). All three severe bugs are this one shape: a consumer-code invocation point the FaultBarrier design didn't count.

## The inventory rule

Whenever a component invokes consumer code, enumerate **every** invocation site — not just the obvious registered callback — and answer four questions per site:

1. **Exception containment** — is a throw caught, logged, and isolated from other consumers?
2. **Mutation during iteration** — can the callback mutate the collection being iterated (Register/Unregister during frame dispatch)? If yes: iterate a snapshot or defer mutations to a queue.
3. **Begin/end pairing under exceptions** — is the end half (EndUiFrame, lock release) guaranteed via try/finally even when consumer code throws?
4. **Entry-point state predicate** — do public API gates distinguish *operation in flight* from merely *session active*?

## Canonical failures (do not regress)

- **S1** — `FrameLoopOrchestrator.RunFrame` iterates the live `List<>` (`ConsumerRegistry.cs:33`); a consumer registering inside its own callback throws `InvalidOperationException` *outside* the FaultBarrier, skips `EndUiFrame()`, and deadlocks both game and render threads (SRWLOCK held BeginFrame→EndFrame, `ContextHost.cpp:129`).
- **S2** — `IsAvailable` (`DearImGuiKSP.cs:43`) is a session predicate, not a frame-open predicate; widget calls from a consumer's own Update reach cimgui with no open frame, and /DNDEBUG means null deref instead of an assert. Gates need a frame-open flag set between BeginUiFrame/EndUiFrame.
- **S3** — `TweenEngine.Tick` setters are consumer code *outside* the FaultBarrier (`TweenEngine.cs:110`); a throwing setter rethrows every frame forever, starving the whole frame loop. Guard per-setter: catch → log → kill that tween.
- **G2-05** — FaultBarrier restores no ImGui stack state; a throwing consumer mis-parents later consumers' widgets that frame unless they use ImGuiEx using-scopes.

## Testing consequence

Any component on this boundary needs an adversarial-consumer test in its gates: a callback that throws, a callback that re-enters (Register/Unregister mid-callback), and a widget call made outside a frame callback. Happy-path tests never exercise this fault model.
