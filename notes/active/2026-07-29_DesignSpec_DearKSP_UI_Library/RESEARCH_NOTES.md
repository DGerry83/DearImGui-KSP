# Research Notes — Dear_KSP UI Library

Session: `2026-07-29_DesignSpec_DearKSP_UI_Library`
Sources: KSP Knowledge Library (ILSpy dump of `Assembly-CSharp.dll`, KSP 1.12.x / Unity 2019.4.18f1), Unity documentation/forums.

---

## 1. Available APIs / capabilities

### Assembly loading & dependency system
- `[assembly: KSPAssembly("Name", major, minor[, revision])]` — self-identification. Without it, the loader falls back to the DLL filename (`AssemblyLoader.cs:636-637`).
- `[assembly: KSPAssemblyDependency("Name", major, minor[, revision])]` — **hard gate + load ordering**:
  - Attributes are read with Mono.Cecil *without loading the assembly* (`AssemblyLoader.cs:659-705`).
  - Loader runs a topological sort over the dependency graph (`AssemblyLoader.cs:356-529`), so consumers of this library will load after it if they declare the dependency.
  - Assemblies whose dependencies are unmet are **removed from the load list entirely** (`AssemblyLoader.cs:504-517`); failure reporting is **log-only, no in-game UI warning** (`AssemblyLoader.cs:1290,1315`).
  - `KSPAssemblyDependencyEqualMajor` variant additionally requires equal major version.
  - The game itself is registered as pseudo-assembly `"KSP"` (`AssemblyLoader.cs:1505-1509`), so `[assembly: KSPAssemblyDependency("KSP", 1, 12)]` is legal.
  - Version satisfaction: higher major always satisfies (unless EqualMajor); equal major requires `minor >= required`, then `revision >= required` (`AssemblyLoader.cs:1178-1254`).
- `AssemblyResolve` handler retries resolution ignoring version and logs "ADDON BINDER" redirects (`AssemblyLoader.cs:2133-2197`) — softens assembly version conflicts for a shared dependency.

### Plugin lifecycle (KSPAddon)
- `[KSPAddon(KSPAddon.Startup.X, once)]` on a `MonoBehaviour` subclass; instantiated via `new GameObject(type.Name).AddComponent(type)` on scene load (`AddonLoader.cs:298-358`).
- `once: true` = created exactly once ever; the GameObject is **not** auto-`DontDestroyOnLoad`'d — the addon must do that itself in `Awake()`.
- `once: false` = fresh instance per matching scene.
- Legal startup values (`KSPAddon.cs:3-35`): `Instantly=-2` (declared but **never handled** by `AddonLevelTest` — do not use), `EveryScene=-1`, `FlightEditorAndKSC=-6`, `AllGameScenes=-5`, `FlightAndEditor=-4`, `FlightAndKSC=-3`, plus exact scenes `MainMenu=2, Settings=3, Credits=4, SpaceCentre=5, EditorVAB/EditorSPH/EditorAny=6, Flight=7, TrackingStation=8, PSystemSpawn=9`. `MISSIONBUILDER=21` has no KSPAddon equivalent.
- No game-version-gating attribute exists for mods; `Versioning` exposes the game version (`Versioning.cs:102-106`).

### Input management
- `InputLockManager` static API: `SetControlLock(ControlTypes, string lockID)` / `RemoveControlLock(string)` / `ClearControlLocks()` (`InputLockManager.cs:37-137`). Locks are named, OR-reduced; no per-ID nesting.
- Relevant `ControlTypes` flags: `GUI`, `KEYBOARDINPUT`, `CAMERACONTROLS`, `ALLBUTCAMERAS`, `UI_DIALOGS`, `UI_DRAGGING` (`ControlTypes.cs:4-95`).
- Game precedent: modal dialogs lock `UI_DIALOGS` (`UIMasterController.cs:1711-1744`); text fields lock `KEYBOARDINPUT` (e.g. `LoadGameSearch.cs:125`); full takeover uses `ALLBUTCAMERAS` (`DebugToolbar.cs:523`).
- Events `GameEvents.onInputLocksModified`, `onGUILock`, `onGUIUnlock` fire on changes.

### Rendering environment
- KSP's production UI is **uGUI** (UnityEngine.UI + EventSystems + TMPro), not IMGUI: `PopupDialog`, `DialogGUIBase`, `UIMasterController` with nine Canvases + `uiCamera` (orthographic) + `vectorCamera` (`UIMasterController.cs:29-51`).
- IMGUI `OnGUI()` appears only in debug/dev overlays (`DebugToolbar.cs:427`, `AeroGUI.cs:204`, VehiclePhysics debug, etc.). An ImGui overlay drawn after everything won't conflict with a game IMGUI frame.
- Camera hook precedents: `OnPreRender`/`OnPostRender`/`OnRenderImage` used by FX systems (`FXCamera.cs`, `UnderwaterFog.cs`, `HighlightingSystem.cs`); command buffers attached to arbitrary cameras via `cam.AddCommandBuffer` (`HighlightingSystem.cs:572-578`).
- **No native-plugin render integration exists in the dump** — zero hits for `GL.IssuePluginEvent`, `UnityRenderingEvent`, `UnityPluginLoad`. The only DllImports are `msvcrt` and `user32`. So the render-thread injection must follow the standard Unity low-level native plugin pattern; the dump confirms no conflicting usage.

## 2. UI / rendering / I/O options and constraints

- **UI Toolkit (UIElements)**: in Unity 2019.4 it is **editor-only**. Runtime support arrives via the UI Toolkit package in 2020.1+ (preview), which requires UPM package installation into the Unity project — impossible for a mod targeting the shipped KSP binary. **Eliminated.**
  - Sources: [Unity 2019.4 UI Builder manual](https://docs.unity3d.com/2019.4/Documentation/Manual/com.unity.ui.builder.html), [Unity Discussions: UI Toolkit for 2019.4?](https://discussions.unity.com/t/ui-toolkit-for-2019-4/826002), [Unity 2019.4 UI manual](https://docs.unity.cn/2019.4/Documentation/Manual/UIToolkits.html).
- **Dear ImGui via native C++ plugin**: viable. Standard pattern = managed `GL.IssuePluginEvent` driving a render-thread callback; native DLL implements the render backend. Must handle at least D3D11 and OpenGL (`SystemInformation.cs:15` shows the game queries `graphicsDeviceVersion` for exactly this distinction).
- **uGUI**: available (the game itself uses it), but doesn't meet the stated goals of performance/control/animations as well as ImGui; remains the zero-native-code fallback.

## 3. Existing precedents

- Game's own uGUI stack (`UIMasterController`, `PopupDialog`, `DialogGUIBase`, `UISkinDef`) — reference for theming and input-lock behavior.
- `Drawing.cs:88-91` — GL immediate-mode precedent.
- `HighlightingSystem` — command buffer injection precedent.
- Debug overlays — IMGUI usage precedent (what we aim to replace in our own mods).

## 4. Compatibility concerns

- Runtime: **Mono** (not IL2CPP), x64-only, .NET 4.x-era API surface (`Versioning.cs:58-60`, `AssemblyLoader.cs:1404-1411`). Managed wrapper can use modern-ish C#; P/Invoke is available.
- Graphics APIs: game branches on D3D/OpenGL/OSX/Linux; native plugin must at minimum support D3D11 + OpenGL, and check `SystemInfo.graphicsDeviceType` before hooking.
- Dependency names must match the library's exact `KSPAssembly` name — consumer mods must reference it exactly.
- No in-game missing-dependency UI: the library should ship its own user-facing incompatibility notice (gap in KSP itself).

## 5. Gaps — require runtime probing or user decisions

- **Player settings unknown**: scripting runtime version, API compatibility level, graphics API order, color space — not in the dump. Must be read from the KSP install (`boot.config`) or at runtime.
- **Scene contents unknown**: canvas render modes, camera depths, culling masks — serialized scene data. Affects *where* to inject rendering; needs runtime inspection.
- **Render-thread injection unproven**: no `GL.IssuePluginEvent` precedent in game code — needs a proof-of-concept spike before committing the design.
- **ImGui C# binding strategy**: dump can't answer; options include P/Invoke to cimgui vs a hand-rolled C ABI over a curated widget subset. User decision.
- **Distribution mechanics** (CKAN, forum release, license of ImGui/cimgui — MIT, fine) — user decision.
