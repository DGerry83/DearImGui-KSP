namespace DearKSP.Application
{
    /// <summary>
    /// Per-frame mouse/keyboard capture snapshot handed from the native bridge to the
    /// input-lock gateway (spec §5.3). Mutable reference type: the bridge fills the
    /// same instance in place each frame. Filled by chunk C9 — the C4 PoC always
    /// returns defaults (nothing captured, no locks applied).
    /// </summary>
    internal sealed class InputCaptureState
    {
        /// <summary>True while ImGui wants the mouse; game mouse input stays locked.</summary>
        internal bool MouseCaptured = false;

        /// <summary>True while ImGui wants the keyboard; game keyboard input stays locked.</summary>
        internal bool KeyboardCaptured = false;
    }
}
