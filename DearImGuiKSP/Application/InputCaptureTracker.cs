using System.Collections.Generic;
using DearImGuiKSP.Application.Interfaces;

namespace DearImGuiKSP.Application
{
    /// <summary>
    /// Computes mouse/keyboard capture state from ImGui IO each frame (spec §5.3).
    /// Locks exist only while capturing. Also drives the uGUI pointer blocker
    /// (ISSUES #001): the blocker is toggled only on capture-state transitions.
    /// </summary>
    internal sealed class InputCaptureTracker
    {
        private readonly IInputLockGateway _gateway;
        private readonly IPointerBlockerGateway _pointerBlocker;
        private readonly ConsumerRegistry _registry;

        private bool _blocked;

        // Reused every frame: this is a per-frame hot path, so no fresh List per Update.
        private readonly List<string> _enabledIds = new List<string>();

        internal InputCaptureTracker(
            IInputLockGateway gateway,
            IPointerBlockerGateway pointerBlocker,
            ConsumerRegistry registry)
        {
            _gateway = gateway;
            _pointerBlocker = pointerBlocker;
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

            if (state.MouseCaptured != _blocked)
            {
                _blocked = state.MouseCaptured;
                _pointerBlocker.SetBlocked(_blocked);
            }
        }

        /// <summary>Releases every input lock held by the gateway.</summary>
        internal void ReleaseAll()
        {
            _gateway.ReleaseLocks();
            if (_blocked)
            {
                _blocked = false;
                _pointerBlocker.SetBlocked(false);
            }
        }
    }
}
