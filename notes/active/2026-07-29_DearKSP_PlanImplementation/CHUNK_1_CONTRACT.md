# Chunk Contract: M1 Close-Out (Logger, Composition, In-Game Log Line)

## Plan: Dear KSP Implementation
## Date: 2026-07-29
## Chunk ID: C1
## Advances Milestone: M1

### Scope

- Implement `ILogger` (Error/Warn/Info/Debug) and `DearKSPLogger` over `UnityEngine.Debug` with the `[DearKSP]` prefix; Debug gated by a verboseLogging stub constant (tracked stub, replaced in C11).
- Implement `Composition` as the DI root: creates the logger, exposes it to the addon.
- `DearKSPAddon` logs its startup line through the composition-provided logger instead of calling `Debug.Log` directly.

### Inputs (must exist before starting)

- Skeleton compiles and deploys (verified during bootstrap).
- INTEGRATION_CONTRACT.md locked contracts.

### Outputs (must be created/changed)

- Modified: `DearKSP/Application/Interfaces/ILogger.cs`
- Modified: `DearKSP/Infrastructure/DearKSPLogger.cs`
- Modified: `DearKSP/Infrastructure/Composition.cs`
- Modified: `DearKSP/Infrastructure/DearKSPAddon.cs`

### Constraints

- Application stays Unity-free: `ILogger` lives in Application but contains no Unity types; the Unity dependency is only in `DearKSPLogger` (Infrastructure).
- Minimal change: no other files touched.
- The verboseLogging gate is a named constant stub — do not wire to settings (that is C11).

### Verification

- `dotnet build DearKSP.slnx` — 0 errors, 0 warnings.
- Deploy into ReformTestInstance via the build's CopyToGame target.
- In-game: KSP.log contains a `[DearKSP]` startup line after a main-menu load (user-run verification).

### Rollback

- `git checkout -- DearKSP/Application/Interfaces/ILogger.cs DearKSP/Infrastructure/DearKSPLogger.cs DearKSP/Infrastructure/Composition.cs DearKSP/Infrastructure/DearKSPAddon.cs`
