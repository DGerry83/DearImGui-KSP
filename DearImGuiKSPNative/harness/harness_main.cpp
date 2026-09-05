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

        // C32: the resize grip is the same kind of merged solid-fill primitive,
        // drawn by Begin into command 0 with one of three state colors
        // (imgui.cpp:7406, 7728-7743). The gradient pass must leave all three
        // byte-unchanged, otherwise the grip repaints to the bottom stop — the
        // window bg color at the bottom-right corner — and is invisible.
        //   idle:    grip 0 of a top-level window is always drawn (imgui.cpp:7405)
        //   hovered: mouse over the bottom-right corner, button up
        //   active:  same mouse, button held (resize engaged)
        // Existence of a post-pass vert with the exact state color IS the
        // byte-survival proof: a shaded vert would carry a gradient stop color
        // (the harness gradient is red->blue alpha 255, never a grip color).
        const ImU32 gripIdleCol    = ImGui::GetColorU32(ImGuiCol_ResizeGrip);
        const ImU32 gripHoveredCol = ImGui::GetColorU32(ImGuiCol_ResizeGripHovered);
        const ImU32 gripActiveCol  = ImGui::GetColorU32(ImGuiCol_ResizeGripActive);

        // Count solid-fill verts inside command 0's referenced range that carry
        // exactly <col> after EndFrame's gradient pass.
        auto countGripColoredVerts = [&](ImU32 col) -> int
        {
            const ImDrawList* dl = testWindow->DrawList;
            if (dl->CmdBuffer.Size == 0)
                return 0;
            const ImDrawCmd& c = dl->CmdBuffer[0];
            int end = 0;
            for (unsigned int n = 0; n < c.ElemCount; ++n)
                if ((int)dl->IdxBuffer.Data[c.IdxOffset + n] + 1 > end)
                    end = (int)dl->IdxBuffer.Data[c.IdxOffset + n] + 1;
            const ImVec2 uv = dl->_Data->TexUvWhitePixel;
            int count = 0;
            for (int i = (int)c.VtxOffset; i < (int)c.VtxOffset + end; ++i)
            {
                const ImDrawVert& v = dl->VtxBuffer.Data[i];
                if (v.uv.x == uv.x && v.uv.y == uv.y && v.col == col)
                    count++;
            }
            return count;
        };
        // Submit one frame with the given input state (gradient descriptor
        // still enabled); FeedFrameInput queues the events NewFrame consumes.
        auto submitStateFrame = [](float mouseX, float mouseY, int buttons)
        {
            DearImGuiKSPNative_FeedFrameInput(mouseX, mouseY, 0.0f, buttons, 0, nullptr);
            DearImGuiKSPNative_BeginFrame(1920.0f, 1080.0f, 1.0f / 60.0f);
            igBegin("grad-test", nullptr, 0);
            igEnd();
            DearImGuiKSPNative_EndFrame();
        };

        // Idle state: the enabled frame above ran with the mouse never fed
        // (io.MousePos unset), so grip 0 was drawn in ResizeGrip. Strong check
        // against the stock reference: every vert that WAS grip-colored must
        // still be exactly that color.
        {
            const ImDrawList* dl = testWindow->DrawList;
            const ImDrawCmd& c = dl->CmdBuffer[0];
            int end = 0;
            for (unsigned int n = 0; n < c.ElemCount; ++n)
                if ((int)dl->IdxBuffer.Data[c.IdxOffset + n] + 1 > end)
                    end = (int)dl->IdxBuffer.Data[c.IdxOffset + n] + 1;
            const ImVec2 uv = dl->_Data->TexUvWhitePixel;
            int idleCount = 0;
            for (int i = 0; i < end; ++i)
            {
                const ImDrawVert& v = dl->VtxBuffer.Data[i];
                if (v.uv.x != uv.x || v.uv.y != uv.y || refCols[i] != gripIdleCol)
                    continue;
                idleCount++;
                if (v.col != refCols[i])
                    return Fail("idle resize grip recolored by the gradient pass", 43);
            }
            if (idleCount == 0)
                return Fail("idle resize grip absent from command 0", 40);
            std::printf("Idle grip verts survived the pass unchanged: %d\n", idleCount);
        }

        // Hovered state: mouse just inside the bottom-right corner, button up.
        {
            const ImVec2 corner(testWindow->Pos.x + testWindow->Size.x,
                                testWindow->Pos.y + testWindow->Size.y);
            submitStateFrame(corner.x - 3.0f, corner.y - 3.0f, 0);
            if (countGripColoredVerts(gripHoveredCol) == 0)
                return Fail("hovered resize grip absent/recolored in command 0", 41);
            std::printf("Hovered grip verts survived the pass unchanged: %d\n",
                        countGripColoredVerts(gripHoveredCol));
        }

        // Active state: same mouse, left button held (resize engaged).
        {
            const ImVec2 corner(testWindow->Pos.x + testWindow->Size.x,
                                testWindow->Pos.y + testWindow->Size.y);
            submitStateFrame(corner.x - 3.0f, corner.y - 3.0f, 1);
            if (countGripColoredVerts(gripActiveCol) == 0)
                return Fail("active resize grip absent/recolored in command 0", 42);
            std::printf("Active grip verts survived the pass unchanged: %d\n",
                        countGripColoredVerts(gripActiveCol));
        }

        // Settle: release the button and move the mouse away so the resize id
        // clears and the grip returns to its idle color; the disable-pass
        // byte-exact check below then runs in the same state as the reference.
        submitStateFrame(0.0f, 0.0f, 0);
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

    // ---- C31: live UI scale ----
    //
    // The managed ThemeEngine contract: a whole-style reset (defaults restored)
    // BEFORE every SetUiScale call, so ScaleAllSizes always multiplies defaults
    // and repeated applies cannot compound. Prove both halves here via the
    // context-host entry point (the harness links ContextHost.cpp, not the
    // exported wrapper TU).

    // Baseline reset, then 1.5: default WindowPadding (8,8) -> (12,12);
    // FontGlobalScale is absolute.
    rc = ContextHost_StyleColorsDark();
    if (rc != 0)
        return Fail("StyleColorsDark (ui-scale baseline)", rc);
    rc = ContextHost_SetUiScale(1.5f);
    if (rc != 0)
        return Fail("SetUiScale(1.5)", rc);
    {
        const ImGuiStyle& s = ImGui::GetStyle();
        if (s.WindowPadding.x != 12.0f || s.WindowPadding.y != 12.0f)
            return Fail("SetUiScale(1.5) WindowPadding != (12,12)", 30);
        if (s.WindowRounding != 0.0f || s.FramePadding.x != 6.0f || s.ItemSpacing.x != 12.0f)
            return Fail("SetUiScale(1.5) other size vars wrong", 31);
        if (ImGui::GetIO().FontGlobalScale != 1.5f)
            return Fail("SetUiScale(1.5) FontGlobalScale", 32);
    }
    std::printf("UiScale 1.5: WindowPadding (8,8)->(12,12), FontGlobalScale 1.5\n");

    // Non-positive scale is rejected and writes nothing.
    rc = ContextHost_SetUiScale(0.0f);
    if (rc != 2)
        return Fail("SetUiScale(0) should return 2", 33);
    if (ImGui::GetIO().FontGlobalScale != 1.5f || ImGui::GetStyle().WindowPadding.x != 12.0f)
        return Fail("SetUiScale(0) wrote something", 34);

    // Re-apply at 1.0 after a fresh reset restores the defaults exactly.
    rc = ContextHost_StyleColorsDark();
    if (rc != 0)
        return Fail("StyleColorsDark (ui-scale restore)", rc);
    rc = ContextHost_SetUiScale(1.0f);
    if (rc != 0)
        return Fail("SetUiScale(1.0)", rc);
    {
        const ImGuiStyle& s = ImGui::GetStyle();
        const ImGuiStyle def; // default-constructed = stock sizes
        if (s.WindowPadding.x != def.WindowPadding.x || s.WindowPadding.y != def.WindowPadding.y)
            return Fail("SetUiScale(1.0) did not restore WindowPadding", 35);
        if (s.WindowRounding != def.WindowRounding || s.FramePadding.x != def.FramePadding.x ||
            s.ItemSpacing.y != def.ItemSpacing.y || s.GrabMinSize != def.GrabMinSize)
            return Fail("SetUiScale(1.0) did not restore size vars", 36);
        if (ImGui::GetIO().FontGlobalScale != 1.0f)
            return Fail("SetUiScale(1.0) FontGlobalScale", 37);
    }
    std::printf("UiScale 1.0 after reset: defaults restored exactly\n");

    // The managed pattern (reset + apply) repeated at 1.5 must land on exactly
    // (12,12) every time — no compounding across applies.
    for (int i = 0; i < 3; ++i)
    {
        rc = ContextHost_StyleColorsDark();
        if (rc != 0)
            return Fail("StyleColorsDark (ui-scale repeat)", rc);
        rc = ContextHost_SetUiScale(1.5f);
        if (rc != 0)
            return Fail("SetUiScale(1.5 repeat)", rc);
        if (ImGui::GetStyle().WindowPadding.x != 12.0f || ImGui::GetStyle().WindowPadding.y != 12.0f)
            return Fail("compounding detected across repeated applies", 38);
    }
    std::printf("UiScale 1.5 x3 with resets: (12,12) every time (no compounding)\n");

    DearImGuiKSPNative_ContextShutdown();

    // C12: the ImPlot context must be gone after shutdown (destroyed before
    // the ImGui context).
    if (ImPlot::GetCurrentContext() != nullptr)
        return Fail("ImPlot context after shutdown", 19);
    std::printf("ImPlot context: null after shutdown\n");

    std::printf("HARNESS PASS\n");
    return 0;
}
