using DearImGuiKSP.Application;
using DearImGuiKSP.Application.Animation;
using DearImGuiKSP.Application.Interfaces;
using UnityEngine;
using ILogger = DearImGuiKSP.Application.Interfaces.ILogger;
using Object = UnityEngine.Object;

namespace DearImGuiKSP.Infrastructure
{
    /// <summary>
    /// Composition root — the ONLY place concrete Infrastructure classes are instantiated
    /// and injected into Application components (CORE_PROTOCOLS §5.5, D18).
    /// Owns the single native bridge reference and its init result for the session (C4),
    /// plus the consumer registry and frame loop orchestrator (C7).
    /// </summary>
    internal static class Composition
    {
        private static ILogger _logger;
        private static NativeBridge _bridge;
        private static ConsumerRegistry _registry;
        private static FrameLoopOrchestrator _orchestrator;
        private static SettingsStore _settingsStore;
        private static SettingsModel _settings;
        private static InputLockGateway _lockGateway;
        private static PointerBlockerGateway _pointerBlockerGateway;
        private static ImguiEventEaterGateway _imguiEventEater;
        private static InputCaptureTracker _captureTracker;
        private static FaultBarrier _faultBarrier;
        private static LifecycleStateMachine _stateMachine;
        private static ThemeEngine _themeEngine;
        private static DockingModeApplier _dockingModeApplier;
        private static LibraryControlPanel _controlPanel;
        private static LibraryPanelToolbar _panelToolbar;
        private static TweenEngine _tweenEngine;
        private static GameEventHooks _gameEventHooks;
        private static FailureNotifier _failureNotifier;

        /// <summary>The library-wide logger. Created once; safe to call before Init.</summary>
        internal static ILogger Logger => _logger ?? (_logger = CreateLogger());

        private static ILogger CreateLogger()
        {
            var logger = new DearImGuiKSPLogger();
            // Deferred lambda: Settings is only resolved when the first Debug line is
            // evaluated, so the logger can exist before the settings file is read (C11).
            logger.VerboseLoggingProvider = () => Settings.VerboseLogging;
            return logger;
        }

        /// <summary>
        /// The native bridge singleton. Created once; <see cref="INativeBridge.Initialize"/>
        /// is driven once by DearImGuiKSPAddon.Start().
        /// </summary>
        internal static NativeBridge Bridge => _bridge ?? (_bridge = new NativeBridge(Logger));

        /// <summary>
        /// Result of the one-time bridge initialization (-1 = not attempted yet, 0 = success).
        /// Held here so later chunks (state machine, C12) can gate on it.
        /// </summary>
        internal static int BridgeInitResult = -1;

        /// <summary>The consumer registry singleton (C7). Shared with the public facade.</summary>
        internal static ConsumerRegistry Registry => _registry ?? (_registry = new ConsumerRegistry());

        /// <summary>The settings store singleton (C11). Used only by <see cref="Settings"/>.</summary>
        private static SettingsStore Store => _settingsStore ?? (_settingsStore = new SettingsStore(Logger));

        /// <summary>The settings model singleton (C11). Loads settings.cfg on first access.</summary>
        internal static SettingsModel Settings => _settings ?? (_settings = new SettingsModel(Store));

        /// <summary>The input lock gateway singleton (C9).</summary>
        internal static InputLockGateway LockGateway => _lockGateway ?? (_lockGateway = new InputLockGateway());

        /// <summary>The pointer blocker gateway singleton (ISSUES #001; G1 rework: logger).</summary>
        internal static PointerBlockerGateway PointerBlocker =>
            _pointerBlockerGateway ?? (_pointerBlockerGateway = new PointerBlockerGateway(Logger));

        /// <summary>
        /// The IMGUI event eater singleton (ISSUES #003). Created eagerly by
        /// <see cref="WireLifecycle"/> (after successful init, before frames run)
        /// on a DontDestroyOnLoad GameObject; always active, eats nothing unless
        /// the tracker raises a shield.
        /// </summary>
        internal static ImguiEventEaterGateway ImguiEventEater =>
            _imguiEventEater ?? (_imguiEventEater = CreateImguiEventEater());

        private static ImguiEventEaterGateway CreateImguiEventEater()
        {
            var go = new GameObject("DearImGuiKSP.ImguiEventEater");
            Object.DontDestroyOnLoad(go);
            return go.AddComponent<ImguiEventEaterGateway>();
        }

        /// <summary>The input capture tracker singleton (C9), driven by the orchestrator.</summary>
        internal static InputCaptureTracker CaptureTracker =>
            _captureTracker ?? (_captureTracker = new InputCaptureTracker(LockGateway, PointerBlocker, ImguiEventEater, Registry));

        /// <summary>The consumer fault barrier singleton (C10), driven by the orchestrator.</summary>
        internal static FaultBarrier Barrier => _faultBarrier ?? (_faultBarrier = new FaultBarrier(Logger));

        /// <summary>The lifecycle state machine singleton (C12). Drives availability and suspend/resume.</summary>
        internal static LifecycleStateMachine StateMachine =>
            _stateMachine ?? (_stateMachine = new LifecycleStateMachine(Logger));

        /// <summary>The game-event hook source singleton (C12). Subscribed by <see cref="WireLifecycle"/>.</summary>
        internal static GameEventHooks Hooks => _gameEventHooks ?? (_gameEventHooks = new GameEventHooks());

        /// <summary>The frame loop orchestrator singleton (C7; C9/C10/C12 wired, C14 timing), driven by DearImGuiKSPAddon.Update().</summary>
        internal static FrameLoopOrchestrator Orchestrator =>
            _orchestrator ?? (_orchestrator = new FrameLoopOrchestrator(Bridge, Registry, CaptureTracker, Barrier, StateMachine, Logger, Settings, ThemeEngine, DockingModeApplier, TweenEngine));

        /// <summary>
        /// The theme engine singleton (C8): subscribes to SettingsModel.Changed,
        /// applies the configured preset at startup (DearImGuiKSPAddon.Start, after
        /// the font load) and re-applies through the orchestrator's dirty-flag path.
        /// </summary>
        internal static ThemeEngine ThemeEngine =>
            _themeEngine ?? (_themeEngine = new ThemeEngine(Settings, Logger));

        /// <summary>
        /// The docking mode applier singleton (ISSUES #011): subscribes to
        /// SettingsModel.Changed and forwards the persisted docking flag to the
        /// native context — once at startup (DearImGuiKSPAddon.Start) and again
        /// through the orchestrator's dirty-flag path on every toggle.
        /// </summary>
        internal static DockingModeApplier DockingModeApplier =>
            _dockingModeApplier ?? (_dockingModeApplier = new DockingModeApplier(Settings, Logger));

        /// <summary>
        /// The library's own control panel singleton (C31): the "DearImGui-KSP
        /// Settings" window. Registered as a regular consumer from
        /// DearImGuiKSPAddon.Start; the toolbar button
        /// (<see cref="LibraryPanelToolbar"/>) toggles its visibility.
        /// </summary>
        internal static LibraryControlPanel ControlPanel =>
            _controlPanel ?? (_controlPanel = new LibraryControlPanel(Settings));

        /// <summary>
        /// The library settings toolbar button singleton (C31; ISSUES #015
        /// rework): a plain class initialized once by DearImGuiKSPAddon after
        /// the state machine reaches Running, and shut down at session end.
        /// The launcher persists across scenes, so registration is one-shot.
        /// </summary>
        internal static LibraryPanelToolbar PanelToolbar =>
            _panelToolbar ?? (_panelToolbar = new LibraryPanelToolbar());

        /// <summary>
        /// The tween engine singleton (C14): advanced once per frame by the
        /// orchestrator; exposed to the public <c>Tween</c> facade via
        /// <see cref="WireApplicationFacade"/>.
        /// </summary>
        internal static TweenEngine TweenEngine =>
            _tweenEngine ?? (_tweenEngine = new TweenEngine(Logger));

        /// <summary>The failure notifier singleton (C13). Shows the one-per-session failure popup.</summary>
        internal static FailureNotifier Notifier =>
            _failureNotifier ?? (_failureNotifier = new FailureNotifier(Logger));

        /// <summary>
        /// Wires the public facade's internal hooks (C7, C12) and arms failure
        /// notification (C13): the notifier is subscribed here, before bridge init in
        /// Start(), so a failed initialization still produces the popup. Called once
        /// from DearImGuiKSPAddon.Awake(); idempotent.
        /// </summary>
        internal static void WireApplicationFacade()
        {
            DearImGuiKSP.Log = Logger;
            DearImGuiKSP.Registry = Registry;
            DearImGuiKSP.Lifecycle = StateMachine;
            DearImGuiKSP.ThemeEngine = ThemeEngine;
            Tween.Engine = TweenEngine;

            // -=/+= keeps the call idempotent: event subscription is not.
            StateMachine.EnteredFailed -= OnEnteredFailed;
            StateMachine.EnteredFailed += OnEnteredFailed;
        }

        private static void OnEnteredFailed(FailureKind kind)
        {
            Notifier.Notify(kind);
        }

        /// <summary>
        /// Connects game-event hooks to the state machine and the bridge (C12), then
        /// subscribes to GameEvents. Called by DearImGuiKSPAddon.Start() after a
        /// successful bridge init.
        /// </summary>
        internal static void WireLifecycle()
        {
            // ISSUES #003: the eater must exist before frames run and regardless of
            // first capture — force creation now (it is inert unless shielded).
            _ = ImguiEventEater;

            // Entering suspension (or failure) while capturing must not leave locks held.
            Hooks.UiVisibilityChanged += visible =>
            {
                if (!visible) CaptureTracker.ReleaseAll();
                StateMachine.SetUiVisible(visible);
            };
            Hooks.LoadingChanged += loading =>
            {
                if (loading) CaptureTracker.ReleaseAll();
                StateMachine.SetLoading(loading);
            };
            // The viewport clamp moved into the frame loop (G3 rework, 2026-09-03):
            // onScreenResolutionModified may never fire, and io.DisplaySize is stale
            // at event time — the orchestrator detects the size change per frame.
            Hooks.ResolutionChanged += (w, h) => Bridge.RebuildViewport(w, h);
            Hooks.Subscribe();
        }
    }
}
