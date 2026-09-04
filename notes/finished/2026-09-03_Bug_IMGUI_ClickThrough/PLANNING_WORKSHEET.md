# Planning Worksheet: ISSUES #003 — IMGUI (OnGUI) click bleed-through
## Date: 2026-09-03
## Type: Bugfix
## Related Investigation: [INVESTIGATION_LOG.md](INVESTIGATION_LOG.md) · Contract: [ARCHITECTURE_CONTRACT.md](ARCHITECTURE_CONTRACT.md)

### Step 1 — Core Entities and State
| Entity | Identity | Attributes | Relationships | Lifetime |
|--------|----------|------------|---------------|----------|
| Shield flags | two bools on the eater component | mouseCaptured, keyboardCaptured | set transition-only by InputCaptureTracker via interface; read per OnGUI event | session |
| InputCaptureState (existing) | per-frame snapshot | MouseCaptured, KeyboardCaptured | produced by INativeBridge.GetIoSnapshot | transient |

### Step 2 — Behaviors and Responsibilities
| Component | Single-Sentence Responsibility | Command / Query / Both |
|-----------|-------------------------------|------------------------|
| IImguiEventEaterGateway (new) | Abstracts "starve Unity-IMGUI of the current input event" so Application stays Unity-free. | Command |
| ImguiEventEaterGateway (new, Infrastructure) | Eats mouse/keyboard `Event.current` in an early-ordered OnGUI while shielded. | Command |
| InputCaptureTracker (modify) | Maps capture state to locks, the uGUI blocker, and the IMGUI eater flags, all transition-only. | Command |

### Step 3 — Data Flow
```
ImGui IO --(InputCaptureState, prev frame)--> InputCaptureTracker
    --(bool, on mouse-capture transition)--> IImguiEventEaterGateway.SetMouseShielded
    --(bool, on keyboard-capture transition)--> IImguiEventEaterGateway.SetKeyboardShielded
Unity event queue --(Event.current)--> ImguiEventEaterGateway.OnGUI [first in script order]
    --(e.Use() while shielded)--> later OnGUI handlers (IMGUI mods) see EventType.Used
```

### Step 4 — Boundaries and Interfaces
| Interface | Defined In | Implemented By | Consumed By | Purpose |
|-----------|------------|----------------|-------------|---------|
| IImguiEventEaterGateway (new) | Application/Interfaces | Infrastructure.ImguiEventEaterGateway | InputCaptureTracker | Shield flags without Unity types in Application |

### Layering Check
- [x] Native untouched (no handshake change; stays v4).
- [x] Application: interface + transition logic only, no Unity types.
- [x] Infrastructure: the only `Event.current`/OnGUI/UnityEngine contact.
