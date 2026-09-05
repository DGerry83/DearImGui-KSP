# Chunk Contract: Orbit Panel (2D Orbital Radar)
## Plan: DearImGui-KSP Pre-Release Feature Wave
## Date: 2026-09-04
## Chunk ID: C21
## Advances Milestone: M6 (telemetry showcase — final chunk; M6 gate follows)

### Scope
Replace the Orbit tab placeholder with the 2D orbital radar + orbital elements
readout (spec §5.5, §6.2: "2D orbital radar — ImDrawList ellipse, live vessel
marker, Ap/Pe/target markers, existing maneuver nodes display-only — plus orbital
elements readout"). Demo-only chunk: the public `ImGuiDraw` surface landed in C20
(committed `b098aad`) — no Interop or library changes this chunk. The radar redraws
per frame from `vessel.orbit` (spec §5.5) and degrades to `No active vessel.`
outside flight.

1. **NEW `DearImGuiKSPDemo/Telemetry/OrbitPanel.cs`** — the Orbit tab content,
   public API only:
   - **Canvas**: `Dummy(width, height)` to reserve the radar area +
     `ImGuiDraw.GetCursorScreenPos()` anchoring (the established pattern; mind
     cursor-before/after-Dummy ordering — Dummy advances the cursor, so capture
     the anchor correctly and document how).
   - **Orbit ellipse**: from `vessel.orbit` — semi-major axis / eccentricity →
     ellipse radii; argument of periapsis → rotation; body at the focus (filled
     circle marker). Verify the exact `Orbit` member names against the KSP
     Knowledge Library (NOTES first, then ILSpy dump; record new findings there
     per AGENTS.md). Scale: fit the ellipse (apoapsis) to the canvas with margin.
   - **Markers**: live vessel position (from orbit true anomaly at current UT —
     verify the member, e.g. `orbit.trueAnomaly` / `getPositionAtUT` or
     `Orbit.getRelativePositionAtUT` — pick the cheapest per-frame form and say
     why), Ap/Pe markers, target marker when `FlightGlobals.fetch.VesselTarget`
     has an orbit (verify member), maneuver node markers display-only from
     `vessel.patchedConicSolver.maneuverNodes` when present (verify).
   - **Elements readout**: Text lines beside/below the canvas — Ap/Pe altitudes,
     period, inclination, eccentricity, semi-major axis. Preformat strings only
     when values change beyond a small epsilon (or at a 1 Hz cadence — the C20
     render-cache precedent), NOT per frame.
   - Colors from `KspPalette` where sensible; constant labels only.
   - Degrades: no active vessel → `No active vessel.`; no target → target marker
     simply absent; hyperbolic orbits (e > 1) must not produce a broken ellipse —
     clamp/skip with a Text note.
   - Wire into `TelemetryAddon.cs` Orbit tab (replace placeholder; touch ONLY
     that tab's block — C20 already did Stages).

### Inputs (must exist before starting)
- C20 committed: `Application/Api/ImGuiDraw.cs` (the public draw surface — read
  it for exact signatures incl. AddEllipse rotation semantics),
  `Telemetry/{TelemetryAddon,StagePanel}.cs` (tab wiring + render-cache
  precedent), `Application/Animation/Tween.cs` (if used).
- `DearImGuiKSPDemo/Telemetry/GraphPanel.cs` (panel structure precedent).
- KSP Knowledge Library (`~\source\repos\TOOLS\KSP Knowledge Library`) for Orbit
  members: semiMajorAxis, eccentricity, argumentOfPeriapsis, trueAnomaly /
  getRelativePositionAtUT, ApA/PeA, period, inclination, VesselTarget,
  patchedConicSolver.maneuverNodes.

### Outputs (must be created/changed)
- `DearImGuiKSPDemo/Telemetry/OrbitPanel.cs` (NEW).
- `DearImGuiKSPDemo/Telemetry/TelemetryAddon.cs` — Orbit tab block only.
- NO library/Interop/native changes, no settings changes.

### Constraints
- Public API only. Zero steady-state per-frame allocation (per-frame ellipse
  geometry is struct math; strings at change/1 Hz cadence; constant labels).
- C# 7.3-era; 4-space indent, braces on new lines; class-doc convention per the
  other panels. No emojis/symbol glyphs (D31 — ASCII "dV" etc.).
- Per-frame redraw from live orbit data (spec §5.5) — no caching of geometry.

### Verification
- `dotnet build DearImGui-KSP.slnx` 0/0; `dotnet test` 90/90.
- Source-level no-alloc review of the per-frame path.
- **M6 gate (user-assisted, NOT yours)**: in-flight — 2x2 graphs at 60 Hz, stage
  panel, orbit radar live; `No active vessel.` outside flight; `approx dV` label;
  D16 compatibility gates; ISSUES #001/#003 mechanisms inert-when-not-capturing.
  Report readiness and hand off.

### Rollback
- Delete `OrbitPanel.cs`; revert `TelemetryAddon.cs`; rebuild managed to confirm
  90/90.
