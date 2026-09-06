# Chunk Contract: ImPlot Vendor Setup + cimplot Regeneration
## Plan: DearImGui-KSP Pre-Release Feature Wave
## Date: 2026-09-04
## Chunk ID: C11
## Advances Milestone: M4 (ImPlot integration — foundation chunk)

### Scope
Vendor ImPlot at the v1.0 tag and a cimplot regenerated against the pinned cimgui
clone, wire both into all three build scripts, and fill their `PIN_RECORD.md` rows.
This chunk is build-infrastructure only: no context lifecycle (C12), no managed
bindings (C13), no demo changes. Isolated by design (INTEGRATION_CONTRACT row 11) —
generator drift here blocks nothing else.

1. **Vendor ImPlot**: fetch epezent/implot (`https://github.com/epezent/implot`) at
   the **v1.0 tag exactly** (do not track master) into
   `DearImGuiKSPNative/vendor/implot/`. Sources only: `implot.cpp`,
   `implot_items.cpp`, `implot.h`, `implot_internal.h` + `LICENSE` (MIT — confirm the
   license file is vendored). Fill the `implot/` row in `vendor/PIN_RECORD.md`
   (repo, tag/commit hash, today's date; the license and file-list columns are
   pre-written — verify them against what actually lands).

2. **Regenerate cimplot**: clone cimgui/cimplot (`https://github.com/cimgui/cimplot`)
   to a scratch location OUTSIDE the repo (e.g. under `build/` or `%TEMP%`), check
   out a recorded commit, and run its generator against the pinned sibling clone
   `..\..\cimgui` (imgui 1.92.9 — the same tree the build scripts compile). The
   generated output is `cimplot.cpp` / `cimplot.h` (+ any `cimplot_helper` or
   definitions files the generator emits that compilation needs). Vendor the
   generated `.cpp/.h` into `DearImGuiKSPNative/vendor/cimplot/`. Fill the
   `cimplot/` row: generator commit hash + today's date in the pin columns.
   - The exact generator invocation is not pre-established — work it out from the
     cimplot repo's README/generator scripts (it mirrors cimgui's Lua-based
     generator; the pinned cimgui clone's own generator docs are the reference).
   - If the generator cannot run cleanly (missing Lua deps, drift vs the pinned
     cimgui, malformed output): **STOP** — record the failure in
     `notes/active/2026-09-03_PreReleaseFeatureWave_Implementation/IMPEDIMENTS.md`
     and report back. Do NOT hand-patch generated output; the fallback (hand-written
     ABI subset, spec §11) is an orchestrator decision.

3. **Build scripts**: add `vendor\implot\implot.cpp`, `vendor\implot\implot_items.cpp`,
   `vendor\cimplot\cimplot.cpp` and the include paths `/Ivendor\implot
   /Ivendor\cimplot` to the explicit file lists in **all three** scripts
   (`build.bat`, `build_release.bat`, `build_harness.bat`). cimplot.cpp also needs
   the existing cimgui/imgui includes — they are already present. All three builds
   must compile and link with 0 errors.

4. **Export sanity**: the rebuilt debug DLL's export table must contain cimplot
   symbols (spot-check `ImPlot_CreateContext`, `ImPlot_BeginPlot`,
   `ImPlot_PlotLine_FloatPtrInt`). Use the PowerShell PE-export parser (existing
   examples: `DearImGuiKSPNative/build/c*_verify.ps1`) — dumpbin fails under MSYS.

### Inputs (must exist before starting)
- M3 verified (build 0/0, tests 75/75, harness PASS; native DLL v5 in GameData).
- `vendor/PIN_RECORD.md` with `_pending_` implot/cimplot rows.
- `vendor/imgui_toggle/` as the working example of a vendored tree + pin row.
- Sibling cimgui clone at `..\..\cimgui`, pinned imgui 1.92.9 (already the build input).

### Outputs (must be created/changed)
- `DearImGuiKSPNative/vendor/implot/` (NEW, 5 files incl. LICENSE).
- `DearImGuiKSPNative/vendor/cimplot/` (NEW, generated `cimplot.cpp/.h` + any helper files compilation needs).
- `vendor/PIN_RECORD.md` — implot + cimplot rows filled (incl. generator commit).
- `DearImGuiKSPNative/build.bat`, `build_release.bat`, `build_harness.bat` — file lists + `/I` paths.
- NO changes under `DearImGuiKSP/` (managed), `DearImGuiKSPDemo/`, or `src/` — C12/C13 own those.

### Constraints
- Vendored sources byte-identical to upstream at the pinned refs — no local patches
  (same discipline as the imgui_toggle row: md5-verify against the upstream clone).
  If implot v1.0 fails to compile against imgui 1.92.9 (the new ImTextureRef system
  is a known friction point): **STOP** via IMPEDIMENTS.md with the error list — do
  not patch; re-pinning is an orchestrator decision.
- Do not bump the native version (still v5 — no new hand-written exports this chunk)
  and do not touch `ContextHost`/`DearImGuiKSPNative.cpp`.
- §5.9 checklist: Check 1 none (no new P/Invoke); Check 2/3 N/A (no managed or
  per-frame code this chunk).
- Windows-first: fetch via git/curl from Git Bash; forward slashes in Bash commands,
  backslash paths stay inside the .bat files.
- The generator scratch clone must NOT be left inside the repo tree (vendor/ holds
  only the generated outputs; record provenance in PIN_RECORD.md).

### Verification
- All 3 native builds 0 errors (`build.bat`, `build_release.bat`, `build_harness.bat`).
- Harness prints HARNESS PASS (existing checks must not regress; no new checks this chunk).
- Export-table spot-check: the three cimplot symbols above present in the debug DLL.
- `dotnet build DearImGui-KSP.slnx` 0/0 and `dotnet test` 75/75 (nothing managed
  changed — this proves no collateral damage; the managed build also mirrors the new
  native DLL into the game install).
- Report: implot tag/commit hash, cimplot generator commit hash, vendored file
  lists, and any compiler warnings the new TUs introduced (warnings tolerated only
  if they originate in vendored code — list them).
- In-game rendering is NOT part of this chunk — C12/C13 wire that up; M4 gate is
  user-assisted at C13.

### Rollback
- Revert the 3 build scripts and PIN_RECORD rows to `_pending_`; delete
  `vendor/implot/` and `vendor/cimplot/`; re-run `build.bat` to restore the prior
  DLL in `GameData/DearImGuiKSP/PluginData/`; rebuild managed to confirm 75/75.
