using UnityEngine;

namespace DearImGuiKSPDemo
{
    /// <summary>
    /// Demo consumer for DearImGui-KSP — ships as a SEPARATE install (GameData/DearImGuiKSPDemo, D7)
    /// so users installing the library as a dependency get no demo UI.
    /// Hosts the example window (AC3) and the torture-test benchmark UI (AC5, D10).
    /// TODO(milestone 3): register with DearImGuiKSP and build the example window via the C# API.
    /// TODO(milestone 6): add the naive vs virtualized 1000-item list benchmark.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public sealed class DemoConsumer : MonoBehaviour
    {
        private void Awake()
        {
            Debug.Log("[DearImGuiKSPDemo] Demo consumer loaded (skeleton).");
        }
    }
}
