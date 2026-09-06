# Chunk Contract: Public CollapsingHeader (Collapsible Sections)
## Plan: DearImGui-KSP Pre-Release Feature Wave
## Date: 2026-09-04
## Chunk ID: C28
## Advances Milestone: Wave polish (pre-M8) — user friction point 2

### Scope
Users expect to group window content into collapsible sections ("same as
minimizing the whole window, but for groups inside the window"). ImGui's
`CollapsingHeader` is exactly this and is already compiled into our native DLL
via the whole-cimgui build — it was simply never wrapped. This chunk exposes
it, demonstrates it in the demo, and documents it.

Grounded facts (already verified against the pinned sibling cimgui clone):
- `CIMGUI_API bool igCollapsingHeader_TreeNodeFlags(const char* label, ImGuiTreeNodeFlags flags);`
  — cimgui.h:4336 (already exported; no native rebuild needed, confirm with a
  verify script per convention).
- `ImGuiTreeNodeFlags_None = 0` (cimgui.h:362), `ImGuiTreeNodeFlags_DefaultOpen = 1 << 5` (cimgui.h:368).
- CollapsingHeader is NOT a Begin/End pair — it returns bool (open?) and the
  content follows inside an `if`. It does not belong in `ImGuiEx` (scopes);
  it follows the widget pattern: a facade partial like
  `Api/DearImGuiKSP.Radio.cs` / `Api/DearImGuiKSP.Toggle.cs`.

1. **Interop** — extern in `Interop/ImGuiNative.cs` + safe wrapper in
   `Interop/ImGuiInternal.cs`, values cited to cimgui.h lines. Expose only the
   `_TreeNodeFlags` overload; internal flags subset: None + DefaultOpen only
   (YAGNI — no BoolPtr close-button variant, no other TreeNodeFlags).
2. **Public API** — new partial file `Application/Api/DearImGuiKSP.Header.cs`:
   `public static bool CollapsingHeader(string label, bool defaultOpen = false)`.
   Follow the facade label convention (managed UTF-8 buffer; see Radio/Toggle).
   Full XML `///` docs in-chunk (summary, params, returns, remarks: content goes
   inside the `if`; state persists per-window via the label ID; `"##"` rules
   apply per docs/10-api-fundamentals).
3. **Demo usage** — use it in `DearImGuiKSPDemo`: wrap the `ThemeDemo` spinner
   block AND knob/wheel block in CollapsingHeader sections (good showcase,
   zero risk to telemetry panels), OR if a clearly better fit exists in the
   telemetry window, choose that — but do not restructure telemetry layout.
   `defaultOpen: true` for these demo sections so the M5-verified visuals still
   greet the user open.
4. **Docs** — add a CollapsingHeader section to `docs/20-widgets.md`
   (signature, `if`-pattern snippet, note that section state is per-label and
   not persisted across sessions — same as window positions). No spec/process
   references (C26 rule). Update any docs index/TOC line if one lists widgets.

### Inputs (must exist before starting)
- Facade/widget pattern files: `Api/DearImGuiKSP.Radio.cs`,
  `Api/DearImGuiKSP.Toggle.cs`, `Interop/ImGuiNative.cs`,
  `Interop/ImGuiInternal.cs`.
- cimgui.h line cites above (sibling clone `..\..\cimgui`).

### Outputs (must be created/changed)
- `Interop/ImGuiNative.cs`, `Interop/ImGuiInternal.cs` (extern + wrapper).
- `Application/Api/DearImGuiKSP.Header.cs` (NEW).
- `DearImGuiKSPDemo/ThemeDemo.cs` (sections).
- `docs/20-widgets.md` (new section).
- `DearImGuiKSPNative/build/c28_verify.ps1` (export presence check, c15/c16
  pattern).
- Tests: none expected (no logic beyond a pass-through); if you add a flags
  enum to managed code, add the parity test per `ImPlotFlagsTests` precedent.

### Constraints
- Native untouched (symbol already compiled in). If you find it NOT exported,
  STOP — that's an impediment, don't rebuild native to force it.
- Zero-alloc steady state beyond the facade's established label convention.
- D31: no emojis in docs/comments. No spec/D-number references in docs.
- XML docs on the new public member in this chunk.

### Verification
- `dotnet build DearImGui-KSP.slnx` 0/0; `dotnet test DearImGui-KSP.slnx` 90/90
  (or more if you add a parity test).
- `powershell build/c28_verify.ps1` shows the export present.
- Demo DLL mirrors into the game install on build (GameData staging).
- In-game check (USER's gate, report readiness only): ThemeDemo sections
  collapse/expand, default open, state persists while the window lives.

### Rollback
- `git checkout --` the touched files; delete `DearImGuiKSP.Header.cs` and
  `c28_verify.ps1`; rebuild to confirm 90/90.
