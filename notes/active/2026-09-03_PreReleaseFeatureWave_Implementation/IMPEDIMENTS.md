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

## I-04 (C8): "dark" = `StyleColorsDark()` + zero overrides does not restore style VARS after a ksp→dark switch

- **Found by:** Chunk C8 (Theme engine), Phase 0/implementation.
- **Contract text:** CHUNK_C8_CONTRACT.md Constraints — '"dark" must be byte-exact
  stock ImGui dark: implement it as native `StyleColorsDark()` + zero
  overrides'; Verification — 'live switch to "dark" matches stock'.
- **Reality:** In imgui 1.92.9 `ImGui::StyleColorsDark()` sets ONLY the
  `ImGuiStyle::Colors` table. Scalar/vector style fields (WindowRounding,
  WindowBorderSize, paddings, ...) are `ImGuiStyle`-constructor defaults and are
  never touched by it (verified by direct read-back in
  `DearImGuiKSPNative/build/c8_verify.ps1`: after writing WindowRounding=6,
  `StyleColorsDark()` leaves it at 6 while Colors[WindowBg] restores byte-exact).
- **Consequence:** fresh session with `theme = dark` is byte-exact stock dark in
  colors AND vars (ContextInit's ImGuiStyle ctor defaults hold). But after a
  live ksp→dark switch, the 9 var slots the ksp preset wrote (rounding 4–6 px,
  1 px border, WindowPadding) persist — only colors return to stock.
- **Resolution taken:** implemented the contract mechanism literally (native
  `StyleColorsDark()` + zero overrides; no hand-copied table of any kind). The
  ksp preset's var set is deliberately small (8 float + 1 vec2 slot). If the
  Lead rules that "live switch to dark" must also restore vars, the clean fix
  is one native line — `ImGui::GetStyle() = ImGuiStyle();` before
  `StyleColorsDark()` inside `ContextHost_StyleColorsDark` (full ctor reset, no
  hand-copied values) — but that exceeds the contract's literal export
  definition, so it is held back for a Lead ruling.
- **Held back:** the optional var-reset line above; nothing else.

## I-04 (C8): StyleColorsDark resets colors only — live ksp→dark switch kept ksp's var overrides

- **Found by:** Chunk C8, Phase 0 (verified by direct read-back against imgui 1.92.9).
- **Issue:** `ImGui::StyleColorsDark()` rewrites only the Colors table. After a live
  ksp→dark switch, the 9 style-var slots ksp wrote (rounding, padding, borders) persisted,
  so "dark" was not byte-exact stock on the switch path (fresh dark sessions were exact).
- **Lead ruling:** ACCEPTED the chunk's documented one-line fix —
  `ImGui::GetStyle() = ImGuiStyle();` before `StyleColorsDark()` in
  `ContextHost_StyleColorsDark` (ContextHost.cpp:275-284). Default-constructed ImGuiStyle
  + StyleColorsDark == stock style by construction. Applied by the Lead, all 3 native
  builds 0 errors, harness PASS, mirrored to game.

## I-05 (C10): Contract's `ToggleFlags` subset names `KnobInset` — no such flag exists in imgui_toggle.h

- **Found by:** Chunk C10 (imgui_toggle end-to-end), Phase 0, after vendoring.
- **Contract text:** CHUNK_C10_CONTRACT.md step 5 — "`public enum ToggleFlags` ... values verified against
  `imgui_toggle.h` (`ImGuiToggleFlags` subset: None, Animated, Bordered, KnobInset — only what the shim forwards)."
- **Reality (pinned commit 2c178f539693117ca736504c22e63ba8a0b1c4f5, imgui_toggle.h:39-53):**
  `ImGuiToggleFlags_` = None(0), Animated(1<<0), BorderedFrame(1<<3), BorderedKnob(1<<4),
  ShadowedFrame(1<<5), ShadowedKnob(1<<6), A11y(1<<8), Bordered(BorderedFrame|BorderedKnob),
  Shadowed(ShadowedFrame|ShadowedKnob). **There is no `KnobInset` flag** — knob inset is an
  `ImOffsetRect` config field (`ImGuiToggleStateConfig::KnobInset`, imgui_toggle.h:152), reachable
  only through the `ImGuiToggleConfig` overload, which the same contract explicitly excludes
  ("no config-struct or preset overloads (YAGNI)").
- **Resolution taken:** implemented the real flag subset instead: None, Animated, BorderedFrame,
  BorderedKnob, ShadowedFrame, ShadowedKnob, A11y, plus the two upstream shorthands Bordered and
  Shadowed (the contract's "Bordered" name exists upstream as the shorthand). All values cited
  to imgui_toggle.h lines in XML docs. `KnobInset` held back — it cannot be forwarded by the
  flags shim without adding a config-struct overload, which the contract forbids.
- **Held back:** any knob-inset knob control. If a consumer asks for it, the route is a
  third shim overload taking inset params (still not a flag), decided at that time.
