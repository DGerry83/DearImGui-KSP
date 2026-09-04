# DearImGuiKSPDemo/Telemetry

Created in milestone M6 of the pre-release feature wave
(`notes/active/2026-09-03_NewProject_PreReleaseFeatureWave/IMPLEMENTATION_PLAN.md`).

The flight telemetry showcase. Consumer of the library's **public API only** — it must
never touch internals (spec §4.1).

Planned contents:

- `TelemetryAddon.cs` — `[KSPAddon(Startup.Flight)]` entry; tab bar (Graphs / Stages / Orbit); ApplicationLauncher toggle.
- `TelemetrySampler.cs` — stock flight-state reads into ring buffers, once per frame.
- `RingBuffer.cs` — fixed-capacity (10k) float history; zero steady-state allocation.
- `StageAnalyzer.cs` — approximate per-stage dV / Isp / burn time (labeled "approx dV", D30).
- `GraphPanel.cs` — 2x2 rolling plots with legends and hover tooltips.
- `StagePanel.cs` — stacked bars, propellant meters, tooltips.
- `OrbitPanel.cs` — ImDrawList orbital radar (ellipse, vessel/Ap/Pe/target/node markers) + elements readout.
