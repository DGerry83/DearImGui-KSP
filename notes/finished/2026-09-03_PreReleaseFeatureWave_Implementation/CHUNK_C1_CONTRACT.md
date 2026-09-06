# Chunk Contract: Unity Math Types in Application (D24)
## Plan: DearImGui-KSP Pre-Release Feature Wave
## Date: 2026-09-03
## Chunk ID: C1
## Advances Milestone: M1 (API ergonomics foundation)

### Scope
Land the D24 amendment: `Vector2`/`Color`/`Color32` in public API signatures, and the
public style push/pop API that those types serve. **Managed-only chunk.**

1. **CoreModule reference check** — `DearImGuiKSP.csproj` has no explicit UnityEngine
   reference; Unity assemblies currently arrive via KSPBuildTools 1.1.1 from the pinned
   game root. Verify `UnityEngine.CoreModule` (home of `Vector2`/`Color`/`Color32`)
   resolves at build time. If it does not, add the minimal explicit reference. Record
   which mechanism applies in the chunk report. **No other Unity assemblies may become
   load-bearing for Application** — D24 is scoped to the math structs.

2. **Public style enums** — new file `DearImGuiKSP/Application/Api/ImGuiStyleEnums.cs`
   with `public enum ImGuiCol` (63 values) and `public enum ImGuiStyleVar` (46 values) in
   namespace `DearImGuiKSP`, values identical to the internal copies C2 wrote in
   `Interop/ImGuiNative.cs` (which cite imgui.h). Then make these the single source of
   truth: delete the internal `ImGuiCol`/`ImGuiStyleVar` enums from `ImGuiNative.cs` and
   change the internal wrappers in `ImGuiNative.cs`/`ImGuiInternal.cs` to take `int`
   (the externs already cast to int internally). This removes a 63-value duplicated table.

3. **Facade additions** in `DearImGuiKSP/Application/DearImGuiKSP.cs` — additive only;
   every existing signature stays byte-identical (public-interface invariant). Same
   no-op-when-unavailable pattern and XML-doc density as existing members:
   - `PushStyleColor(ImGuiCol col, Color value)` and `PushStyleColor(ImGuiCol col, Color32 value)`; `PopStyleColor(int count = 1)`
   - `PushStyleVar(ImGuiStyleVar var, float value)` and `PushStyleVar(ImGuiStyleVar var, Vector2 value)`; `PopStyleVar(int count = 1)`
   - `BeginScrollRegion(string id, Vector2 size)` overload alongside the existing `float height` version
   - `Dummy(Vector2 size)` overload alongside the existing `float, float` version

4. **Internal support** — `ImGuiInternal` gains what the facade needs: `BeginScrollRegion`
   and `Dummy` overloads taking `ImVec2` (wrapping the existing cimgui calls), and a
   `Color`/`Color32` → `ImVec4` conversion helper (pure math, no new externs). Existing
   wrappers keep working unchanged for current callers.

### Inputs (must exist before starting)
- C2 verified: style/draw-list/radio externs + wrappers in place, build green, 59/59.
- `DearImGuiKSP/Application/DearImGuiKSP.cs` facade as-is (259 lines, read this session).
- `DearImGuiKSP.csproj` as-is (KSPBuildTools 1.1.1, no explicit Unity reference).

### Outputs (must be created/changed)
- `DearImGuiKSP/Application/Api/ImGuiStyleEnums.cs` — NEW (public enums). Also removes the placeholder `Application/Api/README.md` (scaffolding removal per INTEGRATION_CONTRACT).
- `DearImGuiKSP/Application/DearImGuiKSP.cs` — additive facade members above.
- `DearImGuiKSP/Interop/ImGuiNative.cs` — internal `ImGuiCol`/`ImGuiStyleVar` removed; wrapper params become `int`.
- `DearImGuiKSP/Interop/ImGuiInternal.cs` — wrapper params become `int`; new `ImVec2` overloads + color conversion helper.
- `DearImGuiKSP/DearImGuiKSP.csproj` — only if the CoreModule check requires it.
- `tests/Application.Tests/` — only if the test runner fails to resolve UnityEngine.CoreModule (then add the same reference mechanism there).

### Constraints
- Additive-only public API: no existing public signature changed or removed.
- D24 scope: Application may use `Vector2`/`Color`/`Color32` — no other UnityEngine types, no lifecycle/scene/object APIs. Grep the layer after implementation: the only `UnityEngine.` references in `Application/` may be those three structs.
- XML `///` docs on every new public member (project invariant; match existing density).
- No native changes; do not run the native build scripts.
- Hot-path checklist verdicts (§5.9): Check 1 N/A (no process-global state); Check 2 zero-allocation — the Color→ImVec4 conversion is struct math, no heap; Check 3 N/A (no resources acquired).

### Verification
- `dotnet build DearImGui-KSP.slnx` — 0 errors, 0 new warnings (confirms CoreModule resolves).
- `dotnet test DearImGui-KSP.slnx` — 59/59 passing (confirms the test runner resolves the new reference — or record the test-project fix if needed).
- Grep evidence: `Application/` references only `UnityEngine.Vector2|Color|Color32` from UnityEngine; public API diff is additive (list old vs new public members).
- Enum parity: public `ImGuiCol`/`ImGuiStyleVar` values match the deleted internal copies 1:1 (diff the value lists).
- Milestone contribution: M1 gate is verified after C3 (in-game + scope exception test).

### Rollback
- Revert the 4–5 touched files to the pre-chunk checkpoint (`git checkout -- <files>`); restore `Application/Api/README.md` if removed; rebuild to confirm the 59/59 baseline.
