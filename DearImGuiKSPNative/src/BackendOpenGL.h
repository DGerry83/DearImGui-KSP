// DearImGuiKSPNative — OpenGL renderer backend interface (original-plan C5; D37 queue item 1).
//
// Initializes imgui_impl_opengl3 and renders the frame's ImGui draw data inside
// the render-event callback (spec §4.1/§4.2), mirroring BackendD3D11's shape.
// Zero game knowledge; the ImGui context itself stays owned by ContextHost (C3 ABI).
//
// No device discovery: unlike D3D11 (texture->GetDevice) and Vulkan (blocked by
// the D19 LoadLibrary model, D37), GL needs nothing handed over — Unity's GL
// context is current on the render thread when the render event fires, and the
// pinned imgui_impl_opengl3 loads its functions through its embedded loader
// (imgui_impl_opengl3_loader.h; no glad/GLEW dependency).
#pragma once

// Called from the render-event callback (render thread). Completes backend
// init lazily once the ImGui context exists, then renders
// ImGui::GetDrawData() into the render target Unity has bound at event time.
void BackendOpenGL_Render(void);

// Releases the backend (ImGui_ImplOpenGL3_Shutdown). Safe to call when
// nothing was initialized.
void BackendOpenGL_Shutdown(void);
