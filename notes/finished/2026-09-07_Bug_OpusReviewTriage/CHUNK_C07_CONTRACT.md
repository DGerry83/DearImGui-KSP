# Chunk Contract: C07 — ABI enum/count validation family
## Plan: Opus Review Remediation (`notes\plans\opus-review-remediation-contracts.md`)
## Date: 2026-09-07
## Chunk ID: C07
## Advances Milestone: Post-1.0.0 remediation wave (G2-10, G3-18, G3-22, G3-23, G3-24, G3-30)

### Scope
Theme: the release native build compiles out ImGui/ImPlot's assert guards (`/DNDEBUG`), so the managed layer validates before crossing the ABI. Guards follow the no-throw widget contract: clamp + no-op + log (`DearImGuiKSP.Log?.Warn`), never an exception. All guards slot in after the C01 `CanDeclareUi` gate.
- **G2-10** (`Interop/ImGuiInternal.cs`, InputTextCore): the native call's `buf_size` was the shared grown buffer's length, ignoring the caller's `capacity`. The shared buffer is now one-reused-buffer-per-distinct-capacity (`Dictionary<int, byte[]>`, created on first use of each capacity), so `buf_size` always equals the documented per-call capacity; the seed copy clamps to `capacity - 1`. `ImGuiNative.cs` untouched (owned by the concurrent native chunk).
- **G3-18** (same region): the seed clamp is UTF-8-boundary-safe via new `ClampToUtf8Boundary` — truncating mid-sequence backs off to the sequence start, so a split multi-byte sequence can no longer persist U+FFFD into the value. (With buf_size == capacity, ImPlot/ImGui-side truncation is codepoint-wise, so only the seed path needed the fix.)
- **G3-22** (`Application/Api/ImGuiPlot.cs`, BeginSubplots): rows/cols < 1 → log + return `default(SubplotScope)` (inert, Visible false) before the ImPlot call; ImPlot's only guard was the compiled-out assert.
- **G3-23** (`Application/DearImGuiKSP.cs`, both PushStyleColor overloads): new private `IsValidStyleColor` rejects `col < 0 || col >= ImGuiCol.COUNT` (the COUNT sentinel is public) with a logged no-op — the push and the `OpenScopeTracker.StyleColors` increment are both skipped, keeping push/pop counting consistent.
- **G3-24** (both PushStyleVar overloads): same shape via private `IsValidStyleVar` against `ImGuiStyleVar.COUNT` — release-native `GetStyleVarInfo(idx)` is unchecked past the end of the var table.
- **G3-30** (`Interop/ImGuiInternal.cs`, SliderFloat): passes `ImGuiSliderFlags.AlwaysClamp` (0x600, already defined) so Ctrl+Click type-in entry clamps to [min, max] like drags. No test — flag is only observable natively; gate B item.
- `ImGuiStyleEnums.cs` needed no change: the COUNT sentinels stay (1:1 `ImGuiCol_`/`ImGuiStyleVar_` parity) and serve as the validators' upper bound.

### Inputs
- Triage sweep verdicts (`notes\active\2026-09-07_Bug_OpusReviewTriage\TRIAGE_SWEEP.md`): G2-10 VALID, G3-22/23/24 WORK, G3-30 PARTIAL-upgraded-to-live, G3-18 WORK.
- C01 guard shape (`CanDeclareUi` gate) already in place; new validation guards follow it.

### Outputs
- Changed: `Interop/ImGuiInternal.cs`, `Application/Api/ImGuiPlot.cs`, `Application/DearImGuiKSP.cs`.
- Changed (test infra): `tests/Application.Tests/FrameLoopOrchestratorTests.cs` — added `[Collection("FacadeStatics")]`. Root cause of a flake my tests exposed: `FrameLoopOrchestrator.RunFrame` writes the facade's `FrameOpen` static (production code, C01), and that class ran in its own xUnit collection, racing the collection classes that depend on facade statics (a RunFrame ending between a harness setup and a widget call cleared FrameOpen mid-test).
- New: `tests/Application.Tests/AbiGuardTests.cs` (12 tests).
- No public API signature changes (patch class, D36); new members are private (`IsValidStyleColor`, `IsValidStyleVar`, `ClampToUtf8Boundary`) or internal test seams (`GetInputTextBuffer`, `StageInputTextSeed`).

### Constraints
- No-throw widget contract preserved: every rejection path is clamp + no-op + log.
- §5.9 native-interop checklist applies (per-frame widget-call paths):
  - **Process-global state**: the per-capacity buffer dictionary is process-static, write-once-per-capacity and read-only thereafter on the single frame-loop thread (same single-threaded rationale as the buffer it replaces). Scope counters unchanged on rejection paths. Verdict: acceptable, documented here.
  - **Hot-path allocation**: steady state allocates nothing new — dictionary lookup is allocation-free, buffers are created once per distinct capacity (callsite-stable in practice), the seed copy was already a per-call `Encoding.UTF8.GetBytes` allocation (unchanged). Log strings allocate only on rejection paths. Verdict: zero new steady-state allocation.
  - **Resource-acquisition symmetry**: no acquisition; rejected pushes skip both the native push and the scope-counter increment, so Push/Pop pairing math is unaffected. Verdict: N/A, symmetry preserved.
- Scope discipline held: no shared-buffer→per-call-buffer restructuring beyond per-capacity reuse; Knob/Wheel format strings untouched; nearby NOTE/INVALID items untouched.

### Verification
- `dotnet build DearImGui-KSP.slnx` green (0 errors), `dotnet test DearImGui-KSP.slnx` green: 122/122 (baseline 110 + 12 new), 6 consecutive full-suite runs green after the FrameLoopOrchestratorTests collection fix.
- New tests (`AbiGuardTests`, `[Collection("FacadeStatics")]`):
  - PushStyleColor/PushStyleVar with COUNT (both overloads each) and with a negative cast → no-op, one warning, scope counter untouched.
  - BeginSubplots rows=0 and cols=-1 → inert scope, one warning, counter untouched.
  - InputText, via the internal seams: per-capacity buffers are right-sized (buf_size IS the buffer length, so capacity is honored by construction), reused per capacity and distinct across capacities (zero steady-state allocation), seed clamps to capacity-1 bytes, and an "ab" + 3×U+00E9 seed clamped mid-sequence keeps "ab" + one U+00E9 with no U+FFFD.
  - Intercepting the real P/Invoke is not viable on the net48 test runner: its reflection does not expose the extern method's function-pointer field (the Mono-only trick), so InputText is tested through the internal seams instead.
- In-game: gate B (user) — SliderFloat Ctrl+Click type-in clamps to [min, max]; invalid-enum/non-positive-subplot rejection warnings appear in KSP.log if exercised.

### Rollback
- Revert the three changed files (`git checkout -- <files>`); delete `tests/Application.Tests/AbiGuardTests.cs`.
