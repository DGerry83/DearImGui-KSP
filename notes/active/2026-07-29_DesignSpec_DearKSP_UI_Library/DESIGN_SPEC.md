# Design Specification: Dear KSP

## 1. Overview

### 1.1 Project Name

**Dear KSP** (no underscore). Assembly name and root namespace: `DearKSP`.

### 1.2 One-Sentence Pitch

A shared KSP mod library providing a modern, high-performance UI framework as a drop-in replacement for Unity IMGUI.

### 1.3 Target Platform & Runtime

- Platform: Windows-first (Linux/Mac = long-term goal).
- Runtime: Kerbal Space Program 1.12.x, Unity 2019.4.18f1 LTS, Mono (not IL2CPP), x64 only, .NET 4.x-era API surface.
- Graphics: **D3D11 only for MVP** (D20 — OpenGL deferred: a meaningful test needs a separate environment since CinematicShaders/CinematicRecorder fail under GL). OpenGL remains the planned second backend. D3D9 explicitly unsupported. DX12 assumed to work via D3D11 backwards compatibility (assumption to verify — Unity 2019.4's DX12 path is experimental).
- Version constraints: library hard-depends only on the game via `KSPAssemblyDependency("KSP", 1, 12)`.

### 1.4 Distribution

Release zip containing `GameData/DearKSP/` with:

- `Plugins/DearKSP.dll` — managed C# assembly.
- `PluginData/DearKSPNative.dll` — native C++ assembly (x64 Windows; Linux/Mac binaries join later). Native DLLs must stay out of `Plugins/`: KSP's assembly loader attempts to load every DLL in its scan path as a managed assembly and hangs on native ones (D19).
- `settings.cfg` — default settings ConfigNode.
- `LICENSE`, `README.md`, and a version file.

Install = extract into the KSP root. CKAN metadata deferred to release time. The demo/example mod ships as a **separate zip** (`GameData/DearKSPDemo/`) so users installing the library purely as a dependency receive no demo UI (D7).

---

## 2. Design Intent

### 2.1 Core Experience

- **Mod authors** get a C# ImGui-style API: they declare their UI per frame and the library handles windowing, input locking, rendering, and lifecycle. No IMGUI, no uGUI plumbing, no native code on their side.
- **Players** get snappy, animated, non-IMGUI mod UIs with no measurable framerate cost.

### 2.2 Success Criteria

1. A render-thread injection proof of concept draws the ImGui demo window in-game on D3D11 at full framerate.
2. A sample consumer mod builds a working window using only the C# API with zero IMGUI.
3. Another mod can declare `KSPAssemblyDependency` on Dear KSP and load correctly (after it, per the loader's topological sort).
4. The torture-test benchmark UI visibly outperforms the equivalent IMGUI implementation (see §4.3).

### 2.3 Inspiration & References

- Dear ImGui (ocornut) and cimgui — the rendering core and C ABI.
- The user's `IMGUI_Helper` repo (`UI/Tabs/PerformanceTab.cs`; `ReferenceNotes/IMGUI.md` §9, §12.2, §15) — the known IMGUI bog-down case used as the performance reference.
- KSP's own uGUI stack (`UIMasterController`, `PopupDialog`, `UISkinDef`) — reference for theming and input-lock behavior.

---

## 3. Scope

### 3.1 In Scope

- Native C++ core: Dear ImGui context, cimgui C ABI, D3D11 + OpenGL render backends, GPU resource management.
- Managed C# wrapper (`DearKSP.dll`): KSP plugin lifecycle, library-owned frame loop, consumer registration API, input-lock integration, theming/styling API surface, settings persistence, failure handling.
- Global library config persisted as a ConfigNode under `GameData/DearKSP/`.
- Demo/example mod as a separate install, including the torture-test benchmark UI.

### 3.2 Out of Scope

- Migrating existing mods (the user's or others') to the library.
- Editor tooling of any kind.
- Controller/touch input support.
- Linux/Mac builds (long-term goal, not MVP).

### 3.3 Minimum Viable Product (MVP)

- Native renderer injected via the Unity low-level native plugin pattern, validated on D3D11.
- Core widgets: windows, buttons, text, sliders, input fields.
- Input locking while capturing (hover locks camera/click-through; active text field locks keyboard).
- One example window in the separately-installed demo mod.
- Stock ImGui dark theme; embedded ProggyClean font.
- Global settings in `settings.cfg` (no settings UI).
- Failure detection with session-permanent self-disable, player popup, and `DearKSP.IsAvailable` consumer escape hatch.

### 3.4 Deferred / Future Work

- Animation/tweening helpers (consumers may animate manually frame-to-frame in MVP).
- ImGui add-ons: docking, node editors, plots.
- KSP-flavored theme, additional themes, styling presets, built-in settings window (theme picker, UI scale).
- Consumer-specified font loading (the font atlas/pipeline design must not preclude it), multi-font/HiDPI polish.
- Window position-persistence helper for consumers.
- Alternative z-ordering ("last interacted with" raise, player-assigned priority).
- CKAN distribution; Linux/Mac ports; DX12 validation.

---

## 4. Technical Architecture

### 4.1 Technology Stack

| Layer | Technology | Purpose |
|-------|------------|---------|
| Core (native) | C++ DLL with Dear ImGui + cimgui, custom D3D11/OpenGL backends | Owns the ImGui context, frame lifecycle, and draw-data → GPU translation. Zero KSP/Unity knowledge. |
| Application (managed) | C# in `DearKSP.dll` | Consumer registration, frame-loop orchestration, input-lock policy, lifecycle state machine, settings model. Reaches Core only through the cimgui P/Invoke boundary. |
| Infrastructure (managed) | C# in `DearKSP.dll` | KSP addon entry point, `InputLockManager` calls, ConfigNode persistence, logging, failure popup. The only layer that touches KSP/Unity APIs. |
| Binding | cimgui, compiled by us; P/Invoke from C# | Full widget coverage with a binding we own; no third-party binding dependency or prebuilt-binary conflicts (D8). |
| Settings | KSP `ConfigNode` file | Native KSP persistence format; editable by players; no custom parser needed. |

Dependency direction: Infrastructure → Application → Core. Core never calls upward except through callback abstractions defined in Application. No layer except Infrastructure references KSP/Unity APIs (D18).

### 4.2 Data Sources

- The library itself reads only: input state (mouse/keyboard, per frame), screen size, and game UI scale.
- Consumer mods push whatever game-state data they want as part of their per-frame UI declaration; the library never queries game state on their behalf.
- Player settings not in the ILSpy dump (scripting runtime version, API compatibility level, graphics API order, color space) must be read from the install's `boot.config` or at runtime during the PoC.

### 4.3 Update Cadence & Performance

- **Library-owned frame loop** (D9): consumers register per-frame callbacks; the library drives input sampling, ImGui NewFrame/Render, and the native render handoff each frame.
- No timers, no background threads owned by the library; rendering rides the game's render thread via `GL.IssuePluginEvent`.
- Performance targets: no measurable FPS impact with 1–3 typical windows; under 1 ms managed frame cost.
- Benchmark strategy (D10): the demo mod ships a **torture-test UI** replicating `IMGUI_Helper`'s `PerformanceTab.cs` (naive vs virtualized 1000-item list). The relative demonstration against IMGUI is the meaningful metric, not absolute milliseconds. Profiling via this benchmark gates any optimization work — correctness first.

### 4.4 Persistence

- The library persists **only its own global config** (theme, UI scale, font scale, logging, enable flag) in `GameData/DearKSP/settings.cfg`, a KSP `ConfigNode`. Read at startup, written on change; no file-watching in MVP. The ConfigNode carries a format version so old configs migrate forward.
- The library is stateless across saves; safe to install/update mid-save (Q8).
- Window state (position, open/closed) belongs to consumer mods. A position-persistence helper is deferred.

### 4.5 Pattern Selection

| Problem / Concern | Selected Pattern | Justification ("Use When" match) |
|---|---|---|
| Third-party mods extend the library at runtime | Plugin / Mod Architecture | Dear KSP is a host: it defines a public API + registration hook; KSP's loader is the plugin loader; consumer lifecycle = register → per-frame callback → auto-disable on repeated exceptions. |
| Library lifecycle has distinct behavioral modes | State Machine | Distinct states (§5.2) with different update/render logic and explicit transitions. |
| One persisted config entity | Repository (thin) | Single data source (`settings.cfg`) abstracted behind load/save so ConfigNode I/O stays out of frame logic; deliberately thin per YAGNI. |
| Exactly one native render context / GPU device | Singleton (composition-root-managed) | One ImGui context and one device exist; owned by the addon's composition root and passed explicitly — not a mutable global. |

Explicitly rejected: Event Bus (consumer communication is one-to-one registration, not one-to-many decoupled), Command, MVC/MVVM, Component-Based (no fit for an immediate-mode renderer).

---

## 5. Mechanics & Behavior

### 5.1 Activation & Triggers

- Entry point: `[KSPAddon(KSPAddon.Startup.MainMenu, once: true)]` MonoBehaviour; calls `DontDestroyOnLoad` in `Awake()` (the game does not do this automatically).
- One-time initialization: native DLL load, graphics device hookup, font atlas build.
- Once initialized, the library renders in **every** scene.

### 5.2 State Machine

`Uninitialized → Initializing → Running`, plus two terminal/parallel states:

- **Failed** — entered from any state on unrecoverable error (§5.4). Session-permanent: no rendering, no consumer callbacks.
- **Suspended** — entered when the game UI is hidden (F2) and during loading screens. No rendering and no consumer callbacks while suspended; returns to Running when the UI is visible again.

### 5.3 Core Algorithms

- **Frame loop** (per frame, in order): sample input → determine capture state (hover/active widget) → apply or release input locks → invoke consumer callbacks in registration order → ImGui NewFrame/Render → hand draw data to the native render thread.
- **Input locking**: locks exist **only while capturing**. Hover over a Dear KSP window locks camera/click-through controls (`CAMERACONTROLS` and click-through-relevant flags); an active text field locks `KEYBOARDINPUT`. Locks are per-consumer via `InputLockManager` with per-consumer lock IDs, and are released the moment capture ends.
- **Z-ordering**: registration order only for MVP.
- **Consumer fault isolation**: a throwing consumer callback is caught, logged, and skipped for that frame. After **5 consecutive throwing frames** that consumer is auto-disabled; other consumers are unaffected.

### 5.4 Edge Cases & Failure Modes

- **Missing/corrupt native DLL, managed/native version mismatch, unsupported graphics API, or render-hook failure** → transition to Failed: session-permanent self-disable, one stock `PopupDialog` shown at the main menu (§7), detailed technical log entry.
- Consumers can query `DearKSP.IsAvailable` before registering to fall back to their own UI.
- **Resolution change** → automatic viewport and font-atlas rebuild.
- **Pause / time warp** → UI continues to run.
- **Determinism**: no randomness anywhere in the library; behavior is fully deterministic.
- **Coexistence**: must run alongside IMGUI mods (by design) and must not conflict with Deferred (hard requirement, baseline environment), TUFX/post-processing, Scatterer, Parallax, Cinematic Shaders, or Cinematic Recorder (D5, D16).

---

## 6. User Experience & Interface

### 6.1 Access Method

No player-facing library UI in MVP. Player access is consumer-driven (each consumer mod decides how its own windows open). Library settings are edited in `settings.cfg`.

### 6.2 Layout & Positioning

Consumers own window placement entirely; the library imposes no layout, anchoring, or sizing rules. Window positions are consumer state.

### 6.3 Visual Design System

| Element | Color | Size/Shape | Font | Effects |
|---------|-------|------------|------|---------|
| All widgets | Stock ImGui dark theme (unmodified) | ImGui defaults, scaled by `uiScale` | ProggyClean (embedded in ImGui), scaled by `fontScale` | None |

KSP-flavored and additional themes are deferred. Consumers receive the full ImGui style API regardless and may restyle their own windows.

### 6.4 Animation & Timing

No animation/tweening helpers in MVP. Consumers can animate manually frame-to-frame through the immediate-mode API. Deferred helpers are on the roadmap.

### 6.5 Audio Feedback

None in the library. Consumers use game audio APIs themselves.

### 6.6 Accessibility

- Global `uiScale` and `fontScale` settings (persisted, §9).
- HiDPI/multi-font polish deferred.
- Colorblind safety is theme-dependent and therefore a consumer concern in MVP; the deferred theme work should consider colorblind-safe palettes.

---

## 7. Content & Strings

### 7.1 User-Facing String Table

The library's only player-facing strings are the startup-failure popup and the log prefix. Voice: plain-language for players — what broke, what to do; technical detail goes to the log only.

| Context | String ID | English Text | Notes |
|---------|-----------|--------------|-------|
| Failure popup title | DK_FailTitle | Dear KSP — Startup Failed | Title case; shown by stock `PopupDialog` at main menu |
| Failure body — native component | DK_FailNative | Dear KSP could not start because its native component is missing or corrupt. Mods that depend on Dear KSP will not work. See KSP.log for details. | Reinstall is the expected remedy |
| Failure body — graphics API | DK_FailGraphics | Dear KSP could not start because this graphics API is not supported. Mods that depend on Dear KSP will not work. See KSP.log for details. | Log states the detected API |
| Failure body — version mismatch | DK_FailVersion | Dear KSP could not start because its components are from different versions. Mods that depend on Dear KSP will not work. See KSP.log for details. | Usually a partial update |
| Log prefix | DK_LogPrefix | [DearKSP] | Prepended to all library log lines; technical detail lives here |

### 7.2 Notifications & Feedback Messages

| Trigger | Message |
|---------|---------|
| Unrecoverable startup failure | The appropriate §7.1 popup body, once per session |
| Consumer auto-disabled after 5 throwing frames | Log-only: consumer name, exception, and disable notice under `[DearKSP]` |

### 7.3 Voice / Tone Guidelines

Plain wording, player-first. State what broke and what the player should do in one or two sentences. No jargon, no stack traces, no blame on other mods. Technical detail (exception, versions, detected API) belongs exclusively in the log.

---

## 8. Assets

### 8.1 Asset Inventory

| Asset ID | Type | File Path | Source | Format | Status |
|----------|------|-----------|--------|--------|--------|
| DK_Font | Font | (embedded in ImGui — no file) | ImGui ProggyClean | Embedded bitmap font | Required |
| DK_ManagedDll | Assembly | `GameData/DearKSP/Plugins/DearKSP.dll` | Authored in-repo | .NET assembly (x64 Mono) | Required |
| DK_NativeDll | Assembly | `GameData/DearKSP/Plugins/DearKSPNative.dll` | Authored in-repo | Native x64 Windows DLL | Required |
| DK_Settings | Config | `GameData/DearKSP/settings.cfg` | Authored in-repo | KSP ConfigNode | Required |
| DK_Demo | Code | `GameData/DearKSPDemo/` | Authored in-repo | C# only, no assets | Required (separate install) |

ImGui's core widgets need no image assets; no textures, icons, sounds, or shaders beyond what ImGui/cimgui provides.

### 8.2 Placeholder Policy

The font may start as a placeholder (embedded ProggyClean) and be swapped before release with no API impact. Nothing else may be placeholder at release.

---

## 9. Configuration

### 9.1 User Settings

All settings live in `GameData/DearKSP/settings.cfg` (ConfigNode, format-versioned; read at startup, written on change).

| Setting ID | Type | Default | Range | Description |
|------------|------|---------|-------|-------------|
| uiScale | Float | 1.0 | 0.5–2.0 | Global UI scale multiplier |
| fontScale | Float | 1.0 | 0.5–2.0 | Font scale multiplier |
| theme | String | "dark" | "dark" only (MVP) | Theme name; additional themes deferred |
| verboseLogging | Toggle | false | true/false | Extra diagnostic logging under `[DearKSP]` |
| enabled | Toggle | true | true/false | Global kill switch; false = library stays dormant |

### 9.2 Default Presets

None in MVP. The shipped `settings.cfg` contains exactly the defaults above.

---

## 10. Compatibility & Distribution

### 10.1 Hard Dependencies

| Dependency | Version | Purpose |
|------------|---------|---------|
| KSP (game) | 1.12.x, declared as `KSPAssemblyDependency("KSP", 1, 12)` | Runtime host; the only hard dependency |

### 10.2 Soft Dependencies

None for MVP. Consumer mods hard-depend on Dear KSP via `KSPAssemblyDependencyEqualMajor("DearKSP", x, y)` (see §10.5).

### 10.3 Known Conflicts

None identified in the game dump — zero existing native-plugin render integration, so no game-side conflicts with the injection pattern. The following must be validated in testing rather than assumed: coexistence with Deferred (hard requirement), TUFX, Scatterer, Parallax, Cinematic Shaders, and Cinematic Recorder — all present in the deployed test environment (D16).

### 10.4 Environment Guidance

- Coexists with IMGUI mods by design (an ImGui overlay drawn after everything does not intersect the game's IMGUI frame).
- Load order is handled by KSP's topological sort over `KSPAssemblyDependency` declarations; consumers must declare the dependency to guarantee they load after the library.
- No in-game missing-dependency warning exists in KSP itself, so consumer READMEs should state the dependency plainly.

### 10.5 Update Path

- SemVer `major.minor.patch`, mirrored identically in the `KSPAssembly` version, assembly `FileVersion`, and the version file in `GameData/DearKSP/`. Managed and native DLLs always release together in lockstep (the version-mismatch failure mode enforces this).
- **0.x phase**: minor bumps may break the consumer API; breakage announced prominently in the changelog.
- **1.0+**: no breaking changes within a major. Minor = backward-compatible additions; patch = fixes. Deprecated APIs carry `[Obsolete]` with migration guidance for at least one full minor cycle before removal at the next major.
- **Consumers must declare `KSPAssemblyDependencyEqualMajor`**, not plain `KSPAssemblyDependency`: KSP's loader treats any higher major as satisfying a dependency, so a plain declaration would let a breaking 2.0 silently load under a consumer built for 1.x. This guidance goes in the consumer README.
- Updates are safe mid-save; old settings configs migrate forward via the ConfigNode format version.
- Per-release changelog explicitly lists API additions / deprecations / removals.
- Roadmap (non-MVP): consumer-compatibility smoke test (a reference consumer built against each release); a documented support window for old majors.

---

## 11. Testing & Acceptance Criteria

| ID | Criterion | Verification Method |
|----|-----------|---------------------|
| AC1 | Render-injection PoC draws the ImGui demo window in-game on D3D11 at full framerate | In-game PoC spike (first ProjectBootstrap milestone) |
| AC2 | ~~PoC also renders correctly on OpenGL~~ **Deferred (D20)**: OpenGL backend + validation move out of MVP pending a clean GL test environment | In-game test with `-force-glcore` (when scheduled) |
| AC3 | A sample consumer builds a working window (all MVP widgets) using only the C# API, zero IMGUI | Demo mod in-game test |
| AC4 | A separate consumer declaring `KSPAssemblyDependencyEqualMajor("DearKSP", x, y)` loads after the library and works | Two-mod install test; load order confirmed in log |
| AC5 | Torture-test UI (1000-item list) visibly outperforms the IMGUI reference implementation | Side-by-side benchmark in the demo mod vs `IMGUI_Helper` `PerformanceTab.cs` |
| AC6 | No measurable FPS impact with 1–3 typical windows; under 1 ms managed frame cost | Benchmark instrumentation |
| AC7 | Input locks engage only while capturing and release correctly (camera, click-through, keyboard) | In-game interaction test: hover, click-through, text entry, vessel control immediately after |
| AC8 | Failure modes (missing native DLL, version mismatch, unsupported graphics API, render-hook failure) each produce Failed state, one popup, detailed log, and `IsAvailable == false` | Sabotage tests per failure mode |
| AC9 | Throwing consumer is skipped, then auto-disabled after 5 consecutive throwing frames; other consumers unaffected | Fault-injection test consumer |
| AC10 | Resolution change rebuilds viewport/atlas without artifacts; F2 and loading screens suspend rendering | In-game test |
| AC11 | Full compatibility environment (Deferred, TUFX, Scatterer, Parallax, Cinematic Shaders, Cinematic Recorder, an IMGUI mod) runs without visual or functional regressions | In-game test in the deployed location |
| AC12 | Settings load/save round-trip; kill switch disables the library; old-format config migrates | Config file test |

---

## 12. Open Questions / Assumptions

- **Assumption (verify in PoC)**: DX12 works via D3D11 backwards compatibility; Unity 2019.4's DX12 path is experimental.
- **Assumption (verify in PoC)**: the standard Unity low-level native plugin pattern (`GL.IssuePluginEvent`) works in KSP — no game-side precedent exists in the dump; this is the gating technical risk.
- **Open (runtime probing)**: player settings not in the dump (scripting runtime version, API compatibility level, graphics API order, color space) — read from `boot.config` or at runtime.
- **Open (runtime probing)**: scene contents (canvas render modes, camera depths, culling masks) determine exactly where rendering is injected.
- **Deferred decisions**: CKAN metadata, Linux/Mac port plan, bundled alternate font choice (if ProggyClean is ever replaced), old-major support window, **OpenGL backend scheduling (D20 — blocked on a clean GL test environment)**.

---

## 13. Revision History

| Date | Author | Change |
|------|--------|--------|
| 2026-07-29 | Agent + User | Initial specification (Q1–Q47 answered across 7 phases; decisions D1–D18) |
