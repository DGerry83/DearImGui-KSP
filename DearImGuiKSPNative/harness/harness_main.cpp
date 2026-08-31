// DearImGuiKSPNative — headless smoke-test harness (chunk C3, milestone M2).
//
// Console exe linking the same TUs as the DLL (minus the Unity plugin entry
// file). Proves the context host initializes, runs headless frames with the
// demo window on, hands out a sane font atlas, and shuts down cleanly.
// Prints "HARNESS PASS" and exits 0 on success; exits 1 on the first failure.

#include <cstdio>

#include "ContextHost.h"

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

    DearImGuiKSPNative_SetDemoWindowVisible(1);

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

    DearImGuiKSPNative_ContextShutdown();

    std::printf("HARNESS PASS\n");
    return 0;
}
