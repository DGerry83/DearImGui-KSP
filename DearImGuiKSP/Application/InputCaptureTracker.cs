using System.Collections.Generic;
using DearImGuiKSP.Application.Interfaces;

namespace DearImGuiKSP.Application
{
    /// <summary>
    /// Computes mouse/keyboard capture state from ImGui IO each frame (spec §5.3).
    /// Locks exist only while capturing.
    /// </summary>
    internal sealed class InputCaptureTracker
    {
        private readonly IInputLockGateway _gateway;
        private readonly ConsumerRegistry _registry;

        internal InputCaptureTracker(IInputLockGateway gateway, ConsumerRegistry registry)
        {
            _gateway = gateway;
            _registry = registry;
        }

        /// <summary>
        /// Samples the registry and applies input locks for all enabled consumers
        /// based on the supplied capture state.
        /// </summary>
        internal void Update(InputCaptureState state)
        {
            List<string> enabledIds = new List<string>();

            foreach (ConsumerRegistry.ConsumerRegistration consumer in _registry.Ordered)
            {
                if (consumer.Enabled)
                {
                    enabledIds.Add(consumer.Id);
                }
            }

            _gateway.ApplyLocks(state, enabledIds);
        }

        /// <summary>Releases every input lock held by the gateway.</summary>
        internal void ReleaseAll()
        {
            _gateway.ReleaseLocks();
        }
    }
}
