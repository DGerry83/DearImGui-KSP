// DearImGuiKSPNative — ImGui context host implementation (chunk C3, milestone M2).
//
// Single context. BackendFlags carries ImGuiBackendFlags_RendererHasTextures
// from the start (imgui 1.92.9 texture protocol): the atlas is built lazily by
// NewFrame and GPU-uploaded by the backend from draw_data->Textures — there is
// deliberately no CPU-side Build() here (see ContextInit for details).

#include "ContextHost.h"

#include "imgui.h"

// The one ImGui context for the DLL.
static ImGuiContext* s_Context = nullptr;

// Lower clamp for frame delta so NewFrame never sees a zero/negative dt.
static const float kMinDeltaSeconds = 1.0f / 240.0f;

// Sane nonzero default so headless frames are legal before the first
// BeginFrame supplies the real game-window size.
static const float kDefaultDisplayWidth  = 1920.0f;
static const float kDefaultDisplayHeight = 1080.0f;

DEARIMGUIKSP_NATIVE_API int DearImGuiKSPNative_ContextInit(void)
{
    if (s_Context != nullptr)
        return 0; // already initialized

    IMGUI_CHECKVERSION();
    s_Context = ImGui::CreateContext();
    if (s_Context == nullptr)
        return 1;

    ImGuiIO& io = ImGui::GetIO();
    // RendererHasTextures is set HERE, not by the backend: imgui 1.92.9's new
    // texture protocol forbids calling ImFontAtlas::Build() once the flag is
    // set, and the flag must be set before the first NewFrame or the legacy
    // "atlas not built" assert fires instead. With the flag set from the
    // start, NewFrame builds the atlas lazily and the backend creates the GPU
    // texture from draw_data->Textures (imgui_draw.cpp:2810-2820).
    io.BackendFlags |= ImGuiBackendFlags_RendererHasTextures;
    io.DisplaySize  = ImVec2(kDefaultDisplayWidth, kDefaultDisplayHeight);
    io.IniFilename  = nullptr; // no imgui.ini: window state belongs to consumers (D7, spec §5.4)

    ImGui::StyleColorsDark();

    // Embedded default font (ProggyClean) registered; the atlas itself is NOT
    // built here — see the BackendFlags comment above (spec §4.1).
    if (io.Fonts->AddFontDefault() == nullptr)
    {
        ImGui::DestroyContext(s_Context);
        s_Context = nullptr;
        return 2;
    }
    return 0;
}

DEARIMGUIKSP_NATIVE_API void DearImGuiKSPNative_ContextShutdown(void)
{
    if (s_Context != nullptr)
    {
        ImGui::DestroyContext(s_Context); // also frees the atlas CPU data
        s_Context = nullptr;
    }
}

DEARIMGUIKSP_NATIVE_API void DearImGuiKSPNative_BeginFrame(float width, float height, float deltaSeconds)
{
    if (s_Context == nullptr)
        return;

    ImGuiIO& io   = ImGui::GetIO();
    io.DisplaySize = ImVec2(width, height);
    io.DeltaTime   = deltaSeconds > kMinDeltaSeconds ? deltaSeconds : kMinDeltaSeconds;

    ImGui::NewFrame();
}

DEARIMGUIKSP_NATIVE_API void DearImGuiKSPNative_EndFrame(void)
{
    if (s_Context == nullptr)
        return;
    ImGui::Render();
}

DEARIMGUIKSP_NATIVE_API int DearImGuiKSPNative_GetFontAtlasPixels(unsigned char** outPixels, int* outWidth, int* outHeight)
{
    if (s_Context == nullptr || outPixels == nullptr || outWidth == nullptr || outHeight == nullptr)
        return 1;

    ImGui::GetIO().Fonts->GetTexDataAsRGBA32(outPixels, outWidth, outHeight);
    if (*outPixels == nullptr || *outWidth <= 0 || *outHeight <= 0)
        return 2;
    return 0;
}

DEARIMGUIKSP_NATIVE_API void DearImGuiKSPNative_GetIoCaptureState(int* wantMouse, int* wantKeyboard)
{
    if (wantMouse != nullptr)
        *wantMouse = 0;
    if (wantKeyboard != nullptr)
        *wantKeyboard = 0;

    if (s_Context == nullptr)
        return;

    ImGuiIO& io = ImGui::GetIO();
    if (wantMouse != nullptr)
        *wantMouse = io.WantCaptureMouse ? 1 : 0;
    if (wantKeyboard != nullptr)
        *wantKeyboard = io.WantCaptureKeyboard ? 1 : 0;
}
