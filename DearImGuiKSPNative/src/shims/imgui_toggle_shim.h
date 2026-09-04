// DearImGuiKSPNative — imgui_toggle C ABI shim (chunk C10, milestone M3).
//
// Thin extern "C" pass-throughs over the vendored cmdwtf/imgui_toggle
// extension (vendor/imgui_toggle/, pinned in vendor/PIN_RECORD.md). Bools are
// 1-byte C++ bools on the extension side, so the ABI surface uses int and the
// shim owns the bool<->int conversion. No KSP/Unity knowledge; compiles
// against the pinned cimgui/imgui 1.92.9 tree. Scalar/flag overloads only
// (spec §4.1, D27) — no config-struct or preset overloads.
#pragma once

#ifndef DEARIMGUIKSP_TOGGLE_SHIM_API
#define DEARIMGUIKSP_TOGGLE_SHIM_API extern "C" __declspec(dllexport)
#endif

// Draws imgui_toggle's default toggle. *value is 0 (off) or nonzero (on) on
// entry and exit. Returns 1 when the value changed this frame, else 0.
DEARIMGUIKSP_TOGGLE_SHIM_API int DK_Toggle(const char* label, int* value);

// Same, with ImGuiToggleFlags modes (imgui_toggle.h: ImGuiToggleFlags_).
// flags is the raw int bitmask; the shim forwards it unchanged. Returns 1
// when the value changed this frame, else 0.
DEARIMGUIKSP_TOGGLE_SHIM_API int DK_ToggleFlags(const char* label, int* value, int flags);
