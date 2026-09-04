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

// Defined below; called between ImGui::EndFrame and ImGui::Render.
static void ApplyWindowBgGradient();

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
    // Explicit EndFrame first: it finalizes every window's draw list, which
    // the gradient pass below shades. Both EndFrame and Render are idempotent
    // (imgui.cpp:6329-6336, imgui.cpp:6443-6451), so the previously observed
    // Render()-only behavior is preserved for the disabled case.
    ImGui::EndFrame();
    ApplyWindowBgGradient();
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

// ---- Window-background gradient (chunk C9, spec §6.1) ----
//
// The theme's window background is a two-stop vertical gradient (top -> bottom).
// cimgui offers no hook, so a descriptor set from the managed ThemeEngine is
// consumed by a per-frame pass between ImGui::EndFrame (draw lists final) and
// ImGui::Render (draw-data build). Disabled by default: the pass early-outs
// and rendering stays byte-exact stock (the "dark" preset regression gate).

static bool  s_WindowBgGradientEnabled = false;
static ImU32 s_WindowBgGradientTop = 0;
static ImU32 s_WindowBgGradientBottom = 0;

int ContextHost_SetWindowBgGradient(int enabled, float r1, float g1, float b1, float a1, float r2, float g2, float b2, float a2)
{
    if (s_Context == nullptr)
        return 1; // no context
    s_WindowBgGradientEnabled = enabled != 0;
    s_WindowBgGradientTop = ImGui::ColorConvertFloat4ToU32(ImVec4(r1, g1, b1, a1));
    s_WindowBgGradientBottom = ImGui::ColorConvertFloat4ToU32(ImVec4(r2, g2, b2, a2));
    return 0;
}

int ContextHost_GetDrawListVtxCount(ImDrawList* drawList)
{
    if (drawList == nullptr)
        return -1;
    return drawList->VtxBuffer.Size;
}

// Shades the window-background fill verts of every visible window. O(windows),
// no allocation; STRICT no-op when the descriptor is disabled.
static void ApplyWindowBgGradient()
{
    if (!s_WindowBgGradientEnabled)
        return;

    ImGuiContext& g = *s_Context;
    const ImGuiStyle& style = g.Style;
    for (int i = 0; i < g.Windows.Size; ++i)
    {
        ImGuiWindow* window = g.Windows[i];
        // Same predicate as ImGui::Render's draw-list inclusion (IsWindowActiveAndVisible,
        // imgui.cpp:5663-5666): Active is THIS frame's flag (WasActive is only
        // refreshed at the next NewFrame, imgui.cpp:5979).
        if (window == nullptr || !window->Active || window->Hidden || window->Collapsed)
            continue; // not submitted/visible this frame, or title-bar only (no bg fill)
        if (window->Flags & (ImGuiWindowFlags_NoBackground | ImGuiWindowFlags_DockNodeHost))
            continue; // ImGui emitted no bg fill for these (imgui.cpp:7622, 7658)
        if (window->DockIsActive)
            continue; // docked bgs are emitted into the host window's draw list

        // Replicate GetWindowBgColorIdx (imgui.cpp:7226-7232): ImGui skips the
        // bg fill entirely when the resulting alpha is 0 (imgui.cpp:7660), and
        // the first command would then be some other geometry — don't shade it.
        const ImVec4& bgColor = (window->Flags & ImGuiWindowFlags_ChildWindow)
            ? style.Colors[ImGuiCol_ChildBg] : style.Colors[ImGuiCol_WindowBg];
        if (bgColor.w * style.Alpha <= 0.0f)
            continue;

        // The bg fill is the first geometry in window->DrawList (Begin,
        // imgui.cpp:7621-7680). It is an indexed draw, so the vertex range is
        // the max referenced vertex + 1 of the first command (indices do not
        // map 1:1 to vertices). The title-bar/border/scrollbar fills use the
        // same white-texture draw and may merge into that first command;
        // shading the merged range is accepted per the C9 contract — verts
        // above the bg top clamp to the top stop (ShadeVerts saturates t,
        // imgui_draw.cpp:2399).
        ImDrawList* drawList = window->DrawList;
        if (drawList->CmdBuffer.Size == 0)
            continue;
        const ImDrawCmd& cmd = drawList->CmdBuffer[0];
        if (cmd.ElemCount == 0)
            continue;
        int vertEnd = 0;
        const ImDrawIdx* idx = drawList->IdxBuffer.Data;
        for (unsigned int n = 0; n < cmd.ElemCount; ++n)
            if ((int)idx[n] + 1 > vertEnd)
                vertEnd = (int)idx[n] + 1;
        if (vertEnd <= 0)
            continue;

        const ImVec2 gradientP0 = window->Pos;
        const ImVec2 gradientP1(window->Pos.x, window->Pos.y + window->Size.y);
        ImGui::ShadeVertsLinearColorGradientKeepAlpha(
            drawList, 0, vertEnd, gradientP0, gradientP1,
            s_WindowBgGradientTop, s_WindowBgGradientBottom);
    }
}

int ContextHost_SetStyleColor(int idx, float r, float g, float b, float a)
{
    if (s_Context == nullptr)
        return 1; // no context
    if (idx < 0 || idx >= ImGuiCol_COUNT)
        return 2; // bad index — the style table is untouched
    ImGui::GetStyle().Colors[idx] = ImVec4(r, g, b, a);
    return 0;
}

int ContextHost_SetStyleVarFloat(int idx, float v)
{
    if (s_Context == nullptr)
        return 1; // no context

    ImGuiStyle& style = ImGui::GetStyle();
    switch (idx)
    {
        case ImGuiStyleVar_WindowRounding:    style.WindowRounding    = v; return 0;
        case ImGuiStyleVar_WindowBorderSize:  style.WindowBorderSize  = v; return 0;
        case ImGuiStyleVar_FrameRounding:     style.FrameRounding     = v; return 0;
        case ImGuiStyleVar_FrameBorderSize:   style.FrameBorderSize   = v; return 0;
        case ImGuiStyleVar_GrabRounding:      style.GrabRounding      = v; return 0;
        case ImGuiStyleVar_GrabMinSize:       style.GrabMinSize       = v; return 0;
        case ImGuiStyleVar_ScrollbarRounding: style.ScrollbarRounding = v; return 0;
        case ImGuiStyleVar_TabRounding:       style.TabRounding       = v; return 0;
        case ImGuiStyleVar_ChildRounding:     style.ChildRounding     = v; return 0;
        case ImGuiStyleVar_PopupRounding:     style.PopupRounding     = v; return 0;
        default: return 2; // not a float var this layer supports — nothing written
    }
}

int ContextHost_SetStyleVarVec2(int idx, float x, float y)
{
    if (s_Context == nullptr)
        return 1; // no context

    ImGuiStyle& style = ImGui::GetStyle();
    switch (idx)
    {
        case ImGuiStyleVar_WindowPadding: style.WindowPadding = ImVec2(x, y); return 0;
        case ImGuiStyleVar_FramePadding:  style.FramePadding  = ImVec2(x, y); return 0;
        case ImGuiStyleVar_ItemSpacing:   style.ItemSpacing   = ImVec2(x, y); return 0;
        default: return 2; // not a Vec2 var this layer supports — nothing written
    }
}

int ContextHost_StyleColorsDark(void)
{
    if (s_Context == nullptr)
        return 1; // no context
    // StyleColorsDark (1.92.9) rewrites only the Colors table. Resetting the whole
    // style to default-constructed values first makes the "dark" preset byte-exact
    // stock — including after a live ksp->dark switch, where ksp's var overrides
    // (rounding, padding) would otherwise persist (I-04).
    ImGui::GetStyle() = ImGuiStyle();
    ImGui::StyleColorsDark(&ImGui::GetStyle());
    return 0;
}
