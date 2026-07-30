namespace DearKSP.Application
{
    /// <summary>
    /// Executes the per-frame sequence: sample input → compute capture → apply locks →
    /// invoke consumer callbacks in order → ImGui NewFrame/Render → native render handoff (spec §5.3).
    /// TODO(milestone 3): implement against INativeBridge, IInputLockGateway, IGameEventSource.
    /// </summary>
    internal sealed class FrameLoopOrchestrator
    {
    }
}
