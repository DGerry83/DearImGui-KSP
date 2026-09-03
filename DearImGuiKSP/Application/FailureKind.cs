namespace DearImGuiKSP.Application
{
    /// <summary>
    /// Categories of unrecoverable startup failure (spec §5.4), used to select the
    /// player-facing popup body from <see cref="FailureText"/> (spec §7.1).
    /// </summary>
    internal enum FailureKind
    {
        /// <summary>Missing/corrupt native component (DLL, exports, device texture, context init).</summary>
        NativeComponent,

        /// <summary>Managed/native version handshake mismatch (spec §5.4, D17).</summary>
        VersionMismatch,

        /// <summary>Unsupported graphics API (MVP requires D3D11).</summary>
        GraphicsApi,

        /// <summary>Render-hook failure (null render-event callback after init).</summary>
        RenderHook
    }
}
