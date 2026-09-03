# Chunk Contract: D3D11 Backend + Managed Bridge PoC

## Plan: DearImGui-KSP Implementation
## Date: 2026-07-29
## Chunk ID: C4
## Advances Milestone: M2 — **this is the kill-or-continue gate (AC1)**

### Scope

- **Native**: a D3D11 backend that initializes from Unity's graphics device and renders ImGui draw data inside the render-event callback.
- **Managed**: a real `NativeBridge` — explicit DLL load from `PluginData/`, version handshake, device-kind gate, and the per-frame `BeginFrame → EndFrame → GL.IssuePluginEvent` pump, driven by the addon.
- **PoC behavior**: the ImGui demo window renders in-game on D3D11 at full framerate, with Deferred + TUFX active. Input interaction is NOT in scope (M4); visual verification only.

### Inputs (must exist before starting)

- C2: Unity headers, `GetGraphicsDeviceKind`, `GetRenderEventFunc` export surface.
- C3: context host C ABI (locked), ProggyClean atlas built CPU-side, harness.
- cimgui clone with `imgui/backends/imgui_impl_dx11.{h,cpp}` at `~\source\repos\cimgui`.
- `IUnityGraphicsD3D11.h` at `~\source\repos\nativerenderingplugin\PluginSource\source\Unity\`.

### Outputs (must be created/changed)

- New: `DearImGuiKSPNative/include/IUnityGraphicsD3D11.h` (verbatim copy).
- New: `DearImGuiKSPNative/src/BackendD3D11.h/.cpp` — device-event handling, backend init/shutdown, render-event rendering.
- Modified: `DearImGuiKSPNative/src/DearImGuiKSPNative.cpp` — wire backend into plugin load/unload and the render-event callback.
- Modified: `DearImGuiKSPNative/build.bat`, `build_release.bat` — add `imgui_impl_dx11.cpp` + `BackendD3D11.cpp`, link `d3d11.lib dxgi.lib`.
- Modified: `DearImGuiKSP/Application/Interfaces/INativeBridge.cs` — the locked method set (below).
- Modified: `DearImGuiKSP/Infrastructure/NativeBridge.cs` — real implementation.
- Modified: `DearImGuiKSP/Infrastructure/Composition.cs`, `DearImGuiKSP/Infrastructure/DearImGuiKSPAddon.cs` — wire the frame pump.

### Locked INativeBridge method set (C6+ consume this)

```
Initialize() -> int result      // LoadLibrary, handshake, device gate, ContextInit; 0 = success
GetIoSnapshot() -> capture info // C9 fills in; PoC returns a default/stub (tracked)
SubmitFrame()                   // BeginFrame + EndFrame + IssuePluginEvent for the frame
RebuildViewport(w, h)           // C12 fills in; PoC may no-op (tracked)
Shutdown()                      // ContextShutdown + FreeLibrary
```

### Design notes (validated against imgui 1.92.9 — still verify in source)

- `imgui_impl_dx11` renders into the **currently bound render target**; it saves/restores D3D11 pipeline state internally — this is what makes coexistence with Deferred/TUFX plausible. Confirm in `backends/imgui_impl_dx11.cpp` before coding.
- Device objects (incl. font texture) are created lazily in `ImGui_ImplDX11_NewFrame`, which we do NOT call (ContextHost owns `ImGui::NewFrame`); the backend must call `ImGui_ImplDX11_CreateDeviceObjects()` explicitly after init.
- Get the device via `IUnityGraphicsD3D11::GetDevice()` and the immediate context via `GetImmediateContext()`. Register a device-event callback (`IUnityGraphics::RegisterDeviceEventCallback`) so backend init/shutdown follows `kUnityGfxDeviceEventInitialize/Shutdown`.
- Draw data is produced on the game thread and consumed on the render thread; it stays valid until the next `ImGui::NewFrame`. Acceptable for the PoC — document the caveat in a comment; hardening (if needed) is later work.
- Managed loads the DLL with `SetDllDirectory(<KSP root>/GameData/DearImGuiKSP/PluginData)` + `LoadLibrary("DearImGuiKSPNative.dll")`, then `GetProcAddress` + `Marshal.GetDelegateForFunctionPointer` for every native function. No implicit `[DllImport]` (documented GameData load-path gotcha).
- Handshake: managed expects `DearImGuiKSPNative_GetVersion() == 1` for the 0.1.x line; mismatch → error log, no init (popup arrives in C13).
- Device gate: `DearImGuiKSPNative_GetGraphicsDeviceKind()` must equal `kUnityGfxRendererD3D11` (2); anything else → log and no init for this PoC (GL arrives in C5).
- Frame pump in the addon: `Update()` → `bridge.SubmitFrame()`; a coroutine on `WaitForEndOfFrame` → `GL.IssuePluginEvent(bridge.RenderEventFunc, 0)`. Enabling the demo window via the bridge is PoC scaffolding — mark it clearly for removal in C7.

### Constraints

- Invariants per PLAN_DIGEST: Application stays Unity-free (bridge interface uses no Unity types); native Core has no game knowledge beyond the Unity plugin API.
- No input handling, no settings, no state machine — later chunks.
- Keep 0-warning builds; keep harness working.

### Verification

- `build.bat` 0 errors/0 warnings; `build_harness.bat` + harness still PASS; `dotnet build` clean.
- Deploy to ReformTestInstance (both build steps), user launches KSP:
  - **AC1**: ImGui demo window renders at the main menu, animates at full framerate, game otherwise normal, Deferred + TUFX active.
  - KSP.log shows `[DearImGuiKSP]` init lines and no exceptions.

### Rollback

- `git checkout -- DearImGuiKSPNative/ DearImGuiKSP/` and delete untracked files under both. **If the PoC itself fails in-game: stop the entire sequence, do not patch around it — report to the user for the uGUI-fallback decision (INTEGRATION_CONTRACT rollback plan).**
