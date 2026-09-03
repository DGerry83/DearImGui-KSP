using KSP.UI.Screens;
using UnityEngine;

namespace DearImGuiKSPDemo
{
    /// <summary>
    /// Demo consumer for DearImGui-KSP — ships as a SEPARATE install (GameData/DearImGuiKSPDemo, D7)
    /// so users installing the library as a dependency get no demo UI.
    /// Hosts the example window (AC3): text, a button with click feedback, a slider, and an
    /// input field, all declared per frame through the public C# API, plus the AC5 benchmark
    /// window (naive vs virtualized 1000-item list) with its IMGUI reference window (D10).
    /// Toggled via an ApplicationLauncher toolbar button (green placeholder icon).
    /// </summary>
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public sealed class DemoConsumer : MonoBehaviour
    {
        private const string ConsumerId = "DearImGuiKSPDemo";

        private ApplicationLauncherButton _toolbarButton;
        private bool _windowVisible = true;
        private bool _benchmarkVisible;
        private BenchmarkUI _benchmark;
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

            _benchmark = new BenchmarkUI();

            GameEvents.onGUIApplicationLauncherReady.Add(OnLauncherReady);
            if (ApplicationLauncher.Ready)
            {
                OnLauncherReady();
            }
        }

        private void OnDestroy()
        {
            GameEvents.onGUIApplicationLauncherReady.Remove(OnLauncherReady);
            if (_toolbarButton != null && ApplicationLauncher.Instance != null)
            {
                ApplicationLauncher.Instance.RemoveModApplication(_toolbarButton);
                _toolbarButton = null;
            }
            DearImGuiKSP.DearImGuiKSP.Unregister(ConsumerId);
        }

        private void OnLauncherReady()
        {
            if (_toolbarButton != null)
            {
                return;
            }
            _toolbarButton = ApplicationLauncher.Instance.AddModApplication(
                OnToolbarOn, OnToolbarOff,
                null, null, null, null,
                ApplicationLauncher.AppScenes.ALWAYS,
                MakePlaceholderIcon());
            _toolbarButton.SetTrue(false); // window starts visible; reflect it without firing the callback
        }

        private void OnToolbarOn()
        {
            _windowVisible = true;
        }

        private void OnToolbarOff()
        {
            _windowVisible = false;
        }

        // Solid-green 38x38 placeholder icon (stock toolbar icon size).
        private static Texture2D MakePlaceholderIcon()
        {
            Texture2D tex = new Texture2D(38, 38, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[38 * 38];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.green;
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        // Per-frame UI declaration — the only place widget calls are valid.
        private void OnFrame()
        {
            if (!_windowVisible)
            {
                return;
            }
            if (!DearImGuiKSP.DearImGuiKSP.BeginWindow("DearImGui-KSP Demo"))
            {
                DearImGuiKSP.DearImGuiKSP.EndWindow();
                DrawBenchmarkWindow();
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

            // MVP has no checkbox — button-toggle is the pattern.
            if (DearImGuiKSP.DearImGuiKSP.Button(_benchmarkVisible
                ? "Hide benchmark window"
                : "Show benchmark window"))
            {
                _benchmarkVisible = !_benchmarkVisible;
            }

            DearImGuiKSP.DearImGuiKSP.EndWindow();
            DrawBenchmarkWindow();
        }

        // Second window in the same registered callback — still one consumer ID,
        // one registration. Skipped entirely (no BeginWindow) while hidden.
        private void DrawBenchmarkWindow()
        {
            if (!_benchmarkVisible || _benchmark == null)
            {
                return;
            }
            if (DearImGuiKSP.DearImGuiKSP.BeginWindow("DearImGui-KSP Benchmark"))
            {
                _benchmark.DrawImGui();
            }
            DearImGuiKSP.DearImGuiKSP.EndWindow();
        }

        // Unity IMGUI callback — hosts the IMGUI reference window so the AC5
        // side-by-side comparison shows alongside the benchmark window.
        private void OnGUI()
        {
            if (!_windowVisible || !_benchmarkVisible || _benchmark == null)
            {
                return;
            }
            _benchmark.OnGUIReference();
        }
    }
}
