// DearImGuiKSPNative — ImGui context host: locked C ABI (chunk C3, milestone M2).
//
// Owns the single ImGui context for the whole DLL, its frame lifecycle, and
// the CPU-side font atlas. Backends (C4 D3D11 / C5 OpenGL) consume this
// interface; they never own the context (spec §4.1). No GPU code lives here.
#pragma once

#ifndef DEARIMGUIKSP_NATIVE_API
#define DEARIMGUIKSP_NATIVE_API extern "C" __declspec(dllexport)
#endif

// Creates the ImGui context, applies the stock dark style, and builds the
// embedded-default-font (ProggyClean) atlas. Returns 0 on success, nonzero on
// failure. Idempotent: calling again while initialized returns 0.
DEARIMGUIKSP_NATIVE_API int DearImGuiKSPNative_ContextInit(void);

// Destroys the context and frees the atlas CPU data. No-op if not initialized.
DEARIMGUIKSP_NATIVE_API void DearImGuiKSPNative_ContextShutdown(void);

// Sets display size / delta time and calls ImGui::NewFrame(). deltaSeconds is
// clamped to a small positive minimum. Shows the demo window when toggled on.
// No-op before ContextInit.
DEARIMGUIKSP_NATIVE_API void DearImGuiKSPNative_BeginFrame(float width, float height, float deltaSeconds);

// Calls ImGui::Render(); draw data remains available to backends until the
// next BeginFrame. No-op before ContextInit.
DEARIMGUIKSP_NATIVE_API void DearImGuiKSPNative_EndFrame(void);

// RGBA32 font atlas pixels for backend texture upload (C4/C5). Returns 0 on
// success, nonzero if uninitialized, the args are null, or the atlas is empty.
DEARIMGUIKSP_NATIVE_API int DearImGuiKSPNative_GetFontAtlasPixels(unsigned char** outPixels, int* outWidth, int* outHeight);

// Toggles the ImGui demo window for the PoC. Safe to call before ContextInit.
DEARIMGUIKSP_NATIVE_API void DearImGuiKSPNative_SetDemoWindowVisible(int visible);
