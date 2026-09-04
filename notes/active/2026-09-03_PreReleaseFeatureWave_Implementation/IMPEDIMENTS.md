# Impediments: DearImGui-KSP Pre-Release Feature Wave Implementation

## I-01 (C1): Color/Color32→ImVec4 conversion helper location — contract vs. layering invariant

- **Found by:** Chunk C1 (Unity math types in Application / D24), Phase 0.
- **Contract text:** CHUNK_C1_CONTRACT.md step 4 — "`ImGuiInternal` gains what the facade
  needs: `BeginScrollRegion` and `Dummy` overloads taking `ImVec2` ... and a
  `Color`/`Color32` → `ImVec4` conversion helper".
- **Conflicting invariant:** AGENTS.md — "`Infrastructure/` = the **only** KSP/Unity-touching
  layer"; PLAN_DIGEST.md invariant 3 — "Application references UnityEngine.CoreModule for
  `Vector2`/`Color`/`Color32` **only**". Putting a `UnityEngine.Color` parameter in
  `DearImGuiKSP.Interop.ImGuiInternal` would make the Interop layer Unity-touching, which
  neither the layering invariant nor the D24 amendment grants.
- **Resolution taken:** the conversion helpers are `private static` methods in
  `DearImGuiKSP/Application/DearImGuiKSP.cs` (same assembly, trivially accessible to the
  facade). `ImGuiInternal` still gained the `ImVec2` overloads as the contract specifies;
  only the helper's declaring type differs. No functional difference; the Interop layer
  remains Unity-free (grep-verifiable).
- **Held back:** nothing functional. If the Implementation Lead rules the contract text
  literal (helper in `ImGuiInternal`), it is a mechanical move of two private methods —
  but it would then violate the layering invariant, so an invariant amendment
  (DECISION_LOG entry) should accompany that ruling.

## I-02 (C3): Contract "nested private struct" vs C# accessibility — scope structs are public readonly

- **Found by:** Chunk C3 (Scope wrappers), Phase 0.
- **Contract text:** CHUNK_C3_CONTRACT.md line 21 — "one nested **private** struct per
  pair kind".
- **Conflicting text (same contract):** line 12 — each method "returns a readonly
  struct implementing IDisposable"; line 46 — hot-path Check 2 requires consumers to
  `var` the concrete struct with no boxing.
- **Conflict:** C# inconsistent-accessibility rules (CS0050) forbid a public factory
  returning a private nested type. A factory returning the `IDisposable` interface
  would box on every call, violating the zero-allocation hard requirement. The only
  compile-consistent reading is `public readonly struct` nested in `ImGuiEx`, one per
  pair kind (WindowScope, ScrollRegionScope, StyleColorScope, StyleVarScope — SRP
  preserved, no mode flags).
- **Secondary finding:** even `private` parameterized constructors on the nested
  structs are not callable from the containing class (CS0122, observed empirically);
  the state-carrying ctors (`WindowScope`/`ScrollRegionScope`) are therefore
  `internal` — consumers in other assemblies still cannot construct scopes directly,
  only the factory methods can.
- **Held back:** nothing. Behavior matches every other contract clause.

## I-03 (M2 gate): Loaded custom font never became the render default — ProggyClean still displayed

- **Found by:** M2 in-game gate, user report 2026-09-04 (happy path failed: UI still ProggyClean).
- **Root cause:** `ContextHost_LoadFontFromFile` (C4) appended the TTF to the atlas but
  never set `io.FontDefault`; ImGui renders with `Fonts[0]` (the embedded ProggyClean
  added in `ContextInit`) when `FontDefault` is null. The load succeeded — the in-game
  log showed v5 handshake with no fallback line, and the C4 direct-call test had verified
  return 0 — but success was invisible because the new font was never selected.
  Secondary gap: `LoadStartupFont` (C5) logged nothing on the success path, which made
  this indistinguishable from a resolver failure without instrumenting.
- **Resolution taken (Lead, patch to C4/C5 scope):** native — capture the `ImFont*` and
  set `io.FontDefault` when it is still null (first custom font = Regular wins; Medium
  stays atlas-only), `ContextHost.cpp:215-226`. Managed — `LoadStartupFont` gained
  verbose-gated `Debug` lines for the resolution result, per-call load results, and
  caught-exception detail (`DearImGuiKSPAddon.cs`). Rebuilt native (harness PASS) and
  managed (0 errors); both mirrored to the game.
- **Verification standing:** M2 gate remains OPEN pending user re-launch.
