# Question Log — Pre-Release Feature Wave Design Spec

Session: `notes/active/2026-09-03_DesignSpec_Theming_Extensions_Showcase/`

## Already answered by the user's briefing (2026-09-03) — recorded, not re-asked

- **Identity/intent**: pre-release feature wave for DearImGui-KSP. Ship default theme approximating the vanilla KSP UI look; integrate the ImGui extensions that earn their place; build a telemetry showcase mod; produce modder-facing API docs. Release happens only after all of this.
- **Theme specifics**: gradients on widgets; rounded corners/round-corner buttons; circular radio buttons; glowing-radio activation effect DEFERRED (sugar); close-to-stock, not pixel-exact; palette per `VisualReferenceMaterial/KSP_Theme_Palette_Notes.md` + `UIStylingRef.png`.
- **Font**: IBM Plex Sans default (local: `C:\Users\Matt\source\repos\Fonts\IBM_Plex_Sans`, OFL), Noto Sans as fallback/optional.
- **Extensions in scope**: ImPlot (2D plotting), wheels, knobs, spinners. Gradients needed for theme. Animations + 3D plotting = stretch, evaluated after core wave.
- **Anti-over-spec directive**: every feature needs a real use case today, not an imagined one someday. Pushback on items that should wait for user requests is invited.
- **API ergonomics**: idiomatic C# — IDisposable/using wrappers for Begin/End pairs, strong types (Vector2/Color) instead of ImVec2/ImVec4, XML docs on everything public, no emojis/symbols in docs.
- **Showcase content**: (1) 2x2 rolling graphs — altitude, dynamic pressure q, throttle, G-force — legends + hover tooltips; (2) stage/delta-v stacked bars + color-transitioning propellant meters + Isp/burn-time tooltips; (3) 2D orbital radar via ImDrawList (ellipse, live vessel position, Ap/Pe/target) + orbital elements readout.

## Phase 1/2 question batch (asked 2026-09-03, pending answers)

Q1. Theme packaging: does "ksp" become the new DEFAULT theme (with "dark" retained as an option), or is it opt-in? (Recommendation: ksp = default; the `theme` setting already exists in settings.cfg.)
Q2. Unity types in the public API: Application/ is Unity-free today (D18). Options: (a) allow a UnityEngine.CoreModule reference in Application for `Vector2`/`Color32` (both are managed blittable structs; unit tests still run; amends the Unity-free rule), (b) library-owned blittable `Vec2`/`Color` structs in Application with conversion operators living in a tiny Unity-touching helper, (c) keep primitives. (Recommendation: (a) — simplest for consumers, tests unaffected, one-line D18 amendment.)
Q3. "Complex dockable layouts": does the showcase need actual ImGui **docking branch** features (drag-to-dock window panes — requires switching our imgui pin to the docking branch, a significant native change), or is "dockable" satisfied by sophisticated multi-panel layouts (tab bars, child regions, columns)? (Recommendation: multi-panel layouts; defer the docking branch until a consumer asks.)
Q4. imgui_toggle (animated toggle switches, 0BSD, LOW cost): in scope for this wave, or defer? It could partially cover the deferred "glowing radio" sugar and suits settings UIs. (Recommendation: include — cheap, real use case in the showcase's own settings.)
Q5. Telemetry mod packaging: new third mod (e.g. DearImGuiKSPTelemetry, own zip) or another window set inside the existing DearImGuiKSPDemo install? (Recommendation: inside the demo install — one showcase package, no third distribution unit.)
Q6. Delta-v depth: precise stock-grade stage simulation (FuelFlowSimulation-class — a large subsystem) vs simplified per-stage Δv approximation (sum engines/propellant per stage, clearly labeled "approximate")? (Recommendation: approximate — this is a showcase, not a Kerbal Engineer replacement.)
Q7. Maneuver node scope: display/existing-node rendering only, or node creation/editing too? (Recommendation: display-only for this wave.)
Q8. IBM Plex Sans bundling: which weights ship (Regular only / Regular+Medium / Regular+Bold)? Variable-font TTF or static instances (ImGui's stb_truetype loads static TTFs; variable fonts are not supported without FreeType)? Ship Noto Sans fallback in the package or leave it user-installable? (Recommendation: static Regular + Medium, bundled with OFL.txt; Noto Sans not shipped, documented as a drop-in alternative.)
Q9. Docs format: Markdown files in-repo under `docs/` (getting started, API walkthrough, theming guide, IMGUI-migration guide) shipped in the release zip — or a generated reference site (DocFX etc.)? (Recommendation: Markdown in-repo; XML docs already cover IntelliSense; no site tooling to maintain.)
Q10. implot3d verdict (research-backed pushback): pre-1.0 with self-declared breaking changes, requires 32-bit indices we don't have, and nothing in the showcase needs 3D. Defer post-release until a consumer asks? (Recommendation: defer.)
Q11. Animation verdict (research-backed): skip the HImGuiAnimation binding (abandoned, buggy, pointer-centric) and instead add a small C# easing/tween helper API in Application/ (Unity-free, unit-testable)? (Recommendation: yes, include — small, real use cases: color transitions, spinner phases, panel fades.)

## Answers

(Recorded 2026-09-03, verbatim from user: "1) yes 2) a 3) multi-panel, defer docking 4) include 5) inside the demo, one showcase package 6) approximate, grab data exposed by stock where possible 7) display-only 8) yes 9) yes 10) defer 11) yes - Overall good call on HImGuiAnimation and implot3d.")

- **Q1**: Yes — "ksp" is the new default theme; "dark" retained as an option.
- **Q2**: (a) — Application may reference UnityEngine.CoreModule for `Vector2`/`Color32`; D18 amended accordingly.
- **Q3**: Multi-panel layouts only; docking branch deferred until a consumer asks.
- **Q4**: Include imgui_toggle in this wave.
- **Q5**: Telemetry showcase lives inside the existing DearImGuiKSPDemo install; one showcase package.
- **Q6**: Approximate per-stage Δv; use stock-exposed data where possible; labeled as approximate.
- **Q7**: Maneuver nodes display-only.
- **Q8**: Yes — static IBM Plex Sans Regular + Medium bundled with OFL.txt; Noto Sans documented, not shipped.
- **Q9**: Yes — Markdown docs in-repo under `docs/`, shipped in the release zip; no site tooling.
- **Q10**: Defer implot3d post-release (user endorsed the pushback).
- **Q11**: Yes — C# easing/tween helper API in Application/; no HImGuiAnimation binding (user endorsed).
