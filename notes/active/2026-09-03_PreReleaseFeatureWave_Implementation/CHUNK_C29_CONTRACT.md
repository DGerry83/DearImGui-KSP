# Chunk Contract: Throttle Dial — Input-Direction Showcase (UI → Game)
## Plan: DearImGui-KSP Pre-Release Feature Wave
## Date: 2026-09-04
## Chunk ID: C29
## Advances Milestone: Wave polish (pre-M8) — user friction point 4

### Scope
The showcase currently only READS from KSP (telemetry graphs, stages, orbit).
The user wants to see the other direction demonstrated: a UI control driving
the game. Concretely: a **dial in the filling-bar style** (user's explicit
pick — NOT a slider) bound to the vessel's throttle, in the demo's telemetry
window, plus a docs example of the pattern.

Grounded facts (already verified):
- `DearImGuiKSP.Knob(string label, ref float value, float min, float max)` and
  the style overload exist (`Api/DearImGuiKSP.Knob.cs:83,105`);
  `KnobStyle.Wiper` = "filled circle with a wiper arc showing the value"
  (imgui-knobs ImGuiKnobVariant_Wiper = 1<<2) — that is the filling-arc dial.
  `WiperOnly`/`WiperDot` are acceptable alternates if Wiper reads poorly at the
  chosen size; pick one and note the choice.
- Throttle lives at `FlightInputHandler.state.mainThrottle` (float 0..1).
  C18 already added a null-guarded `FlightInputHandler.state` access in the
  telemetry sampler — follow that pattern.
- The telemetry window is flight-gated already ("No active vessel." outside
  flight) — the dial must live inside that existing gating.

1. **Demo: throttle dial on the Graphs tab** (`DearImGuiKSPDemo/Telemetry/`).
   The Graphs tab already plots the throttle channel — put the dial there so
   read and write sit side by side (under the grid / beside the hover readout
   line, wherever fits the existing layout without restructuring it).
   Binding semantics (two-way):
   - Each frame, seed a local `float throttle = FlightInputHandler.state.mainThrottle`.
   - `if (Knob("Throttle##input", ref throttle, 0f, 1f, KnobStyle.Wiper, ...))`
     → user dragged: write `FlightInputHandler.state.mainThrottle = throttle`.
   - This makes keyboard throttle changes (Z/X, etc.) reflect in the dial and
     dial drags take effect in-game. Do NOT pass `ref` to the game field
     directly — the seed-then-write-on-change pattern is the teaching example.
   - Small size (fits beside/under the grid; ~40–60 px). A numeric percent
     readout is optional; if included, format ONLY when the value changed
     (C19/C20 hot-path rule: no per-frame string formatting).
2. **Docs example** — `docs/20-widgets.md`, Knob section: a short "driving the
   game" snippet showing the seed → Knob → write-on-change pattern against
   throttle, with one sentence noting it generalizes (any widget that returns
   changed + a game write). No spec/process references (C26 rule), no emojis.

### Inputs (must exist before starting)
- `DearImGuiKSPDemo/Telemetry/GraphPanel.cs`, `TelemetryAddon.cs`,
  `TelemetrySampler.cs` (throttle channel + flight gating).
- `DearImGuiKSP/Application/Api/DearImGuiKSP.Knob.cs` (signature, styles).

### Outputs (must be created/changed)
- Demo telemetry Graphs tab: throttle dial (files: `GraphPanel.cs` and/or
  `TelemetryAddon.cs` — whichever owns the tab's layout).
- `docs/20-widgets.md` Knob section example.
- NO library/API changes. NO native changes.

### Constraints
- Zero steady-state allocation on the render path (label literals; optional
  numeric readout formatted on-change only).
- Null-guard `FlightInputHandler.state`; dial only when an active vessel
  exists (existing telemetry gating).
- Don't disturb the M6-verified graphs/stages/orbit behavior or layout beyond
  the added dial.

### Verification
- `dotnet build DearImGui-KSP.slnx` 0/0; `dotnet test DearImGui-KSP.slnx` 90/90.
- Demo DLL mirrors into the game install on build.
- In-game (USER's gate, report readiness only): Graphs tab shows the dial;
  dragging it changes the vessel's actual throttle (graph line moves, vessel
  responds); Z/X keys move the dial.

### Rollback
- `git checkout -- DearImGuiKSPDemo docs/20-widgets.md`; rebuild.
