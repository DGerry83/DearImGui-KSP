using System.Collections.Generic;
using DearImGuiKSP.Application.Interfaces;

namespace DearImGuiKSP.Application
{
    /// <summary>
    /// Computes mouse/keyboard capture state from ImGui IO each frame (spec §5.3).
    /// Locks exist only while capturing. Also drives the uGUI pointer blocker
    /// (ISSUES #001) and the IMGUI event eater (ISSUES #003): both are toggled
    /// only on capture-state transitions. Single responsibility per gateway —
    /// the blocker stops uGUI raycasts, the eater starves IMGUI (OnGUI) events;
    /// the mouse shield mirrors the blocker's flag but is a separate call.
    /// </summary>
    internal sealed class InputCaptureTracker
    {
        private readonly IInputLockGateway _gateway;
        private readonly IPointerBlockerGateway _pointerBlocker;
        private readonly IImguiEventEaterGateway _imguiEventEater;
        private readonly ConsumerRegistry _registry;

        private bool _blocked;
        private bool _mouseShielded;
        private bool _keyboardShielded;

        // Reused every frame: this is a per-frame hot path, so no fresh List per Update.
        private readonly List<string> _enabledIds = new List<string>();

        internal InputCaptureTracker(
            IInputLockGateway gateway,
            IPointerBlockerGateway pointerBlocker,
            IImguiEventEaterGateway imguiEventEater,
            ConsumerRegistry registry)
        {
            _gateway = gateway;
            _pointerBlocker = pointerBlocker;
            _imguiEventEater = imguiEventEater;
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

            if (state.MouseCaptured != _mouseShielded)
            {
                _mouseShielded = state.MouseCaptured;
                _imguiEventEater.SetMouseShielded(_mouseShielded);
            }

            if (state.KeyboardCaptured != _keyboardShielded)
            {
                _keyboardShielded = state.KeyboardCaptured;
                _imguiEventEater.SetKeyboardShielded(_keyboardShielded);
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
            if (_mouseShielded)
            {
                _mouseShielded = false;
                _imguiEventEater.SetMouseShielded(false);
            }
            if (_keyboardShielded)
            {
                _keyboardShielded = false;
                _imguiEventEater.SetKeyboardShielded(false);
            }
        }
    }
}
