# Chunk Contract: imgui-knobs + imgui-wheels (Vendor, Shims, Wrappers)
## Plan: DearImGui-KSP Pre-Release Feature Wave
## Date: 2026-09-04
## Chunk ID: C15
## Advances Milestone: M5 (widgets + tween)

### Scope
Vendor imgui-knobs (altschuler, MIT) and imgui-wheels (Engineer162, MIT), expose both
through thin C ABI hand shims, bind them, and surface public `Knob`/`Wheel` widgets on
the facade partial. Sequential with C16 (both edit the 3 build scripts +
`Interop/ExtensionShimsNative.cs`): this chunk runs first. C14 (tween) is complete and
committed (`8b823b9`); C10 established the entire vendor/shim/wrapper pattern this
chunk copies — read `CHUNK_C10_CONTRACT.md` and the C10 outputs first.

1. **Vendor knobs**: fetch `https://github.com/altschuler/imgui-knobs` at current
   master HEAD into `DearImGuiKSPNative/vendor/imgui-knobs/` (sources + headers +
   LICENSE — take the repo's actual file list). Record repo, full commit hash, and
   today's date in `vendor/PIN_RECORD.md` (replace the imgui-knobs row's `_pending_`
   values). Verify byte-identity (md5) against the upstream files, as C10/C11 did.

2. **Vendor wheels**: fetch `https://github.com/Engineer162/imgui-wheels` at current
   master HEAD into `DearImGuiKSPNative/vendor/imgui-wheels/` (same drill; LICENSE is
   MIT per the pin record — confirm the actual LICENSE file is present and vendored).
   **This repo is knowingly immature (spec §11; droppable pre-release).** If the
   pinned sources do not compile against imgui 1.92.9, or the API is too broken to
   shim cleanly: STOP on the wheels half, record it as an impediment, and deliver the
   knobs half only. Do NOT patch vendored sources in place.

3. **Shims** — NEW `src/shims/imgui_knobs_shim.h/.cpp` and (if wheels vendor OK)
   `src/shims/imgui_wheels_shim.h/.cpp`, following `src/shims/imgui_toggle_shim.h/.cpp`:
   - `extern "C" __declspec(dllexport)`, bool as int, return 1 on value change.
   - Knobs: expose the float overload of `ImGuiKnobs::Knob`
     (`label, float* value, float v_min, float v_max, float speed, const char* format,
     int variant, float size, int flags, int steps`) — pass-through with sensible
     passable defaults handled managed-side. Add `KnobInt` only if it is a free
     pass-through (no extra shim machinery); otherwise omit (YAGNI, note it).
   - Wheels: inspect the vendored header, mirror its scalar float/int wheel entry
     point(s) the same way. Keep the shim surface minimal — one or two functions.
   - Format strings: pass `const char*` through from managed (UTF-8 via the existing
     convention); NULL format = widget default.
   - No KSP/Unity knowledge; compile against the pinned imgui tree.

4. **Build scripts**: add the vendored TUs + new shim TUs to the explicit `cl` file
   lists in **all three** scripts (`build.bat`, `build_release.bat`,
   `build_harness.bat`) plus needed `/I` include paths. All three must build.

5. **Interop** — extend `Interop/ExtensionShimsNative.cs` (C10's file — this chunk
   owns it): externs + internal safe wrappers for the new shim functions, same
   discipline (Cdecl, bool as I1/int, UTF-8 labels, header line cites in comments).
   Do NOT edit `ImGuiNative.cs`/`ImGuiInternal.cs`.

6. **Public API** — NEW `Application/Api/DearImGuiKSP.Knob.cs` and
   `Application/Api/DearImGuiKSP.Wheel.cs` on the existing `DearImGuiKSP` partial
   (C10's `DearImGuiKSP.Toggle.cs` precedent; the facade is already `partial`):
   - `public static bool Knob(string label, ref float value, float min, float max)`
     plus a full overload exposing speed/variant/size/flags/steps
     (`public enum KnobVariant` / `[Flags] public enum KnobFlags` subsets, values
     hand-verified against the vendored `imgui-knobs.h` with line cites — expose
     only what the shim forwards).
   - `public static bool Wheel(...)` mirroring whatever scalar surface the vendored
     wheels header actually provides (document the mapping in XML docs).
   - No-op/false when `!IsAvailable`, full XML docs, defaults chosen so the simple
     overload looks right at 18 px base font / uiScale 1.0.

### Inputs (must exist before starting)
- C14 committed (`8b823b9`); build 0/0; tests 90/90; native current (all 3 scripts +
  harness PASS as of C13 re-run).
- Pattern files: `src/shims/imgui_toggle_shim.h/.cpp`, `Interop/ExtensionShimsNative.cs`,
  `Application/Api/DearImGuiKSP.Toggle.cs`, `vendor/PIN_RECORD.md` (knobs/wheels rows
  `_pending_`), the 3 build scripts, `CHUNK_C10_CONTRACT.md`.
- Export verification: dumpbin fails under MSYS — use the PowerShell PE-export parser
  pattern from `DearImGuiKSPNative/build/c11_verify.ps1` (write `c15_verify.ps1` the
  same way, checking the new DK_* exports).

### Outputs (must be created/changed)
- `DearImGuiKSPNative/vendor/imgui-knobs/**` (NEW, incl. LICENSE) + PIN_RECORD row filled.
- `DearImGuiKSPNative/vendor/imgui-wheels/**` (NEW, incl. LICENSE) + PIN_RECORD row filled
  — or a documented wheels-dropped impediment.
- `DearImGuiKSPNative/src/shims/imgui_knobs_shim.h/.cpp` (+ `imgui_wheels_shim.h/.cpp`).
- All 3 build scripts — file lists + includes.
- `DearImGuiKSP/Interop/ExtensionShimsNative.cs` — extended.
- `DearImGuiKSP/Application/Api/DearImGuiKSP.Knob.cs` (+ `.Wheel.cs`) (NEW).
- `DearImGuiKSPNative/build/c15_verify.ps1` (NEW, export spot-check).
- NO demo changes (C17 owns the showcase), NO version bump, NO settings changes.

### Constraints
- Vendored sources byte-identical to upstream pins — never patch in place; drift or
  compile failure → STOP + IMPEDIMENTS, no silent fixes.
- §5.9 checklist: Check 1 — every managed extern verified against its shim header
  with line cites; Check 2 — zero per-frame managed allocation beyond the existing
  ToUtf8 label convention (state the story for the format string too); Check 3 — no
  teardown obligations.
- Wheels is the accepted-risk half: knobs must not be blocked by wheels. If wheels
  drops, the chunk still delivers knobs and the PIN_RECORD wheels row says so.
- XML `///` docs on every public member; no emojis (D31). Public enums cited to
  vendored header lines.

### Verification
- All 3 native builds 0 errors; harness HARNESS PASS (re-run `build/harness.exe`).
- `c15_verify.ps1` export check PASS for every new DK_* export.
- `dotnet build DearImGui-KSP.slnx` 0/0; `dotnet test` 90/90 (no new managed logic
  worth xUnit here — wrappers are pass-through; state if you add any).
- Report pinned commit hashes + vendored file lists + md5 spot-checks.
- In-game knob/wheel rendering is the M5 gate (C17, user-assisted) — deferred.

### Rollback
- Revert build scripts + `ExtensionShimsNative.cs`; delete the new vendor trees,
  shim files, facade partials, `c15_verify.ps1`; restore PIN_RECORD rows to
  `_pending_`; re-run `build.bat` to restore the prior DLL; rebuild managed to
  confirm 90/90.
