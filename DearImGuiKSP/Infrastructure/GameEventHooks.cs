using DearImGuiKSP.Application.Interfaces;

namespace DearImGuiKSP.Infrastructure
{
    /// <summary>
    /// Implements <see cref="IGameEventSource"/> by subscribing to GameEvents and
    /// UIMasterController signals (F2 UI hide, loading screens, resolution change).
    /// TODO(milestone 5): implement.
    /// </summary>
    internal sealed class GameEventHooks : IGameEventSource
    {
    }
}
