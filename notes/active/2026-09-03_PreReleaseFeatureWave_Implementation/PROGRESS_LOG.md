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
| C9 | Pending | - | - | - | M3 |
| C10 | Done | `vendor/imgui_toggle/` (11 files @ 2c178f5), `src/shims/imgui_toggle_shim.h/.cpp` (NEW), 3 build scripts (+5 TUs), `Interop/ExtensionShimsNative.cs` (NEW), facade +`partial`, `Api/DearImGuiKSP.Toggle.cs` (NEW), PIN_RECORD toggle row | 3 native builds 0 errors; harness PASS; DK_Toggle/DK_ToggleFlags in export table; build 0/0; tests 74/74 | G3: PASS (C10 scope) | Deviation I-05 accepted (no KnobInset upstream — real ToggleFlags subset); vendored byte-identical, LICENSE (0BSD) included |
| C11 | Pending | - | - | - | M4 |
| C12 | Pending | - | - | - | M4 |
| C13 | Pending | - | - | - | M4 |
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

### Blockers
- None

### Milestone Status
| Milestone | Status | Evidence |
|-----------|--------|----------|
| M1 Ergonomics (C1–C3) | **VERIFIED 2026-09-03** | In-game: demo/benchmark windows render identically through scope API; `Throw inside scope (test)` → fault barrier logged `Consumer 'DearImGuiKSPDemo' threw an exception: InvalidOperationException` and UI kept rendering with no disruption (user-confirmed). Build 0/0, tests 59/59. |
| M2 Fonts (C4–C7) | **VERIFIED 2026-09-04** (happy path) | In-game: IBM Plex Sans renders (user-confirmed); base size tuned 15→18 px per user feedback. Gate failure I-03 (io.FontDefault never set) found, fixed, committed 31d7f0d. **Deferred to M3 gate batch (user decision):** fallback-path test (bogus font name) and v4/v5 mismatch popup test — native_v4.dll/native_v5.dll staged in DearImGuiKSPNative/build/ for this. G6 in-game portion stays Pending until then. |
| M3 Theme (C8–C10) | Not started | |
| M4 ImPlot (C11–C13) | Not started | |
| M5 Widgets+tween (C14–C17) | Not started | |
| M6 Showcase (C18–C21) | Not started | |
| M7 Docs (C22–C24) | Not started | |
| M8 Packaging (C25) | Not started | |

### Decisions Made
- 2026-09-03: M2 order is C6 → C4 → C5 (C7 parallel) so no interface stubs are needed; recorded in INTEGRATION_CONTRACT.md.
- 2026-09-04: C9/C10 serialized (was parallel group B). Both would edit the facade, `ImGuiInternal.cs`, and run concurrent native builds in the same `build/` dir — file/tool conflicts, so sequential per the parallel policy. Order: C10 then C9.
- 2026-09-04: Public widget methods (RadioButton, Toggle, later Knob/Wheel/Spinner) go on the `DearImGuiKSP` facade (as a partial class split across files), consistent with the existing Button/SliderFloat pattern. `Application/Api/ImGuiWidgets.cs` from the plan is superseded; `ImGuiGradients.cs` and `ImGuiPlot.cs` remain separate files (helpers and substantial wrapper respectively).
