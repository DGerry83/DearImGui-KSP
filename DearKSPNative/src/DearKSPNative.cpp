// DearKSPNative — Core layer entry surface.
//
// Exposes the Unity low-level plugin export surface, graphics-device
// detection (milestone 2), and the version handshake, and routes plugin
// load/unload and the render-event callback into the active renderer
// backend (C4: D3D11; C5 adds OpenGL) (spec §4.1/§4.2).

#include <windows.h>

#include "IUnityInterface.h"
#include "IUnityGraphics.h"

#include "BackendD3D11.h"

#define DEARKSP_NATIVE_API extern "C" __declspec(dllexport)

// Managed/native version handshake (spec §5.4, D17). Bump in lockstep with the
// managed [assembly: KSPAssembly] version; mismatch -> Failed state.
DEARKSP_NATIVE_API int DearKSPNative_GetVersion()
{
    return 1; // handshake placeholder, not the release version
}

// Unity interface registry and graphics interface, captured in UnityPluginLoad.
// Null until the plugin is loaded; cleared again in UnityPluginUnload.
static IUnityInterfaces* s_UnityInterfaces = nullptr;
static IUnityGraphics*   s_UnityGraphics   = nullptr;

// Graphics device detection (milestone 2). Returns the UnityGfxRenderer enum
// value (spec §4.1: D3D11 primary, OpenGL secondary), or -1 when the graphics
// interface is unavailable (plugin not loaded, or headless startup).
DEARKSP_NATIVE_API int DearKSPNative_GetGraphicsDeviceKind()
{
    if (s_UnityGraphics == nullptr)
        return -1;
    return (int)s_UnityGraphics->GetRenderer();
}

// Render-event callback handed to Unity via GL.IssuePluginEvent /
// CommandBuffer.IssuePluginEvent. Event 0 is the per-frame "render now"
// signal; it routes to the D3D11 backend (C4). Further event IDs arrive
// with C5+ (spec §4.2).
static void UNITY_INTERFACE_API OnRenderEvent(int eventID)
{
    if (eventID == 0)
        BackendD3D11_Render();
}

// Managed side calls this once to obtain the callback pointer (spec §4.2).
DEARKSP_NATIVE_API UnityRenderingEvent DearKSPNative_GetRenderEventFunc()
{
    return &OnRenderEvent;
}

// Unity low-level native plugin entry points.
DEARKSP_NATIVE_API void UNITY_INTERFACE_API UnityPluginLoad(IUnityInterfaces* unityInterfaces)
{
    s_UnityInterfaces = unityInterfaces;
    s_UnityGraphics   = s_UnityInterfaces->Get<IUnityGraphics>();
    BackendD3D11_OnPluginLoad(unityInterfaces); // registers device-event callback, captures device (C4)
}

DEARKSP_NATIVE_API void UNITY_INTERFACE_API UnityPluginUnload()
{
    BackendD3D11_Shutdown(); // releases backend + device pointers (C4)
    s_UnityGraphics   = nullptr;
    s_UnityInterfaces = nullptr;
}

BOOL APIENTRY DllMain(HMODULE module, DWORD reason, LPVOID reserved)
{
    (void)module; (void)reason; (void)reserved;
    return TRUE;
}
