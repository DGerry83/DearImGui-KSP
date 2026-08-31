using System;
using DearImGuiKSP.Application.Interfaces;

namespace DearImGuiKSP.Application
{
    /// <summary>
    /// Executes the per-frame sequence (locked in C7, spec §5.3):
    /// native BeginUiFrame → consumer callbacks in registration order → native
    /// EndUiFrame. The render-event handoff is issued separately by the addon.
    /// Input sampling/locks (C9), the FaultBarrier (C10), and the lifecycle state
    /// machine (C12) hook in here later; availability is currently the
    /// <see cref="DearImGuiKSP.IsAvailable"/> flag flipped by Composition after bridge init.
    /// </summary>
    internal sealed class FrameLoopOrchestrator
    {
        private readonly INativeBridge _bridge;
        private readonly ConsumerRegistry _registry;

        internal FrameLoopOrchestrator(INativeBridge bridge, ConsumerRegistry registry)
        {
            _bridge = bridge;
            _registry = registry;
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

            _bridge.BeginUiFrame(width, height, deltaTime);
            foreach (ConsumerRegistry.ConsumerRegistration consumer in _registry.Ordered)
            {
                if (!consumer.Enabled)
                {
                    continue;
                }
                // TODO(C10): FaultBarrier replaces this placeholder — counting + auto-disable.
                try
                {
                    consumer.Callback();
                }
                catch (Exception ex)
                {
                    DearImGuiKSP.Log?.Error("Consumer '" + consumer.Id + "' threw an exception: " + ex);
                }
            }
            _bridge.EndUiFrame();
        }
    }
}
