# Chunk Contract: Scope Wrappers (IDisposable Begin/End Guards)
## Plan: DearImGui-KSP Pre-Release Feature Wave
## Date: 2026-09-03
## Chunk ID: C3
## Advances Milestone: M1 (API ergonomics foundation) — completes M1

### Scope
Public RAII scope guards over every existing Begin/End (and Push/Pop) pair, then migrate
the demo mod to the new API. **Managed-only chunk.**

1. **New file `DearImGuiKSP/Application/Api/ImGuiEx.cs`** — `public static class ImGuiEx`
   in namespace `DearImGuiKSP`. Each method returns a **readonly struct implementing
   `IDisposable`** (no classes — per-frame use must not allocate; `using` over a struct
   does not box). Every scope's `Dispose` calls the matching End/Pop via the existing
   facade (which no-ops when unavailable), so the stack closes even when the consumer's
   `using` block throws inside the fault barrier.
   - `Window(string name)` → scope wrapping `BeginWindow`/`EndWindow`; exposes `bool Visible` (the Begin result) so consumers can skip content for collapsed windows. Dispose always calls `EndWindow`.
   - `ScrollRegion(string id, float height)` and `ScrollRegion(string id, Vector2 size)` → scope wrapping `BeginScrollRegion`/`EndScrollRegion`; same `Visible` pattern.
   - `StyleColor(ImGuiCol col, Color value)` / `StyleColor(ImGuiCol col, Color32 value)` → scope whose Dispose pops exactly one color.
   - `StyleVar(ImGuiStyleVar var, float value)` / `StyleVar(ImGuiStyleVar var, Vector2 value)` → scope whose Dispose pops exactly one var.
   - Structs are single-purpose (SRP): one nested private struct per pair kind, not one struct with a mode flag.
   - Contract note: scopes must be disposed within the same frame/callback that created them (immediate-mode rule); state this in XML docs.

2. **Demo migration** — `DearImGuiKSPDemo/DemoConsumer.cs` and `BenchmarkUI.cs`:
   redraw both demo windows through the new API (`using (ImGuiEx.Window(...))`,
   Vector2 overloads where natural). Public API only — the demo must not touch internals.
   Add one clearly-labeled test hook in the demo window: a button
   `Throw inside scope (test)` that throws inside a `using` scope on click — the fault
   barrier catches it; used to verify stack symmetry in-game.

### Inputs (must exist before starting)
- C1 verified: public `ImGuiCol`/`ImGuiStyleVar`, facade style push/pop + Vector2 overloads; build green, 59/59.
- C2 verified: internal wrappers beneath them.
- `DearImGuiKSPDemo/DemoConsumer.cs`, `BenchmarkUI.cs` as-is.

### Outputs (must be created/changed)
- `DearImGuiKSP/Application/Api/ImGuiEx.cs` — NEW.
- `DearImGuiKSPDemo/DemoConsumer.cs`, `DearImGuiKSPDemo/BenchmarkUI.cs` — migrated to scope API + test button.
- No other files. Facade (`DearImGuiKSP.cs`) unchanged — ImGuiEx calls it.

### Constraints
- Zero per-frame allocation: scopes are structs; no closures, no cached delegates, no string work beyond what the facade already does.
- XML `///` docs on every new public member, matching facade density; document the same-frame disposal rule and the `Visible` pattern.
- Demo migration must preserve current behavior 1:1 (same windows, same widgets, same benchmark) — only the declaration style changes, plus the one new test button.
- Public API remains additive.
- Hot-path checklist (§5.9): Check 1 N/A; Check 2 **zero** (struct scopes — verify no boxing: structs implement IDisposable directly and consumers use `var`); Check 3: Dispose is the release point on every exit path — that is the chunk's entire purpose; confirm both the success path and the exception path reach End/Pop exactly once.
- No native changes.

### Verification
- `dotnet build DearImGui-KSP.slnx` — 0 errors, 0 new warnings.
- `dotnet test DearImGui-KSP.slnx` — 59/59.
- In-game (user-assisted at the M1 gate): demo and benchmark windows render identically through the new API; clicking `Throw inside scope (test)` logs the fault-barrier catch and the UI keeps rendering correctly on subsequent frames (stack symmetry).
- IL/struct check: scope types are structs (report the declarations); no `class` scope types.
- Milestone gate M1: this chunk + the in-game checks above close M1.

### Rollback
- Revert the 3 touched/added files to the pre-chunk checkpoint; rebuild to confirm the 59/59 baseline.
