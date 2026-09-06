# Chunk Contract: Tween Engine (Ease + Tween + TweenEngine)
## Plan: DearImGui-KSP Pre-Release Feature Wave
## Date: 2026-09-04
## Chunk ID: C14
## Advances Milestone: M5 (widgets + tween — first chunk; zero native work)

### Scope
Land the pure-managed tween engine (spec §4.3, §5.4, §6.3; plan §2 "Entity:
Tween/TweenHandle", §3 "Component: TweenEngine"). Application-layer math, delta-time
driven, fully xUnit-gated. **No native changes, no Interop changes** — do not touch
`DearImGuiKSPNative/`, `Interop/`, or the 3 build scripts.

1. **`Application/Animation/Ease.cs`** (NEW) — `public enum Ease` with exactly:
   `Linear, QuadIn, QuadOut, QuadInOut, CubicIn, CubicOut, CubicInOut`
   (plan §2 Value Objects). Backed by an internal static class of pure functions
   `float Evaluate(Ease, float t)` over t in [0,1] (standard formulas: quad t²,
   cubic t³, InOut = half-scale in + half-scale out). No state, no allocation.
   Endpoints must be exact: Evaluate(anything, 0) == 0, Evaluate(anything, 1) == 1.

2. **`Application/Animation/Tween.cs`** (NEW) — the public surface:
   - `public static TweenHandle To(Action<float> set, float from, float to, float seconds, Ease ease)`
     — creates a live tween on the engine (wired like the facade hooks, see below),
     invokes `set(from)` immediately (t=0 baseline), returns the handle.
   - Color overload `To(Action<Color> set, Color from, Color to, float seconds, Ease ease)`
     — lerps RGBA by the eased t (`Color.Lerp` semantics, unclamped not needed).
   - `public readonly struct TweenHandle` — `Cancel()` and `IsPlaying`, both
     query/mutate the engine by handle id; internal ctor (I-02 precedent: public
     struct, internal construction, never a class — no boxing).
   - Namespace: match the public widget partials (`DearImGuiKSP`) — check
     `Application/Api/DearImGuiKSP.Toggle.cs` / `ImGuiEx.cs` for the exact
     namespace convention and follow it.
   - Guards consistent with the facade's "availability races never throw" rule:
     engine not wired yet or `DearImGuiKSP.IsAvailable` false → log one Warn line
     via `DearImGuiKSP.Log`, return an inert handle (`IsPlaying` false,
     `Cancel()` a no-op), never throw. Null `set` → `ArgumentNullException`
     (programmer error, same as `Register`).
   - Handle identity: monotonically increasing int id issued by the engine.
     A completed or cancelled id must not collide with a live one.

3. **`Application/Animation/TweenEngine.cs`** (NEW) — `internal sealed class`:
   - `TweenHandle StartFloat(...)`, `TweenHandle StartColor(...)` (internal),
     `void Tick(float deltaTime)`, `bool IsPlaying(int id)`, `void Cancel(int id)`.
   - `Tick` advances elapsed by deltaTime, computes `t = clamp01(elapsed/seconds)`,
     invokes the setter with the eased value; on reaching t=1 the setter gets the
     exact `to` value and the tween is removed (no accumulation, spec §5.4).
   - Removal during iteration: collect-then-remove or swap-back — but a setter
     must never be invoked after its tween completed/cancelled, and a setter that
     cancels its own (or another) tween mid-tick must not corrupt the iteration
     or skip/double-invoke another tween. Pick the simplest structure that makes
     this safe (e.g. snapshot the live list per tick) and state the choice.
   - Zero steady-state allocation at zero live tweens (the common case): `Tick`
     on an empty engine is a Count check. Preallocate the internal list at a small
     capacity; do not allocate per frame when no tweens are live.
   - `seconds <= 0` → complete immediately on the first tick (still one setter
     call at `from` from `To`, then `to` on first tick).

4. **Frame-loop wiring** — `FrameLoopOrchestrator.cs` + `Infrastructure/Composition.cs`:
   - Orchestrator gains a `TweenEngine` constructor parameter and ticks it once
     per frame inside `RunFrame`, after `_themeEngine.ApplyIfDirty()` and BEFORE
     `BeginUiFrame`/consumer callbacks, so setters see this frame's delta and
     consumers read fresh values the same frame. Suspension pause is free:
     `RunFrame` already returns early unless Running (spec §5.4).
   - Composition: `TweenEngine` singleton property (same lazy pattern as
     `ThemeEngine`), passed to the orchestrator; exposed to the public `Tween`
     class via an internal static hook assigned in `WireApplicationFacade()`
     (the `DearImGuiKSP.ThemeEngine` precedent — add `Tween.Engine = ...`
     alongside the existing four assignments).
   - Update the orchestrator's class-doc comment frame-sequence summary to
     include the tween tick (the comment is the locked §5.3 sequence — keep it
     truthful, additive only).

5. **Tests** — `tests/Application.Tests/Animation/TweenEngineTests.cs` (NEW):
   - Each Ease: exact endpoints (0→0, 1→1) + Linear midpoint sanity.
   - QuadIn/Out/Cubic midpoint values against the closed-form expectation
     (assert with tolerance, e.g. 1e-6).
   - Progression: two ticks of half duration land on the eased midpoint value.
   - Completion: tween reaching t=1 invokes setter exactly with `to`, is removed
     (subsequent ticks invoke nothing, `IsPlaying` false).
   - Cancel: no further setter invocations after `Cancel()`; `IsPlaying` false.
   - Self-cancel from inside a setter does not corrupt the tick or skip siblings.
   - Suspension pause: tick absence = pause — prove at orchestrator level via
     `FrameLoopOrchestratorTests` (existing fakes in `TestDoubles.cs`): a tween
     started while Running does not advance across frames where the lifecycle is
     not Running (reuse the existing suspend/resume test pattern there).
   - Engine-not-wired guard: `Tween.To` before wiring returns an inert handle
     and does not throw.
   - Update `FrameLoopOrchestratorTests` constructor call sites for the new
     parameter (C5 precedent: 1-line fake/stub additions are fine).
   - Remove the `Application/Animation/README.md` placeholder (INTEGRATION_CONTRACT
     stub list: first chunk landing real content in the folder removes it).

### Inputs (must exist before starting)
- `Application/FrameLoopOrchestrator.cs` (tick insertion point — read it first).
- `Infrastructure/Composition.cs` (`WireApplicationFacade` hook pattern, lines
  ~125-135; `ThemeEngine` lazy singleton pattern).
- `Application/DearImGuiKSP.cs` (facade guards, `Log?.Warn` availability pattern,
  `IsAvailable`).
- `Application/Api/DearImGuiKSP.Toggle.cs` + `Api/ImGuiEx.cs` (namespace +
  public-struct/internal-ctor conventions, I-02).
- `tests/Application.Tests/{FrameLoopOrchestratorTests,TestDoubles}.cs` (fakes).

### Outputs (must be created/changed)
- `DearImGuiKSP/Application/Animation/Ease.cs` (NEW)
- `DearImGuiKSP/Application/Animation/Tween.cs` (NEW)
- `DearImGuiKSP/Application/Animation/TweenEngine.cs` (NEW)
- `DearImGuiKSP/Application/Animation/README.md` (REMOVED — placeholder)
- `DearImGuiKSP/Application/FrameLoopOrchestrator.cs` (ctor + one Tick call + doc comment)
- `DearImGuiKSP/Infrastructure/Composition.cs` (singleton + hook assignment)
- `tests/Application.Tests/Animation/TweenEngineTests.cs` (NEW)
- `tests/Application.Tests/FrameLoopOrchestratorTests.cs` (ctor call updates + suspension test)
- NO native, Interop, demo, GameData, or settings changes. No version bump.

### Constraints
- Application layer stays Unity-lifecycle-free; `UnityEngine.Color`/`Vector2`
  references are allowed (D24 — already used by the facade).
- XML `///` docs on every public member (Ease values, Tween.To overloads,
  TweenHandle members). No emojis/symbol glyphs (D31).
- Determinism (spec §5.4): driven solely by deltaTime + easing function; no
  randomness, no wall-clock reads.
- Engine is internal; consumers see only `Tween`/`TweenHandle`/`Ease`.
- Do not add tweens to the demo — that is C17's ThemeDemo tab.

### Verification
- `dotnet build DearImGui-KSP.slnx` 0 errors / 0 warnings.
- `dotnet test DearImGui-KSP.slnx` — 76 baseline + new tests all green (report
  the exact count).
- No native rebuild needed (nothing native changed) — state this; do not run
  the native builds.
- Source-level steady-state check: no allocation in `Tick` when the live list
  is empty; state the structure chosen for live-tween storage and the
  removal-during-iteration safety argument.
- In-game animation proof is the M5 gate (C17, user-assisted) — deferred.

### Rollback
- Delete the three new Animation files + new test file; revert
  `FrameLoopOrchestrator.cs`, `Composition.cs`, `FrameLoopOrchestratorTests.cs`;
  restore `Application/Animation/README.md`; rebuild + `dotnet test` to confirm
  the 76/76 baseline.
