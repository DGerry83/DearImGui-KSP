namespace DearKSP.Application.Interfaces
{
    /// <summary>
    /// Isolates the native DLL: explicit LoadLibrary bootstrap, version handshake,
    /// and every P/Invoke declaration (spec §4.1; CinematicRecorder-documented
    /// GameData load-path gotcha).
    /// Implemented by Infrastructure.NativeBridge.
    /// TODO(milestone 2): Initialize / GetIoSnapshot / SubmitFrame / RebuildViewport / Shutdown.
    /// </summary>
    internal interface INativeBridge
    {
    }
}
