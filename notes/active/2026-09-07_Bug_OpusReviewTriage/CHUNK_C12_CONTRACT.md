# Chunk Contract: C12 — Demo flight-dynamics math
## Plan: Opus Review Remediation (`notes\plans\opus-review-remediation-contracts.md`)
## Date: 2026-09-07
## Chunk ID: C12
## Advances Milestone: Post-1.0.0 remediation wave (G2-13, G2-14, G2-15, G3-34, G3-35, G3-40)

### Scope
Theme: correctness of the demo telemetry windows' flight math. The demo teaches by example (sweep theme 7) — consumers copy this code, so wrong units and wrong staging math propagate. Demo-only contract (D7); library projects untouched.

- **G2-13** (`Telemetry/OrbitPanel.cs`, DrawRadar): the ellipse was drawn rotated by `argumentOfPeriapsis` while every marker is plotted in the periapsis-aligned frame (`Project` angle 0 = periapsis direction) — and the raw KSP field was fed to `Math.Cos`/`Math.Sin` despite being **degrees** (KSPSOURCE/Orbit.cs:708 builds it via `* 180/PI`; stock converts back via `* PI/180` at :2627). The double error roughly cancelled only near argPe ≈ 0. Fix: the ellipse now shares the markers' frame — center at `focus − c·scale` on screen −x (c = a·e behind the focus along the periapsis line), rotation `0f`; argPe no longer enters the ellipse at all. The target/node marker offsets kept their (correct) *difference* of the two orbits' argPe fields but added it to the **radian** true anomaly — now converted: `tNu + (tgtArgPe − argPe) * DegToRad` (same for node patches). New `DegToRad` constant documents the KSP unit split (degrees: inclination/LAN/argumentOfPeriapsis; radians: trueAnomaly and the anomaly chain, Orbit.cs:712).
- **G2-14** (`Telemetry/OrbitPanel.cs`, RebuildReadout): the inclination line multiplied `Orbit.inclination` — already degrees (Orbit.cs:619, `AngleBetween(...) * 180/PI`; sweep citation confirmed) — by 180/π, double-converting (a 28.6° orbit read 1638°). The `* RadToDeg` is removed and the now-unused `RadToDeg` constant deleted (replaced by `DegToRad`, which the G2-13 marker offsets need).
- **G2-15** (`Telemetry/StageAnalyzer.cs`): the stage Δv rocket equation used `ln((dryMass + usable)/dryMass)` with `dryMass` = remaining **part** dry mass only — upper-stage propellant still aboard during the burn was omitted from both mass terms. New accounting: a `resourceMassBucket[stage]` accumulates `amount × density` for **every** resource aboard (locked tanks included — mass is mass; tonnes via `PartResourceDefinition.density` as before); a `carriedResourceMass[s]` prefix sum gives the resource mass of stages strictly below s (they burn later, so their contents ride stage s's burn). ComputeStage receives `dryMass = remainingDryMass[s] + carriedResourceMass[s]` plus this stage's own `stageResourceMass`, and computes `massAfterBurn = dryMass + max(0, stageResourceMass − usable)` (the stage's unburnable remainder — locked contents, propellant beyond the limiting mix share — stays aboard after the burn) and `massBeforeBurn = massAfterBurn + usable`. Consistent with the existing approximation that parts of already-fired stages are jettisoned; crossfeed still not modeled (spec-sanctioned "approx" labeling unchanged).
- **G3-34** (`Telemetry/StageAnalyzer.cs`, part loop): multi-mode parts (RAPIER class) carry one `ModuleEnginesFX` per mode and both were summed into stage thrust/Isp/mass flow (≈2x error on such vessels). Fix: per part, find the `MultiModeEngine` module and keep only the module whose `engineID` (KSPSOURCE/ModuleEnginesFX.cs:26) matches the **selected** mode — `runningPrimary ? primaryEngineID : secondaryEngineID` (KSPSOURCE/MultiModeEngine.cs:42; SetPrimary/SetSecondary at :394/:508 enable exactly one module). Choice documented in the class remarks: the selected mode is used whether or not the engine is currently running (the selection persists while shutdown), matching what stock's engine display shows. A non-FX plain `ModuleEngines` on a multi-mode part (doesn't occur in stock) is still counted — it carries no mode identity to filter on.
- **G3-35** (`Telemetry/StageAnalyzer.cs`, resource loop): locked tanks (`PartResource.flowState == false`, KSPSOURCE/PartResource.cs:36) were counted as usable propellant and capacity. They are now excluded from both the `available` and `capacity` accumulations (so the propellant-fraction readout no longer counts them either) while still contributing their contents' mass to `resourceMassBucket` (G2-15's accounting — locked propellant is dead mass, not zero mass).
- **G3-40** (`Telemetry/StageAnalyzer.cs`, Tick): the 1 s recompute cadence used scaled `Time.time`, so physics warp (up to 4x time compression) multiplied the recompute rate up to ~4 Hz of full part iteration. Now `Time.unscaledTime` on both the comparison and the stamp; the per-frame cost remains one flag check + one float comparison.

### Inputs
- Triage sweep verdicts (`TRIAGE_SWEEP.md`): G2-13 VALID, G2-14 VALID (KSP source: Orbit.cs:619/2618), G2-15 VALID, G3-34/35 WORK, G3-40 WORK ("review under-graded this").
- KSP Knowledge Library: `NOTES\staging-and-engine-isp-model.md` (units/ISP model — confirmed consistent); `NOTES\orbit-members-2d-radar.md` (**contained the wrong claim "angles are radians" that seeded G2-13/G2-14 — corrected there** with Orbit.cs:619/:708/:712/:2618/:2627 citations, per the library's record-findings rule).
- Dump verification this session: `MultiModeEngine.cs:6,21,42,394,508` (primaryEngineID/secondaryEngineID, mode, runningPrimary, SetPrimary/SetSecondary enable one module); `ModuleEnginesFX.cs:26` (engineID); `PartResource.cs:24-46` (flowState property).

### Outputs
- Changed: `DearImGuiKSPDemo/Telemetry/OrbitPanel.cs`, `DearImGuiKSPDemo/Telemetry/StageAnalyzer.cs`.
- Changed (external knowledge base, not this repo): `TOOLS\KSP Knowledge Library\NOTES\orbit-members-2d-radar.md` — units claim corrected.
- No public or demo-API surface changes: `StageInfo` fields and `Snapshot` shape unchanged; `ComputeStage` is private.

### Constraints
- Demo only; no library files touched; no new dependencies.
- §5.9 native-interop checklist verdicts for the per-frame telemetry paths:
  - **Allocation (OrbitPanel.DrawRadar, per frame):** the fix *removed* work — two `Math.Cos`/`Math.Sin` pairs per frame gone, ellipse now constant-rotation struct math. Zero allocations before, zero after; the marker loops gained one double multiply each (no allocation). Verdict: strictly reduced per-frame cost; no new steady-state allocation.
  - **Allocation (StageAnalyzer.Tick, per frame):** unchanged — dirty-flag + float comparison only. All new bookkeeping (`resourceMassBucket`, `carriedResourceMass`, the multi-mode pre-pass) lives inside the 1 Hz `Recompute`, which may allocate freely per the class's documented discipline. The multi-mode pre-pass allocates nothing (reference casts; `activeEngineId` is an existing string reference, not a new string). `DensityOf` adds one dictionary lookup per resource per recompute — no allocation. Verdict: per-frame path untouched; cadence path within its allocation budget.
  - **Process-global state:** none added (no new statics; `_lastRecomputeTime` semantics unchanged apart from clock source).
  - **Resource-acquisition symmetry:** N/A — no locks, subscriptions, or scopes touched; `Subscribe`/`Dispose` GameEvents pairing unchanged.
- Scope discipline held: only the six items; C13-owned files (`BenchmarkUI.cs`, `TelemetrySampler.cs`, `PlotDemo.cs`, `DemoConsumer.cs`) untouched; `StagePanel.cs` untouched (it consumes the unchanged `Snapshot` shape); no crossfeed/thrust-curve/atmosphere modeling added (still documented as not modeled).

### Verification
- `dotnet build DearImGui-KSP.slnx` from repo root: **Build succeeded, 0 warnings, 0 errors** (Debug).
- No automated test coverage exists for the demo (per repo convention; correctness is code review + the in-game gate). Math double-checked against the KSP dump with unit citations placed in the code comments where the fix hinges on them.
- In-game: gate C (user) — see below.

### Gate C (in-game) check items
- Orbit panel ellipse matches map view: vessel/Ap/Pe markers sit ON the drawn ellipse for a non-zero argument of periapsis (e.g. a polar or inclined elliptical orbit — previously the ellipse was visibly rotated away from the markers); target marker and a placed maneuver-node marker land at plausible positions relative to the vessel marker.
- Inclination readout matches the stock map-view orbit info (e.g. 28.6° for a standard KSC equatorial-east orbit, not a four-figure number).
- Stage Δv sanity against stock/KER: a two-stage vessel now shows *less* first-stage Δv than before (upper-stage propellant counted as carried mass) and totals in KER's ballpark (approx label; crossfeed/atmospheric Isp not modeled).
- Locked tanks: lock a fuel tank in flight → that stage's Δv/fraction drops (locked contents count as mass only); unlock → figures return.
- Multi-mode: a RAPIER vessel shows single-mode thrust/Isp (air-breathing mode: high Isp, IntakeAir excluded; toggling mode in flight changes the readout accordingly), not the summed both-modes figures.
- Warp: under 2x/4x physics warp the stage panel updates at the same ~1 Hz wall-clock cadence (previously ~2–4 Hz), with no frame-rate dip from repeated part iteration.

### Rollback
- `git checkout -- DearImGuiKSPDemo/Telemetry/OrbitPanel.cs DearImGuiKSPDemo/Telemetry/StageAnalyzer.cs`; delete this file. (The knowledge-library note correction is in a separate repo and stands on its own citations.)

---

## C12b addendum (2026-09-07) — Gate C follow-up: stage dV vs stock

**Gate C failure:** in-game, the stage panel disagreed with the stock ΔV sidebar (stock 2865/688/2671/0 = 6224 total; demo 0/3613/11/653/342 = 4619). Root cause, verified against the dump (new knowledge note `NOTES\stock-deltav-simulation.md`): stock does not group propellant by the stage's physical tanks at all — per engine it queries the live crossfeed/priority graph (`Part.GetConnectedResourceTotals(..., simulate: true)`, DeltaVEngineInfo.cs:271/:424) and drains it through simulated `RequestResource` (:912). The C12 "stage's own tanks" model diverged on any vessel where engines and their tanks sit in different inverseStage buckets (exactly the gate vessel: stage 11's engines found zero own-tank fuel → dV 0, while stage 10 inherited an absurd pool → 2176 s burn).

**Fix (commit C12b):** `StageAnalyzer` now pools each propellant over every unlocked tank still aboard (cumulative snapshots per stage), burns in firing order (currentStage → 0), and subtracts each stage's consumption from the pool before lower stages compute — the closest demo-scale approximation of stock's graph, per the scope cap (the full flow-graph simulation was explicitly NOT reimplemented). Mass terms simplified accordingly: wet = remaining dry mass + all aboard resource mass − already-burned mass; dry = wet − usable. Isp moved from vacuum-only to stock's flight "Actual" situation — `atmosphereCurve.Evaluate(current static pressure atm)` (DeltaVEngineInfo.cs:1915; identical to vacuum in vacuum), via `Vessel.staticPressurekPa * PhysicsGlobals.KpaToAtmospheres`; per-engine fuel flow stays vacuum-rated (pressure-independent, ModuleEngines.cs:1048), which also keeps burn time consistent with the same tank set as the dV calc.

**Expected accuracy:** agrees closely with stock on serial staging with default crossfeed (the gate-vessel shape) — stage 11 now draws from the full pool instead of reading 0, lower stages see the depleted pool (stage-4-style "0 m/s, fuel already burned above" now reproduces). Documented residual divergences (class remarks): explicit flow priorities / disabled crossfeed / fuel lines (pooled instead); parallel staging (engines in different stages firing together) burns sequentially here, shifting per-stage attribution while the total stays close; jet velocity/atm-density Isp multipliers and thrust curves not modeled.

**Verification:** `dotnet build DearImGui-KSP.slnx` green (0 errors, 0 warnings). §5.9: per-frame `Tick` path still flag + float compare; all new pooling structures are inside the 1 Hz recompute (may allocate freely); no new statics. Re-gate in-game: same checks as Gate C above, with the sidebar comparison now expected close on serial-staged vessels.
