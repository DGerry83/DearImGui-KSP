# Investigation Log: Consumer API Additions — Row / Combo / Tooltip (library 1.3.0)
## Date: 2026-09-12
## Status: Scope Checked
## Type: Feature (abbreviated Phase 0 per BugfixPlanning.md change-type switch)

**Source document:** `notes\plans\FEATURE-REQUEST-CinematicRecorder-widgets-1.3.0.md`
(requesting consumer: CinematicRecorder; CR migration design spec at
`C:\Users\Matt\source\repos\CinematicRecorder\ReferenceNotes\active\2026-09-12_DesignSpec_DearImGuiUIMigration\DESIGN_SPEC.md`,
decision D-U3 / Q-U3 / Q-U4 — approved by library owner).

**Routing:** Feature class per FlyByWire v3 Router.md → BugfixPlanning.md (feature
mode). Three small features sharing one release vehicle: library minor **1.3.0**.

### Symptom / Scope Profile
- **Primary Goal**: Ship three curated public API additions in one non-breaking
  minor (1.3.0) so CinematicRecorder can declare
  `KSPAssemblyDependencyEqualMajor("DearImGuiKSP", 1, 3)` and migrate off Unity IMGUI:
  - **FR-1 — Horizontal layout** (HARD BLOCKER): `ImGuiEx.Row()` scope (preferred)
    or public `SameLine()`; rows/grids for FPS selectors, duration row, 2×2 curve
    grid, 4×4 camera-slot grid, preset row, label+control pairs.
  - **FR-2 — Combo/dropdown**: index-based combo over `IReadOnlyList<string>`;
    empty/out-of-range tolerant; popup scrolling for 100+ item lists.
  - **FR-3 — Tooltip**: `ImGuiEx.Tooltip(text)` = hover tooltip on the previous
    item; multi-line + wrapped text.
- **Error Messages**: N/A (feature).
- **Reproduction Steps**: N/A (feature).
- **Conditions Required**: Release protocol applies — library csproj `<Version>` +
  AVC `.version` template bumped in lockstep to 1.3.0; demo changed (new examples)
  so demo pair bumps too (current demo 1.2.1); CHANGELOG sections;
  `package_release.bat`; new GitHub release with both zips; never replace a
  published zip. ISSUES #016 stepping-stone does NOT apply to a minor bump.
- **Success criteria** (from FR): docs updated (`20-widgets.md`, `60-migration-from-imgui.md`,
  plus doc-drift fixes), demo examples added (4×4 grid, 50+ item combo, tooltip),
  xUnit coverage for pure managed logic (row state tracking, combo index clamping),
  CR-side smoke test after release (4×4 grid, preset selector, #007 tooltip).

### Codebase Verification Findings (evidence for Phase 1)

**FR-1 — Row/SameLine: binding already exists internally; only public surface is missing.**
- `DearImGuiKSP/Interop/ImGuiNative.cs:186-189` — `igSameLine` P/Invoke present;
  internal wrapper `SameLine()` at `ImGuiNative.cs:539-541`.
- `DearImGuiKSP/Interop/ImGuiInternal.cs:293-298` — internal facade re-exposure.
- Current internal callers: `DearImGuiKSP/Application/DearImGuiKSP.cs:316`,
  `DearImGuiKSP/Infrastructure/LibraryControlPanel.cs:115,150`.
- No public exposure anywhere (docs `20-widgets.md:644-647` confirms "No public
  SameLine" as of 1.2.0).
- Scope idiom confirmed: `Application/Api/ImGuiEx.cs` — static class, factories
  returning `readonly struct` scopes (Window/ScrollRegion/TabBar/TabItem/
  StyleColor/StyleVar), Dispose-on-throw safety via FaultBarrier, no-op when
  library unavailable. `Row()`/`RowScope` fits this idiom exactly.

**FR-2 — Combo: no bindings today, but all needed cimgui exports are non-variadic and already compiled into the shipped native DLL.**
- Verified against sibling cimgui clone `C:\Users\Matt\source\repos\cimgui`
  (docking_inter branch, imgui v1.92.9-docking-1 — the exact source compiled by
  `DearImGuiKSPNative/build.bat`, which compiles `cimgui.cpp` directly in):
  `igBeginCombo`, `igEndCombo`, `igSelectable_Bool`, `igSelectable_BoolPtr` — all
  `CIMGUI_API`, none variadic.
- **No native rebuild expected** (consistent with FR cross-cutting requirement 2).

**FR-3 — Tooltip: the direct call is variadic; a non-variadic composition path exists.**
- `igSetItemTooltip` / `igSetTooltip` are variadic (`fmt, ...`) — violates the
  repo's binding convention ("No variadic functions — P/Invoke cannot call
  varargs", `ImGuiNative.cs:112`). Precedent: text goes through
  `igTextUnformatted(byte[], IntPtr)` (`ImGuiNative.cs:130-133, 470-473`).
- Non-variadic alternative confirmed exported: `igIsItemHovered` (with
  `ImGuiHoveredFlags_DelayNormal` / `_Stationary` available), `igBeginTooltip`,
  `igEndTooltip`, `igPushTextWrapPos`, `igPopTextWrapPos`, `igGetStyle`. A curated
  `Tooltip(text)` can compose hovered → BeginTooltip → wrap pos → TextUnformatted →
  EndTooltip. Exact hover-flag/delay semantics = Phase 1 design decision.

**Docs drift confirmed (FR cross-cutting item 1):**
- `docs/70-troubleshooting.md:31` — example says "expected version 7"; handshake
  shipped at v9 in 1.2.0 (AGENTS.md). Fix to v9.
- `docs/00-getting-started.md:90` — "The current library version is **1.0.0**";
  fix to 1.3.0 (also fixes that it's stale vs 1.2.0 today).
- `docs/20-widgets.md:644-651` — "No public SameLine" + "no public checkbox,
  combo, ..." absence notes; replaced/updated by the new Layout and Combo sections.
- Handshake: no protocol change expected (new managed-side wrappers over existing
  native exports) → no handshake version bump. Confirm in Phase 1.

**Version state:** library csproj `<Version>1.2.0</Version>`
(`DearImGuiKSP/DearImGuiKSP.csproj:11`) → 1.3.0; demo `<Version>1.2.1</Version>`
(`DearImGuiKSPDemo/DearImGuiKSPDemo.csproj:10`) → bump (demo gains examples).
Library `DearImGuiKSP.version` AVC template bumps in lockstep with csproj.

### Data Loss / Risk Assessment
- **Corruption Risk**: No. Additive public API; no existing signatures touched
  (non-breaking minor per the versioning contract, D17/D36).
- **Affected State**: Public API surface (Critical Path review class for API-shape
  decisions — 1.0+ stability policy means these signatures are permanent);
  internal widget logic (Standard class).
- **Change Risk Level**: Low–Medium. No native rebuild anticipated; no backend or
  render-thread changes; no handshake bump anticipated. Medium only because new
  public API is forever (signature review care) and combo popup behavior near
  autoResize window edges needs in-game verification (FR edge case).
- **Native Interop & Hot-Path Checklist** (FlyByWire v3 reference/08-native-interop.md):
  applies — new P/Invoke bindings (combo/selectable/tooltip compose path) on the
  per-frame widget path. Record verdicts in the chunk contracts during Phase 1/2.

### Fix / Feature Candidates
- **Candidate 1 (recommended): All three FRs in one 1.3.0 minor.** FR's stated
  preference; CR's dependency declaration and migration spec assume the combined
  surface; all three are small and additive.
  **Pros**: one release cycle, CR unblocked in one step, single docs/demo pass.
  **Cons**: slightly larger release surface.
- **Candidate 2 (fallback per FR): FR-1 only in 1.3.0, combo+tooltip in 1.4.0.**
  **Pros**: smallest first release. **Cons**: second release cycle; CR phases
  U3/0.3.0 stay blocked; FR explicitly deprefers.
- **FR-1 API shape candidates** (Phase 1 decision): (a) `ImGuiEx.Row()` scope
  only — matches library idiom, prevents SameLine leakage, consumer-preferred;
  (b) scope + also public `SameLine()` — more flexible, more surface to support
  forever; (c) raw `SameLine()` only — cheapest, but breaks the curated idiom and
  enables the classic leakage bug the FR warns about.

## Phase 0 Checkpoint
Scope confirmed against the codebase; no suspected underlying bug; candidates
presented. Phase 0 complete. Waiting for approval to proceed to Phase 1.
