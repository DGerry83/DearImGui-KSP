using System.Collections;
using UnityEngine;

namespace DearImGuiKSP.Infrastructure
{
    /// <summary>
    /// KSP entry point (spec §5.1). Created once at main menu; must DontDestroyOnLoad itself —
    /// the game does not do it for once-addons (KSP Knowledge Library, assembly-loading notes).
    /// Thin shell only: initializes the native bridge and drives the frame loop
    /// (Update → FrameLoopOrchestrator.RunFrame; WaitForEndOfFrame coroutine →
    /// GL.IssuePluginEvent). All logic lives in NativeBridge/Composition/Application;
    /// failures are log-only for now (state machine + popup arrive in C12/C13).
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
            Composition.BridgeInitResult = Composition.Bridge.Initialize();
            if (Composition.BridgeInitResult == NativeBridge.InitOk)
            {
                Composition.MarkAvailable();
                Composition.Logger.Info("Native bridge up; frame loop running.");
            }
            else
            {
                Composition.Logger.Error("Native bridge initialization failed with code " + Composition.BridgeInitResult + "; library inactive for this session.");
            }
            StartCoroutine(RenderEventPump());
        }

        private void Update()
        {
            if (Composition.BridgeInitResult == NativeBridge.InitOk)
            {
                Composition.Orchestrator.RunFrame(Screen.width, Screen.height, Time.deltaTime);
            }
        }

        // Issues the native render event after each frame's rendering is queued, so the
        // backend draws ImGui on top of the completed frame (spec §4.2).
        private IEnumerator RenderEventPump()
        {
            while (true)
            {
                yield return new WaitForEndOfFrame();
                if (Composition.BridgeInitResult == NativeBridge.InitOk)
                {
                    GL.IssuePluginEvent(Composition.Bridge.RenderEventFunc, 0);
                }
            }
        }
    }
}
