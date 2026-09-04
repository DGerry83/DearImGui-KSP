using System.Collections.Generic;
using DearImGuiKSP.Application;
using Xunit;

namespace Application.Tests
{
    public class InputCaptureTrackerTests
    {
        private static InputCaptureTracker CreateTracker(
            FakeInputLockGateway locks,
            FakePointerBlockerGateway blocker,
            FakeImguiEventEaterGateway eater,
            ConsumerRegistry registry)
        {
            return new InputCaptureTracker(locks, blocker, eater, registry);
        }

        private static ConsumerRegistry CreateRegistry(params string[] ids)
        {
            var registry = new ConsumerRegistry();
            foreach (string id in ids)
            {
                registry.TryRegister(id, () => { });
            }
            return registry;
        }

        [Fact]
        public void Update_NotCapturing_DoesNotToggleBlocker()
        {
            var locks = new FakeInputLockGateway();
            var blocker = new FakePointerBlockerGateway();
            var eater = new FakeImguiEventEaterGateway();
            InputCaptureTracker tracker = CreateTracker(locks, blocker, eater, CreateRegistry());

            tracker.Update(new InputCaptureState { MouseCaptured = false });

            Assert.Equal(0, blocker.CallCount);
            Assert.Equal(1, locks.ApplyLocksCallCount);
        }

        [Fact]
        public void Update_FirstCapturingFrame_CallsSetBlockedTrue()
        {
            var locks = new FakeInputLockGateway();
            var blocker = new FakePointerBlockerGateway();
            var eater = new FakeImguiEventEaterGateway();
            InputCaptureTracker tracker = CreateTracker(locks, blocker, eater, CreateRegistry());

            tracker.Update(new InputCaptureState { MouseCaptured = true });

            Assert.Equal(new List<bool> { true }, blocker.Calls);
        }

        [Fact]
        public void Update_RepeatedCapturingFrames_AreTransitionOnly()
        {
            var locks = new FakeInputLockGateway();
            var blocker = new FakePointerBlockerGateway();
            var eater = new FakeImguiEventEaterGateway();
            InputCaptureTracker tracker = CreateTracker(locks, blocker, eater, CreateRegistry());

            tracker.Update(new InputCaptureState { MouseCaptured = true });
            tracker.Update(new InputCaptureState { MouseCaptured = true });
            tracker.Update(new InputCaptureState { MouseCaptured = true });

            Assert.Equal(new List<bool> { true }, blocker.Calls);
        }

        [Fact]
        public void Update_CaptureThenRelease_TogglesBlockerOnEachTransition()
        {
            var locks = new FakeInputLockGateway();
            var blocker = new FakePointerBlockerGateway();
            var eater = new FakeImguiEventEaterGateway();
            InputCaptureTracker tracker = CreateTracker(locks, blocker, eater, CreateRegistry());

            tracker.Update(new InputCaptureState { MouseCaptured = true });
            tracker.Update(new InputCaptureState { MouseCaptured = true });
            tracker.Update(new InputCaptureState { MouseCaptured = false });
            tracker.Update(new InputCaptureState { MouseCaptured = false });
            tracker.Update(new InputCaptureState { MouseCaptured = true });

            Assert.Equal(new List<bool> { true, false, true }, blocker.Calls);
        }

        [Fact]
        public void Update_PassesEnabledConsumerIdsInRegistrationOrder()
        {
            var locks = new FakeInputLockGateway();
            var blocker = new FakePointerBlockerGateway();
            var eater = new FakeImguiEventEaterGateway();
            ConsumerRegistry registry = CreateRegistry("alpha", "beta", "gamma");
            registry.Ordered[1].Enabled = false;
            InputCaptureTracker tracker = CreateTracker(locks, blocker, eater, registry);

            tracker.Update(new InputCaptureState { MouseCaptured = true });

            Assert.Equal(new List<string> { "alpha", "gamma" }, locks.LastConsumerIds);
        }

        [Fact]
        public void ReleaseAll_ReleasesLocks_AndForcesSetBlockedFalse()
        {
            var locks = new FakeInputLockGateway();
            var blocker = new FakePointerBlockerGateway();
            var eater = new FakeImguiEventEaterGateway();
            InputCaptureTracker tracker = CreateTracker(locks, blocker, eater, CreateRegistry());
            tracker.Update(new InputCaptureState { MouseCaptured = true });

            tracker.ReleaseAll();

            Assert.Equal(1, locks.ReleaseLocksCallCount);
            Assert.Equal(new List<bool> { true, false }, blocker.Calls);
        }

        [Fact]
        public void ReleaseAll_WhenNotBlocked_ReleasesLocksWithoutTogglingBlocker()
        {
            var locks = new FakeInputLockGateway();
            var blocker = new FakePointerBlockerGateway();
            var eater = new FakeImguiEventEaterGateway();
            InputCaptureTracker tracker = CreateTracker(locks, blocker, eater, CreateRegistry());
            tracker.Update(new InputCaptureState { MouseCaptured = false });

            tracker.ReleaseAll();

            Assert.Equal(1, locks.ReleaseLocksCallCount);
            Assert.Equal(0, blocker.CallCount);
        }

        [Fact]
        public void ReleaseAll_ThenCapturingAgain_TogglesBlockerOnceMore()
        {
            var locks = new FakeInputLockGateway();
            var blocker = new FakePointerBlockerGateway();
            var eater = new FakeImguiEventEaterGateway();
            InputCaptureTracker tracker = CreateTracker(locks, blocker, eater, CreateRegistry());

            tracker.Update(new InputCaptureState { MouseCaptured = true });
            tracker.ReleaseAll();
            tracker.Update(new InputCaptureState { MouseCaptured = true });

            Assert.Equal(new List<bool> { true, false, true }, blocker.Calls);
        }

        [Fact]
        public void Update_RepeatedCapturingFrames_ShieldMouseTransitionOnly()
        {
            var locks = new FakeInputLockGateway();
            var blocker = new FakePointerBlockerGateway();
            var eater = new FakeImguiEventEaterGateway();
            InputCaptureTracker tracker = CreateTracker(locks, blocker, eater, CreateRegistry());

            tracker.Update(new InputCaptureState { MouseCaptured = true });
            tracker.Update(new InputCaptureState { MouseCaptured = true });
            tracker.Update(new InputCaptureState { MouseCaptured = true });

            Assert.Equal(new List<bool> { true }, eater.MouseCalls);
            Assert.Empty(eater.KeyboardCalls);
        }

        [Fact]
        public void Update_KeyboardCapturedAlone_ShieldsKeyboardWithoutMouse()
        {
            var locks = new FakeInputLockGateway();
            var blocker = new FakePointerBlockerGateway();
            var eater = new FakeImguiEventEaterGateway();
            InputCaptureTracker tracker = CreateTracker(locks, blocker, eater, CreateRegistry());

            tracker.Update(new InputCaptureState { MouseCaptured = false, KeyboardCaptured = true });

            Assert.Equal(new List<bool> { true }, eater.KeyboardCalls);
            Assert.Empty(eater.MouseCalls);
            Assert.Equal(0, blocker.CallCount);
        }

        [Fact]
        public void Update_MouseAndKeyboardCapture_TransitionIndependently()
        {
            var locks = new FakeInputLockGateway();
            var blocker = new FakePointerBlockerGateway();
            var eater = new FakeImguiEventEaterGateway();
            InputCaptureTracker tracker = CreateTracker(locks, blocker, eater, CreateRegistry());

            tracker.Update(new InputCaptureState { MouseCaptured = true, KeyboardCaptured = false });
            tracker.Update(new InputCaptureState { MouseCaptured = true, KeyboardCaptured = true });
            tracker.Update(new InputCaptureState { MouseCaptured = false, KeyboardCaptured = true });
            tracker.Update(new InputCaptureState { MouseCaptured = false, KeyboardCaptured = false });

            Assert.Equal(new List<bool> { true, false }, eater.MouseCalls);
            Assert.Equal(new List<bool> { true, false }, eater.KeyboardCalls);
        }

        [Fact]
        public void ReleaseAll_ForcesBothShieldsFalse()
        {
            var locks = new FakeInputLockGateway();
            var blocker = new FakePointerBlockerGateway();
            var eater = new FakeImguiEventEaterGateway();
            InputCaptureTracker tracker = CreateTracker(locks, blocker, eater, CreateRegistry());
            tracker.Update(new InputCaptureState { MouseCaptured = true, KeyboardCaptured = true });

            tracker.ReleaseAll();

            Assert.Equal(new List<bool> { true, false }, eater.MouseCalls);
            Assert.Equal(new List<bool> { true, false }, eater.KeyboardCalls);
        }

        [Fact]
        public void ReleaseAll_WhenNotShielded_ReleasesLocksWithoutTouchingShields()
        {
            var locks = new FakeInputLockGateway();
            var blocker = new FakePointerBlockerGateway();
            var eater = new FakeImguiEventEaterGateway();
            InputCaptureTracker tracker = CreateTracker(locks, blocker, eater, CreateRegistry());
            tracker.Update(new InputCaptureState { MouseCaptured = false, KeyboardCaptured = false });

            tracker.ReleaseAll();

            Assert.Equal(1, locks.ReleaseLocksCallCount);
            Assert.Empty(eater.MouseCalls);
            Assert.Empty(eater.KeyboardCalls);
        }

        [Fact]
        public void ReleaseAll_ThenKeyboardCapturingAgain_ShieldsOnceMore()
        {
            var locks = new FakeInputLockGateway();
            var blocker = new FakePointerBlockerGateway();
            var eater = new FakeImguiEventEaterGateway();
            InputCaptureTracker tracker = CreateTracker(locks, blocker, eater, CreateRegistry());

            tracker.Update(new InputCaptureState { KeyboardCaptured = true });
            tracker.ReleaseAll();
            tracker.Update(new InputCaptureState { KeyboardCaptured = true });

            Assert.Equal(new List<bool> { true, false, true }, eater.KeyboardCalls);
            Assert.Empty(eater.MouseCalls);
        }
    }
}
