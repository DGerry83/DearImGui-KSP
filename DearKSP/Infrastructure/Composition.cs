namespace DearKSP.Infrastructure
{
    /// <summary>
    /// Composition root — the ONLY place concrete Infrastructure classes are instantiated
    /// and injected into Application components (CORE_PROTOCOLS §5.5, D18).
    /// Owns the single native render context reference for the session.
    /// TODO(milestone 1): wire DearKSPLogger; later milestones add the rest.
    /// </summary>
    internal static class Composition
    {
    }
}
