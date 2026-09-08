# Chunk Contract: C15 — Docs housekeeping A (root docs + docs/00 + docs/10)
## Plan: Opus Review Remediation (`notes\plans\opus-review-remediation-contracts.md`)
## Date: 2026-09-07
## Chunk ID: C15
## Advances Milestone: Post-1.0.0 remediation wave (G5-M1, G5-M2, O1, O7, O10, O11, O12–O15, O16–O21, M28, M29, T13 — 19 items)

### Scope
Documentation-only contract; no code touched. Every rewritten claim was cross-checked against the code first (verification notes per item below).

- **CHANGELOG.md** (G5-M1, O7): the 1.0.0 entry advertised "ID-scope helpers" and "trees" that do not exist in the public API (PushId/TreeNode appear only in internal `Interop/`), and credited the in-game panel with a window-clamping control it does not have (`LibraryControlPanel.cs` exposes only theme, UI scale, font, font scale, verbose logging). Fixed in place, keeping the entry's spirit: window helpers + the real `ImGuiEx` scope set, collapsing headers (the real `DearImGuiKSP.Header.cs` widget) instead of "trees", and the panel described as font/theme/scale only.
- **README.md** (O1, O10, O11):
  - O1: build recipe — `cd DearImGuiKSPNative` persisted into the next line, so `DearImGui-KSP.slnx` resolved wrong; added `cd ..`.
  - O10 (see Discrepancies): perf claim re-scoped — the measured figure is *managed-side* frame cost (0.12–0.17 ms with 1–3 windows per FINAL_AUDIT AC6; 0.24–0.33 ms with the full busy demo in the wave verboseLogging run), not GPU render cost. Now reads "renders on the GPU; its measured managed-side frame cost stays a fraction of a millisecond even with busy dashboards open."
  - O11 (sweep O10): credits — cimplot's copyright holder added (MIT, Copyright (c) 2020 Victor Bombí; verified in `vendor/cimplot/LICENSE`).
  - O11 (sweep): blanket per-tree licence claim fixed — every vendored tree except the generated `cimspinner` bindings has an upstream LICENSE file (verified by listing all six vendor trees); cimspinner carries the imspinner MIT notice in its file headers (`cimspinner.h` head) instead.
- **docs/00-getting-started.md** (O12–O15):
  - O12: install tree rewritten to match the post-C11 zip (C11's verified 17-file listing): Medium TTF removed (C10), `settings.cfg` removed from the tree with an explicit "not shipped — created on first settings change, upgrades never reset settings" note, `Textures/toolbar.png` and `Docs/` (00–70 + CHANGELOG.md) added.
  - O13: inverted loader rule corrected — KSP's assembly scan covers all of `GameData/` recursively, skipping only folders named `PluginData` (KSP Knowledge Library `UrlDir.cs:2947`; knowledge note `assembly-loading-and-dependencies.md`); `Plugins/` is convention, not a scan rule.
  - O14: stale "generated placeholder until the icon ships" wording — `GameData/DearImGuiKSP/Textures/toolbar.png` exists and ships in the zip; the placeholder is now described as the missing-file fallback (matches `LibraryPanelToolbar.cs:79-98`).
  - O15: `KSPAssemblyDependencyEqualMajor` semantics — equal major required; the declared minor is a **minimum** (equal major requires `minor >= required`, KSP Knowledge Library `assembly-loading-and-dependencies.md:12` / `AssemblyLoader.cs:1178-1254`), not a pin.
- **docs/10-api-fundamentals.md** (G5-M2, O16–O21):
  - O16: registration order is only the *initial* stacking; afterwards z-order follows focus (clicking title bar/body/widget raises the window) — per ISSUES #009's C27 investigation (imgui FocusWindow → BringWindowToDisplayFront paths) and the frame loop (callback order never reorders `g.Windows`).
  - O17: throw list corrected against the facade — `Register` throws `ArgumentNullException` for a null **or empty** id and a null callback (`DearImGuiKSP.cs:103-109`); `Unregister(null)` throws once the library is available (registry `Dictionary.TryGetValue` rejects a null key, `ConsumerRegistry.cs:74`). Added the post-C07/C08 guard family: out-of-range style enums/spinner type/non-positive subplot dims are logged no-ops (`IsValidStyleColor`/`IsValidStyleVar`, `ImGuiPlot.BeginSubplots`, `DearImGuiKSP.Spinner.cs`); empty widget labels get the silent invisible-ID sentinel (`ImGuiInternal.ToIdUtf8`, C08/G3-20).
  - O18: end-of-frame assert claim — true only for debug native builds; the shipped release DLL is compiled `/DNDEBUG` (`build_release.bat:15`), and post-C01 the fault barrier force-closes scopes a throwing callback left open. Reworded; also fixed the same false mechanism one paragraph later in §5 ("passing the wrong StyleVar type triggers a native assert") — flagged as a small adjacent fix beyond the listed lines, same verified mechanism.
  - O19: "only dispose what a factory returned" now names both halves: default `TabBarScope`/`TabItemScope`/plot scopes are inert, but default `WindowScope`/`ScrollRegionScope` end a window/region never begun and default `StyleColorScope`/`StyleVarScope` pop an entry never pushed (verified in `ImGuiEx.cs` scope Dispose bodies and `ImGuiPlot.cs`).
  - O20: `PushStyleColor` Color-vs-Color32 "linear vs sRGB" labels removed — no color-space conversion exists: the `Color` overload passes floats through unchanged, the `Color32` overload only normalizes bytes /255 (`DearImGuiKSP.cs:608-618`). The overloads differ in component encoding only.
  - O21: settings table — `enabled=false` is file-only: dormant session, the settings panel never appears, no in-game re-enable; edit back to `true` and restart (`DearImGuiKSPAddon.cs:29-33`, panel has no Enabled control).
  - G5-M2: settings.cfg liveness rewritten per C06 — read once at startup (`SettingsModel` ctor → `ISettingsStore.Load`, the only Load call site), not watched at runtime; panel edits apply in memory immediately and are written back once, debounced ~0.5 s after changes settle, as an atomic temp+replace write; mid-session hand edits take effect only at next start and only if no panel change rewrites the file from the in-memory settings first.
- **DearImGuiKSP/Application/README.md** (M28): rewritten against the shipped tree — layering text updated to D24 reality (Unity-free except `UnityEngine.CoreModule` math structs in public signatures; never KSP APIs or engine lifecycle/scene APIs; widgets reach native code via `../Interop`, other seams via `Interfaces/`); component inventory now covers `Api/`, `Animation/`, `Theming/`, `OpenScopeTracker`, `LibrarySettings`, `InputCaptureState`, `FailureKind/FailureText` and all eight interfaces; the stale inverted frame order replaced with the real `FrameLoopOrchestrator.RunFrame` sequence (theme apply → debounced persist → tween tick → viewport clamp → capture sample → BeginUiFrame → callbacks via FaultBarrier → EndUiFrame in `finally`).
- **AGENTS.md** (M29, surgical): three line edits only — the Application-layer bullet now states the D24/Interop reality; the machine-specific `~\source\repos\cimgui` path generalized to "a sibling clone of cimgui checked out next to this repo's root (build scripts reference `..\..\cimgui`)"; the nonexistent `notes\archive\` folder dropped from the taxonomy list (actual folders: active, finished, knowledge, indices, plans).
- **tests/Application.Tests/README.md** (T13): rewritten against the current suite — 178 tests, full coverage set enumerated (orchestration core, frame boundary, ABI guards, widget guards, animation, theming, helper/ABI-pin tests); the false "test host never loads KSP/Unity assemblies" claim corrected: the csproj references `UnityEngine.CoreModule.dll` from the pinned game (`Private=true`, loaded by the host) for the real `Color32`/`Vector2` structs; no KSP assemblies are referenced or loaded. The props.user import purpose also corrected (resolves `$(KSPBT_GameRoot)` for the Unity hint path — the test project itself has no KSPBuildTools, per the csproj comment).

### Inputs
- Triage sweep Group 5 rows (all VALID WORK), `notes\active\2026-09-07_Bug_OpusReviewTriage\TRIAGE_SWEEP.md`.
- Contract records C01/C04/C06/C07/C08/C09/C10/C11 in the same folder (the behavior the docs now describe).
- KSP Knowledge Library: `NOTES\assembly-loading-and-dependencies.md`, `KSPSOURCE\UrlDir.cs:2947`, `KSPSOURCE\AssemblyLoader.cs:1178-1254`.
- ISSUES #009 (window z-order investigation).

### Outputs
- Changed: `README.md`, `CHANGELOG.md`, `AGENTS.md`, `docs/00-getting-started.md`, `docs/10-api-fundamentals.md`, `DearImGuiKSP/Application/README.md`, `tests/Application.Tests/README.md`.
- New: this contract record.
- C16-owned files untouched (docs/20,30,50,60,70; native/Infrastructure/Interop/Infrastructure.Tests READMEs).

### Constraints
- Documentation only; no code edits. No new claims beyond the listed items plus the one flagged adjacent fix (§5 StyleVar assert sentence, same /DNDEBUG mechanism as O18).
- §5.9: N/A (no runtime code).

### Verification
- Claim-by-claim cross-check against code (per-item citations above): facade guards, `ImGuiEx` scope bodies, `LibraryControlPanel` control set, `SettingsModel`/`SettingsStore` persist path, `FrameLoopOrchestrator.RunFrame` order, vendor tree license files, `package_release.bat` zip contents (via C11's verified listing), KSP loader semantics (knowledge library).
- `dotnet test tests/Application.Tests/Application.Tests.csproj --no-build`: **178/178 green** (count used in the rewritten test README). Full-solution `dotnet test` could not run: KSP_x64.exe was running (user in-game) and holds `GameData\...\DearImGuiKSPNative.dll`, failing the csproj's CopyToGame deploy step — environment contention, not a code failure; the pre-existing build output tested green.
- Markdown sanity: all edited regions re-read after edit; tables and code fences intact; no links added or removed.

### Discrepancies / flags
- **O9/O10 numbering mismatch between the dispatch brief and the sweep.** The sweep (authoritative) assigns: O9 = README "fraction of a millisecond" (PARTIAL NOTE — wave run did include the full busy demo), O10 = cimplot copyright omission, O11 = cimspinner LICENSE. The brief labeled the perf claim as O10 and merged credits+licence into O11. All three claims were fixed; recorded here so the sweep's accounting stays consistent (O9 closed as "doc improved beyond its NOTE verdict").
- **Adjacent fix beyond listed lines:** docs/10 §5's "wrong StyleVar type triggers a native assert" — same false /DNDEBUG mechanism as O18, fixed in passing. Flagged per the no-new-claims rule.
- **docs/10 §6 guard promise is now literally true** — C14 closed C08's recorded G3-20 gap (`InteropLabelRoutingTests`: Toggle/Knob/Wheel and ImPlot titles route through `ToIdUtf8`), so "every library widget guards this" no longer needs a caveat.
- **For Gate D release notes:** nothing doc-side blocks release; if `package_release.bat` staging changes again before Gate D, re-check the docs/00 install tree against the zip. The CHANGELOG 1.0.0 entry was corrected in place (historical record kept, false claims removed) — the remediation wave's own changelog entry remains Gate D's per D36.

### Rollback
- `git checkout -- README.md CHANGELOG.md AGENTS.md docs/00-getting-started.md docs/10-api-fundamentals.md DearImGuiKSP/Application/README.md tests/Application.Tests/README.md`; delete this file.
