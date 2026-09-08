# Progress Log: OpenGL Render Backend
## Session: 2026-09-08_Feature_OpenGLBackend

| Milestone | Status | Files | Verification | Gates | Notes |
|-----------|--------|-------|--------------|-------|-------|
| M1 Native GL backend | **Done** | `src/BackendOpenGL.h/.cpp` (new), `src/DearImGuiKSPNative.cpp` (routing enum + SetOpenGLBackend export + GetVersion 8 + Unload), `build.bat`, `build_release.bat` (+2 TUs, +opengl32.lib), `README.md` | debug+release builds 0 errors; 1 warning = pre-existing vendor C4190 (cimspinner.h:248, untouched by this work); dumpbin: `DearImGuiKSPNative_SetOpenGLBackend` exported undecorated (21 DearImGuiKSPNative_* exports); harness HARNESS PASS | G1 PASS, G2 native half | Implemented directly by lead (single workstream per contract) |
| M2 Managed gate + handshake v8 | **Done** | `NativeBridge.cs` (gate accepts D3D11/OpenGLCore, GL selection branch, InitErrBackendSelect=9, ExpectedNativeVersion 8, log records actual device), `FailureKind.cs` (stale comment), `docs/70-troubleshooting.md` (2 rows) | `dotnet build` 0 err/0 warn; `dotnet test` 178/178 PASS | G2 managed half, G3 PASS | FailureText.cs DK_FailGraphics needed no change (body never named D3D11); docs/00-getting-started.md has no D3D11 claims |
| M3 D3D11 regression (in-game) | Pending user | — | — | G4 | Default instance, normal launch |
| M4 GL validation (in-game) | Pending user | — | — | G5, G6, G7 | `-force-glcore` on DearImGui-KSP_TESTING |

Deferred to release prep (not this session): root README.md "OpenGL planned" line, CHANGELOG entry, package version bump (expected 1.1.0), AGENTS.md status line, demo `KSPAssemblyDependencyEqualMajor` check (ISSUES #016 applies on major bumps only — N/A here).
