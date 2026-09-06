# Chunk Contract: Graph Panel (2x2 Rolling Plots)
## Plan: DearImGui-KSP Pre-Release Feature Wave
## Date: 2026-09-04
## Chunk ID: C19
## Advances Milestone: M6 (telemetry showcase — first real ImPlot consumer)

### Scope
Replace the Graphs tab placeholder with a 2x2 rolling graph grid fed by C18's four
ring buffers (spec §5.5, §6.2; plan §3 GraphPanel). Isolated as the first real
consumer of C13's plot API (CHUNK_MAP rationale). C13 deliberately bound only
Begin/PlotLine — this chunk extends `Interop/ImPlotNative.cs` with what the grid
needs. §5.9 checklist applies. C18 committed (`0f18770`).

1. **Interop additions** — extend `Interop/ImPlotNative.cs` (self-contained file,
   C13 pattern; do NOT touch ImGuiNative/ImGuiInternal):
   - `ImPlot_BeginSubplots(title, rows, cols, size, flags)` + `ImPlot_EndSubplots()`
     — verify against `vendor/cimplot/cimplot.h`, cite lines. Flags subset:
     `ImPlotSubplotsFlags.None` (+ any trivially verifiable; hand-verify values
     against `vendor/implot/implot.h` with line cites).
   - `ImPlot_IsPlotHovered()` → bool (the no-arg variant).
   - `ImPlot_GetPlotMousePos()` — returns `ImPlotPoint_c` by value (2 doubles);
     mirror as a blittable struct (I-07 discipline; check cimplot.h's actual return
     convention — if it returns via out-pointer, bind that instead and note it).
   - Internal safe wrappers for all of the above (ToUtf8 label convention).
   - Export proof: extend/write `build/c19_verify.ps1` (cimplot symbols are already
     in the DLL — verify presence, no native rebuild expected).

2. **Public API additions** — extend `Application/Api/ImGuiPlot.cs`:
   - `public static SubplotScope BeginSubplots(string title, int rows, int cols, Vector2 size)`
     — `SubplotScope` readonly struct with `Visible`, Dispose calls EndSubplots
     ONLY when Begin returned true (cite the pairing rule from implot.h). Inert
     when unavailable. Same struct discipline as `PlotScope`.
   - `public static bool IsPlotHovered()` and
     `public static Vector2 GetPlotMousePos()` (x = x-value, y = y-value of the
     cursor in plot coordinates; only meaningful inside a begun plot — document).
   - XML docs on all new public members; update the class doc if its scope list
     goes stale.

3. **NEW `DearImGuiKSPDemo/Telemetry/GraphPanel.cs`** — the Graphs tab content:
   - `internal void DrawImGui()` consuming the TelemetrySampler's four ring buffers
     (passed in or referenced per C18's structure — match what C18 exposed).
   - One `BeginSubplots("##telemetry_graphs", 2, 2, ...)` filling the tab; four
     cells, each `ImGuiPlot.Begin(...)` + `PlotLine(label, span)` from the ring
     buffer's oldest-first read: "Altitude (m)", "Dyn pressure (kPa)",
     "Throttle", "G-force". Constant labels (legend comes free from PlotLine
     labels — ImPlot's default legend).
   - Hover tooltip: while `IsPlotHovered()` inside a cell, one readout line with
     the cursor's plot-space values (`GetPlotMousePos`) — string formatting is
     allowed ONLY on the hovered path (user-driven, bounded); steady-state
     unhovered path allocates nothing. Document this in the class doc.
   - No active vessel → the tab's `No active vessel.` placeholder (C18's degrade
     path; the panel is only called with data, or handles empty buffers itself —
     match C18's tab-slot contract).
   - Public API only.

4. **Wiring** — `TelemetryAddon.cs`: Graphs tab placeholder replaced by
   `_graphPanel.DrawImGui()` (stub-list replacement). Class docs updated to stay
   truthful.

### Inputs (must exist before starting)
- C18 committed: `Telemetry/{RingBuffer,TelemetrySampler,TelemetryAddon}.cs` —
  read them for the sampler's ring-buffer exposure and tab-slot structure.
- `Application/Api/ImGuiPlot.cs` (PlotScope pattern + allocation story),
  `Interop/ImPlotNative.cs` (extern discipline, ImPlotFlags precedent),
  `DearImGuiKSPDemo/PlotDemo.cs` (ring-fed plots through the wrapper).
- `vendor/cimplot/cimplot.h`, `vendor/implot/implot.h` for signatures/flags.

### Outputs (must be created/changed)
- `Interop/ImPlotNative.cs`, `Application/Api/ImGuiPlot.cs` (extended).
- `DearImGuiKSPDemo/Telemetry/GraphPanel.cs` (NEW), `TelemetryAddon.cs` (tab wired).
- `DearImGuiKSPNative/build/c19_verify.ps1` (NEW).
- NO native source changes, NO StagePanel/OrbitPanel (C20/C21), no settings changes.

### Constraints
- §5.9: Check 1 — externs line-cited to cimplot.h; struct-by-value returns mirrored
  field-for-field (I-07). Check 2 — zero steady-state per-frame allocation on the
  unhovered path (span reads, constant labels; hover text is the documented
  exception). Check 3 — no teardown obligations.
- Ring buffer reads stay zero-alloc (C18 mechanism — reuse, don't rebuild).
- XML `///` docs on new public members; no emojis (D31).

### Verification
- `dotnet build DearImGui-KSP.slnx` 0/0; `dotnet test` 90/90 (report any adds).
- `c19_verify.ps1` PASS; native `build.bat` + harness re-run to prove the DLL is
  untouched-current.
- Source-level no-alloc review of the panel's per-frame path.
- In-flight 2x2 60 Hz check is the M6 gate (user-assisted) — not yours.

### Rollback
- Revert `ImPlotNative.cs`, `ImGuiPlot.cs`, `TelemetryAddon.cs`; delete
  `GraphPanel.cs` + `c19_verify.ps1`; rebuild managed to confirm 90/90.
