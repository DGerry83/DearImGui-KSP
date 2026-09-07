# DearImGui-KSP — Fix Backlog

Verified against source Ordered by priority.

---

## P0 — `SettingsModel.NormalizeTheme` discards all custom themes

**File:** `DearImGuiKSP/Application/SettingsModel.cs`

```csharp
private static string NormalizeTheme(string value) =>
    string.Equals(value, LibraryConfig.DefaultTheme, StringComparison.OrdinalIgnoreCase)
        ? LibraryConfig.DefaultTheme
        : LibraryConfig.DefaultTheme;
```

Both branches return `LibraryConfig.DefaultTheme`. Any theme a player sets in `settings.cfg`, or via the `Theme` setter, is silently overwritten with the default on every load and every write.

**Fix:** the false branch should return `value` (or a canonicalized form of it), likely with validation against a known theme list, e.g.:

```csharp
private static string NormalizeTheme(string value) =>
    string.IsNullOrEmpty(value) ? LibraryConfig.DefaultTheme : value;
```

Add/restore a unit test asserting a non-default theme round-trips through `SettingsModel`.

---

## P1 — `LifecycleStateMachine.Failed` has no recovery path, and one bad startup init call permanently disables the session

**Files:** `DearImGuiKSP/Application/LifecycleStateMachine.cs`, `DearImGuiKSP/Infrastructure/DearImGuiKSPAddon.cs`

`IsLegalTransition` hard-blocks any transition out of `Failed`. That's intentional per spec (§5.4/§7 — "session-permanent self-disable"), but it's triggered from a **single** call site:

```csharp
// DearImGuiKSPAddon.Start()
Composition.BridgeInitResult = Composition.Bridge.Initialize();
if (Composition.BridgeInitResult == NativeBridge.InitOk) { ... }
else
{
    Composition.StateMachine.Fail(NativeBridge.KindForInitResult(Composition.BridgeInitResult), ...);
}
```

`GraphicsApi` and `RenderHook` failures are treated identically to `VersionMismatch` — i.e. equally permanent — even though a graphics/device failure at that exact moment (e.g. racing a resolution switch during main-menu load) could plausibly be transient.

**Fix:** add a bounded retry (e.g. 2–3 attempts with a short delay) inside `Initialize()` for `GraphicsApi`/`RenderHook` failure kinds specifically, before calling `Fail()`. Keep `VersionMismatch` as immediate-fail (retrying won't help a real version mismatch). Do **not** build a full mid-session recovery/reinit subsystem — `Fail()` is never invoked outside this one startup path, so that scope isn't justified.

---

## P1 — `InputLockGateway.ApplyLocks` allocates a new `HashSet<string>` every capturing frame

**File:** `DearImGuiKSP/Infrastructure/InputLockGateway.cs`

```csharp
public void ApplyLocks(InputCaptureState state, IReadOnlyList<string> consumerIds)
{
    ControlTypes mask = ComputeMask(state);
    HashSet<string> desiredIds = new HashSet<string>();   // <-- fresh alloc every call while mask != 0
    ...
```

Called every `Update()` while any consumer is capturing mouse/keyboard. Low real-world impact at typical KSP mod-count scale, but it's an easy, correct fix.

**Fix:** hoist `desiredIds` to a reused instance field (`Clear()` instead of `new`), same pattern already used correctly in `InputCaptureTracker._enabledIds`.

*(Note: `InputCaptureTracker._enabledIds` itself is already correctly reused — no change needed there.)*



## P2 — `PointerBlockerGateway` sort-order tie at `short.MaxValue`

**File:** `DearImGuiKSP/Infrastructure/PointerBlockerGateway.cs`

```csharp
_canvas.sortingOrder = System.Math.Min(topOrder + 1, short.MaxValue);
```

If another canvas in the scene is already at `short.MaxValue`, the blocker ties with it, and Unity's `RaycastComparer` falls back to scene/instantiation order to break the tie — click-through/blocking behavior becomes load-order-dependent instead of deterministic.

**Fix:** at minimum, log a warning when the clamp actually engages (`_log?.Warn("Pointer blocker tied at short.MaxValue sortingOrder; click-through may be order-dependent.")`) so this is diagnosable in the field. A structural fix (e.g. sorting layer bump instead of order bump when order is maxed) is optional/lower priority since the scenario requires another mod to already be at the ceiling.



## P2 — Early `Register()`/`Unregister()` calls silently drop instead of erroring

**File:** `DearImGuiKSP/Application/DearImGuiKSP.cs`

Not a crash risk (`IsAvailable` is already null-safe on `Lifecycle`), but if another mod calls `DearImGuiKSP.Register()` before this addon's `Awake()` wires `Composition.WireApplicationFacade()`, the call is silently logged as a warning and dropped — the caller has no way to know it needs to retry.

**Fix (optional, low priority):** consider either (a) documenting the ordering requirement clearly in the public API XML docs (partially done already), or (b) queuing early `Register()` calls and flushing them once `WireApplicationFacade()` runs, so load order doesn't matter.



## Suggested order of work

1. `NormalizeTheme` fix + test (5 min, high player-visible impact)
2. `InputLockGateway` allocation fix (trivial, safe)
3. `PointerBlockerGateway` warning log on clamp (trivial)
4. Startup init retry for `GraphicsApi`/`RenderHook` (needs a bit more design/testing thought)
5. Early-`Register()` queuing (optional, only if load-order bug reports actually show up)