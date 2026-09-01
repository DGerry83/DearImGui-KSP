using System;
using KSP.UI.Screens;
using UnityEngine;

namespace DearImGuiKSPDemo
{
    /// <summary>
    /// Demo consumer for DearImGui-KSP — ships as a SEPARATE install (GameData/DearImGuiKSPDemo, D7)
    /// so users installing the library as a dependency get no demo UI.
    /// Hosts the example window (AC3): text, a button with click feedback, a slider, and an
    /// input field, all declared per frame through the public C# API. Zero Unity IMGUI.
    /// Toggled via an ApplicationLauncher toolbar button (green placeholder icon).
    /// TODO(milestone 6): add the naive vs virtualized 1000-item list benchmark (AC5, D10).
    /// </summary>
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public sealed class DemoConsumer : MonoBehaviour
    {
        private const string ConsumerId = "DearImGuiKSPDemo";

        // TEMP(C10-verification): always-throwing consumer for AC9 in-game verification
        // (auto-disabled after 5 consecutive throwing frames; demo window must keep
        // rendering). Removed once AC9 passes.
        private const string FaultProbeId = "DearImGuiKSPDemoFaultProbe";

        private ApplicationLauncherButton _toolbarButton;
        private bool _windowVisible = true;
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
            DearImGuiKSP.DearImGuiKSP.Register(FaultProbeId, FaultProbe); // TEMP(C10-verification)
            Debug.Log("[DearImGuiKSPDemo] Registered with DearImGui-KSP.");

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
            DearImGuiKSP.DearImGuiKSP.Unregister(FaultProbeId); // TEMP(C10-verification)
        }

        // TEMP(C10-verification): throws every frame; expect 5 logged exceptions then an
        // auto-disable notice, with the demo window unaffected. Removed once AC9 passes.
        private void FaultProbe()
        {
            throw new InvalidOperationException("AC9 fault-injection probe (intentional).");
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
