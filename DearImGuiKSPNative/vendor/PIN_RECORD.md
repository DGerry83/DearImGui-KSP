# Vendor Pin Record — DearImGuiKSPNative

Every third-party source tree under `vendor/` is pinned here (spec §4.2, §8.1 asset
`DK_VendoredSrc`). Populate each row when the tree is vendored (milestone M4 vendor
setup; imgui_toggle may land earlier with M3). Build must fail loud on drift — never
patch vendored sources in place without updating this record.

imgui/cimgui themselves are **not** vendored: they come from the sibling clone
`..\..\cimgui` (pinned imgui 1.92.9) compiled directly in, per `DearImGuiKSPNative/README.md`.

| Tree | Upstream repo | Tag / commit | Pinned date | License | Notes |
|------|---------------|--------------|-------------|---------|-------|
| `implot/` | epezent/implot | v1.0 tag = 524f9fcd48d76c13fdf94c5ffbba8787a1ff7e39 | 2026-09-04 | MIT | implot.cpp, implot_items.cpp, implot.h, implot_internal.h + LICENSE (5 files, md5-verified byte-identical). PLUS implot_demo.cpp (md5 8fee560d37ba5a76896e803e51de748a, byte-identical) — REQUIRED: generated cimplot.cpp wraps `ImPlot::ShowDemoWindow`, which is only defined in implot_demo.cpp (upstream cimplot's own CMakeLists compiles it too). Contract's 4-file list is insufficient to link; deviation flagged to Lead in C11 report/I-06 |
| `cimplot/` | cimgui/cimplot | generator @ 11f13e6cd0f80e83d6409e78592b392967fa7954 | 2026-09-04 | MIT | Regenerated 2026-09-04 against pinned cimgui clone @ b705b24 (imgui 1.92.9) + implot v1.0 sources; invocation `luajit generator.lua gcc "internal"` (canonical gcc path per generator.sh; cl preprocessing breaks cpp2ffi struct tracking — see I-06). Generated: cimplot.cpp/.h (790 CIMGUI_API decls, incl. implot_internal API) vendored byte-identical from generator output |
| `imgui-knobs/` | altschuler/imgui-knobs | 6b4b5923129c1e8fa4cd30f8c59439643d44f22f (master HEAD) | 2026-09-04 | MIT | Float + int scalar overloads only; hand shim in `src/shims/`. Vendored: imgui-knobs.cpp/.h, LICENSE (3 files, md5-verified byte-identical) |
| `imgui-wheels/` | Engineer162/imgui-wheels | 1347dabe89cf08678846b0025a885e0306d1d80c (master HEAD) | 2026-09-04 | MIT | Immature repo (accepted knowingly, spec §11) but compiles clean against imgui 1.92.9 unpatched; float + int scalar entry points only; hand shim in `src/shims/`. Vendored: imgui-wheels.cpp/.h, LICENSE (3 files, md5-verified byte-identical). Upstream `external/SDL` submodule (example-only) intentionally not vendored |
| `imspinner/` | dalerank/imspinner | _pending_ | _pending_ | MIT | Header-only; C ABI via cimspinner |
| `cimspinner/` | dalerank/imspinner `cimspinner/` | _regenerated_ | _pending_ | MIT | Regenerated against the pinned cimgui clone; record generator commit here |
| `imgui_toggle/` | cmdwtf/imgui_toggle | 2c178f539693117ca736504c22e63ba8a0b1c4f5 (main HEAD) | 2026-09-04 | 0BSD | Scalar/flag overloads only; hand shim in `src/shims/`. Vendored: imgui_toggle.cpp/.h, imgui_toggle_palette.cpp/.h, imgui_toggle_presets.cpp/.h, imgui_toggle_renderer.cpp/.h, imgui_toggle_math.h, imgui_offset_rect.h, LICENSE (11 files, md5-verified byte-identical) |
