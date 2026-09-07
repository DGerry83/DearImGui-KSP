# Application layer (managed)

Use cases, orchestration, and the public consumer API. Unity-free except for the
`UnityEngine.CoreModule` math structs (`Vector2`/`Color`/`Color32`) allowed in
public signatures (D24); this folder never references KSP APIs or engine
lifecycle/scene APIs. Widget calls reach the native Core through the P/Invoke
bindings in `../Interop`; everything else native or engine-facing goes through
the seams in `Interfaces/`, which Infrastructure implements.

Components:

- `DearImGuiKSP.cs` — public facade consumed by other mods (`IsAvailable`, registration, style push/pop). Every widget call is gated on `CanDeclareUi` (available **and** frame open, C01/S2), so out-of-frame calls are safe no-ops; invalid style enums are logged no-ops (C07).
- `Api/` — the rest of the public surface: facade widget partials (`DearImGuiKSP.Header/Knob/Radio/Spinner/TextColored/Toggle/Wheel`), the `ImGuiEx` exception-safe scope guards, `ImGuiDraw`/`ImGuiGradients` draw helpers, `ImGuiPlot` (ImPlot scopes + `PlotLine`), and the style enums (`ImGuiStyleEnums.cs`).
- `Animation/` — the tween engine (`Tween`, `TweenEngine`, `Ease`). Ticked at frame start before any consumer callback; setters are fault-contained (C02).
- `Theming/` — named theme presets (`ThemePresets`, `KspPalette`) and `ThemeEngine` (dirty-flag deferred apply at frame start).
- `ConsumerRegistry.cs` — tracks registered consumers in registration order with per-consumer fault state; the frame loop iterates a lazily-refreshed snapshot, so register/unregister from inside a callback applies from the next frame (C01/S1).
- `FrameLoopOrchestrator.cs` — the per-frame sequence: deferred theme apply → debounced settings persist (C06) → tween tick → viewport clamp on size change → capture sample → native BeginUiFrame → consumer callbacks in registration order through the FaultBarrier → native EndUiFrame (in `finally`, so the native frame lock always releases). Runs only while Running; F2/loading suspension and failed sessions produce no frames.
- `InputCaptureTracker.cs` — computes mouse/keyboard capture from ImGui IO each frame (previous frame's state, spec §5.3).
- `LifecycleStateMachine.cs` — `Uninitialized → Initializing → Running`, plus `Suspended` and `Failed`.
- `FaultBarrier.cs` — catches consumer exceptions; auto-disables at 5 consecutive throwing frames. On a throw it unwinds any scopes the callback left open via `OpenScopeTracker` (C01/G2-05), so later consumers are not mis-parented; also exposes the running consumer id (`CurrentConsumerId`) for the facade's window-title collision warning (C08/G2-11).
- `OpenScopeTracker.cs` — per-frame count of facade Begin/End and Push/Pop pairs; drives the fault-barrier unwind.
- `SettingsModel.cs` — in-memory settings snapshot with change notifications; setters only flag dirty, the frame loop writes settings.cfg once changes settle (debounced, C06).
- `LibrarySettings.cs`, `InputCaptureState.cs`, `FailureKind.cs`, `FailureText.cs` — plain records/enums shared across the layer.
- `Interfaces/` — the seams Infrastructure implements (`ISettingsStore`, `INativeBridge`, `IInputLockGateway`, `IPointerBlockerGateway`, `IImguiEventEaterGateway`, `IGameEventSource`, `IFailureNotifier`, `ILogger`).
