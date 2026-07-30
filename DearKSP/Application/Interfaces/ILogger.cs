namespace DearKSP.Application.Interfaces
{
    /// <summary>
    /// Writes [DearKSP]-prefixed lines to KSP.log; Debug is gated by verboseLogging (spec §7, §9).
    /// Implemented by Infrastructure.DearKSPLogger.
    /// TODO(milestone 1): Error / Warn / Info / Debug.
    /// </summary>
    internal interface ILogger
    {
    }
}
