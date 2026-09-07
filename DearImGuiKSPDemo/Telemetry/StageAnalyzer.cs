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
    /// parts are grouped by <c>Part.inverseStage</c>; stages are evaluated in
    /// firing order, from <c>Vessel.currentStage</c> downward. A stage's
    /// engines are the <c>ModuleEngines</c> modules on parts in that stage —
    /// on multi-mode parts (RAPIER-class) only the SELECTED mode's module
    /// counts (<c>MultiModeEngine.runningPrimary</c> picks between the two
    /// <c>ModuleEnginesFX</c> modules by <c>engineID</c>; stock's dV sim does
    /// the same, KSPSOURCE/DeltaVEngineInfo.cs:124-140), matching what stock's
    /// engine display shows. Stage Isp is the thrust-weighted
    /// <c>atmosphereCurve</c> value at the CURRENT static pressure — stock's
    /// flight "Actual" situation (KSPSOURCE/DeltaVEngineInfo.cs:1915; equals
    /// vacuum Isp in vacuum, so this never diverges there). Per-engine fuel
    /// flow is pressure-independent (<c>maxFuelFlow = maxThrust /
    /// (atmosphereCurve.Evaluate(0) * g)</c>, KSPSOURCE/ModuleEngines.cs:1048),
    /// so burn time uses vacuum-rated flow regardless of situation.
    /// Usable propellant is POOLED per propellant over every unlocked tank
    /// (<c>PartResource.flowState</c> — locked tanks are dead mass, not
    /// propellant) still aboard during the burn, limited by the scarcest
    /// engine propellant per its normalized mix ratio (IntakeAir-style
    /// <c>ignoreForIsp</c> propellants excluded); each stage's consumption is
    /// subtracted from the pool before lower stages compute. Δv is the rocket
    /// equation with wet mass = remaining dry mass (all parts at or above the
    /// stage) plus ALL resource mass still aboard minus what higher stages
    /// already burned, and dry mass = wet minus this stage's usable
    /// propellant; burn time is usable propellant over full-throttle mass
    /// flow (Σ thrust / Isp·g0).
    /// Why pooled: stock simulates the REAL fuel-flow graph (flow priorities,
    /// crossfeed toggles, fuel lines) via <c>Part.GetConnectedResourceTotals
    /// </c>(..., simulate: true) and <c>RequestResource(..., simulate: true)</c>
    /// (KSPSOURCE/DeltaVEngineInfo.cs:271/:912) — reimplementing that graph is
    /// out of scope for a demo. Pooling matches stock on serial staging with
    /// default crossfeed; documented divergences: explicit flow priorities,
    /// disabled crossfeed and fuel lines are not modeled (everything pooled);
    /// engines in DIFFERENT stages firing simultaneously (parallel staging)
    /// burn sequentially here, so per-stage attribution shifts between those
    /// stages (the total stays close); velocity/atm-density Isp multipliers
    /// (jets) and thrust curves are not modeled; parts of already-fired
    /// stages are assumed jettisoned with their remaining contents.
    /// All mass figures are tonnes (KSP's native unit for part mass and
    /// resource density), thrust in kN, so kN/(m/s) = t/s makes the burn-time
    /// ratio consistent. Stages without engines or propellant yield zeros —
    /// never exceptions. Recompute may allocate freely (1 Hz cadence); the
    /// per-frame path is a dirty-flag check and a time comparison. The cadence
    /// timer runs on <c>Time.unscaledTime</c> so physics warp (scaled
    /// <c>Time.time</c> runs up to 4x fast) does not multiply the recompute
    /// rate.
    /// </remarks>
    internal sealed class StageAnalyzer
    {
        private const float RecomputeIntervalSeconds = 1f;

        // Standard gravity, matching ModuleEngines.g (KSPSOURCE/ModuleEngines.cs:281).
        private const double StandardGravity = 9.80665;

        /// <summary>
        /// Immutable per-stage figures for one recompute. Structs in a
        /// snapshot array — the panel reads them without copying per frame.
        /// All figures are approximations; Δv in m/s, Isp in s (at the current
        /// static pressure — stock's flight "Actual" situation), burn time in
        /// s, fraction 0–1 (usable propellant mass over the pooled, still-aboard
        /// capacity mass for this stage's engine mix).
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
        /// <see cref="Recompute"/>. The timer uses unscaled time: scaled
        /// <c>Time.time</c> accelerates with physics warp, which would
        /// multiply the recompute rate by the warp factor.
        /// </summary>
        public void Tick()
        {
            if (!_dirty && Time.unscaledTime - _lastRecomputeTime < RecomputeIntervalSeconds)
            {
                return;
            }
            _dirty = false;
            _lastRecomputeTime = Time.unscaledTime;
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
            // becomes "remaining mass" via a low-to-high suffix sum below;
            // resourceMassBucket does the same for the mass of everything the
            // tanks hold (locked tanks included — their contents are aboard).
            int[] partCount = new int[currentStage + 1];
            double[] dryMassBucket = new double[currentStage + 1];
            double[] resourceMassBucket = new double[currentStage + 1];
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

                // Multi-mode parts (RAPIER class) carry one ModuleEnginesFX
                // per mode, but only the SELECTED mode burns:
                // MultiModeEngine.runningPrimary picks between the two modes
                // (KSPSOURCE/MultiModeEngine.cs:42; SetPrimary/SetSecondary at
                // :394/:508 enable exactly one module), matched by engineID
                // (KSPSOURCE/ModuleEnginesFX.cs:26). The selection persists
                // while shutdown, so this matches the stock engine display
                // whether or not the engine is running. Summing both modes
                // would double the stage's thrust and mass flow.
                MultiModeEngine multiMode = null;
                for (int m = 0; m < part.Modules.Count; m++)
                {
                    multiMode = part.Modules[m] as MultiModeEngine;
                    if (multiMode != null)
                    {
                        break;
                    }
                }
                string activeEngineId = multiMode != null
                    ? (multiMode.runningPrimary ? multiMode.primaryEngineID : multiMode.secondaryEngineID)
                    : null;

                for (int m = 0; m < part.Modules.Count; m++)
                {
                    ModuleEngines engine = part.Modules[m] as ModuleEngines;
                    if (engine == null)
                    {
                        continue;
                    }
                    ModuleEnginesFX modeEngine = engine as ModuleEnginesFX;
                    if (activeEngineId != null && modeEngine != null &&
                        modeEngine.engineID != activeEngineId)
                    {
                        continue; // the non-selected mode of a multi-mode part
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
                    // Mass is mass: everything the tank holds rides the rocket
                    // equation's mass terms, locked or not.
                    resourceMassBucket[stage] += resource.amount * DensityOf(resource.resourceName);
                    if (!resource.flowState)
                    {
                        // Locked tank (KSPSOURCE/PartResource.cs:36): dead
                        // mass only — not usable propellant, and not capacity
                        // for the propellant-fraction readout.
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
            // cumulativeResourceMass is the same suffix sum over tank contents
            // (locked tanks included — mass is mass). poolAvailable/poolCapacity
            // are the POOLED propellant snapshots: propellant units/capacities
            // summed over all parts still aboard at stage s (the crossfeed
            // approximation — see the class remarks).
            double[] remainingDryMass = new double[currentStage + 1];
            double[] cumulativeResourceMass = new double[currentStage + 1];
            Dictionary<string, double>[] poolAvailable = new Dictionary<string, double>[currentStage + 1];
            Dictionary<string, double>[] poolCapacity = new Dictionary<string, double>[currentStage + 1];
            double running = 0.0;
            double runningResources = 0.0;
            Dictionary<string, double> runningAvailable = new Dictionary<string, double>();
            Dictionary<string, double> runningCapacity = new Dictionary<string, double>();
            for (int s = 0; s <= currentStage; s++)
            {
                running += dryMassBucket[s];
                runningResources += resourceMassBucket[s];
                remainingDryMass[s] = running;
                cumulativeResourceMass[s] = runningResources;
                MergeInto(runningAvailable, availableByStage[s]);
                MergeInto(runningCapacity, capacityByStage[s]);
                poolAvailable[s] = new Dictionary<string, double>(runningAvailable);
                poolCapacity[s] = new Dictionary<string, double>(runningCapacity);
            }

            // Burn pass in firing order (currentStage downward). Each stage
            // draws from the pooled propellant and its consumption is recorded
            // (consumedUnits / consumedMass) so lower stages see a depleted
            // pool — this is what keeps a shared tank from being counted once
            // per stage.
            double pressureAtm = Math.Max(0.0, vessel.staticPressurekPa) * PhysicsGlobals.KpaToAtmospheres;
            List<StageInfo> stages = new List<StageInfo>();
            double totalDeltaV = 0.0;
            Dictionary<string, double> consumedUnits = new Dictionary<string, double>();
            double consumedMass = 0.0;
            for (int s = currentStage; s >= 0; s--)
            {
                if (partCount[s] == 0)
                {
                    continue; // no parts at this index — not a stage
                }
                double burnedMass;
                StageInfo info = ComputeStage(
                    s,
                    enginesByStage[s],
                    poolAvailable[s],
                    poolCapacity[s],
                    consumedUnits,
                    consumedMass,
                    remainingDryMass[s],
                    cumulativeResourceMass[s],
                    pressureAtm,
                    out burnedMass);
                consumedMass += burnedMass;
                totalDeltaV += info.ApproxDeltaV;
                stages.Add(info);
            }

            return new Snapshot(stages.ToArray(), totalDeltaV, true);
        }

        // poolAvailable/poolCapacity: pooled unlocked propellant units and
        // capacities over all parts still aboard at this stage (the crossfeed
        // approximation). consumedUnits/consumedMass: what higher (already
        // burned) stages drew from the pool; consumedUnits is updated with
        // this stage's draw. dryMass/resourceMassAboard: part dry mass and
        // total tank-content mass (locked included) still aboard at ignition.
        // pressureAtm: current static pressure in atm for the situation Isp.
        private static StageInfo ComputeStage(
            int stage,
            List<ModuleEngines> engines,
            Dictionary<string, double> poolAvailable,
            Dictionary<string, double> poolCapacity,
            Dictionary<string, double> consumedUnits,
            double consumedMass,
            double dryMass,
            double resourceMassAboard,
            double pressureAtm,
            out double burnedMass)
        {
            StageInfo info = new StageInfo();
            info.Stage = stage;
            burnedMass = 0.0;
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
                float ispVac = engine.atmosphereCurve != null ? engine.atmosphereCurve.Evaluate(0f) : 0f;
                // Situation Isp: the curve at the CURRENT static pressure
                // (stock's flight "Actual" — DeltaVEngineInfo.cs:1915; equal
                // to ispVac in vacuum). Velocity/atm-density multipliers
                // (jets) are not modeled — see the class remarks.
                float ispSituation = engine.atmosphereCurve != null
                    ? engine.atmosphereCurve.Evaluate((float)pressureAtm)
                    : 0f;
                float thrust = engine.maxThrust * engine.thrustPercentage * 0.01f;
                if (ispVac <= 0f || ispSituation <= 0f || thrust <= 0f)
                {
                    continue;
                }
                thrustSum += thrust;
                ispTimesThrustSum += thrust * ispSituation;
                // Fuel flow is pressure-independent (maxFuelFlow =
                // maxThrust/(ispVac*g0), KSPSOURCE/ModuleEngines.cs:1048);
                // kN / (m/s) = t/s — see the class remarks on units.
                massFlow += thrust / (ispVac * StandardGravity);

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
            // aggregate mix ratio, drawn from the POOLED pool minus what
            // higher stages already burned; mass = resource units x
            // definition density.
            double usableMass = 0.0;
            double capacityMass = 0.0;
            if (aggregateRatio.Count > 0 && poolAvailable != null)
            {
                double limitUnits = double.MaxValue;
                double limitCapacityUnits = double.MaxValue;
                foreach (KeyValuePair<string, double> pair in aggregateRatio)
                {
                    double ratio = pair.Value / thrustSum; // normalized mix share
                    double amount;
                    if (!poolAvailable.TryGetValue(pair.Key, out amount))
                    {
                        amount = 0.0;
                    }
                    double consumed;
                    if (consumedUnits.TryGetValue(pair.Key, out consumed))
                    {
                        amount -= consumed;
                        if (amount < 0.0)
                        {
                            amount = 0.0; // higher stages drained it all
                        }
                    }
                    if (ratio > 0.0)
                    {
                        double engineLimited = amount / ratio;
                        if (engineLimited < limitUnits)
                        {
                            limitUnits = engineLimited;
                        }
                    }
                    if (poolCapacity != null)
                    {
                        double maxAmount;
                        if (!poolCapacity.TryGetValue(pair.Key, out maxAmount))
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

                // Record this stage's draw (in resource units) so lower
                // stages see a depleted pool.
                if (limitUnits > 0.0)
                {
                    foreach (KeyValuePair<string, double> pair in aggregateRatio)
                    {
                        double ratio = pair.Value / thrustSum;
                        double existing;
                        consumedUnits[pair.Key] =
                            (consumedUnits.TryGetValue(pair.Key, out existing) ? existing : 0.0) +
                            ratio * limitUnits;
                    }
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
            burnedMass = usableMass;

            if (usableMass > 0.0 && info.ApproxIsp > 0.0)
            {
                // Rocket equation over what is actually aboard at ignition:
                // dry parts plus every tank content, minus what higher stages
                // already burned. The unburnable remainder (locked tanks,
                // propellant beyond the limiting mix share) stays aboard
                // after the burn and belongs on both sides of the ratio.
                double massBeforeBurn = dryMass + Math.Max(0.0, resourceMassAboard - consumedMass);
                double massAfterBurn = massBeforeBurn - usableMass;
                if (massAfterBurn > 0.0)
                {
                    info.ApproxDeltaV = info.ApproxIsp * StandardGravity * Math.Log(massBeforeBurn / massAfterBurn);
                }
            }

            return info;
        }

        // Adds every entry of source into target (sum on key collision).
        // Null source is a no-op (stages without tanks).
        private static void MergeInto(Dictionary<string, double> target, Dictionary<string, double> source)
        {
            if (source == null)
            {
                return;
            }
            foreach (KeyValuePair<string, double> pair in source)
            {
                double existing;
                target[pair.Key] = target.TryGetValue(pair.Key, out existing)
                    ? existing + pair.Value
                    : pair.Value;
            }
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
