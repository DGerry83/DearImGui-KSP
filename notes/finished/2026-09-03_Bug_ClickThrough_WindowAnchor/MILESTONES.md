# Milestones: ISSUES #001, #002, unit tests
## Date: 2026-09-03
## Source: [ARCHITECTURE_CONTRACT.md](ARCHITECTURE_CONTRACT.md)

| # | Milestone | Components | Verification | Success Criteria | Gates |
|---|-----------|------------|--------------|------------------|-------|
| 1 | #001 pointer blocker — clicks over ImGui windows no longer reach uGUI/world picking below | IPointerBlockerGateway, PointerBlockerGateway, InputCaptureTracker, InputLockGateway (MAIN_MENU), Composition | In-game (user): main menu, KSC, flight with demo window over stock UI; D16 mod environment | Clicks swallowed over ImGui windows; ImGui widgets still work; no coexistence regressions | G1, G2 |
| 2 | #002 viewport clamp — opt-out settings.cfg clamp on resolution change + handshake v4 | LibraryConfig/LibrarySettings/SettingsModel/SettingsStore, settings.cfg, INativeBridge/NativeBridge, ContextHost, DearImGuiKSPNative.cpp | In-game (user): resolution reduction with near-edge window; deliberate version-mismatch run | Windows end fully visible (when they fit) with setting on; old behavior with setting off; mismatch fails safely | G3, G4 |
| 3 | Unit tests — xUnit Application suite runs green | tests/Application.Tests csproj + suites, InternalsVisibleTo, slnx, test README deferral notes | `dotnet test` locally | Suite green; covers the five Application types incl. new blocker/settings logic | G5 |
| — | Cross-cutting regression (after every milestone) | full build + harness + D16 smoke | build.bat, harness.exe, dotnet build, in-game smoke | Build green, HARNESS PASS, no public API diff | G6, G7 |
