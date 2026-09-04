// DearImGuiKSPNative — imgui-wheels C ABI shim (chunk C15, milestone M5).
// See imgui_wheels_shim.h for the contract.
#include "imgui_wheels_shim.h"

#include "imgui-wheels.h"

int DK_WheelFloat(
        const char* label,
        float* value,
        float v_min,
        float v_max,
        float size_x,
        float size_y,
        int orientation,
        const char* format,
        float speed,
        int invert_colors)
{
    if (value == nullptr)
    {
        return 0;
    }
    return ImGuiWheels::WheelFloat(
                   label,
                   value,
                   v_min,
                   v_max,
                   ImVec2(size_x, size_y),
                   orientation,
                   format != nullptr ? format : "%.2f",
                   speed,
                   invert_colors != 0)
                   ? 1
                   : 0;
}

int DK_WheelInt(
        const char* label,
        int* value,
        int v_min,
        int v_max,
        float size_x,
        float size_y,
        int orientation,
        const char* format,
        float speed,
        int invert_colors)
{
    if (value == nullptr)
    {
        return 0;
    }
    return ImGuiWheels::WheelInt(
                   label,
                   value,
                   v_min,
                   v_max,
                   ImVec2(size_x, size_y),
                   orientation,
                   format != nullptr ? format : "%d",
                   speed,
                   invert_colors != 0)
                   ? 1
                   : 0;
}
