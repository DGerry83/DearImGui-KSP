# Chunk Contract: C10 — Font reachability (G3-10)
## Plan: Opus Review Remediation (`notes\plans\opus-review-remediation-contracts.md`)
## Date: 2026-09-07
## Chunk ID: C10
## Advances Milestone: Post-1.0.0 remediation wave (G3-10)

### Scope
- **G3-10, decision already recorded** (triage sweep row G3-10, 2026-09-07; commit f5fffbc): DROP the dead IBM Plex Medium atlas load. No PushFont/font-selection API exists to reach the second atlas entry, and adding one is a SemVer-minor public feature (D17/D36 lockstep) — out of scope for a patch-class remediation wave; deferred as potential 1.1.0 work via DesignSpecRefinement.
- `FontResolver.Resolve` no longer resolves a secondary file for `IBMPlexSans`; `FontResolution.SecondaryPath` and the `PlexSecondaryFile` constant are removed. The `FontResolution` class doc gains the one-line deferred-font-selection note.
- `DearImGuiKSPAddon.LoadStartupFont` drops the secondary load + its debug-log field; a single successful primary load now returns.
- `GameData\DearImGuiKSP\Fonts\IBMPlexSans-Medium.ttf` removed from the staging tree (dead asset once nothing loads it).
- `package_release.bat` step-4 existence check for the Medium TTF removed (deviation: C11 owns that file, but leaving the fail-loud check would break release packaging after the staging removal — one line, called out for C11's awareness).

### Inputs
- Triage sweep row G3-10 with the recorded DROP decision.

### Outputs
- Changed: `DearImGuiKSP/Infrastructure/FontResolver.cs` (note: the plan listed it under `Application/`; it actually lives in `Infrastructure/`), `DearImGuiKSP/Infrastructure/DearImGuiKSPAddon.cs`, `package_release.bat`.
- Deleted: `GameData/DearImGuiKSP/Fonts/IBMPlexSans-Medium.ttf`.
- Stale references left for the later docs/native contracts (flagged, not fixed here): `docs/00-getting-started.md:25` still lists the Medium TTF in the install tree (C15/C16); native comments still mention the Medium second load (`DearImGuiKSPNative/src/ContextHost.cpp:384`, `ContextHost.h:144`, `DearImGuiKSPNative.cpp:46` — native side, out of this managed-only chunk's scope); `FontResolver.cs:57`-area fontScale/uiScale "multiply" comment (I28) is explicitly C16's. Design-spec history (D29/Q8, "Regular + Medium bundled") remains as the historical record.
- No test changes: FontResolver is Infrastructure and has no unit coverage (Infrastructure.Tests intentionally deferred); the removed code had none either.

### Constraints
- Public API surface unchanged (all touched members internal; patch class, D36). Font rendering behavior unchanged: the Medium face was loaded into the atlas but never selected, so nothing on screen referenced it.
- §5.9: N/A per the plan (font loading is gated pre-first-frame).

### Verification
- `dotnet build DearImGui-KSP.slnx`: 0 errors, 0 warnings.
- `dotnet test DearImGui-KSP.slnx`: 129/129 green. (Two mid-verification retries were needed — a concurrent agent was mid-edit in `Application/Api/*` and `Application/DearImGuiKSP.cs`; failures pointed only at their files and cleared after 60 s waits.)
- `grep` clean: no remaining managed references to `SecondaryPath`/`PlexSecondary`/`IBMPlexSans-Medium`.
- In-game: gate B (user) — fonts render unchanged (default `IBMPlexSans`, `ProggyClean`, and fallback path all as before).

### Rollback
- Revert the three changed files; restore the deleted TTF (`git checkout -- GameData/DearImGuiKSP/Fonts/IBMPlexSans-Medium.ttf` pre-merge, or from the parent commit).
