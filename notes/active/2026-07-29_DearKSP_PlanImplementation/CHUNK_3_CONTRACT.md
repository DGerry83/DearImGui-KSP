# Chunk Contract: imgui/cimgui Build Integration + Context Host + Font Atlas

## Plan: Dear KSP Implementation
## Date: 2026-07-29
## Chunk ID: C3
## Advances Milestone: M2

### Scope

- Compile Dear ImGui 1.92.9 + cimgui directly into `DearKSPNative.dll` from the sibling clone at `C:\Users\Matt\source\repos\cimgui` (imgui submodule pin — do not update it).
- Add a **context host** to the native Core: a new source file owning the single ImGui context lifecycle with a pure C ABI for the rest of the DLL.
- Font atlas: build the CPU-side atlas from ImGui's embedded default font (ProggyClean — `io.Fonts->AddFontDefault()`), expose the RGBA pixels for backends to upload in C4/C5. **No GPU work in this chunk.**
- A minimal native smoke-test harness (console exe, no Unity) proving the context host creates a context, runs headless frames, and shuts down cleanly.
- Update both build scripts to compile the imgui/cimgui translation units and the new sources.

### Inputs (must exist before starting)

- C2 done: Unity headers in `DearKSPNative/include/`, export surface pattern established.
- cimgui clone at `C:\Users\Matt\source\repos\cimgui` with imgui 1.92.9 submodule checked out.

### Outputs (must be created/changed)

- New: `DearKSPNative/src/ContextHost.h`, `DearKSPNative/src/ContextHost.cpp` — the C ABI + implementation.
- New: `DearKSPNative/harness/` + `DearKSPNative/build_harness.bat` — console smoke test (CinematicRecorder convention).
- Modified: `DearKSPNative/build.bat`, `DearKSPNative/build_release.bat` — add imgui/cimgui TUs, new sources, include paths.
- `DearKSPNative/src/DearKSPNative.cpp` — only if wiring is needed to expose context-host exports; keep changes minimal.

### Context Host C ABI (locked — C4/C5 consume this)

| Export | Signature | Purpose |
|--------|-----------|---------|
| `DearKSPNative_ContextInit` | `int (void)` — 0 on success, nonzero on failure | Creates the ImGui context, applies stock dark style, builds the ProggyClean atlas |
| `DearKSPNative_ContextShutdown` | `void (void)` | Destroys the context and frees atlas CPU data |
| `DearKSPNative_BeginFrame` | `void (float width, float height, float deltaSeconds)` | Sets display size/delta, calls `ImGui::NewFrame()` |
| `DearKSPNative_EndFrame` | `void (void)` | Calls `ImGui::Render()`; draw data remains available to backends |
| `DearKSPNative_GetFontAtlasPixels` | `int (unsigned char** outPixels, int* outWidth, int* outHeight)` — 0 on success | RGBA32 atlas for backend texture upload (C4/C5) |
| `DearKSPNative_SetDemoWindowVisible` | `void (int visible)` | Toggles the ImGui demo window for the PoC |

### Constraints

- Pure C ABI on the context host; backends must consume, never own, the context.
- No `imgui_impl_*` backend files yet (C4/C5). No GPU/device code. No managed changes.
- C++17, MSVC `cl.exe` batch builds only; warnings must stay at 0 (imgui compiles clean at `/W3`).
- Do not modify files under `C:\Users\Matt\source\repos\cimgui` — treat as read-only.
- Keep object files out of the source tree (`/Fo:build\`).

### Verification

- `build.bat` compiles 0 errors / 0 warnings with all imgui/cimgui TUs; DLL deploys to `GameData/DearKSP/PluginData/`.
- `dumpbin /exports` additionally lists the six context-host exports.
- `build_harness.bat` produces a harness exe that runs: ContextInit → several BeginFrame/EndFrame cycles with demo window on → GetFontAtlasPixels returns sane dimensions → ContextShutdown, printing PASS and exiting 0.
- `dotnet build DearKSP.slnx` still clean (no managed edits).

### Rollback

- `git checkout -- DearKSPNative/` and delete untracked new files under `DearKSPNative/`.
