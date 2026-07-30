// DearKSPNative — D3D11 renderer backend interface (chunk C4, milestone M2 gate AC1).
//
// Initializes imgui_impl_dx11 from Unity's graphics device and renders the
// frame's ImGui draw data inside the render-event callback (spec §4.1/§4.2).
// Zero game knowledge beyond the Unity low-level plugin API; the ImGui
// context itself stays owned by ContextHost (C3 ABI).
#pragma once

#include "IUnityInterface.h"

// Called from UnityPluginLoad: registers the graphics device-event callback
// and attempts an immediate device capture (Unity may have already fired
// kUnityGfxDeviceEventInitialize before the plugin finished loading).
void BackendD3D11_OnPluginLoad(IUnityInterfaces* unityInterfaces);

// Called from UnityPluginUnload and on kUnityGfxDeviceEventShutdown:
// releases the backend (ImGui_ImplDX11_Shutdown) and the captured device
// pointers. Safe to call when nothing was initialized.
void BackendD3D11_Shutdown(void);

// Called from the render-event callback (render thread). Completes backend
// init lazily once both the device and the ImGui context exist, then renders
// ImGui::GetDrawData() into the render target Unity has bound at event time.
void BackendD3D11_Render(void);
