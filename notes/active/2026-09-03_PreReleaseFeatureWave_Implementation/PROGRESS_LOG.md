# Progress Log: DearImGui-KSP Pre-Release Feature Wave Implementation
## Date: 2026-09-03

| Chunk | Status | Files Modified | Verification | Gate Verdicts | Notes |
|-------|--------|----------------|--------------|---------------|-------|
| C2 | Done | `Interop/ImGuiNative.cs` (+296), `Interop/ImGuiInternal.cs` (+174) | Build 0 err/0 warn; tests 59/59; 16/16 export-table matches (`C2_export_check.txt`); 16/16 signature checks vs pinned cimgui.h | G3: PASS (C2 scope) | Added ImGuiCol(63)/ImGuiStyleVar(46) enums, ImVec4, ImDrawListHandle; backup at /tmp/C2_backup/ |
| C1 | Done | `Application/Api/ImGuiStyleEnums.cs` (NEW), `Application/DearImGuiKSP.cs`, `Interop/ImGuiNative.cs`, `Interop/ImGuiInternal.cs`, `Api/README.md` (removed) | Build 0 err/0 warn; tests 59/59; enum parity 1:1; facade diff strictly additive; CoreModule via KSPBuildTools (csproj untouched) | G3: PASS (C1 scope) | Deviation I-01 (Color→ImVec4 helpers live in facade, not Interop) accepted |
| C3 | Done | `Application/Api/ImGuiEx.cs` (NEW), `DemoConsumer.cs`, `BenchmarkUI.cs`, IMPEDIMENTS.md (I-02) | Build 0 err/0 warn; tests 59/59; 4/4 scope types readonly struct, 0 class; no-boxing source-verified | G3: PASS (C3 scope) | Deviation I-02 accepted (public readonly structs, internal ctors — CS0050/CS0122); in-game checks deferred to M1 gate |
| C6 | Done | `LibraryConfig.cs`, `LibrarySettings.cs`, `SettingsModel.cs`, `SettingsStore.cs`, `Infrastructure/FontResolver.cs` (NEW), `SettingsModelTests.cs` (+4) | Build 0 err/0 warn; tests 63/63 (59 baseline + 4 font cases); NormalizeTheme/native/bridge untouched (grep-verified) | G3: PASS (C6 scope) | FontResolution: UseEmbeddedDefault/PrimaryPath/SecondaryPath/SizePixels; resolver is pure (no log, no native); Check 3: length probe, no retained handles |
| C4 | Done | `src/ContextHost.h/.cpp`, `src/DearImGuiKSPNative.cpp` (+44/−1) | 3 native builds 0 errors; harness HARNESS PASS; export present; GetVersion()=5 (build + GameData DLLs); return codes 0/1/2/3 runtime-verified via direct P/Invoke | G3: PASS; G6 native half: PASS | Committed 8fdd44b; s_FramesBegun gate added; GameData DLL now v5 vs managed v4 (intentional interim mismatch) |
| C7 | Done | `GameData/DearImGuiKSP/Fonts/` (2 TTF + OFL.txt) | Build 0 errors; all 3 files mirrored to game install (sizes match); TTFs tracked in git per repo convention (source assets, not regenerated) | G3: PASS (asset mirror) | Committed 7d41e92; static TTFs only (variable fonts excluded per D29) |
| C5 | Done | `INativeBridge.cs`, `NativeBridge.cs`, `DearImGuiKSPAddon.cs`, `FrameLoopOrchestratorTests.cs` (+1-line fake stub) | Build 0 err/0 warn; tests 63/63; init order proven (font load at Start step 3, first frame gated on MarkRunning at step 5); §5.9 Check 3 teardown traced | G3: PASS; G6 managed half: PASS (compile-time) | Deviations accepted: test-fake stub, Info log level, stale doc fix; in-game gate deferred |
| C8 | Done | Native: ContextHost.h/.cpp, DearImGuiKSPNative.cpp (4 exports); Managed: Theming/{KspPalette,ThemePresets,ThemeEngine}.cs (NEW), SettingsModel, LibraryConfig, FrameLoopOrchestrator, DearImGuiKSP.cs (CurrentTheme), Composition, DearImGuiKSPAddon, settings.cfg; Tests: ThemePresetsTests (NEW 7), SettingsModelTests (+4) | 3 native builds 0 errors, harness PASS, 4 exports verified w/ direct-call read-back; build 0/0; tests 74/74 (63+11) | G3: PASS (C8 scope) | Committed 7cf14ae; I-04 accepted + fixed (whole-style reset before StyleColorsDark); deviations accepted: Composition.cs + test csproj edits; in-game visual pass deferred to M3 gate |
| C9 | Done | `Api/ImGuiGradients.cs` (NEW), `Api/DearImGuiKSP.Radio.cs` (NEW), `Interop/ImGuiNative.cs` (+9 externs), `Interop/ImGuiInternal.cs` (+10 wrappers), native `ContextHost.h/.cpp` + `DearImGuiKSPNative.cpp` (2 exports: SetWindowBgGradient, GetDrawListVtxCount), `ThemeEngine.cs`, `DemoConsumer.cs` (Theme section), `harness_main.cpp` (gradient read-back checks) | 3 native builds 0 errors; harness PASS (gradient applied + byte-exact disabled reference); 2 exports direct-call verified; build 0/0; tests 74/74 | G3: PASS (C9 scope) | igButtonBehavior unusable (no FramePadding accessor) → sanctioned InvisibleButton fallback; bg rect excludes title bar in 1.92.9 (cleaner than contract assumed); in-game visual pass = M3 gate |
| C10 | Done | `vendor/imgui_toggle/` (11 files @ 2c178f5), `src/shims/imgui_toggle_shim.h/.cpp` (NEW), 3 build scripts (+5 TUs), `Interop/ExtensionShimsNative.cs` (NEW), facade +`partial`, `Api/DearImGuiKSP.Toggle.cs` (NEW), PIN_RECORD toggle row | 3 native builds 0 errors; harness PASS; DK_Toggle/DK_ToggleFlags in export table; build 0/0; tests 74/74 | G3: PASS (C10 scope) | Deviation I-05 accepted (no KnobInset upstream — real ToggleFlags subset); vendored byte-identical, LICENSE (0BSD) included |
| C11 | Done | `vendor/implot/` (6 files @ v1.0 = 524f9fc), `vendor/cimplot/` (generated, generator @ 11f13e6), `PIN_RECORD.md` (2 rows filled), 3 build scripts (+3 TUs, `/Ivendor\implot /Ivendor\cimplot /Ivendor`) | 3 native builds 0 err/0 new warn; harness PASS; 606 `ImPlot_*` exports (3-symbol spot-check PASS via `build/c11_verify.ps1`); build 0/0; tests 75/75 | G3: PASS (C11 scope) | I-06 accepted: gcc-canonical generator run (cl breaks cpp2ffi struct tracking), `implot_demo.cpp` vendored as link-required 6th file, `/Ivendor` include added. Byte-identity md5-verified for all vendored files |
| C12 | Done | `src/ContextHost.cpp` (+23: s_PlotContext, create in ContextInit, destroy-before-ImGui in ContextShutdown), `harness_main.cpp` (+13: context non-null-after-init / null-after-shutdown checks, exit codes 18/19) | 3 native builds 0 errors; harness PASS incl. both new checks; build 0/0; tests 75/75 | G3: PASS (C12 scope) | No new exports, version stays 5; C++ API used internally (cimplot reserved for C13's managed path); failure path reuses rc 1 (managed rc contract frozen) |
| C13 | Done | `Interop/ImPlotNative.cs` (NEW: 4 externs + ImPlotFlags subset + blittable ImPlotSpec mirror), `Application/Api/ImGuiPlot.cs` (NEW: Begin→PlotScope, PlotLine float/double), `DearImGuiKSPDemo/PlotDemo.cs` (NEW: 2 live plots, ring buffers), `DemoConsumer.cs` (wiring), `ImPlotFlagsTests.cs` (NEW) | Build 0/0; tests 76/76; native untouched-current (build + harness PASS re-run); cimplot.h/implot.h line-cited; no-alloc source review clean | G3: PASS (C13 scope) | I-07 accepted: ImPlotSpec_c struct ABI (AUTO sentinels load-bearing) instead of assumed flags/offset/stride tail; DangerousGetPinnableReference pin path (Unity 2019.4 mscorlib predates C# 7.3 span pinning); xscale/x0 overload omitted per YAGNI. M4 gate (in-game) pending user |
| C14 | Pending | - | - | - | M5 |
| C15 | Pending | - | - | - | M5 |
| C16 | Pending | - | - | - | M5 |
| C17 | Pending | - | - | - | M5 |
| C18 | Pending | - | - | - | M6 |
| C19 | Pending | - | - | - | M6 |
| C20 | Pending | - | - | - | M6 |
| C21 | Pending | - | - | - | M6 |
| C22 | Pending | - | - | - | M7 |
| C23 | Pending | - | - | - | M7 |
| C24 | Pending | - | - | - | M7 |
| C25 | Pending | - | - | - | M8 |

| M3-TUNE | Done | `Theming/{KspPalette,ThemePresets}`, `Api/{ImGuiGradients,DearImGuiKSP.TextColored.cs (NEW)}`, `Application/DearImGuiKSP.cs` (InputText), `Interop/{ImGuiNative,ImGuiInternal}` (SameLine + hidden-label input), `DemoConsumer.cs`, `ThemePresetsTests.cs` | Build 0/0; tests 75/75; native untouched (gradient flip is descriptor data) | G3: PASS (patch scope) | User M3-gate tuning applied; spec §6.1 revised 2026-09-04; radio-label highlight deferred (boundary ruling); ISSUES #004 filed (UI flicker) |
| M3-FIX | Done | `src/ContextHost.cpp` (gradient pass: white-UV filter, inline lerp replacing ShadeVerts call), `harness_main.cpp` (checks scoped to solid-fill verts + new glyph-untouched check 16), `Api/DearImGuiKSP.Radio.cs` (light-grey interior rim) | 3 native builds 0 errors; harness PASS (incl. new code-16 glyph check); build 0/0; tests 75/75 | G3: PASS (patch scope) | Root cause: glyph verts share the font-atlas texture with the bg fill and merge into draw cmd 0 — shading them tinted list text into the gradient. Radio rim: 1.5 px TextLightGrey circle at frame-height radius −1, button size unchanged |

### Blockers
- None

### Milestone Status
| Milestone | Status | Evidence |
|-----------|--------|----------|
| M1 Ergonomics (C1–C3) | **VERIFIED 2026-09-03** | In-game: demo/benchmark windows render identically through scope API; `Throw inside scope (test)` → fault barrier logged `Consumer 'DearImGuiKSPDemo' threw an exception: InvalidOperationException` and UI kept rendering with no disruption (user-confirmed). Build 0/0, tests 59/59. |
| M2 Fonts (C4–C7) | **VERIFIED 2026-09-04** | In-game, user-confirmed: Plex Sans renders (size tuned to 18 px base); fallback path (bogus font → log line + ProggyClean) PASS; v4/v5 mismatch popup PASS (G6 in-game evidence complete). I-03 found/fixed along the way. |
| M3 Theme (C8–C10) | **VERIFIED 2026-09-04** (residual spot-check pending) | In-game, user-approved after tuning (M3-TUNE: gradient flip, frame backfill rgb(58,58,63), input text light orange, secondary grey button gradient, brighter active green; header text = consumer choice via TextColored). Two gate defects fixed inline (M3-FIX): list text tinted by gradient pass (glyph verts merged into draw cmd 0 — now filtered by white-pixel UV), radio rim added. Spot-check of those two on next launch; ISSUES #004 (flicker) filed separately. |
| M4 ImPlot (C11–C13) | Not started | |
| M5 Widgets+tween (C14–C17) | Not started | |
| M6 Showcase (C18–C21) | Not started | |
| M7 Docs (C22–C24) | Not started | |
| M8 Packaging (C25) | Not started | |

### Decisions Made
- 2026-09-03: M2 order is C6 → C4 → C5 (C7 parallel) so no interface stubs are needed; recorded in INTEGRATION_CONTRACT.md.
- 2026-09-04: C9/C10 serialized (was parallel group B). Both would edit the facade, `ImGuiInternal.cs`, and run concurrent native builds in the same `build/` dir — file/tool conflicts, so sequential per the parallel policy. Order: C10 then C9.
- 2026-09-04: Public widget methods (RadioButton, Toggle, later Knob/Wheel/Spinner) go on the `DearImGuiKSP` facade (as a partial class split across files), consistent with the existing Button/SliderFloat pattern. `Application/Api/ImGuiWidgets.cs` from the plan is superseded; `ImGuiGradients.cs` and `ImGuiPlot.cs` remain separate files (helpers and substantial wrapper respectively).
