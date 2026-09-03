# Chunk Contract: Torture-Test Benchmark + Instrumentation

## Plan: DearImGui-KSP Implementation
## Date: 2026-09-03
## Chunk ID: C14
## Advances Milestone: M6

### Scope

- Demo-mod torture test replicating `IMGUI_Helper`'s `PerformanceTab.cs` (spec §4.3, D10): naive vs virtualized 1000-item list, **plus a self-contained OnGUI/IMGUI reference window** so AC5's side-by-side comparison needs no IMGUI_Helper install.
- Benchmark instrumentation (AC6): FPS + per-side declaration-cost readouts in the benchmark UI; library-side managed frame cost logged periodically under verboseLogging.
- Minimal additive public API for consumer-side virtualization: scroll region + scroll offset + cursor positioning (invariant 2 allows additive).

### Inputs (must exist before starting)

- Public API + frame loop (C7), input feeding (C9), demo consumer with toolbar toggle (C8) — all verified in-game.
- Reference implementation: `~\source\repos\IMGUI_Helper\UI\Tabs\PerformanceTab.cs` (read-only reference; naive = 1000 `GUILayout.Label` in a scroll view; virtualized = reserve total height, draw only visible rows at explicit rects; row height 22, view height 200).
- cimgui signatures verified against the pinned clone (`~\source\repos\cimgui`, imgui 1.92.9):
  - `cimgui.h:4112` `bool igBeginChild_Str(const char* str_id, const ImVec2_c size, ImGuiChildFlags child_flags, ImGuiWindowFlags window_flags)`
  - `cimgui.h:4114` `void igEndChild(void)`
  - `cimgui.h:4144` `float igGetScrollY(void)`
  - `cimgui.h:4187` `void igSetCursorPosY(float local_y)`
  - `ImGuiChildFlags` is `typedef int` (imgui.h:249); `ImGuiChildFlags_Borders = 1 << 0` (imgui.h:1270).
- Existing interop patterns: `Interop/ImGuiNative.cs` (private externs + internal wrappers; cimgui `bool` = `UnmanagedType.I1`; null-terminated UTF-8 `byte[]`; `ImVec2` by value) and `Interop/ImGuiInternal.cs` (string encoding wrappers).

### Locked designs

**`Interop/ImGuiNative.cs`** (additive): four new private externs mirroring the exact cimgui signatures above, plus internal wrappers:
```csharp
internal static bool BeginScrollRegion(byte[] idUtf8, float height); // igBeginChild_Str(id, new ImVec2(0, height), ImGuiChildFlags_Borders, 0)
internal static void EndScrollRegion();                              // igEndChild
internal static float GetScrollY();                                  // igGetScrollY
internal static void SetCursorY(float y);                            // igSetCursorPosY
```
Add an `ImGuiChildFlags` enum (`None = 0`, `Borders = 1`) next to the existing flag enums. Audit each binding line-by-line against cimgui.h, same as C6.

**`Interop/ImGuiInternal.cs`** (additive): matching string-handling wrappers (null-terminated UTF-8 for the region id), same patterns as the existing widget wrappers.

**`Application/DearImGuiKSP.cs`** (additive public API — signatures become locked once shipped):
```csharp
public static bool BeginScrollRegion(string id, float height); // fixed-height scrolling child region, bordered; false = clipped (EndScrollRegion still required)
public static void EndScrollRegion();
public static float GetScrollY();        // current region/window vertical scroll offset, px
public static void SetCursorY(float y);  // set vertical cursor position within current region/window
```
Each guarded by `IsAvailable` exactly like the existing methods (GetScrollY returns 0 when unavailable). XML docs must state the manual-virtualization pattern: `GetScrollY` → compute visible row range → `SetCursorY(first*rowHeight)` → draw visible rows → `SetCursorY(count*rowHeight)`.

**`DearImGuiKSPDemo/BenchmarkUI.cs`** (new plain class; demo mod has no layer rules):
- Holds: 1000-entry `List<string>` built once in the ctor (`"Telemetry Entry {i:D4}: {Random.value:F4}"`, same format as PerformanceTab), naive/virtualized toggle, FPS accumulator (EMA of `Time.unscaledDeltaTime`), and two Stopwatch-based 60-frame rolling averages (ImGui declaration cost, IMGUI declaration cost).
- `DrawImGui()` — the ImGui benchmark window content: FPS line, declaration-ms line, a toggle `Button` whose label shows the current mode (MVP has no checkbox — button-toggle is the pattern), then a `BeginScrollRegion("benchmarkList", 200)` containing either 1000 `Text` calls (naive) or the manual-virtualization pattern above with row height 22 (virtualized).
- `OnGUIReference()` — called from the consumer's `OnGUI`: a draggable `GUILayout.Window` titled "IMGUI Reference" with the same data/toggle, porting PerformanceTab's two paths verbatim-in-spirit (`GUILayout.BeginScrollView` + 1000 `GUILayout.Label` naive; `GUILayoutUtility.GetRect` reservation + visible-rows-only `GUI.Label` virtualized). Time only `EventType.Repaint` passes for the IMGUI cost readout; display its own FPS/ms lines inside the window.

**`DearImGuiKSPDemo/DemoConsumer.cs`**: add a "Toggle benchmark window" `Button` to the existing demo window; when visible, declare the benchmark as a second window (`BeginWindow("DearImGui-KSP Benchmark")` → `BenchmarkUI.DrawImGui()`), and call `BenchmarkUI.OnGUIReference()` from a new `OnGUI()` so the IMGUI reference window shows alongside. Remove the `TODO(milestone 6)` comment. One consumer ID, one registration — the second window is just more widgets in the same callback.

### Outputs — exclusive file ownership

- Agent owns: `Interop/ImGuiNative.cs`, `Interop/ImGuiInternal.cs`, `Application/DearImGuiKSP.cs`, `DearImGuiKSPDemo/BenchmarkUI.cs` (new), `DearImGuiKSPDemo/DemoConsumer.cs`.
- **Lead wires after the agent returns** (agent must NOT touch): `FrameLoopOrchestrator.cs` (ctor gains `ILogger`; `Stopwatch` reused across frames around the `RunFrame` body; every 600 frames a `Debug` line: `Managed frame cost avg X.XXX ms over 600 frames` — Debug is already gated by verboseLogging), `Composition.cs` (pass `Logger` to the Orchestrator ctor).

### Constraints

- No native changes — the four bindings target cimgui exports already in the DLL since C3; handshake version stays 3 (invariant 4 unaffected).
- Public API additions only; nothing existing changes signature or behavior (invariant 2).
- Application/Interop stay Unity-free; all Unity/KSP types stay in the demo mod and Infrastructure.
- Per-frame allocations in the benchmark's steady state must be limited to the item strings already allocated once — the toggle/readout lines may format strings (demo code, not library hot path), but no per-row string building: rows use the pre-built list verbatim.
- No git commits — the lead commits after review.

### Verification

- `dotnet build DearImGui-KSP.slnx` — 0 errors, 0 warnings (gate G3). Native untouched — no native rebuild; harness unchanged.
- Binding audit: agent diffs each new extern against cimgui.h line numbers above (C6 precedent).
- **AC5 (user, in-game)**: open the benchmark window; toggle naive/virtualized on the ImGui side and the IMGUI reference side. Expected: IMGUI naive stutters visibly (esp. while scrolling), IMGUI virtualized is smooth; ImGui naive and virtualized both stay visibly smoother than IMGUI naive. Readouts corroborate (declaration ms).
- **AC6 (user, in-game)**: with 1–3 windows open (demo + benchmark + reference), the FPS readout shows no measurable drop vs all windows closed. With `verboseLogging = true` in the instance's `settings.cfg` (re-apply after any build — builds re-mirror defaults), KSP.log shows periodic `Managed frame cost avg` lines under `[DearImGuiKSP]`, each < 1 ms.

### Rollback

- `git checkout -- DearImGuiKSP/Interop/ImGuiNative.cs DearImGuiKSP/Interop/ImGuiInternal.cs DearImGuiKSP/Application/DearImGuiKSP.cs DearImGuiKSP/Application/FrameLoopOrchestrator.cs DearImGuiKSP/Infrastructure/Composition.cs DearImGuiKSPDemo/DemoConsumer.cs` and delete `DearImGuiKSPDemo/BenchmarkUI.cs`.

---

## Addendum C14b (2026-09-03): Dummy binding — boundary-growth assert

First in-game run hard-failed on the imgui debug assert `ErrorCheckUsingSetCursorPosToExtendParentBoundaries` (imgui.cpp:11693): the virtualized pattern's trailing `SetCursorY(rowCount * rowHeight)` extends the child region's boundaries, and ImGui requires an item (not a bare cursor move) to legitimize that growth before `End`/`EndChild`. Fix: new additive binding `igDummy` (cimgui.h:4193) → `ImGuiInternal.Dummy` → public `DearImGuiKSP.Dummy(float width, float height)`; the benchmark's virtualized path now ends `SetCursorY(total) + Dummy(0, 0)`. Verified against imgui.cpp that any submitted item clears `DC.IsSetPos`, so one trailing Dummy covers the pattern. Public API docs for BeginScrollRegion updated to include the Dummy step. Implemented directly by lead; no native change, handshake stays v3.
