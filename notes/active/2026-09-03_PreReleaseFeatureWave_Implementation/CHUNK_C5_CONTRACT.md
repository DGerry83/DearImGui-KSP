# Chunk Contract: Managed Bridge — LoadFontFromFile Delegate + v5 + Startup Wiring
## Plan: DearImGui-KSP Pre-Release Feature Wave
## Date: 2026-09-04
## Chunk ID: C5
## Advances Milestone: M2 (font pipeline) — completes M2

### Scope
Managed side of handshake v5 and the startup font load. **Managed-only chunk.**

1. **`Application/Interfaces/INativeBridge.cs`** — add
   `bool LoadFontFromFile(string utf8Path, float sizePixels)` (false = native load failed →
   caller falls back to embedded default). XML-doc the before-first-frame requirement.

2. **`Infrastructure/NativeBridge.cs`** —
   - `ExpectedNativeVersion` 4 → **5** (lockstep with C4's native bump, D17).
   - `GetProcAddress` + delegate for `DearImGuiKSPNative_LoadFontFromFile` following the
     existing delegate pattern exactly (same marshalling conventions as the current 10
     exports; the native signature is `int (const char* utf8, float)` — convert the managed
     string to UTF-8 the same way `FeedFrameInput` handles text; map int return → bool).
   - If the export is missing, bridge init must fail via the existing version-mismatch /
     init-failure path (a v4 native DLL lacks the export — this is the intended mismatch
     behavior, G6).

3. **Startup wiring** (`Infrastructure/DearImGuiKSPAddon.cs` and/or `Composition.cs` —
   follow wherever bridge init currently lives):
   - After successful bridge init, before the first frame: resolve via
     `FontResolver.Resolve(settings.Font, settings.FontScale)`.
   - `UseEmbeddedDefault` → no native call. Log the spec line
     `Font '<name>' not found or unreadable; using embedded default font.` **only when a
     TTF was requested but resolution failed** — an explicit `font = "ProggyClean"` is a
     deliberate choice and logs nothing (contract decision; spec string table has no
     variant for the deliberate case).
   - Otherwise call `LoadFontFromFile(PrimaryPath, SizePixels)`; if `SecondaryPath` is
     non-null, call it too. Any false return → log the same fallback line and continue
     (native atlas still holds the embedded default — never a failure-mode trigger,
     spec §5.2).
   - This must run before `MarkRunning`/the first `Update` frame — verify the actual call
     order in `Start` and state it in the report.

### Inputs (must exist before starting)
- C6 verified: `FontResolver.Resolve(font, fontScale)` → `FontResolution { UseEmbeddedDefault, PrimaryPath, SecondaryPath, SizePixels }` (internal, Infrastructure).
- C4 verified: native export live, `GetVersion()` = 5 in the staged GameData DLL.
- `NativeBridge.cs`, `INativeBridge.cs`, `DearImGuiKSPAddon.cs`, `Composition.cs` as-is.

### Outputs (must be created/changed)
- `DearImGuiKSP/Application/Interfaces/INativeBridge.cs` — new member.
- `DearImGuiKSP/Infrastructure/NativeBridge.cs` — v5 + delegate.
- `DearImGuiKSP/Infrastructure/DearImGuiKSPAddon.cs` and/or `Composition.cs` — startup font wiring (whichever owns bridge init; keep it minimal).
- No test changes expected (bridge is Infrastructure-bound; suite must stay 63/63).

### Constraints
- Follow the existing NativeBridge delegate pattern byte-for-byte in style; no new marshalling machinery if an existing convention covers UTF-8 strings.
- Do NOT touch SettingsModel/FontResolver (C6, done), theme code (C8), or the native side (C4, done).
- §5.9 checklist: Check 1 — **scoped-and-restored**: NativeBridge already does SetDllDirectory→LoadLibrary→restore; your addition must not extend that window. Check 2 — N/A (startup-only). Check 3 — if GetProcAddress for the new export fails after earlier acquisitions, the existing teardown path must still release everything; trace and report it.
- Log strings verbatim from spec §7: `Font '<name>' not found or unreadable; using embedded default font.` with the `[DearImGuiKSP]` prefix (the logger adds it).

### Verification
- `dotnet build DearImGui-KSP.slnx` — 0 errors, 0 new warnings.
- `dotnet test DearImGui-KSP.slnx` — 63/63.
- Grep: no changes outside the 3–4 contract files.
- Report the exact init call order in `Start` proving font load precedes first frame.
- In-game verification (Plex Sans renders; fallback line; mismatch path) is the M2 gate — user-assisted, deferred, NOT yours to run.

### Rollback
- Revert the touched files to the pre-chunk checkpoint; rebuild to confirm 63/63. (The staged native v5 DLL can stay — C4 stands alone.)
