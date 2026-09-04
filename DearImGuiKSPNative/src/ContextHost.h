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
// clamped to a small positive minimum. No-op before ContextInit.
DEARIMGUIKSP_NATIVE_API void DearImGuiKSPNative_BeginFrame(float width, float height, float deltaSeconds);

// Calls ImGui::Render(); draw data remains available to backends until the
// next BeginFrame. No-op before ContextInit.
DEARIMGUIKSP_NATIVE_API void DearImGuiKSPNative_EndFrame(void);

// RGBA32 font atlas pixels for backend texture upload (C4/C5). Returns 0 on
// success, nonzero if uninitialized, the args are null, or the atlas is empty.
DEARIMGUIKSP_NATIVE_API int DearImGuiKSPNative_GetFontAtlasPixels(unsigned char** outPixels, int* outWidth, int* outHeight);

// Mouse/keyboard capture state from ImGui IO (spec §5.3). Writes 1/0 to each
// out-arg when the context exists; writes 0/0 otherwise. Null pointers are safe.
DEARIMGUIKSP_NATIVE_API void DearImGuiKSPNative_GetIoCaptureState(int* wantMouse, int* wantKeyboard);

// Queues one frame of mouse/keyboard input into the ImGui IO event queue before
// NewFrame (spec §5.3 addendum). mouseButtons/keyBits are bitmasks; utf8Chars is
// a null-terminated UTF-8 string (may be null). No-op before ContextInit.
DEARIMGUIKSP_NATIVE_API void DearImGuiKSPNative_FeedFrameInput(float mouseX, float mouseY, float wheel, int mouseButtons, int keyBits, const char* utf8Chars);

// Clamps every visible, active window of the current context fully into
// the PASSED viewport size (ISSUES #002): pos = clamp(pos, 0, max(0,
// viewport - size)). width/height are the live new-viewport values; never
// read io.DisplaySize here — it can still hold the pre-change size when a
// resolution reduction is being handled. Windows larger than the viewport
// pin to the top-left corner. No-op before ContextInit. Not part of the
// exported C ABI itself — the exported wrapper
// DearImGuiKSPNative_ClampWindowsToViewport lives in DearImGuiKSPNative.cpp.
void ContextHost_ClampWindowsToViewport(float width, float height);

// Loads a font file into the context atlas (spec §4.2). Legal only before
// the first NewFrame — afterwards the atlas is built and locked. Returns
// 0 on success, 1 if there is no context, 2 if frames have already begun,
// 3 if AddFontFromFileTTF failed (the atlas is left exactly as it was).
// May be called multiple times before the first frame (Plex Sans Regular,
// then Medium); each call appends one font. The atlas itself is NOT built
// here — NewFrame builds it lazily (BackendFlags, imgui 1.92.9). Not part
// of the exported C ABI itself — the exported wrapper
// DearImGuiKSPNative_LoadFontFromFile lives in DearImGuiKSPNative.cpp.
int ContextHost_LoadFontFromFile(const char* utf8Path, float sizePixels);
