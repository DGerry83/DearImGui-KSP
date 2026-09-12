# Milestones: Consumer API Additions — Row / Combo / Tooltip (1.3.0)
## Date: 2026-09-12
## Source: [ARCHITECTURE_CONTRACT.md](ARCHITECTURE_CONTRACT.md)

| # | Milestone | Components | Verification | Success Criteria | Chunk Group |
|---|-----------|------------|--------------|------------------|-------------|
| 1 | FR-1 Row works (library) | `RowState.cs`, `ImGuiEx.Row`/`RowScope`, `RowItemHook` + widget insertions, `SameLine(float)` internal overload, reset wiring, `RowStateTests.cs` | `dotnet build DearImGui-KSP.slnx` + `dotnet test` (row tests) | Build green; row-state unit tests pass; first item never SameLine'd; nested row inert | G1, G2 (unit part), G5 |
| 2 | FR-2 Combo works (library) | Combo bindings in `ImGuiNative`/`ImGuiInternal`, `DearImGuiKSP.Combo.cs`, `ComboLogicTests.cs` | build + test (combo tests) | Build green; preview/clamp unit tests pass; try/finally EndCombo in place | G1, G3 (unit part), G5 |
| 3 | FR-3 Tooltip works (library) | Tooltip bindings, `DearImGuiKSP.Tooltip.cs`, hovered-flags enum + pin test, guard tests | build + test | Build green; guard + pin tests pass; no variadic P/Invoke added | G1, G4 (unit part), G5 |
| 4 | Docs + demo updated | `docs/20,60,70,00`, `DemoConsumer.cs` | build (demo compiles against new API); doc greps | Absence notes replaced; mapping rows present; demo shows grid/combo/tooltips; demo builds | G6, G7 |
| 5 | Release prep | csproj/`.version` pairs, `CHANGELOG.md`, `package_release.bat` | script run; zip listing | Library 1.3.0 + Demo 1.3.0 zips in `dist\`, symbols archived, handshake unchanged | G8 |
| 6 | In-game acceptance | Built mod in pinned test instance | User-driven in-game check (D3D11 primary, GL smoke) | 4×4 grid lays out in rows incl. autoResize window; combo scrolls 50+ items without clipping; tooltips appear with stock delay and wrap; no regression to existing demo widgets | G2/G3/G4 (in-game parts), G9 |

Milestones are sequential gates: do not start N until N-1 passes verification.
M1–M3 = Scope A (single implementer). M4 = Scopes B+C (parallel, after A).
M5–M6 = parent agent with the user.
