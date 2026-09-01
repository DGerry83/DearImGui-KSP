using System;
using ILogger = DearImGuiKSP.Application.Interfaces.ILogger;

namespace DearImGuiKSP.Infrastructure
{
    /// <summary>
    /// Implements <see cref="ILogger"/> over UnityEngine.Debug with the [DearImGuiKSP] prefix.
    /// Debug-level lines are gated by the verboseLogging setting (spec §9) via a
    /// SettingsModel-backed provider set by the composition root.
    /// </summary>
    internal sealed class DearImGuiKSPLogger : ILogger
    {
        /// <summary>
        /// Supplies the current verbose-logging flag. When null, debug output is suppressed.
        /// Wired to <see cref="Application.SettingsModel.VerboseLogging"/> by the composition root.
        /// </summary>
        internal Func<bool> VerboseLoggingProvider { get; set; }

        public void Error(string message) => UnityEngine.Debug.LogError(LibraryConfig.LogPrefix + " " + message);

        public void Warn(string message) => UnityEngine.Debug.LogWarning(LibraryConfig.LogPrefix + " " + message);

        public void Info(string message) => UnityEngine.Debug.Log(LibraryConfig.LogPrefix + " " + message);

        public void Debug(string message)
        {
            if (VerboseLoggingProvider?.Invoke() == true)
            {
                UnityEngine.Debug.Log(LibraryConfig.LogPrefix + " [debug] " + message);
            }
        }
    }
}
