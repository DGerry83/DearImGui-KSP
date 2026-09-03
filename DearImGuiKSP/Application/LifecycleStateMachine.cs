using System;
using DearImGuiKSP.Application.Interfaces;

namespace DearImGuiKSP.Application
{
    internal enum LifecycleState
    {
        Uninitialized,
        Initializing,
        Running,
        Suspended,
        Failed
    }

    /// <summary>
    /// Enforces the library lifecycle: Uninitialized → Initializing → Running,
    /// plus Suspended (F2 / loading screens) and Failed (session-permanent) (spec §5.2).
    /// </summary>
    internal sealed class LifecycleStateMachine
    {
        private readonly ILogger _log;
        private bool _uiHidden;
        private bool _loading;

        internal LifecycleStateMachine(ILogger log)
        {
            _log = log;
            State = LifecycleState.Uninitialized;
        }

        internal LifecycleState State { get; private set; }

        internal bool IsRunning => State == LifecycleState.Running;

        /// <summary>
        /// Fired once per session, after the transition to terminal Failed, with the
        /// failure kind for the player-facing popup (C13; spec §5.4, §7).
        /// </summary>
        internal event Action<FailureKind> EnteredFailed;

        internal void MarkInitializing()
        {
            TransitionTo(LifecycleState.Initializing);
        }

        internal void MarkRunning()
        {
            TransitionTo(LifecycleState.Running);
            EvaluateSuspended();
        }

        internal void SetUiVisible(bool visible)
        {
            _uiHidden = !visible;
            if (State != LifecycleState.Running && State != LifecycleState.Suspended)
            {
                _log?.Warn("SetUiVisible ignored: state machine is " + State);
                return;
            }
            EvaluateSuspended();
        }

        internal void SetLoading(bool loading)
        {
            _loading = loading;
            if (State != LifecycleState.Running && State != LifecycleState.Suspended)
            {
                _log?.Warn("SetLoading ignored: state machine is " + State);
                return;
            }
            EvaluateSuspended();
        }

        internal void Fail(FailureKind kind, string reason)
        {
            if (State == LifecycleState.Failed)
            {
                return;
            }

            _log?.Error("Lifecycle failure (" + kind + "): " + reason);
            TransitionTo(LifecycleState.Failed);
            if (State == LifecycleState.Failed && EnteredFailed != null)
            {
                EnteredFailed(kind);
            }
        }

        private void EvaluateSuspended()
        {
            if (State != LifecycleState.Running && State != LifecycleState.Suspended)
            {
                return;
            }

            bool shouldSuspend = _uiHidden || _loading;
            if (shouldSuspend && State == LifecycleState.Running)
            {
                TransitionTo(LifecycleState.Suspended);
            }
            else if (!shouldSuspend && State == LifecycleState.Suspended)
            {
                TransitionTo(LifecycleState.Running);
            }
        }

        private void TransitionTo(LifecycleState next)
        {
            if (State == next)
            {
                return;
            }

            if (!IsLegalTransition(State, next))
            {
                _log?.Warn("Illegal lifecycle transition: " + State + " -> " + next + "; ignoring.");
                return;
            }

            LifecycleState previous = State;
            State = next;
            _log?.Debug("Lifecycle transition: " + previous + " -> " + next);
        }

        private static bool IsLegalTransition(LifecycleState from, LifecycleState to)
        {
            if (from == LifecycleState.Failed)
            {
                return false;
            }

            switch (to)
            {
                case LifecycleState.Uninitialized:
                    return false;
                case LifecycleState.Initializing:
                    return from == LifecycleState.Uninitialized;
                case LifecycleState.Running:
                    return from == LifecycleState.Initializing || from == LifecycleState.Suspended;
                case LifecycleState.Suspended:
                    return from == LifecycleState.Running;
                case LifecycleState.Failed:
                    return true;
                default:
                    return false;
            }
        }
    }
}
