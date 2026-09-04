# Vendor Pin Record — DearImGuiKSPNative

Every third-party source tree under `vendor/` is pinned here (spec §4.2, §8.1 asset
`DK_VendoredSrc`). Populate each row when the tree is vendored (milestone M4 vendor
setup; imgui_toggle may land earlier with M3). Build must fail loud on drift — never
patch vendored sources in place without updating this record.

imgui/cimgui themselves are **not** vendored: they come from the sibling clone
`..\..\cimgui` (pinned imgui 1.92.9) compiled directly in, per `DearImGuiKSPNative/README.md`.

| Tree | Upstream repo | Tag / commit | Pinned date | License | Notes |
|------|---------------|--------------|-------------|---------|-------|
| `implot/` | epezent/implot | v1.0 tag (exact — do not track master) | _pending_ | MIT | implot.cpp, implot_items.cpp, implot.h, implot_internal.h |
| `cimplot/` | cimgui/cimplot | _regenerated_ | _pending_ | MIT | Regenerated against the pinned cimgui clone; record generator commit here |
| `imgui-knobs/` | altschuler/imgui-knobs | _pending_ | _pending_ | MIT | Hand shim in `src/shims/` |
| `imgui-wheels/` | Engineer162/imgui-wheels | _pending_ | _pending_ | MIT | Immature repo (accepted knowingly, spec §11); hand shim in `src/shims/` |
| `imspinner/` | dalerank/imspinner | _pending_ | _pending_ | MIT | Header-only; C ABI via cimspinner |
| `cimspinner/` | dalerank/imspinner `cimspinner/` | _regenerated_ | _pending_ | MIT | Regenerated against the pinned cimgui clone; record generator commit here |
| `imgui_toggle/` | cmdwtf/imgui_toggle | 2c178f539693117ca736504c22e63ba8a0b1c4f5 (main HEAD) | 2026-09-04 | 0BSD | Scalar/flag overloads only; hand shim in `src/shims/`. Vendored: imgui_toggle.cpp/.h, imgui_toggle_palette.cpp/.h, imgui_toggle_presets.cpp/.h, imgui_toggle_renderer.cpp/.h, imgui_toggle_math.h, imgui_offset_rect.h, LICENSE (11 files, md5-verified byte-identical) |
