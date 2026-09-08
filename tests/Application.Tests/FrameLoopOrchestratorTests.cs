using System.Collections.Generic;
using DearImGuiKSP;
using DearImGuiKSP.Application;
using DearImGuiKSP.Application.Animation;
using Xunit;

namespace Application.Tests
{
    /// <summary>
    /// Covers the G3-rework viewport-clamp detection in FrameLoopOrchestrator:
    /// the clamp is driven by the live (width, height) fed to RunFrame, fires
    /// only on size change (never on the first observed frame), only when the
    /// clampWindowsToViewport setting is on, and passes the NEW size through.
    /// Also covers the C14 tween tick: it runs inside RunFrame before consumer
    /// callbacks, so a tween does not advance across frames where the lifecycle
    /// is not Running (suspension is a pause, spec §5.4).
    /// Shares the "FacadeStatics" collection: RunFrame writes the facade's
    /// FrameOpen static (C01), so this class must not run in parallel with the
    /// other classes that depend on facade statics (C07 found the race).
    /// </summary>
    [Collection("FacadeStatics")]
    public class FrameLoopOrchestratorTests
    {
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
            // Real DockingModeApplier (ISSUES #011): same contract — no docking
            // change fires here, so ApplyIfDirty never reaches native code.
            var dockingApplier = new DockingModeApplier(settings, new FakeLogger());
            return new FrameLoopOrchestrator(
                bridge, registry, tracker, new FaultBarrier(new FakeLogger()), machine,
                new FakeLogger(), settings, themeEngine, dockingApplier, new TweenEngine());
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

        [Fact]
        public void Tween_DoesNotAdvance_WhileLifecycleNotRunning()
        {
            var bridge = new FakeNativeBridge();
            SettingsModel settings = CreateSettings(true);
            var tweenEngine = new TweenEngine();
            var registry = new ConsumerRegistry();
            var tracker = new InputCaptureTracker(
                new FakeInputLockGateway(), new FakePointerBlockerGateway(),
                new FakeImguiEventEaterGateway(), registry);
            var machine = new LifecycleStateMachine(new FakeLogger());
            machine.MarkInitializing();
            machine.MarkRunning();
            var themeEngine = new ThemeEngine(settings, new FakeLogger());
            var orchestrator = new FrameLoopOrchestrator(
                bridge, registry, tracker, new FaultBarrier(new FakeLogger()), machine,
                new FakeLogger(), settings, themeEngine,
                new DockingModeApplier(settings, new FakeLogger()), tweenEngine);

            float value = -1f;
            TweenHandle handle = tweenEngine.StartFloat(v => value = v, 0f, 10f, 1f, Ease.Linear);
            orchestrator.RunFrame(1920f, 1080f, 0.5f);
            Assert.True(handle.IsPlaying);
            Assert.Equal(5f, value);

            // Suspension: RunFrame returns early, so the tween sees no ticks.
            machine.SetLoading(true);
            orchestrator.RunFrame(1920f, 1080f, 0.5f);
            Assert.Equal(5f, value);

            // Resume: the same frame loop path advances it again, and the tween
            // completes with the exact target on this tick.
            machine.SetLoading(false);
            orchestrator.RunFrame(1920f, 1080f, 0.5f);
            Assert.Equal(10f, value);
            Assert.False(handle.IsPlaying);
        }
    }
}
