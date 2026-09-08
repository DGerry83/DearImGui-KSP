# Chunk Contract: C04 — Native rendering & diagnostics
## Plan: Opus Review Remediation (`notes\plans\opus-review-remediation-contracts.md`)
## Date: 2026-09-07
## Chunk ID: C04
## Advances Milestone: Post-1.0.0 remediation wave (native items G2-01, G2-04, G3-01, G3-03)

### Scope
- **G2-01 (gradient pass mis-targets child windows)**: `ContextHost.cpp` `ApplyWindowBgGradient`. When Begin renders a child window's decorations into the parent's draw list (imgui.cpp:8585-8611 swap), the child's own command 0 is empty and the fills were silently unshaded. New: an empty command 0 on a `ImGuiWindowFlags_ChildWindow` window retargets to `ShadeChildDecorationsInAncestors`, which scans every command PAST 0 of each direct-line ancestor (never command 0 — that holds the ancestor's own decorations, shaded by its own pass; verts are append-only so the ranges never mix) and shades only chrome-colored solid-fill verts contained in the child's outer rect (expanded by the border size). The whole ancestor chain is walked to cover nested swapped children. The per-vert shade logic is now shared (`TryShadeChromeVert`) so the C35 inclusion filter applies identically on both paths; the top-level path is otherwise unchanged.
- **G2-04 (no ImGui ErrorCallback; red debug tooltips; nothing reaches KSP.log)**: `ContextInit` disables `io.ConfigErrorRecoveryEnableTooltip` (the red overlay; ErrorLog still satisfies its one-sink assert via the callback, imgui.cpp:11747) and installs `g.ErrorCallback` (internal-only in 1.92.9, imgui_internal.h:2809 — this TU already uses imgui_internal.h). Callback text lands in a fixed 4 KB native diagnostics buffer (own SRWLOCK, overflow drops + drop-notice). New additive export `DearImGuiKSPNative_DrainDiagnostics(dst, capacity)` (query with null dst → pending bytes; non-null drains and clears). Managed half: `NativeBridge.EndUiFrame` drains once per frame after `_endFrame()` and logs non-empty text via `_logger.Error` → KSP.log. Recovery/assert/debug-log flags stay stock.
- **G3-01 (uiScale < 1.0 zeroes 1px style sizes)**: `ContextHost_SetUiScale` snapshots the 14 line-thickness fields (`*BorderSize`, `TabBarOverlineSize`, `SeparatorSize`, `SeparatorTextBorderSize`, `DockingSeparatorSize`, `TreeLinesSize`, `InputTextCursorSize`) before `ScaleAllSizes` and re-applies a 1px floor (`ScaledLineSizeFloor`) to any that were nonzero before scaling — zero defaults (e.g. stock `FrameBorderSize`) stay zero. All other sizes scale exactly as before.
- **G3-03 (D3D11 bring-up failure silent, retried every frame, flaps RendererHasTextures)**: `BackendD3D11.cpp` `TryInitBackend` latches `s_BackendFailed` on the first `ImGui_ImplDX11_Init`/`CreateDeviceObjects` failure, pushes one line into the G2-04 diagnostics buffer, and never retries (no more per-frame `ImGui_ImplDX11_Shutdown` flag flapping). The latch clears in `BackendD3D11_Shutdown` (teardown symmetry).
- **Handshake**: `DearImGuiKSPNative_GetVersion()` 6 → 7, `ExpectedNativeVersion` 6 → 7, per the lockstep convention (4: #001-#003; 5: C5 font load; 6: C31 SetUiScale; 7: this channel). Any later contract adding exports must bump to 8.
- **Harness**: `harness_main.cpp` covers all four items (see Verification).

### Inputs
- Triage sweep verdicts G2-01/G2-04 (VALID), G3-01/G3-03 (VALID): `notes\active\2026-09-07_Bug_OpusReviewTriage\TRIAGE_SWEEP.md`.
- imgui 1.92.9 pinned sources (sibling clone): child-decoration swap imgui.cpp:8585-8611, ErrorLog imgui.cpp:11984-12026, `ImGuiStyle::ScaleAllSizes` imgui.cpp:1628-1681, style defaults (imgui.cpp style ctor).

### Outputs
- Changed native: `src/ContextHost.cpp`, `src/ContextHost.h` (internal decls), `src/BackendD3D11.cpp`, `src/DearImGuiKSPNative.cpp` (export wrapper + handshake 7).
- Changed managed (the one binding file): `Infrastructure/NativeBridge.cs` — **not** `Interop/ImGuiNative.cs`, deliberately: the drained export belongs to the own-ABI frame-loop family (`GetIoCaptureState`, `BeginFrame`/`EndFrame`), which is GetProcAddress-bound in NativeBridge, and the per-frame call site + logger both live there. `Interop/ImGuiNative.cs` (the cimgui/widget family) is untouched. INativeBridge unchanged.
- Changed harness: `harness/harness_main.cpp` (coverage below).

### Constraints
- §5.9 native-interop checklist verdicts:
  - **Process-global state**: new statics are the diagnostics buffer (+ its own SRWLOCK) and the backend failure latch — both process-wide by design (one ImGui context, one backend), reset in ContextShutdown/BackendD3D11_Shutdown. The diag lock is never held while taking the frame SRWLOCK (no lock-order hazard; the render thread pushes while holding the frame lock, the game thread drains holding neither). Verdict: acceptable, documented in code.
  - **Hot-path allocation**: native buffer is fixed 4 KB, no allocation ever; the error callback formats into a 512-byte stack line. Managed steady state (no errors) is one P/Invoke returning 0 — the drain buffer is allocated lazily on the first error and reused. Verdict: zero new steady-state allocation.
  - **Resource-acquisition symmetry**: the G3-03 latch changes no acquire/release pairing (device refs still released in Shutdown only; `ImGui_ImplDX11_Shutdown` still pairs with a successful Init on the CreateDeviceObjects failure path, exactly once now instead of per frame). Frame-lock pairing untouched. Verdict: symmetry preserved; flag flapping removed.
- No ABI breaks: one additive export + handshake bump (lockstep convention). No new dependencies; no vendored-source patches.
- Scope discipline: frame lock, device-loss handling, UnityPluginLoad/Unload untouched per the settled NOTE items.

### Verification
- `cmd //c build_harness.bat` + `build\harness.exe`: **HARNESS PASS**, including new coverage:
  - G2-01: bordered child (`grad-child-host`/`##grad-child`) with non-zero-alpha ChildBg — precondition proven (child's own cmd 0 empty), 4 child-bg verts shaded in the PARENT's list, no vert outside the child rect recolored, alpha preserved; disabled pass byte-exact vs stock reference for all three windows (32 + 108 + 100 verts). Note: the host window submits FIRST in the helper because appearing windows take focus — grad-scroll must stay the reference-frame-focused window for the pre-existing byte-exact comparisons.
  - G3-01: at uiScale 0.5 all 1px line sizes floor at exactly 1px, zero defaults stay 0, 2px fields scale to 1.
  - G2-04: `ConfigErrorRecoveryEnableTooltip == false`, callback installed, injected "Calling End() too many times!" error reaches the buffer, drain clears it.
- `cmd //c build.bat` (debug): PASS, no new warnings. `cmd //c build_release.bat`: PASS, `build\DearImGuiKSPNative.pdb` emitted (C05). Export table contains `DearImGuiKSPNative_DrainDiagnostics` (dumpbin).
- `dotnet build DearImGuiKSP/DearImGuiKSP.csproj`: 0 errors (managed binding compiles; INativeBridge unchanged so no test impact).
- In-game: gate B (user) — gradients on child windows (scroll regions in ksp theme), uiScale 0.5 borders/separators/caret visible, red ImGui error overlay suppressed and errors visible in KSP.log as `[DearImGuiKSP] ... DearImGuiKSPNative: ImGui error in window '...'`.

### Rollback
- Revert the listed files (`git checkout -- <files>`); the handshake pair must be reverted together (native `GetVersion` 7→6 and managed `ExpectedNativeVersion` 7→6).
