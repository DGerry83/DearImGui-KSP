using KSP.UI.Screens;
using UnityEngine;

namespace DearImGuiKSPDemo.Telemetry
{
    /// <summary>
    /// M6 telemetry showcase entry (spec §5.5; C18, Graphs tab C19): a SEPARATE
    /// KSPAddon from DemoConsumer, with its own ApplicationLauncher toolbar button
    /// (blue placeholder icon), its own consumer registration id, and one window
    /// ("DearImGui-KSP Telemetry") hosting a tab bar with the Graphs panel (C19),
    /// the Stages panel (C20), and the Orbit panel (C21). Sampling runs
    /// before the visibility check so history stays warm while the window is closed.
    /// All ImGui calls go through the library's public API only; every label/title is
    /// a constant, so the per-frame path allocates no managed memory (the Graphs
    /// hover readout is the documented user-driven exception, see GraphPanel).
    /// </summary>
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public sealed class TelemetryAddon : MonoBehaviour
    {
        private const string ConsumerId = "DearImGuiKSPDemo.Telemetry";
        private const string WindowTitle = "DearImGui-KSP Telemetry";
        private const string TabBarId = "TelemetryTabs";
        private const string GraphsTab = "Graphs";
        private const string StagesTab = "Stages";
        private const string OrbitTab = "Orbit";
        private const string NoVesselText = "No active vessel.";

        private readonly TelemetrySampler _sampler = new TelemetrySampler();
        private readonly StageAnalyzer _stageAnalyzer = new StageAnalyzer();
        private readonly GraphPanel _graphPanel;
        private readonly StagePanel _stagePanel;
        private readonly OrbitPanel _orbitPanel;

        private ApplicationLauncherButton _toolbarButton;
        private bool _windowVisible;

        public TelemetryAddon()
        {
            _graphPanel = new GraphPanel(_sampler);
            _stagePanel = new StagePanel(_stageAnalyzer);
            _orbitPanel = new OrbitPanel();
        }

        private void Start()
        {
            if (!DearImGuiKSP.DearImGuiKSP.IsAvailable)
            {
                Debug.Log("[DearImGuiKSPDemo] DearImGui-KSP not available; telemetry addon disabled.");
                return;
            }
            DearImGuiKSP.DearImGuiKSP.Register(ConsumerId, OnFrame);
            Debug.Log("[DearImGuiKSPDemo] Telemetry registered with DearImGui-KSP.");
            _stageAnalyzer.Subscribe();

            GameEvents.onGUIApplicationLauncherReady.Add(OnLauncherReady);
            if (ApplicationLauncher.Ready)
            {
                OnLauncherReady();
            }
        }

        private void OnDestroy()
        {
            _stageAnalyzer.Dispose();
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
        }

        private void OnToolbarOn()
        {
            _windowVisible = true;
        }

        private void OnToolbarOff()
        {
            _windowVisible = false;
        }

        // Solid-blue 38x38 placeholder icon (stock toolbar icon size) — distinct from
        // DemoConsumer's green button so the two demo windows are easy to tell apart.
        private static Texture2D MakePlaceholderIcon()
        {
            Texture2D tex = new Texture2D(38, 38, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[38 * 38];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.blue;
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        // Per-frame path. Sampling runs first and unconditionally: history stays warm
        // while the window is closed (spec §5.5). The stage analyzer's Tick follows
        // the same rule — its only per-frame cost is a dirty-flag check and a float
        // comparison; part iteration happens inside the 1 s recompute (C20). All tab
        // labels and placeholder strings are constants — no string building, no
        // per-frame allocation (the Graphs hover readout is hover-only; see
        // GraphPanel; StagePanel preformats its readouts at recompute cadence).
        private void OnFrame()
        {
            _sampler.Sample();
            _stageAnalyzer.Tick();
            if (!_windowVisible)
            {
                return;
            }

            using (var window = DearImGuiKSP.ImGuiEx.Window(WindowTitle, autoResize: true))
            {
                if (!window.Visible)
                {
                    return;
                }

                using (var tabBar = DearImGuiKSP.ImGuiEx.TabBar(TabBarId))
                {
                    if (!tabBar.Visible)
                    {
                        return;
                    }

                    using (var tab = DearImGuiKSP.ImGuiEx.TabItem(GraphsTab))
                    {
                        if (tab.Visible)
                        {
                            if (FlightGlobals.ActiveVessel == null)
                            {
                                DearImGuiKSP.DearImGuiKSP.Text(NoVesselText);
                            }
                            else
                            {
                                _graphPanel.DrawImGui();
                            }
                        }
                    }
                    using (var tab = DearImGuiKSP.ImGuiEx.TabItem(StagesTab))
                    {
                        if (tab.Visible)
                        {
                            _stagePanel.DrawImGui();
                        }
                    }
                    using (var tab = DearImGuiKSP.ImGuiEx.TabItem(OrbitTab))
                    {
                        if (tab.Visible)
                        {
                            if (FlightGlobals.ActiveVessel == null)
                            {
                                DearImGuiKSP.DearImGuiKSP.Text(NoVesselText);
                            }
                            else
                            {
                                _orbitPanel.DrawImGui();
                            }
                        }
                    }
                }
            }
        }
    }
}
