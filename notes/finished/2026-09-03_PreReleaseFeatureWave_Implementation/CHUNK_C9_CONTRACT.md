# Chunk Contract: Gradient Helpers + Circular Radio + Window-Bg Gradient
## Plan: DearImGui-KSP Pre-Release Feature Wave
## Date: 2026-09-04
## Chunk ID: C9
## Advances Milestone: M3 (KSP theme) — completes M3's code

### Scope
The theme's visual signature: two-stop vertical gradients (buttons + window background),
the public radio button, and a minimal theme showcase in the demo window so the M3 gate
has something to look at. Runs AFTER C10 (shared build dir/facade — serialized).

1. **Gradient primitives (managed, D28 technique)** — NEW `Application/Api/ImGuiGradients.cs`
   (`public static class ImGuiGradients`, namespace `DearImGuiKSP`):
   - `AddRectFilledGradientVertical(Vector2 min, Vector2 max, Color32 top, Color32 bottom, float rounding)` — AddRectFilled with the mid color, then `igShadeVertsLinearColorGradientKeepAlpha` over exactly the verts just appended (C2 bindings; capture the draw list's vertex count before/after the fill to get the range — check what cimgui exports for `VtxBuffer` size; if no accessor exists, add ONE tiny native export `DearImGuiKSPNative_GetDrawListVtxCount(IntPtr drawList)` in ContextHost + the facade-internal plumbing, and record the addition in the report).
   - `GradientButton(string label, Color32 top, Color32 bottom, Vector2 size)` → bool — gradient rect + interaction + centered label. Preferred: `igButtonBehavior` if the pinned cimgui exports it (research flagged it as likely; verify against cimgui.h). Fallback: `InvisibleButton` + `IsItemHovered`/`IsItemActivated` (all standard exports). Text via `igCalcTextSize` + `ImDrawList_AddText` (add these externs to ImGuiNative/Internal per the C2 pattern — you own those files this chunk).
   - Hover/lighten and active/shift-toward-green per spec §6.1 (hover ~15% lighter; active toward KSP green) — implement as small color-math helpers, tunable constants.

2. **Window-background gradient (native per-frame pass)** — spec §6.1 requires the
   two-stop window bg. cimgui offers no hook for this, so:
   - ContextHost: a gradient descriptor (enabled flag + top/bottom RGBA) set via new export
     `DearImGuiKSPNative_SetWindowBgGradient(int enabled, float r1,g1,b1,a1, r2,g2,b2,a2)`.
   - In `ContextHost_EndFrame` (after `ImGui::EndFrame`, before render): when enabled,
     iterate `g.Windows` (imgui_internal, same as the existing clamp function), skip
     hidden/inactive windows, and apply `ImGradientShadeVertsLinearColorGradientKeepAlpha`
     (or the igShade* equivalent — use the internal `ImGui::ShadeVertsLinearColorGradientKeepAlpha`) to each window's background fill verts. Window bg is the first fill in `window->DrawList`; determine the vert range from the first draw command and guard against command merging (title-bar fill merges with bg since both use the white texture — shading the merged range is ACCEPTABLE: the gradient's top color at title height is visually close to the flat title color, and the visual gate judges).
   - Managed: ThemeEngine sets/clears the descriptor on apply (ksp preset supplies its
     window-bg gradient params; dark preset disables → flat stock bg). Binding via the C2
     pattern in ImGuiNative/ImGuiInternal.
   - If the vert-range approach produces visibly wrong rendering you cannot fix within the
     chunk, STOP and document in IMPEDIMENTS.md — do NOT silently ship a flat fallback.

3. **Public radio buttons** — on the facade via the partial-class pattern C10 established:
   NEW `Application/Api/DearImGuiKSP.Radio.cs` with
   `RadioButton(string label, ref bool value)` and
   `RadioButton(string label, ref int value, int option)` over the C2 bindings
   (stock igRadioButton geometry is already circular; the ksp preset's CheckMark color
   supplies the light-green active fill — verify C8's preset sets it, add if missing).
   No-op/false when unavailable; full XML docs.

4. **Demo showcase (minimal)** — in `DearImGuiKSPDemo/DemoConsumer.cs`, add a small
   "Theme" section to the existing demo window: two `GradientButton`s, one `Toggle`
   (C10's API), two `RadioButton`s. This is what the M3 visual gate inspects. (Knobs/
   wheels/spinners/tweens join it in C17 — keep this small.)

### Inputs (must exist before starting)
- C10 done (shim pattern, partial facade, export-check scripts); C8 done (ThemeEngine + presets with gradient params; `ImGuiCol`/`ImGuiStyleVar` public).
- C2 draw-list bindings incl. `igShadeVertsLinearColorGradientKeepAlpha`, `ImDrawListHandle`.

### Outputs (must be created/changed)
- `DearImGuiKSP/Application/Api/ImGuiGradients.cs` — NEW.
- `DearImGuiKSP/Application/Api/DearImGuiKSP.Radio.cs` — NEW.
- `DearImGuiKSP/Interop/ImGuiNative.cs`, `ImGuiInternal.cs` — new externs/wrappers (you own them this chunk).
- `DearImGuiKSPNative/src/ContextHost.h/.cpp`, `DearImGuiKSPNative.cpp` — window-bg gradient descriptor + EndFrame pass (+ optional vtx-count export).
- `DearImGuiKSP/Application/Theming/ThemeEngine.cs` / `ThemePresets.cs` — wire the gradient descriptor on apply.
- `DearImGuiKSPDemo/DemoConsumer.cs` — minimal Theme section.
- No build-script TU additions expected (ContextHost is already listed); if you add a TU, update all 3 scripts.

### Constraints
- §5.9 checklist: Check 1 none; Check 2 — the EndFrame gradient pass is per-frame native code: O(windows) iteration, no allocation, early-out when disabled; managed gradient helpers run only when a consumer calls them, zero heap (struct math + existing bindings); Check 3 N/A.
- The EndFrame pass must be a no-op when the descriptor is disabled (dark preset = stock rendering, byte-exact).
- Vendored code untouched; no new vendored trees in this chunk.
- XML docs on all new public members; additive public API only.
- imgui_internal use native-side is sanctioned (pinned tree, spec §4.1) — the clamp function already does it.

### Verification
- All 3 native builds 0 errors; harness HARNESS PASS; export-table check for the new export(s).
- `dotnet build` 0/0; `dotnet test` 74/74.
- Direct native call evidence: set gradient descriptor → enabled; disable → pass is no-op.
- Only contract-listed files modified.
- In-game: deferred to the M3 gate (visual pass vs UIStylingRef.png: window bg gradient, gradient buttons incl. hover/active, circular radios with green fill, animated toggles; ksp default; dark regression; live switch).

### Rollback
- Revert listed files; re-run `build.bat` to restore the prior DLL; rebuild managed to confirm 74/74.
