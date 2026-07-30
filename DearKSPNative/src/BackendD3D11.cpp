// DearKSPNative — D3D11 renderer backend (chunk C4, milestone M2 gate AC1).
//
// imgui_impl_dx11 renders into the currently bound render target and
// saves/restores the full D3D11 pipeline state around the draw (verified in
// imgui 1.92.9 backends/imgui_impl_dx11.cpp) — the basis for coexistence
// with Deferred/TUFX (D16, spec §4.2).

#include "BackendD3D11.h"

#include <d3d11.h>

#include "imgui.h"
#include "imgui_impl_dx11.h"

#include "IUnityGraphics.h"
#include "IUnityGraphicsD3D11.h"

// Interface registry captured at plugin load; used to fetch IUnityGraphics
// and IUnityGraphicsD3D11 when the device-event callback fires.
static IUnityInterfaces* s_UnityInterfaces = nullptr;

// Device pointers captured on kUnityGfxDeviceEventInitialize (render thread).
// IUnityGraphicsD3D11 exposes GetDevice() only, so the immediate context
// comes from ID3D11Device::GetImmediateContext — that call AddRefs, and the
// extra reference is released again once ImGui_ImplDX11_Init has taken its
// own (s_DeviceContext then becomes a non-owning presence marker).
static ID3D11Device*        s_Device        = nullptr;
static ID3D11DeviceContext* s_DeviceContext = nullptr;

// True once ImGui_ImplDX11_Init + ImGui_ImplDX11_CreateDeviceObjects succeeded.
static bool s_BackendUp = false;

// Full backend bring-up. Needs the device pointers AND a live ImGui context
// (ImGui_ImplDX11_Init touches ImGui::GetIO()), so it cannot run at
// device-event time: the device initializes during plugin load, long before
// managed code calls DearKSPNative_ContextInit. The device event only
// captures pointers; Render() retries here on every event until both halves
// exist. We deliberately do NOT call the backend's NewFrame — ContextHost
// owns ImGui::NewFrame (C3 ABI) — so device objects are created explicitly
// via ImGui_ImplDX11_CreateDeviceObjects(). In imgui 1.92.9 that covers
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
    s_BackendUp = true;
}

// Captures the D3D11 device pointers. No-op unless Unity reports the D3D11
// renderer — the PoC is D3D11-only; GL arrives in C5 (spec §4.1).
static void CaptureDevice()
{
    if (s_Device != nullptr || s_UnityInterfaces == nullptr)
        return;

    IUnityGraphics* graphics = s_UnityInterfaces->Get<IUnityGraphics>();
    if (graphics == nullptr || graphics->GetRenderer() != kUnityGfxRendererD3D11)
        return;

    IUnityGraphicsD3D11* d3d = s_UnityInterfaces->Get<IUnityGraphicsD3D11>();
    if (d3d == nullptr)
        return;

    s_Device = d3d->GetDevice();
    if (s_Device != nullptr)
        s_Device->GetImmediateContext(&s_DeviceContext);
}

static void UNITY_INTERFACE_API OnGraphicsDeviceEvent(UnityGfxDeviceEventType eventType)
{
    switch (eventType)
    {
    case kUnityGfxDeviceEventInitialize:
        CaptureDevice();
        break;
    case kUnityGfxDeviceEventShutdown:
        BackendD3D11_Shutdown();
        break;
    default:
        break; // BeforeReset/AfterReset: viewport rebuild is C12 work
    }
}

void BackendD3D11_OnPluginLoad(IUnityInterfaces* unityInterfaces)
{
    s_UnityInterfaces = unityInterfaces;

    IUnityGraphics* graphics = s_UnityInterfaces->Get<IUnityGraphics>();
    if (graphics == nullptr)
        return;
    graphics->RegisterDeviceEventCallback(&OnGraphicsDeviceEvent);

    // The Initialize event is missed when the plugin loads after device
    // creation (IUnityGraphics.h), so attempt the capture immediately too.
    CaptureDevice();
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
    s_Device = nullptr;
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
