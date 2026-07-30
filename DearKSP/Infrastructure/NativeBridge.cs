using DearKSP.Application.Interfaces;

namespace DearKSP.Infrastructure
{
    /// <summary>
    /// Implements <see cref="INativeBridge"/>. Loads DearKSPNative.dll explicitly
    /// (LoadLibrary + SetDllDirectory) from GameData/DearKSP/PluginData/ — native DLLs
    /// must stay out of the loader's assembly scan path (D19), and implicit [DllImport]
    /// resolution fails from GameData subfolders. Performs the managed/native version
    /// handshake (spec §5.4).
    /// TODO(milestone 2): implement.
    /// </summary>
    internal sealed class NativeBridge : INativeBridge
    {
    }
}
