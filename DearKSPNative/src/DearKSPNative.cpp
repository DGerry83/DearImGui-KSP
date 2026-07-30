// DearKSPNative — Core layer entry surface (skeleton).
//
// Owns (in later milestones): the single ImGui context, frame lifecycle, and
// draw-data -> GPU translation via D3D11/OpenGL backends (spec §4.1).
// This file currently exposes only the Unity low-level plugin export surface
// and the version-handshake placeholder so the build pipeline (milestone 1)
// produces a loadable DLL.
//
// TODO(milestone 2): UnityPluginLoad must grab IUnityInterfaces, register the
// render-event callback, and select the backend from the actual graphics device.

#include <windows.h>

#define DEARKSP_NATIVE_API extern "C" __declspec(dllexport)

// Managed/native version handshake (spec §5.4, D17). Bump in lockstep with the
// managed [assembly: KSPAssembly] version; mismatch -> Failed state.
DEARKSP_NATIVE_API int DearKSPNative_GetVersion()
{
    return 1; // handshake placeholder, not the release version
}

// Unity low-level native plugin entry points.
DEARKSP_NATIVE_API void UnityPluginLoad(void* unityInterfaces)
{
    (void)unityInterfaces; // TODO(milestone 2): store IUnityInterfaces, get IUnityGraphics
}

DEARKSP_NATIVE_API void UnityPluginUnload()
{
    // TODO(milestone 2): release context, device objects, font atlas
}

BOOL APIENTRY DllMain(HMODULE module, DWORD reason, LPVOID reserved)
{
    (void)module; (void)reason; (void)reserved;
    return TRUE;
}
