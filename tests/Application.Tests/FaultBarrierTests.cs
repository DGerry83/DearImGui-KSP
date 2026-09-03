using System;
using DearImGuiKSP;
using DearImGuiKSP.Application;
using Xunit;

namespace Application.Tests
{
    public class FaultBarrierTests
    {
        private static ConsumerRegistry.ConsumerRegistration CreateRegistration(string id, Action callback)
        {
            var registry = new ConsumerRegistry();
            registry.TryRegister(id, callback);
            return registry.Ordered[0];
        }

        [Fact]
        public void Invoke_Success_InvokesCallback_AndResetsConsecutiveFailureCount()
        {
            var barrier = new FaultBarrier(new FakeLogger());
            ConsumerRegistry.ConsumerRegistration consumer = CreateRegistration("alpha", () => { });
            consumer.ConsecutiveFailureCount = 3;

            barrier.Invoke(consumer);

            Assert.Equal(0, consumer.ConsecutiveFailureCount);
            Assert.True(consumer.Enabled);
        }

        [Fact]
        public void Invoke_BelowThreshold_KeepsConsumerEnabled_AndCountsFailures()
        {
            var barrier = new FaultBarrier(new FakeLogger());
            ConsumerRegistry.ConsumerRegistration consumer = CreateRegistration("alpha", () => throw new InvalidOperationException("boom"));

            for (int i = 1; i < LibraryConfig.ConsumerFailureThreshold; i++)
            {
                barrier.Invoke(consumer);
                Assert.True(consumer.Enabled);
                Assert.Equal(i, consumer.ConsecutiveFailureCount);
            }
        }

        [Fact]
        public void Invoke_AtThreshold_AutoDisablesConsumer()
        {
            var log = new FakeLogger();
            var barrier = new FaultBarrier(log);
            ConsumerRegistry.ConsumerRegistration consumer = CreateRegistration("alpha", () => throw new InvalidOperationException("boom"));

            for (int i = 0; i < LibraryConfig.ConsumerFailureThreshold; i++)
            {
                barrier.Invoke(consumer);
            }

            Assert.False(consumer.Enabled);
            Assert.Equal(LibraryConfig.ConsumerFailureThreshold, consumer.ConsecutiveFailureCount);
            Assert.NotEmpty(log.Errors);
        }

        [Fact]
        public void Invoke_DisabledConsumer_DoesNotInvokeCallback()
        {
            var barrier = new FaultBarrier(new FakeLogger());
            int callCount = 0;
            ConsumerRegistry.ConsumerRegistration consumer = CreateRegistration("alpha", () => { callCount++; });
            consumer.Enabled = false;

            barrier.Invoke(consumer);

            Assert.Equal(0, callCount);
        }

        [Fact]
        public void Invoke_OneConsumerThrowing_DoesNotAffectOthers()
        {
            var barrier = new FaultBarrier(new FakeLogger());
            var registry = new ConsumerRegistry();
            registry.TryRegister("bad", () => throw new InvalidOperationException("boom"));
            int goodCalls = 0;
            registry.TryRegister("good", () => { goodCalls++; });

            for (int i = 0; i < LibraryConfig.ConsumerFailureThreshold + 2; i++)
            {
                barrier.Invoke(registry.Ordered[0]);
                barrier.Invoke(registry.Ordered[1]);
            }

            Assert.False(registry.Ordered[0].Enabled);
            Assert.Equal(LibraryConfig.ConsumerFailureThreshold, registry.Ordered[0].ConsecutiveFailureCount);
            Assert.True(registry.Ordered[1].Enabled);
            Assert.Equal(LibraryConfig.ConsumerFailureThreshold + 2, goodCalls);
            Assert.Equal(0, registry.Ordered[1].ConsecutiveFailureCount);
        }

        [Fact]
        public void Invoke_CounterResetsOnSuccess_BelowThreshold()
        {
            var barrier = new FaultBarrier(new FakeLogger());
            int calls = 0;
            ConsumerRegistry.ConsumerRegistration consumer = CreateRegistration("alpha", () =>
            {
                calls++;
                if (calls % 2 == 1)
                {
                    throw new InvalidOperationException("boom");
                }
            });

            // throw (1), success (0), throw (1), ... never reaches threshold.
            // Odd total so the sequence ends on a throw: count must be 1, not accumulated.
            for (int i = 0; i < LibraryConfig.ConsumerFailureThreshold * 2 + 1; i++)
            {
                barrier.Invoke(consumer);
            }

            Assert.True(consumer.Enabled);
            Assert.Equal(1, consumer.ConsecutiveFailureCount);
        }

        [Fact]
        public void Invoke_NullConsumer_Throws()
        {
            var barrier = new FaultBarrier(new FakeLogger());

            Assert.Throws<ArgumentNullException>(() => barrier.Invoke(null));
        }

        [Fact]
        public void Constructor_NullLogger_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new FaultBarrier(null));
        }
    }
}
