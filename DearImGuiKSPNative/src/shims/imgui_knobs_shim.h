// DearImGuiKSPNative — imgui-knobs C ABI shim (chunk C15, milestone M5).
//
// Thin extern "C" pass-throughs over the vendored altschuler/imgui-knobs
// extension (vendor/imgui-knobs/, pinned in vendor/PIN_RECORD.md). Bools are
// 1-byte C++ bools on the extension side, so the ABI surface uses int and the
// shim owns the bool<->int conversion. No KSP/Unity knowledge; compiles
// against the pinned cimgui/imgui 1.92.9 tree. Float + int scalar overloads
// only (spec §4.1) — the color_set struct stays extension-internal.
#pragma once

#ifndef DEARIMGUIKSP_KNOBS_SHIM_API
#define DEARIMGUIKSP_KNOBS_SHIM_API extern "C" __declspec(dllexport)
#endif

// Draws ImGuiKnobs::Knob (float overload, imgui-knobs.h:47). variant is the
// raw int of ImGuiKnobVariant_, flags the raw bitmask of ImGuiKnobFlags_;
// both are forwarded unchanged. format may be NULL for the widget default
// ("%.3f"). *value is clamped/updated by the drag. Returns 1 when the value
// changed this frame, else 0.
DEARIMGUIKSP_KNOBS_SHIM_API int DK_Knob(
        const char* label,
        float* value,
        float v_min,
        float v_max,
        float speed,
        const char* format,
        int variant,
        float size,
        int flags,
        int steps);

// Same for the integer overload ImGuiKnobs::KnobInt (imgui-knobs.h:60).
// format may be NULL for the widget default ("%i").
DEARIMGUIKSP_KNOBS_SHIM_API int DK_KnobInt(
        const char* label,
        int* value,
        int v_min,
        int v_max,
        float speed,
        const char* format,
        int variant,
        float size,
        int flags,
        int steps);
