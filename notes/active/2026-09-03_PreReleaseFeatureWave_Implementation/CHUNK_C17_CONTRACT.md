# Chunk Contract: ThemeDemo Widget Showcase (Demo Mod)
## Plan: DearImGui-KSP Pre-Release Feature Wave
## Date: 2026-09-04
## Chunk ID: C17
## Advances Milestone: M5 (widgets + tween — final chunk; M5 gate follows)

### Scope
Add the widget showcase to the demo mod consuming C9 (gradients/radio), C10 (toggle),
C14 (tween), C15 (knob/wheel), C16 (spinner) — folded into the EXISTING demo window
(spec §6.2: "Theme demos fold into the existing demo window rather than the flight
window"). The demo consumes ONLY the public API (no internals). This chunk makes all
of M5's widgets visible for the user-assisted M5 gate.

1. **NEW `DearImGuiKSPDemo/ThemeDemo.cs`** — one panel class, `internal void DrawImGui()`,
   rendered as a labelled section inside the existing demo window (same pattern as the
   M3 "Theme showcase" section in `DemoConsumer.cs`, but its own file). Contents:
   - **Spinner row**: at least 4 distinct `SpinnerType` values side by side
     (`Spinner(type, radius, thickness)` — radius ~10-14 px at 18 px font), one with a
     non-default tint to prove the Color? path. Use SameLine/layout helpers available
     on the public facade.
   - **Knobs**: at least 2 knobs of different `KnobVariant` (e.g. Tick and WiperOnly)
     bound to persistent float fields.
   - **Wheels**: 1 horizontal + 1 vertical `Wheel`, bound to persistent fields.
   - **Toggle + radio + gradient buttons**: already showcased by the M3 section — do
     NOT duplicate them; the new section is additive (spinners/knobs/wheels/tween).
   - **Tween demo**: a "Play tween" button that starts
     `Tween.To(v => _tweenValue = v, _tweenValue, target, 2f, Ease.QuadInOut)`-
     style animations (use cached `Action<float>` delegate FIELDS, not lambdas
     capturing per click — allocation only on click is fine, per-frame is not):
     - one float tween driving a knob value (visible motion), ping-ponging
       between two targets on each click;
     - one Color tween driving a `TextColored` header through two palette colors;
     - a "Cancel" button demonstrating `TweenHandle.Cancel()` (state: keep the
       handles in fields; guard double-click by cancelling any live tween first).
   - Section header via `TextColored` with a KspPalette color (M3 precedent).

2. **Wiring** — `DemoConsumer.cs`: instantiate `ThemeDemo` in `Start()` alongside
   `_benchmark`/`_plotDemo`, call `_themeDemo.DrawImGui()` inside the existing main
   window scope after the M3 section. Update the class-doc comment to list the new
   showcase (it enumerates the demo's contents — keep it truthful, additive).
   No new window, no new toolbar button, no new registration.

### Inputs (must exist before starting)
- C14–C16 committed (`8b823b9`, `c117866`, `9a1ed51`): `Tween`/`TweenHandle`/`Ease`,
  `Knob`/`KnobVariant`/`KnobFlags`, `Wheel`/`WheelOrientation`,
  `Spinner`/`SpinnerType` — read `Application/Api/DearImGuiKSP.{Knob,Wheel,Spinner}.cs`
  and `Application/Animation/Tween.cs` for exact signatures and defaults.
- `DearImGuiKSPDemo/DemoConsumer.cs` (wiring + section precedent) and
  `DearImGuiKSPDemo/PlotDemo.cs` (separate-panel-class precedent, field-held state).
- `Application/Api/ImGuiEx.cs` if layout scopes are needed; check the facade for the
  SameLine/Separator/Text helpers actually available before writing layout code.

### Outputs (must be created/changed)
- `DearImGuiKSPDemo/ThemeDemo.cs` (NEW).
- `DearImGuiKSPDemo/DemoConsumer.cs` (instantiate + one call + doc comment).
- NO library changes (Application/Interop/Infrastructure/native untouched), NO
  settings changes, NO new xUnit tests (demo mod has no test project).

### Constraints
- Public API only — the demo must never touch internals (spec §4.1 demo row).
- Zero steady-state per-frame managed allocation in `DrawImGui`: all state in
  fields, labels are string constants, tween delegates cached in fields. Spinner
  per-frame calls are native-side animation — fine.
- Tweens started only from button clicks; `IsPlaying` guards, cancel-before-restart.
- Match the demo's existing style (4-space indent, `DearImGuiKSP.DearImGuiKSP.`
  qualification as the file does, or add a using alias if the file already does).
- XML docs on the new class and its public/internal members (demo convention:
  class-level doc comment at minimum, matching PlotDemo/BenchmarkUI).

### Verification
- `dotnet build DearImGui-KSP.slnx` 0/0; `dotnet test` 90/90 (unchanged count).
- Source-level no-alloc check of `DrawImGui`'s per-frame path (grep-review, C3
  precedent) — state the allocation story explicitly.
- The managed build mirrors GameData into the game install — confirm the demo DLL
  staged (`DearImGuiKSPDemo` bin → GameData path per the csproj).
- **M5 gate (user-assisted, NOT yours)**: in-game — spinner row animating, knobs/
  wheels draggable, tween plays/pauses/cancels visibly, suspension (F2) pauses
  tween ticks. Report readiness and hand off.

### Rollback
- Delete `ThemeDemo.cs`; revert `DemoConsumer.cs`; rebuild managed to confirm 90/90.
