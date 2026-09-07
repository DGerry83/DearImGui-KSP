# tests/Infrastructure.Tests

Tests for managed Infrastructure (primarily SettingsStore ConfigNode round-trip, AC12).

**Deferral decision (2026-09-03, Scope C of session `2026-09-03_Bug_ClickThrough_WindowAnchor`):**
the SettingsStore ConfigNode round-trip test is deferred — it needs Assembly-CSharp
(KSP install) references at test runtime. The same KSP/Unity-type coupling defers
the rest of the layer, all covered by in-game gates instead of unit tests:

- `InputLockGateway` — mask computation (including the MAIN_MENU bit added for
  ISSUES #001) is `ControlTypes`-coupled; covered by in-game gates G2/G7.
- `PointerBlockerGateway` / `ImguiEventEaterGateway` — uGUI EventSystem and
  IMGUI `hotControl` mechanics; verified in-game (click-through gates).
- `NativeBridge` — LoadLibrary/handshake/D3D11 gate against the real native DLL;
  exercised by every startup.
- `GameEventHooks`, `FailureNotifier`, `DearImGuiKSPLogger`, `FontResolver`,
  `LibraryControlPanel`, `LibraryPanelToolbar`, `Composition`, `DearImGuiKSPAddon` —
  GameEvents/PopupDialog/KSP-addon lifecycle/ApplicationLauncher coupling.

All remain candidates for a future integration-test harness that runs against a
pinned KSP install.

One exception noted during the 2026-09-07 review-remediation wave (triage item
T27): `PointerBlockerGateway.AdoptTopSortOrder` is pure sorting logic that could
be extracted behind an interface seam and unit-tested without KSP types. The
extraction itself was considered and **deferred by chunk C14** (Infrastructure-layer
refactoring was out of that contract's scope) — revisit if the gateway logic grows.
