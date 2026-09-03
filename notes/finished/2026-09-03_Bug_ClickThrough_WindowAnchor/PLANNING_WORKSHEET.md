# Planning Worksheet: ISSUES #001 (pointer blocker), #002 (viewport clamp setting), unit tests
## Date: 2026-09-03
## Type: Bugfix (#001, #002) + Feature (unit tests)
## Related Investigation: [INVESTIGATION_LOG.md](INVESTIGATION_LOG.md)

### Step 1 — Core Entities and State
| Entity | Identity | Attributes | Relationships | Lifetime |
|--------|----------|------------|---------------|----------|
| InputCaptureState | singleton per frame | MouseCaptured, KeyboardCaptured (bools) | produced by INativeBridge.GetIoSnapshot; consumed by InputCaptureTracker | transient (per frame) |
| PointerBlocker (new) | one GameObject | Canvas (Overlay, top sortOrder) + GraphicRaycaster + alpha-0 full-screen Image | driven by IPointerBlockerGateway.SetBlocked | session (DontDestroyOnLoad) |
| LibrarySettings | singleton record | + new field ClampWindowsToViewport (bool, default true) | persisted by ISettingsStore; held by SettingsModel | persisted (settings.cfg) |
| Held input-lock mask | singleton | + MAIN_MENU bit while MouseCaptured | computed by InputLockGateway.ComputeMask | transient |
| Native window list | ImGui context | window Pos/Size vs io.DisplaySize | enumerated by new native clamp export | transient |

### Step 2 — Behaviors and Responsibilities
| Component | Single-Sentence Responsibility | Command / Query / Both |
|-----------|-------------------------------|------------------------|
| IPointerBlockerGateway (new, Application/Interfaces) | Abstracts "swallow uGUI pointer events right now" so Application stays Unity-free. | Command |
| PointerBlockerGateway (new, Infrastructure) | Owns the blocker GameObject and toggles it active/inactive. | Command |
| InputCaptureTracker (modify) | Maps capture state to input locks AND the pointer-blocker toggle, firing only on transitions. | Command |
| InputLockGateway (modify) | Computes the ControlTypes mask from capture state (now including MAIN_MENU for mouse). | Query (pure computation) |
| SettingsModel / LibrarySettings / SettingsStore / LibraryConfig (modify) | Carry the new clampWindowsToViewport setting with default true (spec §9.1 pattern). | Both |
| INativeBridge.ClampWindowsToViewport (new method) | Tells the native core to clamp every ImGui window into the current DisplaySize. | Command |
| ContextHost clamp export (new, native) | Iterates GImGui->Windows and clamps positions into the viewport. | Command |
| Application.Tests (new, tests/) | Verifies Application-layer units via xUnit + InternalsVisibleTo. | Query |

### Step 3 — Data Flow
```
#001: ImGui IO --(InputCaptureState)--> InputCaptureTracker --(bool, on transition)--> IPointerBlockerGateway --> PointerBlocker GameObject (uGUI raycast intercept)
      InputCaptureTracker --(InputCaptureState + enabled ids)--> IInputLockGateway --> InputLockManager (mask now includes MAIN_MENU while mouse-captured)

#002: GameEvents.onScreenResolutionModified --(w,h)--> GameEventHooks --> Composition handler
      --> Bridge.RebuildViewport(w,h) [existing]
      --> if Settings.ClampWindowsToViewport --> INativeBridge.ClampWindowsToViewport() --> native GImGui window clamp
      settings.cfg --(ConfigNode)--> SettingsStore --> SettingsModel.ClampWindowsToViewport

#003: dotnet test --> Application.Tests --> DearImGuiKSP.dll internals (InternalsVisibleTo)
```

### Step 4 — Boundaries and Interfaces
| Interface | Defined In | Implemented By | Consumed By | Purpose |
|-----------|------------|----------------|-------------|---------|
| IPointerBlockerGateway (new) | Application/Interfaces | Infrastructure.PointerBlockerGateway | InputCaptureTracker | Pointer-block toggle without Unity types in Application |
| IInputLockGateway (existing) | Application/Interfaces | Infrastructure.InputLockGateway | InputCaptureTracker | Unchanged signature; mask computation gains MAIN_MENU bit |
| INativeBridge (existing, +1 method) | Application/Interfaces | Infrastructure.NativeBridge | Composition (resolution handler) | ClampWindowsToViewport added — internal interface, additive |
| ISettingsStore (existing) | Application/Interfaces | Infrastructure.SettingsStore | SettingsModel | Unchanged; new key handled inside LibrarySettings |

### Layering Check
- [x] Core (native) imports nothing from managed layers; new clamp export is plain C ABI, zero game knowledge.
- [x] Application imports from Core only via interfaces (new IPointerBlockerGateway is an interface; INativeBridge gains a method).
- [x] Infrastructure imports from Core and Application (PointerBlockerGateway implements the Application interface; only place UnityEngine.UI is touched).
- [x] Tests reference the managed assembly only; no KSP/Unity runtime needed for Application types.
