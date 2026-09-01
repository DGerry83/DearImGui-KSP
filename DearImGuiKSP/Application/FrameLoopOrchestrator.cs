using DearImGuiKSP.Application.Interfaces;

namespace DearImGuiKSP.Application
{
    /// <summary>
    /// Executes the per-frame sequence (locked in C7, spec §5.3):
    /// sample capture state → apply/release input locks (C9) → native BeginUiFrame →
    /// consumer callbacks in registration order through the FaultBarrier (C10) →
    /// native EndUiFrame. The render-event handoff is issued separately by the addon.
    /// The lifecycle state machine (C12) hooks in here later; availability is currently
    /// the <see cref="DearImGuiKSP.IsAvailable"/> flag flipped by Composition after bridge init.
    /// </summary>
    internal sealed class FrameLoopOrchestrator
    {
        private readonly INativeBridge _bridge;
        private readonly ConsumerRegistry _registry;
        private readonly InputCaptureTracker _captureTracker;
        private readonly FaultBarrier _faultBarrier;

        internal FrameLoopOrchestrator(
            INativeBridge bridge,
            ConsumerRegistry registry,
            InputCaptureTracker captureTracker,
            FaultBarrier faultBarrier)
        {
            _bridge = bridge;
            _registry = registry;
            _captureTracker = captureTracker;
            _faultBarrier = faultBarrier;
        }

        /// <summary>
        /// Runs one UI frame. No-op while the bridge is not initialized.
        /// Unity-side values (screen size, delta time) arrive as parameters so
        /// Application stays Unity-free.
        /// </summary>
        internal void RunFrame(float width, float height, float deltaTime)
        {
            if (!DearImGuiKSP.IsAvailable)
            {
                return;
            }

            // Capture state reflects the previous frame's ImGui IO (spec §5.3 order:
            // sample → locks → callbacks). Sampled before BeginUiFrame on purpose.
            _captureTracker.Update(_bridge.GetIoSnapshot());

            _bridge.BeginUiFrame(width, height, deltaTime);
            foreach (ConsumerRegistry.ConsumerRegistration consumer in _registry.Ordered)
            {
                _faultBarrier.Invoke(consumer);
            }
            _bridge.EndUiFrame();
        }
    }
}
