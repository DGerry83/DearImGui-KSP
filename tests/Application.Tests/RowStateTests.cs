using System;
using DearImGuiKSP;
using DearImGuiKSP.Application;
using DearImGuiKSP.Application.Animation;
using DearImGuiKSP.Application.Interfaces;
using Xunit;
using Facade = DearImGuiKSP.DearImGuiKSP;

namespace Application.Tests
{
    /// <summary>
    /// FR-1 row state (1.3.0): TryPush/Pop/Reset semantics, the widget-hook
    /// decision (first item never separated, every later item is), nested-row
    /// rejection, the frame-open reset (FrameLoopOrchestrator) and the fault
    /// unwind backstop (UnwindOpenScopes), plus the ImGuiEx.Row factories
    /// (managed-only scopes — no P/Invoke, so the real factories are testable).
    /// Shares the "FacadeStatics" collection: the factory tests mutate facade
    /// statics (FrameOpen/Lifecycle) and the reset tests mutate RowState.
    /// Note: the nested-row Debug.Assert in ImGuiEx.Row(float) is deliberately
    /// not triggered here — Debug test runs would fail on the assert dialog;
    /// nested rejection is covered through RowState.TryPush directly.
    /// </summary>
    [Collection("FacadeStatics")]
    public class RowStateTests : IDisposable
    {
        public RowStateTests()
        {
            RowState.Reset();
        }

        public void Dispose()
        {
            RowState.Reset();
            Facade.FrameOpen = false;
        }

        private sealed class Harness : IDisposable
        {
            internal Harness()
            {
                var machine = new LifecycleStateMachine(new FakeLogger());
                machine.MarkInitializing();
                machine.MarkRunning();

                Facade.Log = new FakeLogger();
                Facade.Lifecycle = machine;
                Facade.FrameOpen = true;
            }

            public void Dispose()
            {
                Facade.Log = null;
                Facade.Lifecycle = null;
                Facade.FrameOpen = false;
                RowState.Reset();
            }
        }

        // ---- TryPush / Pop / Reset ----

        [Fact]
        public void TryPush_WhenNoRowOpen_SucceedsAndRecordsSpacing()
        {
            Assert.True(RowState.TryPush(RowState.StyleDefaultSpacing));

            Assert.Equal(1, RowState.Depth);
            Assert.Equal(0, RowState.ItemsInCurrentRow);
            Assert.Equal(-1f, RowState.CurrentSpacing); // style ItemSpacing.x (scaled)
        }

        [Fact]
        public void TryPush_ExplicitSpacing_StoredVerbatim()
        {
            Assert.True(RowState.TryPush(8f));
            Assert.Equal(8f, RowState.CurrentSpacing); // pixels, NOT ui-scale-adjusted
        }

        [Fact]
        public void TryPush_NestedRow_ReturnsFalseAndChangesNothing()
        {
            Assert.True(RowState.TryPush(4f));

            Assert.False(RowState.TryPush(2f));

            Assert.Equal(1, RowState.Depth);
            Assert.Equal(0, RowState.ItemsInCurrentRow);
            Assert.Equal(4f, RowState.CurrentSpacing);
        }

        [Fact]
        public void Pop_ClosesRowAndClearsSpacing()
        {
            RowState.TryPush(8f);
            RowState.OnRowItem();
            RowState.OnRowItem();

            RowState.Pop();

            Assert.Equal(0, RowState.Depth);
            Assert.Equal(0, RowState.ItemsInCurrentRow);
            Assert.Equal(-1f, RowState.CurrentSpacing);
        }

        [Fact]
        public void Pop_WithoutOpenRow_IsSafeNoOp()
        {
            RowState.Pop(); // idempotent — Dispose may run on any path
            Assert.Equal(0, RowState.Depth);
        }

        [Fact]
        public void Reset_ZeroesEverything()
        {
            RowState.TryPush(8f);
            RowState.OnRowItem();

            RowState.Reset();

            Assert.Equal(0, RowState.Depth);
            Assert.Equal(0, RowState.ItemsInCurrentRow);
            Assert.Equal(-1f, RowState.CurrentSpacing);
        }

        // ---- Widget-hook decision: first item never SameLine'd ----

        [Fact]
        public void OnRowItem_OutsideRow_NeverSeparates()
        {
            Assert.False(RowState.OnRowItem());
            Assert.False(RowState.OnRowItem());
            Assert.Equal(0, RowState.ItemsInCurrentRow); // untouched outside a row
        }

        [Fact]
        public void OnRowItem_FirstItem_NotSeparated_EveryLaterItem_Is()
        {
            RowState.TryPush(RowState.StyleDefaultSpacing);

            Assert.False(RowState.OnRowItem()); // first item keeps the vertical cursor
            Assert.True(RowState.OnRowItem());
            Assert.True(RowState.OnRowItem());
            Assert.Equal(3, RowState.ItemsInCurrentRow);
        }

        [Fact]
        public void OnRowItem_AfterPop_StopsSeparating()
        {
            RowState.TryPush(RowState.StyleDefaultSpacing);
            RowState.OnRowItem();
            RowState.Pop();

            Assert.False(RowState.OnRowItem());
        }

        // ---- ImGuiEx.Row factories (managed-only; real factories, no native) ----

        [Fact]
        public void Row_InsideFrame_PushesAndDisposePops()
        {
            using (var h = new Harness())
            {
                var scope = ImGuiEx.Row();
                Assert.Equal(1, RowState.Depth);
                Assert.Equal(-1f, RowState.CurrentSpacing);

                scope.Dispose();
                Assert.Equal(0, RowState.Depth);
            }
        }

        [Fact]
        public void Row_ExplicitSpacing_PushedVerbatim()
        {
            using (var h = new Harness())
            {
                using (ImGuiEx.Row(12f))
                {
                    Assert.Equal(12f, RowState.CurrentSpacing);
                }
            }
        }

        [Fact]
        public void Row_MultipleIndependentRows_SequentialPushPop()
        {
            using (var h = new Harness())
            {
                using (ImGuiEx.Row())
                {
                    Assert.Equal(1, RowState.Depth);
                }
                using (ImGuiEx.Row())
                {
                    Assert.Equal(1, RowState.Depth);
                }
                Assert.Equal(0, RowState.Depth);
            }
        }

        [Fact]
        public void Row_DisposeTwice_IsSafe()
        {
            using (var h = new Harness())
            {
                var scope = ImGuiEx.Row();
                scope.Dispose();
                scope.Dispose(); // using-on-throw paths can double-run nothing harmful
                Assert.Equal(0, RowState.Depth);
            }
        }

        [Fact]
        public void Row_WithoutOpenFrame_ReturnsInertScopeWithoutTouchingState()
        {
            using (var h = new Harness())
            {
                Facade.FrameOpen = false; // availability race: safe no-op, never throws

                var scope = ImGuiEx.Row(6f);

                Assert.Equal(0, RowState.Depth);
                scope.Dispose();
                Assert.Equal(0, RowState.Depth);
            }
        }

        [Fact]
        public void Row_UnavailableSession_ReturnsInertScopeWithoutTouchingState()
        {
            using (var h = new Harness())
            {
                Facade.Lifecycle = null; // IsAvailable false

                using (ImGuiEx.Row())
                {
                    Assert.Equal(0, RowState.Depth);
                }
                Assert.Equal(0, RowState.Depth);
            }
        }

        // ---- Backstops: frame-open reset + fault unwind ----

        [Fact]
        public void FrameOpen_ResetClearsUndisposedRowState()
        {
            var registry = new ConsumerRegistry();
            var bridge = new FakeNativeBridge();
            var tracker = new InputCaptureTracker(
                new FakeInputLockGateway(), new FakePointerBlockerGateway(),
                new FakeImguiEventEaterGateway(), registry);
            var machine = new LifecycleStateMachine(new FakeLogger());
            machine.MarkInitializing();
            machine.MarkRunning();
            var settings = new SettingsModel(new FakeSettingsStore());
            var orchestrator = new FrameLoopOrchestrator(
                bridge, registry, tracker, new FaultBarrier(new FakeLogger()), machine,
                new FakeLogger(), settings, new ThemeEngine(settings, new FakeLogger()),
                new DockingModeApplier(settings, new FakeLogger()), new TweenEngine());

            try
            {
                // A consumer that opens a row and never disposes it (misuse).
                RowState.TryPush(3f);
                RowState.OnRowItem();
                RowState.OnRowItem();

                orchestrator.RunFrame(1920f, 1080f, 1f / 60f);

                Assert.Equal(0, RowState.Depth);
                Assert.Equal(0, RowState.ItemsInCurrentRow);
            }
            finally
            {
                RowState.Reset();
            }
        }

        [Fact]
        public void UnwindOpenScopes_ResetsRowState()
        {
            var closer = new RecordingCloser();
            Facade.FrameOpen = true;
            try
            {
                RowState.TryPush(3f);
                RowState.OnRowItem();

                Facade.UnwindOpenScopes(closer);

                Assert.Equal(0, RowState.Depth);
                Assert.Equal(0, RowState.ItemsInCurrentRow);
            }
            finally
            {
                Facade.FrameOpen = false;
                RowState.Reset();
            }
        }

        [Fact]
        public void UnwindOpenScopes_WithoutOpenFrame_LeavesRowStateUntouched()
        {
            var closer = new RecordingCloser();
            RowState.TryPush(3f);
            Facade.FrameOpen = false;

            Facade.UnwindOpenScopes(closer);

            Assert.Equal(1, RowState.Depth); // same guard as OpenScopeTracker
            RowState.Reset();
        }

        private sealed class RecordingCloser : IScopeCloser
        {
            public void EndWindow() { }
            public void EndScrollRegion() { }
            public void EndTabItem() { }
            public void EndTabBar() { }
            public void EndPlot() { }
            public void EndSubplots() { }
            public void PopStyleColor() { }
            public void PopStyleVar() { }
        }
    }
}
