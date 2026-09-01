using System;

namespace DearImGuiKSP.Application.Interfaces
{
    /// <summary>
    /// Translates game events into library signals: F2 UI hide/show, loading screens,
    /// resolution change (spec §5.2, §5.4).
    /// Implemented by Infrastructure.GameEventHooks.
    /// </summary>
    internal interface IGameEventSource
    {
        event Action<bool> UiVisibilityChanged;   // false = hidden (F2), true = shown
        event Action<bool> LoadingChanged;        // true = scene load requested, false = scene GUI ready
        event Action<int, int> ResolutionChanged; // GameEvents.onScreenResolutionModified

        void Subscribe();
        void Unsubscribe();
    }
}
