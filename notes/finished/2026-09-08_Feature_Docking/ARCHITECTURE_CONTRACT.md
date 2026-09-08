# Architecture Contract: Window Docking (ISSUES #011)
## Date: 2026-09-08
## Type: Feature
## Related Investigation: [INVESTIGATION_LOG.md](INVESTIGATION_LOG.md)

### Change Specifics
- **Feature Scope**: Enable ImGui window docking (already compiled in via the pinned `v1.92.9-docking-1` build), expose a curated docking API to consumers, gate it behind a persisted player setting (default ON), showcase it in the demo, document it. Branch `feature-Docking`, PR-merged at the end.
- **User decisions (2026-09-08)**: (1) settings-gated, default ON, player toggle retained; (2) DockBuilder subset included in the public API; (3) showcase on the multi-window DemoConsumer (main/plot/benchmark windows), not the single-window telemetry panel.
- **Success criteria**: frozen in [GATES.md](GATES.md).

### Locked Design Decisions (candidates for D38–D40 at closure)
1. **`docking` settings key, default `true`, live-applied** via new native export `DearImGuiKSPNative_SetDockingEnabled` following the SetUiScale precedent (implicit DllImport in `ImGuiNative.cs`, dirty-apply at frame start, never mid-callback). New `DockingModeApplier` in Application owns the dirty/apply cycle (ThemeEngine keeps its style-only scope — SRP).
2. **Handshake v9** in lockstep (D17): native `DearImGuiKSPNative_GetVersion()` returns 9; managed `ExpectedNativeVersion = 9`. Deliberate: 1.1.0's native DLL lacks the new export.
3. **`ImGuiConfigFlags_ViewportsEnable` stays OFF.** Docked windows cannot leave the game window; documented as a limitation (docs/70-troubleshooting.md). An embedded Unity plugin cannot sanely own OS platform windows.
4. **`io.IniFilename` stays `nullptr`** (D7, spec §6.2). No layout persistence; consumers reapply layouts programmatically via the DockBuilder API each session. A save/load layout helper is out of scope unless requested later.
5. **Public API is additive only.** Existing `BeginWindow(name, autoResize)` and `ImGuiEx.Window(...)` signatures untouched (binary compat at 1.x); `NoDocking` arrives as a new overload / new optional-parameter-free overload, plus new enums and new methods.
6. **Clamp skip**: `ContextHost_ClampWindowsToViewport` must skip docked windows (`window->DockIsActive`/`DockNode != NULL`) — docked positions are owned by the dock node.
7. **Release versioning deferred** to release prep (OpenGL-session precedent): no csproj/.version bump in this session; expected next release is 1.2.0 (minor). Handshake constant is code, bumps now.

### Structural Invariants
- Layering preserved: native Core (zero KSP/Unity) ← Interop (one-way leaf) ← Application ← Infrastructure.
- Every new public member carries XML docs; no emojis/symbols in docs (D31).
- New public enums mirror imgui.h ordinals exactly and get pin tests (StyleEnumPinTests pattern).
- All new public facade methods no-op outside a registered frame callback (`CanDeclareUi` gate), never throw.
- Window state (incl. dock layout) is consumer-owned; the library persists only settings.cfg.
- D5/D16 compat: no changes to Deferred/TUFX/Scatterer/Parallax/Cinematic/IMGUI-mod interaction paths beyond the clamp skip.
- Failure handling (spec §5.4): SetDockingEnabled failure is non-fatal (log + return code), never trips the Failed state.
- Minimal Change Principle: no refactors outside the listed files; vendored trees untouched.

### Files to Modify
| File | Change Type | Invariants Applied | Risk Level | Lines Affected (Est.) |
|------|-------------|-------------------|------------|---------------------|
| `DearImGuiKSPNative/src/ContextHost.cpp` | Modify | enable flag at init (:166 area); `ContextHost_SetDockingEnabled`; clamp skip docked (:343-365) | Med | ~40 |
| `DearImGuiKSPNative/src/DearImGuiKSPNative.cpp` | Modify | export + version 9 (:39-42) | Low | ~15 |
| `DearImGuiKSPNative/README.md`, `vendor/PIN_RECORD.md` | Modify | doc drift: `-docking` qualifier | Low | ~4 |
| `DearImGuiKSP/Interop/ImGuiNative.cs` | Modify | raw externs: igDockSpace, igDockSpaceOverViewport, igDockBuilder subset, SetDockingEnabled; internal wrappers | Med | ~120 |
| `DearImGuiKSP/Interop/ImGuiInternal.cs` | Modify | UTF-8 label handling for DockBuilderDockWindow title | Low | ~30 |
| `DearImGuiKSP/Application/Api/DearImGuiKSP.Docking.cs` | Add | facade partial: dockspace + dock-builder subset, `CanDeclareUi` gated | Med | ~150 |
| `DearImGuiKSP/Application/Api/ImGuiDockingEnums.cs` | Add | public `ImGuiDockNodeFlags`, `ImGuiDir` (ordinal-mirrored) | Low | ~60 |
| `DearImGuiKSP/Application/DearImGuiKSP.cs` + `Api/ImGuiEx.cs` | Modify | additive `noDocking` overloads only | Med | ~30 |
| `DearImGuiKSP/LibraryConfig.cs`, `Application/LibrarySettings.cs`, `Application/SettingsModel.cs` | Modify | `docking` default/record/model + snapshot | Low | ~25 |
| `DearImGuiKSP/Application/DockingModeApplier.cs` | Add | dirty/apply at frame start | Low | ~60 |
| `DearImGuiKSP/Application/FrameLoopOrchestrator.cs` | Modify | invoke applier at frame start | Low | ~10 |
| `DearImGuiKSP/Infrastructure/SettingsStore.cs` | Modify | key const + load/save | Low | ~8 |
| `DearImGuiKSP/Infrastructure/LibraryControlPanel.cs` | Modify | docking toggle section | Low | ~20 |
| `DearImGuiKSP/Infrastructure/NativeBridge.cs` | Modify | ExpectedNativeVersion 9 | Low | ~2 |
| `DearImGuiKSPDemo/DemoConsumer.cs` | Modify | dockspace + one-time dock-builder layout for the 3 demo windows | Low | ~60 |
| `tests/Application.Tests/*` | Modify/Add | settings default/persistence tests, enum pins, facade guards, FakeNativeBridge if touched | Low | ~120 |
| `docs/10-api-fundamentals.md`, `20-widgets.md`, `70-troubleshooting.md` (+`30-theming.md` if dock colors mapped) | Modify | settings key row, docking section, limitations | Low | ~80 |
| Optional: ksp theme mapping of `DockingPreview`/`DockingEmptyBg` (Application/Theming) | Modify | dock chrome matches theme | Low | ~10 |

### Migration Strategy
- None breaking. Additive API; new settings key defaults to `true` for old settings.cfg files (no formatVersion bump, existing pattern).

### Sub-Agent Scopes
- **Scope A (native)**: ContextHost flag + export + clamp skip + handshake v9 native side + README/PIN_RECORD wording. Verification: `build.bat` + `build_release.bat` pass (pin check green), harness builds.
- **Scope B (managed)**: Interop bindings, facade + enums, settings end-to-end, DockingModeApplier, frame-loop hook, control panel, handshake v9 managed side, xUnit additions. Verification: `dotnet build` + `dotnet test` green. Runs **parallel** with A (contract pins export/enum names; managed build/test never loads the native DLL).
- **Scope C (demo + docs + theme)**: DemoConsumer layout, docs, optional dock-color theme mapping. **Sequential after B** (needs the final API shape).
- Dependency order: A ∥ B → C. Max 2 concurrent.
