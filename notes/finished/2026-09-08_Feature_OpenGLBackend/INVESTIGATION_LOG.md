# Investigation Log: OpenGL Render Backend (C5/AC2, D37 queue item 1)
## Date: 2026-09-08
## Status: Scope Checked
## Type: Feature

### Symptom / Scope Profile
- **Primary Goal**: Add the OpenGL renderer backend to DearImGuiKSPNative so the library works when KSP runs on OpenGL Core (`-force-glcore`), per D37 (post-release graphics-API queue, OpenGL first), unblocking AC2 (deferred per D20/D35).
- **User framing**: Work happens on branch `feature-OpenGL`, committed along the way, PR-merged to main when fully ready.
- **Boundaries (in scope)**:
  - Native: new GL backend (imgui_impl_opengl3 from the pinned cimgui clone, imgui 1.92.9), render-event routing between backends, build scripts, handshake bump (new export → v8, D17 lockstep).
  - Managed (Infrastructure only): device gate accepts OpenGLCore alongside D3D11; GL path skips the D3D11 texture handoff; failure-text wording update.
  - Docs: `docs/70-troubleshooting.md` D3D11-only rows; `DearImGuiKSPNative/README.md` link-list/GL note.
- **Boundaries (out of scope)**:
  - Docking (#011), Metal, Vulkan (D37 queue order).
  - Package version bump / release packaging (release-time decision; expected 1.1.0 minor).
  - OpenGL ES / legacy GL (Unity 2019.4 Windows only produces OpenGLCore).
  - Changes to widgets/extensions/theming — all draw-list-level, backend-agnostic (D35).
- **Success criteria**: frozen GATES.md G1–G7; in-game AC2 on the GL test instance.

### Environment
- GL test instance exists and is the default build target: `C:\SSDGames\DearImGui-KSP_TESTING` (stripped-down GL-capable; Deferred, TUFX, Scatterer, ParallaxContinued present; CinematicShaders/CinematicRecorder deliberately absent — they fail under GL, D20). See `notes\knowledge\ENVIRONMENT.md`.
- Pinned cimgui clone (`..\..\cimgui`, imgui 1.92.9) already carries `imgui_impl_opengl3.cpp/.h` + `imgui_impl_opengl3_loader.h` (embedded loader — no glad/GLEW dependency).
- Pinned `imgui_impl_opengl3.cpp` confirmed: `ImGui_ImplOpenGL3_Init()` auto-runs the embedded loader, sets `RendererHasTextures` (1.92.9 dynamic font atlas via `draw_data->Textures` — same protocol the D3D11 path relies on), and backs up/restores GL state incl. `GL_UNPACK_ROW_LENGTH`/`GL_UNPACK_ALIGNMENT` in `UpdateTexture`.

### Current Architecture (verified by reading)
- `DearImGuiKSPNative.cpp`: `OnRenderEvent(0)` routes unconditionally to `BackendD3D11_Render()`; header comment already anticipates "C5 adds OpenGL".
- `BackendD3D11.cpp/.h`: self-contained backend — init-from-texture (device discovery), lazy bring-up at first render event (needs live ImGui context), failure latch + one-shot diagnostic (G3-03), frame-lock around the draw (ISSUES #004), guarded shutdown. **This is the shape the GL backend mirrors.**
- `ContextHost` owns the ImGui context, frame lock, and diagnostics buffer — backend-agnostic, no changes expected.
- Managed `NativeBridge.Initialize` (DearImGuiKSP/Infrastructure/NativeBridge.cs:142): hard gate `graphicsDeviceType != Direct3D11` → `InitErrUnsupportedDevice` → `FailureKind.GraphicsApi` popup (AC8 path, previously verified via `-force-glcore` — that saboteur now becomes the success path).
- Device texture handoff (NativeBridge.cs:152-153) is D3D11-specific; GL needs no device discovery (GL context is current at render-event time — no IUnityInterfaces equivalent needed, unlike Vulkan per D37).
- Render pump (`DearImGuiKSPAddon.cs:144`) issues `GL.IssuePluginEvent(RenderEventFunc, 0)` every EndOfFrame — API-agnostic, no change expected.
- Handshake convention (CHUNK_C04_CONTRACT): any contract adding exports bumps the pair; current = 7 → this work adds an export → **8**.

### Data Loss / Risk Assessment
- **Corruption Risk**: No (no persisted state touched; settings.cfg schema unchanged).
- **Change Risk Level**: Medium — native per-frame render path + new device class, but isolated to a new backend file + one routing branch; D3D11 path behaviorally untouched.
- **Key technical risks**:
  - R1: GL state pollution of the game/co-mods (backend runs inside Unity's GL render pass). Mitigation: imgui_impl_opengl3's backup/restore block; in-game validation with Scatterer/Parallax/TUFX active (G7).
  - R2: Unity GLCore render-event timing/context currency. Standard `GL.IssuePluginEvent` semantics carry over; in-game validation (G5) is the proof.
  - R3: GLSL version detection on Unity GLCore — backend auto-detects (330 core on GL 3.3+, falls back to 150/130). Low.
  - R4: AC8 failure-path regression — the updated gate must still reject everything that is not D3D11/OpenGLCore with exactly one popup (G6).

### Fix / Feature Candidates
- **Candidate 1 (chosen)**: Mirror-backend — new `BackendOpenGL.cpp/.h` sibling with the D3D11 backend's interface shape (Init marker / Render / Shutdown), active-backend routing in `OnRenderEvent`, explicit managed→native GL selection export.
  - **Pros**: isolated failure domain (CHUNK_MAP rationale for C5≠C4); D3D11 code paths untouched; minimal diff; matches documented GL design (imgui_impl_opengl3 + embedded loader + 1.92.9 texture protocol).
  - **Cons**: two backend files with similar structure (accepted — they change for different reasons, DRY exception per CORE_PROTOCOLS §5.5).
- **Candidate 2 (rejected)**: Single backend file with per-API branches.
  - Pros: one file. Cons: edits tested D3D11 code (violates Open/Closed), couples two failure domains, larger review surface.
- **Candidate 3 (rejected)**: Infer GL mode natively (no new export; "no D3D11 texture arrived ⇒ GL").
  - Pros: no handshake bump. Cons: implicit over explicit (§5.5); a failed D3D11 handoff would silently fall through to GL rendering — wrong failure semantics for AC8.
