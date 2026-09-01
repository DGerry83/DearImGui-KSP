# Chunk Contract: Settings Store + Model + Migration

## Plan: DearImGui-KSP Implementation
## Date: 2026-08-31
## Chunk ID: C11
## Advances Milestone: M5 (parallel group A — runs concurrently with C9, C10)

### Scope

- Real settings persistence (spec §4.4, §9.1): `settings.cfg` as a KSP ConfigNode, read at startup, written on change, format-versioned with a forward-migration hook.
- `SettingsModel` (Application): in-memory snapshot, range clamping, change notification.
- Replace the `VerboseLoggingStub = true` constant in `DearImGuiKSPLogger` with a SettingsModel-backed gate (tracked stub in INTEGRATION_CONTRACT.md).

### Inputs (must exist before starting)

- Shipped `GameData/DearImGuiKSP/settings.cfg` (root node `DEARIMGUIKSP_SETTINGS`, `formatVersion = 1`, five settings — spec §9.1 defaults).
- `LibraryConfig.cs`: `SettingsPath`, `DefaultUiScale/DefaultFontScale/DefaultTheme/MinScale/MaxScale` constants.
- Skeletons: `Application/SettingsModel.cs`, `Application/Interfaces/ISettingsStore.cs` (TODO pins `Load()` / `Save(LibrarySettings)`), `Infrastructure/SettingsStore.cs`.
- Spec §9.1 setting table: `uiScale`/`fontScale` float 0.5–2.0 default 1.0; `theme` string "dark" only; `verboseLogging` bool default false; `enabled` bool default true (global kill switch — dormant when false).
- KSP Knowledge Library (`C:\Users\Matt\source\repos\TOOLS\KSP Knowledge Library`): verify `ConfigNode` load/save/parse API against the ILSpy dump before writing file IO — do not guess signatures.

### Outputs (must be created/changed) — exclusive file ownership

- `Application/LibrarySettings.cs` (new): plain mutable settings record — `UiScale`, `FontScale`, `Theme`, `VerboseLogging`, `Enabled` — defaults from `LibraryConfig`.
- `Application/Interfaces/ISettingsStore.cs` — replace the TODO with:
  ```csharp
  LibrarySettings Load();              // tolerant; defaults on missing/unreadable file
  void Save(LibrarySettings settings); // writes the full node, current formatVersion
  ```
- `Application/SettingsModel.cs` — implement: ctor takes `ISettingsStore` and loads; exposes the five values as properties; setters clamp `UiScale`/`FontScale` to `[MinScale, MaxScale]`, reject non-"dark" themes (MVP), raise `event Action Changed` on any actual change, and persist via `store.Save`. Load-time values are clamped/normalized the same way.
- `Infrastructure/SettingsStore.cs` — implement over `ConfigNode` at `KSPUtil.ApplicationRootPath + LibraryConfig.SettingsPath`:
  - Load: missing file/IO error → defaults, no write, one `Warn` log. Unknown keys ignored; missing keys take defaults; unparseable values take defaults. Missing `formatVersion` treated as 1.
  - Migration: current format is 1; if a future/older `formatVersion` appears, normalize to current on load and write the file back once (the structural hook — no real older versions exist yet). Log one `Info` line when a migration write happens.
  - Save: write the complete `DEARIMGUIKSP_SETTINGS` node with all five values + `formatVersion`; never partial writes.
- `Infrastructure/DearImGuiKSPLogger.cs` — remove `VerboseLoggingStub`; `Debug()` gates on a settable `Func<bool>` provider (default null → suppressed). Instance property, e.g. `internal Func<bool> VerboseLoggingProvider { get; set; }`. Update the class doc comment (stub reference removed).
- `LibraryConfig.cs` — add `SettingsFormatVersion = 1`, `DefaultVerboseLogging = false`, `DefaultEnabled = true`.

**Explicitly NOT owned by this chunk** (lead wires after group A): `Composition.cs`, `DearImGuiKSPAddon.cs`, `FrameLoopOrchestrator.cs`. The kill switch (`enabled = false` → library stays dormant) is enforced in the addon by the lead — not this chunk.

### Constraints

- Application stays Unity-free: `SettingsModel`/`LibrarySettings`/`ISettingsStore` reference no UnityEngine/KSP types; all ConfigNode IO lives in `SettingsStore`.
- The library persists only its own global config (hard constraint); consumer state is never written here.
- Applying `uiScale`/`fontScale` to the ImGui IO is NOT this chunk (later milestone); model and persistence only.
- No file-watching (spec §4.4); read at startup, write on change.
- No git commits — the lead commits after review.

### Verification

- `dotnet build DearImGui-KSP.slnx` — 0 errors, 0 warnings (gate G3).
- AC12 (round-trip, migration, kill switch) is user-verified in-game after the lead wires Composition + addon: edit `settings.cfg` (`verboseLogging = true` → `[debug]` lines appear; `enabled = false` → library dormant), confirm load/save round-trip and migration behavior.

### Rollback

- `git checkout -- DearImGuiKSP/Application/SettingsModel.cs DearImGuiKSP/Application/LibrarySettings.cs DearImGuiKSP/Application/Interfaces/ISettingsStore.cs DearImGuiKSP/Infrastructure/SettingsStore.cs DearImGuiKSP/Infrastructure/DearImGuiKSPLogger.cs DearImGuiKSP/LibraryConfig.cs`
