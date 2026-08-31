// DearImGuiKSPNative — D3D11 renderer backend (chunk C4, milestone M2 gate AC1).
//
// imgui_impl_dx11 renders into the currently bound render target and
// saves/restores the full D3D11 pipeline state around the draw (verified in
// imgui 1.92.9 backends/imgui_impl_dx11.cpp) — the basis for coexistence
// with Deferred/TUFX (D16, spec §4.2).
//
// Device discovery note: Unity only calls UnityPluginLoad for plugins IT
// loads; we are LoadLibrary'd from managed code (D19 layout), so the device
// is captured from a Unity-created texture instead (see header).

#include "BackendD3D11.h"

#include <d3d11.h>

#include "imgui.h"
#include "imgui_impl_dx11.h"

// Device pointers captured in BackendD3D11_InitFromTexture. GetDevice and
// GetImmediateContext both AddRef; the extra context reference is released
// once ImGui_ImplDX11_Init has taken its own, the device reference is held
// until BackendD3D11_Shutdown.
static ID3D11Device*        s_Device        = nullptr;
static ID3D11DeviceContext* s_DeviceContext = nullptr;

// True once ImGui_ImplDX11_Init + ImGui_ImplDX11_CreateDeviceObjects succeeded.
static bool s_BackendUp = false;

int BackendD3D11_InitFromTexture(void* d3d11TexturePtr)
{
    if (s_Device != nullptr)
        return 0; // already captured
    if (d3d11TexturePtr == nullptr)
        return 1;

    ID3D11Texture2D* texture = (ID3D11Texture2D*)d3d11TexturePtr;
    texture->GetDevice(&s_Device); // AddRef'd; released in Shutdown
    if (s_Device == nullptr)
        return 2;

    s_Device->GetImmediateContext(&s_DeviceContext); // AddRef'd
    if (s_DeviceContext == nullptr)
    {
        s_Device->Release();
        s_Device = nullptr;
        return 3;
    }
    return 0;
}

// Full backend bring-up. Needs the device pointers AND a live ImGui context
// (ImGui_ImplDX11_Init touches ImGui::GetIO()), and the managed bridge passes
// the texture before DearImGuiKSPNative_ContextInit runs — so init completes lazily
// here on the first render event where both halves exist. We deliberately do
// NOT call the backend's NewFrame — ContextHost owns ImGui::NewFrame (C3 ABI)
// — so device objects are created explicitly via
// ImGui_ImplDX11_CreateDeviceObjects(). In imgui 1.92.9 that covers
// shaders/states/samplers; the font texture is created from
// draw_data->Textures on the first RenderDrawData (RendererHasTextures).
static void TryInitBackend()
{
    if (s_BackendUp || s_Device == nullptr || s_DeviceContext == nullptr)
        return;
    if (ImGui::GetCurrentContext() == nullptr)
        return; // context not up yet; retry on the next render event

    if (!ImGui_ImplDX11_Init(s_Device, s_DeviceContext))
        return;
    if (!ImGui_ImplDX11_CreateDeviceObjects())
    {
        ImGui_ImplDX11_Shutdown();
        return;
    }
    s_DeviceContext->Release(); // backend AddRef'd its own; drop our capture ref
    s_DeviceContext = nullptr;  // non-owning marker no longer needed
    s_BackendUp = true;
}

void BackendD3D11_Shutdown(void)
{
    // ImGui_ImplDX11_Shutdown asserts when no backend/context is up; guard both.
    if (s_BackendUp && ImGui::GetCurrentContext() != nullptr)
        ImGui_ImplDX11_Shutdown();
    s_BackendUp = false;

    if (s_DeviceContext != nullptr)
    {
        s_DeviceContext->Release(); // still owned only if backend init never ran
        s_DeviceContext = nullptr;
    }
    if (s_Device != nullptr)
    {
        s_Device->Release();
        s_Device = nullptr;
    }
}

void BackendD3D11_Render(void)
{
    TryInitBackend();
    if (!s_BackendUp || ImGui::GetCurrentContext() == nullptr)
        return;

    // Draw-data lifetime caveat: the draw data is produced by ContextHost on
    // the game thread (BeginFrame/EndFrame) and consumed here on the render
    // thread. ImGui guarantees it stays valid until the next ImGui::NewFrame,
    // which is adequate for the PoC; if tearing ever shows up, double-
    // buffering the draw data is later hardening work, not a C4 blocker.
    ImDrawData* drawData = ImGui::GetDrawData();
    if (drawData != nullptr)
        ImGui_ImplDX11_RenderDrawData(drawData);
}
