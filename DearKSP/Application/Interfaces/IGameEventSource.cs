namespace DearKSP.Application.Interfaces
{
    /// <summary>
    /// Translates game events into library signals: F2 UI hide/show, loading screens,
    /// resolution change (spec §5.2, §5.4).
    /// Implemented by Infrastructure.GameEventHooks.
    /// TODO(milestone 5): UiVisibilityChanged / LoadingScreenChanged / ResolutionChanged.
    /// </summary>
    internal interface IGameEventSource
    {
    }
}
