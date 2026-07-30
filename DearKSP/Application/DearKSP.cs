namespace DearKSP
{
    /// <summary>
    /// Public consumer-facing API for Dear KSP (spec §5.4, §6.1).
    /// Consumers check <see cref="IsAvailable"/> before registering their UI.
    /// TODO(milestone 3): registration and style access.
    /// </summary>
    public static class DearKSP
    {
        /// <summary>
        /// True when the library initialized successfully and is rendering.
        /// Consumers should fall back to their own UI when this is false.
        /// </summary>
        public static bool IsAvailable => false; // skeleton: wired to LifecycleStateMachine in milestone 3
    }
}
