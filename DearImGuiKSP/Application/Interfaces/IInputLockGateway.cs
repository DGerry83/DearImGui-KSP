using System.Collections.Generic;

namespace DearImGuiKSP.Application.Interfaces
{
    /// <summary>
    /// Applies and releases KSP input locks under per-consumer lock IDs (spec §5.3).
    /// Locks exist only while capturing.
    /// Implemented by Infrastructure.InputLockGateway.
    /// </summary>
    internal interface IInputLockGateway
    {
        /// <summary>
        /// Applies input locks for enabled consumers based on the current capture
        /// state. Diffed: locks are added/updated when capture is active and removed
        /// when no longer warranted. Never touches locks it did not set.
        /// </summary>
        void ApplyLocks(InputCaptureState state, IReadOnlyList<string> consumerIds);

        /// <summary>Removes every lock this gateway currently holds.</summary>
        void ReleaseLocks();
    }
}
