namespace DearImGuiKSP.Application.Interfaces
{
    /// <summary>
    /// Shows the single plain-language startup-failure PopupDialog at the main menu (spec §7).
    /// Implemented by Infrastructure.FailureNotifier.
    /// </summary>
    internal interface IFailureNotifier
    {
        /// <summary>
        /// Shows the failure popup for the given kind. Called once per session, when the
        /// lifecycle state machine enters terminal Failed (spec §5.4).
        /// </summary>
        void Notify(FailureKind kind);
    }
}
