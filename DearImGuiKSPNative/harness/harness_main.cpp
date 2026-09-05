// DearImGuiKSPNative — headless smoke-test harness (chunk C3, milestone M2).
//
// Console exe linking the same TUs as the DLL (minus the Unity plugin entry
// file). Proves the context host initializes, runs headless frames, hands out
// a sane font atlas, and shuts down cleanly.
// Prints "HARNESS PASS" and exits 0 on success; exits 1 on the first failure.

#include <cstdio>
#include <cstring>
#include <vector>

#include "ContextHost.h"
#include "imgui.h"
#include "imgui_internal.h" // ImGuiContext::Windows / ImGuiWindow (gradient read-back)
#include "cimgui.h"         // igBegin/igEnd (window submission)
#include "implot.h"

static int Fail(const char* step, int code)
{
    std::printf("HARNESS FAIL: %s (code %d)\n", step, code);
    return 1;
}

int main()
{
    int rc = DearImGuiKSPNative_ContextInit();
    if (rc != 0)
        return Fail("ContextInit", rc);

    // C12: the ImPlot context must exist after init (created in lockstep with
    // the ImGui context).
    if (ImPlot::GetCurrentContext() == nullptr)
        return Fail("ImPlot context after init", 18);
    std::printf("ImPlot context: non-null after init\n");

    // 60 headless frames at 1920x1080, 60 fps.
    for (int i = 0; i < 60; ++i)
    {
        DearImGuiKSPNative_BeginFrame(1920.0f, 1080.0f, 1.0f / 60.0f);
        DearImGuiKSPNative_EndFrame();
    }

    unsigned char* pixels = nullptr;
    int width = 0, height = 0;
    rc = DearImGuiKSPNative_GetFontAtlasPixels(&pixels, &width, &height);
    if (rc != 0)
        return Fail("GetFontAtlasPixels", rc);
    if (pixels == nullptr || width <= 0 || height <= 0)
        return Fail("GetFontAtlasPixels sanity", 2);
    std::printf("Font atlas: %d x %d RGBA32, pixels=%p\n", width, height, (void*)pixels);

    // ---- C9: window-bg gradient descriptor + draw-list vtx-count export ----

    // Null draw list must report -1.
    if (ContextHost_GetDrawListVtxCount(nullptr) != -1)
        return Fail("GetDrawListVtxCount(null)", 10);

    // Warm-up frames: the window's first frames have deferred/auto-fit sizing
    // (AutoFitFrames counts down from creation), so the reference snapshot
    // below (and every later comparison frame) must run with stable geometry.
    for (int i = 0; i < 3; ++i)
    {
        DearImGuiKSPNative_BeginFrame(1920.0f, 1080.0f, 1.0f / 60.0f);
        igBegin("grad-test", nullptr, 0);
        igEnd();
        DearImGuiKSPNative_EndFrame();
    }

    // Stock reference frame (descriptor is disabled by default): the gradient
    // pass must be an exact no-op, so every later comparison targets this.
    DearImGuiKSPNative_BeginFrame(1920.0f, 1080.0f, 1.0f / 60.0f);
    igBegin("grad-test", nullptr, 0);
    igEnd();
    DearImGuiKSPNative_EndFrame();

    ImGuiContext* ctx = ImGui::GetCurrentContext();
    ImGuiWindow* testWindow = nullptr;
    for (int i = 0; i < ctx->Windows.Size; ++i)
    {
        ImGuiWindow* w = ctx->Windows[i];
        // Post-EndFrame the current-frame flag is Active (WasActive is only
        // refreshed at the next NewFrame, imgui.cpp:5979).
        if (w != nullptr && w->Active && std::strcmp(w->Name, "grad-test") == 0)
        {
            testWindow = w;
            break;
        }
    }
    if (testWindow == nullptr)
        return Fail("grad-test window not found", 11);

    const int refVtxCount = testWindow->DrawList->VtxBuffer.Size;
    std::vector<ImU32> refCols;
    for (int i = 0; i < refVtxCount; ++i)
        refCols.push_back(testWindow->DrawList->VtxBuffer.Data[i].col);

    // The vtx-count export must agree with the live draw list.
    if (ContextHost_GetDrawListVtxCount(testWindow->DrawList) != refVtxCount || refVtxCount <= 0)
        return Fail("GetDrawListVtxCount mismatch", 12);

    // Enable a red->blue gradient (alpha 1 both stops) and resubmit the window.
    rc = ContextHost_SetWindowBgGradient(1, 1.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f, 1.0f, 1.0f);
    if (rc != 0)
        return Fail("SetWindowBgGradient enable", rc);

    DearImGuiKSPNative_BeginFrame(1920.0f, 1080.0f, 1.0f / 60.0f);
    igBegin("grad-test", nullptr, 0);
    igEnd();
    DearImGuiKSPNative_EndFrame();

    {
        const ImDrawList* dl = testWindow->DrawList;
        if (dl->VtxBuffer.Size != refVtxCount)
            return Fail("enabled-pass vertex count differs", 13);

        const ImU32 topStop = IM_COL32(255, 0, 0, 255);
        const ImU32 bottomStop = IM_COL32(0, 0, 255, 255);

        // The first command's referenced vertex range, and the white-pixel UV
        // the pass uses to select solid-fill verts (glyph verts sharing the
        // atlas texture are deliberately left untouched so text keeps its
        // color).
        const ImDrawCmd& cmd = dl->CmdBuffer[0];
        int vertEnd = 0;
        for (unsigned int n = 0; n < cmd.ElemCount; ++n)
            if ((int)dl->IdxBuffer.Data[n] + 1 > vertEnd)
                vertEnd = (int)dl->IdxBuffer.Data[n] + 1;
        const ImVec2 whiteUv = dl->_Data->TexUvWhitePixel;

        float minY = 1e30f, maxY = -1e30f;
        for (int i = 0; i < vertEnd; ++i)
        {
            const ImDrawVert& v = dl->VtxBuffer.Data[i];
            if (v.uv.x != whiteUv.x || v.uv.y != whiteUv.y)
                continue;
            if (v.pos.y < minY) minY = v.pos.y;
            if (v.pos.y > maxY) maxY = v.pos.y;
        }

        bool bottomOk = false, topChanged = false, anyChanged = false;
        for (int i = 0; i < refVtxCount; ++i)
        {
            const ImDrawVert& v = dl->VtxBuffer.Data[i];
            const ImU32 col = v.col;
            // KeepAlpha: alpha channel must be untouched everywhere.
            if ((col >> 24) != (refCols[i] >> 24))
                return Fail("gradient changed alpha", 14);
            if (col != refCols[i])
                anyChanged = true;
            const bool shaded = i < vertEnd
                && v.uv.x == whiteUv.x && v.uv.y == whiteUv.y;
            if (shaded)
            {
                // KeepAlpha: the stop comparison is RGB-only (the fill keeps
                // its own alpha, e.g. 0xF0 for stock-dark WindowBg).
                if (v.pos.y == maxY && (col & 0x00FFFFFFu) == (bottomStop & 0x00FFFFFFu))
                    bottomOk = true; // bg bottom lands exactly on the bottom stop
                if (v.pos.y == minY && col != refCols[i])
                    topChanged = true; // top region (title bar or bg top) recolored
            }
            else if (col != refCols[i])
            {
                return Fail("non-solid-fill vert recolored", 16); // text/glyph verts must be untouched
            }
        }
        if (!anyChanged || !topChanged || !bottomOk)
            return Fail("gradient stops not applied", 15);

        // ISSUES #008: the title-bar collapse arrow is a Text-colored solid-fill
        // primitive (white-pixel UV) that merges into command 0; the gradient
        // pass must leave it (and any other Text-colored primitive vert)
        // exactly as the stock frame had it, or it repaints to the gradient top
        // stop and vanishes against the title bar. The reference frame is stock
        // dark, where Text is opaque white — no other command-0 solid fill in a
        // bare window shares that color.
        const ImU32 textCol = ImGui::GetColorU32(ImGuiCol_Text);
        int textVertCount = 0;
        for (int i = 0; i < vertEnd; ++i)
        {
            const ImDrawVert& v = dl->VtxBuffer.Data[i];
            if (v.uv.x != whiteUv.x || v.uv.y != whiteUv.y)
                continue;
            if (refCols[i] != textCol)
                continue;
            textVertCount++;
            if (v.col != refCols[i])
                return Fail("text-colored solid-fill primitive recolored", 21);
        }
        if (textVertCount == 0)
            return Fail("text-colored solid-fill primitive absent from command 0", 20);
        std::printf("Text-colored primitive verts survived the pass unchanged: %d\n", textVertCount);
    }
    std::printf("Gradient enabled: stops applied to bg range, alpha preserved\n");

    // Disable: the pass must no-op — the draw list must return to the stock
    // reference byte-for-byte.
    rc = ContextHost_SetWindowBgGradient(0, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f);
    if (rc != 0)
        return Fail("SetWindowBgGradient disable", rc);

    DearImGuiKSPNative_BeginFrame(1920.0f, 1080.0f, 1.0f / 60.0f);
    igBegin("grad-test", nullptr, 0);
    igEnd();
    DearImGuiKSPNative_EndFrame();

    {
        const ImDrawList* dl = testWindow->DrawList;
        if (dl->VtxBuffer.Size != refVtxCount)
            return Fail("disabled-pass vertex count differs", 16);
        for (int i = 0; i < refVtxCount; ++i)
            if (dl->VtxBuffer.Data[i].col != refCols[i])
                return Fail("disabled pass is not byte-exact", 17);
    }
    std::printf("Gradient disabled: draw list byte-exact vs. stock reference (%d verts)\n", refVtxCount);

    DearImGuiKSPNative_ContextShutdown();

    // C12: the ImPlot context must be gone after shutdown (destroyed before
    // the ImGui context).
    if (ImPlot::GetCurrentContext() != nullptr)
        return Fail("ImPlot context after shutdown", 19);
    std::printf("ImPlot context: null after shutdown\n");

    std::printf("HARNESS PASS\n");
    return 0;
}
