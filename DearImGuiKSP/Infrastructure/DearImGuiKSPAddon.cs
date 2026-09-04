using System;
using System.Collections;
using UnityEngine;

namespace DearImGuiKSP.Infrastructure
{
    /// <summary>
    /// KSP entry point (spec §5.1). Created once at main menu; must DontDestroyOnLoad itself —
    /// the game does not do it for once-addons (KSP Knowledge Library, assembly-loading notes).
    /// Thin shell only: initializes the native bridge, drives the frame loop
    /// (Update → FrameLoopOrchestrator.RunFrame; WaitForEndOfFrame coroutine →
    /// GL.IssuePluginEvent), and runs the lifecycle state machine (C12). All logic
    /// lives in NativeBridge/Composition/Application.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public sealed class DearImGuiKSPAddon : MonoBehaviour
    {
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            Composition.WireApplicationFacade();
            Composition.Logger.Info("DearImGui-KSP loaded. Initializing native bridge.");
        }

        private void Start()
        {
            // Global kill switch (spec §9.1, C11): enabled = false → dormant session
            // (state machine stays Uninitialized).
            if (!Composition.Settings.Enabled)
            {
                Composition.Logger.Info("Disabled by settings.cfg (enabled = false); library dormant for this session.");
                return;
            }

            Composition.StateMachine.MarkInitializing();
            Composition.BridgeInitResult = Composition.Bridge.Initialize();
            if (Composition.BridgeInitResult == NativeBridge.InitOk)
            {
                LoadStartupFont();
                Composition.WireLifecycle();
                Composition.StateMachine.MarkRunning();
                Composition.Logger.Info("Native bridge up; frame loop running.");
            }
            else
            {
                // Terminal failure state (C12) + one plain-language popup (C13, spec §5.4/§7).
                Composition.StateMachine.Fail(
                    NativeBridge.KindForInitResult(Composition.BridgeInitResult),
                    "native bridge init failed with code " + Composition.BridgeInitResult);
                Composition.Logger.Error("Native bridge initialization failed with code " + Composition.BridgeInitResult + "; library inactive for this session.");
            }
            StartCoroutine(RenderEventPump());
        }

        private void Update()
        {
            if (Composition.StateMachine.IsRunning)
            {
                Composition.Orchestrator.RunFrame(Screen.width, Screen.height, Time.deltaTime);
            }
        }

        // Startup font pipeline (C5, spec §5.2): the native atlas is baked on the
        // first frame, so the configured font must be loaded between bridge init
        // and MarkRunning — frames only run once the state machine is Running, and
        // the first BeginUiFrame/NewFrame happens in the first Update after that.
        // Font degradation is log-only (one spec §7 line) and never fails the
        // session (plan invariant 4).
        private void LoadStartupFont()
        {
            string font = Composition.Settings.Font;
            try
            {
                FontResolution resolution = FontResolver.Resolve(font, Composition.Settings.FontScale);

                if (!resolution.UseEmbeddedDefault)
                {
                    bool primaryOk = Composition.Bridge.LoadFontFromFile(resolution.PrimaryPath, resolution.SizePixels);
                    bool secondaryOk = resolution.SecondaryPath == null
                        || Composition.Bridge.LoadFontFromFile(resolution.SecondaryPath, resolution.SizePixels);
                    if (primaryOk && secondaryOk)
                    {
                        return;
                    }
                }
                else if (string.Equals(font, LibraryConfig.EmbeddedFontName, StringComparison.OrdinalIgnoreCase))
                {
                    // Deliberate "ProggyClean" choice: embedded default, no log line
                    // (the spec string table has no variant for the deliberate case).
                    return;
                }
            }
            catch (Exception)
            {
                // Resolution or marshalling must not take the session down.
            }

            Composition.Logger.Info("Font '" + font + "' not found or unreadable; using embedded default font.");
        }

        // Issues the native render event after each frame's rendering is queued, so the
        // backend draws ImGui on top of the completed frame (spec §4.2). Gated on the
        // lifecycle (C12): suspended/failed means no new frames — and no stale redraws.
        private IEnumerator RenderEventPump()
        {
            while (true)
            {
                yield return new WaitForEndOfFrame();
                if (Composition.StateMachine.IsRunning)
                {
                    GL.IssuePluginEvent(Composition.Bridge.RenderEventFunc, 0);
                }
            }
        }
    }
}
