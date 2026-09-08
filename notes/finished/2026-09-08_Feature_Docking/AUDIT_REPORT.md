# Audit Report: Window Docking (ISSUES #011)
## Phase 3: Regression Detection
## Date: 2026-09-08
## Branch: `feature-Docking` (all changes uncommitted)
## Gates: [GATES.md](GATES.md) (frozen 2026-09-08T12:21:33-0400) | Contract: [ARCHITECTURE_CONTRACT.md](ARCHITECTURE_CONTRACT.md)

### Audit Inputs Verified

| Probe | Command / Method | Result |
|-------|------------------|--------|
| Environment cache | `dotnet --version`, `bash --version` | 10.0.301 / 5.2.37(1)-release — matches `notes/knowledge/ENVIRONMENT.md`; cache adopted |
| Change set | `git status --porcelain` | 27 modified + 7 untracked paths (3 library sources, 3 test files, session folder); matches contract file list + recorded deviations |
| Managed build (re-run by auditor) | `dotnet build DearImGui-KSP.slnx` | Build succeeded, 0 Warning(s), 0 Error(s) |
| Test suite (re-run by auditor) | `dotnet test DearImGui-KSP.slnx` | `Passed! - Failed: 0, Passed: 191, Skipped: 0, Total: 191` |
| Handshake lockstep | grep | `ExpectedNativeVersion = 9` (`DearImGuiKSP/Infrastructure/NativeBridge.cs:44`); native `return 9;` (`DearImGuiKSPNative/src/DearImGuiKSPNative.cpp:42`) |
| Native artifact currency | `stat` + binary grep | `build/DearImGuiKSPNative.dll` mtime 12:31:47 > `ContextHost.cpp` mtime 12:29:32; export string `DearImGuiKSPNative_SetDockingEnabled` present in both `build/` and deployed `GameData/DearImGuiKSP/PluginData/` DLLs. Native rebuild not re-run (per audit scope); Scope A's recorded build output stands |

## Gate Verdicts

| Gate | Recorded | Audit Verdict | Basis |
|------|----------|---------------|-------|
| G1 Native builds + GetVersion 9 | PASS | **PASS** | Source changes confirmed in diff: docking flag at init (`ContextHost.cpp:168-172`), `ContextHost_SetDockingEnabled` (:375-387) + header decl (`ContextHost.h:100-106`), clamp skip (:365-366), export + v9 (`DearImGuiKSPNative.cpp:42`, :105-112). Built DLL post-dates source and carries the new export. Recorded `build.bat`/`build_release.bat`/`build_harness.bat`/harness-run exit-0 evidence accepted per audit scope |
| G2 Managed build + full xUnit | PASS | **PASS** | Independently re-run: 0 warn / 0 err; 191/191 (baseline 178 + 13 new: 3 new test files + settings additions) |
| G3 Handshake lockstep | PASS (managed half) | **PASS** | Both constants = 9 (citations above). Mismatch path intact: `NativeBridge.cs:129-131` (`InitErrVersionMismatch` → `FailureKind.VersionMismatch`); history comment extended, block otherwise untouched |
| G4 `docking` key default/live/persist | PASS | **PASS** | Unit half in suite: default test (`SettingsModelTests.cs:26`), persisted-false load (:49-60), round-trip snapshot (`SettingsPersistenceTests.cs:133-151`); store wiring (`SettingsStore.cs:26`, :90-92, :134); live apply (`DockingModeApplier.cs`, orchestrator hook `FrameLoopOrchestrator.cs:90-94`; panel toggle `LibraryControlPanel.cs:162-170`). In-game half user-verified 2026-09-08; evidence cell complete |
| G5 In-game D3D11 smoke | PASS (user) | **PASS** | Evidence cell complete and honest: records the initial FAIL (4px auto-resize clamp defect, root cause imgui.cpp:20831-20836), the fix, and the re-run PASS with KSP.log line counts (28 `[DearImGuiKSP]` lines, 0 ERR/EXC/WRN). User-verified gate; not re-run by auditor |
| G6 In-game OpenGL smoke | PASS (user) | **PASS** | Evidence cell complete (`-force-glcore`, same smoke items). User-verified; not re-run |
| G7 D16-compat regression | PASS (user) | **PASS** | Evidence cell complete and honest: records the EndChild defect (imgui.cpp:8908-8909 path), root cause (dockspace host was dockable), demo-side fix + docs guidance, and the re-verification run (25 log lines, 0 ERR/EXC/WRN). User-verified; not re-run |
| G8 Docs + PIN_RECORD + ISSUES closure | PASS (docs portion) | **PASS (partial, deferral recorded)** | All four claimed doc changes verified in the diff: settings row + sample cfg (`docs/10-api-fundamentals.md:268`, :283), docking section (`docs/20-widgets.md:521-628`), four limitations (`docs/70-troubleshooting.md:141-144`), PIN_RECORD/README `-docking` wording. ISSUES #011 still `Open` in `ISSUES/TRACKER.md:9` — the verdict cell explicitly defers closure + TRACKER sync to the parent after G5-G7. Deferral is recorded, not hidden |

## Session Verdict

- **Verdict: CONTINUE**
- **Reason**: No gate is FAIL. G1-G4 independently spot-checked green; G5-G7 evidence cells complete and honest (defects, root causes, fixes, and re-runs all recorded); G8's open sub-items are explicitly recorded deferrals to the parent, not silent gaps.

## Frozen Gates Integrity

- [x] GATES.md predates implementation: freeze stamp 2026-09-08T12:21:33-0400; `ARCHITECTURE_CONTRACT.md`/`PLANNING_WORKSHEET.md` mtime 12:22:48; first implementation record (Scope A) is later the same day; native source first modified 12:29:32 — all after freeze.
- [x] GATES.md mtime (14:38:19) post-dates freeze — consistent with verdict/evidence cells being filled during the session; the criterion column references the contract (GATES.md:3) and shows no sign of criterion drift (all criteria traceable to contract sections: handshake D17, settings D34 pattern, docs D31, D5/D16 compat).
- [x] Evidence cells are honest, not retrofitted: G5 records the initial FAIL and remediation; G7 records the in-game defect and fix cycle; G8 records its own partial status. GATES.md is untracked (no git history to diff), so integrity rests on the PROGRESS_LOG/IMPEDIMENTS timeline, which is internally consistent (IMP-001 filed 13:08, ruled, implemented, then two fix chunks 14:08+).
- [x] Session Verdict row in GATES.md left as the `[KILL / CONTINUE]` placeholder — filled by this report's verdict (CONTINUE) for the parent to transcribe; auditor edited no file but this report.

## Invariant Check Results

| Invariant | Result | Evidence |
|-----------|--------|----------|
| Existing public signatures byte-identical | PASS | HEAD `BeginWindow(string, bool)` (`DearImGuiKSP.cs:168` @HEAD) still exists with identical signature, now delegating (`DearImGuiKSP.cs:169-172`); HEAD `ImGuiEx.Window(string, bool)` (:49 @HEAD) untouched. New 3-param overloads are additive (contract decision 5) |
| Shared state shape stable | PASS | `LibrarySettings` +1 field (:17), `SettingsModel` +1 property + snapshot field — additive only; no existing member retyped/renamed/removed |
| Layering | PASS | Native Core: zero KSP/Unity (flag/export/clamp only). `DockingModeApplier` (Application) imports `Interop` + `Application.Interfaces` only (:1-3). Facade partial imports Interop + `UnityEngine.Vector2` (allowed, D24). Interop remains one-way leaf (no Application/Infrastructure refs in diff). Demo touches facade only |
| Enum ordinals mirror pinned imgui.h | PASS | Verified against `C:\Users\Matt\source\repos\cimgui\imgui\imgui.h` (IMGUI_VERSION "1.92.9", :32): `ImGuiDockNodeFlags_` :1551-1565 (8 non-deprecated bits; 1.90 aliases :1564-1565 correctly omitted), `ImGuiDir` :1617-1625 (None=-1..COUNT=4), `ImGuiWindowFlags_NoDocking = 1 << 19` = 0x80000 (:1238). All in-code line citations accurate. Pin tests: `DockingEnumPinTests.cs:25-47` |
| Minimal change (lines vs contract) | PASS with notes | Total +684/-23 across 27 files. Per-file vs contract estimates: ContextHost.cpp 24/~40; native.cpp 14/~15; ImGuiNative.cs 117/~120; Docking.cs 178/~150; enums 75/~60; settings trio 18/~25; orchestrator 12/~10; store 5/~8; panel 18/~20; bridge 5/~2. Overshoots are XML docs/tests/fix-chunks, not scope creep — see Violations V4 |
| Handshake lockstep | PASS | Both = 9 (citations above); deployed native DLL contains the v9 export |
| XML docs on all new public members | PASS | Every facade method, enum member, and overload documented (Docking.cs, ImGuiDockingEnums.cs, DearImGuiKSP.cs:175-206, ImGuiEx.cs:54-84) |
| No emojis/symbols in docs (D31) | PASS | Non-ASCII scan of new/changed content: only em-dashes and `§`, the established codebase style (HEAD already carries 27 em-dashes in docs/20-widgets.md, 19 in DearImGuiKSP.cs, 8 in NativeBridge.cs). Zero emoji/pictographs |
| Vendored trees untouched | PASS | No diff under `DearImGuiKSPNative/vendor/` except `PIN_RECORD.md` wording (contract-listed) |

## Principles & Anti-Patterns Check (CORE_PROTOCOLS §5.5-5.7)

| Check | Result | Evidence |
|-------|--------|----------|
| SRP — `DockingModeApplier` | PASS | One sentence without "and": "applies the persisted docking flag to the native context when dirty." ThemeEngine explicitly keeps style-only scope (`DockingModeApplier.cs:13-14`) |
| SRP — facade partial | PASS | `DearImGuiKSP.Docking.cs` holds only the docking surface; existing partials untouched |
| SRP — `ContextHost_SetDockingEnabled` | PASS | Mirrors the `SetUiScale` single-flag precedent; one responsibility |
| No God Class growth | PASS | `FrameLoopOrchestrator` +1 injected dependency + one call site; `SettingsModel` +1 property following the existing per-key pattern; `LibraryControlPanel` +1 section method (matches existing section-per-key structure) |
| No global mutable state introduced | PASS | New state is instance fields (`_dirty`, `_appliedDocking`, `_dockLayoutApplied`); Composition singleton follows the existing DI pattern; `DemoDockspaceId`/`DockspaceHeight`/title strings are consts |
| Magic numbers named | PASS | `0x80000` carries imgui.h:1238 citation (`ImGuiNative.cs:18`); `0xD0C1A11` named `DemoDockspaceId` (DemoConsumer.cs:34); `300f` named `DockspaceHeight` with root-cause comment (:36-44). See V5 for the one inline literal |
| Dependency inversion | PASS | `DockingModeApplier` depends on `SettingsModel` + `ILogger` abstractions, constructor-injected (`DockingModeApplier.cs:33-38`); native access routed through Interop |
| Open/Closed | PASS | Feature lands as new files + additive overloads; the only edits to tested code are the orchestrator hook, ctor wiring, and the clamp skip — all contract-listed |
| DRY | PASS | Enums declared once, consumed as `int` by Interop ("single source of truth" note, `ImGuiDockingEnums.cs:10-11`); window titles centralized as consts shared by scopes and DockBuilder (DemoConsumer.cs:28-32) |

## Native Interop & Hot-Path Checklist (§5.9)

| Item | Verdict | Evidence |
|------|---------|----------|
| Process-global state | PASS | `ContextHost_SetDockingEnabled` mutates only the DLL's own ImGui context `ConfigFlags` (`ContextHost.cpp:375-387`) — no OS/process-global state. Startup default applied once at `ContextInit` (:171); restore N/A (runtime flag, not scoped state) |
| Hot-path allocation | PASS | Native: flag write only; clamp skip adds one field test per window per frame (:365-366), zero allocation. Managed steady state: one bool check per frame (`DockingModeApplier.cs:56-64`); per-frame `DockSpace` facade passes an `ImVec2` struct, no heap allocation. `ToUtf8` allocation occurs only in `DockBuilderDockWindow` — a one-time-per-layout call, not per-frame |
| Acquisition symmetry | PASS | No new handles/resources; the only early return (`return 1`, no context, `ContextHost.cpp:379-380`) acquires nothing. Managed `Apply` has no partial-failure state (non-fatal log + continue, `DockingModeApplier.cs:73-81`, spec §5.4 honored — never trips Failed) |
| ABI safety | PASS | New P/Invokes match pinned cimgui.h prototypes (citations in `ImGuiNative.cs:398-430`); opaque pointers (`ImGuiWindowClass*`, `ImGuiViewport*`) always NULL; `DockBuilderGetCentralNode` dereferences only the first struct field (`ImGuiID`, imgui_internal.h:2067) so no raw pointer crosses the safe surface (Q46) |

## Similar Bugs Sweep

Defect (a): zero-size `DockSpace` in an auto-resize host collapses to a 4px strip (imgui.cpp:20831-20836 clamp).
Defect (b): a window hosting a dockspace must be declared `noDocking` (EndChild error, imgui.cpp:8908-8909).

| Sweep target | Result |
|--------------|--------|
| Repo-wide `DockSpace` call sites (code) | Exactly one: `DemoConsumer.cs:251` — uses explicit `new Vector2(0f, DockspaceHeight)` (fixed). No other code call site exists |
| Other auto-resize windows | `TelemetryAddon.cs:120` (autoResize) hosts no dockspace — unaffected. LibraryControlPanel hosts no dockspace — unaffected |
| Other dockspace hosts without `noDocking` | None. The only host (DemoConsumer main window) is declared `noDocking: true` (`DemoConsumer.cs:165`) with the upstream-citation comment. Telemetry/benchmark/plot windows host no dockspace |
| docs/20-widgets.md sample | **RESIDUAL VECTOR (V1/V2)**: the copy-idiom block (`docs/20-widgets.md:599`) uses `DockSpace(MyDockspaceId, Vector2.zero)` — the exact defect-(a) trigger — and does not show the host window's `noDocking: true` declaration (defect b). Adjacent prose mitigates both ("Auto-resize caution" :561-565; "Host-window caution" :567-573), but the self-contained block a reader copies diverges from the fixed demo code it claims to mirror |
| Facade XML docs | `DockSpace` `size` param doc (`DearImGuiKSP.Docking.cs:31`) says only "Dockspace size in pixels" — the autoResize/zero-size pitfall exists in docs/20-widgets.md but not at the API surface (V3) |

## Contract-Deviation Review

| Deviation from ARCHITECTURE_CONTRACT.md file list | Ruled & recorded? | Where |
|---------------------------------------------------|-------------------|-------|
| `ContextHost.h` not in file table (additive declaration) | Yes | PROGRESS_LOG.md Scope A, line 9 ("flagged in Phase 0, additive declaration only") |
| `Composition.cs` + `DearImGuiKSPAddon.cs` not in file list (mandatory wiring) | Yes | PROGRESS_LOG.md Scope B, line 18 ("Phase 0 disagreements filed (none material)") |
| IMP-001: viewport dockspace → in-window dockspace (R1) + curated-recipe docs (D2) | Yes | IMPEDIMENTS.md IMP-001 status line: parent ruling "D1 ACCEPT — proceed with R1 (R2 rejected, YAGNI) and D2 ACCEPT" |
| Aux-window visibility defaults false → true (DemoConsumer.cs:46-48) | Yes | Recorded in G5 evidence cell ("fixed via explicit dockspace height + aux windows default visible") and fix-chunk log; behavioral change confined to the demo |
| Docs autoResize/noDocking caution notes beyond the contract's doc rows | Yes | Present at `docs/20-widgets.md:561-573`; logged in the two fix chunks |

## Violations Found

| # | Severity | Finding |
|---|----------|---------|
| V1 | Minor (docs) | `docs/20-widgets.md:599` — the labeled "idiom to copy" sample uses `Vector2.zero` for the dockspace size; pasted into an `autoResize` host it reproduces defect (a). The demo it references was fixed to an explicit height. Prose caution exists at :561-565 but the block is not self-contained-safe |
| V2 | Minor (docs) | Same sample omits the host window declaration, so the required `noDocking: true` (defect b guard) is invisible in the copied code; covered only by prose at :567-573 |
| V3 | Minor (API docs) | `DearImGuiKSP.Docking.cs:31` — `DockSpace` `size` param XML doc lacks the autoResize/zero-size pitfall warning that the user-facing docs carry |
| V4 | Informational | `ImGuiInternal.cs` +112 vs contract estimate ~30 (estimate counted only UTF-8 label handling; the 8 wrapper pass-throughs landed here). Combined Interop 229 vs ~150 estimated. Additive, single-responsibility, contract-scoped — estimate deviation, not scope creep. `DearImGuiKSP.cs`+`ImGuiEx.cs` 82/~30 and docs 117/~80 similarly doc-text-driven |
| V5 | Informational | `DemoConsumer.cs:285` — `0.4f` split ratio is an inline literal (comment explains the 60/40 intent); consistent with demo-code style |

No gate-blocking violations. No SRP/layering/anti-pattern violations.

## Recommendations

1. **Parent: transcribe verdict CONTINUE into GATES.md's Session Verdict row**, then proceed to closure items: close ISSUES #011 + sync `ISSUES/TRACKER.md` (G8 deferred half), PR-merge `feature-Docking`, and record D38-D40 decision candidates from the contract.
2. **V1/V2 fix (one small docs edit, pre-merge)**: change the docs/20-widgets.md sample's `DockSpace(MyDockspaceId, Vector2.zero)` to an explicit height (or add a one-line comment in the block pointing at the auto-resize caution), and show the host window declaration with `noDocking: true` in the sample.
3. **V3 fix (trivial)**: extend the `DockSpace` `size` param XML doc with the autoResize/zero-size note.
4. **Release prep (per contract decision 7)**: version bump to 1.2.0 deferred to release prep — remember the demo's `KSPAssemblyDependencyEqualMajor` stepping-stone rule (ISSUES #016) at that time; handshake is already code-bumped to 9 in lockstep.
