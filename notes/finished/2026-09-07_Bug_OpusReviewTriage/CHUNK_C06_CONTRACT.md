# Chunk Contract: C06 — Settings write-amplification family
## Plan: Opus Review Remediation (`notes\plans\opus-review-remediation-contracts.md`)
## Date: 2026-09-07
## Chunk ID: C06
## Advances Milestone: Post-1.0.0 remediation wave (G3-08, G3-09, G3-14, G3-16, G3-43 + merged G3-19)

### Scope
- **Shared debounce fix (G3-08/09/14/16/19)**: `SettingsModel` setters no longer call `ISettingsStore.Save` synchronously; a change sets `PersistPending` and rearms a quiet timer (`LibraryConfig.SettingsSaveDebounceSeconds = 0.5 s`). `FrameLoopOrchestrator.RunFrame` calls `_settings.PersistIfSettled(deltaTime)` once per frame at frame start — before `BeginUiFrame`, so the disk write never happens inside the held native frame lock — and the write lands only after changes settle. A slider drag now costs one settings.cfg write after the drag, not one full rewrite per changed frame. The `Changed` event still fires synchronously per change, so ThemeEngine's live re-apply during a drag is untouched (only the disk write is debounced). Session-end safety: `DearImGuiKSPAddon.OnDestroy` calls `Settings.SaveNow()` so an edit inside the debounce window at quit is not lost; `SaveNow()` is also the test seam.
- **Atomic write (G3-16)**: `SettingsStore.Save` writes a `settings.cfg.tmp` sibling first, then `File.Replace` (first-ever save: `File.Move`); the temp file is best-effort cleaned up on failure. A crash/concurrent reader mid-write can no longer see a truncated settings.cfg.
- **G3-43**: shipped default `GameData\DearImGuiKSP\settings.cfg` gains the documented `font = IBMPlexSans` key with a one-line comment naming the accepted values (docs/10 §8 table).

### Inputs
- Triage sweep verdicts G3-08/09/14/16/43 (all VALID WORK), `notes\active\2026-09-07_Bug_OpusReviewTriage\TRIAGE_SWEEP.md`.
- Existing patterns: ThemeEngine dirty-flag/deferred-apply (the model for this fix), SettingsModelTests.

### Outputs
- Changed: `DearImGuiKSP/Application/SettingsModel.cs` (dirty flag + `PersistIfSettled`/`SaveNow`/`BuildSnapshot`; `Persist()` removed), `DearImGuiKSP/Application/FrameLoopOrchestrator.cs` (frame-start flush + class-doc sequence line), `DearImGuiKSP/Infrastructure/SettingsStore.cs` (atomic write + class doc), `DearImGuiKSP/Infrastructure/LibraryControlPanel.cs` (stale "persists immediately"/"saved now" doc text), `DearImGuiKSP/Infrastructure/DearImGuiKSPAddon.cs` (OnDestroy flush — one line, deviation from the listed file set, needed so the debounce cannot lose the last edit at quit), `DearImGuiKSP/LibraryConfig.cs` (`SettingsSaveDebounceSeconds` constant), `GameData/DearImGuiKSP/settings.cfg` (font key), `tests/Application.Tests/SettingsModelTests.cs` (5 tests re-aimed at SaveNow).
- New: `tests/Application.Tests/SettingsPersistenceTests.cs` (7 tests).
- `ThemeEngine.cs` listed in the plan but unchanged: its live re-apply path is the required kept behavior; nothing in it referenced the old write behavior.

### Constraints
- Public API surface unchanged (all members internal; patch class, D36). Config file format unchanged — only the shipped default gained the already-documented `font` key.
- §5.9 native-interop checklist applies (per-frame slider path):
  - **Process-global state**: file I/O moved out of the held native SRWLOCK region; atomic replace avoids torn-read by any concurrent reader (e.g. ModuleManager cache tooling). Verdict: improved, documented.
  - **Hot-path allocation**: idle steady state is one bool check (`PersistPending`) per frame, zero allocation; the flush allocates the ConfigNode snapshot only on an actual write, after a 0.5 s quiet period. Verdict: zero new steady-state allocation; write amplification removed.
  - **Resource-acquisition symmetry**: no acquisition; the write no longer extends the BeginFrame→EndFrame lock hold. Verdict: lock-hold window shortened.

### Verification
- `dotnet build DearImGui-KSP.slnx`: 0 errors, 0 warnings.
- `dotnet test DearImGui-KSP.slnx`: 129/129 green (baseline 122 + 7 new SettingsPersistenceTests: idle no-save, below-threshold pending, settle-saves-once-with-latest-values, simulated 30-frame drag rearms debounce → one write, re-arm after settle, SaveNow immediate flush / no-op-when-clean).
- SettingsStore is Infrastructure (KSP ConfigNode); atomicity not unit-testable at the Application layer — the Application-layer dirty-flag/debounce logic is the covered half, per the contract.
- In-game: gate B (user) — drag the uiScale slider: theme live-updates during the drag, settings.cfg is rewritten once ~0.5 s after the drag ends, file content intact.

### Rollback
- Revert the listed changed files; delete `tests/Application.Tests/SettingsPersistenceTests.cs`.
