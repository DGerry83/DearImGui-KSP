using DearKSP.Application.Interfaces;

namespace DearKSP.Infrastructure
{
    /// <summary>
    /// Composition root — the ONLY place concrete Infrastructure classes are instantiated
    /// and injected into Application components (CORE_PROTOCOLS §5.5, D18).
    /// Owns the single native render context reference for the session (added in C4).
    /// </summary>
    internal static class Composition
    {
        private static ILogger _logger;

        /// <summary>The library-wide logger. Created once; safe to call before Init.</summary>
        internal static ILogger Logger => _logger ?? (_logger = new DearKSPLogger());

        // Later chunks add: Init() wiring SettingsStore/Model (C11), NativeBridge (C4),
        // state machine (C12), and the frame loop (C7).
    }
}
