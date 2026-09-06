# Chunk Contract: Library Control Panel + Live uiScale + Resize-Grip Visibility
## Plan: DearImGui-KSP Pre-Release Feature Wave
## Date: 2026-09-04
## Chunk ID: C31
## Advances Milestone: Wave polish (pre-M8) — user points 4 & 5 (2026-09-04, pre-packaging)

### Scope
Three pre-release items from the user's final friction pass. All grounded facts
below were verified by the orchestrator against current code.

**A. Resize-grip visibility (user point 4).** The ksp preset never sets
`ImGuiCol.ResizeGrip`/`ResizeGripHovered`/`ResizeGripActive`
(`Application/Theming/ThemePresets.cs` — grep-confirmed absent), so windows fall
through to the StyleColorsDark baseline (white at ~20% alpha — nearly invisible
on the theme's bg). Users should see at a glance that windows are
corner-draggable. Add the three colors to the **ksp** preset: grip subtle but
clearly visible at rest, brighter on hover/active. Palette tones exist in
`KspPalette` (TextLightGrey, TextOffWhite, ButtonGradientTop…) — compose with
alpha; do NOT add new named palette entries unless genuinely needed. Check the
dark preset stays stock (it's the reference — leave it). In-game visual sign-off
is the user's gate (same convention as M3 tuning).

**B. Live uiScale (user point 5, half 1).** `uiScale` (0.5–2.0, LibraryConfig)
is plumbed through SettingsModel/SettingsStore but consumed NOWHERE — a dead
setting today. Make it live:
- Native: new export in `ContextHost.cpp/.h` + `DearImGuiKSPNative.cpp`, e.g.
  `DearImGuiKSPNative_SetUiScale(float scale)`: `ImGui::GetStyle().ScaleAllSizes(scale)`
  (cimgui method form exists at cimgui.h:4547 — call the C++ method directly,
  not the export) AND `ImGui::GetIO().FontGlobalScale = scale`. No handshake
  bump: this is a new export consumed by the managed side in lockstep — WAIT:
  handshake versioning is D17 lockstep; adding an export consumed by managed
  code without a bump breaks the mismatch story. Instead: bump the handshake to
  v6 following the exact C4 pattern (GetVersion, managed expected version,
  popup path already handles mismatch — see how v4→v5 was done in C4/C5).
- Managed: `ThemeEngine` — (1) `OnSettingsChanged` currently dirties ONLY on
  theme change (ThemeEngine.cs:125-131): extend to UiScale. (2) `Apply()` ends
  with a fresh style (the C8/I-04 reset at `ContextHost_StyleColorsDark`:
  `GetStyle() = ImGuiStyle()` then StyleColorsDark, ContextHost.cpp:461-471) —
  so calling SetUiScale(_settings.UiScale) at the END of every Apply is
  compounding-safe: ScaleAllSizes always multiplies default sizes, and
  FontGlobalScale is set absolutely. Scale 1.0 must be a true no-op outcome.
- Harness: extend `harness_main.cpp` — SetUiScale(1.5) after a dark reset
  scales a known default var (e.g. WindowPadding 8,8 → 12,12) and sets
  io.FontGlobalScale = 1.5; re-apply at 1.0 restores defaults exactly.
- Startup: the initial theme apply (DearImGuiKSPAddon.Start) then also applies
  the loaded uiScale automatically via the same path. Verify the M2 font load
  (18px base) is unaffected: FontGlobalScale multiplies rendered glyph size on
  top — document in the panel that uiScale scales everything, fontScale
  refines text.

**C. Library control panel (user point 5, half 2).** The library ships its own
small settings window so PLAYERS can adjust config in-game:
- Registration: the library registers its OWN consumer callback (id
  `"DearImGuiKSP"`) through the same ConsumerRegistry/frame-loop path any
  consumer uses — no special-casing. Infrastructure wires it in
  `DearImGuiKSPAddon` (Start/OnDestroy symmetry, fault barrier applies to it
  like any consumer).
- Toolbar: ApplicationLauncher button, all scenes where the launcher exists
  (check how `DearImGuiKSPDemo` does it — DemoConsumer/TelemetryAddon pattern).
  Icon: load `GameData/DearImGuiKSP/Textures/toolbar.png` if present (the user
  is making a PNG; use 38px); if absent, generate a simple placeholder
  Texture2D so the feature works before the art lands. Loading pattern: check
  what the demo does for its icon.
- Window "DearImGui-KSP Settings", contents (all edits persist immediately —
  SettingsModel setters already save on change):
  - Theme: two radios (ksp / dark) — already live via ApplyIfDirty.
  - UI scale: SliderFloat 0.5–2.0 (ImGui sliders have ctrl-click type-in
    natively — keep it) — live via B.
  - Font: two radios (IBM Plex Sans / ProggyClean) + fontScale SliderFloat
    0.5–2.0 — **persisted, applied on restart** (atlas rebuild is startup-only;
    live font rebuild is out of scope — file as an upgrade issue). The panel
    must say so plainly next to the font controls: "applies on next KSP start".
  - Verbose logging: Toggle (live; support value — the C25 verbose run-through
    and future player bug reports need it reachable).
- Panel availability/failure behavior: if the library self-disabled at startup,
  no button (the addon never reaches the running state) — confirm this falls
  out naturally.
- Docs: brief player-facing note — `docs/00-getting-started.md` install section
  gets 2-3 sentences (toolbar button, what the panel changes, font=restart);
  `docs/70-troubleshooting.md` verboseLogging bullet points at the panel toggle.

### Inputs (must exist before starting)
- `Application/Theming/{ThemePresets,KspPalette,ThemeEngine}.cs`,
  `Application/SettingsModel.cs`, `Infrastructure/{DearImGuiKSPAddon,SettingsStore,FontResolver}.cs`,
  `Application/FrameLoopOrchestrator.cs`, `Interop/{ImGuiNative,ImGuiInternal}.cs`.
- C4/C5 handshake v4→v5 pattern (PROGRESS_LOG rows C4/C5; ContextHost
  GetVersion; managed expected-version constant; mismatch popup already
  generic).
- Demo toolbar/consumer patterns: `DearImGuiKSPDemo/DemoConsumer.cs`,
  `Telemetry/TelemetryAddon.cs`.

### Outputs (must be created/changed)
- `ThemePresets.cs` (ksp ResizeGrip colors).
- Native: ContextHost.h/.cpp + DearImGuiKSPNative.cpp (SetUiScale export,
  handshake v6), `harness_main.cpp` (scale checks). All 3 build scripts only if
  a TU is added (not expected).
- Managed: ThemeEngine (dirty-on-uiScale + apply), Interop extern+wrapper,
  `Infrastructure/` panel + toolbar + icon loading, `DearImGuiKSPAddon` wiring,
  managed expected handshake version 5→6.
- Tests: extend/add Application.Tests for the ThemeEngine dirty-on-uiScale
  logic and any settings normalization touched (per existing test conventions).
- Docs edits (00, 70).
- GameData/DearImGuiKSP/Textures/ placeholder note (the real PNG is the user's;
  do NOT commit a binary placeholder — runtime-generated fallback only).
- New ISSUES upgrade entry: live font atlas rebuild (font + fontScale without
  restart), post-release.

### Constraints
- Handshake bump done exactly per the C4/C5 pattern; mismatch popup path must
  keep working (v5-native/v6-managed → popup, self-disable).
- uiScale live must not compound across repeated applies (the reset makes it
  safe — prove it in the harness).
- Zero-alloc steady state; panel is user-driven (open/adjust) so minor
  allocations while the panel is OPEN are acceptable — steady-state closed
  panel must cost nothing (consumer callback returns early when hidden, per
  the demo pattern).
- No public-API additions for consumers in this chunk.
- The M3-approved theme look changes ONLY in the resize grip.
- Docs: no emojis, no spec/D-number refs.
- Failure handling per spec §5.4 must be unaffected.

### Verification
- 3 native builds 0 errors (1 tolerated C4190); harness PASS incl. new scale
  checks; verify script c31 pattern (SetUiScale export + GetVersion()==6).
- `dotnet build` 0/0; `dotnet test` all green (90 + any new).
- GameData mirrors to the game install on build; settings.cfg round-trips.
- In-game (USER gate, report readiness): grip visible/hover-bright; panel opens
  from toolbar in multiple scenes; theme radios live-switch; uiScale slider
  live-scales; font radios + fontScale persist and apply after restart;
  verbose toggle works; v5/v6 mismatch popup still correct (stash DLLs exist:
  build/native_v4.dll / native_v5.dll).

### Rollback
- `git checkout --` touched files; restore GameData copies from git; rebuild.
