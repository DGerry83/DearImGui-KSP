# Planning Worksheet: Dear KSP

## Date: 2026-07-29

## Design Spec Reference: [DESIGN_SPEC.md](../2026-07-29_DesignSpec_DearKSP_UI_Library/DESIGN_SPEC.md)

### Step 1 — Core Entities and State

| Entity | Identity | Attributes | Relationships | Lifetime |
|--------|----------|------------|---------------|----------|
| LibrarySettings | Singleton config (one per install) | formatVersion:int, uiScale:float, fontScale:float, theme:string, verboseLogging:bool, enabled:bool | Read/written by SettingsStore; consumed by frame loop and atlas | Persistent (`GameData/DearKSP/settings.cfg` ConfigNode) |
| ConsumerRegistration | Consumer-supplied string ID | id:string, callback:per-frame delegate, enabled:bool, consecutiveFailureCount:int, registrationOrder:int | Owned by ConsumerRegistry; references the consumer's delegate | Transient (per game session) |
| RenderContext | One per native DLL load | ImGui context pointer, graphics device handle, font atlas texture, viewport w/h, backend kind (D3D11/OpenGL) | Owned by native Core; rebuilt on resolution change | Transient (per game session) |
| InputCaptureState | One per frame | mouseCaptured:bool, keyboardCaptured:bool, activeLockIds:list | Computed by InputCaptureTracker; consumed by InputLockGateway | Transient (per frame) |
| FailureInfo | One per failure event | kind:enum (MissingNative / UnsupportedGraphicsApi / VersionMismatch / RenderHookFailure), technicalDetail:string | Produced by initialization; consumed by LifecycleStateMachine and FailureNotifier | Transient (logged + shown once) |

Value objects / enumerations:

- **LibraryState** (enum): `Uninitialized`, `Initializing`, `Running`, `Suspended`, `Failed`.
- **FailureKind** (enum): `MissingNative`, `UnsupportedGraphicsApi`, `VersionMismatch`, `RenderHookFailure`.
- **Theme** (value object): name string; MVP valid set = `{ "dark" }`.

### Step 2 — Behaviors and Responsibilities

| Component | Single-Sentence Responsibility | Command / Query / Both | Layer |
|-----------|-------------------------------|------------------------|-------|
| ImGuiContextHost | Owns the lifecycle of the single native ImGui context. | Command | Core (native) |
| RenderBackendD3D11 | Translates ImGui draw data into D3D11 GPU commands. | Command | Core (native) |
| RenderBackendOpenGL | Translates ImGui draw data into OpenGL GPU commands. | Command | Core (native) |
| RenderEventEntry | Exposes the Unity low-level plugin export surface (plugin load/unload, render event). | Both | Core (native) |
| FontAtlasBuilder | Builds the font atlas texture from embedded ProggyClean at init and on scale change. | Command | Core (native) |
| DearKSP (public facade) | Exposes availability, consumer registration, and style access to consumer mods. | Both | Application (managed public API) |
| ConsumerRegistry | Tracks registered consumers in registration order with per-consumer fault state. | Both | Application |
| FrameLoopOrchestrator | Executes the per-frame sequence from input sampling through native render handoff. | Command | Application |
| InputCaptureTracker | Computes mouse/keyboard capture state from ImGui IO each frame. | Query | Application |
| LifecycleStateMachine | Enforces legal state transitions and per-state behavior gates. | Both | Application |
| FaultBarrier | Catches consumer exceptions, counts consecutive failures, and disables consumers at the threshold. | Command | Application |
| SettingsModel | Holds the in-memory settings snapshot and raises change notifications. | Both | Application |
| DearKSPAddon | Is the KSP entry point and composition root that wires all components at startup. | Command | Infrastructure |
| NativeBridge | Loads the native DLL explicitly and isolates all P/Invoke declarations. | Both | Infrastructure |
| InputLockGateway | Applies and releases KSP input locks under per-consumer lock IDs. | Command | Infrastructure |
| GameEventHooks | Translates game events (F2 UI hide, loading screens, resolution change) into library signals. | Query | Infrastructure |
| SettingsStore | Loads and saves LibrarySettings as a KSP ConfigNode file. | Both | Infrastructure |
| FailureNotifier | Shows the single plain-language startup-failure PopupDialog at the main menu. | Command | Infrastructure |
| DearKSPLogger | Writes `[DearKSP]`-prefixed lines to KSP.log with verbosity control. | Command | Infrastructure |

### Step 3 — Data Flow

Startup:

```
[settings.cfg] --(LibrarySettings)--> SettingsStore --(LibrarySettings)--> SettingsModel --(scale/theme)--> FontAtlasBuilder
[KSPAddon MainMenu] --(start signal)--> DearKSPAddon --(concrete instances)--> Composition root --(wired components)--> LifecycleStateMachine
[Native DLL on disk] --(LoadLibrary)--> NativeBridge --(device/context handles)--> RenderContext
[FailureInfo] --(failure)--> LifecycleStateMachine --(Failed)--> FailureNotifier --(popup text)--> [Player screen]
```

Per frame (Running):

```
[Unity input] --(mouse/keyboard state)--> FrameLoopOrchestrator --(ImGui IO)--> InputCaptureTracker --(InputCaptureState)--> InputLockGateway --(locks)--> [KSP InputLockManager]
[Consumer mods] --(UI declarations)--> ConsumerRegistry --(callbacks in order)--> FaultBarrier --(guarded invocation)--> FrameLoopOrchestrator
FrameLoopOrchestrator --(NewFrame/Render)--> ImGuiContextHost --(draw data)--> RenderBackend{D3D11,OpenGL} --(GPU commands)--> [Screen via render thread]
[Game events F2/load/resolution] --(signals)--> GameEventHooks --(state signals)--> LifecycleStateMachine --(Suspend/Resume/rebuild)--> FrameLoopOrchestrator / RenderContext
```

No fan-out warrants an event bus; consumer communication is one-to-one registration with direct calls (D18).

### Step 4 — Boundaries and Interfaces

| Interface | Defined In | Implemented By | Consumed By | Purpose |
|-----------|------------|----------------|-------------|---------|
| ISettingsStore | Application | SettingsStore (Infrastructure) | SettingsModel | Keeps ConfigNode I/O out of the settings model |
| INativeBridge | Application | NativeBridge (Infrastructure) | FrameLoopOrchestrator, LifecycleStateMachine | Isolates P/Invoke and DLL loading so Application never touches interop directly |
| IInputLockGateway | Application | InputLockGateway (Infrastructure) | FrameLoopOrchestrator | Keeps KSP `InputLockManager` specifics out of the frame loop |
| IGameEventSource | Application | GameEventHooks (Infrastructure) | LifecycleStateMachine, FrameLoopOrchestrator | Decouples game-event subscription from state policy |
| IFailureNotifier | Application | FailureNotifier (Infrastructure) | LifecycleStateMachine | Lets the state machine report failure without knowing uGUI |
| ILogger | Application | DearKSPLogger (Infrastructure) | All Application components | Single `[DearKSP]` logging seam with verbosity control |

Native Core calls upward only through the per-frame callback abstraction defined by Application (the registered consumer delegates marshaled through cimgui-style calls managed-side; the native side exposes a pure C ABI and never references managed code).

### Step 5 — Pattern Selection

| Problem / Concern | Selected Pattern | Justification |
|---|---|---|
| Third-party mods extend the library at runtime | Plugin / Mod Architecture | Dear KSP is a host defining a public API + registration hook; KSP's loader is the plugin loader; consumer lifecycle = register → per-frame callback → auto-disable on repeated exceptions. |
| Library lifecycle has distinct behavioral modes | State Machine | Five states with different update/render behavior and explicit transitions (spec §5.2). |
| One persisted config entity | Repository (thin) | Single data source abstracted behind `ISettingsStore`; deliberately thin per YAGNI. |
| Exactly one native render context / GPU device | Singleton (composition-root-managed) | One ImGui context exists; owned by the composition root and passed explicitly — not a mutable global. |
| Consumer exception containment | None (direct guard) | A single try/catch with a counter in FaultBarrier; no pattern warranted. |

### Step 6 — Project Layout

```text
Dear_KSP/
├── DearKSP.slnx
├── DearKSP.props.user              # gitignored: pins KSPBT_GameRoot
├── .gitignore
├── README.md
├── AGENTS.md
├── DearKSP/                        # managed plugin — Application + Infrastructure layers
│   ├── DearKSP.csproj              # SDK-style, net48, KSPBuildTools
│   ├── DearKSP.version             # AVC version template
│   ├── LibraryConfig.cs            # constants: mod name, paths, failure threshold, defaults
│   ├── Properties/
│   │   └── AssemblyInfo.cs         # [assembly: KSPAssembly("DearKSP", ...)]
│   ├── Application/
│   │   ├── README.md
│   │   ├── DearKSP.cs              # public consumer-facing facade
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
│       ├── DearKSPAddon.cs         # KSPAddon entry point
│       ├── Composition.cs          # DI root — the only place concrete Infrastructure is instantiated
│       ├── NativeBridge.cs
│       ├── InputLockGateway.cs
│       ├── GameEventHooks.cs
│       ├── SettingsStore.cs
│       ├── FailureNotifier.cs
│       └── DearKSPLogger.cs
├── DearKSPNative/                  # C++ Core layer (no KSP/Unity knowledge)
│   ├── README.md
│   ├── src/
│   │   └── DearKSPNative.cpp       # plugin export surface stub
│   ├── build.bat                   # debug cl.exe build (vcvars64)
│   └── build_release.bat           # release cl.exe build
├── DearKSPDemo/                    # demo mod — separate install (D7)
│   ├── DearKSPDemo.csproj
│   ├── DearKSPDemo.version
│   └── DemoConsumer.cs
├── GameData/
│   └── DearKSP/
│       └── settings.cfg            # default config, staged + deployed by build
├── tests/
│   ├── Core.Tests/
│   ├── Application.Tests/
│   └── Infrastructure.Tests/
└── notes/                          # existing artifact taxonomy (active/finished/archive/knowledge/indices/plans)
```

Dependency direction check: managed `Application/` references nothing KSP/Unity; `Infrastructure/` references Application interfaces + KSP/Unity; native Core references neither. No cycles.

### Step 7 — Dependencies and Risks

| Dependency | Version | Purpose | Risk Level |
|---|---|---|---|
| KSP (Assembly-CSharp, UnityEngine) | 1.12.x / Unity 2019.4.18f1 | Runtime host APIs | Med — version-locked game |
| KSPBuildTools (NuGet) | 1.1.1 | Game detection, references, GameData staging, AVC version file, deploy | Low |
| Dear ImGui | 1.92.9 (cimgui submodule pin) | UI core, compiled into native DLL | Med — API churn across versions; pinned |
| cimgui | master (repo cloned 2026-07-29) | C ABI for P/Invoke | Med — generator drift; pinned by clone |
| Unity low-level native plugin API | 2019.4 | Render-thread injection | **High** — unproven in KSP (gating PoC) |
| Windows SDK / D3D11, DXGI, OpenGL32 | system | Render backends | Low |
| MSVC (VS 2026, vcvars64) | 18.x | Native compilation | Low |
| .NET Framework reference assemblies | 4.8 | Managed target (net48) | Low |

Error handling strategy: initialization failures → fail fast into `Failed` state with one popup + detailed log (spec §5.4); consumer callback failures → catch/skip/count, auto-disable at 5 consecutive (spec §5.3); settings file missing/corrupt → regenerate defaults and log; unsupported graphics API → detect via `SystemInfo.graphicsDeviceType` before hooking, fail into `Failed`.

Logging: KSP.log via `UnityEngine.Debug.Log` wrapper, `[DearKSP]` prefix, plain text; levels = error/warning/info always, debug gated by `verboseLogging`.

Platform assumptions: Windows x64 only for MVP; D3D11 + OpenGL backends; `LoadLibrary` + `SetDllDirectory` bootstrap required because implicit `[DllImport]` resolution fails from GameData subfolders (CinematicRecorder-documented gotcha).

### Step 8 — Verification Checkpoints (Sequential Milestones)

| # | Milestone | Components | Verification | Success Criteria | PlanImplementation Chunk Group |
|---|-----------|------------|--------------|------------------|-------------------------------|
| 1 | Build pipeline + deployment | slnx, both csproj, native build scripts, addon stub | `dotnet build` + native `build.bat`; launch KSP | Both DLLs deploy into ReformTestInstance `GameData/DearKSP/`; KSP.log shows `[DearKSP]` startup line | G1 |
| 2 | Render-injection PoC | RenderEventEntry, ImGuiContextHost, both backends, NativeBridge (minimal) | In-game on D3D11 and `-force-glcore` | ImGui demo window renders in-game at full framerate on D3D11 (AC1) and OpenGL (AC2), with Deferred + TUFX active | G2 |
| 3 | Consumer API + core widgets | DearKSP facade, ConsumerRegistry, cimgui bindings for MVP widgets, DemoConsumer | Demo mod in-game | Demo window with all MVP widgets via C# API only, zero IMGUI (AC3); separate consumer loads after library via EqualMajor dependency (AC4) | G3 |
| 4 | Input locking + fault isolation | InputCaptureTracker, InputLockGateway, FaultBarrier | In-game interaction + fault-injection consumer | Locks engage only while capturing and release cleanly (AC7); throwing consumer auto-disabled after 5 frames, others unaffected (AC9) | G4 |
| 5 | Settings + lifecycle + failure UX | SettingsStore/Model, LifecycleStateMachine, GameEventHooks, FailureNotifier | Config round-trip + sabotage tests | Settings persist and migrate (AC12); F2/loading suspend, resolution rebuilds (AC10); each failure mode → Failed + one popup + IsAvailable=false (AC8) | G5 |
| 6 | Benchmark + compatibility validation | Demo torture-test UI, instrumentation | In-game side-by-side + full mod environment | Torture test beats IMGUI reference (AC5); no measurable FPS cost with 1–3 windows (AC6); full D16 environment clean (AC11) | G6 |
