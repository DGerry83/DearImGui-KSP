using DearKSP.Application.Interfaces;

namespace DearKSP.Infrastructure
{
    /// <summary>
    /// Implements <see cref="ILogger"/> over UnityEngine.Debug with the [DearKSP] prefix.
    /// Debug-level lines are gated by the verboseLogging setting (spec §9).
    /// TODO(milestone 1): implement.
    /// </summary>
    internal sealed class DearKSPLogger : ILogger
    {
    }
}
