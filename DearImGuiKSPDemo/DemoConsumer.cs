using KSP.UI.Screens;
using UnityEngine;

namespace DearImGuiKSPDemo
{
    /// <summary>
    /// Demo consumer for DearImGui-KSP — ships as a SEPARATE install (GameData/DearImGuiKSPDemo, D7)
    /// so users installing the library as a dependency get no demo UI.
    /// Hosts the example window (AC3): text, a button with click feedback, a slider, and an
    /// input field, all declared per frame through the public C# API, plus the AC5 benchmark
    /// window (naive vs virtualized 1000-item list) with its IMGUI reference window (D10),
    /// and the C13/M4 ImPlot proof window (two live line plots fed by ring buffers),
    /// plus the M5 widget showcase section (C17: spinners, knobs, wheels, and a
    /// cancellable tween demo, drawn by ThemeDemo inside the main window).
    /// All Begin/End pairs are declared through ImGuiEx scopes (C3), including one
    /// "Throw inside scope (test)" fault-barrier test hook, plus a temporary
    /// fault-injection section (F1-F4) for in-game verification of the C01/C02
    /// frame-boundary and tween fault fixes — remove before release.
    /// Toggled via an ApplicationLauncher toolbar button (green placeholder icon).
    /// </summary>
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public sealed class DemoConsumer : MonoBehaviour
    {
        private const string ConsumerId = "DearImGuiKSPDemo";

        private ApplicationLauncherButton _toolbarButton;
        private bool _windowVisible = true;
        private bool _benchmarkVisible;
        private bool _plotVisible;
        private BenchmarkUI _benchmark;
        private PlotDemo _plotDemo;
        private ThemeDemo _themeDemo;
        private int _clickCount;
        private float _sliderValue = 0.5f;
        private string _inputText = "edit me";

        // M3 tuning-pass showcase: the styled gradient buttons pull their stops
        // from the active theme preset (KspPalette is public, but the demo no
        // longer needs its own copies — the style overload reads the preset).
        private bool _themeToggle = true;
        private int _radioChoice;

        // --- Temporary fault-injection test hooks (Gate A verification of the
        // C01/C02 fixes; remove this whole section before release) ---
        private const string FaultProbeId = "DearImGuiKSPDemo.FaultProbe";
        private bool _faultProbeRegistered;
        private bool _pendingOutOfFrameProbe;
        private string _faultStatus = "no fault test run yet";

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
            _plotDemo = new PlotDemo();
            _themeDemo = new ThemeDemo();

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

        // S2 regression probe (temporary): fires a widget call OUTSIDE any
        // registered frame callback — deferred here from a button press so the
        // call site is Update, not OnFrame. Pre-C01 this dereferenced ImGui's
        // null current window and crashed to desktop; the C01 frame gate must
        // turn it into the documented silent no-op.
        private void Update()
        {
            if (!_pendingOutOfFrameProbe)
            {
                return;
            }
            _pendingOutOfFrameProbe = false;
            DearImGuiKSP.DearImGuiKSP.Text("out-of-frame fault probe");
            _faultStatus = "F2 fired: out-of-frame call returned, no crash (expected: silent no-op, nothing drawn)";
            Debug.Log("[DearImGuiKSPDemo] " + _faultStatus);
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
        // All Begin/End pairs run through ImGuiEx scopes (C3): Dispose closes the
        // stack on every exit path, including exceptions (the fault barrier test hook).
        private void OnFrame()
        {
            if (!_windowVisible)
            {
                return;
            }
            using (var window = DearImGuiKSP.ImGuiEx.Window("DearImGui-KSP Demo", autoResize: true))
            {
                if (window.Visible)
                {
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
                    if (DearImGuiKSP.DearImGuiKSP.Button(_plotVisible
                        ? "Hide plot window"
                        : "Show plot window"))
                    {
                        _plotVisible = !_plotVisible;
                    }

                    // C3 test hook (M1 gate): throwing inside a using scope must leave
                    // the style stack symmetric — the scope's Dispose pops the pushed
                    // color during unwind, then the fault barrier logs the exception.
                    if (DearImGuiKSP.DearImGuiKSP.Button("Throw inside scope (test)"))
                    {
                        using (DearImGuiKSP.ImGuiEx.StyleColor(
                            DearImGuiKSP.ImGuiCol.Text, Color.red))
                        {
                            throw new System.InvalidOperationException(
                                "[DearImGuiKSPDemo] Intentional throw inside a scope (test hook).");
                        }
                    }

                    // M3 tuning-pass theme showcase: a consumer-choice colored
                    // header (the theme never auto-colors text), the two themed
                    // gradient button styles, an animated toggle, and circular
                    // radios. The window background gradient is applied
                    // natively by the theme.
                    DearImGuiKSP.DearImGuiKSP.TextColored(
                        DearImGuiKSP.Application.KspPalette.GreenLight, "Theme showcase");
                    if (DearImGuiKSP.ImGuiGradients.GradientButton(
                        "Primary gradient", new Vector2(180f, 28f), DearImGuiKSP.GradientButtonStyle.Primary))
                    {
                        _clickCount++;
                    }
                    if (DearImGuiKSP.ImGuiGradients.GradientButton(
                        "Secondary gradient", new Vector2(180f, 28f), DearImGuiKSP.GradientButtonStyle.Secondary))
                    {
                        _clickCount++;
                    }
                    DearImGuiKSP.DearImGuiKSP.Toggle(
                        "Animated toggle", ref _themeToggle, DearImGuiKSP.ToggleFlags.Animated);
                    DearImGuiKSP.DearImGuiKSP.RadioButton("Radio option 1", ref _radioChoice, 0);
                    DearImGuiKSP.DearImGuiKSP.RadioButton("Radio option 2", ref _radioChoice, 1);

                    // M5 widget showcase (C17): spinner row, knobs, wheels, and
                    // the tween demo — additive to the M3 section above.
                    _themeDemo.DrawImGui();

                    // --- Temporary fault-injection tests (Gate A verification of
                    // the C01/C02 fixes; remove this block before release) ---
                    DearImGuiKSP.DearImGuiKSP.TextColored(
                        DearImGuiKSP.Application.KspPalette.OrangeLight, "Fault injection tests (temporary)");
                    DearImGuiKSP.DearImGuiKSP.Text(_faultStatus);

                    if (DearImGuiKSP.DearImGuiKSP.Button(_faultProbeRegistered
                        ? "F1: Unregister probe consumer mid-callback"
                        : "F1: Register probe consumer mid-callback"))
                    {
                        // S1: mutating the registry from inside a callback froze KSP
                        // pre-C01 (live-list foreach invalidated -> EndUiFrame skipped
                        // -> native frame lock held forever).
                        if (_faultProbeRegistered)
                        {
                            DearImGuiKSP.DearImGuiKSP.Unregister(FaultProbeId);
                            _faultProbeRegistered = false;
                            _faultStatus = "F1: probe unregistered mid-callback; its window should vanish, game alive";
                        }
                        else
                        {
                            DearImGuiKSP.DearImGuiKSP.Register(FaultProbeId, OnFaultProbeFrame);
                            _faultProbeRegistered = true;
                            _faultStatus = "F1: probe registered mid-callback; its window should appear, game alive";
                        }
                        Debug.Log("[DearImGuiKSPDemo] " + _faultStatus);
                    }

                    if (DearImGuiKSP.DearImGuiKSP.Button("F2: Call widget outside a frame callback"))
                    {
                        // Deferred to Update so the call really happens outside OnFrame.
                        _pendingOutOfFrameProbe = true;
                        _faultStatus = "F2 queued: out-of-frame widget call fires from Update next frame";
                    }

                    if (DearImGuiKSP.DearImGuiKSP.Button("F3: Tween setter throws immediately"))
                    {
                        // S3/G3-07: the baseline set(from) inside Tween.To escaped
                        // unguarded pre-C02.
                        DearImGuiKSP.Tween.To(
                            v =>
                            {
                                throw new System.InvalidOperationException(
                                    "[DearImGuiKSPDemo] Intentional baseline setter throw (fault test F3).");
                            },
                            0f, 1f, 1f, DearImGuiKSP.Ease.Linear);
                        _faultStatus = "F3: baseline-throw tween refused; log should show a containment error";
                        Debug.Log("[DearImGuiKSPDemo] " + _faultStatus);
                    }

                    if (DearImGuiKSP.DearImGuiKSP.Button("F4: Tween setter throws mid-tween"))
                    {
                        // S3: a setter throwing inside TweenEngine.Tick rethrew every
                        // frame pre-C02, freezing every consumer's UI for the session.
                        DearImGuiKSP.Tween.To(
                            v =>
                            {
                                if (v >= 0.5f)
                                {
                                    throw new System.InvalidOperationException(
                                        "[DearImGuiKSPDemo] Intentional mid-tween setter throw (fault test F4).");
                                }
                            },
                            0f, 1f, 2f, DearImGuiKSP.Ease.Linear);
                        _faultStatus = "F4: mid-tween throw armed (~1s in); spinners/toggles must keep animating";
                        Debug.Log("[DearImGuiKSPDemo] " + _faultStatus);
                    }
                }
            }
            DrawBenchmarkWindow();
            DrawPlotWindow();
        }

        // Frame callback for the temporary F1 fault probe — registered and
        // unregistered from INSIDE OnFrame to exercise the S1 fix.
        private void OnFaultProbeFrame()
        {
            using (var window = DearImGuiKSP.ImGuiEx.Window("Fault Probe Consumer", autoResize: true))
            {
                if (window.Visible)
                {
                    DearImGuiKSP.DearImGuiKSP.Text("Registered from inside another consumer's callback.");
                }
            }
        }

        // Third window in the same registered callback — one consumer ID, one
        // registration. Skipped entirely (no window scope opened) while hidden.
        private void DrawPlotWindow()
        {
            if (!_plotVisible || _plotDemo == null)
            {
                return;
            }
            using (var window = DearImGuiKSP.ImGuiEx.Window("DearImGui-KSP Plots"))
            {
                if (window.Visible)
                {
                    _plotDemo.DrawImGui();
                }
            }
        }

        // Second window in the same registered callback — still one consumer ID,
        // one registration. Skipped entirely (no window scope opened) while hidden.
        private void DrawBenchmarkWindow()
        {
            if (!_benchmarkVisible || _benchmark == null)
            {
                return;
            }
            using (var window = DearImGuiKSP.ImGuiEx.Window("DearImGui-KSP Benchmark"))
            {
                if (window.Visible)
                {
                    _benchmark.DrawImGui();
                }
            }
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
