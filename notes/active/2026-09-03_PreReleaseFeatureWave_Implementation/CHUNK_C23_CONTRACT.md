# Chunk Contract: Modder Docs Set B (Plotting, Animation, Migration, Troubleshooting)
## Plan: DearImGui-KSP Pre-Release Feature Wave
## Date: 2026-09-04
## Chunk ID: C23
## Advances Milestone: M7 (documentation)

### Scope
Write docs set B (spec §8.2, D31), replacing the placeholder stubs in `docs/`:
`40-plotting.md`, `50-animation.md`, `60-migration-from-imgui.md`,
`70-troubleshooting.md`. Parallel with C22 (set A) — disjoint files. Audience: a
KSP modder who has never used the library; the M7 gate is a from-zero dry run
against the docs ALONE. Read the actual source for every signature — never invent
API.

1. **`docs/40-plotting.md`** — the ImPlot wrapper: `ImGuiPlot.Begin`/PlotScope
   pairing rules, `PlotLine` float/double span overloads (zero-copy pinning,
   what "the buffer must stay alive and unmodified during the call" means),
   `BeginSubplots`/SubplotScope for grids, hover queries
   (`IsPlotHovered`/`GetPlotMousePos`), the ring-buffer pattern (teach it with a
   small self-contained example; reference the demo's `RingBuffer` as a full
   implementation), and the performance section: per-frame cost scales with
   plotted point count — use a ROLLING WINDOW (the demo's 1200-sample/20 s
   default, ISSUES #007), auto-fit is fine at rolling-window sizes.
   Sources: `Application/Api/ImGuiPlot.cs`, `Interop/ImPlotNative.cs` (public
   surface only), `DearImGuiKSPDemo/PlotDemo.cs`, `Telemetry/GraphPanel.cs`.

2. **`docs/50-animation.md`** — the tween API: `Tween.To` float and Color
   overloads, `TweenHandle` (`Cancel`/`IsPlaying`), the `Ease` set, frame-loop
   ticking (delta-time, suspension pauses tweens), fire-and-forget semantics,
   completion removal, allocation guidance (cache setter delegates in fields —
   allocation on creation only), availability guards (inert handle when
   unavailable). A minimal example animating a slider/knob value.
   Sources: `Application/Animation/{Tween,Ease,TweenEngine}.cs`, demo usage in
   `ThemeDemo.cs`/`StagePanel.cs`.

3. **`docs/60-migration-from-imgui.md`** — Unity IMGUI (OnGUI) → library mapping:
   OnGUI per-frame declaration maps to the registered callback; GUILayout.Label/
   Button/TextField/Slider → Text/Button/InputText/SliderFloat; window rects/
   GUI.Window → ImGuiEx.Window (positions are ImGui-managed; drag by title bar);
   GUIStyle/GUISkin → theme + style push/pop; GUILayout horizontal/vertical
   areas → current layout model (vertical default, Dummy/SetCursorY; NOTE the
   honest gap: no public SameLine yet — see PROGRESS_LOG decisions);
   Event.current/mouse handling → not needed (the library handles input capture
   and click-through protection automatically). Include a side-by-side before/
   after example of one small IMGUI window ported.
   Sources: the facade + `ISSUES/` #001/#003 resolutions (input capture model),
   `DemoConsumer.cs` as a worked example.

4. **`docs/70-troubleshooting.md`** — symptoms → causes → fixes:
   - Startup failure popup / `[DearImGuiKSP]` log lines (self-disable model,
     spec §5.4 failure handling — one popup, log has detail).
   - Version mismatch popup (managed/native lockstep, D17 — reinstall matching pair).
   - Font fallback (missing TTF → ProggyClean + one log line).
   - "My widget calls do nothing" → called outside a registered callback.
   - "Register ignored" → availability/duplicate id warnings.
   - Empty-ID assert (## ids; ISSUES #005 lesson) — debug builds assert, release
     behavior.
   - The 16-bit ImDrawIdx limit (D32, spec §3.2): >65,535 vertices in one draw
     list asserts/fails; remedy (split content, fewer points — rolling windows).
   - Performance: plot cost scales with points (rolling windows); hover text
     allocation note; benchmark window in the demo as the regression instrument.
   - Known limitations: spinner tint quirks (ISSUES #006), flicker under
     investigation (ISSUES #004 — honest note), no OpenGL yet (D33 pending).
   Sources: spec §5.6/§3.2, `Application/{LifecycleStateMachine,FaultBarrier}.cs`,
   ISSUES tracker files.

### Constraints
- **Accuracy over coverage**: every signature/default/behavior must match source.
- D31: no emojis, no symbol glyphs (ASCII in examples).
- Code examples are C# 7.3-era (Unity 2019.4).
- Each file links its neighbors (40 ↔ 50 ↔ 60 ↔ 70 and back to set A).
- Docs-only chunk: NO code changes anywhere.

### Verification
- Self-review every documented signature against source (state the files read).
- Nothing to build — state so.
- Modder-from-zero dry run is the M7 gate (user-assisted) after C24.

### Rollback
- Restore the four placeholder stubs from git.
