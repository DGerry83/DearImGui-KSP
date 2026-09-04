// DearImGuiKSPNative — imgui_toggle C ABI shim (chunk C10, milestone M3).
// See imgui_toggle_shim.h for the contract.
#include "imgui_toggle_shim.h"

#include "imgui_toggle.h"

int DK_Toggle(const char* label, int* value)
{
    bool v = value != nullptr && *value != 0;
    const bool changed = ImGui::Toggle(label, &v);
    if (value != nullptr)
    {
        *value = v ? 1 : 0;
    }
    return changed ? 1 : 0;
}

int DK_ToggleFlags(const char* label, int* value, int flags)
{
    bool v = value != nullptr && *value != 0;
    const bool changed = ImGui::Toggle(label, &v, flags);
    if (value != nullptr)
    {
        *value = v ? 1 : 0;
    }
    return changed ? 1 : 0;
}
