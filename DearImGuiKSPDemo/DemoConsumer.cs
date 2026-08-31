using UnityEngine;

namespace DearImGuiKSPDemo
{
    /// <summary>
    /// Demo consumer for DearImGui-KSP — ships as a SEPARATE install (GameData/DearImGuiKSPDemo, D7)
    /// so users installing the library as a dependency get no demo UI.
    /// Hosts the example window (AC3): text, a button with click feedback, a slider, and an
    /// input field, all declared per frame through the public C# API. Zero Unity IMGUI.
    /// TODO(milestone 6): add the naive vs virtualized 1000-item list benchmark (AC5, D10).
    /// </summary>
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public sealed class DemoConsumer : MonoBehaviour
    {
        private const string ConsumerId = "DearImGuiKSPDemo";

        private int _clickCount;
        private float _sliderValue = 0.5f;
        private string _inputText = "edit me";

        private void Start()
        {
            if (!DearImGuiKSP.DearImGuiKSP.IsAvailable)
            {
                Debug.Log("[DearImGuiKSPDemo] DearImGui-KSP not available; demo disabled.");
                return;
            }
            DearImGuiKSP.DearImGuiKSP.Register(ConsumerId, OnFrame);
            Debug.Log("[DearImGuiKSPDemo] Registered with DearImGui-KSP.");
        }

        private void OnDestroy()
        {
            DearImGuiKSP.DearImGuiKSP.Unregister(ConsumerId);
        }

        // Per-frame UI declaration — the only place widget calls are valid.
        private void OnFrame()
        {
            if (!DearImGuiKSP.DearImGuiKSP.BeginWindow("DearImGui-KSP Demo"))
            {
                DearImGuiKSP.DearImGuiKSP.EndWindow();
                return;
            }

            DearImGuiKSP.DearImGuiKSP.Text("Hello from the demo consumer!");
            DearImGuiKSP.DearImGuiKSP.Text("Button clicked " + _clickCount + " time(s).");

            if (DearImGuiKSP.DearImGuiKSP.Button("Click me"))
            {
                _clickCount++;
            }

            DearImGuiKSP.DearImGuiKSP.SliderFloat("Slider", ref _sliderValue, 0f, 1f);
            DearImGuiKSP.DearImGuiKSP.InputText("Input", ref _inputText);

            DearImGuiKSP.DearImGuiKSP.EndWindow();
        }
    }
}
