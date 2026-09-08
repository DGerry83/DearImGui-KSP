# Milestones: Window Docking (ISSUES #011)
## Date: 2026-09-08
## Source: [ARCHITECTURE_CONTRACT.md](ARCHITECTURE_CONTRACT.md)

| # | Milestone | Components | Verification | Success Criteria | Chunk Group |
|---|-----------|------------|--------------|------------------|-------------|
| 1 | Native foundation builds | ContextHost flag/export/clamp-skip, handshake v9 native | `build.bat` + `build_release.bat` (pin check green), harness builds | Both DLLs build; GetVersion returns 9 | G1 |
| 2 | Managed docking surface works | Interop bindings, facade + enums, settings end-to-end, applier, control panel, handshake v9 managed | `dotnet build` + `dotnet test` | Build passes; full xUnit suite green incl. new docking tests | G2–G4 |
| 3 | Showcase + docs | DemoConsumer dock layout, docs, optional theme mapping | Demo build; docs review | Layout code compiles against final API; docs cover setting + limitations | G8 |
| 4 | In-game acceptance | Everything, on both backends + D16 instance | User-driven in-game runs | D3D11 + GL docking verified; input/clamp regressions absent; compat clean | G5–G7 |

Milestone N does not start until N-1 is verified. M1 ∥ M2 may run concurrently (contract-pinned names); M3 after M2; M4 last (user verification).
