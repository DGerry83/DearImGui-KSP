using DearImGuiKSP.Application;
using Xunit;

namespace Application.Tests
{
    public class LifecycleStateMachineTests
    {
        private static LifecycleStateMachine CreateMachine(out FakeLogger log)
        {
            log = new FakeLogger();
            return new LifecycleStateMachine(log);
        }

        [Fact]
        public void InitialState_IsUninitialized()
        {
            LifecycleStateMachine machine = CreateMachine(out FakeLogger _);

            Assert.Equal(LifecycleState.Uninitialized, machine.State);
            Assert.False(machine.IsRunning);
        }

        [Fact]
        public void MarkInitializing_FromUninitialized_Transitions()
        {
            LifecycleStateMachine machine = CreateMachine(out FakeLogger _);

            machine.MarkInitializing();

            Assert.Equal(LifecycleState.Initializing, machine.State);
            Assert.False(machine.IsRunning);
        }

        [Fact]
        public void MarkRunning_FromInitializing_Transitions()
        {
            LifecycleStateMachine machine = CreateMachine(out FakeLogger _);
            machine.MarkInitializing();

            machine.MarkRunning();

            Assert.Equal(LifecycleState.Running, machine.State);
            Assert.True(machine.IsRunning);
        }

        [Fact]
        public void MarkRunning_FromUninitialized_IsIgnored()
        {
            LifecycleStateMachine machine = CreateMachine(out FakeLogger log);

            machine.MarkRunning();

            Assert.Equal(LifecycleState.Uninitialized, machine.State);
            Assert.Single(log.Warnings);
        }

        [Fact]
        public void MarkInitializing_FromRunning_IsIgnored()
        {
            LifecycleStateMachine machine = CreateMachine(out FakeLogger log);
            machine.MarkInitializing();
            machine.MarkRunning();

            machine.MarkInitializing();

            Assert.Equal(LifecycleState.Running, machine.State);
        }

        [Fact]
        public void SetUiVisibleFalse_WhileRunning_Suspends()
        {
            LifecycleStateMachine machine = CreateMachine(out FakeLogger _);
            machine.MarkInitializing();
            machine.MarkRunning();

            machine.SetUiVisible(false);

            Assert.Equal(LifecycleState.Suspended, machine.State);
            Assert.False(machine.IsRunning);
        }

        [Fact]
        public void SetUiVisibleTrue_WhileSuspended_Resumes()
        {
            LifecycleStateMachine machine = CreateMachine(out FakeLogger _);
            machine.MarkInitializing();
            machine.MarkRunning();
            machine.SetUiVisible(false);

            machine.SetUiVisible(true);

            Assert.Equal(LifecycleState.Running, machine.State);
        }

        [Fact]
        public void SetLoadingTrue_WhileRunning_Suspends_AndSetLoadingFalse_Resumes()
        {
            LifecycleStateMachine machine = CreateMachine(out FakeLogger _);
            machine.MarkInitializing();
            machine.MarkRunning();

            machine.SetLoading(true);
            Assert.Equal(LifecycleState.Suspended, machine.State);

            machine.SetLoading(false);
            Assert.Equal(LifecycleState.Running, machine.State);
        }

        [Fact]
        public void SetUiVisible_BeforeRunning_IsIgnoredButFlagIsRetained()
        {
            LifecycleStateMachine machine = CreateMachine(out FakeLogger log);

            machine.SetUiVisible(false);
            Assert.Equal(LifecycleState.Uninitialized, machine.State);
            Assert.Single(log.Warnings);

            // The retained flag applies as soon as the machine reaches Running.
            machine.MarkInitializing();
            machine.MarkRunning();
            Assert.Equal(LifecycleState.Suspended, machine.State);
        }

        [Theory]
        [InlineData(0)] // FailureKind.NativeComponent
        [InlineData(1)] // FailureKind.VersionMismatch
        [InlineData(2)] // FailureKind.GraphicsApi
        [InlineData(3)] // FailureKind.RenderHook
        public void Fail_FromAnyState_EntersFailed_AndFiresEnteredFailedOnce(int kindValue)
        {
            FailureKind kind = (FailureKind)kindValue;
            LifecycleStateMachine machine = CreateMachine(out FakeLogger _);
            int eventCount = 0;
            FailureKind observedKind = default(FailureKind);
            machine.EnteredFailed += k =>
            {
                eventCount++;
                observedKind = k;
            };

            machine.Fail(kind, "test reason");

            Assert.Equal(LifecycleState.Failed, machine.State);
            Assert.False(machine.IsRunning);
            Assert.Equal(1, eventCount);
            Assert.Equal(kind, observedKind);
        }

        [Fact]
        public void Fail_WhenAlreadyFailed_DoesNotFireEnteredFailedAgain()
        {
            LifecycleStateMachine machine = CreateMachine(out FakeLogger _);
            int eventCount = 0;
            machine.EnteredFailed += _ => eventCount++;
            machine.Fail(FailureKind.NativeComponent, "first");

            machine.Fail(FailureKind.RenderHook, "second");

            Assert.Equal(1, eventCount);
        }

        [Fact]
        public void AfterFailed_AllTransitionsAreIgnored()
        {
            LifecycleStateMachine machine = CreateMachine(out FakeLogger log);
            machine.MarkInitializing();
            machine.MarkRunning();
            machine.Fail(FailureKind.NativeComponent, "boom");

            machine.MarkRunning();
            machine.MarkInitializing();
            machine.SetUiVisible(true);
            machine.SetLoading(false);

            Assert.Equal(LifecycleState.Failed, machine.State);
        }
    }
}
