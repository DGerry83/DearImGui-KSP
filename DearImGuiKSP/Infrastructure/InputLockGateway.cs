using System.Collections.Generic;
using DearImGuiKSP.Application;
using DearImGuiKSP.Application.Interfaces;

namespace DearImGuiKSP.Infrastructure
{
    /// <summary>
    /// Implements <see cref="IInputLockGateway"/> over KSP's InputLockManager.
    /// Lock IDs are namespaced per consumer; locks are held only while capturing (spec §5.3).
    /// </summary>
    internal sealed class InputLockGateway : IInputLockGateway
    {
        private readonly HashSet<string> _heldConsumerIds = new HashSet<string>();
        private ControlTypes _heldMask;

        /// <inheritdoc/>
        public void ApplyLocks(InputCaptureState state, IReadOnlyList<string> consumerIds)
        {
            ControlTypes mask = ComputeMask(state);
            HashSet<string> desiredIds = new HashSet<string>();

            if (mask != 0)
            {
                for (int i = 0; i < consumerIds.Count; ++i)
                {
                    desiredIds.Add(consumerIds[i]);
                }
            }

            bool maskChanged = mask != _heldMask;

            // Remove locks for consumers that are no longer enabled or no longer capturing.
            foreach (string heldId in _heldConsumerIds)
            {
                if (!desiredIds.Contains(heldId))
                {
                    InputLockManager.RemoveControlLock(LockIdFor(heldId));
                }
            }

            // Set locks for newly-desired consumers, or refresh all locks when the mask changed.
            foreach (string desiredId in desiredIds)
            {
                if (maskChanged || !_heldConsumerIds.Contains(desiredId))
                {
                    InputLockManager.SetControlLock(mask, LockIdFor(desiredId));
                }
            }

            _heldConsumerIds.Clear();
            _heldConsumerIds.UnionWith(desiredIds);
            _heldMask = mask;
        }

        /// <inheritdoc/>
        public void ReleaseLocks()
        {
            foreach (string heldId in _heldConsumerIds)
            {
                InputLockManager.RemoveControlLock(LockIdFor(heldId));
            }

            _heldConsumerIds.Clear();
            _heldMask = 0;
        }

        private static string LockIdFor(string consumerId)
        {
            return LibraryConfig.ModName + "." + consumerId;
        }

        private static ControlTypes ComputeMask(InputCaptureState state)
        {
            ControlTypes mask = 0;

            if (state.MouseCaptured)
            {
                // MAIN_MENU disables the main-menu 3D buttons (TextProButton3D),
                // which bypass EventSystem and answer only to this lock — the same
                // lock stock uses (ISSUES #001 spike; MainMenu.cs:1850-1893).
                mask |= ControlTypes.CAMERACONTROLS | ControlTypes.GUI | ControlTypes.MAIN_MENU;
            }

            if (state.KeyboardCaptured)
            {
                mask |= ControlTypes.KEYBOARDINPUT;
            }

            return mask;
        }
    }
}
