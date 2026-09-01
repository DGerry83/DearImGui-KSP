using System;
using DearImGuiKSP.Application.Interfaces;

namespace DearImGuiKSP.Infrastructure
{
    /// <summary>
    /// Implements <see cref="IGameEventSource"/> by subscribing to KSP GameEvents
    /// (onHideUI/onShowUI, onGameSceneLoadRequested/onLevelWasLoadedGUIReady,
    /// onScreenResolutionModified) and translating them into library signals.
    /// </summary>
    internal sealed class GameEventHooks : IGameEventSource
    {
        private bool _subscribed;

        public event Action<bool> UiVisibilityChanged;   // false = hidden (F2), true = shown
        public event Action<bool> LoadingChanged;        // true = scene load requested, false = scene GUI ready
        public event Action<int, int> ResolutionChanged; // GameEvents.onScreenResolutionModified

        public void Subscribe()
        {
            if (_subscribed)
            {
                return;
            }

            _subscribed = true;

            GameEvents.onHideUI.Add(OnHideUI);
            GameEvents.onShowUI.Add(OnShowUI);
            GameEvents.onGameSceneLoadRequested.Add(OnGameSceneLoadRequested);
            GameEvents.onLevelWasLoadedGUIReady.Add(OnLevelWasLoadedGUIReady);
            GameEvents.onScreenResolutionModified.Add(OnScreenResolutionModified);
        }

        public void Unsubscribe()
        {
            if (!_subscribed)
            {
                return;
            }

            _subscribed = false;

            GameEvents.onHideUI.Remove(OnHideUI);
            GameEvents.onShowUI.Remove(OnShowUI);
            GameEvents.onGameSceneLoadRequested.Remove(OnGameSceneLoadRequested);
            GameEvents.onLevelWasLoadedGUIReady.Remove(OnLevelWasLoadedGUIReady);
            GameEvents.onScreenResolutionModified.Remove(OnScreenResolutionModified);
        }

        private void OnHideUI()
        {
            UiVisibilityChanged?.Invoke(false);
        }

        private void OnShowUI()
        {
            UiVisibilityChanged?.Invoke(true);
        }

        private void OnGameSceneLoadRequested(GameScenes scene)
        {
            LoadingChanged?.Invoke(true);
        }

        private void OnLevelWasLoadedGUIReady(GameScenes scene)
        {
            LoadingChanged?.Invoke(false);
        }

        private void OnScreenResolutionModified(int width, int height)
        {
            ResolutionChanged?.Invoke(width, height);
        }
    }
}
