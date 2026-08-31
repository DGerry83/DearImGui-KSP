// DearImGuiKSPNative — Core layer entry surface.
//
// Exposes the version handshake, the D3D11 device handoff, and the render-event
// callback routed into the active renderer backend (C4: D3D11; C5 adds OpenGL)
// (spec §4.1/§4.2).
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

#define DEARIMGUIKSP_NATIVE_API extern "C" __declspec(dllexport)

// Managed/native version handshake (spec §5.4, D17). Bump in lockstep with the
// managed [assembly: KSPAssembly] version; mismatch -> Failed state.
DEARIMGUIKSP_NATIVE_API int DearImGuiKSPNative_GetVersion()
{
    return 1; // handshake constant for the 0.1.x line
}

// Hands the D3D11 backend a Unity-created ID3D11Texture2D
// (Texture.GetNativeTexturePtr() managed-side) so it can capture the device.
// Returns 0 on success; see BackendD3D11.h for error codes.
DEARIMGUIKSP_NATIVE_API int DearImGuiKSPNative_SetD3D11DeviceTexture(void* d3d11TexturePtr)
{
    return BackendD3D11_InitFromTexture(d3d11TexturePtr);
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
    s_UnityInterfaces = nullptr;
}

BOOL APIENTRY DllMain(HMODULE module, DWORD reason, LPVOID reserved)
{
    (void)module; (void)reason; (void)reserved;
    return TRUE;
}
