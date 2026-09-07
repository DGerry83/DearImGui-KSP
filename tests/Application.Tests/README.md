# tests/Application.Tests

Unit tests for the managed Application layer — the Unity-free orchestration core.
178 tests (xUnit, net48), hand-written fakes against `Application.Interfaces`
(no mocking framework). Access to internals via `InternalsVisibleTo("Application.Tests")`
in `DearImGuiKSP.csproj`.

Coverage set:

- Orchestration core: `LifecycleStateMachine`, `ConsumerRegistry`, `FaultBarrier`,
  `SettingsModel` (incl. `ClampWindowsToViewport` and the C06 debounced persistence),
  `InputCaptureTracker` (incl. pointer-blocker transitions), `FrameLoopOrchestrator`.
- Frame boundary (C01): the `FrameOpen` widget gate, registry snapshot iteration,
  and fault-barrier scope unwinding (`FrameBoundaryTests`).
- ABI guards (C07): out-of-range style enums, non-positive subplot dims, InputText
  capacity/UTF-8 seed clamping (`AbiGuardTests`).
- Widget guards (C08): RadioButton ref assignment, `##` label stripping, spinner
  id rules, window-title collision warning (`WidgetGuardTests`).
- Animation: `TweenEngine` tick semantics and throwing-setter containment (C02).
- Theming: `ThemeEngine` and the `ThemePresets` palette anchors.
- Pure helpers and ABI pins: gradient color math (Pack/Lerp/Lighten), failure-text
  mappings (`HelperTests`); `ImGuiCol`/`ImGuiStyleVar` ordinal pins and the ImPlot
  flag pins against the vendored headers (`StyleEnumPinTests`, `ImPlotFlagsTests`);
  Interop label/id routing through the empty-id sentinel (`InteropLabelRoutingTests`).

Run: `dotnet test` (solution) or `dotnet test tests/Application.Tests/Application.Tests.csproj`.
The csproj imports `DearImGui-KSP.props.user` (machine KSP pin) so standalone runs
outside the solution still resolve `$(KSPBT_GameRoot)` — the test project references
`UnityEngine.CoreModule.dll` from the pinned game: the theming and facade tests use
the real `Color32`/`Vector2` structs, so the test host **does** load that one Unity
assembly (pure blittable structs, safe outside Unity). No KSP assemblies
(`Assembly-CSharp` etc.) are referenced or loaded.

Landed 2026-09-03 as Scope C of session `2026-09-03_Bug_ClickThrough_WindowAnchor`;
extended through the 2026-09-07 post-release remediation wave (C01/C02/C06/C07/C08/C14).
