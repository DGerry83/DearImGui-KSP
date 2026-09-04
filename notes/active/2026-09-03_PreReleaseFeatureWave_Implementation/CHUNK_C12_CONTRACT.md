# Chunk Contract: ImPlot Context Lifecycle
## Plan: DearImGui-KSP Pre-Release Feature Wave
## Date: 2026-09-04
## Chunk ID: C12
## Advances Milestone: M4 (ImPlot integration)

### Scope
Give the native Core an ImPlot context alongside its ImGui context, created and
destroyed in lockstep, with harness proof. Small native-only chunk; no new exports,
no managed changes, no demo changes. C11 (vendored implot/cimplot, green builds) is
complete and committed (964a68b).

1. **ContextHost.cpp** (`DearImGuiKSPNative/src/ContextHost.cpp`):
   - `#include "implot.h"` (include path `/Ivendor\implot` already in all 3 build scripts).
   - Add `static ImPlotContext* s_PlotContext = nullptr;` next to `s_Context`.
   - In `DearImGuiKSPNative_ContextInit` (ContextHost.cpp:32): after
     `ImGui::CreateContext()` succeeds (and before the font step is fine — ImPlot
     context creation just registers with the current ImGui context), call
     `s_PlotContext = ImPlot::CreateContext();`. On the (malloc-only) failure path,
     destroy the ImGui context, null both statics, and return the SAME code as the
     ImGui-create failure (`return 1;`) — do not add a new return code; the managed
     rc contract is frozen.
   - In `DearImGuiKSPNative_ContextShutdown` (ContextHost.cpp:66): call
     `ImPlot::DestroyContext(s_PlotContext)` and null it BEFORE
     `ImGui::DestroyContext(s_Context)` (ImPlot's destructor touches the ImGui
     context — reverse order would be use-after-free).
   - Call the **C++ API directly** (`ImPlot::CreateContext`/`DestroyContext`) — the
     implot TUs are compiled into the same DLL; cimplot is for the managed side (C13).

2. **Harness** (`DearImGuiKSPNative/harness/harness_main.cpp`):
   - Include `implot.h`.
   - After the existing `DearImGuiKSPNative_ContextInit` call: fail (new exit code,
     next free number) if `ImPlot::GetCurrentContext() == nullptr`.
   - After the existing shutdown at the end: fail if
     `ImPlot::GetCurrentContext() != nullptr`.
   - Keep every existing check untouched (gradient read-back, byte-exact disabled
     reference, font, etc.).

### Inputs (must exist before starting)
- C11 verified: implot v1.0 + cimplot vendored, all 3 build scripts include
  `/Ivendor\implot`, harness PASS, committed at 964a68b.
- `src/ContextHost.cpp` current state (gradient pass with white-UV filter — do not
  touch `ApplyWindowBgGradient`).

### Outputs (must be created/changed)
- `DearImGuiKSPNative/src/ContextHost.cpp` — the additions above only.
- `DearImGuiKSPNative/harness/harness_main.cpp` — the two context checks only.
- No other files. No new DLL exports; native version stays 5; no build-script edits
  (C11 already wired the implot TUs and includes).

### Constraints
- §5.9 checklist: Check 1 none; Check 2/3 N/A (no managed/per-frame code — context
  create/destroy happen once at init/shutdown).
- Do not call any ImPlot plotting API — context lifecycle only. C13 owns first use.
- Do not edit the vendored trees, PIN_RECORD.md, or any managed/demo file.

### Verification
- All 3 native builds (`build.bat`, `build_release.bat`, `build_harness.bat`) 0 errors.
- Harness prints HARNESS PASS with the two new checks active (state them in its output
  lines like the existing "Gradient enabled/disabled" lines).
- `dotnet build DearImGui-KSP.slnx` 0/0 and `dotnet test` 75/75 from the repo root
  (proves no collateral damage; managed build mirrors the new DLL into the game).
- Report the exact diff summary of both files.

### Rollback
- `git checkout -- src/ContextHost.cpp harness/harness_main.cpp` (both were clean at
  964a68b), re-run `build.bat` to restore the prior DLL, rebuild managed.
