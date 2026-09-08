# Audit Report: OpenGL Render Backend
## Date: 2026-09-08
## Type: Feature

### Gate Verdicts
| Gate ID | Criterion | Verdict | Evidence |
|---------|-----------|---------|----------|
| G1 | Native builds clean, export present, harness PASS | PASS | build.bat/build_release.bat 0 errors (1 pre-existing vendor C4190); dumpbin: SetOpenGLBackend undecorated; HARNESS PASS |
| G2 | Handshake v8 lockstep | PASS | Native GetVersion()=8 + managed ExpectedNativeVersion=8 in commit 2e3f6ed (same commit); mismatch path code untouched |
| G3 | Managed build + xUnit green | PASS | 0 err/0 warn; 178/178 |
| G4 | D3D11 regression | PASS | User in-game 2026-09-08 |
| G5 | AC2 OpenGL in-game | PASS | User in-game 2026-09-08 + lead KSP.log cross-check (v8 on OpenGLCore, zero library errors/diagnostics) |
| G6 | Unsupported-API failure path intact | PASS | Code review NativeBridge.cs:145-153 + unchanged DK_FailGraphics path; in-game sabotage impractical (documented) |
| G7 | GL state hygiene | PASS | User in-game 2026-09-08 (Scatterer/Parallax/TUFX/Deferred active, no corruption) |

### Session Verdict
- **Verdict**: CONTINUE
- **Reason**: All gates PASS. AC2 (deferred since D20) is satisfied.

### Frozen Gates Integrity
- [x] GATES.md exists and was frozen before implementation (frozen 2026-09-08T09:30-04:00, first code change after)
- [x] GATES.md not modified after freezing except verdict/evidence columns and the session verdict (git diff shows criteria and timestamp unchanged)

### Invariant Check Results
- [x] Public interfaces preserved: consumer API untouched; INativeBridge unchanged; additive native export only (check: diff of DearImGuiKSP.cs/Interfaces — no changes)
- [x] Shared state shape stable: settings.cfg schema untouched; new state is native-file-local statics (s_ActiveBackend, s_BackendUp, s_BackendFailed)
- [x] Language/runtime compliance: C++17 cl.exe, C# net48 idioms match surrounding code
- [x] Public utility signatures stable: BackendD3D11.h unchanged (check: git diff — no changes to that file)
- [x] Minimal change principle: 7 files modified + 2 added (~130 net lines of code), contract estimate held
- [x] Layered dependency direction preserved: native Core stays Unity-free; managed change confined to Infrastructure (NativeBridge) + a comment in Application (FailureKind)

### Principles & Anti-Patterns Check
- [x] SRP: BackendOpenGL has one job (render draw data via imgui_impl_opengl3); mirrors BackendD3D11 rather than merging — the two change for different reasons (DRY exception, contract candidate 1)
- [x] SoC: device-kind decision managed-side (Infrastructure), rendering native-side — unchanged boundary
- [x] Dependency inversion: backends consume ContextHost abstractions (context, frame lock, diagnostics), never own the context
- [x] No global mutable state introduced beyond the two backends' own session singletons (same pattern as existing backend)
- [x] No God Class/Golden Hammer/Leaky Abstraction observed
- [x] Magic numbers: new error codes (native 4/1, managed InitErrBackendSelect=9) documented at declaration

### Native Interop & Hot-Path Checklist (CORE_PROTOCOLS §5.9)
- Check 1 (process-global state): no OS-global mutation; GL context state backed up/restored by imgui_impl_opengl3 around the draw — validated in-game (G7)
- Check 2 (hot-path allocation): BackendOpenGL_Render adds zero allocations; managed gate is init-time only
- Check 3 (resource symmetry): GL objects released in BackendOpenGL_Shutdown; failed CreateDeviceObjects runs ImGui_ImplOpenGL3_Shutdown before latching; shutdown guarded against assert (backend-up && context live)

### Similar Bugs Sweep
- **Pattern searched**: backend bring-up failure handling (retry/flap risk) and unguarded backend shutdown asserts
- **Files checked**: BackendOpenGL.cpp vs BackendD3D11.cpp (G3-03 latch + shutdown guard mirrored verbatim); OnRenderEvent routing (D3D11 default preserves pre-selection no-op)
- **Findings**: none — the GL backend carries the same protections as the audited D3D11 one

### Violations Found
- None.

### Recommendations
- Clear to proceed. Release-prep leftovers (deliberately out of scope this session): root README.md "OpenGL planned" line, CHANGELOG entry, version bump to 1.1.0, AGENTS.md status line, repackaged dist zips.
