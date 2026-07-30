// DearKSPNative — Core layer entry surface.
//
// Owns (in later milestones): the single ImGui context, frame lifecycle, and
// draw-data -> GPU translation via D3D11/OpenGL backends (spec §4.1).
// This file currently exposes the Unity low-level plugin export surface,
// graphics-device detection (milestone 2), and the version-handshake
// placeholder so the build pipeline produces a loadable DLL.
//
// TODO(C4/C5): register the render-event callback with real work and
// create device objects for the selected backend.

#include <windows.h>

#include "IUnityInterface.h"
#include "IUnityGraphics.h"

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
// CommandBuffer.IssuePluginEvent. No-op for now; actual rendering arrives in
// C4 (D3D11) and C5 (OpenGL) (spec §4.2).
static void UNITY_INTERFACE_API OnRenderEvent(int eventID)
{
    (void)eventID; // TODO(C4/C5): translate event ID into backend render work
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
    // TODO(C4/C5): register device-event callback, select backend
}

DEARKSP_NATIVE_API void UNITY_INTERFACE_API UnityPluginUnload()
{
    // TODO(C4/C5): release context, device objects, font atlas
    s_UnityGraphics   = nullptr;
    s_UnityInterfaces = nullptr;
}

BOOL APIENTRY DllMain(HMODULE module, DWORD reason, LPVOID reserved)
{
    (void)module; (void)reason; (void)reserved;
    return TRUE;
}
