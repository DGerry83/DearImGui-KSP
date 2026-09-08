# Chunk Contract: C16 — Docs housekeeping B (docs/20–70 + layer READMEs)
## Plan: Opus Review Remediation (`notes\plans\opus-review-remediation-contracts.md`)
## Date: 2026-09-07
## Chunk ID: C16
## Advances Milestone: Post-1.0.0 remediation wave (G5-M3, M13, O2, O22, O23, O24, O25, O26, G3-28, I28, N17, I41, A26, T27, RowHeight doc)

### Scope
Documentation correctness only — every rewritten claim cross-checked against the post-wave code. Two code-comment fixes explicitly in scope (FontResolver, native Medium mentions); no other code edits. Ran parallel to C15 (disjoint files).

- **G5-M3** (`docs/70-troubleshooting.md`): "The 16-bit index limit" section was false — the shipped stock `imgui_impl_dx11` sets `RendererHasVtxOffset` (sibling cimgui clone, `imgui_impl_dx11.cpp:639`) and nothing native-side clears it, so there is no 65,535-vertex ceiling per draw list. Section rewritten as "Very large draw lists": 16-bit indices retained per decision D32, VtxOffset chunks oversized lists, density is a per-frame perf concern not a limit; remedy bullets kept with the limit references removed.
- **M13** (docs/70 auto-disable section): replaced "Fix the exception, restart the game" — verified in `ConsumerRegistry` that `Unregister` + `Register` creates a fresh registration (`Enabled = true`, zero failure count), so in-session recovery works; docs now say so, with the burn-through caveat.
- **O25** (docs/50 + docs/70): docs/50's "Next" linked to a nonexistent troubleshooting entry. Created a "Tween warnings" section in docs/70 covering the three real log lines (`Tween.To(...) ignored: ... not available`, `... baseline setter threw; the tween was not started`, `Tween setter threw; the tween has been stopped` — verbatim from `Tween.cs`/`TweenEngine.cs`) and pointed docs/50 at its anchor. docs/50's throw-behavior bullets corrected per C02: baseline `set(from)` throw is now contained (inert handle, no propagation), mid-flight setter throw stops only that tween; null setter remains the only `Tween.To` throw.
- **O2** (`docs/20-widgets.md` Spinner sizing): removed "multiply by the configured scale yourself" — C13's public-surface survey confirmed no scale accessor exists. Guidance now: measure a style-driven size live or pick a size that reads across 0.5–2.0.
- **O22** (docs/20 Spinner intro): the 15-type subset was blamed on upstream compile-time config; the vendored `cimspinner_config.h` actually enables 26 groups (PIN_RECORD.md concurs). Corrected: the subset is the managed binding layer's curation.
- **RowHeight doc** (docs/20 scroll-region example): replaced the hard-coded `const float RowHeight = 22f` with the live-measure pattern the demo now uses post-C13 (`GetCursorScreenPos` before/after the first declared row; seed value replaced on first measurement; one frame of lag after a scale change). Explained why: no public style metric exists.
- **O23** (`docs/30-theming.md` §6): the "under dark, gradient drawing still uses the KSP constants (window background and gradient buttons)" claim contradicted §1 and `ThemeEngine.Apply` (`gradient = preset.Name != "dark"` → `SetWindowBgGradient(0,...)`). Fixed: under `dark` the native window-bg gradient pass is disabled; `GradientButton` still renders its gradient because it is an explicit consumer drawing path.
- **G3-28** (docs/30 §6 InputText bullet): documented the label-side difference — `ksp` draws the label separately off-white to the LEFT of the field, `dark` keeps the stock layout (label RIGHT). Verified in `DearImGuiKSP.cs:261-276`. Also noted `##` ID suffixes never render as visible text in either theme (ksp strips via `StripIdSuffix`, C08/G2-09; dark is stock `FindRenderedTextEnd` behavior).
- **O24 + I28** (docs/30 §7): the settings bullet now covers the live in-game panel ("DearImGui-KSP Settings" toolbar window, verified in `LibraryControlPanel.cs`), the live-vs-restart asymmetry (theme/uiScale live at next frame start via the ThemeEngine dirty path; font/fontScale startup-only — atlas baked on first frame), and that uiScale × fontScale **multiply** for text size (verified: `FontResolver.BaseSizePixels(18) * fontScale` for the atlas load; `ContextHost_SetUiScale` sets `io.FontGlobalScale`, `ContextHost.cpp:722`).
- **I28 comment half** (`DearImGuiKSP/Infrastructure/FontResolver.cs`): one line added to the `BaseSizePixels` doc noting the multiply relationship. (File lives in `Infrastructure/`, not `Application/` as the plan said — C10's record already flagged this.)
- **O26** (`docs/60-migration-from-imgui.md`): the mapping table's single-qualified `DearImGuiKSP.Text(...)` form never compiles (namespace shadows the same-named facade type). Fixed to `DearImGuiKSP.DearImGuiKSP.*` throughout the table (incl. the `Register` row and the `ImGuiEx.Window` row), plus the same broken form in the Layout section (`SetCursorY`/`Dummy`) and the Styling section (`TextColored`).
- **N17** (`DearImGuiKSPNative/README.md`): removed the nonexistent `opengl32` link claim (build scripts link only `d3d11.lib dxgi.lib`; added the D20/D35 deferred-GL note); documented `check_build_env.bat` (vcvars fallback + cimgui/imgui pin enforcement, C05) and release PDB emission (C05/G2-03); extended the install-layout note with `Textures/` and `Docs/` (verified against `package_release.bat` staging); handshake version now stated as 7 (C04). ENVIRONMENT.md was NOT stale (sweep verdict PARTIAL) — untouched.
- **Native Medium comments** (C10 leftovers, flagged in CHUNK_C10): `ContextHost.cpp` FontDefault comment, `ContextHost.h` and `DearImGuiKSPNative.cpp` "once or twice (Regular + Medium)" comments corrected to the single-load reality.
- **I41** (`DearImGuiKSP/Infrastructure/README.md`): rewritten against the actual folder — NativeBridge no longer described as owning all P/Invoke declarations (Interop's implicit DllImports resolve by module name once loaded); added the five omitted components: `FontResolver`, `LibraryControlPanel`, `LibraryPanelToolbar`, `PointerBlockerGateway`, `ImguiEventEaterGateway`.
- **A26** (new `DearImGuiKSP/Interop/README.md`): documents the layer's six files and the caller-direction rule (leaf layer; Application and Infrastructure call in, Interop references neither — verified: no `using UnityEngine` / `using DearImGuiKSP.*` in the folder), matching the sibling layer READMEs' structure.
- **T27** (`tests/Infrastructure.Tests/README.md`): deferral list expanded to the current gateway/component set (PointerBlockerGateway, ImguiEventEaterGateway, NativeBridge, GameEventHooks, FailureNotifier, DearImGuiKSPLogger, FontResolver, LibraryControlPanel, LibraryPanelToolbar, Composition, DearImGuiKSPAddon); notes that `AdoptTopSortOrder` is extractable pure logic whose extraction was deferred by C14.
- **Optional add-on**: `notes\knowledge\latent-findings-recording.md` already existed (record format + examples) — appended a "Gate A outcomes" section (G2-U1 unreachable, G2-U2 disproven, pointers to TRIAGE_SWEEP.md) instead of creating a duplicate register.

### Inputs
- Triage sweep Group 5 rows + M13 (`notes\active\2026-09-07_Bug_OpusReviewTriage\TRIAGE_SWEEP.md`).
- Sibling contract records C02/C04/C05/C07/C08/C09/C10/C13/C14 for post-fix behavior.

### Outputs
- Changed: `docs/20-widgets.md`, `docs/30-theming.md`, `docs/50-animation.md`, `docs/60-migration-from-imgui.md`, `docs/70-troubleshooting.md`, `DearImGuiKSPNative/README.md`, `DearImGuiKSP/Infrastructure/README.md`, `tests/Infrastructure.Tests/README.md`, `DearImGuiKSP/Infrastructure/FontResolver.cs` (one comment line), `DearImGuiKSPNative/src/ContextHost.cpp` (comment), `DearImGuiKSPNative/src/ContextHost.h` (comment), `DearImGuiKSPNative/src/DearImGuiKSPNative.cpp` (comment), `notes/knowledge/latent-findings-recording.md` (Gate A section).
- New: `DearImGuiKSP/Interop/README.md`, this contract record.
- Beyond-brief fix, flagged: docs/70's handshake section still said "expected version 6"; updated to 7 (C04 bumped it; `NativeBridge.cs:41` + `DearImGuiKSPNative.cpp:31` verified). This is the same class of stale-claim correction as N17.

### Constraints
- Docs + the two flagged code-comment families only; no behavior change. C15's files (root README/CHANGELOG/AGENTS, docs/00, docs/10, Application README, Application.Tests README) untouched.
- §5.9: N/A (documentation; comment-only code edits).

### Verification
- `dotnet build DearImGui-KSP.slnx`: 0 errors, 0 warnings (after both code-comment edits).
- Every rewritten claim cross-checked against code: VtxOffset (`imgui_impl_dx11.cpp:639`), in-session recovery (`ConsumerRegistry.cs`), tween log lines verbatim (`Tween.cs:49,60,95,104`, `TweenEngine.cs:134`), spinner config (26 unique `SPINNER_*` defines in `vendor/cimspinner/cimspinner_config.h`), gradient gating (`ThemeEngine.cs:123`), label-side paths (`DearImGuiKSP.cs:267-276`), panel live/restart semantics (`LibraryControlPanel.cs:44,93,109,153`), scale multiply (`FontResolver.cs:56`, `ContextHost.cpp:722`), native link libs and pin enforcement (`build.bat`, `build_release.bat`, `check_build_env.bat`), install tree (`package_release.bat:71-75`), handshake 7, Infrastructure folder contents, Interop leaf-layer rule (grep: no upward references).
- Docs-internal links touched: docs/50 → `70-troubleshooting.md#tween-warnings-ignored-and-setter-threw` (anchor matches the new header); docs/20 → `#scroll-regions` (header exists).
- In-game: none (docs-only); behavior claims describe already-gated code.

### Rollback
- Revert the listed changed files; delete `DearImGuiKSP/Interop/README.md` and this record.
