using DearImGuiKSP.Application.Interfaces;

namespace DearImGuiKSP.Infrastructure
{
    /// <summary>
    /// Implements <see cref="ISettingsStore"/> over KSP ConfigNode.
    /// Read at startup, written on change; migrates older formatVersions forward (spec §4.4, §10.5).
    /// TODO(milestone 5): implement.
    /// </summary>
    internal sealed class SettingsStore : ISettingsStore
    {
    }
}
