using System;
using System.Collections.Generic;
using UnityEngine;

namespace DearImGuiKSPDemo.Telemetry
{
    /// <summary>
    /// Approximate per-stage Δv/Isp/burn-time/propellant-fraction analyzer
    /// (spec §5.5, §6.2; plan §2 StageSnapshot; C20). Recomputes ONLY on
    /// staging events (<c>GameEvents.onStageActivate</c> / <c>onStageSeparation</c>,
    /// which set a dirty flag) and on a 1 s timer — never per frame — and
    /// publishes an immutable-per-recompute <see cref="Snapshot"/> that the
    /// StagePanel reads as its sole data source.
    /// </summary>
    /// <remarks>
    /// Computation model (approximation is spec-sanctioned; every figure the
    /// panel renders is labeled "approx"):
    /// parts are grouped by <c>Part.inverseStage</c>; stages are evaluated from
    /// <c>Vessel.currentStage</c> downward. A stage's engines are the
    /// <c>ModuleEngines</c> modules on parts in that stage; stage Isp is the
    /// thrust-weighted vacuum Isp (<c>atmosphereCurve.Evaluate(0)</c>); usable
    /// propellant is the stage's own tanks' content limited by the scarcest
    /// engine propellant per its normalized ratio (IntakeAir-style
    /// <c>ignoreForIsp</c> propellants excluded); Δv is the rocket equation over
    /// the remaining dry mass (all parts at or above the stage); burn time is
    /// usable propellant over full-throttle mass flow (Σ thrust / Isp·g0).
    /// Crossfeed, thrust curves, and atmospheric Isp are NOT modeled.
    /// All mass figures are tonnes (KSP's native unit for part mass and
    /// resource density), thrust in kN, so kN/(m/s) = t/s makes the burn-time
    /// ratio consistent. Stages without engines or propellant yield zeros —
    /// never exceptions. Recompute may allocate freely (1 Hz cadence); the
    /// per-frame path is a dirty-flag check and a time comparison.
    /// </remarks>
    internal sealed class StageAnalyzer
    {
        private const float RecomputeIntervalSeconds = 1f;

        // Standard gravity, matching ModuleEngines.g (KSPSOURCE/ModuleEngines.cs:281).
        private const double StandardGravity = 9.80665;

        /// <summary>
        /// Immutable per-stage figures for one recompute. Structs in a
        /// snapshot array — the panel reads them without copying per frame.
        /// All figures are approximations; Δv in m/s, Isp in s, burn time in s,
        /// fraction 0–1 (usable propellant mass over capacity mass).
        /// </summary>
        public struct StageInfo
        {
            public int Stage;
            public bool HasEngines;
            public double ApproxDeltaV;
            public double ApproxIsp;
            public double BurnTimeSeconds;
            public double PropellantFraction;
        }

        /// <summary>
        /// Immutable-per-recompute analysis result. Replaces the previous
        /// instance wholesale; the panel detects changes by reference.
        /// </summary>
        public sealed class Snapshot
        {
            /// <summary>Shared empty result (no active vessel / nothing to analyze).</summary>
            public static readonly Snapshot Empty = new Snapshot(new StageInfo[0], 0.0, false);

            /// <summary>Per-stage figures, ordered from current stage downward.</summary>
            public readonly StageInfo[] Stages;

            /// <summary>Sum of per-stage approx Δv, m/s.</summary>
            public readonly double TotalApproxDeltaV;

            /// <summary>True when a vessel was analyzed; false for the empty snapshot.</summary>
            public readonly bool HasVessel;

            internal Snapshot(StageInfo[] stages, double totalApproxDeltaV, bool hasVessel)
            {
                Stages = stages;
                TotalApproxDeltaV = totalApproxDeltaV;
                HasVessel = hasVessel;
            }
        }

        private Snapshot _snapshot = Snapshot.Empty;
        private bool _dirty = true;
        private float _lastRecomputeTime = float.NegativeInfinity;

        /// <summary>Latest published snapshot; never null.</summary>
        public Snapshot Current
        {
            get { return _snapshot; }
        }

        /// <summary>
        /// Subscribes the staging-event dirty flags. Called from the addon's
        /// Start (after the availability gate); paired with <see cref="Dispose"/>.
        /// </summary>
        public void Subscribe()
        {
            GameEvents.onStageActivate.Add(OnStageActivate);
            GameEvents.onStageSeparation.Add(OnStageSeparation);
            _dirty = true;
        }

        /// <summary>Unsubscribes the staging-event dirty flags.</summary>
        public void Dispose()
        {
            GameEvents.onStageActivate.Remove(OnStageActivate);
            GameEvents.onStageSeparation.Remove(OnStageSeparation);
        }

        // Event handlers only set the dirty flag: the actual recompute runs on
        // the main thread inside Tick (Unity game-logic event threading is not
        // a contract this analyzer relies on).
        private void OnStageActivate(int stage)
        {
            _dirty = true;
        }

        private void OnStageSeparation(EventReport report)
        {
            _dirty = true;
        }

        /// <summary>
        /// Recompute gate, called once per frame from the addon's Update.
        /// Dirty-flag check plus a 1 s timer — the only per-frame cost is a
        /// float comparison; part iteration happens exclusively inside
        /// <see cref="Recompute"/>.
        /// </summary>
        public void Tick()
        {
            if (!_dirty && Time.time - _lastRecomputeTime < RecomputeIntervalSeconds)
            {
                return;
            }
            _dirty = false;
            _lastRecomputeTime = Time.time;
            Recompute();
        }

        private void Recompute()
        {
            Vessel vessel = FlightGlobals.ActiveVessel;
            if (vessel == null || vessel.Parts == null)
            {
                _snapshot = Snapshot.Empty;
                return;
            }
            _snapshot = Compute(vessel);
        }

        private static Snapshot Compute(Vessel vessel)
        {
            List<Part> parts = vessel.Parts;
            int currentStage = vessel.currentStage;
            if (currentStage < 0)
            {
                currentStage = 0;
            }

            // Per-stage accumulation buckets (index = inverseStage). Dry mass
            // becomes "remaining mass" via a low-to-high suffix sum below.
            int[] partCount = new int[currentStage + 1];
            double[] dryMassBucket = new double[currentStage + 1];
            List<ModuleEngines>[] enginesByStage = new List<ModuleEngines>[currentStage + 1];
            Dictionary<string, double>[] availableByStage = new Dictionary<string, double>[currentStage + 1];
            Dictionary<string, double>[] capacityByStage = new Dictionary<string, double>[currentStage + 1];

            for (int i = 0; i < parts.Count; i++)
            {
                Part part = parts[i];
                if (part == null)
                {
                    continue;
                }
                int stage = part.inverseStage;
                if (stage < 0)
                {
                    stage = 0;
                }
                if (stage > currentStage)
                {
                    continue; // already jettisoned — not part of any future stage
                }

                partCount[stage]++;
                dryMassBucket[stage] += part.mass;

                for (int m = 0; m < part.Modules.Count; m++)
                {
                    ModuleEngines engine = part.Modules[m] as ModuleEngines;
                    if (engine == null)
                    {
                        continue;
                    }
                    List<ModuleEngines> list = enginesByStage[stage];
                    if (list == null)
                    {
                        list = new List<ModuleEngines>();
                        enginesByStage[stage] = list;
                    }
                    list.Add(engine);
                }

                PartResourceList resources = part.Resources;
                if (resources == null || !resources.IsValid)
                {
                    continue;
                }
                for (int r = 0; r < resources.Count; r++)
                {
                    PartResource resource = resources[r];
                    if (resource == null)
                    {
                        continue;
                    }
                    Dictionary<string, double> available = availableByStage[stage];
                    if (available == null)
                    {
                        available = new Dictionary<string, double>();
                        capacityByStage[stage] = new Dictionary<string, double>();
                        availableByStage[stage] = available;
                    }
                    double amount;
                    if (available.TryGetValue(resource.resourceName, out amount))
                    {
                        available[resource.resourceName] = amount + resource.amount;
                    }
                    else
                    {
                        available[resource.resourceName] = resource.amount;
                    }
                    Dictionary<string, double> capacity = capacityByStage[stage];
                    double maxAmount;
                    if (capacity.TryGetValue(resource.resourceName, out maxAmount))
                    {
                        capacity[resource.resourceName] = maxAmount + resource.maxAmount;
                    }
                    else
                    {
                        capacity[resource.resourceName] = resource.maxAmount;
                    }
                }
            }

            // Remaining dry mass per stage: everything at or above the stage
            // (lower/equal inverseStage) is still onboard while it burns.
            double[] remainingDryMass = new double[currentStage + 1];
            double running = 0.0;
            for (int s = 0; s <= currentStage; s++)
            {
                running += dryMassBucket[s];
                remainingDryMass[s] = running;
            }

            List<StageInfo> stages = new List<StageInfo>();
            double totalDeltaV = 0.0;
            for (int s = currentStage; s >= 0; s--)
            {
                if (partCount[s] == 0)
                {
                    continue; // no parts at this index — not a stage
                }
                StageInfo info = ComputeStage(
                    s,
                    enginesByStage[s],
                    availableByStage[s],
                    capacityByStage[s],
                    remainingDryMass[s]);
                totalDeltaV += info.ApproxDeltaV;
                stages.Add(info);
            }

            return new Snapshot(stages.ToArray(), totalDeltaV, true);
        }

        private static StageInfo ComputeStage(
            int stage,
            List<ModuleEngines> engines,
            Dictionary<string, double> available,
            Dictionary<string, double> capacity,
            double dryMass)
        {
            StageInfo info = new StageInfo();
            info.Stage = stage;
            if (engines == null || engines.Count == 0)
            {
                // Decoupler-only / probe stage: zeros by contract, no exceptions.
                return info;
            }
            info.HasEngines = true;

            double thrustSum = 0.0;
            double ispTimesThrustSum = 0.0;
            double massFlow = 0.0; // tonnes per second at full throttle

            // Thrust-weighted aggregate propellant mix (normalized per engine,
            // ignoreForIsp propellants such as IntakeAir excluded).
            Dictionary<string, double> aggregateRatio = new Dictionary<string, double>();

            for (int i = 0; i < engines.Count; i++)
            {
                ModuleEngines engine = engines[i];
                float isp = engine.atmosphereCurve != null ? engine.atmosphereCurve.Evaluate(0f) : 0f;
                float thrust = engine.maxThrust * engine.thrustPercentage * 0.01f;
                if (isp <= 0f || thrust <= 0f)
                {
                    continue;
                }
                thrustSum += thrust;
                ispTimesThrustSum += thrust * isp;
                // kN / (m/s) = t/s — see the class remarks on unit consistency.
                massFlow += thrust / (isp * StandardGravity);

                List<Propellant> propellants = engine.propellants;
                if (propellants == null)
                {
                    continue;
                }
                double ratioSum = 0.0;
                for (int p = 0; p < propellants.Count; p++)
                {
                    Propellant propellant = propellants[p];
                    if (propellant != null && !propellant.ignoreForIsp)
                    {
                        ratioSum += propellant.ratio;
                    }
                }
                if (ratioSum <= 0.0)
                {
                    continue;
                }
                for (int p = 0; p < propellants.Count; p++)
                {
                    Propellant propellant = propellants[p];
                    if (propellant == null || propellant.ignoreForIsp)
                    {
                        continue;
                    }
                    double normalized = propellant.ratio / ratioSum;
                    double current;
                    if (aggregateRatio.TryGetValue(propellant.name, out current))
                    {
                        aggregateRatio[propellant.name] = current + thrust * normalized;
                    }
                    else
                    {
                        aggregateRatio[propellant.name] = thrust * normalized;
                    }
                }
            }

            if (thrustSum <= 0.0)
            {
                info.HasEngines = false;
                return info;
            }

            info.ApproxIsp = ispTimesThrustSum / thrustSum;

            // Usable propellant limited by the scarcest propellant per the
            // aggregate mix ratio; mass = resource units x definition density.
            double usableMass = 0.0;
            double capacityMass = 0.0;
            if (aggregateRatio.Count > 0 && available != null)
            {
                double limitUnits = double.MaxValue;
                double limitCapacityUnits = double.MaxValue;
                foreach (KeyValuePair<string, double> pair in aggregateRatio)
                {
                    double ratio = pair.Value / thrustSum; // normalized mix share
                    double amount;
                    if (!available.TryGetValue(pair.Key, out amount))
                    {
                        amount = 0.0;
                    }
                    if (ratio > 0.0)
                    {
                        double engineLimited = amount / ratio;
                        if (engineLimited < limitUnits)
                        {
                            limitUnits = engineLimited;
                        }
                    }
                    if (capacity != null)
                    {
                        double maxAmount;
                        if (!capacity.TryGetValue(pair.Key, out maxAmount))
                        {
                            maxAmount = 0.0;
                        }
                        if (ratio > 0.0)
                        {
                            double capacityLimited = maxAmount / ratio;
                            if (capacityLimited < limitCapacityUnits)
                            {
                                limitCapacityUnits = capacityLimited;
                            }
                        }
                    }
                }
                if (limitUnits == double.MaxValue)
                {
                    limitUnits = 0.0;
                }
                if (limitCapacityUnits == double.MaxValue)
                {
                    limitCapacityUnits = 0.0;
                }

                foreach (KeyValuePair<string, double> pair in aggregateRatio)
                {
                    double ratio = pair.Value / thrustSum;
                    double density = DensityOf(pair.Key);
                    if (density <= 0.0)
                    {
                        continue;
                    }
                    usableMass += ratio * limitUnits * density;
                    capacityMass += ratio * limitCapacityUnits * density;
                }
            }

            if (usableMass < 0.0)
            {
                usableMass = 0.0;
            }
            info.PropellantFraction = capacityMass > 0.0
                ? Clamp01(usableMass / capacityMass)
                : 0.0;
            info.BurnTimeSeconds = massFlow > 0.0 ? usableMass / massFlow : 0.0;

            if (usableMass > 0.0 && dryMass > 0.0 && info.ApproxIsp > 0.0)
            {
                double wetMass = dryMass + usableMass;
                info.ApproxDeltaV = info.ApproxIsp * StandardGravity * Math.Log(wetMass / dryMass);
            }

            return info;
        }

        // Tons per resource unit (PartResourceDefinition.density, KSPSOURCE/
        // PartResourceDefinition.cs:57). Cached across stages per recompute via
        // the stock PartResourceLibrary singleton.
        private static float DensityOf(string resourceName)
        {
            PartResourceDefinition definition = PartResourceLibrary.Instance.GetDefinition(resourceName);
            return definition != null ? definition.density : 0f;
        }

        private static double Clamp01(double value)
        {
            if (value < 0.0)
            {
                return 0.0;
            }
            if (value > 1.0)
            {
                return 1.0;
            }
            return value;
        }
    }
}
