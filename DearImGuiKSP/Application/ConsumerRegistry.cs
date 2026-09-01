using System;
using System.Collections.Generic;

namespace DearImGuiKSP.Application
{
    /// <summary>
    /// Tracks registered consumers in registration order with per-consumer fault state.
    /// Registration order is the MVP z-order (spec §5.3). Implemented in C7; the fault
    /// fields (<see cref="ConsumerRegistration.ConsecutiveFailureCount"/>) are counted
    /// by <see cref="FaultBarrier"/> (C10).
    /// </summary>
    internal sealed class ConsumerRegistry
    {
        /// <summary>One registered consumer: id, per-frame callback, and fault state.</summary>
        internal sealed class ConsumerRegistration
        {
            internal readonly string Id;
            internal readonly Action Callback;
            internal bool Enabled = true;
            internal int ConsecutiveFailureCount = 0;

            internal ConsumerRegistration(string id, Action callback)
            {
                Id = id;
                Callback = callback;
            }
        }

        private readonly List<ConsumerRegistration> _ordered = new List<ConsumerRegistration>();
        private readonly Dictionary<string, ConsumerRegistration> _byId = new Dictionary<string, ConsumerRegistration>();

        /// <summary>Registrations in registration order; the frame loop iterates this.</summary>
        internal List<ConsumerRegistration> Ordered => _ordered;

        /// <summary>Registers a consumer. Returns false when the id is already taken.</summary>
        internal bool TryRegister(string id, Action callback)
        {
            if (_byId.ContainsKey(id))
            {
                return false;
            }
            var registration = new ConsumerRegistration(id, callback);
            _ordered.Add(registration);
            _byId.Add(id, registration);
            return true;
        }

        /// <summary>Removes a consumer. Returns true when it was registered.</summary>
        internal bool Unregister(string id)
        {
            ConsumerRegistration registration;
            if (!_byId.TryGetValue(id, out registration))
            {
                return false;
            }
            _byId.Remove(id);
            _ordered.Remove(registration);
            return true;
        }
    }
}
