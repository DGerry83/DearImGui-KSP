namespace DearImGuiKSP.Application.Interfaces
{
    /// <summary>
    /// Isolates the native DLL: explicit LoadLibrary bootstrap, version handshake,
    /// device-kind gate, and the per-frame Begin/End UiFrame pair (spec §4.1;
    /// CinematicRecorder-documented GameData load-path gotcha). Method set locked in
    /// chunk C4 and amended in C5 (startup font load) and C7 (the per-frame pump was
    /// split into BeginUiFrame/EndUiFrame — the interface is internal, no external
    /// consumers existed yet). Implemented by Infrastructure.NativeBridge.
    /// Contains no Unity types — Application stays Unity-free.
    /// </summary>
    internal interface INativeBridge
    {
        /// <summary>
        /// Loads DearImGuiKSPNative.dll from GameData/DearImGuiKSP/PluginData, performs the
        /// managed/native version handshake and the D3D11 device gate, and brings up
        /// the native ImGui context. Returns 0 on success; each failure mode returns
        /// a distinct nonzero code (see Infrastructure.NativeBridge constants).
        /// </summary>
        int Initialize();

        /// <summary>
        /// Loads one TTF font file into the native atlas at the given pixel size.
        /// MUST be called after <see cref="Initialize"/> succeeded and before the
        /// first <see cref="BeginUiFrame"/>: the atlas is baked on the first frame
        /// and cannot be rebuilt afterwards (spec §5.2). Returns true when the
        /// native load succeeded; false means the atlas still holds the embedded
        /// default — the caller logs the fallback and continues (log-only, never
        /// a failure-mode trigger). False when not initialized.
        /// </summary>
        bool LoadFontFromFile(string utf8Path, float sizePixels);

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
        /// Resolution-change hook; the implementation is an intentional no-op.
        /// ImGui DisplaySize is set every <see cref="BeginUiFrame"/>, the backend
        /// draws into whatever render target Unity has bound at render-event time,
        /// and the font atlas is resolution-independent, so a resolution or
        /// fullscreen change needs no native rebuild.
        /// </summary>
        void RebuildViewport(int width, int height);

        /// <summary>
        /// Clamps every visible ImGui window fully into the viewport of the
        /// PASSED size (width/height are the live values for the new viewport —
        /// io.DisplaySize may still be stale at call time): each window's
        /// position is clamped to [0, viewport - window size], pinning oversized
        /// windows to the top-left corner. Called from the frame loop when the
        /// viewport size changes and the clampWindowsToViewport setting is on
        /// (ISSUES #002, G3 rework).
        /// No-op when not initialized.
        /// </summary>
        void ClampWindowsToViewport(float width, float height);

        /// <summary>
        /// Shuts down the native context and frees the DLL. No-op when not loaded.
        /// </summary>
        void Shutdown();
    }
}
