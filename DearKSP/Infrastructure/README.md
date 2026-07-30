# Infrastructure layer (managed)

The **only** layer that touches KSP/Unity APIs. Implements the interfaces defined in `Application/Interfaces/`.

Components (see `IMPLEMENTATION_PLAN.md` §3):

- `DearKSPAddon.cs` — `[KSPAddon(Startup.MainMenu, once: true)]` entry point; self-`DontDestroyOnLoad`s.
- `Composition.cs` — the DI root; the **only** place concrete Infrastructure classes are instantiated.
- `NativeBridge.cs` — explicit `LoadLibrary` + `SetDllDirectory` bootstrap and all P/Invoke declarations.
- `InputLockGateway.cs` — wraps KSP `InputLockManager` with per-consumer lock IDs.
- `GameEventHooks.cs` — subscribes to `GameEvents` / `UIMasterController` for F2, loading screens, resolution changes.
- `SettingsStore.cs` — ConfigNode load/save of `GameData/DearKSP/settings.cfg` with format migration.
- `FailureNotifier.cs` — the one-time stock `PopupDialog` for startup failure.
- `DearKSPLogger.cs` — `[DearKSP]`-prefixed `UnityEngine.Debug` wrapper.
