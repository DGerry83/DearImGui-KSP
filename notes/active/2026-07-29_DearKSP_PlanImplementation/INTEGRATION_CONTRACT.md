# Integration Contract: Dear KSP Implementation

## Date: 2026-07-29
## Chunk Map Reference: [CHUNK_MAP.md](CHUNK_MAP.md)

### Execution Order

| Order | Chunk ID | Name | Why This Position | Parallel Group |
|-------|----------|------|-------------------|----------------|
| 1 | C1 | M1 close-out | Low-risk; proves in-game logging before any native risk is taken | — |
| 2 | C2 | Native plugin exports + device detection | Foundation both backends hang off | — |
| 3 | C3 | imgui/cimgui integration + context host + atlas | Backends need a live context and font texture | — |
| 4 | C4 | D3D11 backend + managed bridge PoC | Kill-or-continue gate (AC1); end-to-end by design | — |
| 5 | C5 | OpenGL backend | Isolated failure domain after D3D11 proven (AC2) | — |
| 6 | C6 | cimgui interop layer | Mechanical bindings; precedes the API that consumes them | — |
| 7 | C7 | Consumer API: facade, registry, frame loop | Locks the public consumer contract | — |
| 8 | C8 | Demo window + load-order proof | First real consumer; validates C7 (AC3, AC4) | — |
| 9 | C9 | Input capture + locking | Disjoint files; consumes stable C7 contract | A |
| 9 | C10 | Fault barrier + auto-disable | Disjoint files; consumes stable C7 contract | A |
| 9 | C11 | Settings store + model + migration | No dependency on input/faults; fills parallel slot | A |
| 10 | C12 | Lifecycle state machine + game-event hooks | Needs SettingsModel for verboseLogging gating (soft) | — |
| 11 | C13 | Failure notifier + failure-mode tests | Needs Failed state from C12 (AC8) | — |
| 12 | C14 | Torture-test benchmark + instrumentation | Needs the full M3–M5 surface to be meaningful (AC5, AC6) | — |
| 13 | C15 | Compatibility environment validation | Final gate; test-only (AC11) | — |

### Stub / Scaffolding List

| Stub | Location | Replaced By | Remove In |
|------|----------|-------------|-----------|
| `DearKSP.IsAvailable => false` hard-coded | `Application/DearKSP.cs` | Wired to LifecycleStateMachine | C7 |
| Skeleton log line in `DearKSPAddon.Awake` | `Infrastructure/DearKSPAddon.cs` | Composition-driven startup | C1 |
| `DearKSPNative_GetVersion` returning placeholder `1` | `src/DearKSPNative.cpp` | Real lockstep handshake value | C4 |
| Empty `INativeBridge` interface | `Application/Interfaces/INativeBridge.cs` | C4 method set (locked) | C4 |
| `verboseLogging` constant stub (if C12 precedes C11 completion) | `DearKSPLogger.cs` | SettingsModel-backed value | C11 |
| Empty widget-binding surface | `DearKSP/Interop/` (created in C6) | Real bindings | C6 |

### Inter-Chunk Contracts (Locked)

| Contract Element | Definition | Owner | Consumers |
|-------------------|------------|-------|-----------|
| Unity export surface | `UnityPluginLoad(void*)`, `UnityPluginUnload()`, render-event callback registered via `GL.IssuePluginEvent`; device-kind query export | C2 | C4, C5 |
| Context host C ABI | init/shutdown, NewFrame/Render, atlas texture handle; backends consume, never own, the context | C3 | C4, C5 |
| `INativeBridge` | `Initialize(deviceKind) → result`, `GetIoSnapshot() → capture state`, `SubmitFrame()`, `RebuildViewport(w,h)`, `Shutdown()`; version handshake inside Initialize | C4 | C6, C7, C9, C12 |
| Widget bindings | Internal-only cimgui P/Invoke for MVP widgets behind safe C# types; no raw pointers escape `DearKSP/Interop/` | C6 | C7 |
| Public facade | `DearKSP.IsAvailable`, `Register(string id, Action callback)`, `Unregister(string id)`; additive-only within a major version | C7 | C8 and all external consumers |
| Settings seam | `SettingsModel` exposes current values + change notification; `ILogger.Debug` gated by `verboseLogging` | C11 | C12, all loggers |

### Build/Test Sequence

| After Chunk | Verification |
|-------------|--------------|
| C1 | `dotnet build` clean; KSP launch shows `[DearKSP]` line in KSP.log |
| C2 | `build.bat` compiles; exports visible (`dumpbin /exports`) |
| C3 | `build.bat` compiles with imgui/cimgui TUs; DLL size sanity |
| C4 | **AC1**: ImGui demo window renders in-game on D3D11 at full framerate, Deferred+TUFX active |
| C5 | **AC2**: same under `-force-glcore` |
| C6 | `dotnet build` clean; bindings resolve against the native DLL |
| C7 | `dotnet build` clean; registry/facade unit-inspectable |
| C8 | **AC3**: demo window with all MVP widgets, zero IMGUI; **AC4**: EqualMajor load order in log |
| C9 | **AC7**: locks engage on hover/text focus, release cleanly |
| C10 | **AC9**: fault-injection consumer auto-disabled after 5 throwing frames; others unaffected |
| C11 | **AC12**: settings round-trip; old-format config migrates; kill switch works |
| C12 | **AC10**: F2/loading suspend; resolution change rebuilds viewport/atlas |
| C13 | **AC8**: each failure mode → Failed state + one popup + `IsAvailable == false` |
| C14 | **AC5**: torture test beats IMGUI reference; **AC6**: no measurable FPS cost with 1–3 windows, <1 ms managed |
| C15 | **AC11**: full D16 environment clean |

### Rollback Plan

- Git checkpoint before each chunk (commit per chunk; chunks never share in-flight files except C10's `ConsumerRegistry.cs`, which is sequenced inside group A accordingly).
- If a chunk fails verification: revert only that chunk's files to its checkpoint.
- **If C4 fails**: stop the sequence entirely — the render-injection premise is dead. Do not patch around it. Return to the user for the uGUI-fallback re-plan decision (RESEARCH_NOTES §2).
- If a downstream chunk reveals an upstream defect: halt, document in `IMPEDIMENTS.md`, fix upstream, re-verify upstream before resuming.
- Stub removal is Phase 4 work only; no chunk removes another chunk's scaffolding.

---

Phase 2 complete. Integration contract ready. Waiting for approval to proceed to Phase 3 (chunk execution).
