# Project Skeleton: DearImGui-KSP

## Date: 2026-07-29
## Session: `notes/finished/2026-07-29_NewProject_DearImGuiKSP/`

File tree as created, with status. Layering follows CORE_PROTOCOLS §5.6 adapted to the KSP mod stack: **Core = native C++** (`DearImGuiKSPNative/`), **Application + Infrastructure = managed** (`DearImGuiKSP/`).

```text
DearImGui-KSP/
├── DearImGuiKSP.slnx                          # XML solution, 2 managed projects        [builds]
├── DearImGuiKSP.props.user                    # gitignored KSP install pin              [local]
├── .gitignore                                                            [created]
├── README.md                             # human-facing overview + build         [created]
├── AGENTS.md                             # agent onboarding (updated: layout,    [updated]
│                                         #   build commands, bootstrap status)
├── DearImGuiKSP/                              # MANAGED: Application + Infrastructure
│   ├── DearImGuiKSP.csproj                    # SDK-style net48, KSPBuildTools 1.1.1  [builds]
│   ├── DearImGuiKSP.version                   # AVC template                          [staged]
│   ├── LibraryConfig.cs                  # constants/defaults                    [stub]
│   ├── Properties/AssemblyInfo.cs        # note: KSPAssembly is KSPBT-generated  [stub]
│   ├── Application/
│   │   ├── README.md                     # layer rules                           [created]
│   │   ├── DearImGuiKSP.cs                    # public facade (IsAvailable)           [stub]
│   │   ├── ConsumerRegistry.cs                                               [stub]
│   │   ├── FrameLoopOrchestrator.cs                                          [stub]
│   │   ├── InputCaptureTracker.cs                                            [stub]
│   │   ├── LifecycleStateMachine.cs                                          [stub]
│   │   ├── FaultBarrier.cs                                                   [stub]
│   │   ├── SettingsModel.cs                                                  [stub]
│   │   └── Interfaces/                   # 6 seams for Infrastructure          [stubs]
│   │       ├── ISettingsStore.cs / INativeBridge.cs / IInputLockGateway.cs
│   │       ├── IGameEventSource.cs / IFailureNotifier.cs / ILogger.cs
│   └── Infrastructure/
│       ├── README.md                     # layer rules                           [created]
│       ├── DearImGuiKSPAddon.cs               # KSPAddon entry; logs startup line     [working]
│       ├── Composition.cs                # DI root                               [stub]
│       ├── NativeBridge.cs / InputLockGateway.cs / GameEventHooks.cs         [stubs]
│       ├── SettingsStore.cs / FailureNotifier.cs / DearImGuiKSPLogger.cs          [stubs]
├── DearImGuiKSPNative/                        # NATIVE: Core layer
│   ├── README.md                         # layer rules, build, sourcing          [created]
│   ├── src/DearImGuiKSPNative.cpp             # Unity plugin exports + version stub   [compiles]
│   ├── build.bat                         # debug cl.exe build -> GameData        [verified]
│   └── build_release.bat                 # release cl.exe build                  [created]
├── DearImGuiKSPDemo/                          # demo mod, SEPARATE install (D7)
│   ├── DearImGuiKSPDemo.csproj                # KSPBT; ProjectReference Private=false [builds]
│   ├── DearImGuiKSPDemo.version               # AVC template                          [staged]
│   ├── DemoConsumer.cs                   # example window host (skeleton)        [working]
│   └── Properties/AssemblyInfo.cs        # KSPAssemblyDependencyEqualMajor       [working]
├── GameData/                             # staging, mirrored into game on build
│   ├── DearImGuiKSP/settings.cfg              # default config (spec §9)              [created]
│   └── (build-generated: DLLs, .version, Readme.txt — gitignored via bin/obj? NO:
│        staged DLLs land here; see note below)
└── tests/
    ├── Core.Tests/README.md              # placeholders                          [created]
    ├── Application.Tests/README.md                                           [created]
    └── Infrastructure.Tests/README.md                                        [created]
```

## Conventions established

- **Build model** mirrors CinematicRecorder: SDK-style `net48` + KSPBuildTools 1.1.1 (game detection via gitignored `*.props.user`, auto game references, GameData staging, AVC version generation, deploy-on-build); native via raw `cl.exe` batch scripts after `vcvars64`, no CMake/vcxproj/vcpkg.
- **imgui/cimgui are not vendored** — compiled from the sibling clone `~\source\repos\cimgui` (imgui 1.92.9 pinned by submodule).
- **`KSPAssembly` is KSPBuildTools-generated** from csproj `<Version>`; only `KSPAssemblyDependencyEqualMajor` is declared by hand (demo), per D17.
- Dependency direction enforced: Application is Unity-free; only Infrastructure references KSP/Unity; native Core references neither.

## Verification performed (beyond gate requirements)

- `dotnet build DearImGuiKSP.slnx` — clean, 0 warnings (after removing duplicate KSPAssembly attribute).
- Staging + deploy confirmed: `GameData/DearImGuiKSP/{Plugins/DearImGuiKSP.dll, settings.cfg, DearImGuiKSP.version, Readme.txt}` and `GameData/DearImGuiKSPDemo/` mirrored into `the pinned KSP test instance\GameData\`.
- `DearImGuiKSPNative\build.bat` — compiles and copies `DearImGuiKSPNative.dll` into staging; mirror confirmed in game instance.
- Not yet verified (milestone 1 completion): in-game `[DearImGuiKSP]` startup log line.
