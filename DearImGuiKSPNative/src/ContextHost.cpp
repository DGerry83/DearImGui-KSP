// DearImGuiKSPNative — ImGui context host implementation (chunk C3, milestone M2).
//
// Single context. BackendFlags carries ImGuiBackendFlags_RendererHasTextures
// from the start (imgui 1.92.9 texture protocol): the atlas is built lazily by
// NewFrame and GPU-uploaded by the backend from draw_data->Textures — there is
// deliberately no CPU-side Build() here (see ContextInit for details).

#include "ContextHost.h"

#include "imgui.h"
#include "imgui_internal.h" // ImGuiContext::Windows / ImGuiWindow (ISSUES #002 clamp)

// The one ImGui context for the DLL.
static ImGuiContext* s_Context = nullptr;

// Set by the first BeginFrame: NewFrame builds the atlas lazily (1.92.9
// texture protocol), after which AddFontFromFileTTF would assert on the
// locked atlas — font loads are legal only before this flips (spec §4.2).
static bool s_FramesBegun = false;

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
    s_FramesBegun = false;
}

DEARIMGUIKSP_NATIVE_API void DearImGuiKSPNative_BeginFrame(float width, float height, float deltaSeconds)
{
    if (s_Context == nullptr)
        return;

    ImGuiIO& io   = ImGui::GetIO();
    io.DisplaySize = ImVec2(width, height);
    io.DeltaTime   = deltaSeconds > kMinDeltaSeconds ? deltaSeconds : kMinDeltaSeconds;

    s_FramesBegun = true;
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

// Previous-frame input masks so we only queue events on change.
static int s_PreviousMouseButtons = 0;
static int s_PreviousKeyBits = 0;

static const ImGuiKey s_KeyBitToImGuiKey[] =
{
    ImGuiKey_Backspace,
    ImGuiKey_Delete,
    ImGuiKey_LeftArrow,
    ImGuiKey_RightArrow,
    ImGuiKey_UpArrow,
    ImGuiKey_DownArrow,
    ImGuiKey_Home,
    ImGuiKey_End,
    ImGuiKey_Enter,
    ImGuiKey_Escape,
    ImGuiKey_Tab,
    ImGuiKey_LeftCtrl,
    ImGuiKey_RightCtrl,
};

DEARIMGUIKSP_NATIVE_API void DearImGuiKSPNative_FeedFrameInput(float mouseX, float mouseY, float wheel, int mouseButtons, int keyBits, const char* utf8Chars)
{
    if (s_Context == nullptr)
        return;

    ImGuiIO& io = ImGui::GetIO();

    io.AddMousePosEvent(mouseX, mouseY);

    if (wheel != 0.0f)
        io.AddMouseWheelEvent(0.0f, wheel);

    int changedButtons = s_PreviousMouseButtons ^ mouseButtons;
    for (int button = 0; button < 3; ++button)
    {
        if (changedButtons & (1 << button))
        {
            bool down = (mouseButtons & (1 << button)) != 0;
            io.AddMouseButtonEvent(button, down);
        }
    }
    s_PreviousMouseButtons = mouseButtons;

    const int keyCount = sizeof(s_KeyBitToImGuiKey) / sizeof(s_KeyBitToImGuiKey[0]);
    int changedKeys = s_PreviousKeyBits ^ keyBits;
    for (int bit = 0; bit < keyCount; ++bit)
    {
        if (changedKeys & (1 << bit))
        {
            bool down = (keyBits & (1 << bit)) != 0;
            io.AddKeyEvent(s_KeyBitToImGuiKey[bit], down);
        }
    }
    s_PreviousKeyBits = keyBits;

    if (utf8Chars != nullptr && utf8Chars[0] != '\0')
        io.AddInputCharactersUTF8(utf8Chars);
}

void ContextHost_ClampWindowsToViewport(float width, float height)
{
    if (s_Context == nullptr)
        return;

    // Clamp against the PASSED viewport size, never io.DisplaySize: when the
    // resolution drops, io.DisplaySize still holds the old (larger) value at
    // the moment the change is handled, so reading it here would clamp against
    // a stale size and the fix would be a no-op (ISSUES #002 G3 rework).
    const ImVec2 viewport(width, height);
    ImVector<ImGuiWindow*>& windows = s_Context->Windows;
    for (int i = 0; i < windows.Size; ++i)
    {
        ImGuiWindow* window = windows[i];
        if (window == nullptr || window->Hidden || !window->WasActive)
            continue; // hidden this frame or not submitted by any consumer

        const float maxX = ImMax(0.0f, viewport.x - window->Size.x);
        const float maxY = ImMax(0.0f, viewport.y - window->Size.y);
        window->Pos.x = ImClamp(window->Pos.x, 0.0f, maxX);
        window->Pos.y = ImClamp(window->Pos.y, 0.0f, maxY);
    }
}

int ContextHost_LoadFontFromFile(const char* utf8Path, float sizePixels)
{
    if (s_Context == nullptr)
        return 1; // no context
    if (s_FramesBegun)
        return 2; // atlas already built/locked by NewFrame — too late to add fonts

    // AddFontFromFileTTF leaves the atlas untouched when the file cannot be
    // read (imgui_draw.cpp:3251-3253 returns NULL before any state change),
    // so on failure the embedded default remains exactly as it was.
    ImGuiIO& io = ImGui::GetIO();
    ImFont* font = io.Fonts->AddFontFromFileTTF(utf8Path, sizePixels);
    if (font == nullptr)
        return 3;
    // ImGui renders with io.FontDefault, or Fonts[0] when it is null — and
    // Fonts[0] is the embedded ProggyClean added in ContextInit. The first
    // successfully loaded custom font (Regular weight) must claim FontDefault
    // or it never renders; later loads (Medium) stay atlas-only.
    if (io.FontDefault == nullptr)
        io.FontDefault = font;
    return 0;
}
