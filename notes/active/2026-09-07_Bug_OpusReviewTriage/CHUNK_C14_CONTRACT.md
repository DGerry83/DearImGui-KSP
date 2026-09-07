# Chunk Contract: C14 — Test hardening
## Plan: Opus Review Remediation (`notes\plans\opus-review-remediation-contracts.md`)
## Date: 2026-09-07
## Chunk ID: C14
## Advances Milestone: Post-1.0.0 remediation wave (G3-27 + T-series NOTE block + N13/T19 ABI-pin theme + C08's recorded G3-20 residual gap)

### Scope
Theme: test-suite hardening with one strictly-bounded production closure. Production
code changes were limited to the label-routing in the two interop files C08's record
explicitly left as a known gap; everything else is test-only. No public API changes,
no new dependencies. Baseline at start: **146 tests, all green** (confirmed by a
baseline run before any edit).

- **G3-27 — pure-helper unit tests** (`tests/Application.Tests/HelperTests.cs`, new):
  `ImGuiGradients.Pack/Lerp/Lighten`, `FailureText.BodyFor`, and
  `NativeBridge.KindForInitResult` are all internal members of the SUT assembly and
  reachable from the test assembly via the existing
  `InternalsVisibleTo("Application.Tests")` (csproj line 36) — no accessibility
  changes needed. Assertions are grounded in external truth (IM_COL32 channel
  layout from imgui.h, the spec §7.1 string table, the init-code constants), not
  in the production constants alone, so each test can fail on a real regression.
  One test (`Pack_KnownColorsMatchImCol32`) caught the author's own wrong expected
  constant on first run — evidence the pin is live, not tautological.
- **N13/T19 — ABI enum ordinal pins** (`tests/Application.Tests/StyleEnumPinTests.cs`,
  new): `ImGuiCol` (63 slots + COUNT) and `ImGuiStyleVar` (45 slots + COUNT) are
  pinned name-by-name, in declaration order, against the pinned native header. The
  expected tables were **mechanically extracted** from
  `C:\Users\Matt\source\repos\cimgui\imgui\imgui.h` (IMGUI_VERSION "1.92.9",
  IMGUI_VERSION_NUM 19290 — the sibling clone the native core compiles from per
  `DearImGuiKSPNative/vendor/PIN_RECORD.md`), `enum ImGuiCol_` (imgui.h:1839-1912)
  and `enum ImGuiStyleVar_` (imgui.h:1922-1970), via awk extraction — not copied
  from the C# enum (the sweep's tautology warning). Runtime header parsing was
  rejected: the header lives outside the repo, so a checked-in derived table with
  a regeneration comment is the honest mechanism. The pin catches renamed /
  inserted / removed / reordered members, which is exactly the drift class that
  silently recolors the wrong native style slot under /DNDEBUG.
- **C08 residual — G3-20 closure** (production, two files): C08's record listed as
  a known gap that `ExtensionShimsNative` (Toggle/Knob/Wheel labels) and
  `ImPlotNative` (BeginPlot/PlotLine/BeginSubplots titles/labels) kept their own
  unguarded `ToUtf8`, so a null/empty label there still aliased the window
  identity — contrary to the docs/10 §6 "every library widget guards" promise.
  All six shim wrappers and all four plot wrappers now encode their ID-bearing
  labels through `ImGuiInternal.ToIdUtf8` (internal, same assembly — no
  accessibility change; C08's exact semantics: silent `##dk_empty_id` sentinel
  for null/empty, verbatim otherwise). Non-ID paths deliberately keep plain
  encoding: Knob/Wheel printf `format` strings (display text feeding native
  varargs, never an item ID) still use the local `ToUtf8`/`ToUtf8OrNull`;
  `SetupAxesAutoFit` still passes real NULL axis labels (the C++ defaults).
  Side benefit: non-empty labels now get C08's single-allocation `ToUtf8`
  (G3-29) instead of the shim copies' two-array encode. `ImPlotNative`'s private
  `ToUtf8` lost its last caller to the routing and was deleted (dead code); the
  now-unused `using System.Text` went with it. XML docs updated to say ID labels
  route through the guarded helper; both class headers no longer claim
  self-containment.
- **T-series mechanical wins applied** (tests only):
  - Dead field: `FakeInputLockGateway.LastState` was written on every ApplyLocks
    call and read by **no** test — removed (the T-series "dead fields in
    TestDoubles" item).
  - Duplicated bridge double: `FrameBoundaryTests.RecordingBridge` and
    `FrameLoopOrchestratorTests.FakeNativeBridge` were two hand-copies of the
    same recording `INativeBridge` — extracted to one shared
    `TestDoubles.FakeNativeBridge` (the sweep's "AdoptTopSortOrder extraction"
    suggestion applied at the seam that actually had duplication). Behavior
    unchanged (BeginCount/EndCount/ClampCalls recording only).
  - Collection hygiene verified: all six classes that mutate facade statics carry
    `[Collection("FacadeStatics")]` (`AbiGuardTests`, `FrameBoundaryTests`,
    `FrameLoopOrchestratorTests`, `TweenEngineTests`, `TweenFaultTests`,
    `WidgetGuardTests`); the three new classes touch no facade statics and need
    no collection. No change required beyond verification.

### Inputs
- Triage sweep verdicts: G3-27 WORK ("all testable"); T-series NOTE block
  (T1–T27) "collectively argue for one test-hardening contract"; cross-cutting
  theme 5 (hand-mirrored constants without pins: ImGuiCol/StyleVar ordinals N13/T19).
- C08 contract record's "Known gap (out of C08's file list)" — the authoritative
  residual this chunk closes.
- docs/10 §6 — the empty-label guard promise that drives the sentinel semantics.
- Ground truth headers: sibling cimgui clone @ imgui 1.92.9 (PIN_RECORD.md pin).

### Outputs
- Changed (production, label-routing only): `DearImGuiKSP/Interop/ExtensionShimsNative.cs`,
  `DearImGuiKSP/Interop/ImPlotNative.cs`.
- Changed (tests): `tests/Application.Tests/TestDoubles.cs` (dead field removed,
  shared `FakeNativeBridge` added), `tests/Application.Tests/FrameBoundaryTests.cs`
  (local bridge double deleted, uses shared one),
  `tests/Application.Tests/FrameLoopOrchestratorTests.cs` (same).
- New (tests): `tests/Application.Tests/HelperTests.cs` (24 tests: 15 facts —
  10 gradient helpers, 5 failure-text — plus 9 theory rows over the init-kind
  mapping), `tests/Application.Tests/StyleEnumPinTests.cs` (2 tests),
  `tests/Application.Tests/InteropLabelRoutingTests.cs` (6 tests). 32 new tests
  total: 146 → 178.
- No new production members: the routing reuses C08's `ImGuiInternal.ToIdUtf8`;
  the routing tests pin call sites via IL metadata tokens instead of new seams
  (adding a seam member would have exceeded "label-routing only").

### Constraints
- Hard-rule compliance: production edits confined to the two interop files and
  to label routing (the dead-`ToUtf8` removal and doc/comment updates are direct
  consequences of that routing). No public API surface changed; no new test
  dependencies (xUnit/net48 only).
- No tautological tests: helper assertions use externally-grounded values; the
  enum pins are mechanically header-derived; the routing pins scan each wrapper's
  IL for `ToIdUtf8`'s method token (and, for the shims, forbid the local
  unguarded `ToUtf8` token on ID paths while requiring it to remain on the printf
  format path) — a revert of any call site to plain `ToUtf8` fails the suite.
  IL scanning reads metadata (pre-JIT), so runtime inlining cannot mask a revert.
- Known limitation of the routing pins: a byte-level token scan can in principle
  false-positive if the 4 token bytes appear in an unrelated operand; accepted —
  the affected methods are small and the token is checked per wrapper.
- `InteropLabelRoutingTests` references the unsafe `PlotLine` overloads purely by
  reflection (`ParameterType.IsPointer`), so the test project needs no
  `AllowUnsafeBlocks`.

### Deferred items (T-series deliberately skipped)
The sweep records T-items only by ID (T1–T27) without per-item text in this repo;
the following were considered during the read-through and deferred with reasons:
- **AdoptTopSortOrder extraction** (T20–T27 suggestion as recorded in the sweep):
  the referenced logic lives in `Infrastructure/PointerBlockerGateway.cs` and is
  only testable behind new UnityEngine/KSP production seams — Infrastructure-layer
  refactoring is explicitly out of scope for C14. The test-side duplication that
  *did* exist (bridge doubles) was extracted instead.
- **T13 / T27 (tests README staleness)**: README content belongs to C15/C16 per
  the chunk map; C14's file list covers "freshness where touched" and the README
  coverage-set text was not touched.
- **`TweenEngineTests.Tick_WithNoLiveTweens_InvokesNothing`** (assertion-free
  smoke test): kept — "Tick on an empty engine does not throw" is a legitimate
  no-exception contract, and removing it would reduce coverage, not harden it.
- Runtime header parsing for the enum pins: rejected (header outside repo;
  checked-in derived table is the honest, reviewable mechanism — see Outputs).
- In-game gate: not applicable (no player-visible behavior change beyond the
  docs-promised guard now actually applying to Toggle/Knob/Wheel/plot identity;
  spot-check naturally alongside gate B's empty-label item).

### Verification
- `dotnet build DearImGui-KSP.slnx` — succeeded, 0 errors (after one fix: xUnit
  theory methods must be public, so the `FailureKind` InlineData parameter became
  an int cast inside the body — internal enum can't appear in a public method
  signature).
- `dotnet test DearImGui-KSP.slnx` — **three consecutive full-suite runs green,
  178/178** (baseline was 146; +32 new, all passing). One mid-development failure
  was the author's own wrong IM_COL32 constant in a new test (fixed; kept as
  evidence the pin is live).
- Suite exercised the FacadeStatics collection under parallel xUnit execution in
  all three runs — no flakes observed.

### Rollback
- `git checkout -- DearImGuiKSP/Interop/ExtensionShimsNative.cs DearImGuiKSP/Interop/ImPlotNative.cs tests/Application.Tests/TestDoubles.cs tests/Application.Tests/FrameBoundaryTests.cs tests/Application.Tests/FrameLoopOrchestratorTests.cs`; delete
  `tests/Application.Tests/HelperTests.cs`,
  `tests/Application.Tests/StyleEnumPinTests.cs`,
  `tests/Application.Tests/InteropLabelRoutingTests.cs`.
