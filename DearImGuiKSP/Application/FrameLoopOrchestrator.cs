using System.Diagnostics;
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
    /// Also times its own managed frame cost (C14, AC6): a reused Stopwatch accumulates
    /// per-frame elapsed time, and every 600 frames a rolling average is logged at
    /// Debug level (gated by verboseLogging, so silent in normal operation).
    /// </summary>
    internal sealed class FrameLoopOrchestrator
    {
        // Rolling-average window for the managed-cost log line (10 s at 60 fps).
        private const int TimingWindowFrames = 600;

        private readonly INativeBridge _bridge;
        private readonly ConsumerRegistry _registry;
        private readonly InputCaptureTracker _captureTracker;
        private readonly FaultBarrier _faultBarrier;
        private readonly LifecycleStateMachine _lifecycle;
        private readonly ILogger _log;

        private readonly Stopwatch _frameWatch = new Stopwatch();
        private double _frameMsSum;
        private int _frameCount;

        internal FrameLoopOrchestrator(
            INativeBridge bridge,
            ConsumerRegistry registry,
            InputCaptureTracker captureTracker,
            FaultBarrier faultBarrier,
            LifecycleStateMachine lifecycle,
            ILogger log)
        {
            _bridge = bridge;
            _registry = registry;
            _captureTracker = captureTracker;
            _faultBarrier = faultBarrier;
            _lifecycle = lifecycle;
            _log = log;
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

            _frameWatch.Restart();

            // Capture state reflects the previous frame's ImGui IO (spec §5.3 order:
            // sample → locks → callbacks). Sampled before BeginUiFrame on purpose.
            _captureTracker.Update(_bridge.GetIoSnapshot());

            _bridge.BeginUiFrame(width, height, deltaTime);
            foreach (ConsumerRegistry.ConsumerRegistration consumer in _registry.Ordered)
            {
                _faultBarrier.Invoke(consumer);
            }
            _bridge.EndUiFrame();

            _frameWatch.Stop();
            _frameMsSum += _frameWatch.Elapsed.TotalMilliseconds;
            _frameCount++;
            if (_frameCount >= TimingWindowFrames)
            {
                _log?.Debug("Managed frame cost avg " + (_frameMsSum / _frameCount).ToString("0.000") + " ms over " + _frameCount + " frames.");
                _frameMsSum = 0.0;
                _frameCount = 0;
            }
        }
    }
}
