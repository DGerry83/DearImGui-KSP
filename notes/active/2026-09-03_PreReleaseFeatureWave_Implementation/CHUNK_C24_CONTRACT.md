# Chunk Contract: XML-Doc Sweep + License Attribution
## Plan: DearImGui-KSP Pre-Release Feature Wave
## Date: 2026-09-04
## Chunk ID: C24
## Advances Milestone: M7 (documentation — final chunk; M7 gate follows)

### Scope
The M7 closer (plan M7 row, spec §8.2): sweep XML docs on every public member the
wave added, aggregate third-party license attributions, and harmonize the docs set
cross-links. Sequenced after C22/C23 (committed `95f6da8`) so the sweep sees final
API shapes and final docs.

1. **XML-doc sweep** — every NEW public member from this wave must carry complete
   `///` docs (summary, params, returns, remarks where behavior is non-obvious).
   Sweep: `Application/DearImGuiKSP.cs`, `Application/Api/*.cs` (ImGuiEx,
   ImGuiPlot, ImGuiDraw, ImGuiGradients, ImGuiStyleEnums, the widget partials),
   `Application/Animation/*.cs`, `Application/Theming/KspPalette.cs` (public
   constants). Most were documented in-chunk — this is a gap-filling pass, not a
   rewrite. Fix only: missing docs, docs that drifted from behavior (compare
   against the C22/C23 findings: IsPlaying is a property, window positions do not
   persist, etc.), and stale class-level docs that enumerate members. Do NOT
   reword accurate docs.

2. **License aggregation** — the wave vendored: imgui_toggle (0BSD), ImPlot +
   cimplot (MIT), imgui-knobs (MIT), imgui-wheels (MIT), imspinner + cimspinner
   (MIT), IBM Plex Sans (OFL). Ensure the repo's top-level LICENSE/attribution
   story (check `README.md` and any existing LICENSE file) aggregates all of them
   with upstream copyright lines — read each `vendor/*/LICENSE*` file for the
   exact copyright holder/year. OFL obligations for the bundled font are already
   met by `GameData/DearImGuiKSP/Fonts/OFL.txt` — confirm and reference it.
   Update README.md's credits/dependencies section if present (or add one,
   matching the existing README tone).

3. **Docs harmonization** — the two doc sets were written in parallel: verify
   cross-links between all 8 files resolve (filenames, anchors), titles/tones are
   consistent, and set A's forward links match set B's actual headings. Fix links
   only; do not rewrite content.

### Inputs (must exist before starting)
- C22/C23 committed: `docs/*.md` final content.
- `vendor/PIN_RECORD.md` (the pin/attribution record), `vendor/*/LICENSE*`.
- The wave's public API files (list above).

### Outputs (must be created/changed)
- XML-doc gap fixes in the listed source files (minimal diffs).
- README.md (or LICENSE) attribution aggregation.
- `docs/*.md` link harmonization only.
- NO behavior changes, NO signature changes.

### Constraints
- XML docs must be compile-safe (no broken `<see cref>` — build with warnings
  visible: 0 new warnings).
- D31: no emojis/symbol glyphs in added docs.
- Attribution must name each upstream project, its license, and copyright holder.

### Verification
- `dotnet build DearImGui-KSP.slnx` 0/0 (0 NEW warnings); `dotnet test` 90/90.
- Docs link check: every relative .md link in docs/ resolves to an existing file
  (state the check method).
- **M7 gate (user-assisted, NOT yours)**: modder-from-zero dry run against the
  docs alone — the user follows docs/ from zero to a working themed window with a
  plot. Report readiness and hand off.

### Rollback
- Revert the touched files; rebuild to confirm 90/90.
