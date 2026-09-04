// DearImGuiKSPNative — imgui-wheels C ABI shim (chunk C15, milestone M5).
//
// Thin extern "C" pass-throughs over the vendored Engineer162/imgui-wheels
// extension (vendor/imgui-wheels/, pinned in vendor/PIN_RECORD.md). Bools are
// 1-byte C++ bools on the extension side, so the ABI surface uses int and the
// shim owns the bool<->int conversion. The ImVec2 layout box crosses the ABI
// as two floats (size_x, size_y) to avoid struct-layout coupling. No
// KSP/Unity knowledge; compiles against the pinned cimgui/imgui 1.92.9 tree.
// Scalar float/int entry points only (spec §4.1).
#pragma once

#ifndef DEARIMGUIKSP_WHEELS_SHIM_API
#define DEARIMGUIKSP_WHEELS_SHIM_API extern "C" __declspec(dllexport)
#endif

// Draws ImGuiWheels::WheelFloat (imgui-wheels.h:31). orientation is the raw
// int of ImGuiWheels::WheelOrientation_ (0 = horizontal, 1 = vertical);
// format may be NULL for the widget default ("%.2f"). invert_colors is 0/1.
// Returns 1 when the value changed this frame, else 0.
DEARIMGUIKSP_WHEELS_SHIM_API int DK_WheelFloat(
        const char* label,
        float* value,
        float v_min,
        float v_max,
        float size_x,
        float size_y,
        int orientation,
        const char* format,
        float speed,
        int invert_colors);

// Same for the integer overload ImGuiWheels::WheelInt (imgui-wheels.h:34).
// format may be NULL for the widget default ("%d").
DEARIMGUIKSP_WHEELS_SHIM_API int DK_WheelInt(
        const char* label,
        int* value,
        int v_min,
        int v_max,
        float size_x,
        float size_y,
        int orientation,
        const char* format,
        float speed,
        int invert_colors);
