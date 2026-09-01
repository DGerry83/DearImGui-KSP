using DearImGuiKSP.Application;
using DearImGuiKSP.Application.Interfaces;

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
        private static InputCaptureTracker _captureTracker;
        private static FaultBarrier _faultBarrier;
        private static LifecycleStateMachine _stateMachine;
        private static GameEventHooks _gameEventHooks;

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

        /// <summary>The input capture tracker singleton (C9), driven by the orchestrator.</summary>
        internal static InputCaptureTracker CaptureTracker =>
            _captureTracker ?? (_captureTracker = new InputCaptureTracker(LockGateway, Registry));

        /// <summary>The consumer fault barrier singleton (C10), driven by the orchestrator.</summary>
        internal static FaultBarrier Barrier => _faultBarrier ?? (_faultBarrier = new FaultBarrier(Logger));

        /// <summary>The lifecycle state machine singleton (C12). Drives availability and suspend/resume.</summary>
        internal static LifecycleStateMachine StateMachine =>
            _stateMachine ?? (_stateMachine = new LifecycleStateMachine(Logger));

        /// <summary>The game-event hook source singleton (C12). Subscribed by <see cref="WireLifecycle"/>.</summary>
        internal static GameEventHooks Hooks => _gameEventHooks ?? (_gameEventHooks = new GameEventHooks());

        /// <summary>The frame loop orchestrator singleton (C7; C9/C10/C12 wired), driven by DearImGuiKSPAddon.Update().</summary>
        internal static FrameLoopOrchestrator Orchestrator =>
            _orchestrator ?? (_orchestrator = new FrameLoopOrchestrator(Bridge, Registry, CaptureTracker, Barrier, StateMachine));

        /// <summary>
        /// Wires the public facade's internal hooks (C7, C12): Application cannot see
        /// Infrastructure, so the logger, registry, and state machine are handed over
        /// from here. Called once from DearImGuiKSPAddon.Awake(); idempotent.
        /// </summary>
        internal static void WireApplicationFacade()
        {
            DearImGuiKSP.Log = Logger;
            DearImGuiKSP.Registry = Registry;
            DearImGuiKSP.Lifecycle = StateMachine;
        }

        /// <summary>
        /// Connects game-event hooks to the state machine and the bridge (C12), then
        /// subscribes to GameEvents. Called by DearImGuiKSPAddon.Start() after a
        /// successful bridge init.
        /// </summary>
        internal static void WireLifecycle()
        {
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
            Hooks.ResolutionChanged += (w, h) => Bridge.RebuildViewport(w, h);
            Hooks.Subscribe();
        }

        // Later chunks add: failure notifier (C13).
    }
}
