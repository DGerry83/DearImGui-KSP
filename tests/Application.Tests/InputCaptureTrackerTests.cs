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
            ConsumerRegistry registry)
        {
            return new InputCaptureTracker(locks, blocker, registry);
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
            InputCaptureTracker tracker = CreateTracker(locks, blocker, CreateRegistry());

            tracker.Update(new InputCaptureState { MouseCaptured = false });

            Assert.Equal(0, blocker.CallCount);
            Assert.Equal(1, locks.ApplyLocksCallCount);
        }

        [Fact]
        public void Update_FirstCapturingFrame_CallsSetBlockedTrue()
        {
            var locks = new FakeInputLockGateway();
            var blocker = new FakePointerBlockerGateway();
            InputCaptureTracker tracker = CreateTracker(locks, blocker, CreateRegistry());

            tracker.Update(new InputCaptureState { MouseCaptured = true });

            Assert.Equal(new List<bool> { true }, blocker.Calls);
        }

        [Fact]
        public void Update_RepeatedCapturingFrames_AreTransitionOnly()
        {
            var locks = new FakeInputLockGateway();
            var blocker = new FakePointerBlockerGateway();
            InputCaptureTracker tracker = CreateTracker(locks, blocker, CreateRegistry());

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
            InputCaptureTracker tracker = CreateTracker(locks, blocker, CreateRegistry());

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
            ConsumerRegistry registry = CreateRegistry("alpha", "beta", "gamma");
            registry.Ordered[1].Enabled = false;
            InputCaptureTracker tracker = CreateTracker(locks, blocker, registry);

            tracker.Update(new InputCaptureState { MouseCaptured = true });

            Assert.Equal(new List<string> { "alpha", "gamma" }, locks.LastConsumerIds);
        }

        [Fact]
        public void ReleaseAll_ReleasesLocks_AndForcesSetBlockedFalse()
        {
            var locks = new FakeInputLockGateway();
            var blocker = new FakePointerBlockerGateway();
            InputCaptureTracker tracker = CreateTracker(locks, blocker, CreateRegistry());
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
            InputCaptureTracker tracker = CreateTracker(locks, blocker, CreateRegistry());
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
            InputCaptureTracker tracker = CreateTracker(locks, blocker, CreateRegistry());

            tracker.Update(new InputCaptureState { MouseCaptured = true });
            tracker.ReleaseAll();
            tracker.Update(new InputCaptureState { MouseCaptured = true });

            Assert.Equal(new List<bool> { true, false, true }, blocker.Calls);
        }
    }
}
