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

## I-06 (C11): cimplot regeneration — cl preprocessing breaks cpp2ffi struct tracking; implot_demo.cpp is a link-required 5th vendored file

- **Found by:** Chunk C11 (ImPlot vendor setup + cimplot regeneration), Phase 1/2.
- **Issue 1 (generator environment):** cimplot's `generator.lua` accepts `cl` as preprocessor, but under
  `cl /E /d1PP` imgui.h's `IM_MSVC_RUNTIME_CHECKS_OFF` expands to
  `__pragma(runtime_checks("",off)) __pragma(check_stack(off)) __pragma(strict_gs_check(push,off))`,
  and that line breaks the pinned cimgui cpp2ffi parser's struct context: every struct inside the
  pragma region (`ImPlotPoint`, `ImPlotRange` at implot.h v1.0 lines 607-620) was silently dropped —
  no ctors, no methods, no `_c` struct emission. Symptom in generated output: header used plain
  `ImPlotPoint` instead of `ImPlotPoint_c` and the template's hardcoded `ImPlotPoint_c` typedef failed
  to compile (`cimplot.h(922): error C2061: syntax error: identifier 'ImPlotPoint_c'`). Verified
  root cause by bisection: gcc-style preprocessing (where the macro expands to nothing, per
  `_MSC_VER` guard) parses correctly; upstream's own checked-in cimplot output also shows `_c`
  types, i.e. upstream generates with gcc (`generator.sh`), not cl.
- **Resolution taken:** ran the generator with its canonical gcc invocation
  (`luajit generator.lua gcc "internal"`) using LuaJIT 2.1 (msys2 mingw64 pkg via 7-Zip manual
  extract, scoop's 7zip dep was broken) + nuwen mingw gcc 15.2.0 — no patches to any generator
  input or output; vendored cimplot.cpp/.h are byte-identical copies of the generator output.
- **Issue 2 (contract file list insufficient):** the contract's implot file list (4 sources + LICENSE)
  cannot link: generated cimplot.cpp exports `ImPlot_ShowDemoWindow`, which calls
  `ImPlot::ShowDemoWindow` — declared unconditionally in implot.h but DEFINED only in
  implot_demo.cpp (same v1.0 tag, MIT). Upstream cimplot's own CMakeLists compiles
  `implot/implot_demo.cpp` for exactly this reason.
- **Resolution taken:** vendored `implot_demo.cpp` (md5 8fee560d37ba5a76896e803e51de748a,
  byte-identical to v1.0 tag) as a 6th file in `vendor/implot/` and added it to all three build
  scripts. PIN_RECORD implot/ Notes column updated to match what actually landed.
- **Held back:** nothing functional — all 3 native builds 0 errors/0 new warnings, harness PASS,
  606 ImPlot_* exports in the DLL. If the Lead rules the demo TU must NOT ship, the only clean
  alternative is dropping `ImPlot_ShowDemoWindow` from the ABI (an orchestrator-level ABI decision,
  since the generated wrapper cannot be removed without hand-patching generated output, which the
  contract forbids).
- **Lead ruling 2026-09-04:** ACCEPTED as implemented. (1) gcc is upstream's canonical generator
  path (`generator.sh`) — provenance is recorded, so the toolchain deviation carries no drift risk.
  (2) `implot_demo.cpp` stays: it matches upstream cimplot's own build, keeps the ABI unpatched,
  and `ImPlot_ShowDemoWindow` is useful while developing the M6 telemetry panels. (3) `/Ivendor`
  include accepted — it is what lets the generated `#include "./implot/implot.h"` resolve without
  duplicating sources.

## I-07 (C13): Vendored cimplot passes `ImPlotSpec_c`, not the raw `flags, offset, stride` tail the contract cites

- **Found by:** Chunk C13 (ImPlot managed wrapper), Phase 0 signature verification.
- **Contract text:** CHUNK_C13_CONTRACT.md — "Verify each signature against
  `DearImGuiKSPNative/vendor/cimplot/cimplot.h` (param order, types, the
  `double xscale, double x0, int flags, int offset, int stride` tail)".
- **Actual vendored ABI (verified against cimplot.h):**
  `ImPlot_PlotLine_FloatPtrInt(const char* label_id, const float* values, int count, double xscale, double xstart, const ImPlotSpec_c spec)`
  (cimplot.h:1040; double variant :1041; `ImPlot_BeginPlot`/`ImPlot_EndPlot` at :1015-1016 match the
  contract). The v1.0 spec-based API folds flags/offset/stride plus item styling into one struct.
- **Resolution taken:** no native change (contract forbids). Marshaled a blittable sequential
  mirror of `ImPlotSpec_c` (field-for-field, declaration order, 144 bytes on Win64 Pack-8;
  layout hand-derived from cimplot.h:856-875) and pass a `static readonly` default by value —
  zero per-frame allocation (blittable struct copy on the stack). The default mirrors C++
  `ImPlot::ImPlotSpec()`'s field defaults exactly (implot.h:515-532): the AUTO sentinels are
  load-bearing — a zeroed spec would draw a transparent 0-weight line and read every point
  through Stride 0. Stride therefore rides `IMPLOT_AUTO = -1` (implot.h:72) rather than the
  contract's literal "element size"; at consumption that resolves to `sizeof(T)`, which is
  identical for the contiguous spans the API accepts. Everything is line-cited in
  `Interop/ImPlotNative.cs`.
- **Secondary finding (pin path):** `fixed (float* p = span)` does NOT compile against the
  Unity 2019.4 mscorlib this repo builds against — its `ReadOnlySpan<T>`/`Span<T>` predate the
  C# 7.3 `GetPinnableReference` pattern (verified by probe build: CS8385; `&span[0]` fails too,
  CS0211 — no ref-returning indexer). Pinning goes through Mono's
  `DangerousGetPinnableReference()` (present in the Unity mscorlib, verified via metadata dump):
  `ref T first = ref span.DangerousGetPinnableReference(); fixed (T* p = &first)`. Still `fixed`,
  still zero-alloc, still released at scope exit; the IsEmpty guard runs before the reference is
  taken. Documented at both call sites.
- **Held back:** the optional `PlotLine(label, values, xscale, x0)` overload — omitted per
  YAGNI (adds no allocation, but nothing consumes it; C19 showcase can add it with its first
  consumer). In-game two-plot rendering + benchmark no-regression remains the user-assisted M4
  gate.
- **Lead ruling 2026-09-04:** ACCEPTED as implemented. (1) The spec-struct ABI is the real
  v1.0 surface; the blittable mirror + AUTO-sentinel default is the correct call — a zeroed
  spec would have been a silent rendering bug. The C13 contract's param-tail text was written
  from an assumed signature and stands corrected. (2) `DangerousGetPinnableReference` under
  Unity 2019.4 mscorlib is the only viable pin path and keeps the zero-alloc invariant;
  documented at the call sites. (3) YAGNI omission endorsed — C19 adds the overload if it
  needs it.
