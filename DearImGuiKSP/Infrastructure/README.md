# Infrastructure layer (managed)

The **only** layer that touches KSP/Unity APIs. Implements the interfaces defined in `Application/Interfaces/`.

Components (see `IMPLEMENTATION_PLAN.md` §3):

- `DearImGuiKSPAddon.cs` — `[KSPAddon(Startup.MainMenu, once: true)]` entry point; self-`DontDestroyOnLoad`s.
- `Composition.cs` — the DI root; the **only** place concrete Infrastructure classes are instantiated.
- `NativeBridge.cs` — explicit `LoadLibrary` + `SetDllDirectory` bootstrap of `DearImGuiKSPNative.dll`, the managed/native version handshake, and the bridge's own bootstrap/frame-loop functions bound via `GetProcAddress`. It does **not** own all P/Invoke declarations: once the module is loaded, the Interop layer's implicit `[DllImport("DearImGuiKSPNative")]` cimgui/widget calls resolve against it by module name.
- `InputLockGateway.cs` — wraps KSP `InputLockManager` with per-consumer lock IDs.
- `GameEventHooks.cs` — subscribes to `GameEvents` / `UIMasterController` for F2, loading screens, resolution changes.
- `SettingsStore.cs` — ConfigNode load/save of `GameData/DearImGuiKSP/settings.cfg` with format migration; writes are atomic (temp file + replace).
- `FailureNotifier.cs` — the one-time stock `PopupDialog` for startup failure.
- `DearImGuiKSPLogger.cs` — `[DearImGuiKSP]`-prefixed `UnityEngine.Debug` wrapper; debug lines gated by the `verboseLogging` setting.
- `FontResolver.cs` — maps the normalized `font` + `fontScale` settings to a concrete TTF under `Fonts/` (embedded-default fallback signal).
- `LibraryControlPanel.cs` — the library's own in-game settings window ("DearImGui-KSP Settings"), registered as a regular consumer through the same frame-loop path.
- `LibraryPanelToolbar.cs` — the ApplicationLauncher toolbar button that opens the settings window.
- `PointerBlockerGateway.cs` — invisible uGUI raycast blocker that absorbs clicks while an ImGui window captures the pointer.
- `ImguiEventEaterGateway.cs` — IMGUI `hotControl` grab that keeps IMGUI windows beneath a capturing ImGui window from receiving input (ISSUES #003).
