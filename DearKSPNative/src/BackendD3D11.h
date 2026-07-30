// DearKSPNative — D3D11 renderer backend interface (chunk C4, milestone M2 gate AC1).
//
// Initializes imgui_impl_dx11 from Unity's graphics device and renders the
// frame's ImGui draw data inside the render-event callback (spec §4.1/§4.2).
// Zero game knowledge; the ImGui context itself stays owned by ContextHost (C3 ABI).
//
// Device discovery: this DLL is loaded via LoadLibrary from managed code, so
// Unity never calls UnityPluginLoad and IUnityInterfaces is unavailable. The
// device is therefore taken from a Unity-created D3D11 texture passed in by
// the managed bridge (texture->GetDevice) — the same pattern
// CinematicRecorderNative uses in this game.
#pragma once

// Captures the D3D11 device + immediate context from a Unity-created
// ID3D11Texture2D (Texture.GetNativeTexturePtr() managed-side).
// Returns 0 on success (or already captured), 1 on null pointer,
// 2 when GetDevice fails, 3 when GetImmediateContext fails.
int BackendD3D11_InitFromTexture(void* d3d11TexturePtr);

// Releases the backend (ImGui_ImplDX11_Shutdown) and the captured device
// pointers. Safe to call when nothing was initialized.
void BackendD3D11_Shutdown(void);

// Called from the render-event callback (render thread). Completes backend
// init lazily once both the device and the ImGui context exist, then renders
// ImGui::GetDrawData() into the render target Unity has bound at event time.
void BackendD3D11_Render(void);
