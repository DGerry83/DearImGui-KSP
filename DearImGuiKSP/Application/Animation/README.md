# Application/Animation

Created in milestone M5 of the pre-release feature wave
(`notes/active/2026-09-03_NewProject_PreReleaseFeatureWave/IMPLEMENTATION_PLAN.md`).

Planned contents:

- `Ease.cs` — easing enum and pure easing functions (linear, quad/cubic in/out/in-out).
- `Tween.cs` — `Tween.To(...)` factory and the public tween handle (`Cancel`, `IsPlaying`).
- `TweenEngine.cs` — delta-time-driven tick from the library frame loop; completed tweens are removed; ticks pause on library suspension (spec §5.4).
