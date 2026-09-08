# Architecture Contract: OpenGL Render Backend
## Date: 2026-09-08
## Type: Feature
## Related Investigation: [INVESTIGATION_LOG.md](INVESTIGATION_LOG.md)

### Change Specifics
- **Feature Scope**: Add the OpenGL backend so the library runs on KSP launched with `-force-glcore` (GraphicsDeviceType.OpenGLCore), per D37 queue item 1, satisfying the deferred AC2. D3D11 behavior must be byte-for-byte unaffected. Branch `feature-OpenGL`; commits land incrementally; PR merge only when all gates pass.

### Structural Invariants
1. **D3D11 path untouched behaviorally**: `BackendD3D11.cpp/.h` unchanged; existing D3D11 init order (texture handoff → ContextInit → lazy bring-up) preserved.
2. **Isolated failure domains** (CHUNK_MAP C5≠C4 rationale): a GL bring-up failure latches and disables GL rendering with one diagnostic; it never disturbs a working D3D11 session (different process runs) and never retries/flaps (mirror the G3-03 latch).
3. **Context ownership stays with ContextHost** (C3 ABI): backends consume `ImGui::GetCurrentContext()`, the frame lock, and the diagnostics channel; they never own the context or call `ImGui::NewFrame`.
4. **Explicit backend selection**: managed declares the device kind via the matching export; no native inference from "absence of a D3D11 texture".
5. **Handshake lockstep (D17)**: new export `DearImGuiKSPNative_SetOpenGLBackend` ⇒ native `GetVersion()` 7→8 and managed `ExpectedNativeVersion` 7→8 in the same commit.
6. **Frame-lock discipline (ISSUES #004)**: `BackendOpenGL_Render` holds `ContextHost_LockFrame`/`UnlockFrame` exactly like the D3D11 render body, on every exit path.
7. **Failure handling per spec §5.4/§7**: unsupported API still yields Failed state + one popup + detailed log; wording updated to "requires D3D11 or OpenGL (Core)".
8. **Native Interop & Hot-Path Checklist verdicts** (CORE_PROTOCOLS §5.9):
   - Check 1 (process-global state): no OS-global mutation. GL context state is Unity's render-thread state — imgui_impl_opengl3 backs up/restores around the draw (incl. UNPACK_ROW_LENGTH/ALIGNMENT in UpdateTexture, verified in the pinned source); residual pollution risk is in-game validated (G7).
   - Check 2 (hot-path allocation): `BackendOpenGL_Render` runs per frame; adds zero allocations of its own (device objects are created once at lazy bring-up; texture creates happen only on atlas updates via the 1.92.9 texture protocol). Managed device gate is init-time only — no per-frame managed change.
   - Check 3 (resource symmetry): GL device objects (shader, VBO/EBO/VAO, textures) are released in `BackendOpenGL_Shutdown`; a failed lazy bring-up runs `ImGui_ImplOpenGL3_Shutdown` before latching (mirrors BackendD3D11 TryInitBackend failure path); shutdown is guarded (backend-up && context live) because `ImGui_ImplOpenGL3_Shutdown` asserts otherwise.

### Files to Modify
| File | Change Type | Invariants Applied | Risk Level | Lines Affected (Est.) |
|------|-------------|-------------------|------------|---------------------|
| `DearImGuiKSPNative/src/BackendOpenGL.h` | Add | 2, 3, 6, 8 | Med | ~30 (new) |
| `DearImGuiKSPNative/src/BackendOpenGL.cpp` | Add | 2, 3, 6, 8 | Med | ~110 (new) |
| `DearImGuiKSPNative/src/DearImGuiKSPNative.cpp` | Modify | 1, 4, 5 | Med | ~30 (routing branch, new export, Unload, version 8) |
| `DearImGuiKSPNative/build.bat` | Modify | — | Low | +2 TUs, +opengl32.lib |
| `DearImGuiKSPNative/build_release.bat` | Modify | — | Low | +2 TUs, +opengl32.lib |
| `DearImGuiKSPNative/README.md` | Modify | — | Low | link list, GL note |
| `DearImGuiKSP/Infrastructure/NativeBridge.cs` | Modify | 4, 5, 7 | Med | ~40 (gate, GL branch, bind, ExpectedNativeVersion 8) |
| `DearImGuiKSP/Application/FailureText.cs` | Modify | 7 | Low | DK_FailGraphics wording |
| `DearImGuiKSP/Application/FailureKind.cs` | Modify | 7 | Low | stale "MVP requires D3D11" comment |
| `docs/70-troubleshooting.md` | Modify | — | Low | 2 D3D11-only rows |
| `docs/00-getting-started.md` | Modify (if it claims D3D11-only — verify during implementation) | — | Low | 0–5 |

No changes: `ContextHost.*`, `BackendD3D11.*`, render pump (`DearImGuiKSPAddon.cs`), demo mod, settings schema, public consumer API (`DearImGuiKSP.cs`), tests beyond what already passes.

### Migration Strategy
- None for consumers: the public managed API is unchanged; GL support is automatic at runtime. Consumer-facing note lands in the changelog at release (expected minor bump → 1.1.0; packaging out of scope this session).

### Sub-Agent Scopes
- **Single workstream, lead-implemented** (C13 precedent): native + managed are coupled through the handshake pair and one ABI export; splitting adds coordination cost with no independence. No parallel sub-agents.
- Ordering constraint: native first (M1), then managed against it (M2), then in-game (M3, M4 — user-verified).
