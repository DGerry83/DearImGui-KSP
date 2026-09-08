using System;
using DearImGuiKSP;
using DearImGuiKSP.Application;
using Xunit;
using Facade = DearImGuiKSP.DearImGuiKSP;

namespace Application.Tests
{
    /// <summary>
    /// ISSUES #011 facade guards: every docking facade method must be a safe
    /// no-op outside a registered callback (the C01 frame-open gate), never an
    /// exception — same contract as the widget guards (WidgetGuardTests). With
    /// the gate closed the calls return before any P/Invoke; the out-parameters
    /// of DockBuilderSplitNode are assigned 0. Shares the "FacadeStatics"
    /// collection: these tests mutate facade statics and must not run in
    /// parallel with the other classes that do.
    /// </summary>
    [Collection("FacadeStatics")]
    public class DockingFacadeGuardTests
    {
        private sealed class Harness : IDisposable
        {
            internal Harness(bool frameOpen)
            {
                var machine = new LifecycleStateMachine(new FakeLogger());
                machine.MarkInitializing();
                machine.MarkRunning();

                Facade.Lifecycle = machine;
                Facade.FrameOpen = frameOpen;
                OpenScopeTracker.Reset();
            }

            public void Dispose()
            {
                Facade.Lifecycle = null;
                Facade.FrameOpen = false;
                OpenScopeTracker.Reset();
            }
        }

        [Fact]
        public void GateClosed_AllDockingMethods_AreSafeNoOps()
        {
            using (new Harness(frameOpen: false))
            {
                // No lifecycle/frame: CanDeclareUi is false. Every call must
                // return its documented "unavailable" value without throwing.
                Assert.Equal(0u, Facade.DockSpace(1, UnityEngine.Vector2.one));
                Assert.Equal(0u, Facade.DockSpaceOverViewport());
                Assert.Equal(0u, Facade.DockBuilderAddNode());
                Facade.DockBuilderRemoveNode(1);              // must not throw
                Facade.DockBuilderSetNodeSize(1, UnityEngine.Vector2.one);
                uint atDir, atOpposite;
                Assert.Equal(0u, Facade.DockBuilderSplitNode(1, ImGuiDir.Left, 0.5f, out atDir, out atOpposite));
                Assert.Equal(0u, atDir);
                Assert.Equal(0u, atOpposite);
                Facade.DockBuilderDockWindow("Window", 1);
                Facade.DockBuilderFinish(1);
                Assert.Equal(0u, Facade.DockBuilderGetCentralNode(1));
                Assert.False(Facade.BeginWindow("Docking guard test", autoResize: true, noDocking: true));
            }
        }

        [Fact]
        public void GateClosed_NoLifecycle_AllDockingMethods_AreSafeNoOps()
        {
            // Session unavailable entirely (no lifecycle): same no-op contract.
            Facade.Lifecycle = null;
            Facade.FrameOpen = true;
            try
            {
                Assert.Equal(0u, Facade.DockSpaceOverViewport(flags: ImGuiDockNodeFlags.PassthruCentralNode));
                uint atDir, atOpposite;
                Assert.Equal(0u, Facade.DockBuilderSplitNode(9, ImGuiDir.Down, 0.25f, out atDir, out atOpposite));
                Assert.Equal(0u, atDir);
                Assert.Equal(0u, atOpposite);
            }
            finally
            {
                Facade.FrameOpen = false;
            }
        }
    }
}
