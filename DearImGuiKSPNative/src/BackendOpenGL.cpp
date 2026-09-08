// DearImGuiKSPNative — OpenGL renderer backend (original-plan C5, deferred per
// D20/D35 pending a clean GL test environment; D37 queue item 1).
//
// imgui_impl_opengl3 renders into the currently bound render target and
// saves/restores GL state around the draw ("saving/setting up/restoring every
// OpenGL state explicitly ... to run within an OpenGL engine that doesn't do
// so" — imgui_impl_opengl3.cpp, pinned imgui 1.92.9) — the basis for
// coexistence with the GL instance's render mods (gate G7). Its UpdateTexture
// additionally restores GL_UNPACK_ROW_LENGTH/GL_UNPACK_ALIGNMENT.
//
// No device discovery: this DLL is LoadLibrary'd from managed code (D19), but
// GL needs no device — Unity's GL context is current when the render event
// fires on the render thread.

#include "BackendOpenGL.h"

#include "ContextHost.h" // ContextHost_LockFrame/UnlockFrame (ISSUES #004)
#include "imgui.h"
#include "imgui_impl_opengl3.h"

// True once ImGui_ImplOpenGL3_Init + ImGui_ImplOpenGL3_CreateDeviceObjects succeeded.
static bool s_BackendUp = false;

// Latched bring-up failure, same contract as the D3D11 backend's G3-03 latch:
// one failure = one diagnostic (drained to KSP.log via the C04 channel) + no
// retry — a failed CreateDeviceObjects would otherwise flap the
// RendererHasTextures backend flag on every render event. Clears in
// BackendOpenGL_Shutdown.
static bool s_BackendFailed = false;

// Full backend bring-up. Needs a live ImGui context (ImGui_ImplOpenGL3_Init
// touches ImGui::GetIO()) and a current GL context — both hold at render-event
// time, so init completes lazily here on the first render event after
// ContextInit. We deliberately do NOT call the backend's NewFrame —
// ContextHost owns ImGui::NewFrame (C3 ABI) — so device objects are created
// explicitly via ImGui_ImplOpenGL3_CreateDeviceObjects(). In imgui 1.92.9 that
// covers shaders/buffers/samplers; the font texture is created from
// draw_data->Textures on the first RenderDrawData (RendererHasTextures).
static void TryInitBackend()
{
    if (s_BackendUp || s_BackendFailed)
        return;
    if (ImGui::GetCurrentContext() == nullptr)
        return; // context not up yet; retry on the next render event

    if (!ImGui_ImplOpenGL3_Init(nullptr)) // nullptr: auto-detect the GLSL version from the context
    {
        s_BackendFailed = true;
        ContextHost_PushDiagnostic("OpenGL backend bring-up failed (ImGui_ImplOpenGL3_Init); UI rendering is disabled for this session.");
        return;
    }
    if (!ImGui_ImplOpenGL3_CreateDeviceObjects())
    {
        ImGui_ImplOpenGL3_Shutdown();
        s_BackendFailed = true;
        ContextHost_PushDiagnostic("OpenGL backend bring-up failed (ImGui_ImplOpenGL3_CreateDeviceObjects); UI rendering is disabled for this session.");
        return;
    }
    s_BackendUp = true;
}

void BackendOpenGL_Shutdown(void)
{
    // ImGui_ImplOpenGL3_Shutdown asserts when no backend/context is up; guard both.
    if (s_BackendUp && ImGui::GetCurrentContext() != nullptr)
        ImGui_ImplOpenGL3_Shutdown();
    s_BackendUp = false;
    s_BackendFailed = false; // latch clears with the backend teardown
}

void BackendOpenGL_Render(void)
{
    // ISSUES #004: same frame-guard contract as BackendD3D11_Render — the whole
    // body runs on Unity's render thread against draw data the game thread may
    // be rebuilding; the guard blocks the next BeginFrame until this draw
    // completes, so the draw data here is always a complete, stable frame.
    ContextHost_LockFrame();

    TryInitBackend();
    if (!s_BackendUp || ImGui::GetCurrentContext() == nullptr)
    {
        ContextHost_UnlockFrame();
        return;
    }

    ImDrawData* drawData = ImGui::GetDrawData();
    if (drawData != nullptr)
        ImGui_ImplOpenGL3_RenderDrawData(drawData);

    ContextHost_UnlockFrame();
}
