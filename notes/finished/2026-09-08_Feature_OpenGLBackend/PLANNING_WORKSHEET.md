# Planning Worksheet: OpenGL Render Backend
## Date: 2026-09-08
## Type: Feature
## Related Investigation: [INVESTIGATION_LOG.md](INVESTIGATION_LOG.md)

### Step 1 — Core Entities and State
| Entity | Identity | Attributes | Relationships | Lifetime |
|--------|----------|------------|---------------|----------|
| Active backend (native) | process-singleton enum { None, D3D11, OpenGL } | set once by the backend-selection export(s) | read by OnRenderEvent routing | session |
| GL backend state (native) | statics in BackendOpenGL.cpp | backend-up flag, failure latch | consumes ContextHost (context, frame lock, diagnostics) | session (init→shutdown) |
| Device kind (managed) | `SystemInfo.graphicsDeviceType` read once at init | D3D11 / OpenGLCore accepted; else Failed | selects native backend-selection export + texture handoff | init-time only |

### Step 2 — Behaviors and Responsibilities
| Component | Single-Sentence Responsibility | Command / Query / Both |
|-----------|-------------------------------|------------------------|
| `BackendOpenGL` (new, native) | Renders ImGui draw data through imgui_impl_opengl3 inside Unity's GL render pass | Command |
| `BackendD3D11` (existing) | Renders ImGui draw data through imgui_impl_dx11 | Command (unchanged) |
| `OnRenderEvent` routing (native entry) | Dispatches the render event to the selected backend | Command |
| `NativeBridge` device gate (managed) | Selects the native backend matching the runtime graphics API or fails with GraphicsApi | Command (init-time) |
| `ContextHost` (existing) | Owns the ImGui context, frame lock, diagnostics | Both (unchanged) |

### Step 3 — Data Flow
```
[Unity render thread: GL.IssuePluginEvent(0)]
   --(render event)--> OnRenderEvent
   --(active backend)--> BackendOpenGL_Render
   --(ImDrawData*)----> ImGui_ImplOpenGL3_RenderDrawData  [inside ContextHost frame lock]
[Managed init] --(GraphicsDeviceType)--> device gate
   D3D11     --> SetD3D11DeviceTexture(texture)  [existing]
   OpenGLCore--> SetOpenGLBackend()              [new export]
   other     --> Failed(GraphicsApi) + one popup [existing path, updated wording]
```

### Step 4 — Boundaries and Interfaces
| Interface | Defined In | Implemented By | Consumed By | Purpose |
|-----------|------------|----------------|-------------|---------|
| `BackendOpenGL_Init/Render/Shutdown` | `src/BackendOpenGL.h` (native Core) | `src/BackendOpenGL.cpp` | `DearImGuiKSPNative.cpp` entry surface | GL backend lifecycle, mirrors BackendD3D11.h |
| `DearImGuiKSPNative_SetOpenGLBackend` (new export) | `src/DearImGuiKSPNative.cpp` | entry surface → BackendOpenGL | managed `NativeBridge` | Explicit backend selection (handshake v8) |
| `INativeBridge` (managed) | `Application/Interfaces/` | `Infrastructure/NativeBridge.cs` | Application frame loop | Unchanged public surface |

### Layering Check
- [x] Core (native) imports nothing from managed layers; zero KSP/Unity knowledge preserved (GL context arrives via the render-event calling convention, not Unity headers).
- [x] Application imports from Core only (no managed Application change at all — the gate lives in Infrastructure).
- [x] Infrastructure imports from Core and Application (NativeBridge only).
