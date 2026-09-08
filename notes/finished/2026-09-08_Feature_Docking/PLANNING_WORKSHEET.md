# Planning Worksheet: Window Docking (ISSUES #011)
## Date: 2026-09-08
## Type: Feature
## Related Investigation: [INVESTIGATION_LOG.md](INVESTIGATION_LOG.md)

### Step 1 — Core Entities and State
| Entity | Identity | Attributes | Relationships | Lifetime |
|--------|----------|------------|---------------|----------|
| Docking mode (native) | singleton (the one ImGui context) | `io.ConfigFlags & ImGuiConfigFlags_DockingEnable` (bool, native-side) | owned by ContextHost; mirrors `LibrarySettings.Docking` | transient (runtime) |
| `LibrarySettings.Docking` | settings.cfg key `docking` | bool, default `true` (`LibraryConfig.DefaultDocking`) | loaded by SettingsStore; applied live to native | persistent (library's own global config, spec §4.4) |
| Dock layout (consumer) | per-consumer, per-window titles | dock node tree, window→node assignment | built by consumer via DockBuilder API; owned by ImGui context | transient (no ini — reapplied programmatically each session, D7) |
| `ImGuiDockNodeFlags` (value type) | n/a | bitflags mirroring imgui.h ordinals | crosses ABI as raw int | value object |
| `ImGuiDir` (value type) | n/a | enum (Left/Right/Up/Down) mirroring imgui.h ordinals | parameter of DockBuilderSplitNode | value object |

### Step 2 — Behaviors and Responsibilities
| Component | Single-Sentence Responsibility | Command / Query / Both |
|-----------|-------------------------------|------------------------|
| `ContextHost` (native, extended) | Owns the ImGui context config flags including the docking-enable bit. | Both |
| `DearImGuiKSPNative_SetDockingEnabled` (new export) | Sets or clears `ImGuiConfigFlags_DockingEnable` on the live context. | Command |
| `DockingModeApplier` (new, Application) | Applies the persisted docking setting to the native context at frame start when dirty. | Command |
| `SettingsModel` (extended) | Owns the `Docking` value with change-notification and debounced persistence. | Both |
| `SettingsStore` (extended) | Reads and writes the `docking` key in settings.cfg. | Both |
| `LibraryControlPanel` (extended) | Renders the player-facing docking toggle. | Command |
| `DearImGuiKSP` facade docking partial (new, Application/Api) | Exposes dockspace and dock-builder calls as idiomatic static methods. | Both |
| `ImGuiNative`/`ImGuiInternal` (extended, Interop) | Marshals the cimgui docking symbols and the new SetDockingEnabled export. | Both |
| `ContextHost_ClampWindowsToViewport` (modified, native) | Clamps floating windows to the viewport, skipping docked windows. | Command |
| Demo dock layout (DearImGuiKSPDemo) | Declares a dockspace and applies a one-time dock-builder layout for the three demo windows. | Command |

### Step 3 — Data Flow
```
settings.cfg ("docking = true") --(bool)--> SettingsStore.Load --> LibrarySettings --> SettingsModel.Docking
SettingsModel.Changed --(arm dirty)--> DockingModeApplier
FrameLoopOrchestrator.RunFrame (frame start, pre-callback) --> DockingModeApplier.ApplyIfDirty
    --(int code)--> Interop SetDockingEnabled --> DearImGuiKSPNative_SetDockingEnabled --> io.ConfigFlags
LibraryControlPanel toggle --> SettingsModel.Docking (same Changed path as above)
Consumer callback --> facade DockSpace*/DockBuilder* --> Interop --> cimgui igDock* --> dock node tree (context)
Consumer window title --(string, UTF-8)--> DockBuilderDockWindow --> window->DockNode assignment
```

### Step 4 — Boundaries and Interfaces
| Interface | Defined In | Implemented By | Consumed By | Purpose |
|-----------|------------|----------------|-------------|---------|
| cimgui docking ABI (`igDockSpace`, `igDockSpaceOverViewport`, `igDockBuilder*`) | native (pinned cimgui) | cimgui (already compiled in) | Interop | Raw docking calls |
| Library ABI export `DearImGuiKSPNative_SetDockingEnabled` | native Core | ContextHost | Interop (`ImGuiNative` implicit DllImport, SetUiScale precedent) | Live docking toggle |
| `DearImGuiKSP` public docking API (facade partial) | Application | `DearImGuiKSP.Docking.cs` | Consumer mods | Idiomatic docking surface |
| Settings keys/model (`Docking`) | Application (`LibraryConfig`, `LibrarySettings`, `SettingsModel`) | `SettingsStore` (Infrastructure) | Frame loop, control panel | Persisted player setting |

### Layering Check
- [x] Native Core imports nothing from managed; no KSP/Unity knowledge (flag + export only).
- [x] Application imports from Interop only for the new calls; no KSP/Unity APIs (D18/D24 unchanged).
- [x] Infrastructure touches KSP/Unity only where it already does (control panel, settings store).
- [x] Interop stays a one-way leaf (no Application/Infrastructure references from Interop).
