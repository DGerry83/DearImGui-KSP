using System;
using UnityEngine;
using Color32 = UnityEngine.Color32;

namespace DearImGuiKSPDemo.Telemetry
{
    /// <summary>
    /// Orbit tab content (spec §5.5, §6.2; chunk C21, M6): a 2D orbital radar
    /// plus an orbital elements readout, redrawn per frame from the active
    /// vessel's live <c>Orbit</c> object. The radar is a top-down projection
    /// onto the vessel's orbital plane in the periapsis-aligned frame (angle 0 = the
    /// periapsis direction): the reference body sits at the ellipse focus, the
    /// ellipse comes straight from semiMajorAxis/eccentricity, and markers
    /// cover the live vessel position, Ap/Pe, the current target (same SOI
    /// only), and any maneuver nodes (display-only). All ImGui calls go through the library's public API
    /// only (<see cref="DearImGuiKSP.ImGuiDraw"/>, <see cref="DearImGuiKSP.DearImGuiKSP"/>).
    /// </summary>
    /// <remarks>
    /// Allocation story (§5.9 hot-path discipline): the steady-state per-frame
    /// path allocates nothing — every radar position is struct math on the
    /// orbit element fields (the vessel's true anomaly is a maintained field,
    /// the cheapest live position form; node true anomalies use the public
    /// getMeanAnomalyAtUT/solveEccentricAnomaly/GetTrueAnomaly chain), all
    /// draw calls take cached constants, and the marker loop indexes the
    /// solver's node list without copying. The readout strings are the only
    /// allocations: they are preformatted at a 1 s cadence (or immediately when
    /// the orbit reference changes — the C20 render-cache precedent, here
    /// time-triggered because Orbit is mutated in place rather than
    /// snapshotted). <see cref="DearImGuiKSP.ImGuiDraw.AddText(Vector2, Color32, string)"/>
    /// is deliberately unused (its per-call UTF-8 encode would sit on the
    /// per-frame path); markers are geometric only. The whole radar body is
    /// wrapped in a try/catch — a display-only panel must never take the
    /// consumer's frame down on an unexpected scene/vessel state.
    /// Degradations: no active vessel -> "No active vessel." (this panel
    /// repeats the addon's contract so the tab slot stays self-contained);
    /// missing/degenerate orbit data -> a plain note; hyperbolic orbits (e >= 1)
    /// skip the ellipse entirely (an ellipse is undefined there) and say so;
    /// no target -> the target marker is simply absent.
    /// </remarks>
    internal sealed class OrbitPanel
    {
        private const string NoVesselText = "No active vessel.";
        private const string NoOrbitText = "No orbit data.";
        private const string HyperbolicText = "Hyperbolic orbit - radar view unavailable.";
        private const string UnavailableText = "Orbit data unavailable.";

        // Canvas geometry (pixels). Square canvas, fixed size keeps the
        // per-frame path free of content-region queries.
        private const float CanvasWidth = 360f;
        private const float CanvasHeight = 360f;
        private const float CanvasMargin = 16f;
        private const float CanvasRounding = 4f;

        // Marker sizes (pixels).
        private const float VesselMarkerRadius = 4f;
        private const float ApPeMarkerRadius = 3f;
        private const float TargetMarkerRadius = 4f;
        private const float NodeMarkerRadius = 3f;
        private const float NodeCrossHalf = 6f;
        private const float OrbitLineThickness = 1.5f;
        private const float MinBodyRadiusPx = 2f;

        // Readout reformat cadence (seconds of game UT).
        private const double ReadoutInterval = 1.0;

        // KSP unit trap: Orbit.inclination and Orbit.argumentOfPeriapsis are
        // DEGREES (KSPSOURCE/Orbit.cs:619 and :708 — both built via
        // * 180/PI, and stock itself converts back via * PI/180 at :2618/:2627)
        // while trueAnomaly and the getMeanAnomalyAtUT/solveEccentricAnomaly/
        // GetTrueAnomaly chain are RADIANS (:712, Atan2 with no conversion).
        // The radar's math frame is radians; element-angle offsets convert
        // through this constant.
        private const double DegToRad = Math.PI / 180.0;

        // Radar colors: KSP palette accents where one fits (vessel green, Ap/Pe
        // oranges, text); target/node need hues the palette does not carry, so
        // those two are panel-local constants.
        private static readonly Color32 RadarBackground = new Color32(30, 32, 38, 110);
        private static readonly Color32 BodyFill = new Color32(96, 100, 110, 255);
        private static readonly Color32 OrbitLine = DearImGuiKSP.Application.KspPalette.TextLightGrey;
        private static readonly Color32 VesselMarker = DearImGuiKSP.Application.KspPalette.GreenLight;
        private static readonly Color32 ApMarker = DearImGuiKSP.Application.KspPalette.OrangeLight;
        private static readonly Color32 PeMarker = DearImGuiKSP.Application.KspPalette.OrangeDark;
        private static readonly Color32 TargetMarker = new Color32(80, 200, 255, 255);
        private static readonly Color32 NodeMarker = new Color32(230, 120, 255, 255);
        private static readonly Color32 ReadoutText = DearImGuiKSP.Application.KspPalette.TextOffWhite;

        // Render cache, rebuilt at ReadoutInterval cadence or on orbit change.
        private Orbit _lastOrbit;
        private double _lastReadoutUt = double.NegativeInfinity;
        private string _apLine = "Ap: n/a";
        private string _peLine = "Pe: n/a";
        private string _periodLine = "Period: n/a";
        private string _inclLine = "Inclination: n/a";
        private string _eccLine = "Eccentricity: n/a";
        private string _smaLine = "SMA: n/a";

        /// <summary>
        /// Content of the Orbit tab: the radar canvas (when the orbit is
        /// elliptical and sane) followed by the elements readout. Call inside
        /// a tab item scope whose Visible is true; handles the no-vessel /
        /// no-orbit / hyperbolic degradation itself.
        /// </summary>
        public void DrawImGui()
        {
            Vessel vessel = FlightGlobals.ActiveVessel;
            if (vessel == null)
            {
                DearImGuiKSP.DearImGuiKSP.Text(NoVesselText);
                return;
            }
            Orbit orbit = vessel.orbit;
            if (orbit == null || orbit.referenceBody == null ||
                double.IsNaN(orbit.semiMajorAxis) || double.IsNaN(orbit.eccentricity))
            {
                DearImGuiKSP.DearImGuiKSP.Text(NoOrbitText);
                return;
            }

            if (orbit.eccentricity >= 1.0)
            {
                // No ellipse exists for e >= 1; say so instead of drawing a
                // broken one. The elements readout still renders (Pe, etc.).
                DearImGuiKSP.DearImGuiKSP.Text(HyperbolicText);
            }
            else
            {
                try
                {
                    DrawRadar(vessel, orbit);
                }
                catch (Exception)
                {
                    // Display-only panel: never let an unexpected orbit /
                    // target / node state take the consumer's frame down.
                    DearImGuiKSP.DearImGuiKSP.Text(UnavailableText);
                }
            }

            DrawReadout(orbit);
        }

        // The radar: reserve the canvas slot, then paint background, body disc
        // at the focus, the orbit ellipse, and every marker. Struct math only;
        // the try/catch in the caller covers the whole body.
        private static void DrawRadar(Vessel vessel, Orbit orbit)
        {
            // Cursor anchor: capture BEFORE Dummy — Dummy advances the cursor,
            // so the canvas origin is the pre-Dummy cursor position.
            Vector2 origin = DearImGuiKSP.ImGuiDraw.GetCursorScreenPos();
            DearImGuiKSP.ImGuiDraw.Dummy(CanvasWidth, CanvasHeight);

            float halfW = CanvasWidth * 0.5f;
            float halfH = CanvasHeight * 0.5f;
            Vector2 focus = new Vector2(origin.x + halfW, origin.y + halfH);
            float fitRadius = (halfW < halfH ? halfW : halfH) - CanvasMargin;

            double a = orbit.semiMajorAxis;
            double e = orbit.eccentricity;
            double apR = a * (1.0 + e);
            if (fitRadius <= 0f || apR <= 0.0 || double.IsInfinity(apR))
            {
                DearImGuiKSP.DearImGuiKSP.Text(UnavailableText);
                return;
            }
            float scale = (float)(fitRadius / apR); // pixels per meter

            // argumentOfPeriapsis (DEGREES — see DegToRad above) is kept here
            // only for the target/node marker offsets below. The ellipse
            // itself is drawn in the SAME periapsis-aligned frame as every
            // marker (angle 0 = the periapsis direction, +x on screen), so it
            // takes no rotation: its center simply sits c BEHIND the focus
            // along the periapsis line (screen -x).
            double argPe = orbit.argumentOfPeriapsis;
            double c = a * e; // focus-to-center distance, along the periapsis line
            double b = a * Math.Sqrt(Math.Max(0.0, 1.0 - e * e));

            Vector2 center = new Vector2((float)(focus.x - c * scale), focus.y);
            Vector2 radii = new Vector2((float)(a * scale), (float)(b * scale));

            DearImGuiKSP.ImGuiDraw.AddRectFilled(
                origin,
                new Vector2(origin.x + CanvasWidth, origin.y + CanvasHeight),
                RadarBackground,
                CanvasRounding);

            double bodyRadiusPx = orbit.referenceBody.Radius * scale;
            if (bodyRadiusPx < MinBodyRadiusPx)
            {
                bodyRadiusPx = MinBodyRadiusPx;
            }
            DearImGuiKSP.ImGuiDraw.AddCircleFilled(focus, (float)bodyRadiusPx, BodyFill);

            DearImGuiKSP.ImGuiDraw.AddEllipse(center, radii, OrbitLine, 0f, OrbitLineThickness);

            // Vessel: live true-anomaly field -> polar -> radar coords (axes =
            // vessel periapsis direction + in-plane perpendicular, so the
            // in-plane angle is just nu).
            double nu = orbit.trueAnomaly;
            double r = RadiusAtTrueAnomaly(a, e, nu);
            DearImGuiKSP.ImGuiDraw.AddCircleFilled(
                Project(focus, scale, r, nu), VesselMarkerRadius, VesselMarker);

            // Ap/Pe: apoapsis is opposite the periapsis direction (angle pi).
            DearImGuiKSP.ImGuiDraw.AddCircle(
                Project(focus, scale, apR, Math.PI), ApPeMarkerRadius, ApMarker);
            DearImGuiKSP.ImGuiDraw.AddCircle(
                Project(focus, scale, a * (1.0 - e), 0.0), ApPeMarkerRadius, PeMarker);

            DrawTargetMarker(orbit, focus, scale, argPe);
            DrawNodeMarkers(vessel, orbit, focus, scale, argPe);
        }

        // Target marker: only when the target has an orbit around the SAME
        // body (another SOI's frame does not map onto this radar); the marker
        // is simply absent otherwise. In-plane angle carries the argument-of-
        // periapsis offset between the two orbits (exact when coplanar, a
        // projection otherwise — display-only).
        private static void DrawTargetMarker(Orbit vesselOrbit, Vector2 focus, float scale, double argPe)
        {
            if (FlightGlobals.fetch == null)
            {
                return;
            }
            ITargetable target = FlightGlobals.fetch.VesselTarget;
            if (target == null)
            {
                return;
            }
            Orbit targetOrbit = target.GetOrbit();
            if (targetOrbit == null || targetOrbit.referenceBody != vesselOrbit.referenceBody ||
                double.IsNaN(targetOrbit.trueAnomaly))
            {
                return;
            }
            double tNu = targetOrbit.trueAnomaly;
            double tR = RadiusAtTrueAnomaly(
                targetOrbit.semiMajorAxis, targetOrbit.eccentricity, tNu);
            // tNu is radians; the argPe difference is degrees — convert.
            double tAngle = tNu + (targetOrbit.argumentOfPeriapsis - argPe) * DegToRad;
            DearImGuiKSP.ImGuiDraw.AddCircle(
                Project(focus, scale, tR, tAngle), TargetMarkerRadius, TargetMarker);
        }

        // Maneuver-node markers, display-only: each node sits on its pre-node
        // orbit (node.patch) at node.UT; the true anomaly there comes from the
        // public mean-anomaly chain (hyperbolic-safe). Nodes whose patch
        // circles another body are skipped. Marker is a circle plus a small
        // cross so it reads as a node rather than a body.
        private static void DrawNodeMarkers(
            Vessel vessel, Orbit vesselOrbit, Vector2 focus, float scale, double argPe)
        {
            PatchedConicSolver solver = vessel.patchedConicSolver;
            if (solver == null || solver.maneuverNodes == null)
            {
                return;
            }
            System.Collections.Generic.List<ManeuverNode> nodes = solver.maneuverNodes;
            for (int i = 0; i < nodes.Count; i++)
            {
                ManeuverNode node = nodes[i];
                if (node == null)
                {
                    continue;
                }
                Orbit patch = node.patch;
                if (patch == null || patch.referenceBody != vesselOrbit.referenceBody ||
                    double.IsNaN(node.UT))
                {
                    continue;
                }
                double nodeNu = TrueAnomalyAtUt(patch, node.UT);
                if (double.IsNaN(nodeNu))
                {
                    continue;
                }
                double nR = RadiusAtTrueAnomaly(patch.semiMajorAxis, patch.eccentricity, nodeNu);
                // nodeNu is radians; the argPe difference is degrees — convert.
                double nAngle = nodeNu + (patch.argumentOfPeriapsis - argPe) * DegToRad;
                Vector2 pos = Project(focus, scale, nR, nAngle);
                DearImGuiKSP.ImGuiDraw.AddCircle(pos, NodeMarkerRadius, NodeMarker);
                DearImGuiKSP.ImGuiDraw.AddLine(
                    new Vector2(pos.x - NodeCrossHalf, pos.y),
                    new Vector2(pos.x + NodeCrossHalf, pos.y),
                    NodeMarker);
                DearImGuiKSP.ImGuiDraw.AddLine(
                    new Vector2(pos.x, pos.y - NodeCrossHalf),
                    new Vector2(pos.x, pos.y + NodeCrossHalf),
                    NodeMarker);
            }
        }

        // Orbital radius at true anomaly: r = p / (1 + e cos nu), p = a(1-e^2).
        // Valid for e >= 1 as well (p negative, nu range-limited), so node and
        // target markers survive hyperbolic patches.
        private static double RadiusAtTrueAnomaly(double a, double e, double nu)
        {
            return a * (1.0 - e * e) / (1.0 + e * Math.Cos(nu));
        }

        // Radar-plane projection: math-frame polar (r, angle) -> screen pixels.
        // Angle 0 is the vessel's periapsis direction; screen y is down, hence
        // the minus on y.
        private static Vector2 Project(Vector2 focus, float scale, double r, double angle)
        {
            return new Vector2(
                (float)(focus.x + r * scale * Math.Cos(angle)),
                (float)(focus.y - r * scale * Math.Sin(angle)));
        }

        // True anomaly of an orbit at an arbitrary UT via the public anomaly
        // chain (what getRelativePositionAtT does internally, minus the frame
        // swizzle we do not need): mean anomaly -> eccentric/hyperbolic
        // anomaly -> true anomaly. Pure double math.
        private static double TrueAnomalyAtUt(Orbit orbit, double ut)
        {
            double meanAnomaly = orbit.getMeanAnomalyAtUT(ut);
            double eccAnomaly = orbit.solveEccentricAnomaly(meanAnomaly, orbit.eccentricity);
            return orbit.GetTrueAnomaly(eccAnomaly);
        }

        // Elements readout: cached strings, preformatted only at the 1 s
        // cadence or when the orbit reference changes (Orbit is mutated in
        // place, so there is no snapshot reference to watch — the C20
        // time-based equivalent). The string.Format calls below are the only
        // allocations in this panel and run at that cadence.
        private void DrawReadout(Orbit orbit)
        {
            double now = Planetarium.GetUniversalTime();
            if (!object.ReferenceEquals(orbit, _lastOrbit) ||
                Math.Abs(now - _lastReadoutUt) >= ReadoutInterval)
            {
                RebuildReadout(orbit);
                _lastOrbit = orbit;
                _lastReadoutUt = now;
            }

            DearImGuiKSP.DearImGuiKSP.Text(_apLine);
            DearImGuiKSP.DearImGuiKSP.Text(_peLine);
            DearImGuiKSP.DearImGuiKSP.Text(_periodLine);
            DearImGuiKSP.DearImGuiKSP.Text(_inclLine);
            DearImGuiKSP.DearImGuiKSP.Text(_eccLine);
            DearImGuiKSP.DearImGuiKSP.Text(_smaLine);
        }

        private void RebuildReadout(Orbit orbit)
        {
            bool elliptical = orbit.eccentricity < 1.0;
            double apA = orbit.ApA;
            double peA = orbit.PeA;
            _apLine = elliptical && !double.IsNaN(apA)
                ? string.Format("Ap: {0:0.0} km", apA / 1000.0)
                : "Ap: n/a";
            _peLine = !double.IsNaN(peA)
                ? string.Format("Pe: {0:0.0} km", peA / 1000.0)
                : "Pe: n/a";
            _periodLine = elliptical && !double.IsNaN(orbit.period) && orbit.period > 0.0
                ? "Period: " + KSPUtil.PrintTime(orbit.period, 4, false)
                : "Period: n/a";
            // Orbit.inclination is ALREADY degrees (KSPSOURCE/Orbit.cs:619) —
            // no conversion.
            _inclLine = string.Format("Inclination: {0:0.##} deg", orbit.inclination);
            _eccLine = string.Format("Eccentricity: {0:0.###}", orbit.eccentricity);
            _smaLine = string.Format("SMA: {0:0.0} km", orbit.semiMajorAxis / 1000.0);
        }
    }
}
