# Chunk Contract: Failure Notifier + Failure-Mode Tests

## Plan: DearImGui-KSP Implementation
## Date: 2026-09-03
## Chunk ID: C13
## Advances Milestone: M5 (final chunk of M5)

### Scope

- Player-facing failure notification (spec §5.4, §7): on entering terminal `Failed`, show **one** plain-language stock `PopupDialog` per session. Technical detail stays log-only under `[DearImGuiKSP]` (already in place from C12).
- Failure *kinds* mapped to the §7.1 string table; the kind must travel with the failure so the right body string is shown.
- New init failure detection: render-hook failure (`GetRenderEventFunc` returns a null pointer after otherwise-successful init) — currently undetected.
- In-game AC8 verification per failure mode (user-driven sabotage tests; see Verification).

### Inputs (must exist before starting)

- C12 lifecycle state machine with terminal `Fail` (`Application/LifecycleStateMachine.cs`) and the addon's init-failure routing (`DearImGuiKSPAddon.cs:45`).
- §7.1 string table: `DK_FailTitle` + bodies `DK_FailNative`, `DK_FailGraphics`, `DK_FailVersion`; voice rules §7.3.
- PopupDialog API verified against the Knowledge Library dump (`index/skeletons/PopupDialog.cs:39`): simple string overload `SpawnPopupDialog(Vector2 anchorMin, Vector2 anchorMax, string dialogName, string title, string message, string buttonMessage, bool persistAcrossScenes, UISkinDef skin, bool isModal = true, string titleExtra = "")`.
- Skeletons awaiting implementation: `Application/Interfaces/IFailureNotifier.cs`, `Infrastructure/FailureNotifier.cs`.
- `NativeBridge` init result codes `InitOk…InitErrDeviceTexture` (0–7) at `Infrastructure/NativeBridge.cs:24-31`.

### Locked designs

**`Application/FailureKind.cs`** (new):
```csharp
internal enum FailureKind { NativeComponent, VersionMismatch, GraphicsApi, RenderHook }
```

**`Application/FailureText.cs`** (new, static, Unity-free): the §7.1 strings verbatim as constants named after their string IDs, plus `internal static string BodyFor(FailureKind kind)`. Mapping: `NativeComponent` → DK_FailNative, `VersionMismatch` → DK_FailVersion, `GraphicsApi` → DK_FailGraphics, `RenderHook` → DK_FailNative (§7.1 defines only three bodies; the render hook is part of the native/render component — recorded as a design note here).

**`Application/Interfaces/IFailureNotifier.cs`** (replace skeleton):
```csharp
internal interface IFailureNotifier
{
    void Notify(FailureKind kind);
}
```

**`Application/LifecycleStateMachine.cs`** (amendment — internal only):
- `Fail(string reason)` becomes `Fail(FailureKind kind, string reason)` (single call site, the addon — amend it).
- New `internal event Action<FailureKind> EnteredFailed;` fired once, after the transition to `Failed`. The terminal guard (`Fail` from `Failed` ignored) already guarantees once-per-session.
- Behavior rules unchanged otherwise (log `Error` with reason, terminal).

**`Infrastructure/FailureNotifier.cs`** (replace skeleton): implements `IFailureNotifier`.
- `Notify(kind)` shows `PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), "DearImGuiKSP_StartupFailure", FailureText.DK_FailTitle, FailureText.BodyFor(kind), "OK", false, HighLogic.UISkin)` (modal default).
- Own once-per-session guard (`_notified` flag) in addition to the state machine's terminal guard — belt and braces, since the notifier is Infrastructure and could later be called from other paths.
- All current failure triggers fire during addon `Start()` at main menu, so immediate display satisfies "shown at the main menu" (spec §5.4); document this assumption in the class comment.
- Constructor takes `ILogger`; `Debug` log when the popup is shown.

**`Infrastructure/NativeBridge.cs`** (handshake paths, per CHUNK_MAP):
- New result code `internal const int InitErrRenderHook = 8;` — after `_getRenderEventFunc()`, if the returned pointer is `IntPtr.Zero`: log `Error`, `DestroyDeviceTexture()`, `Unload()`, return `InitErrRenderHook`. (Only reached on successful init; no texture exists on earlier paths.)
- New `internal static FailureKind KindForInitResult(int result)`: `InitErrVersionMismatch` → `VersionMismatch`; `InitErrUnsupportedDevice` → `GraphicsApi`; `InitErrRenderHook` → `RenderHook`; everything else → `NativeComponent`.
- No native-side change, no new exports → handshake version stays 3 (invariant 4 unaffected).

### Outputs — exclusive file ownership

- Agent owns: `Application/FailureKind.cs` (new), `Application/FailureText.cs` (new), `Application/Interfaces/IFailureNotifier.cs`, `Application/LifecycleStateMachine.cs`, `Infrastructure/FailureNotifier.cs`, `Infrastructure/NativeBridge.cs`.
- **Lead wires after the agent returns** (agent must NOT touch): `Composition.cs` (`FailureNotifier` singleton; subscribe `StateMachine.EnteredFailed += kind => FailureNotifier.Notify(kind)` in `WireApplicationFacade` so notification is armed even when bridge init fails; remove the "Later chunks add: failure notifier (C13)" comment), `DearImGuiKSPAddon.cs` (`Fail` call gains `NativeBridge.KindForInitResult(Composition.BridgeInitResult)`; drop the "popup arrives in C13" comments).

### Constraints

- Application stays Unity-free: `FailureKind`/`FailureText`/interface/event reference only `System` types. PopupDialog/HighLogic appear only in `Infrastructure/FailureNotifier.cs`.
- Public API surface unchanged (invariant 2) — `Fail` signature is internal.
- Popup text must be the §7.1 strings verbatim; no technical detail, no stack traces in the dialog (§7.3).
- No git commits — the lead commits after review.
- Native interop checklist (v3 reference 08): no P/Invoke or native-surface changes in this chunk; the only NativeBridge edit is managed-side error handling.

### Verification

- `dotnet build DearImGui-KSP.slnx` — 0 errors, 0 warnings (gate G3). Native untouched — no native rebuild.
- **AC8 (user, in-game, one session per mode; restore after each):**
  1. **Missing native DLL**: rename `DearImGuiKSPNative.dll` in the instance's `GameData/DearImGuiKSP/PluginData/` → expect exactly one popup with the DK_FailNative body, detailed `[DearImGuiKSP]` log lines, `IsAvailable == false` (demo window absent), game otherwise normal. Restore the name.
  2. **Version mismatch**: temporary managed-only build with `ExpectedNativeVersion` bumped by 1 (revert after) → popup with DK_FailVersion body. 
  3. **Unsupported graphics API**: launch with `-force-glcore` → popup with DK_FailGraphics body. (Acceptable here despite C5's deferral: we are verifying the failure path, not GL rendering.)
  4. **Render-hook failure**: temporary probe build forcing `RenderEventFunc = IntPtr.Zero` after `_getRenderEventFunc()` (like the C10 probe — added, verified, removed) → popup with DK_FailNative body, `InitErrRenderHook` in the log.
- Each run also confirms: popup appears once, at the main menu; library logs continue normally otherwise; other mods (Deferred/TUFX baseline) unaffected.

### Rollback

- `git checkout -- DearImGuiKSP/Application/LifecycleStateMachine.cs DearImGuiKSP/Application/Interfaces/IFailureNotifier.cs DearImGuiKSP/Infrastructure/FailureNotifier.cs DearImGuiKSP/Infrastructure/NativeBridge.cs` and delete `Application/FailureKind.cs`, `Application/FailureText.cs`; revert lead wiring in `Composition.cs` / `DearImGuiKSPAddon.cs`.
