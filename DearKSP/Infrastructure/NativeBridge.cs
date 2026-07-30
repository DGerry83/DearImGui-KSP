using DearKSP.Application.Interfaces;

namespace DearKSP.Infrastructure
{
    /// <summary>
    /// Implements <see cref="INativeBridge"/>. Loads DearKSPNative.dll explicitly
    /// (LoadLibrary + SetDllDirectory) because implicit [DllImport] resolution fails
    /// from GameData subfolders; performs the managed/native version handshake (spec §5.4).
    /// TODO(milestone 2): implement.
    /// </summary>
    internal sealed class NativeBridge : INativeBridge
    {
    }
}
