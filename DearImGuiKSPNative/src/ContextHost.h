// DearImGuiKSPNative — ImGui context host: locked C ABI (chunk C3, milestone M2).
//
// Owns the single ImGui context for the whole DLL, its frame lifecycle, and
// the CPU-side font atlas. Backends (C4 D3D11 / C5 OpenGL) consume this
// interface; they never own the context (spec §4.1). No GPU code lives here.
#pragma once

#ifndef DEARIMGUIKSP_NATIVE_API
#define DEARIMGUIKSP_NATIVE_API extern "C" __declspec(dllexport)
#endif

// imgui.h types used by value/pointer in this interface; ContextHost.cpp and
// the wrapper TU include imgui.h itself.
struct ImDrawList;

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

// Writes ImGui::GetStyle().Colors[idx] (theme presets, spec §5.1). idx is an
// ImGuiCol_* value, bounds-checked against ImGuiCol_COUNT. Returns 0 on success,
// 1 if there is no context, 2 for a bad index (nothing written). Not part of
// the exported C ABI itself — the exported wrapper
// DearImGuiKSPNative_SetStyleColor lives in DearImGuiKSPNative.cpp.
int ContextHost_SetStyleColor(int idx, float r, float g, float b, float a);

// Writes a float ImGuiStyle field selected by an ImGuiStyleVar_* value; the
// switch covers exactly the subset the theme presets use (WindowRounding,
// WindowBorderSize, FrameRounding, FrameBorderSize, GrabRounding, GrabMinSize,
// ScrollbarRounding, TabRounding, ChildRounding, PopupRounding). Unknown idx
// returns 2 and writes nothing; 1 = no context. Not exported directly — the
// wrapper DearImGuiKSPNative_SetStyleVarFloat lives in DearImGuiKSPNative.cpp.
int ContextHost_SetStyleVarFloat(int idx, float v);

// Writes an ImVec2 ImGuiStyle field (WindowPadding, FramePadding, ItemSpacing).
// Same return codes as ContextHost_SetStyleVarFloat. Not exported directly —
// the wrapper DearImGuiKSPNative_SetStyleVarVec2 lives in DearImGuiKSPNative.cpp.
int ContextHost_SetStyleVarVec2(int idx, float x, float y);

// Re-applies the stock ImGui dark style to the live style (spec §5.1: "dark"
// is the exact stock dark, and every theme apply resets to it first so slots
// a preset does not map are never stale). Returns 0 on success, 1 if there is
// no context. Not exported directly — the wrapper
// DearImGuiKSPNative_StyleColorsDark lives in DearImGuiKSPNative.cpp.
int ContextHost_StyleColorsDark(void);

// Sets the two-stop vertical window-background gradient descriptor (C9,
// spec §6.1). enabled != 0 turns on the per-frame EndFrame shading pass; the
// two RGBA float pairs (0–1) are the top/bottom stops. enabled == 0 disables
// the pass, which is then a strict no-op (the "dark" preset depends on that
// for byte-exact stock rendering). Returns 0 on success, 1 if there is no
// context. Not part of the exported C ABI itself — the exported wrapper
// DearImGuiKSPNative_SetWindowBgGradient lives in DearImGuiKSPNative.cpp.
int ContextHost_SetWindowBgGradient(int enabled, float r1, float g1, float b1, float a1, float r2, float g2, float b2, float a2);

// Vertex count of a live ImDrawList. cimgui exports no VtxBuffer accessor
// (verified against cimgui.h: only GetClipRectMin/Max exist), so the DLL
// provides this tiny pass-through for the C9 managed gradient helpers, which
// record before/after counts to shade exactly the verts a fill appended.
// Returns -1 for a null draw list. Not exported directly — the wrapper
// DearImGuiKSPNative_GetDrawListVtxCount lives in DearImGuiKSPNative.cpp.
int ContextHost_GetDrawListVtxCount(ImDrawList* drawList);

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
