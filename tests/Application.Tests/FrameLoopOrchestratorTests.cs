using System.Collections.Generic;
using DearImGuiKSP.Application;
using DearImGuiKSP.Application.Interfaces;
using Xunit;

namespace Application.Tests
{
    /// <summary>
    /// Covers the G3-rework viewport-clamp detection in FrameLoopOrchestrator:
    /// the clamp is driven by the live (width, height) fed to RunFrame, fires
    /// only on size change (never on the first observed frame), only when the
    /// clampWindowsToViewport setting is on, and passes the NEW size through.
    /// </summary>
    public class FrameLoopOrchestratorTests
    {
        /// <summary>Records ClampWindowsToViewport calls with their arguments.</summary>
        private sealed class FakeNativeBridge : INativeBridge
        {
            public readonly List<KeyValuePair<float, float>> ClampCalls =
                new List<KeyValuePair<float, float>>();

            private readonly InputCaptureState _captureState = new InputCaptureState();

            public int Initialize() { return 0; }
            public bool LoadFontFromFile(string utf8Path, float sizePixels) { return true; }
            public InputCaptureState GetIoSnapshot() { return _captureState; }
            public void BeginUiFrame(float width, float height, float deltaSeconds) { }
            public void EndUiFrame() { }
            public void RebuildViewport(int width, int height) { }

            public void ClampWindowsToViewport(float width, float height)
            {
                ClampCalls.Add(new KeyValuePair<float, float>(width, height));
            }

            public void Shutdown() { }
        }

        private static FrameLoopOrchestrator CreateOrchestrator(
            FakeNativeBridge bridge,
            SettingsModel settings)
        {
            var registry = new ConsumerRegistry();
            var tracker = new InputCaptureTracker(
                new FakeInputLockGateway(), new FakePointerBlockerGateway(),
                new FakeImguiEventEaterGateway(), registry);
            var machine = new LifecycleStateMachine(new FakeLogger());
            machine.MarkInitializing();
            machine.MarkRunning();
            // Real ThemeEngine over the test settings: no theme change fires in
            // these tests, so ApplyIfDirty never reaches native code.
            var themeEngine = new ThemeEngine(settings, new FakeLogger());
            return new FrameLoopOrchestrator(
                bridge, registry, tracker, new FaultBarrier(new FakeLogger()), machine,
                new FakeLogger(), settings, themeEngine);
        }

        private static SettingsModel CreateSettings(bool clampWindowsToViewport)
        {
            var settings = new SettingsModel(new FakeSettingsStore());
            settings.ClampWindowsToViewport = clampWindowsToViewport;
            return settings;
        }

        [Fact]
        public void FirstObservedFrame_DoesNotClamp()
        {
            var bridge = new FakeNativeBridge();
            FrameLoopOrchestrator orchestrator =
                CreateOrchestrator(bridge, CreateSettings(true));

            orchestrator.RunFrame(1920f, 1080f, 1f / 60f);

            Assert.Empty(bridge.ClampCalls);
        }

        [Fact]
        public void SizeChange_WithSettingOn_ClampsOnce_WithNewSize()
        {
            var bridge = new FakeNativeBridge();
            FrameLoopOrchestrator orchestrator =
                CreateOrchestrator(bridge, CreateSettings(true));

            orchestrator.RunFrame(1920f, 1080f, 1f / 60f);
            orchestrator.RunFrame(1280f, 720f, 1f / 60f);
            orchestrator.RunFrame(1280f, 720f, 1f / 60f); // unchanged: no second clamp

            Assert.Single(bridge.ClampCalls);
            Assert.Equal(1280f, bridge.ClampCalls[0].Key);
            Assert.Equal(720f, bridge.ClampCalls[0].Value);
        }

        [Fact]
        public void SizeChange_WithSettingOff_DoesNotClamp()
        {
            var bridge = new FakeNativeBridge();
            FrameLoopOrchestrator orchestrator =
                CreateOrchestrator(bridge, CreateSettings(false));

            orchestrator.RunFrame(1920f, 1080f, 1f / 60f);
            orchestrator.RunFrame(1280f, 720f, 1f / 60f);

            Assert.Empty(bridge.ClampCalls);
        }

        [Fact]
        public void SizeUnchanged_WithSettingOn_DoesNotClamp()
        {
            var bridge = new FakeNativeBridge();
            FrameLoopOrchestrator orchestrator =
                CreateOrchestrator(bridge, CreateSettings(true));

            for (int i = 0; i < 5; i++)
            {
                orchestrator.RunFrame(1920f, 1080f, 1f / 60f);
            }

            Assert.Empty(bridge.ClampCalls);
        }
    }
}
