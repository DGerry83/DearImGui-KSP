# Milestones: OpenGL Render Backend
## Date: 2026-09-08
## Source: [ARCHITECTURE_CONTRACT.md](ARCHITECTURE_CONTRACT.md)

| # | Milestone | Components | Verification | Success Criteria | Gate(s) |
|---|-----------|------------|--------------|------------------|---------|
| 1 | Native GL backend compiles and links | `BackendOpenGL.*`, entry routing, build scripts | `build.bat` + `build_release.bat` 0 err/0 warn; dumpbin exports; harness | Both builds clean; `DearImGuiKSPNative_SetOpenGLBackend` + `GetVersion`=8 exported undecorated; `harness` prints HARNESS PASS | G1, G2 (native half) |
| 2 | Managed device gate + handshake v8 | `NativeBridge.cs`, `FailureText.cs`, `FailureKind.cs` | `dotnet build` 0 err/0 warn; `dotnet test` | Build clean; xUnit suite green; gate accepts D3D11 + OpenGLCore, rejects others | G2 (managed half), G3 |
| 3 | D3D11 regression in-game | all | in-game on default instance | Demo window, input locks, theming exactly as 1.0.1; no new log lines (G4) | G4 |
| 4 | OpenGL in-game validation (AC2) | all | in-game, `-force-glcore`, GL test instance | UI renders/interacts correctly at full framerate; co-mod coexistence clean; suspend/resume + resolution change clean (G5, G7); G6 by code review | G5, G6, G7 |

M3/M4 are user-verified in-game gates (same model as prior sessions). Do not start milestone N until N-1 passes.
