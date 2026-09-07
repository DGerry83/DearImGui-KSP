using UnityEngine;

namespace DearImGuiKSPDemo.Telemetry
{
    /// <summary>
    /// Reads stock flight state once per frame into the four locked-contract channel
    /// rings (spec §6.2; C18): altitude (m), dynamic pressure (kPa), main throttle
    /// (0–1), and g-force. Called from the TelemetryAddon's registered frame callback
    /// BEFORE the window-visibility check, so sampling continues while the window is
    /// closed and history stays warm on reopen (spec §5.5). No-ops without an active
    /// vessel — scene transitions and vessel loss never throw. The rings are
    /// recreated (cleared) whenever the active vessel changes, so two vehicles'
    /// traces never concatenate in one history (G3-36); a vessel switch, a switch to
    /// no vessel, and a scene change all reset. No string building, no per-frame
    /// managed allocation: each channel write is one ring-slot array store, and the
    /// ring replacement cost is paid only on the (rare) vessel change.
    /// </summary>
    internal sealed class TelemetrySampler
    {
        // Non-readonly: replaced (fresh empty rings) on active-vessel change. The
        // panels read through the properties every frame, so they pick up the new
        // instances without any re-wiring.
        private RingBuffer _altitude = new RingBuffer();
        private RingBuffer _dynamicPressurekPa = new RingBuffer();
        private RingBuffer _mainThrottle = new RingBuffer();
        private RingBuffer _geeForce = new RingBuffer();

        // Identity of the vessel the current rings belong to; any reference change
        // (switch, vessel loss, scene change) clears the history.
        private Vessel _sampledVessel;

        /// <summary>Altitude above sea level, meters (oldest-to-newest spans).</summary>
        public RingBuffer Altitude
        {
            get { return _altitude; }
        }

        /// <summary>Dynamic pressure, kPa.</summary>
        public RingBuffer DynamicPressurekPa
        {
            get { return _dynamicPressurekPa; }
        }

        /// <summary>Main throttle position, 0–1.</summary>
        public RingBuffer MainThrottle
        {
            get { return _mainThrottle; }
        }

        /// <summary>Current g-force.</summary>
        public RingBuffer GeeForce
        {
            get { return _geeForce; }
        }

        /// <summary>
        /// One sample per frame from the active vessel; silently skips frames with no
        /// active vessel (pre-launch, vessel switch, scene transitions). The rings are
        /// cleared whenever the active vessel changes so one vehicle's history never
        /// bleeds into the next (G3-36).
        /// </summary>
        public void Sample()
        {
            Vessel vessel = FlightGlobals.ActiveVessel;
            if (!ReferenceEquals(vessel, _sampledVessel))
            {
                _sampledVessel = vessel;
                _altitude = new RingBuffer();
                _dynamicPressurekPa = new RingBuffer();
                _mainThrottle = new RingBuffer();
                _geeForce = new RingBuffer();
            }
            if (vessel == null)
            {
                return;
            }
            FlightCtrlState state = FlightInputHandler.state;
            if (state == null)
            {
                return;
            }
            _altitude.Push((float)vessel.altitude);
            _dynamicPressurekPa.Push((float)vessel.dynamicPressurekPa);
            _mainThrottle.Push(state.mainThrottle);
            _geeForce.Push((float)vessel.geeForce);
        }
    }
}
