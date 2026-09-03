namespace DearImGuiKSP.Application.Interfaces
{
    /// <summary>
    /// Toggles the uGUI pointer blocker that stops clicks passing through ImGui
    /// windows to stock UI beneath (spec §5.3, ISSUES #001). The blocker exists
    /// only while ImGui captures the mouse.
    /// Implemented by Infrastructure.PointerBlockerGateway.
    /// </summary>
    internal interface IPointerBlockerGateway
    {
        /// <summary>
        /// Enables or disables pointer blocking. Driven only on capture-state
        /// transitions and on release; must be cheap (per-frame hot path) and
        /// idempotent for a repeated state.
        /// </summary>
        void SetBlocked(bool blocked);
    }
}
