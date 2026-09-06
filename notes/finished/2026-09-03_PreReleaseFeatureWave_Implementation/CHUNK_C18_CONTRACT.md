# Chunk Contract: Telemetry Foundation (RingBuffer + Sampler + Addon Skeleton)
## Plan: DearImGui-KSP Pre-Release Feature Wave
## Date: 2026-09-04
## Chunk ID: C18
## Advances Milestone: M6 (telemetry showcase — foundation chunk)

### Scope
Build the M6 foundation (spec §5.5, §6.2; plan §2 RingBuffer, §3 TelemetrySampler):
a fixed-capacity ring buffer, a per-frame flight sampler, and the TelemetryAddon
skeleton (own window + tab bar + toolbar button) with three placeholder tabs that
C19/C20/C21 replace. Includes the missing **tab bindings** — no tab API exists yet.
M5 is VERIFIED (`fa39d3b`); the §5.9 native interop & hot-path checklist applies
(new externs + per-frame sampling path).

1. **Tab bindings** (C2 binding-pattern contract; this chunk owns
   `Interop/ImGuiNative.cs` / `Interop/ImGuiInternal.cs` for these additions):
   - Externs (verify against the pinned cimgui clone's `cimgui.h`, cite lines):
     `igBeginTabBar(str_id, flags)`, `igEndTabBar()`, `igBeginTabItem(label,
     p_open, flags)`, `igEndTabItem()`. Internal `ImGuiTabBarFlags`/`ImGuiTabItemFlags`
     subsets only if trivially verifiable — `None` alone is acceptable (YAGNI).
   - Internal safe wrappers in `ImGuiInternal.cs` (UTF-8 label convention).
   - Public scopes in `Application/Api/ImGuiEx.cs` (C3 pattern, readonly structs,
     internal ctors): `ImGuiEx.TabBar(string id) : IDisposable` and
     `ImGuiEx.TabItem(string label) : IDisposable`, each with a `Visible` bool.
     Pairing rules: `EndTabBar` only when Begin returned true; `EndTabItem` only
     when its Begin returned true (cite imgui.h for both rules in comments).
     Inert no-op scope when unavailable. No fault-barrier surprises: Dispose from
     `using` on exception must not call an unpaired End.

2. **NEW `DearImGuiKSPDemo/Telemetry/RingBuffer.cs`** — fixed capacity 10,000
   floats (locked contract): preallocated `float[]`, `Push(float)`, zero-alloc
   read. Study `DearImGuiKSPDemo/PlotDemo.cs` first — it already feeds
   `ImGuiPlot.PlotLine(ReadOnlySpan<float>)` from ring storage; reuse its proven
   approach for the wrapped-read problem (or improve it, but keep zero-alloc and
   document the mechanism). No class-in-hot-loop allocation; Push is an array slot.

3. **NEW `DearImGuiKSPDemo/Telemetry/TelemetrySampler.cs`** — reads stock flight
   state once per frame, FLIGHT SCENE ONLY (locked contract channels):
   `altitude` (`vessel.altitude`), `dynamicPressurekPa` (`vessel.dynamicPressurekPa`),
   `mainThrottle` (`FlightInputHandler.state.mainThrottle`), `geeForce`
   (`vessel.geeForce`) — verify exact member names against the KSP API (KSP
   Knowledge Library `~\source\repos\TOOLS\KSP Knowledge Library` — check its
   `NOTES\` first; FlightGlobals.ActiveVessel null → no sample). Sampling continues
   while the window is closed (history warm on reopen, spec §5.5). Exposes the four
   RingBuffers read-only to the panels.

4. **NEW `DearImGuiKSPDemo/Telemetry/TelemetryAddon.cs`** —
   `[KSPAddon(KSPAddon.Startup.Flight, false)]`, own ApplicationLauncher toolbar
   button (DemoConsumer.cs precedent: placeholder icon, onGUIApplicationLauncherReady),
   own consumer registration id `"DearImGuiKSPDemo.Telemetry"`, one window
   ("DearImGui-KSP Telemetry") through `ImGuiEx.Window`, tab bar via the new scopes,
   three tabs "Graphs" / "Stages" / "Orbit" each rendering a placeholder
   `Text("...")` line (stub list, INTEGRATION_CONTRACT). Panels degrade to
   `Text("No active vessel.")` outside flight/with no active vessel — the sampler
   already gates, so the placeholder covers the no-data case. Remove
   `Telemetry/README.md` (placeholder, stub list).

### Inputs (must exist before starting)
- M5 verified; build 0/0; tests 90/90; harness PASS.
- `Application/Api/ImGuiEx.cs` (scope pattern), `Interop/ImGuiNative.cs` /
  `ImGuiInternal.cs` (extern + wrapper discipline), `Interop/ImVec2.cs`.
- `DearImGuiKSPDemo/{DemoConsumer,PlotDemo}.cs` (addon/toolbar/registration +
  ring-fed plot precedents).
- cimgui clone `..\..\cimgui` for signature verification (cite header lines).
- KSP Knowledge Library for FlightGlobals/vessel/FlightInputHandler member names.

### Outputs (must be created/changed)
- `Interop/ImGuiNative.cs`, `Interop/ImGuiInternal.cs` (+ tab externs/wrappers only).
- `Application/Api/ImGuiEx.cs` (+ `TabBar`/`TabItem` scopes).
- `DearImGuiKSPDemo/Telemetry/RingBuffer.cs`, `TelemetrySampler.cs`,
  `TelemetryAddon.cs` (NEW); `Telemetry/README.md` REMOVED.
- NO native rebuild needed (tab symbols already in the DLL — cimgui is compiled in
  whole; prove with an export check via the build/c*_verify.ps1 parser: write
  `c18_verify.ps1` checking `igBeginTabBar`/`igBeginTabItem` presence).
- NO GraphPanel/StagePanel/OrbitPanel yet (C19–C21), no settings changes.

### Constraints
- §5.9: Check 1 — externs line-cited to cimgui.h. Check 2 — zero steady-state
  per-frame managed allocation in the sampler + addon frame path (labels/titles
  constant; no string building). Check 3 — sampler holds no unmanaged resources.
- Demo consumes ONLY the public API; sampler/panels use KSP APIs freely (demo
  layer, D18 does not apply).
- Sampler must tolerate no-active-vessel and scene transitions without exceptions.
- XML docs on new public members (scopes); demo classes follow the existing
  class-doc convention. No emojis (D31).

### Verification
- `dotnet build DearImGui-KSP.slnx` 0/0; `dotnet test` 90/90 (report any adds).
- `c18_verify.ps1` export check PASS (tab symbols present in the current DLL).
- Native builds: re-run `build.bat` + harness once to prove the DLL is
  untouched-current (nothing native changed).
- Source-level no-alloc review of the sampler + placeholder frame path.
- In-flight placeholder/sampling check rides with the M6 gate (user-assisted) —
  not yours.

### Rollback
- Delete the three Telemetry files; restore `Telemetry/README.md`; revert the two
  Interop files + ImGuiEx.cs; rebuild managed to confirm 90/90.
