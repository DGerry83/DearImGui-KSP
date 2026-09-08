// DearImGuiKSPNative — Core layer entry surface.
//
// Exposes the version handshake, the backend-selection exports, and the
// render-event callback routed into the active renderer backend
// (C4: D3D11; original-plan C5: OpenGL, landed per D37) (spec §4.1/§4.2).
//
// UnityPluginLoad/Unload are kept for completeness but are effectively dead in
// our deployment: Unity only calls them for plugins IT loads at startup, and
// we are LoadLibrary'd from managed code out of GameData/DearImGuiKSP/PluginData
// (D19). That is why the graphics device arrives via a Unity-created texture
// (DearImGuiKSPNative_SetD3D11DeviceTexture) instead of IUnityInterfaces, and why
// device-kind detection lives managed-side (SystemInfo.graphicsDeviceType).

#include <windows.h>

#include "IUnityInterface.h"
#include "IUnityGraphics.h" // UnityRenderingEvent only — the interface itself is unavailable to a LoadLibrary'd plugin

#include "BackendD3D11.h"
#include "BackendOpenGL.h"
#include "ContextHost.h"
#include "imgui.h" // ImDrawList (DearImGuiKSPNative_GetDrawListVtxCount wrapper)

#define DEARIMGUIKSP_NATIVE_API extern "C" __declspec(dllexport)

// Renderer backend selected for this session: set by the matching managed-side
// selection call (DearImGuiKSPNative_SetD3D11DeviceTexture /
// DearImGuiKSPNative_SetOpenGLBackend), read by the render-event routing.
// None preserves the pre-selection behavior: routing falls through to
// BackendD3D11_Render, which no-ops safely until its device arrives.
enum class ActiveBackend { None, D3D11, OpenGL };
static ActiveBackend s_ActiveBackend = ActiveBackend::None;

// Managed/native version handshake (spec §5.4, D17). Bump in lockstep with the
// managed ExpectedNativeVersion constant; mismatch -> Failed state.
// 4: ISSUES #001-#003 fixed; 5: C5 font load (LoadFontFromFile); 6: C31 SetUiScale;
// 7: C04 native diagnostics channel (DrainDiagnostics export, G2-04/G3-03);
// 8: OpenGL backend (SetOpenGLBackend export; original-plan C5, D37);
// 9: ISSUES #011 docking (SetDockingEnabled export; docking defaults ON).
DEARIMGUIKSP_NATIVE_API int DearImGuiKSPNative_GetVersion()
{
    return 9;
}

// Native diagnostics drain (C04, G2-04/G3-03): the ImGui error callback and
// D3D11 backend bring-up failures accumulate in a fixed native buffer; the
// managed bridge polls once per frame after EndUiFrame (query with a null
// dst, then drain into a reused buffer) and writes any lines to KSP.log.
// Returns the bytes pending before the call; 0 = nothing pending.
DEARIMGUIKSP_NATIVE_API int DearImGuiKSPNative_DrainDiagnostics(char* dst, int dstCapacity)
{
    return ContextHost_DrainDiagnostics(dst, dstCapacity);
}

// Loads a font file into the atlas before the first frame (spec §4.2). The
// managed font pipeline calls this once (Regular); each call appends one
// font. Returns 0 on success, 1 = no context,
// 2 = frames already begun, 3 = font load failed (embedded default intact).
DEARIMGUIKSP_NATIVE_API int DearImGuiKSPNative_LoadFontFromFile(const char* utf8Path, float sizePixels)
{
    return ContextHost_LoadFontFromFile(utf8Path, sizePixels);
}

// Clamps all visible ImGui windows into the viewport of the passed size after
// a resolution change (ISSUES #002); the setting gate and the size-change
// detection live managed-side (frame loop). No-op before ContextInit.
DEARIMGUIKSP_NATIVE_API void DearImGuiKSPNative_ClampWindowsToViewport(float width, float height)
{
    ContextHost_ClampWindowsToViewport(width, height);
}

// Theme style setters (chunk C8, spec §5.1). Thin pass-throughs over the
// context host; the "dark" preset's exactness comes from
// DearImGuiKSPNative_StyleColorsDark being the stock ImGui::StyleColorsDark,
// never a hand-copied color table. Return codes: 0 ok, 1 no context, 2 bad idx.
DEARIMGUIKSP_NATIVE_API int DearImGuiKSPNative_SetStyleColor(int idx, float r, float g, float b, float a)
{
    return ContextHost_SetStyleColor(idx, r, g, b, a);
}

DEARIMGUIKSP_NATIVE_API int DearImGuiKSPNative_SetStyleVarFloat(int idx, float v)
{
    return ContextHost_SetStyleVarFloat(idx, v);
}

DEARIMGUIKSP_NATIVE_API int DearImGuiKSPNative_SetStyleVarVec2(int idx, float x, float y)
{
    return ContextHost_SetStyleVarVec2(idx, x, y);
}

DEARIMGUIKSP_NATIVE_API int DearImGuiKSPNative_StyleColorsDark(void)
{
    return ContextHost_StyleColorsDark();
}

// Live UI scale (chunk C31): scales all style sizes by 'scale' (legal only
// right after a whole-style reset — see ContextHost_SetUiScale) and sets
// io.FontGlobalScale absolutely. Returns 0 on success, 1 = no context,
// 2 = non-positive scale (nothing written).
DEARIMGUIKSP_NATIVE_API int DearImGuiKSPNative_SetUiScale(float scale)
{
    return ContextHost_SetUiScale(scale);
}

// Live docking enable/disable (ISSUES #011): sets or clears
// ImGuiConfigFlags_DockingEnable on the live context (docking defaults ON from
// ContextInit). Called by the managed DockingModeApplier only when the persisted
// setting changes. Returns 0 on success, 1 = no context.
DEARIMGUIKSP_NATIVE_API int DearImGuiKSPNative_SetDockingEnabled(int enabled)
{
    return ContextHost_SetDockingEnabled(enabled);
}

// Window-background gradient descriptor (chunk C9, spec §6.1). enabled != 0
// turns on the per-frame EndFrame shading pass over every visible window's
// background fill; the two RGBA float pairs (0–1) are the top/bottom stops.
// enabled == 0 disables the pass, which is then a strict no-op so the "dark"
// preset renders byte-exact stock. Returns 0 on success, 1 = no context.
DEARIMGUIKSP_NATIVE_API int DearImGuiKSPNative_SetWindowBgGradient(int enabled, float r1, float g1, float b1, float a1, float r2, float g2, float b2, float a2)
{
    return ContextHost_SetWindowBgGradient(enabled, r1, g1, b1, a1, r2, g2, b2, a2);
}

// ImDrawList vertex count (chunk C9): cimgui exports no VtxBuffer accessor, so
// the managed gradient helpers record before/after counts through this
// pass-through to shade exactly the verts a fill appended. Returns -1 for a
// null draw list.
DEARIMGUIKSP_NATIVE_API int DearImGuiKSPNative_GetDrawListVtxCount(ImDrawList* drawList)
{
    return ContextHost_GetDrawListVtxCount(drawList);
}

// Hands the D3D11 backend a Unity-created ID3D11Texture2D
// (Texture.GetNativeTexturePtr() managed-side) so it can capture the device.
// Also selects D3D11 as the active backend. Returns 0 on success; see
// BackendD3D11.h for error codes, plus 4 when OpenGL was already selected.
DEARIMGUIKSP_NATIVE_API int DearImGuiKSPNative_SetD3D11DeviceTexture(void* d3d11TexturePtr)
{
    if (s_ActiveBackend == ActiveBackend::OpenGL)
        return 4; // conflicting backend selection — one backend per process
    int rc = BackendD3D11_InitFromTexture(d3d11TexturePtr);
    if (rc == 0)
        s_ActiveBackend = ActiveBackend::D3D11;
    return rc;
}

// Selects the OpenGL backend for this session (original-plan C5, D37): the
// managed bridge calls this instead of SetD3D11DeviceTexture when SystemInfo
// reports OpenGLCore. GL needs no device discovery — the context is current at
// render-event time. Returns 0 on selection (idempotent), 1 when D3D11 was
// already selected (conflicting backend selection).
DEARIMGUIKSP_NATIVE_API int DearImGuiKSPNative_SetOpenGLBackend()
{
    if (s_ActiveBackend == ActiveBackend::D3D11)
        return 1;
    s_ActiveBackend = ActiveBackend::OpenGL;
    return 0;
}

// Render-event callback handed to Unity via GL.IssuePluginEvent /
// CommandBuffer.IssuePluginEvent. Event 0 is the per-frame "render now"
// signal; it routes to the active backend (C4: D3D11; C5: OpenGL) — defaulting
// to the D3D11 backend before selection, preserving its safe no-op behavior
// (spec §4.2).
static void UNITY_INTERFACE_API OnRenderEvent(int eventID)
{
    if (eventID != 0)
        return;
    if (s_ActiveBackend == ActiveBackend::OpenGL)
        BackendOpenGL_Render();
    else
        BackendD3D11_Render();
}

// Managed side calls this once to obtain the callback pointer (spec §4.2).
// Valid regardless of who loaded the DLL — IssuePluginEvent only needs the
// function pointer.
DEARIMGUIKSP_NATIVE_API UnityRenderingEvent DearImGuiKSPNative_GetRenderEventFunc()
{
    return &OnRenderEvent;
}

// Unity low-level native plugin entry points. In our deployment these are
// never called (see header comment); they remain so the DLL also works if it
// is ever placed in KSP_x64_Data/Plugins for Unity to load directly.
static IUnityInterfaces* s_UnityInterfaces = nullptr; // always null in practice

DEARIMGUIKSP_NATIVE_API void UNITY_INTERFACE_API UnityPluginLoad(IUnityInterfaces* unityInterfaces)
{
    s_UnityInterfaces = unityInterfaces;
}

DEARIMGUIKSP_NATIVE_API void UNITY_INTERFACE_API UnityPluginUnload()
{
    BackendD3D11_Shutdown(); // releases backend + device pointers (C4)
    BackendOpenGL_Shutdown(); // releases backend GL objects (C5)
    s_UnityInterfaces = nullptr;
}

BOOL APIENTRY DllMain(HMODULE module, DWORD reason, LPVOID reserved)
{
    (void)module; (void)reason; (void)reserved;
    return TRUE;
}
