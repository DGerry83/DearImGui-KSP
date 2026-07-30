# Implementation Plan: Dear KSP

## 1. Overview

### 1.1 Project Name

Dear KSP (assembly/namespace: `DearKSP`).

### 1.2 Description

Dear KSP is a shared KSP mod library that gives other mods a modern, high-performance UI framework as a drop-in replacement for Unity IMGUI. A native C++ core (Dear ImGui + cimgui, D3D11/OpenGL backends, Unity render-thread injection) does the drawing; a managed C# wrapper owns the KSP lifecycle, a library-driven frame loop, input locking, and the consumer-facing API.

### 1.3 Target Platform & Runtime

- OS: Windows 10+ (x64) for MVP; Linux/Mac deferred.
- Runtime: KSP 1.12.x, Unity 2019.4.18f1, Mono x64, .NET Framework 4.8 target.
- Distribution: release zip with `GameData/DearKSP/` (managed + native DLLs, `settings.cfg`, LICENSE, README, version file); demo mod as a separate zip.

### 1.4 Language & Dependencies

- Primary languages: C# (managed), C++17 (native).
- Core framework/engine: Dear ImGui 1.92.9 via cimgui (cloned at `C:\Users\Matt\source\repos\cimgui`, submodule-pinned); Unity low-level native plugin API.
- External libraries:
  - KSPBuildTools 1.1.1 (NuGet) — game detection, references, staging, AVC version file, deploy.
  - System libs only for native: `d3d11`, `dxgi`, `opengl32`.
- Forbidden/restricted: no vcpkg/CMake/vcxproj (native builds via `cl.exe` batch scripts, per CinematicRecorder convention); no third-party managed UI bindings; no IMGUI or uGUI use for library-rendered UI (stock `PopupDialog` is allowed solely for the failure notice); no D3D9.

### 1.5 Scope

- **In scope**: native renderer + core widgets (windows, buttons, text, sliders, input fields); input locking while capturing; library-owned frame loop; consumer registration API; settings ConfigNode; failure detection with popup + `IsAvailable`; demo mod (separate install) with torture-test benchmark.
- **Out of scope**: migrating existing mods, editor tooling, controller/touch input, Linux/Mac builds.
- **MVP**: spec §3.3 — renderer validated on D3D11+OpenGL, core widgets, input locking, one example window, stock dark theme + ProggyClean, settings file, failure UX.
- **Deferred**: spec §3.4 — animations, ImGui add-ons, themes/settings window, consumer font loading, position persistence, z-order upgrades, CKAN, ports.

---

## 2. State Model (Step 1 Output)

### Entity: LibrarySettings
- Identity: singleton per install (one ConfigNode file).
- Lifetime: persistent.
- Attributes:
  - formatVersion: int — config schema version for forward migration
  - uiScale: float — global UI scale, default 1.0, range 0.5–2.0
  - fontScale: float — font scale, default 1.0, range 0.5–2.0
  - theme: string — default "dark" (only valid value in MVP)
  - verboseLogging: bool — gates debug-level log lines
  - enabled: bool — global kill switch
- Relationships:
  - read/written by → SettingsStore
  - consumed by → SettingsModel, FontAtlasBuilder (via scale change)

### Entity: ConsumerRegistration
- Identity: consumer-supplied string ID (unique per registration).
- Lifetime: transient (per session).
- Attributes:
  - id: string — unique registration key
  - callback: delegate — per-frame UI declaration
  - enabled: bool — false after auto-disable
  - consecutiveFailureCount: int — reset on any clean frame
  - registrationOrder: int — z-order within the MVP model
- Relationships:
  - contained-by → ConsumerRegistry

### Entity: RenderContext
- Identity: one per native DLL load.
- Lifetime: transient (per session); rebuilt on resolution change.
- Attributes:
  - imGuiContext: native pointer — the single ImGui context
  - deviceHandle: native pointer — D3D11 device/context or GL state
  - fontAtlasTexture: native texture — built from embedded ProggyClean
  - viewportWidth/viewportHeight: int — current framebuffer size
  - backendKind: enum — D3D11 or OpenGL
- Relationships:
  - owned by → ImGuiContextHost

### Entity: InputCaptureState
- Identity: recomputed every frame (no stable identity).
- Lifetime: transient (per frame).
- Attributes:
  - mouseCaptured: bool — pointer over a Dear KSP window
  - keyboardCaptured: bool — a text field is active
  - activeLockIds: list of strings — locks currently held, per consumer
- Relationships:
  - produced by → InputCaptureTracker; consumed by → InputLockGateway

### Entity: FailureInfo
- Identity: one per failure occurrence.
- Lifetime: transient (logged, popup shown once).
- Attributes:
  - kind: FailureKind — category of failure
  - technicalDetail: string — log-only diagnostics
- Relationships:
  - produced by → initialization path; consumed by → LifecycleStateMachine, FailureNotifier

### Value Object / Enum: LibraryState
- `Uninitialized`, `Initializing`, `Running`, `Suspended`, `Failed`. Transitions per spec §5.2.

### Value Object / Enum: FailureKind
- `MissingNative`, `UnsupportedGraphicsApi`, `VersionMismatch`, `RenderHookFailure`. Maps 1:1 to the §7.1 popup body variants.

### Value Object: Theme
- Name string; MVP valid set `{ "dark" }`. Extensible for the deferred theme work.

---

## 3. Component Responsibilities (Step 2 Output)

### Component: ImGuiContextHost
- Responsibility: Owns the lifecycle of the single native ImGui context.
- Layer: Core (native)
- Type: System
- Collaborators: FontAtlasBuilder, RenderBackendD3D11, RenderBackendOpenGL
- State access: write — RenderContext

### Component: RenderBackendD3D11
- Responsibility: Translates ImGui draw data into D3D11 GPU commands.
- Layer: Core (native)
- Type: System
- Collaborators: ImGuiContextHost
- State access: read — RenderContext

### Component: RenderBackendOpenGL
- Responsibility: Translates ImGui draw data into OpenGL GPU commands.
- Layer: Core (native)
- Type: System
- Collaborators: ImGuiContextHost
- State access: read — RenderContext

### Component: RenderEventEntry
- Responsibility: Exposes the Unity low-level plugin export surface (plugin load/unload, render event).
- Layer: Core (native)
- Type: Controller
- Collaborators: ImGuiContextHost
- State access: read — RenderContext

### Component: FontAtlasBuilder
- Responsibility: Builds the font atlas texture from embedded ProggyClean at init and on scale change.
- Layer: Core (native)
- Type: Service
- Collaborators: ImGuiContextHost
- State access: write — RenderContext.fontAtlasTexture

### Component: DearKSP (public facade)
- Responsibility: Exposes availability, consumer registration, and style access to consumer mods.
- Layer: Application (public API)
- Type: Facade
- Collaborators: LifecycleStateMachine, ConsumerRegistry
- State access: read — LibraryState; write — ConsumerRegistry

### Component: ConsumerRegistry
- Responsibility: Tracks registered consumers in registration order with per-consumer fault state.
- Layer: Application
- Type: Service
- Collaborators: FaultBarrier
- State access: write — ConsumerRegistration

### Component: FrameLoopOrchestrator
- Responsibility: Executes the per-frame sequence from input sampling through native render handoff.
- Layer: Application
- Type: System
- Collaborators: InputCaptureTracker, ConsumerRegistry, FaultBarrier, IInputLockGateway, INativeBridge, IGameEventSource
- State access: read — ConsumerRegistration, LibrarySettings; write — InputCaptureState

### Component: InputCaptureTracker
- Responsibility: Computes mouse/keyboard capture state from ImGui IO each frame.
- Layer: Application
- Type: Service
- Collaborators: INativeBridge
- State access: write — InputCaptureState

### Component: LifecycleStateMachine
- Responsibility: Enforces legal state transitions and per-state behavior gates.
- Layer: Application
- Type: System
- Collaborators: IGameEventSource, IFailureNotifier, ILogger
- State access: write — LibraryState; read — FailureInfo

### Component: FaultBarrier
- Responsibility: Catches consumer exceptions, counts consecutive failures, and disables consumers at the threshold.
- Layer: Application
- Type: Service
- Collaborators: ConsumerRegistry, ILogger
- State access: write — ConsumerRegistration.enabled/consecutiveFailureCount

### Component: SettingsModel
- Responsibility: Holds the in-memory settings snapshot and raises change notifications.
- Layer: Application
- Type: Service
- Collaborators: ISettingsStore
- State access: write — LibrarySettings

### Component: DearKSPAddon
- Responsibility: Is the KSP entry point and composition root that wires all components at startup.
- Layer: Infrastructure
- Type: Controller
- Collaborators: Composition
- State access: none directly

### Component: Composition
- Responsibility: Instantiates concrete Infrastructure classes and injects them into Application components.
- Layer: Infrastructure
- Type: DI root
- Collaborators: all Infrastructure implementations, all Application components
- State access: none

### Component: NativeBridge
- Responsibility: Loads the native DLL explicitly and isolates all P/Invoke declarations.
- Layer: Infrastructure
- Type: Service
- Collaborators: DearKSPLogger
- State access: read — RenderContext handles (opaque)

### Component: InputLockGateway
- Responsibility: Applies and releases KSP input locks under per-consumer lock IDs.
- Layer: Infrastructure
- Type: Service
- Collaborators: KSP `InputLockManager`
- State access: read — InputCaptureState

### Component: GameEventHooks
- Responsibility: Translates game events (F2 UI hide, loading screens, resolution change) into library signals.
- Layer: Infrastructure
- Type: Service
- Collaborators: KSP `GameEvents`, `UIMasterController`
- State access: none (raises signals)

### Component: SettingsStore
- Responsibility: Loads and saves LibrarySettings as a KSP ConfigNode file.
- Layer: Infrastructure
- Type: Repository
- Collaborators: KSP `ConfigNode`
- State access: read/write — settings.cfg

### Component: FailureNotifier
- Responsibility: Shows the single plain-language startup-failure PopupDialog at the main menu.
- Layer: Infrastructure
- Type: Service
- Collaborators: KSP `PopupDialog`
- State access: read — FailureInfo

### Component: DearKSPLogger
- Responsibility: Writes `[DearKSP]`-prefixed lines to KSP.log with verbosity control.
- Layer: Infrastructure
- Type: Service
- Collaborators: `UnityEngine.Debug`
- State access: read — LibrarySettings.verboseLogging

---

## 4. Data Flow Diagram (Step 3 Output)

Startup:

```
[settings.cfg] --(LibrarySettings)--> SettingsStore --(LibrarySettings)--> SettingsModel --(scale/theme)--> FontAtlasBuilder
[KSPAddon MainMenu] --(start)--> DearKSPAddon --(wiring)--> Composition --(components)--> LifecycleStateMachine --(Running)--> FrameLoopOrchestrator
[DearKSPNative.dll] --(LoadLibrary)--> NativeBridge --(handles)--> ImGuiContextHost --(RenderContext)--> RenderBackend
[init failure] --(FailureInfo)--> LifecycleStateMachine --(Failed)--> FailureNotifier --(popup)--> [player]
```

Per frame (Running):

```
[Unity input] --(input state)--> FrameLoopOrchestrator --(ImGui IO)--> InputCaptureTracker --(InputCaptureState)--> InputLockGateway --(locks)--> [InputLockManager]
[consumers] --(UI declarations)--> ConsumerRegistry --(ordered callbacks)--> FaultBarrier --(guarded calls)--> FrameLoopOrchestrator
FrameLoopOrchestrator --(NewFrame/Render)--> ImGuiContextHost --(draw data)--> RenderBackend{D3D11|OpenGL} --(GPU)--> [screen]
[GameEvents: F2 / loading / resolution] --(signals)--> GameEventHooks --(signals)--> LifecycleStateMachine --(Suspend/Resume/rebuild)--> FrameLoopOrchestrator, ImGuiContextHost
```

## 5. Interface Definitions (Step 4 Output)

### Interface: ISettingsStore
- Defined in layer: Application
- Implemented by: SettingsStore (Infrastructure)
- Consumed by: SettingsModel
- Methods:
  - Load() -> LibrarySettings: reads the ConfigNode, migrating older formatVersions forward
  - Save(LibrarySettings) -> void: writes the ConfigNode

### Interface: INativeBridge
- Defined in layer: Application
- Implemented by: NativeBridge (Infrastructure)
- Consumed by: FrameLoopOrchestrator, InputCaptureTracker, LifecycleStateMachine
- Methods:
  - Initialize(graphicsDeviceKind) -> Result: loads the DLL, creates context/device, builds atlas
  - GetIoSnapshot() -> InputCaptureState: capture flags for the current frame
  - SubmitFrame() -> void: finalizes the ImGui frame and queues the render event
  - RebuildViewport(width, height) -> void: resolution-change rebuild
  - Shutdown() -> void: releases context and device

### Interface: IInputLockGateway
- Defined in layer: Application
- Implemented by: InputLockGateway (Infrastructure)
- Consumed by: FrameLoopOrchestrator
- Methods:
  - ApplyLocks(InputCaptureState) -> void: sets per-consumer locks while capturing
  - ReleaseLocks() -> void: removes all library-held locks

### Interface: IGameEventSource
- Defined in layer: Application
- Implemented by: GameEventHooks (Infrastructure)
- Consumed by: LifecycleStateMachine, FrameLoopOrchestrator
- Methods:
  - UiVisibilityChanged(bool visible): F2 hide/show signal
  - LoadingScreenChanged(bool loading): loading-screen signal
  - ResolutionChanged(int width, int height): viewport signal

### Interface: IFailureNotifier
- Defined in layer: Application
- Implemented by: FailureNotifier (Infrastructure)
- Consumed by: LifecycleStateMachine
- Methods:
  - Notify(FailureInfo) -> void: shows the one-time plain-language popup at main menu

### Interface: ILogger
- Defined in layer: Application
- Implemented by: DearKSPLogger (Infrastructure)
- Consumed by: all Application components
- Methods:
  - Error(message) / Warn(message) / Info(message) / Debug(message): `[DearKSP]`-prefixed KSP.log lines; Debug gated by verboseLogging

---

## 6. Pattern Selection (Step 5 Output)

| Problem / Concern | Selected Pattern | Justification |
|---|---|---|
| Third-party mods extend the library at runtime | Plugin / Mod Architecture | Host defines public API + registration hook; KSP loader is the plugin loader; consumer lifecycle = register → per-frame callback → auto-disable on fault threshold. |
| Library lifecycle has distinct behavioral modes | State Machine | Five states (Uninitialized/Initializing/Running/Suspended/Failed) with different update/render behavior and explicit transitions. |
| One persisted config entity | Repository (thin) | Single data source (`settings.cfg`) behind `ISettingsStore`; thin per YAGNI. |
| Exactly one native render context / GPU device | Singleton (composition-root-managed) | One ImGui context exists; owned by Composition and passed explicitly. |
| Consumer exception containment | None (direct) | Single try/catch + counter in FaultBarrier; no pattern warranted. |

---

## 7. Project Structure (Step 6 Output)

```text
Dear_KSP/
├── DearKSP.slnx
├── DearKSP.props.user                    # gitignored local KSP install pin
├── .gitignore / README.md / AGENTS.md
├── DearKSP/
│   ├── DearKSP.csproj
│   ├── DearKSP.version
│   ├── LibraryConfig.cs
│   ├── Properties/AssemblyInfo.cs
│   ├── Application/
│   │   ├── README.md
│   │   ├── DearKSP.cs
│   │   ├── ConsumerRegistry.cs
│   │   ├── FrameLoopOrchestrator.cs
│   │   ├── InputCaptureTracker.cs
│   │   ├── LifecycleStateMachine.cs
│   │   ├── FaultBarrier.cs
│   │   ├── SettingsModel.cs
│   │   └── Interfaces/
│   │       ├── ISettingsStore.cs
│   │       ├── INativeBridge.cs
│   │       ├── IInputLockGateway.cs
│   │       ├── IGameEventSource.cs
│   │       ├── IFailureNotifier.cs
│   │       └── ILogger.cs
│   └── Infrastructure/
│       ├── README.md
│       ├── DearKSPAddon.cs
│       ├── Composition.cs
│       ├── NativeBridge.cs
│       ├── InputLockGateway.cs
│       ├── GameEventHooks.cs
│       ├── SettingsStore.cs
│       ├── FailureNotifier.cs
│       └── DearKSPLogger.cs
├── DearKSPNative/
│   ├── README.md
│   ├── src/DearKSPNative.cpp
│   ├── build.bat
│   └── build_release.bat
├── DearKSPDemo/
│   ├── DearKSPDemo.csproj
│   ├── DearKSPDemo.version
│   └── DemoConsumer.cs
├── GameData/DearKSP/settings.cfg
└── tests/{Core.Tests,Application.Tests,Infrastructure.Tests}/
```

| File Path | Contains | Responsibility |
|---|---|---|
| `DearKSP/LibraryConfig.cs` | Constants | Mod name, paths, failure threshold, setting defaults |
| `DearKSP/Properties/AssemblyInfo.cs` | Attributes | `KSPAssembly` self-identification |
| `DearKSP/Application/DearKSP.cs` | Facade | Public consumer API (IsAvailable, registration, style) |
| `DearKSP/Application/ConsumerRegistry.cs` | Component | Consumer registration and ordering |
| `DearKSP/Application/FrameLoopOrchestrator.cs` | Component | Per-frame sequence |
| `DearKSP/Application/InputCaptureTracker.cs` | Component | Capture-state computation |
| `DearKSP/Application/LifecycleStateMachine.cs` | Component | State transitions and gates |
| `DearKSP/Application/FaultBarrier.cs` | Component | Consumer exception containment |
| `DearKSP/Application/SettingsModel.cs` | Component | Settings snapshot and change notice |
| `DearKSP/Application/Interfaces/*.cs` | Interfaces | Cross-boundary seams (§5) |
| `DearKSP/Infrastructure/DearKSPAddon.cs` | Entry point | KSPAddon bootstrap |
| `DearKSP/Infrastructure/Composition.cs` | DI root | Concrete wiring |
| `DearKSP/Infrastructure/NativeBridge.cs` | Component | DLL load + P/Invoke isolation |
| `DearKSP/Infrastructure/InputLockGateway.cs` | Component | InputLockManager wrapper |
| `DearKSP/Infrastructure/GameEventHooks.cs` | Component | Game-event signals |
| `DearKSP/Infrastructure/SettingsStore.cs` | Component | ConfigNode persistence |
| `DearKSP/Infrastructure/FailureNotifier.cs` | Component | Failure popup |
| `DearKSP/Infrastructure/DearKSPLogger.cs` | Component | Log wrapper |
| `DearKSPNative/src/DearKSPNative.cpp` | Component | Plugin export surface (Core layer) |
| `DearKSPDemo/DemoConsumer.cs` | Demo | Example window + torture test |

---

## 8. Dependencies & Error Handling (Step 7 Output)

### External Dependencies

| Library / API | Version | Purpose | Risk Level |
|---|---|---|---|
| KSP / UnityEngine | 1.12.x / 2019.4.18f1 | Host APIs | Med |
| KSPBuildTools | 1.1.1 | Build/deploy automation | Low |
| Dear ImGui | 1.92.9 (pinned) | UI core | Med |
| cimgui | cloned 2026-07-29 | C ABI | Med |
| Unity native plugin API | 2019.4 | Render injection | **High** (gating PoC) |
| D3D11 / DXGI / OpenGL32 | system | Backends | Low |
| MSVC / .NET Framework 4.8 ref assemblies | VS 2026 / 4.8 | Compilation | Low |

### Error Handling Strategy

| Failure Scenario | Detection | Response | User Notification |
|---|---|---|---|
| Native DLL missing/corrupt | LoadLibrary failure at init | Fail fast → Failed state, session-permanent | One popup (DK_FailNative) + detailed log |
| Unsupported graphics API | `SystemInfo.graphicsDeviceType` check before hooking | Fail fast → Failed state | One popup (DK_FailGraphics) + log |
| Managed/native version mismatch | Version handshake during init | Fail fast → Failed state | One popup (DK_FailVersion) + log |
| Render hook failure | Render event never fires / device error | Fail fast → Failed state | One popup (DK_FailNative) + log |
| Consumer callback throws | try/catch in FaultBarrier | Skip frame; auto-disable at 5 consecutive | Log-only under `[DearKSP]` |
| settings.cfg missing/corrupt | ConfigNode load failure | Regenerate defaults, continue | Log warning, silent |
| Resolution change | IGameEventSource signal | Rebuild viewport + atlas | Silent |

### Logging

- Target: KSP.log (via `UnityEngine.Debug`) through the `ILogger` seam.
- Level: error/warning/info always; debug gated by `verboseLogging`.
- Format: plain text, `[DearKSP]` prefix.

---

## 9. Milestones (Step 8 Output)

| # | Milestone | Components Implemented | Verification Method | Success Criteria |
|---|---|---|---|---|
| 1 | Build pipeline + deployment | slnx, csproj ×2, native build scripts, addon stub, Composition | `dotnet build` + `build.bat`, launch KSP | DLLs deploy to ReformTestInstance; `[DearKSP]` startup line in KSP.log |
| 2 | Render-injection PoC | RenderEventEntry, ImGuiContextHost, both backends, FontAtlasBuilder, minimal NativeBridge | In-game, D3D11 and `-force-glcore`, with Deferred+TUFX | ImGui demo window at full framerate (AC1, AC2) |
| 3 | Consumer API + core widgets | DearKSP facade, ConsumerRegistry, MVP widget bindings, DemoConsumer | Demo mod in-game | All MVP widgets via C# API, zero IMGUI (AC3); EqualMajor consumer load order (AC4) |
| 4 | Input locking + fault isolation | InputCaptureTracker, InputLockGateway, FaultBarrier | Interaction test + fault-injection consumer | Locks only while capturing (AC7); auto-disable at 5 throwing frames (AC9) |
| 5 | Settings + lifecycle + failure UX | SettingsStore/Model, LifecycleStateMachine, GameEventHooks, FailureNotifier | Config round-trip + sabotage tests | Settings persist/migrate (AC12); suspend/resume + resolution rebuild (AC10); all failure modes (AC8) |
| 6 | Benchmark + compatibility | Torture-test UI, instrumentation | Side-by-side vs IMGUI; full D16 environment | Beats IMGUI reference (AC5); no measurable FPS cost (AC6); compat clean (AC11) |

Dependencies are strictly sequential: 2 gates all managed API work; 3 requires 2's render path; 4–5 build on 3's consumer model; 6 validates everything.

---

## 10. Open Questions / Assumptions

- Assumption: the standard `GL.IssuePluginEvent` pattern works in KSP (no game-side precedent; milestone 2 exists to prove or kill this — fallback is uGUI per RESEARCH_NOTES §2).
- Assumption: DX12 works via D3D11 back-compat (Unity 2019.4 DX12 is experimental).
- Assumption: cimgui master + pinned imgui 1.92.9 remain the build baseline; re-pin deliberately, never casually.
- Question (deferred to milestone 2): exact injection point (which camera event / command-buffer timing) — requires runtime scene probing.

---

## Plan Checklist (Agent Self-Verification)

- [x] Every entity in Section 2 has a clear identity and lifetime classification
- [x] Every component in Section 3 has a single-responsibility description without "and" / "or"
- [x] Every cross-boundary dependency in Section 5 has an interface in Application
- [x] Dependency direction (Core ← Application ← Infrastructure) is never violated in Section 7
- [x] Every pattern in Section 6 has a stated justification matching its "Use When" condition
- [x] Every milestone in Section 9 has an observable, testable success criterion
- [x] No God Classes, no global mutable state, no deep inheritance trees
- [x] The MVP in Section 1.5 is achievable with Milestones 1 through 6 only
