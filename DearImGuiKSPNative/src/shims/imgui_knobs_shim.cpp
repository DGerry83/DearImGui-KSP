// DearImGuiKSPNative — imgui-knobs C ABI shim (chunk C15, milestone M5).
// See imgui_knobs_shim.h for the contract.
#include "imgui_knobs_shim.h"

#include "imgui-knobs.h"

int DK_Knob(
        const char* label,
        float* value,
        float v_min,
        float v_max,
        float speed,
        const char* format,
        int variant,
        float size,
        int flags,
        int steps)
{
    if (value == nullptr)
    {
        return 0;
    }
    return ImGuiKnobs::Knob(
                   label,
                   value,
                   v_min,
                   v_max,
                   speed,
                   format != nullptr ? format : "%.3f",
                   variant,
                   size,
                   flags,
                   steps)
                   ? 1
                   : 0;
}

int DK_KnobInt(
        const char* label,
        int* value,
        int v_min,
        int v_max,
        float speed,
        const char* format,
        int variant,
        float size,
        int flags,
        int steps)
{
    if (value == nullptr)
    {
        return 0;
    }
    return ImGuiKnobs::KnobInt(
                   label,
                   value,
                   v_min,
                   v_max,
                   speed,
                   format != nullptr ? format : "%i",
                   variant,
                   size,
                   flags,
                   steps)
                   ? 1
                   : 0;
}
