namespace DearImGuiKSP.Application.Interfaces
{
    /// <summary>
    /// Shields Unity IMGUI — the legacy immediate-mode GUI driven by MonoBehaviour
    /// OnGUI handlers, NOT Dear ImGui — from input events while an ImGui window
    /// captures them (ISSUES #003). IMGUI reads events straight from Unity's event
    /// queue, outside EventSystem raycasts and InputLockManager, so neither the
    /// uGUI pointer blocker nor capture locks can reach it.
    /// Implemented by Infrastructure.ImguiEventEaterGateway.
    /// </summary>
    internal interface IImguiEventEaterGateway
    {
        /// <summary>
        /// Enables or disables eating of IMGUI mouse events (clicks, drags, scroll).
        /// Driven only on capture-state transitions and on release; must be cheap
        /// (per-frame hot path) and idempotent for a repeated state.
        /// </summary>
        void SetMouseShielded(bool shielded);

        /// <summary>
        /// Enables or disables eating of IMGUI keyboard events. Driven only on
        /// capture-state transitions and on release; must be cheap (per-frame hot
        /// path) and idempotent for a repeated state.
        /// </summary>
        void SetKeyboardShielded(bool shielded);
    }
}
