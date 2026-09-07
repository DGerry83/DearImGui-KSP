// DearImGuiKSPNative — ImGui context host implementation (chunk C3, milestone M2).
//
// Single context. BackendFlags carries ImGuiBackendFlags_RendererHasTextures
// from the start (imgui 1.92.9 texture protocol): the atlas is built lazily by
// NewFrame and GPU-uploaded by the backend from draw_data->Textures — there is
// deliberately no CPU-side Build() here (see ContextInit for details).

#include "ContextHost.h"

#include <windows.h> // SRWLOCK (ISSUES #004 frame guard)

#include <cstdio>  // _snprintf (diagnostics line formatting, G2-04)
#include <cstring> // strlen/memcpy (diagnostics buffer, G2-04)

#include "imgui.h"
#include "imgui_internal.h" // ImGuiContext::Windows / ImGuiWindow (ISSUES #002 clamp)
#include "implot.h"

// The one ImGui context for the DLL.
static ImGuiContext* s_Context = nullptr;

// The one ImPlot context for the DLL — created/destroyed in lockstep with
// s_Context (create after ImGui::CreateContext, destroy before its destruction).
static ImPlotContext* s_PlotContext = nullptr;

// Set by the first BeginFrame: NewFrame builds the atlas lazily (1.92.9
// texture protocol), after which AddFontFromFileTTF would assert on the
// locked atlas — font loads are legal only before this flips (spec §4.2).
static bool s_FramesBegun = false;

// ---- ISSUES #004 frame guard ----
//
// The render event draws the previous frame's draw data on Unity's render
// thread while the game thread is free to enter the next frame's NewFrame.
// ImGui::NewFrame marks every viewport's DrawData invalid
// (imgui.cpp:5831-5834, so GetDrawData() returns null and our render callback
// would skip the draw — one blank UI frame) and Begin reuses the same
// ImDrawList objects in place (_ResetForNewFrame, imgui.cpp:8137) that the
// render thread may still be walking. The lock serializes the two sides: held
// by the game thread from BeginFrame to EndFrame, taken by the render thread
// for the duration of RenderDrawData. SRWLOCK_INIT needs no teardown.
static SRWLOCK s_FrameLock = SRWLOCK_INIT;

void ContextHost_LockFrame(void)
{
    AcquireSRWLockExclusive(&s_FrameLock);
}

void ContextHost_UnlockFrame(void)
{
    ReleaseSRWLockExclusive(&s_FrameLock);
}

// Lower clamp for frame delta so NewFrame never sees a zero/negative dt.
static const float kMinDeltaSeconds = 1.0f / 240.0f;

// Sane nonzero default so headless frames are legal before the first
// BeginFrame supplies the real game-window size.
static const float kDefaultDisplayWidth  = 1920.0f;
static const float kDefaultDisplayHeight = 1080.0f;

// Defined below; called between ImGui::EndFrame and ImGui::Render.
static void ApplyWindowBgGradient();

// ---- Diagnostics channel (C04, review items G2-04/G3-03) ----
//
// ImGui's stock recoverable-error path (ErrorLog, imgui.cpp:11984-12026)
// paints a red debug tooltip over the game and otherwise writes only to the
// never-shown debug-log buffer — diagnostics never reach KSP.log. ContextInit
// disables the tooltip (io.ConfigErrorRecoveryEnableTooltip) and installs an
// error callback whose text lands in this fixed buffer; the managed bridge
// drains it once per frame (DearImGuiKSPNative_DrainDiagnostics, after
// EndUiFrame) and writes the lines to KSP.log. The D3D11 backend bring-up
// failure (G3-03) reports through the same buffer from the render thread, so
// the buffer has its own lock (never held while taking s_FrameLock — no
// lock-order hazard). Fixed storage, no allocation; on overflow the new
// message is dropped and a drop notice becomes the next pending message.
static SRWLOCK s_DiagLock = SRWLOCK_INIT;
static const int kDiagCapacityBytes = 4096;
static char s_DiagBuffer[kDiagCapacityBytes]; // '\n'-separated lines, no embedded NULs
static int s_DiagUsed = 0;
static int s_DiagDropped = 0;

void ContextHost_PushDiagnostic(const char* msg)
{
    if (msg == nullptr || msg[0] == '\0')
        return;
    AcquireSRWLockExclusive(&s_DiagLock);
    const int len = (int)strlen(msg);
    if (s_DiagUsed + len + 1 <= kDiagCapacityBytes) // +1: '\n' separator
    {
        if (s_DiagUsed > 0)
            s_DiagBuffer[s_DiagUsed++] = '\n';
        memcpy(s_DiagBuffer + s_DiagUsed, msg, len);
        s_DiagUsed += len;
    }
    else
    {
        ++s_DiagDropped;
    }
    ReleaseSRWLockExclusive(&s_DiagLock);
}

// ImGuiErrorCallback (typedef imgui_internal.h:2315). The g.ErrorCallback
// field is internal-only in 1.92.9 ("May be exposed in public API eventually",
// imgui_internal.h:2809) — this TU already compiles against imgui_internal.h
// for the ISSUES #002 clamp. Line format mirrors the debug log's
// ("In window 'X': msg", imgui.cpp:11997).
static void OnImGuiError(ImGuiContext* ctx, void* userData, const char* msg)
{
    (void)userData;
    ImGuiWindow* window = ctx != nullptr ? ctx->CurrentWindow : nullptr;
    char line[512];
    _snprintf(line, sizeof(line) - 1, "ImGui error in window '%s': %s",
        window != nullptr ? window->Name : "NULL", msg != nullptr ? msg : "(null)");
    line[sizeof(line) - 1] = '\0'; // _snprintf does not NUL-terminate on truncation
    ContextHost_PushDiagnostic(line);
}

// Returns the bytes pending BEFORE the call (0 = nothing pending — the managed
// side queries with a null dst first, so steady state allocates nothing). A
// non-null dst drains: copies up to dstCapacity-1 bytes, NUL-terminates, and
// clears the buffer.
int ContextHost_DrainDiagnostics(char* dst, int dstCapacity)
{
    AcquireSRWLockExclusive(&s_DiagLock);
    const int pending = s_DiagUsed;
    if (dst != nullptr && dstCapacity > 0 && s_DiagUsed > 0)
    {
        const int copy = s_DiagUsed < dstCapacity - 1 ? s_DiagUsed : dstCapacity - 1;
        memcpy(dst, s_DiagBuffer, copy);
        dst[copy] = '\0';
        s_DiagUsed = 0;
        if (s_DiagDropped > 0)
        {
            // The overflow notice becomes the next pending message.
            const int n = _snprintf(s_DiagBuffer, kDiagCapacityBytes - 1,
                "(%d native diagnostics dropped: buffer overflow)", s_DiagDropped);
            s_DiagUsed = n > 0 ? n : 0;
            s_DiagDropped = 0;
        }
    }
    ReleaseSRWLockExclusive(&s_DiagLock);
    return pending;
}

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

    // G2-04: route ImGui recoverable errors to the diagnostics buffer (drained
    // to KSP.log by the managed bridge) instead of the stock red debug tooltip
    // painted over the game. Recovery itself and the assert/debug-log flags
    // stay stock (the assert is compiled out of the /DNDEBUG release build
    // anyway; the debug log only feeds the never-shown metrics UI). ErrorLog
    // still requires one sink to be enabled (imgui.cpp:11747) — the callback
    // satisfies that with all three stock sinks off or on.
    io.ConfigErrorRecoveryEnableTooltip = false;
    s_Context->ErrorCallback = &OnImGuiError;
    s_Context->ErrorCallbackUserData = nullptr;

    ImGui::StyleColorsDark();

    // ImPlot registers with the current ImGui context, so this goes after
    // CreateContext. The (malloc-only) failure path returns the same code as
    // the ImGui-create failure above — the managed rc contract is frozen.
    s_PlotContext = ImPlot::CreateContext();
    if (s_PlotContext == nullptr)
    {
        ImGui::DestroyContext(s_Context);
        s_Context = nullptr;
        return 1;
    }

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
        // ImPlot's destructor touches the ImGui context — it must go first
        // (reverse order would be a use-after-free).
        if (s_PlotContext != nullptr)
        {
            ImPlot::DestroyContext(s_PlotContext);
            s_PlotContext = nullptr;
        }
        ImGui::DestroyContext(s_Context); // also frees the atlas CPU data
        s_Context = nullptr;
    }
    s_FramesBegun = false;

    // Drop any undrained diagnostics with the context that produced them.
    AcquireSRWLockExclusive(&s_DiagLock);
    s_DiagUsed = 0;
    s_DiagDropped = 0;
    ReleaseSRWLockExclusive(&s_DiagLock);
}

DEARIMGUIKSP_NATIVE_API void DearImGuiKSPNative_BeginFrame(float width, float height, float deltaSeconds)
{
    if (s_Context == nullptr)
        return;

    ContextHost_LockFrame(); // ISSUES #004: held until EndFrame

    ImGuiIO& io   = ImGui::GetIO();
    io.DisplaySize = ImVec2(width, height);
    io.DeltaTime   = deltaSeconds > kMinDeltaSeconds ? deltaSeconds : kMinDeltaSeconds;

    s_FramesBegun = true;
    ImGui::NewFrame();
}

DEARIMGUIKSP_NATIVE_API void DearImGuiKSPNative_EndFrame(void)
{
    if (s_Context == nullptr)
        return; // BeginFrame did not lock either; the pair stays balanced
    // Explicit EndFrame first: it finalizes every window's draw list, which
    // the gradient pass below shades. Both EndFrame and Render are idempotent
    // (imgui.cpp:6329-6336, imgui.cpp:6443-6451), so the previously observed
    // Render()-only behavior is preserved for the disabled case.
    ImGui::EndFrame();
    ApplyWindowBgGradient();
    ImGui::Render();
    ContextHost_UnlockFrame(); // ISSUES #004: draw data stable for the render thread
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

// Shades one solid-fill vert in place when its full RGBA is one of the window
// chrome fills (the C35 inclusion filter: resolved bg, title bar either focus
// state, menu bar, border); glyph verts and every other primitive keep their
// color BY CONSTRUCTION. t saturates like ShadeVertsLinearColorGradientKeepAlpha
// (imgui_draw.cpp:2399); alpha is never touched. Returns true when the vert
// was shaded. Shared by the own-command-0 path and the G2-01 child retarget.
static bool TryShadeChromeVert(ImDrawVert& vert, const ImVec2& whiteUv,
    ImU32 bgCol, ImU32 titleBgCol, ImU32 titleBgActiveCol, ImU32 menuBarBgCol, ImU32 borderCol,
    float gradientTopY, float gradientHeight)
{
    if (vert.uv.x != whiteUv.x || vert.uv.y != whiteUv.y)
        return false; // glyph vert — never a fill
    if (vert.col != bgCol && vert.col != titleBgCol && vert.col != titleBgActiveCol
        && vert.col != menuBarBgCol && vert.col != borderCol)
        return false; // not window chrome — keeps its theme color
    float t = gradientHeight > 0.0f ? (vert.pos.y - gradientTopY) / gradientHeight : 0.0f;
    t = t < 0.0f ? 0.0f : (t > 1.0f ? 1.0f : t);
    const float invT = 1.0f - t;
    const ImU32 r = (ImU32)(((s_WindowBgGradientTop      ) & 0xFF) * invT + ((s_WindowBgGradientBottom      ) & 0xFF) * t + 0.5f);
    const ImU32 gcol = (ImU32)(((s_WindowBgGradientTop >>  8) & 0xFF) * invT + ((s_WindowBgGradientBottom >>  8) & 0xFF) * t + 0.5f);
    const ImU32 b = (ImU32)(((s_WindowBgGradientTop >> 16) & 0xFF) * invT + ((s_WindowBgGradientBottom >> 16) & 0xFF) * t + 0.5f);
    vert.col = (vert.col & 0xFF000000u) | r | (gcol << 8) | (b << 16);
    return true;
}

// G2-01 retarget: when a child window's decorations (bg fill, border,
// scrollbars) are rendered into an ANCESTOR's draw list — Begin swaps
// window->DrawList to the parent's for RenderWindowDecorations to save a draw
// call, then restores it (imgui.cpp:8585-8611) — the child's own list holds
// only content and the fills must be found in the ancestor chain. They merge
// into whichever ancestor command was current at the child's Begin (never the
// ancestor's own command 0, which holds the ancestor's decorations and is
// shaded by that ancestor's own pass), so scan every command past 0 of each
// direct-line ancestor and shade only chrome-colored solid fills contained in
// the child's outer rect. Walking the whole chain covers nested children (a
// swapped parent forwarded its decorations to ITS parent in turn).
static void ShadeChildDecorationsInAncestors(ImGuiWindow* window,
    ImU32 bgCol, ImU32 titleBgCol, ImU32 titleBgActiveCol, ImU32 menuBarBgCol, ImU32 borderCol)
{
    // The border strokes sit on the rect edge; expand the containment test by
    // the border size so they are included.
    const float pad = ImMax(window->WindowBorderSize, 1.0f);
    const float minX = window->Pos.x - pad;
    const float minY = window->Pos.y - pad;
    const float maxX = window->Pos.x + window->Size.x + pad;
    const float maxY = window->Pos.y + window->Size.y + pad;
    for (ImGuiWindow* ancestor = window->ParentWindow; ancestor != nullptr; ancestor = ancestor->ParentWindow)
    {
        ImDrawList* ancestorList = ancestor->DrawList;
        if (ancestorList == nullptr || ancestorList->CmdBuffer.Size == 0)
            continue;
        const ImVec2 whiteUv = ancestorList->_Data->TexUvWhitePixel;
        for (int c = 1; c < ancestorList->CmdBuffer.Size; ++c) // cmd 0 = the ancestor's own decorations
        {
            const ImDrawCmd& cmd = ancestorList->CmdBuffer[c];
            if (cmd.ElemCount == 0)
                continue;
            int vertEnd = 0;
            const ImDrawIdx* idx = ancestorList->IdxBuffer.Data + cmd.IdxOffset;
            for (unsigned int n = 0; n < cmd.ElemCount; ++n)
                if ((int)idx[n] + 1 > vertEnd)
                    vertEnd = (int)idx[n] + 1;
            ImDrawVert* verts = ancestorList->VtxBuffer.Data + (int)cmd.VtxOffset;
            for (int n = 0; n < vertEnd; ++n)
            {
                ImDrawVert& vert = verts[n];
                if (vert.pos.x < minX || vert.pos.x > maxX || vert.pos.y < minY || vert.pos.y > maxY)
                    continue; // outside the child's rect — the ancestor's own content
                TryShadeChromeVert(vert, whiteUv, bgCol, titleBgCol, titleBgActiveCol, menuBarBgCol, borderCol,
                    window->Pos.y, window->Size.y);
            }
        }
    }
}

// Shades the window-background fill verts of every visible window. O(windows +
// shaded vert ranges), no allocation; STRICT no-op when the descriptor is
// disabled.
static void ApplyWindowBgGradient()
{
    if (!s_WindowBgGradientEnabled)
        return;

    ImGuiContext& g = *s_Context;
    const ImGuiStyle& style = g.Style;
    // C35 (ISSUES #014 audit follow-up): the filter is now INCLUSION, not
    // exclusion. Begin merges every decoration fill into command 0 (see the
    // per-window comment below), and the pre-C35 exclusion filter (Text /
    // grips / scrollbars) needed three patches (#008/C27, C32, C34) and still
    // leaked two more decoration classes (title-button hover/held background,
    // resize-border highlight — the C34 audit findings). The M3-approved
    // shaded set is exactly the solid fills imgui paints as window chrome:
    // the resolved window bg, the title bar (either focus state), the menu
    // bar, and the border. Those are cached once per pass; a vert is shaded
    // only if its full RGBA (RGB+alpha) equals one of them, so every other
    // primitive — interaction-state chrome especially — keeps its theme color
    // BY CONSTRUCTION. The exclusion-era casualty list is closed.
    //   BorderShadow is deliberately absent: the pinned 1.92.9 window border
    // path (RenderWindowOuterBorders, imgui.cpp:7565-7587) emits only
    // ImGuiCol_Border; BorderShadow is used by the widget-frame helpers
    // (RenderFrame/RenderFrameBorder, imgui.cpp:4103-4126), which render
    // after the content clip push and never merge into command 0.
    const ImU32 titleBgCol       = ImGui::GetColorU32(ImGuiCol_TitleBg);
    const ImU32 titleBgActiveCol = ImGui::GetColorU32(ImGuiCol_TitleBgActive);
    const ImU32 menuBarBgCol     = ImGui::GetColorU32(ImGuiCol_MenuBarBg);
    const ImU32 borderCol        = ImGui::GetColorU32(ImGuiCol_Border);
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

        // Replicate GetWindowBgColorIdx (imgui.cpp:7226-7234) so the inclusion
        // test uses the bg color THIS window actually filled with: popups and
        // tooltips fill with PopupBg, child windows with ChildBg, everything
        // else with WindowBg (pre-C35 the pass resolved only ChildBg/WindowBg;
        // popup bg verts were still shaded only because the filter was
        // exclusionary). ImGui skips the bg fill entirely when the resolved
        // color's alpha is 0 (imgui.cpp:7660), and the first command would
        // then be some other geometry — don't shade it.
        ImGuiCol bgIdx = ImGuiCol_WindowBg;
        if (window->Flags & (ImGuiWindowFlags_Tooltip | ImGuiWindowFlags_Popup))
            bgIdx = ImGuiCol_PopupBg;
        else if (window->Flags & ImGuiWindowFlags_ChildWindow)
            bgIdx = ImGuiCol_ChildBg;
        const ImVec4& bgColor = style.Colors[bgIdx];
        if (bgColor.w * style.Alpha <= 0.0f)
            continue;
        const ImU32 bgCol = ImGui::GetColorU32(bgIdx);

        // The bg fill is the first geometry in window->DrawList (Begin,
        // imgui.cpp:7621-7680). It is an indexed draw, so the vertex range is
        // the max referenced vertex + 1 of the first command (indices do not
        // map 1:1 to vertices). The title-bar/border/scrollbar fills use the
        // same white-texture draw and may merge into that first command;
        // shading the merged range is accepted per the C9 contract — verts
        // above the bg top clamp to the top stop (saturate t as
        // ShadeVertsLinearColorGradientKeepAlpha does, imgui_draw.cpp:2399).
        //
        // Glyph verts share the font-atlas texture with the bg fill, so text
        // (e.g. scroll-region list rows) merges into this same first command.
        // Only solid-fill verts — the ones sampling the atlas white pixel —
        // are ever shaded; recoloring glyph verts tints item text toward the
        // gradient and rows "disappear" into it (M3 in-game fix).
        //
        // Everything Begin emits before the content clip push merges into
        // this command (verified against imgui.cpp 1.92.9: bg fill, title/menu-
        // bar fills, scrollbars, resize grips, borders, then the title-bar
        // collapse arrow and close cross). Since C9 the M3-approved shaded
        // look covers exactly the window chrome fills; the C34 decoration
        // audit enumerated every other merged primitive (Text foreground,
        // grip states, scrollbar states, title-button hover/held background,
        // resize-border highlight, dock tab triangle, NavCursor) and none of
        // them is a window fill — they must keep their theme colors. The
        // inclusion filter above is exactly that split, and unlike the old
        // exclusion list it cannot leak: a fill not in the set keeps its
        // color by construction.
        ImDrawList* drawList = window->DrawList;
        if (drawList->CmdBuffer.Size == 0)
            continue;
        const ImDrawCmd& cmd = drawList->CmdBuffer[0];
        if (cmd.ElemCount == 0)
        {
            // G2-01: an empty first command on a CHILD window means Begin
            // rendered its decorations into an ancestor's draw list
            // (imgui.cpp:8585-8611); retarget the shading there instead of
            // silently skipping the child. Any other window simply has no
            // fill in command 0 to shade.
            if (window->Flags & ImGuiWindowFlags_ChildWindow)
                ShadeChildDecorationsInAncestors(window, bgCol, titleBgCol, titleBgActiveCol, menuBarBgCol, borderCol);
            continue;
        }
        const int vtxBase = (int)cmd.VtxOffset;
        int vertEnd = 0;
        const ImDrawIdx* idx = drawList->IdxBuffer.Data + cmd.IdxOffset;
        for (unsigned int n = 0; n < cmd.ElemCount; ++n)
            if ((int)idx[n] + 1 > vertEnd)
                vertEnd = (int)idx[n] + 1;
        if (vertEnd <= 0)
            continue;

        const ImVec2 gradientP0 = window->Pos;
        const float gradientHeight = window->Size.y;
        const ImVec2 whiteUv = drawList->_Data->TexUvWhitePixel;
        ImDrawVert* verts = drawList->VtxBuffer.Data + vtxBase;
        for (int n = 0; n < vertEnd; ++n)
            TryShadeChromeVert(verts[n], whiteUv, bgCol, titleBgCol, titleBgActiveCol, menuBarBgCol, borderCol,
                gradientP0.y, gradientHeight);
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

// G3-01 1px floor: a line-thickness field that was nonzero before scaling may
// not truncate to zero (ScaleAllSizes uses ImTrunc), or borders/separators/
// the text caret vanish below uiScale 1.0.
static float ScaledLineSizeFloor(float before, float scaled)
{
    return (before > 0.0f && scaled < 1.0f) ? 1.0f : scaled;
}

int ContextHost_SetUiScale(float scale)
{
    if (s_Context == nullptr)
        return 1; // no context
    if (scale <= 0.0f)
        return 2; // bad scale — the style and IO are untouched

    // ScaleAllSizes multiplies the CURRENT values: the managed ThemeEngine
    // resets the whole style (ContextHost_StyleColorsDark) before every apply,
    // so this always multiplies the defaults and repeated applies stay exact.
    ImGuiStyle& style = ImGui::GetStyle();

    // G3-01: ScaleAllSizes truncates with ImTrunc (imgui.cpp:1628-1681), so at
    // uiScale < 1.0 every 1px line size collapses to 0 and the line vanishes
    // (window/child/popup borders, separators, the text caret; MinScale 0.5 is
    // reachable from the settings panel). Snapshot the line-thickness fields
    // and re-apply a 1px floor to any that were nonzero before scaling —
    // deliberately zero fields (e.g. stock FrameBorderSize) stay zero.
    const float windowBorderSize         = style.WindowBorderSize;
    const float childBorderSize          = style.ChildBorderSize;
    const float popupBorderSize          = style.PopupBorderSize;
    const float frameBorderSize          = style.FrameBorderSize;
    const float imageBorderSize          = style.ImageBorderSize;
    const float tabBorderSize            = style.TabBorderSize;
    const float tabBarBorderSize         = style.TabBarBorderSize;
    const float tabBarOverlineSize       = style.TabBarOverlineSize;
    const float separatorSize            = style.SeparatorSize;
    const float separatorTextBorderSize  = style.SeparatorTextBorderSize;
    const float dockingSeparatorSize     = style.DockingSeparatorSize;
    const float treeLinesSize            = style.TreeLinesSize;
    const float inputTextCursorSize      = style.InputTextCursorSize;
    const float dragDropTargetBorderSize = style.DragDropTargetBorderSize;

    style.ScaleAllSizes(scale);

    style.WindowBorderSize         = ScaledLineSizeFloor(windowBorderSize, style.WindowBorderSize);
    style.ChildBorderSize          = ScaledLineSizeFloor(childBorderSize, style.ChildBorderSize);
    style.PopupBorderSize          = ScaledLineSizeFloor(popupBorderSize, style.PopupBorderSize);
    style.FrameBorderSize          = ScaledLineSizeFloor(frameBorderSize, style.FrameBorderSize);
    style.ImageBorderSize          = ScaledLineSizeFloor(imageBorderSize, style.ImageBorderSize);
    style.TabBorderSize            = ScaledLineSizeFloor(tabBorderSize, style.TabBorderSize);
    style.TabBarBorderSize         = ScaledLineSizeFloor(tabBarBorderSize, style.TabBarBorderSize);
    style.TabBarOverlineSize       = ScaledLineSizeFloor(tabBarOverlineSize, style.TabBarOverlineSize);
    style.SeparatorSize            = ScaledLineSizeFloor(separatorSize, style.SeparatorSize);
    style.SeparatorTextBorderSize  = ScaledLineSizeFloor(separatorTextBorderSize, style.SeparatorTextBorderSize);
    style.DockingSeparatorSize     = ScaledLineSizeFloor(dockingSeparatorSize, style.DockingSeparatorSize);
    style.TreeLinesSize            = ScaledLineSizeFloor(treeLinesSize, style.TreeLinesSize);
    style.InputTextCursorSize      = ScaledLineSizeFloor(inputTextCursorSize, style.InputTextCursorSize);
    style.DragDropTargetBorderSize = ScaledLineSizeFloor(dragDropTargetBorderSize, style.DragDropTargetBorderSize);

    ImGui::GetIO().FontGlobalScale = scale;
    return 0;
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
