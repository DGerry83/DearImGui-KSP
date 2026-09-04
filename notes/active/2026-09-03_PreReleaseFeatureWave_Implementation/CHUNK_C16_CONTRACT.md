# Chunk Contract: imspinner + cimspinner (Vendor, Regenerate, Wrapper)
## Plan: DearImGui-KSP Pre-Release Feature Wave
## Date: 2026-09-04
## Chunk ID: C16
## Advances Milestone: M5 (widgets + tween — last native slice)

### Scope
Vendor imspinner (dalerank, MIT, header-only), regenerate cimspinner against the
pinned cimgui clone (the C11/I-06 generator drill), bind it, and surface a public
enum-dispatched `Spinner(type, ...)` over ~15 curated types (spec §4.3 — NOT all
~590). Sequential after C15 (committed `c117866`) — this chunk edits the same 3
build scripts and adds a new Interop file; no conflicts expected.

1. **Vendor imspinner**: fetch `https://github.com/dalerank/imspinner` at current
   master HEAD into `DearImGuiKSPNative/vendor/imspinner/` (the header(s) + LICENSE —
   confirm MIT file present). Record repo, full commit hash, date in
   `vendor/PIN_RECORD.md` (imspinner row). md5-verify byte-identity.

2. **Regenerate cimspinner**: the `cimspinner/` subdirectory of the same repo holds
   the generator (mirrors cimgui/cimplot's Lua generator). Clone to scratch OUTSIDE
   the repo (`%TEMP%`), record the commit, and run it against the pinned sibling
   clone `..\..\cimgui` (imgui 1.92.9). **I-06 discipline applies in full**:
   - Canonical **gcc** path — `luajit generator.lua gcc "internal"` (cl
     preprocessing breaks cpp2ffi struct tracking; see IMPEDIMENTS.md I-06 and the
     cimplot PIN_RECORD row for the exact C11 invocation).
   - Vendor the generated `cimspinner.cpp/.h` (+ helper files compilation needs)
     into `DearImGuiKSPNative/vendor/cimspinner/`.
   - Fill the cimspinner PIN_RECORD row with the generator commit + date.
   - If the generator cannot run cleanly: **STOP** — record in IMPEDIMENTS.md; do
     NOT hand-patch generated output (fallback = hand-written ABI subset, spec §11,
     an orchestrator decision).
   - Scratch clones must not remain inside the repo tree.

3. **Build scripts**: add the cimspinner TU + `/Ivendor\imspinner
   /Ivendor\cimspinner` (imspinner.h is included by cimspinner.cpp) to the explicit
   file lists in **all three** scripts. All three build with 0 errors.

4. **Interop** — NEW `Interop/ImSpinnerNative.cs` (ImPlotNative.cs precedent:
   self-contained externs + internal safe wrappers; do NOT touch
   `ImGuiNative.cs`/`ImGuiInternal.cs`; `ExtensionShimsNative.cs` stays shim-only).
   Externs for the ~15 curated spinner functions only, each cited to its
   `vendor/cimspinner/cimspinner.h` declaration line. Watch the ABI: cimgui-style
   generators pass small structs like ImVec4/ImVec2 by value in the C signature —
   mirror them exactly as blittable structs (I-07 discipline) and state each
   function's parameter mapping.

5. **Public API** — NEW `Application/Api/DearImGuiKSP.Spinner.cs` on the facade
   partial (C10/C15 precedent):
   - `public enum SpinnerType` — ~15 curated visually-distinct types. Candidate set
     (verify each name exists in the vendored `imspinner.h`; swap any that don't
     for a near-equivalent and note it): Ang, Ang8, AngTriple, Atom, BounceBall,
     BounceDots, Clock, Dots, FadeBlocks, Heart, IncDots, MoonLine, Pulsar,
     Rainbow, Ring, RotateDots. Final count 14-16.
   - `public static void Spinner(SpinnerType type, float radius, float thickness)`
     — enum-dispatched to the matching native call. Common parameter mapping:
     radius/thickness forwarded; a `Color? tint` optional parameter (null = widget
     default/white); every other bespoke upstream arg gets a sensible constant
     default documented in the XML doc for that enum value or method remark.
   - No-op when `!IsAvailable`. Full XML docs; enum values cited to imspinner.h lines.
   - Spinners animate themselves natively (extension-native motion, spec §6.3) —
     no tween interaction.

### Inputs (must exist before starting)
- C15 committed (`c117866`); all 3 native builds green; harness PASS; tests 90/90.
- I-06 in `IMPEDIMENTS.md` + the cimplot row of `vendor/PIN_RECORD.md` (generator
  invocation precedent); `Interop/ImPlotNative.cs` (generated-ABI binding pattern);
  `Application/Api/DearImGuiKSP.Knob.cs` (widget facade precedent);
  `build/c15_verify.ps1` (export parser — write `c16_verify.ps1` the same way).
- Sibling cimgui clone `..\..\cimgui` pinned imgui 1.92.9.

### Outputs (must be created/changed)
- `DearImGuiKSPNative/vendor/imspinner/` (NEW) + PIN_RECORD row filled.
- `DearImGuiKSPNative/vendor/cimspinner/` (NEW, generated) + PIN_RECORD row filled
  (generator commit).
- All 3 build scripts — file lists + includes.
- `DearImGuiKSP/Interop/ImSpinnerNative.cs` (NEW).
- `DearImGuiKSP/Application/Api/DearImGuiKSP.Spinner.cs` (NEW, incl. `SpinnerType`).
- `DearImGuiKSPNative/build/c16_verify.ps1` (NEW).
- NO demo changes (C17 owns the showcase), NO version bump.

### Constraints
- Vendored sources byte-identical; generated outputs byte-identical to generator
  output — never hand-patch (drift → IMPEDIMENTS.md, not silent fixes).
- ~15 curated types only — do not bind the full imspinner surface.
- §5.9: Check 1 — every extern verified against cimspinner.h with line cites.
  Check 2 — zero per-frame managed allocation (Spinner is a value-type dispatch;
  no strings on the path unless a type genuinely needs a label — if so, state it).
  Check 3 — no teardown obligations.
- XML `///` docs on all public members; no emojis (D31).
- If cimspinner generation drifts or the header-only sources fail against 1.92.9:
  STOP with the error list; do not patch.

### Verification
- All 3 native builds 0 errors; harness HARNESS PASS (re-run `build/harness.exe`).
- `c16_verify.ps1` export spot-check: at least 3 of the curated spinner symbols
  present in the debug DLL + regression-check `DK_Knob`/`ImPlot_BeginPlot`.
- `dotnet build DearImGui-KSP.slnx` 0/0; `dotnet test` 90/90 (report if you add any).
- Report: imspinner + generator commit hashes, vendored file lists, final curated
  `SpinnerType` list with any candidate swaps, warnings introduced by new TUs
  (tolerated only if from vendored/generated code — list them).
- In-game spinner rendering is the M5 gate (C17, user-assisted) — deferred.

### Rollback
- Revert the 3 build scripts; delete `vendor/imspinner/`, `vendor/cimspinner/`,
  `ImSpinnerNative.cs`, `DearImGuiKSP.Spinner.cs`, `c16_verify.ps1`; restore the
  PIN_RECORD rows to `_pending_`; re-run `build.bat` to restore the prior DLL;
  rebuild managed to confirm 90/90.
