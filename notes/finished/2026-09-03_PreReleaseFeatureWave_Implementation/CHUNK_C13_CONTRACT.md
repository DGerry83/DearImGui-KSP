# Chunk Contract: ImPlot Managed Wrapper + Demo Proof Window
## Plan: DearImGui-KSP Pre-Release Feature Wave
## Date: 2026-09-04
## Chunk ID: C13
## Advances Milestone: M4 (ImPlot integration — final chunk; M4 gate follows)

### Scope
Bind cimplot, surface a public zero-alloc plot API, and prove it with a two-plot
live-data window in the demo mod. High-risk chunk (span pinning on the hot path) —
the §5.9 native interop & hot-path checklist applies in full. C11 (vendored
implot/cimplot, 606 `ImPlot_*` exports live in the DLL) and C12 (ImPlot context
lifecycle in ContextHost) are complete and committed (`964a68b`, `7a7588c`).
**No native changes this chunk** — every native symbol needed already exists.

1. **Interop** — NEW `DearImGuiKSP/Interop/ImPlotNative.cs`, self-contained
   (externs + internal safe wrappers in one file, the `ExtensionShimsNative.cs`
   pattern from C10; do NOT edit `ImGuiNative.cs`/`ImGuiInternal.cs`):
   - `DllImport("DearImGuiKSPNative", CallingConvention = Cdecl)` externs, each
     commented with its cimplot.h declaration line (same discipline as
     `ImGuiNative.cs`): `ImPlot_BeginPlot` (title, ImVec2 size, ImPlotFlags),
     `ImPlot_EndPlot`, `ImPlot_PlotLine_FloatPtrInt`,
     `ImPlot_PlotLine_doublePtrInt`. Nothing else — the showcase chunk (C19) will
     add more when it needs them.
   - Verify each signature against `DearImGuiKSPNative/vendor/cimplot/cimplot.h`
     (param order, types, the `double xscale, double x0, int flags, int offset,
     int stride` tail) — cite line numbers in the comments.
   - `internal enum ImPlotFlags` subset needed by the wrapper (at minimum `None`;
     include `NoTitle`, `NoLegend`, `NoMenus` if trivially verifiable). Values
     hand-verified against `vendor/implot/implot.h` with line cites (same
     discipline as the existing ImGui flag enums).
   - UTF-8 labels via the existing `ToUtf8` convention — if it is private to
     `ImGuiInternal`, duplicate the 3-line helper locally (C10 precedent); labels
     are short-lived, document the allocation.

2. **Public API** — NEW `DearImGuiKSP/Application/Api/ImGuiPlot.cs`
   (`public static class ImGuiPlot`, full XML docs, spec §4.3):
   - `public static PlotScope Begin(string title, Vector2 size)` — calls
     `ImPlot_BeginPlot`; returns a `public readonly struct PlotScope : IDisposable`
     with a `Visible` bool (the `ImGuiEx` scope pattern from C3 — study
     `Application/Api/ImGuiEx.cs` first). `Dispose` calls `ImPlot_EndPlot` ONLY
     when Begin returned true (ImPlot's pairing rule), so the scope is
     fault-barrier safe (Dispose runs from the `using` even on exception).
     No-op inert scope when `DearImGuiKSP.IsAvailable` is false.
   - `public static void PlotLine(string label, ReadOnlySpan<float> values)` and
     the `ReadOnlySpan<double>` overload — pin with `fixed` and forward the pointer
     + `values.Length`; no copies, no per-frame allocation (xscale=1, x0=0,
     flags=0, offset=0, stride=element size). Guard on availability and on
     `values.IsEmpty` (skip the native call for empty spans).
   - Optional convenience overload `PlotLine(label, values, double xscale, double x0)`
     only if it adds no allocation — otherwise omit (YAGNI).

3. **Demo proof window** — NEW `DearImGuiKSPDemo/PlotDemo.cs`: a window showing
   TWO live line plots through the public API only (the demo must never touch
   internals). Feed both from small fixed-capacity ring buffers updated once per
   frame (e.g. smoothed FPS and per-frame ms, or a rolling sine + random walk —
   either is fine, both series must move visibly). Register/toggle it consistent
   with how `DemoConsumer.cs` wires its existing windows. Zero steady-state
   managed allocation in the per-frame path (ring buffer is preallocated; no
   string building per frame — labels are constants).

### Inputs (must exist before starting)
- C11/C12 verified and committed: cimplot exports in the DLL, ImPlot context
  created at ContextInit (harness proves it).
- `Application/Api/ImGuiEx.cs` (scope-struct pattern), `Interop/ExtensionShimsNative.cs`
  (self-contained native-file pattern), `Interop/ImGuiNative.cs` (extern comment
  discipline), `Interop/ImVec2.cs` (`X`/`Y` capital members).
- Demo structure: `DearImGuiKSPDemo/DemoConsumer.cs`, `BenchmarkUI.cs`.

### Outputs (must be created/changed)
- `DearImGuiKSP/Interop/ImPlotNative.cs` (NEW).
- `DearImGuiKSP/Application/Api/ImGuiPlot.cs` (NEW).
- `DearImGuiKSPDemo/PlotDemo.cs` (NEW) + the minimal `DemoConsumer.cs` wiring.
- Optional: a small xUnit test pinning the `ImPlotFlags` subset values
  (ThemePresetsTests precedent) — only if it fits cleanly; do not force it.
- NO native file changes, NO edits to `ImGuiNative.cs`/`ImGuiInternal.cs`, no
  version bump, no other facade members.

### Constraints
- §5.9: Check 1 — every extern verified against `vendor/cimplot/cimplot.h` with
  line cites. Check 2 — zero steady-state per-frame managed allocation in
  `PlotLine` and the demo's per-frame path (pin via `fixed`; state the allocation
  story for the label UTF-8 conversion explicitly in your report). Check 3 — no
  teardown obligations (pins release at scope exit; nothing native is retained).
- ReadOnlySpan in a public API is fine (consumers are C#); `PlotScope` must be a
  readonly struct, never a class (no boxing, I-02 precedent).
- XML `///` docs on every public member; no emojis/symbol glyphs (D31).
- The demo consumes ONLY the public API.

### Verification
- `dotnet build DearImGui-KSP.slnx` 0/0; `dotnet test` 75/75 (or 76 with the
  optional enum test — report the count).
- All 3 native builds still 0 errors + harness HARNESS PASS (nothing native
  changed — run `build.bat` + harness once to prove the DLL is untouched-current).
- Signature spot-check: state the cimplot.h line numbers you verified against.
- Source-level no-boxing/no-alloc check of the new hot paths (grep-review, same
  as C3).
- In-game two-plot rendering + benchmark no-regression is the **M4 gate**
  (user-assisted) — deferred, not yours.

### Rollback
- Delete the three new files; revert `DemoConsumer.cs`; rebuild managed to confirm
  75/75. Native tree is untouched, so no native rollback needed.
