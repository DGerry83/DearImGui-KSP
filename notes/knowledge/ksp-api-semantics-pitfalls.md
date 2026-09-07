# KSP API Semantics Pitfalls — Units, Time Sources, Vessel Accounting

**Source:** Opus review triage 2026-09-07 (G2-06, G2-13, G2-14, G2-15, G3-34, G3-35, G3-40). These are the review findings no generic workflow could catch — they are KSP API knowledge. Check here before writing orbital/staging/timing code. Cross-check the KSP Knowledge Library ILSpy dump (`~\source\repos\TOOLS\KSP Knowledge Library\`) when unsure.

## Units — KSP often already gives you degrees

- `Orbit.inclination` is **already in degrees** — do not multiply by 180/π (G2-14; KSP source Orbit.cs:619/2618).
- `argumentOfPeriapsis` is **degrees** — convert to radians before `Math.Cos/Sin` (G2-13 fed degrees straight into trig).
- Rule of thumb: KSP's Orbit class exposes degrees; the .NET Math API takes radians. The conversion must appear exactly once, at the trig call.

## Time sources — scaled vs unscaled

- `Time.deltaTime` / `Time.time` are **scaled**: zero under pause, 4x under max physics warp (G2-06, G3-40).
- UI animation (tweens) and ImGui's `io.DeltaTime` should use **unscaled** time, or UI freezes on pause and runs 4x fast under warp.
- Any cadence gate ("recompute every N seconds") built on scaled time rails to per-frame under warp (G3-40 — the review under-graded this).
- Decide per consumer: gameplay-locked logic may want scaled time; UI and wall-clock cadence want unscaled (`Time.unscaledDeltaTime` / `Planetarium.GetUniversalTime()` for KSP clock semantics).

## Vessel/staging accounting

- **Multi-mode engines:** sum only the *active* mode's thrust/Isp/mass flow — summing all modes overcounts (G3-34).
- **Locked tanks** (`flowState == false`) are not usable propellant — exclude from dV masses (G3-35).
- **Stage dV (rocket equation):** the wet/dry masses of a stage must include all upper-stage propellant as payload mass; omitting it inflates per-stage dV (G2-15). If the demo ships an approximation, label it "approx" in the UI, not just in a comment.

## Demo code is reference code

Consumers copy the demo verbatim — these bugs propagate. A doc-recommended constant (virtualized-list `RowHeight = 22px`) that desyncs under uiScale/fontScale teaches every consumer the same bug (G2-12). Demo math and docs-recommended values get the same scrutiny as library code.
