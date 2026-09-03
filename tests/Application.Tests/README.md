# tests/Application.Tests

Unit tests for the managed Application layer — the Unity-free orchestration core:
`LifecycleStateMachine`, `ConsumerRegistry`, `FaultBarrier`, `SettingsModel`
(incl. `ClampWindowsToViewport`), and `InputCaptureTracker` (incl. pointer-blocker
transitions). xUnit, net48, hand-written fakes against `Application.Interfaces`
(no mocking framework). Access to internals via `InternalsVisibleTo("Application.Tests")`
in `DearImGuiKSP.csproj`.

Run: `dotnet test` (solution) or `dotnet test tests/Application.Tests/Application.Tests.csproj`.
The csproj imports `DearImGui-KSP.props.user` (machine KSP pin) so standalone runs
outside the solution still satisfy KSPBuildTools. Application types are Unity-free, so
the test host never loads KSP/Unity assemblies.

Landed 2026-09-03 as Scope C of session `2026-09-03_Bug_ClickThrough_WindowAnchor`.
