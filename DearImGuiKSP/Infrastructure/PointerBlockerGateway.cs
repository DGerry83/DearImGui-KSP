using DearImGuiKSP.Application.Interfaces;
using UnityEngine;
using UnityEngine.UI;
using ILogger = DearImGuiKSP.Application.Interfaces.ILogger;

namespace DearImGuiKSP.Infrastructure
{
    /// <summary>
    /// Implements <see cref="IPointerBlockerGateway"/> with an invisible uGUI
    /// raycast blocker: a full-screen alpha-0 Image on a Screen Space - Overlay
    /// canvas sorted above stock UI. While active, EventSystem graphic raycasts
    /// — and every IsPointerOverGameObject() consumer, which gates world picking —
    /// stop at the blocker instead of reaching the stock UI below ImGui windows
    /// (ISSUES #001; KSP Knowledge Library note ugui-click-blocking-and-canvas-sorting.md).
    /// Sort position is NOT hardcoded: Unity's RaycastComparer compares sorting
    /// layer before sorting order, and KSP's stock canvas layers/orders are
    /// prefab-serialized, so on every activation the blocker adopts the highest
    /// sorting layer in the scene plus one above the top sortingOrder on it
    /// (G1 rework, 2026-09-03). Diagnostics: creation and transitions are
    /// Debug-logged, and while verboseLogging is on the Image carries a faint
    /// red tint so the blocker is visible for in-game verification.
    /// Created lazily on the first SetBlocked(true), on the main thread, and
    /// lives for the session (DontDestroyOnLoad).
    /// </summary>
    internal sealed class PointerBlockerGateway : IPointerBlockerGateway
    {
        private const string GameObjectName = "DearImGuiKSP.PointerBlocker";

        private static readonly Color VerboseTint = new Color(1f, 0.4f, 0.4f, 0.15f);
        private static readonly Color InvisibleTint = new Color(1f, 1f, 1f, 0f);

        private readonly ILogger _log;
        private readonly System.Func<bool> _verboseLogging;

        private GameObject _blocker;
        private Canvas _canvas;
        private Image _image;

        internal PointerBlockerGateway(ILogger log, System.Func<bool> verboseLogging)
        {
            _log = log;
            _verboseLogging = verboseLogging;
        }

        /// <inheritdoc/>
        public void SetBlocked(bool blocked)
        {
            bool justCreated = false;
            if (blocked && _blocker == null)
            {
                // First capture of the session: build it in the desired state.
                _blocker = CreateBlocker();
                _canvas = _blocker.GetComponent<Canvas>();
                _image = _blocker.GetComponent<Image>();
                justCreated = true;
            }

            if (_blocker == null)
            {
                return;
            }

            if (blocked)
            {
                // (Re)adopt the winning sort position: stock mods can add
                // higher-sorted canvases at any time. SetBlocked only fires on
                // capture-state transitions (InputCaptureTracker), so the scene
                // scan here is not a per-frame cost.
                AdoptTopSortOrder();
            }

            if (justCreated)
            {
                _log?.Debug("Pointer blocker created: sortingLayerID " + _canvas.sortingLayerID +
                            ", sortingOrder " + _canvas.sortingOrder + ".");
            }

            if (_blocker.activeSelf != blocked)
            {
                _blocker.SetActive(blocked);
                _log?.Debug("Pointer blocker " + (blocked ? "activated" : "deactivated") + ".");
            }

            // Tint decision re-evaluated on every call: verboseLogging can be
            // toggled at runtime, and the blocker must default to invisible.
            _image.color = _verboseLogging() ? VerboseTint : InvisibleTint;
        }

        private static GameObject CreateBlocker()
        {
            var go = new GameObject(GameObjectName);
            Object.DontDestroyOnLoad(go);

            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;

            // Required so the blocker Image participates in raycasts.
            go.AddComponent<GraphicRaycaster>();

            Image image = go.AddComponent<Image>();
            image.raycastTarget = true;

            RectTransform rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            return go;
        }

        // Unity's EventSystem RaycastComparer sorts graphic raycast results by
        // sorting layer BEFORE sorting order, and the stock canvases' layers and
        // orders are prefab-serialized (invisible to us) — a hardcoded sortOrder
        // (the original G1 defect) can therefore lose to a higher named layer.
        // Adopt the scene's top sorting layer and one above the maximum
        // sortingOrder on that layer; short.MaxValue is the documented ceiling.
        private void AdoptTopSortOrder()
        {
            Canvas[] canvases = Object.FindObjectsOfType<Canvas>();

            int topLayerValue = int.MinValue;
            int topLayerId = 0; // Default layer
            int topOrder = 0;

            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas other = canvases[i];
                if (other == _canvas)
                {
                    continue; // never compete with ourselves
                }

                int layerValue = SortingLayer.GetLayerValueFromID(other.sortingLayerID);
                if (layerValue > topLayerValue ||
                    (layerValue == topLayerValue && other.sortingOrder > topOrder))
                {
                    topLayerValue = layerValue;
                    topLayerId = other.sortingLayerID;
                    topOrder = other.sortingOrder;
                }
            }

            _canvas.sortingLayerID = topLayerId;
            _canvas.sortingOrder = System.Math.Min(topOrder + 1, short.MaxValue);
        }
    }
}
