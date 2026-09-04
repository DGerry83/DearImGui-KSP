using UnityEngine;

namespace DearImGuiKSPDemo.Telemetry
{
    /// <summary>
    /// Reads stock flight state once per frame into the four locked-contract channel
    /// rings (spec §6.2; C18): altitude (m), dynamic pressure (kPa), main throttle
    /// (0–1), and g-force. Called from the TelemetryAddon's registered frame callback
    /// BEFORE the window-visibility check, so sampling continues while the window is
    /// closed and history stays warm on reopen (spec §5.5). No-ops without an active
    /// vessel — scene transitions and vessel loss never throw. No string building, no
    /// per-frame managed allocation: each channel write is one ring-slot array store.
    /// </summary>
    internal sealed class TelemetrySampler
    {
        private readonly RingBuffer _altitude = new RingBuffer();
        private readonly RingBuffer _dynamicPressurekPa = new RingBuffer();
        private readonly RingBuffer _mainThrottle = new RingBuffer();
        private readonly RingBuffer _geeForce = new RingBuffer();

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
        /// active vessel (pre-launch, vessel switch, scene transitions).
        /// </summary>
        public void Sample()
        {
            Vessel vessel = FlightGlobals.ActiveVessel;
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
