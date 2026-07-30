namespace DearKSP.Application.Interfaces
{
    /// <summary>
    /// Writes [DearKSP]-prefixed lines to KSP.log; Debug is gated by verboseLogging (spec §7, §9).
    /// Implemented by Infrastructure.DearKSPLogger. Contains no Unity types — Application stays Unity-free.
    /// </summary>
    internal interface ILogger
    {
        void Error(string message);
        void Warn(string message);
        void Info(string message);
        void Debug(string message);
    }
}
