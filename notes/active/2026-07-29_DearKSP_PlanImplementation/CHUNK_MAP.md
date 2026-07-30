# Chunk Map: Dear KSP Implementation

## Date: 2026-07-29
## Digest Reference: [PLAN_DIGEST.md](PLAN_DIGEST.md)

### Chunk List

| ID | Name | Type | Files/Records | Est. Complexity | Milestone | Status |
|----|------|------|---------------|-----------------|-----------|--------|
| C1 | M1 close-out: logger, composition wiring, in-game log line | Cleanup | `DearKSPLogger.cs`, `Composition.cs`, `DearKSPAddon.cs` | Low | M1 | Pending |
| C2 | Native plugin exports + device detection | Foundation | `DearKSPNative/src/` (plugin entry, Unity interface structs) | Med | M2 | Pending |
| C3 | imgui/cimgui build integration + context host + font atlas | Foundation | `build*.bat`, new native sources (context host, atlas) | Med | M2 | Pending |
| C4 | D3D11 backend + managed bridge PoC | Vertical slice | native D3D11 backend, `NativeBridge.cs`, addon frame hook | **High** | M2 | Pending |
| C5 | OpenGL backend | Vertical slice | native GL backend | Med | M2 | Pending |
| C6 | cimgui interop layer (MVP widgets) | Foundation | `DearKSP/Interop/` (internal P/Invoke bindings) | Med | M3 | Pending |
| C7 | Consumer API: facade, registry, frame loop | Vertical slice | `DearKSP.cs`, `ConsumerRegistry.cs`, `FrameLoopOrchestrator.cs`, `Composition.cs` | Med | M3 | Pending |
| C8 | Demo example window + load-order proof | Consumer | `DemoConsumer.cs` | Low | M3 | Pending |
| C9 | Input capture + locking | Vertical slice | `InputCaptureTracker.cs`, `InputLockGateway.cs` | Med | M4 | Pending |
| C10 | Fault barrier + auto-disable | Vertical slice | `FaultBarrier.cs`, `ConsumerRegistry.cs` | Low | M4 | Pending |
| C11 | Settings store + model + migration | Vertical slice | `SettingsStore.cs`, `SettingsModel.cs` | Med | M5 | Pending |
| C12 | Lifecycle state machine + game-event hooks | Vertical slice | `LifecycleStateMachine.cs`, `GameEventHooks.cs` | Med | M5 | Pending |
| C13 | Failure notifier + failure-mode tests | Consumer | `FailureNotifier.cs`, `NativeBridge.cs` (handshake paths) | Low | M5 | Pending |
| C14 | Torture-test benchmark + instrumentation | Vertical slice | `DearKSPDemo/` benchmark window | Med | M6 | Pending |
| C15 | Full compatibility environment validation | Integration | none (test-only) | Low | M6 | Pending |

### Dependency Graph

```
C1 ──► C2 ──► C3 ──► C4 ──► C5 ──► C6 ──► C7 ──► C8 ──► C9  ─┐
                                                  │     ├─► C10 ─┤
                                                  │     └─► C11 ─┤─► C12 ──► C13 ──► C14 ──► C15
                                                  └──────────────┘
```

- Solid arrows = hard dependency (cannot compile/run without the predecessor).
- C9, C10, C11 have no hard dependencies on each other → **parallel group A** after C8.
- C12 depends on C11 only softly (verboseLogging gates logger output; a constant stub is acceptable until C11 lands).

### Interface Contracts Between Chunks

| From | To | Contract | Rationale |
|------|----|----------|-----------|
| C2 | C4/C5 | Unity export surface: `UnityPluginLoad/Unload`, render-event callback ID, device-kind query | Single entry point both backends hang off |
| C3 | C4/C5 | Context host C ABI: init/shutdown, NewFrame/Render, atlas texture handle | Backends consume a live context, never own it |
| C4 | C6+ | `INativeBridge` method set: Initialize, GetIoSnapshot, SubmitFrame, RebuildViewport, Shutdown + version handshake | The interop boundary every managed chunk rides on; locked after C4 |
| C6 | C7 | Internal widget bindings for MVP set (windows, buttons, text, sliders, input fields) with safe C# types — no raw pointers in public API | Q46 leaky-abstraction mitigation |
| C7 | C8+ | Public facade: `DearKSP.IsAvailable`, `Register(id, callback)`, `Unregister(id)` | Consumer contract; additive-only thereafter (invariant 2) |
| C11 | C12 | `SettingsModel` change notification + `verboseLogging` read | Logger gating without Infrastructure reaching into settings |

### Milestone Mapping

| Milestone | Chunks Advancing It | Verification Gate |
|-----------|---------------------|-------------------|
| M1 Build pipeline | C1 | `[DearKSP]` startup line in KSP.log |
| M2 Render PoC | C2, C3, C4, C5 | Demo window at full framerate on D3D11 (AC1) and OpenGL (AC2), Deferred+TUFX active |
| M3 Consumer API | C6, C7, C8 | All MVP widgets via C# API, zero IMGUI (AC3); EqualMajor load order (AC4) |
| M4 Input + faults | C9, C10 | Locks only while capturing (AC7); 5-strike auto-disable (AC9) |
| M5 Settings + lifecycle | C11, C12, C13 | Config round-trip/migration (AC12); suspend/resume + rebuild (AC10); failure modes (AC8) |
| M6 Benchmark + compat | C14, C15 | Beats IMGUI reference (AC5); no measurable FPS cost (AC6); D16 environment clean (AC11) |

### Cross-Cutting Concerns

- Every chunk respects the five invariants in PLAN_DIGEST.md (layer boundaries, additive public API, net48/C++17/x64, version lockstep, rendering constraints).
- Managed/native handshake surface changes always ship in the same chunk on both sides.
- No chunk removes scaffolding owned by another chunk; stubs are tracked in INTEGRATION_CONTRACT.md.
- In-game verification always runs against ReformTestInstance with the D16 mod environment, never a clean install.

### Chunking Decisions & Rationale

- **C4 fuses the D3D11 backend with the minimal managed bridge**: the PoC's success criterion (AC1) is in-game and end-to-end; splitting backend from bridge would produce an unverifiable intermediate. This is the one deliberately larger chunk, and it is the plan's kill-or-continue gate.
- **C5 separate from C4**: OpenGL is a different failure domain; isolating it keeps a D3D11 success intact if GL misbehaves.
- **C6 separate from C7**: the interop layer is pure mechanical binding work with no design decisions; the consumer API above it is where the design lives. Disjoint files, clean seam.
- **C9/C10/C11 parallel**: disjoint files, all consumers of the stable C7 contract.
- **C15 is test-only**: compatibility validation writes no code; kept as its own chunk so a failure blocks release explicitly rather than silently.

---

Phase 1 complete. Chunk map ready. Waiting for approval to proceed to Phase 2 (sequencing & contract design).
