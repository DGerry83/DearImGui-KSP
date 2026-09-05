# Chunk Contract: Window Auto-Resize Option (Fit-to-Content)
## Plan: DearImGui-KSP Pre-Release Feature Wave
## Date: 2026-09-04
## Chunk ID: C33
## Advances Milestone: Wave polish (pre-M8) — user pre-release note 2026-09-04

### Scope
User request (paraphrased): windows should optionally snap to the scale of
their contents — expanding when a CollapsingHeader opens, shrinking when it
closes, reflowing when uiScale changes. This is stock ImGui behavior via
`ImGuiWindowFlags_AlwaysAutoResize` — consumer choice, exposed per-window.
Our public API never exposed window flags (every window is `Flags=None`).
Vertical scrollbars on overflow are already default (no window sets
NoScrollbar) — confirm and document, don't implement.

1. **Public API** — extend `ImGuiEx.Window` with an opt-in auto-resize:
   preferred shape `Window(string name, bool autoResize = false)` (optional
   param keeps every existing call site source-compatible; if an overload is
   cleaner per the file's conventions, fine). Map to
   `ImGuiWindowFlags_AlwaysAutoResize` — cite the pinned imgui.h value in the
   Interop enum comment. Route through the existing
   `ImGuiInternal.BeginWindow(name, flags)` path. Full XML docs: fits content
   every frame; adapts to collapsed sections and uiScale; NOT user-resizable
   while enabled (grip/edges inactive); position stays draggable; scrollbars
   don't appear because the window always fits.
2. **Demo + library windows, per the user's split**:
   - Auto-resize ON: the main demo window (`DemoConsumer.cs`), the telemetry
     window (`Telemetry/TelemetryAddon.cs`), and the library control panel
     (`Infrastructure/LibraryControlPanel.cs` — fixed-size content; reflow on
     uiScale change is the point).
   - User-resizable (unchanged): benchmark (`BenchmarkUI.cs`) and plot demo
     (`PlotDemo.cs`) — each gains one small grey text line (the existing
     Text/secondary-text convention; constant string, zero-alloc):
     "Auto-sizing off — drag the corner or edge to resize."
3. **Docs** — `docs/10-api-fundamentals.md` window section (or wherever the
   Window scope is documented): the sizing model in plain terms — default:
   user-resizable with automatic scrollbars on overflow; `autoResize: true`:
   window fits its content every frame (collapse/expand sections, uiScale
   changes) at the cost of manual resizing. No spec refs, no emojis.

### Inputs (must exist before starting)
- `Application/Api/ImGuiEx.cs` (WindowScope), `Interop/ImGuiInternal.cs`
  (BeginWindow), `Interop/ImGuiNative.cs` (ImGuiWindowFlags internal enum).
- Demo windows: `DemoConsumer.cs`, `BenchmarkUI.cs`, `PlotDemo.cs`,
  `Telemetry/TelemetryAddon.cs`; panel: `LibraryControlPanel.cs`.

### Outputs (must be created/changed)
- `ImGuiEx.cs` (+ Interop enum value if not already present), demo/panel
  call sites, docs/10 (and 20-widgets cross-ref only if it discusses window
  sizing).
- Tests: none expected (pass-through); if the change touches logic, follow
  existing test conventions.

### Constraints
- Native untouched (flag is data to igBegin — no new export).
- Zero-alloc steady state (constant strings only).
- No behavior change for existing consumers: default stays user-resizable.
- Verify no demo window relied on being non-resizable (none do — flags were
  always None).

### Verification
- `dotnet build DearImGui-KSP.slnx` 0/0; `dotnet test` 99/99.
- Demo DLL + library DLL mirror into the game install.
- In-game (USER gate, report readiness): main/telemetry/panel windows shrink
  when a CollapsingHeader closes and grow when it opens; uiScale slider
  reflows them; benchmark + plot windows show the note and still resize
  manually; overflowing content in any window gets a scrollbar.

### Rollback
- `git checkout --` touched files; rebuild.
