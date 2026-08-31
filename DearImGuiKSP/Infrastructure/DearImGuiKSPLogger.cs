using ILogger = DearImGuiKSP.Application.Interfaces.ILogger;
using UnityEngine;

namespace DearImGuiKSP.Infrastructure
{
    /// <summary>
    /// Implements <see cref="ILogger"/> over UnityEngine.Debug with the [DearImGuiKSP] prefix.
    /// Debug-level lines are gated by the verboseLogging setting (spec §9) — currently a
    /// stub constant, replaced by SettingsModel in chunk C11 (tracked in INTEGRATION_CONTRACT.md).
    /// </summary>
    internal sealed class DearImGuiKSPLogger : ILogger
    {
        // STUB(C11): replaced by SettingsModel-backed verboseLogging.
        private const bool VerboseLoggingStub = true;

        public void Error(string message) => UnityEngine.Debug.LogError(LibraryConfig.LogPrefix + " " + message);

        public void Warn(string message) => UnityEngine.Debug.LogWarning(LibraryConfig.LogPrefix + " " + message);

        public void Info(string message) => UnityEngine.Debug.Log(LibraryConfig.LogPrefix + " " + message);

        public void Debug(string message)
        {
            if (VerboseLoggingStub)
            {
                UnityEngine.Debug.Log(LibraryConfig.LogPrefix + " [debug] " + message);
            }
        }
    }
}
