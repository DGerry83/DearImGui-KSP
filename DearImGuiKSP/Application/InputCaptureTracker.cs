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

        // Reused every frame: this is a per-frame hot path, so no fresh List per Update.
        private readonly List<string> _enabledIds = new List<string>();

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
            _enabledIds.Clear();

            foreach (ConsumerRegistry.ConsumerRegistration consumer in _registry.Ordered)
            {
                if (consumer.Enabled)
                {
                    _enabledIds.Add(consumer.Id);
                }
            }

            _gateway.ApplyLocks(state, _enabledIds);
        }

        /// <summary>Releases every input lock held by the gateway.</summary>
        internal void ReleaseAll()
        {
            _gateway.ReleaseLocks();
        }
    }
}
