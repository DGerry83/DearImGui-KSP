namespace DearImGuiKSP.Application
{
    /// <summary>
    /// Catches consumer callback exceptions, counts consecutive failures per consumer,
    /// and disables the consumer at LibraryConfig.ConsumerFailureThreshold (spec §5.3).
    /// TODO(milestone 4): implement.
    /// </summary>
    internal sealed class FaultBarrier
    {
    }
}
