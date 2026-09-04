# Chunk Contract: Stage Analyzer + Stage Panel (+ public draw surface)
## Plan: DearImGui-KSP Pre-Release Feature Wave
## Date: 2026-09-04
## Chunk ID: C20
## Advances Milestone: M6 (telemetry showcase)

### Scope
Two halves: (1) the public drawing surface both remaining panels need (none exists:
`ImGuiGradients.AddRectFilledGradientVertical` takes SCREEN coordinates and the
facade has no position query); (2) the stage analyzer + Stages tab panel (spec
§5.5, §6.2; plan §3 StageAnalyzer/StagePanel). **C20/C21 are SERIALIZED** (decision
2026-09-04, recorded in PROGRESS_LOG): both need Interop edits + the shared
TelemetryAddon wiring, so C20 lands the whole public draw surface (incl. the
ellipse/circle primitives C21 will consume) and C21 follows demo-only.
§5.9 checklist applies (new externs). C19 committed (`5a533c7`).

1. **Interop additions** — extend `Interop/ImGuiNative.cs` + `ImGuiInternal.cs`
   (C2 discipline, cimgui.h line cites; this chunk owns them):
   - `igGetCursorScreenPos(ImVec2* pOut)` — position anchor for custom drawing.
   - `ImDrawList_AddEllipse` / `ImDrawList_AddEllipseFilled` (imgui 1.92.9 has
     them — verify the exact cimgui signatures; if absent, fall back to
     `ImDrawList_AddPolyline` with a point list and NOTE the substitution).
   - Reuse existing internal bindings for AddLine/AddCircle/AddCircleFilled/
     AddRectFilled/AddText where present (they are — C9 landed them); add only
     what's missing.

2. **Public API** — NEW `Application/Api/ImGuiDraw.cs` (`public static class
   ImGuiDraw`, XML docs, spec §5.5 orbit-radar sanction):
   - `Vector2 GetCursorScreenPos()` — current draw position, screen coords
     (documented as the anchor for custom drawing; pairs with `Dummy` to reserve
     canvas area).
   - `AddLine(p1, p2, Color32, float thickness = 1)`, `AddCircle(center, radius,
     Color32, thickness = 1)`, `AddCircleFilled(center, radius, Color32)`,
     `AddEllipse(center, Vector2 radii, Color32, float rotation = 0, thickness = 1)`,
     `AddEllipseFilled(center, Vector2 radii, Color32, float rotation = 0)`,
     `AddRectFilled(min, max, Color32, rounding = 0)`,
     `AddText(Vector2 pos, Color32, string text)` (per-call ToUtf8 allocation —
     same documented convention as facade labels).
   - All no-op when unavailable; all screen-coordinate, current-window draw list.
   - Colors: reuse the existing internal Color32→ImU32 pack helper (find it in
     ImGuiGradients.cs / ImGuiInternal — do not duplicate if shareable in-layer).

3. **NEW `DearImGuiKSPDemo/Telemetry/StageAnalyzer.cs`** — approximate per-stage
   Δv (rocket equation), Isp, burn time, propellant fractions (plan §2
   StageSnapshot; spec §5.5: recompute on staging events / once per second, NOT
   per frame; "approx dV" labeling is mandatory, spec §3.2):
   - Group parts by stage (inverseStage / current stage — verify against the KSP
     Knowledge Library `~\source\repos\TOOLS\KSP Knowledge Library`, NOTES first,
     then the ILSpy dump; record new findings there per AGENTS.md).
   - Per stage: wet/dry mass, thrust-weighted average Isp across that stage's
     engines (ModuleEngines — verify ISP-at-condition members), usable propellant
     limited by the scarcest propellant per engine propellant ratios, burn time
     from mass flow. Approximation is sanctioned — label everything "approx".
   - Produces an immutable-per-recompute snapshot object the panel reads.
   - Recompute triggers: GameEvents staging event + a 1 s timer (cheap fallback);
     no per-frame part iteration.

4. **NEW `DearImGuiKSPDemo/Telemetry/StagePanel.cs`** — Stages tab content,
   public API only:
   - Per stage (current downward): a propellant meter (gradient-filled rect via
     ImGuiGradients/ImGuiDraw, fraction from the snapshot) whose fill color
     TRANSITIONS via C14's `Tween` Color overload (spec §6.3 "showcase meter
     color transitions") — tween from previous to new fraction-driven color on
     recompute (green → orange → red as fraction drops); cached delegates, no
     per-frame allocation.
   - "approx dV" + approx Isp + burn time as inline Text readouts per stage
     (the C19 ruling: no tooltip primitive exists — inline readouts, one-line
     swap later if a tooltip API lands).
   - Stacked summary bar (per-stage dV proportion) if it stays simple with the
     new draw primitives; otherwise omit and note (YAGNI).
   - No active vessel → `No active vessel.` (C18 tab-slot contract).
   - Wire into `TelemetryAddon.cs` Stages tab (replace placeholder; touch ONLY
     that tab's block).

### Inputs (must exist before starting)
- C18/C19 committed: `Telemetry/{RingBuffer,TelemetrySampler,TelemetryAddon,GraphPanel}.cs`
  — tab-slot + placeholder structure, tween usage patterns.
- `Application/Animation/Tween.cs` (Color overload), `Api/ImGuiGradients.cs`
  (Pack helper + gradient rect), `Interop/{ImGuiNative,ImGuiInternal}.cs`
  (existing draw-list bindings to reuse).
- KSP Knowledge Library NOTES + ILSpy dump for: Vessel parts/staging
  (`part.inverseStage`, StageManager/StageController), `ModuleEngines` ISP/thrust/
  propellant members, `Part.Resources` amounts.
- cimgui clone `..\..\cimgui` for the new extern signatures.

### Outputs (must be created/changed)
- `Interop/ImGuiNative.cs`, `Interop/ImGuiInternal.cs` (+ externs/wrappers).
- `Application/Api/ImGuiDraw.cs` (NEW).
- `DearImGuiKSPDemo/Telemetry/StageAnalyzer.cs`, `StagePanel.cs` (NEW).
- `TelemetryAddon.cs` — Stages tab block only.
- `DearImGuiKSPNative/build/c20_verify.ps1` (NEW — new exports present in the
  current DLL; no native rebuild expected, cimgui is compiled in whole).
- NO OrbitPanel (C21), no settings changes.

### Constraints
- §5.9: Check 1 — externs line-cited. Check 2 — zero steady-state per-frame
  allocation in analyzer (recompute-only) and panel frame path (snapshot reads,
  constant labels, cached tween delegates). Check 3 — no teardown obligations.
- Demo consumes ONLY the public API (the new ImGuiDraw surface included).
- "approx dV" (or "approx Δv" — ASCII "dV" per D31 no-symbol-glyphs) label is
  mandatory wherever the Δv figure appears.
- Analyzer degrades gracefully: no engines/propellant in a stage → 0/empty, no
  exceptions (uncrewed probes, stage with only decouplers).
- XML `///` docs on new public members; no emojis/symbol glyphs (D31).

### Verification
- `dotnet build DearImGui-KSP.slnx` 0/0; `dotnet test` 90/90 (report any adds).
- `c20_verify.ps1` PASS; native `build.bat` + harness re-run (untouched-current).
- Source-level no-alloc review of the per-frame panel path + analyzer recompute
  cadence proof (not per frame).
- In-flight stage-panel check is the M6 gate (user-assisted) — not yours.

### Rollback
- Revert Interop files + TelemetryAddon.cs; delete ImGuiDraw.cs, StageAnalyzer.cs,
  StagePanel.cs, c20_verify.ps1; rebuild managed to confirm 90/90. (C21 must not
  have started — it consumes ImGuiDraw.)
