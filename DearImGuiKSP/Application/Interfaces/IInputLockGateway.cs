namespace DearImGuiKSP.Application.Interfaces
{
    /// <summary>
    /// Applies and releases KSP input locks under per-consumer lock IDs (spec §5.3).
    /// Locks exist only while capturing.
    /// Implemented by Infrastructure.InputLockGateway.
    /// TODO(milestone 4): ApplyLocks(InputCaptureState) / ReleaseLocks().
    /// </summary>
    internal interface IInputLockGateway
    {
    }
}
