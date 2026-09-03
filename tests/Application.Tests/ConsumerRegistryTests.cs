using System;
using DearImGuiKSP.Application;
using Xunit;

namespace Application.Tests
{
    public class ConsumerRegistryTests
    {
        [Fact]
        public void TryRegister_NewId_ReturnsTrue_AndAppearsInOrdered()
        {
            var registry = new ConsumerRegistry();

            bool result = registry.TryRegister("alpha", () => { });

            Assert.True(result);
            Assert.Single(registry.Ordered);
            Assert.Equal("alpha", registry.Ordered[0].Id);
        }

        [Fact]
        public void TryRegister_DuplicateId_ReturnsFalse_AndDoesNotAdd()
        {
            var registry = new ConsumerRegistry();
            registry.TryRegister("alpha", () => { });

            bool result = registry.TryRegister("alpha", () => { });

            Assert.False(result);
            Assert.Single(registry.Ordered);
        }

        [Fact]
        public void TryRegister_AfterUnregister_AllowsIdReuse()
        {
            var registry = new ConsumerRegistry();
            registry.TryRegister("alpha", () => { });
            registry.Unregister("alpha");

            bool result = registry.TryRegister("alpha", () => { });

            Assert.True(result);
            Assert.Single(registry.Ordered);
        }

        [Fact]
        public void Unregister_KnownId_ReturnsTrue_AndRemovesFromOrdered()
        {
            var registry = new ConsumerRegistry();
            registry.TryRegister("alpha", () => { });
            registry.TryRegister("beta", () => { });

            bool result = registry.Unregister("alpha");

            Assert.True(result);
            Assert.Single(registry.Ordered);
            Assert.Equal("beta", registry.Ordered[0].Id);
        }

        [Fact]
        public void Unregister_UnknownId_ReturnsFalse()
        {
            var registry = new ConsumerRegistry();

            Assert.False(registry.Unregister("missing"));
        }

        [Fact]
        public void Ordered_PreservesRegistrationOrder()
        {
            var registry = new ConsumerRegistry();
            registry.TryRegister("first", () => { });
            registry.TryRegister("second", () => { });
            registry.TryRegister("third", () => { });

            Assert.Equal(3, registry.Ordered.Count);
            Assert.Equal("first", registry.Ordered[0].Id);
            Assert.Equal("second", registry.Ordered[1].Id);
            Assert.Equal("third", registry.Ordered[2].Id);
        }

        [Fact]
        public void Registration_StoresCallback_AndDefaultsToEnabled()
        {
            var registry = new ConsumerRegistry();
            bool invoked = false;
            registry.TryRegister("alpha", () => { invoked = true; });

            registry.Ordered[0].Callback();

            Assert.True(invoked);
            Assert.True(registry.Ordered[0].Enabled);
            Assert.Equal(0, registry.Ordered[0].ConsecutiveFailureCount);
        }
    }
}
