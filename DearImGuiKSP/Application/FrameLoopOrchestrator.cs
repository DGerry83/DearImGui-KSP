using DearImGuiKSP.Application.Interfaces;

namespace DearImGuiKSP.Application
{
    /// <summary>
    /// Executes the per-frame sequence (locked in C7, spec §5.3):
    /// sample capture state → apply/release input locks (C9) → native BeginUiFrame →
    /// consumer callbacks in registration order through the FaultBarrier (C10) →
    /// native EndUiFrame. The render-event handoff is issued separately by the addon.
    /// Frames run only while the lifecycle state machine (C12) is Running —
    /// suspended (F2/loading) and failed sessions produce no frames.
    /// </summary>
    internal sealed class FrameLoopOrchestrator
    {
        private readonly INativeBridge _bridge;
        private readonly ConsumerRegistry _registry;
        private readonly InputCaptureTracker _captureTracker;
        private readonly FaultBarrier _faultBarrier;
        private readonly LifecycleStateMachine _lifecycle;

        internal FrameLoopOrchestrator(
            INativeBridge bridge,
            ConsumerRegistry registry,
            InputCaptureTracker captureTracker,
            FaultBarrier faultBarrier,
            LifecycleStateMachine lifecycle)
        {
            _bridge = bridge;
            _registry = registry;
            _captureTracker = captureTracker;
            _faultBarrier = faultBarrier;
            _lifecycle = lifecycle;
        }

        /// <summary>
        /// Runs one UI frame. No-op unless the library is in the Running state.
        /// Unity-side values (screen size, delta time) arrive as parameters so
        /// Application stays Unity-free.
        /// </summary>
        internal void RunFrame(float width, float height, float deltaTime)
        {
            if (!_lifecycle.IsRunning)
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
