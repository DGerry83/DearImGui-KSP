# Chunk Contract: Native Font Export + Handshake v5 (Native Side)
## Plan: DearImGui-KSP Pre-Release Feature Wave
## Date: 2026-09-03
## Chunk ID: C4
## Advances Milestone: M2 (font pipeline)

### Scope
Native side of the font pipeline: one new export and the lockstep version bump.
**Native-only chunk — no managed changes.**

1. **`src/ContextHost.h/.cpp`** — new function:
   `int ContextHost_LoadFontFromFile(const char* utf8Path, float sizePixels)`
   - Return 0 on success, nonzero on failure (enumerate: 1 = no context, 2 = frames already begun, 3 = AddFontFromFileTTF failed).
   - Loads via `io.Fonts->AddFontFromFileTTF(utf8Path, sizePixels)` (default glyph range, no config merge).
   - Legal only before the first `NewFrame` (spec §4.2); if frames have begun, return 2 without touching the atlas.
   - Atlas build stays lazy under the 1.92.9 texture protocol — do NOT call `Build()`.
   - Result is recorded so `ContextInit`-time state stays consistent; the managed side may call it twice (Regular, then Medium) — each call appends one font to the atlas.

2. **`src/DearImGuiKSPNative.cpp`** — new export
   `DearImGuiKSPNative_LoadFontFromFile` (`extern "C" __declspec(dllexport) int`, same convention as existing exports) forwarding to ContextHost; and bump `DearImGuiKSPNative_GetVersion()` from 4 to **5** (D17/D29 lockstep — managed side bumps in C5).

3. **No build-script changes** — no new translation units in this chunk.

4. **Order dependency note**: after this chunk the GameData native DLL is v5 while managed still expects v4 — the game will hit the version-mismatch failure path until C5 lands. This is intentional and doubles as the G6 mismatch test opportunity.

### Inputs (must exist before starting)
- M1 verified; C6 done (managed resolver contract: path + sizePixels + fallback signal).
- `src/ContextHost.cpp/.h`, `src/DearImGuiKSPNative.cpp` as-is (`GetVersion()` at DearImGuiKSPNative.cpp:28 returns 4; `AddFontDefault` in ContextHost.cpp:49).
- Pinned cimgui clone at `~/source/repos/cimgui` (present).

### Outputs (must be created/changed)
- `DearImGuiKSPNative/src/ContextHost.h` — declaration.
- `DearImGuiKSPNative/src/ContextHost.cpp` — implementation.
- `DearImGuiKSPNative/src/DearImGuiKSPNative.cpp` — export + version 5.
- Rebuilt DLLs copied by the build scripts into `GameData/DearImGuiKSP/PluginData/` (debug via build.bat; also run build_release.bat and build_harness.bat).

### Constraints
- Match existing code style in the two native files (naming, export macro pattern, comment density).
- No new TUs, no build-script edits, no vendor/ changes in this chunk.
- §5.9 checklist applies (native export + startup-path code):
  - Check 1 (process-global state): must be **none** — AddFontFromFileTTF touches only the context's own atlas; no DLL search path, env, or CWD changes.
  - Check 2 (hot-path allocation): N/A — startup-only, called before first frame.
  - Check 3 (resource-acquisition symmetry): trace init failure paths — if font load fails, the atlas must remain exactly as it was (embedded default still valid); no partial state, nothing to release.
- The version bump is exactly `return 4;` → `return 5;` — nothing else about the handshake changes.

### Verification
- `cd DearImGuiKSPNative && ./build.bat` — compiles, 0 errors; DLL lands in GameData PluginData.
- `./build_release.bat` — compiles, 0 errors.
- `./build_harness.bat` + run the harness exe — prints `HARNESS PASS`.
- Export-table check: `DearImGuiKSPNative_LoadFontFromFile` present in the rebuilt DLL's exports (same PE-export technique C2 used: PowerShell parser — dumpbin fails under MSYS).
- Report `GetVersion()` evidence (harness output or a minimal call) showing 5.
- Milestone contribution: with C6 done and C7 parallel, only C5 remains before the M2 gate.

### Rollback
- Revert the 3 source files to the pre-chunk checkpoint, re-run `build.bat` to restore the v4 DLL in GameData PluginData.
