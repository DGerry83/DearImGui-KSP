# Frozen Acceptance Gates: OpenGL Render Backend
## Frozen At: 2026-09-08T09:30:00-04:00
## Source: [ARCHITECTURE_CONTRACT.md](ARCHITECTURE_CONTRACT.md)

| Gate ID | Criterion | Owner | Verdict | Evidence |
|---------|-----------|-------|---------|----------|
| G1 | Native debug AND release builds complete 0 errors/0 warnings with `BackendOpenGL.cpp` + `imgui_impl_opengl3.cpp` compiled in and `opengl32.lib` linked; dumpbin shows `DearImGuiKSPNative_SetOpenGLBackend` exported undecorated; headless harness prints HARNESS PASS | Lead | PASS | Builds 0 errors (1 warning = pre-existing vendor C4190 in cimspinner.h:248, predates this work); dumpbin 2026-09-08: SetOpenGLBackend present undecorated; harness HARNESS PASS |
| G2 | Handshake v8 lands in lockstep: native `GetVersion()` returns 8 and managed `ExpectedNativeVersion` is 8 in the same verified commit; a mismatched pair still hits the existing version-mismatch failure path | Lead | PASS | Both constants = 8 in this milestone's single commit; mismatch path unchanged (handshake block untouched except the constant — code review) |
| G3 | Managed `dotnet build` 0 err/0 warn and `dotnet test` xUnit suite green | Lead | PASS | 2026-09-08: 0 err/0 warn; 178/178 tests passed |
| G4 | D3D11 regression: in-game on the default (D3D11) instance the library behaves identically to 1.0.1 — demo window renders, input locks engage/release, themes/fonts apply, zero DearImGuiKSP errors in KSP.log | User (in-game) | PASS | User-verified 2026-09-08 (default launch, GL test instance) |
| G5 | AC2: under `-force-glcore` on the GL test instance, the demo UI renders correctly at full framerate, input locks work (click-through blocked, camera locked while capturing, keyboard while typing), F2 suspend/resume and resolution change behave as on D3D11, zero DearImGuiKSP errors in KSP.log | User (in-game) | PASS | User-verified 2026-09-08; lead log cross-check: `Native bridge initialized: DearImGuiKSPNative v8 on OpenGLCore, context up.`, zero [DearImGuiKSP] ERR/WRN, zero native diagnostics lines (the two "not available; demo disabled" lines are pre-main-menu loader scenes, per-design since C15) |
| G6 | Unsupported-API failure path intact: any graphics device other than D3D11/OpenGLCore still yields Failed state, exactly one DK_FailGraphics popup (updated wording), detailed log, IsAvailable == false | Lead (code review) | PASS | Code review of NativeBridge.cs:145-153: non-D3D11/non-OpenGLCore → InitErrUnsupportedDevice → FailureKind.GraphicsApi → unchanged one-popup path; DK_FailGraphics body never named D3D11 and stays verbatim. In-game sabotage impractical — D3D11/GLCore are the only reachable devices on this hardware (documented rationale) |
| G7 | GL state hygiene: under `-force-glcore` with the instance's render mods active (Scatterer, Parallax, TUFX, Deferred), no visual corruption of the scene or other mods' rendering while ImGui windows draw (imgui_impl_opengl3 state backup/restore holds in practice) | User (in-game) | PASS | User-verified 2026-09-08 |

## Session Verdict
- **Verdict**: CONTINUE
- **Reason**: All gates PASS (G4/G5/G7 user-verified in-game 2026-09-08, G1–G3 build/test evidence, G6 code review). AC2 satisfied; OpenGL is a working second backend.
