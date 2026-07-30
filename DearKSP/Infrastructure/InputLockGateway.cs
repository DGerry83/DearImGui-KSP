using DearKSP.Application.Interfaces;

namespace DearKSP.Infrastructure
{
    /// <summary>
    /// Implements <see cref="IInputLockGateway"/> over KSP's InputLockManager.
    /// Lock IDs are namespaced per consumer; locks are held only while capturing (spec §5.3).
    /// TODO(milestone 4): implement.
    /// </summary>
    internal sealed class InputLockGateway : IInputLockGateway
    {
    }
}
