# Chunk Contract: imgui_toggle End-to-End (Vendor, Shim, Wrapper)
## Plan: DearImGui-KSP Pre-Release Feature Wave
## Date: 2026-09-04
## Chunk ID: C10
## Advances Milestone: M3 (KSP theme — animated toggles)

### Scope
Vendor imgui_toggle, expose it through a thin C ABI shim, bind it, and surface a public
`Toggle` widget. Sequential with C9 (shared build dir + facade): this chunk runs first.

1. **Vendor**: fetch cmdwtf/imgui_toggle (upstream: `https://github.com/cmdwtf/imgui_toggle`)
   at its current master HEAD commit into `DearImGuiKSPNative/vendor/imgui_toggle/`
   (sources only: `imgui_toggle.cpp`, `imgui_toggle_presets.cpp`, `imgui_toggle_math.cpp`,
   `imgui_toggle_palette.cpp`, `imgui_toggle_renderer.cpp` + headers — take the repo's
   actual file list). Record repo, commit hash, and today's date in
   `vendor/PIN_RECORD.md` (replace the toggle row's `_pending_` values). License 0BSD —
   confirm `LICENSE` file is vendored too.

2. **Shim** — NEW `src/shims/imgui_toggle_shim.h/.cpp` (remove the shims README placeholder):
   `extern "C" __declspec(dllexport)` functions, bool as int:
   - `int DK_Toggle(const char* label, int* value)` — imgui_toggle's default `Toggle(label, bool*)`.
   - `int DK_ToggleFlags(const char* label, int* value, int flags)` — the flags overload; expose only scalar/flag overloads (spec §4.1 / D27).
   Both return 1 when the value changed (matching igButton-style bool semantics). The shim
   owns the bool↔int conversion. No KSP/Unity knowledge; compiles against the pinned tree.

3. **Build scripts**: add `vendor/imgui_toggle/*.cpp` + `src/shims/imgui_toggle_shim.cpp`
   to the explicit file lists in **all three** scripts (`build.bat`, `build_release.bat`,
   `build_harness.bat`) plus the needed `/I` include paths. All three must build.

4. **Interop** — NEW `Interop/ExtensionShimsNative.cs`: self-contained externs + internal
   safe wrappers for the two shim functions (C2 pattern: Cdecl, `[In] byte[]` UTF-8 labels
   via the existing `ToUtf8` convention — if `ToUtf8` is private to ImGuiInternal, move it
   to a shared internal location or duplicate the 3-line helper; do NOT edit
   ImGuiNative.cs/ImGuiInternal.cs — C9 owns them in this milestone).

5. **Public API** — the facade becomes partial: change
   `public static class DearImGuiKSP` to `public static partial class DearImGuiKSP` in
   `Application/DearImGuiKSP.cs` (that one token only), then NEW
   `Application/Api/DearImGuiKSP.Toggle.cs` with
   `public static bool Toggle(string label, ref bool value)` and
   `public static bool Toggle(string label, ref bool value, ToggleFlags flags)` —
   no-op/false when unavailable, full XML docs. `public enum ToggleFlags` in the same file,
   values verified against `imgui_toggle.h` (`ImGuiToggleFlags` subset: None, Animated,
   Bordered, KnobInset — only what the shim forwards).

### Inputs (must exist before starting)
- C8 verified (build 0/0, tests 74/74, native v5 + style exports live).
- C2 binding pattern; `ImGuiNative.cs`/`ImGuiInternal.cs` as-is (do not edit).
- `vendor/PIN_RECORD.md` template (toggle row `_pending_`).

### Outputs (must be created/changed)
- `DearImGuiKSPNative/vendor/imgui_toggle/**` (NEW, incl. LICENSE) + `PIN_RECORD.md` row filled.
- `DearImGuiKSPNative/src/shims/imgui_toggle_shim.h/.cpp` (NEW; removes `src/shims/README.md`).
- `DearImGuiKSPNative/build.bat`, `build_release.bat`, `build_harness.bat` — file lists + includes.
- `DearImGuiKSP/Interop/ExtensionShimsNative.cs` (NEW).
- `DearImGuiKSP/Application/DearImGuiKSP.cs` — the single `partial` token only.
- `DearImGuiKSP/Application/Api/DearImGuiKSP.Toggle.cs` (NEW; incl. public `ToggleFlags`).

### Constraints
- Do NOT edit `ImGuiNative.cs`/`ImGuiInternal.cs` (C9's region this milestone) and do not
  add any other facade members.
- Shim API surface is exactly the two functions — no config-struct or preset overloads (YAGNI;
  presets can come later if a consumer asks).
- §5.9 checklist: Check 1 none; Check 2 zero — the managed wrapper passes a pinned bool/
  byte[] without per-call allocation beyond the existing ToUtf8 convention (labels are
  short-lived; document if you reuse a buffer); Check 3 N/A.
- Vendored sources are byte-identical to upstream at the pinned commit — no local patches.
  If the pinned commit fails to compile against imgui 1.92.9, STOP (IMPEDIMENTS.md) — do
  not patch; report the errors.
- XML docs on the new public members; `ToggleFlags` values cited to imgui_toggle.h.

### Verification
- All 3 native builds 0 errors; harness HARNESS PASS.
- Export-table check: `DK_Toggle`, `DK_ToggleFlags` present in the rebuilt DLL.
- `dotnet build` 0/0; `dotnet test` 74/74.
- Report the pinned commit hash + upstream file list.
- In-game toggle rendering/animation is the M3 gate (user-assisted) — deferred, not yours.

### Rollback
- Revert build scripts + facade token; delete `vendor/imgui_toggle/`, `src/shims/imgui_toggle_shim.*`,
  `ExtensionShimsNative.cs`, `DearImGuiKSP.Toggle.cs`; restore PIN_RECORD row to `_pending_`;
  re-run `build.bat` to restore the prior DLL; rebuild managed to confirm 74/74.
