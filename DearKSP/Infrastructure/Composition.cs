using DearKSP.Application;
using DearKSP.Application.Interfaces;

namespace DearKSP.Infrastructure
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

        /// <summary>The library-wide logger. Created once; safe to call before Init.</summary>
        internal static ILogger Logger => _logger ?? (_logger = new DearKSPLogger());

        /// <summary>
        /// The native bridge singleton. Created once; <see cref="INativeBridge.Initialize"/>
        /// is driven once by DearKSPAddon.Start().
        /// </summary>
        internal static NativeBridge Bridge => _bridge ?? (_bridge = new NativeBridge(Logger));

        /// <summary>
        /// Result of the one-time bridge initialization (-1 = not attempted yet, 0 = success).
        /// Held here so later chunks (state machine, C12) can gate on it.
        /// </summary>
        internal static int BridgeInitResult = -1;

        /// <summary>The consumer registry singleton (C7). Shared with the public facade.</summary>
        internal static ConsumerRegistry Registry => _registry ?? (_registry = new ConsumerRegistry());

        /// <summary>The frame loop orchestrator singleton (C7), driven by DearKSPAddon.Update().</summary>
        internal static FrameLoopOrchestrator Orchestrator =>
            _orchestrator ?? (_orchestrator = new FrameLoopOrchestrator(Bridge, Registry));

        /// <summary>
        /// Wires the public facade's internal hooks (C7): Application cannot see
        /// Infrastructure, so the logger and registry are handed over from here.
        /// Called once from DearKSPAddon.Awake(); idempotent.
        /// </summary>
        internal static void WireApplicationFacade()
        {
            DearKSP.Log = Logger;
            DearKSP.Registry = Registry;
        }

        /// <summary>
        /// Flips <see cref="DearKSP.IsAvailable"/> on. Called by DearKSPAddon.Start()
        /// after a successful bridge init. Later: driven by the state machine (C12).
        /// </summary>
        internal static void MarkAvailable()
        {
            DearKSP.SetAvailable(true);
        }

        // Later chunks add: SettingsStore/Model (C11), state machine (C12).
    }
}
