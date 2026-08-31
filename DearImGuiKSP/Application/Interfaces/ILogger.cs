namespace DearImGuiKSP.Application.Interfaces
{
    /// <summary>
    /// Writes [DearImGuiKSP]-prefixed lines to KSP.log; Debug is gated by verboseLogging (spec §7, §9).
    /// Implemented by Infrastructure.DearImGuiKSPLogger. Contains no Unity types — Application stays Unity-free.
    /// </summary>
    internal interface ILogger
    {
        void Error(string message);
        void Warn(string message);
        void Info(string message);
        void Debug(string message);
    }
}
