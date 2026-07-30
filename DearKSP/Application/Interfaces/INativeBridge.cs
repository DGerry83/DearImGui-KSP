namespace DearKSP.Application.Interfaces
{
    /// <summary>
    /// Isolates the native DLL: explicit LoadLibrary bootstrap, version handshake,
    /// device-kind gate, and the per-frame Begin/End UiFrame pair (spec §4.1;
    /// CinematicRecorder-documented GameData load-path gotcha). Method set locked in
    /// chunk C4 and amended in C7 (the per-frame pump was split into
    /// BeginUiFrame/EndUiFrame — the interface is internal, no external consumers
    /// existed yet). Implemented by Infrastructure.NativeBridge.
    /// Contains no Unity types — Application stays Unity-free.
    /// </summary>
    internal interface INativeBridge
    {
        /// <summary>
        /// Loads DearKSPNative.dll from GameData/DearKSP/PluginData, performs the
        /// managed/native version handshake and the D3D11 device gate, and brings up
        /// the native ImGui context. Returns 0 on success; each failure mode returns
        /// a distinct nonzero code (see Infrastructure.NativeBridge constants).
        /// </summary>
        int Initialize();

        /// <summary>
        /// Per-frame mouse/keyboard capture snapshot (spec §5.3). Filled from the
        /// ImGui IO by chunk C9; the C4 PoC returns defaults (nothing captured).
        /// </summary>
        InputCaptureState GetIoSnapshot();

        /// <summary>
        /// Opens one UI frame: native BeginFrame(display size, delta). Consumer
        /// callbacks run between this and <see cref="EndUiFrame"/>.
        /// No-op when not initialized.
        /// </summary>
        void BeginUiFrame(float width, float height, float deltaSeconds);

        /// <summary>
        /// Closes the UI frame opened by <see cref="BeginUiFrame"/>: native EndFrame.
        /// The render-event callback is issued separately by the addon.
        /// No-op when not initialized.
        /// </summary>
        void EndUiFrame();

        /// <summary>
        /// Recreates the render viewport after a resolution/fullscreen change.
        /// PoC no-op; real work lands with the state machine in C12 (tracked).
        /// </summary>
        void RebuildViewport(int width, int height);

        /// <summary>
        /// Shuts down the native context and frees the DLL. No-op when not loaded.
        /// </summary>
        void Shutdown();
    }
}
