using System;
using System.Collections.Generic;
using DearImGuiKSP;
using DearImGuiKSP.Application;
using DearImGuiKSP.Application.Animation;
using DearImGuiKSP.Application.Interfaces;
using Xunit;
using Facade = DearImGuiKSP.DearImGuiKSP;

namespace Application.Tests
{
    /// <summary>
    /// C01 frame-boundary fault model: S1 (registry snapshot iteration +
    /// guaranteed EndUiFrame), S2 (frame-open gate makes out-of-frame widget
    /// calls safe no-ops), G2-05 (open-scope tracking and fault unwind order).
    /// Shares the "FacadeStatics" collection with TweenEngineTests so xUnit
    /// never runs the two classes in parallel — both mutate facade statics.
    /// </summary>
    [Collection("FacadeStatics")]
    public class FrameBoundaryTests
    {
        private sealed class RecordingBridge : INativeBridge
        {
            public int BeginCount;
            public int EndCount;
            private readonly InputCaptureState _captureState = new InputCaptureState();

            public int Initialize() { return 0; }
            public bool LoadFontFromFile(string utf8Path, float sizePixels) { return true; }
            public InputCaptureState GetIoSnapshot() { return _captureState; }
            public void BeginUiFrame(float width, float height, float deltaSeconds) { BeginCount++; }
            public void EndUiFrame() { EndCount++; }
            public void RebuildViewport(int width, int height) { }
            public void ClampWindowsToViewport(float width, float height) { }
            public void Shutdown() { }
        }

        private sealed class RecordingCloser : IScopeCloser
        {
            public readonly List<string> Calls = new List<string>();
            public void EndWindow() { Calls.Add("EndWindow"); }
            public void EndScrollRegion() { Calls.Add("EndScrollRegion"); }
            public void EndTabItem() { Calls.Add("EndTabItem"); }
            public void EndTabBar() { Calls.Add("EndTabBar"); }
            public void EndPlot() { Calls.Add("EndPlot"); }
            public void EndSubplots() { Calls.Add("EndSubplots"); }
            public void PopStyleColor() { Calls.Add("PopStyleColor"); }
            public void PopStyleVar() { Calls.Add("PopStyleVar"); }
        }

        /// <summary>
        /// Builds a Running orchestrator over a recording bridge and wires the
        /// facade statics to it, so consumer callbacks can call the real
        /// Register/Unregister. Dispose restores the null/unset statics.
        /// </summary>
        private sealed class Harness : IDisposable
        {
            internal readonly ConsumerRegistry Registry = new ConsumerRegistry();
            internal readonly RecordingBridge Bridge = new RecordingBridge();
            internal readonly FrameLoopOrchestrator Orchestrator;

            internal Harness()
            {
                var machine = new LifecycleStateMachine(new FakeLogger());
                machine.MarkInitializing();
                machine.MarkRunning();
                var tracker = new InputCaptureTracker(
                    new FakeInputLockGateway(), new FakePointerBlockerGateway(),
                    new FakeImguiEventEaterGateway(), Registry);
                var settings = new SettingsModel(new FakeSettingsStore());
                Orchestrator = new FrameLoopOrchestrator(
                    Bridge, Registry, tracker, new FaultBarrier(new FakeLogger()), machine,
                    new FakeLogger(), settings, new ThemeEngine(settings, new FakeLogger()),
                    new TweenEngine());

                Facade.Log = new FakeLogger();
                Facade.Registry = Registry;
                Facade.Lifecycle = machine;
            }

            public void Dispose()
            {
                Facade.Log = null;
                Facade.Registry = null;
                Facade.Lifecycle = null;
                Facade.FrameOpen = false;
                OpenScopeTracker.Reset();
            }
        }

        [Fact]
        public void Register_InsideCallback_DoesNotThrow_AppliesNextFrame()
        {
            using (var h = new Harness())
            {
                bool bRan = false;
                h.Registry.TryRegister("a", () => Facade.Register("b", () => bRan = true));

                h.Orchestrator.RunFrame(1920f, 1080f, 1f / 60f);
                Assert.False(bRan); // snapshot semantics: not this frame
                Assert.Equal(1, h.Bridge.EndCount);

                h.Orchestrator.RunFrame(1920f, 1080f, 1f / 60f);
                Assert.True(bRan);
                Assert.Equal(2, h.Bridge.EndCount);
            }
        }

        [Fact]
        public void Unregister_InsideCallback_DoesNotThrow_AppliesNextFrame()
        {
            using (var h = new Harness())
            {
                bool bRan = false;
                h.Registry.TryRegister("a", () => Facade.Unregister("b"));
                h.Registry.TryRegister("b", () => bRan = true);

                h.Orchestrator.RunFrame(1920f, 1080f, 1f / 60f);
                Assert.True(bRan); // snapshot semantics: still ran this frame
                Assert.Equal(1, h.Bridge.EndCount);

                bRan = false;
                h.Orchestrator.RunFrame(1920f, 1080f, 1f / 60f);
                Assert.False(bRan);
                Assert.Equal(2, h.Bridge.EndCount);
            }
        }

        [Fact]
        public void EndUiFrame_AlwaysRuns_WhenConsumerCallbackThrows()
        {
            using (var h = new Harness())
            {
                h.Registry.TryRegister("bad", () => { throw new InvalidOperationException("boom"); });

                h.Orchestrator.RunFrame(1920f, 1080f, 1f / 60f);

                Assert.Equal(1, h.Bridge.BeginCount);
                Assert.Equal(1, h.Bridge.EndCount);
                Assert.False(Facade.FrameOpen);
            }
        }

        [Fact]
        public void WidgetCalls_OutsideFrame_AreSafeNoOps()
        {
            using (var h = new Harness())
            {
                // Session is Running but no frame is open (S2). Every one of
                // these would P/Invoke into the native DLL if the guard failed.
                Assert.False(Facade.CanDeclareUi);
                Assert.False(Facade.BeginWindow("w"));
                Assert.False(Facade.Button("x"));
                Facade.Text("x");
                Facade.PushStyleColor(ImGuiCol.Text, new UnityEngine.Color32(1, 2, 3, 4));
                Facade.PopStyleColor();
                Facade.PushStyleVar(ImGuiStyleVar.FrameRounding, 4f);
                Facade.PopStyleVar();
                Assert.False(ImGuiPlot.Begin("t", UnityEngine.Vector2.zero).Visible);
                Assert.False(ImGuiPlot.BeginSubplots("t", 1, 1, UnityEngine.Vector2.zero).Visible);
            }
        }

        [Fact]
        public void Register_WithoutOpenFrame_StillWorks()
        {
            using (var h = new Harness())
            {
                // Registration is session-level, not frame-gated (consumers
                // register from their own addon Start/Update).
                Facade.Register("x", () => { });
                Assert.Single(h.Registry.Ordered);
            }
        }

        [Fact]
        public void WidgetGate_OpenDuringCallbacks_ClosedAfter()
        {
            using (var h = new Harness())
            {
                bool? during = null;
                h.Registry.TryRegister("a", () => during = Facade.CanDeclareUi);

                h.Orchestrator.RunFrame(1920f, 1080f, 1f / 60f);

                Assert.True(during);
                Assert.False(Facade.FrameOpen);
                Assert.False(Facade.CanDeclareUi);
            }
        }

        [Fact]
        public void Unwind_ClosesScopes_InnermostFirst()
        {
            OpenScopeTracker.Reset();
            OpenScopeTracker.Windows = 1;
            OpenScopeTracker.ScrollRegions = 1;
            OpenScopeTracker.TabBars = 1;
            OpenScopeTracker.TabItems = 1;
            OpenScopeTracker.Plots = 1;
            OpenScopeTracker.Subplots = 1;
            OpenScopeTracker.StyleColors = 2;
            OpenScopeTracker.StyleVars = 1;

            Facade.FrameOpen = true;
            try
            {
                var closer = new RecordingCloser();
                Facade.UnwindOpenScopes(closer);

                Assert.Equal(
                    new[]
                    {
                        "PopStyleVar", "PopStyleColor", "PopStyleColor",
                        "EndPlot", "EndSubplots", "EndTabItem", "EndTabBar",
                        "EndScrollRegion", "EndWindow"
                    },
                    closer.Calls);
                Assert.Equal(0, OpenScopeTracker.TotalOpen);
            }
            finally
            {
                Facade.FrameOpen = false;
                OpenScopeTracker.Reset();
            }
        }

        [Fact]
        public void Unwind_WithoutOpenFrame_LeavesCountersUntouched()
        {
            OpenScopeTracker.Reset();
            OpenScopeTracker.Windows = 2;
            Facade.FrameOpen = false;

            var closer = new RecordingCloser();
            Facade.UnwindOpenScopes(closer);

            Assert.Empty(closer.Calls);
            Assert.Equal(2, OpenScopeTracker.Windows);
            OpenScopeTracker.Reset();
        }
    }
}
