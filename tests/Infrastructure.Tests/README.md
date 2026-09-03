# tests/Infrastructure.Tests

Tests for managed Infrastructure (primarily SettingsStore ConfigNode round-trip, AC12).

**Deferral decision (2026-09-03, Scope C of session `2026-09-03_Bug_ClickThrough_WindowAnchor`):**
the SettingsStore ConfigNode round-trip test is deferred — it needs Assembly-CSharp
(KSP install) references at test runtime. `InputLockGateway` mask computation (including
the MAIN_MENU bit added for ISSUES #001) is likewise KSP-type-coupled (`ControlTypes`) and
is covered by the in-game gates G2/G7 instead of unit tests. Both remain candidates for a
future integration-test harness that runs against a pinned KSP install.
