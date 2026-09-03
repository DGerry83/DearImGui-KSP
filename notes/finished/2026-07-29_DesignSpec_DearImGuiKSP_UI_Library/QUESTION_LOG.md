# Question Log — DearImGui-KSP UI Library

Questions asked during DesignSpec refinement and the user's answers.

## Phase 1 — Intent & Scope (answered 2026-07-29)

- **Q1. Identity** — Name is **"DearImGui-KSP"** (no underscore). Pitch confirmed: *"A shared KSP mod library providing a modern, high-performance UI framework as a drop-in replacement for Unity IMGUI."* Audience = mod authors (self first, others welcome). Repo: local-only for now; when a remote is created it will be **private until release**, but pushing before release is fine.
- **Q2. Core intent** — Confirmed: mod authors get a C# ImGui-style API with windowing/input-locking/rendering handled for them; players get snappy, animated, non-IMGUI UIs.
- **Q3. Success criteria** — Confirmed as proposed: (a) render-thread injection PoC draws the ImGui demo window in-game on D3D11 at full framerate; (b) a sample consumer mod builds a working window using only the C# API with zero IMGUI; (c) another mod can declare `KSPAssemblyDependency` on it and load correctly.
- **Q4. Scope boundaries** — Confirmed: in scope = native core + C# wrapper + input-lock integration + theming/styling API + demo/example mod (**as a separate install** so users don't get demo UI from installing the dependency). Out of scope = migrating existing mods, editor tooling, controller/touch input.
- **Q5. MVP vs deferred** — Confirmed: MVP = native renderer + core widgets (windows, buttons, text, sliders, input fields) + input locking + one example window. Deferred = animations/tweening helpers, docking/node editors/plots (ImGui add-ons), styling presets, multi-font/HiDPI polish.
- **Q6. Platform constraints** — KSP 1.12.x, Unity 2019.4.18f1, Mono x64, **Windows-first**, D3D11 primary + OpenGL secondary. Linux/Mac = long-term goal. D3D9 = no. DX12 assumed to work via backwards compatibility with D3D11 (flagged as assumption to verify — Unity 2019.4's DX12 path is experimental).
- **Q7. Compatibility** — Must coexist gracefully with IMGUI mods (by design). No specific mod interop required. **Hard requirement: must not conflict with Deferred (treated as baseline environment for all mods) or TUFX/post-processing.**
- **Q8. Persistence & lifecycle** — Confirmed: library is stateless across saves; consumer mods own their UI state; library may persist its own global config (font scale, theme) in a config file under `GameData`. Safe to install/update mid-save.

## Phase 2 — Technical Architecture & Integration (answered 2026-07-29)

- **Q9. Implementation layer** — Confirmed split: `DearImGuiKSP.dll` (managed C#: KSP plugin, lifecycle, input locks, theming, public API) + `DearImGuiKSPNative.dll` (native C++: ImGui context, frame rendering, GPU resources).
- **Q10. C# binding strategy** — **Option (a): P/Invoke against cimgui**, built by us. Full widget coverage, binding we own, no third-party binding dependency.
- **Q11. Data/schema** — No game-data changes. Library global config as a KSP `ConfigNode` file under `GameData/DearImGuiKSP/`.
- **Q12. Dependencies** — Library hard-depends only on the game (`KSPAssemblyDependency("KSP", 1, 12)`). Consumers hard-depend via `KSPAssemblyDependency("DearImGuiKSP", x, y)`. No soft deps for MVP. Assembly name and root namespace: `DearImGuiKSP`.
- **Q13. Data sources** — Confirmed: consumer mods push whatever game-state data they want as part of their per-frame UI declaration; the library itself reads only input state, screen size, and game UI scale.
- **Q14. Update cadence** — **Library-owned frame loop**: consumers register callbacks; the library drives input sampling, ImGui NewFrame/Render, and the native render handoff each frame.
- **Q15. Performance budget** — Baseline targets accepted (no measurable FPS impact with 1–3 typical windows; <1ms managed frame cost). Absolute measurement is hard, so we will design a **"torture test" benchmark UI** — reference: the `IMGUI_Helper` repo on this machine (`UI/Tabs/PerformanceTab.cs`, naive vs virtualized 1000-item list; `ReferenceNotes/IMGUI.md` §9, §12.2, §15) as the known IMGUI bog-down case to outperform.
- **Q16. Persistence** — Confirmed: library persists only its own global config (theme, font scale, UI scale) in the ConfigNode; window state belongs to consumer mods; optional position-persistence helper deferred.

## Phase 3 — Mechanics & Behavior (answered 2026-07-29)

- **Q17. Initialization & scenes** — Confirmed proposal (`KSPAddon(Startup.MainMenu, once:true)` + `DontDestroyOnLoad`, one-time native/device/atlas init). Library renders in **every** scene.
- **Q18. State machine** — Confirmed `Uninitialized → Initializing → Running → Failed`, **plus a `Suspended` state**: entered when the game UI is hidden (F2) and during loading screens; no rendering or consumer callbacks while suspended.
- **Q19. Consumer model** — Registration order is enough for MVP z-ordering. Deferred alternatives to note in the roadmap: "last interacted with" raise, player-assigned priority, etc.
- **Q20. Frame loop & input locking** — Confirmed frame loop as proposed. Input locks **only while capturing**: hover over a DearImGui-KSP window locks camera/click-through controls; active text field locks `KEYBOARDINPUT`; per-consumer lock IDs via `InputLockManager`.
- **Q21. Edge cases** — Confirmed: (a) throwing consumer callbacks caught/logged/skipped, auto-disabled after 5 consecutive throwing frames, other consumers unaffected; (b) resolution change → automatic viewport/atlas rebuild; (c) UI runs during pause/time warp.
- **Q22. Failure modes** — Confirmed: missing native DLL, managed/native version mismatch, unsupported graphics API, or render-hook failure → session-permanent self-disable + one stock `PopupDialog` at main menu + detailed log. Consumers can query `DearImGuiKSP.IsAvailable` before registering to fall back to their own UI.
- **Q23. Determinism** — Confirmed: no randomness; fully deterministic behavior.

## Phase 4 — UX & Interface (answered 2026-07-29)

- **Q24. Player-facing access** — **Option (a): no player-facing library UI in MVP** (config file only; access is consumer-driven). Built-in settings window (theme picker, UI scale) deferred.
- **Q25. Layout** — Stated and confirmed: consumers own window placement; library imposes no layout.
- **Q26/Q27. Theme** — **Stock ImGui dark theme only for MVP.** KSP-flavored theme and additional themes deferred. Consumers get full ImGui style API access regardless.
- **Q28. Animation** — Confirmed: no animation/tweening helpers in MVP (deferred); consumers can animate manually frame-to-frame.
- **Q29. Audio** — Confirmed: no audio in the library for MVP; consumers use game audio APIs themselves.
- **Q30. Accessibility** — Confirmed: global UI scale + font scale settings in the persisted config; HiDPI/multi-font polish deferred; colorblind safety is theme-dependent (consumer concern).
- **Q31. Fonts & localization** — Consumer-specified font loading **deferred**. User will choose a legally redistributable bundled default font. Library's own user-facing strings = failure popup only.

## Phase 5 — Content, Assets & Strings (answered 2026-07-29)

- **Q32. Asset inventory** — Confirmed complete as proposed: (a) one bundled default font; (b) `DearImGuiKSP.dll` (managed); (c) `DearImGuiKSPNative.dll` (native, x64 Windows; Linux/Mac binaries join later); (d) default settings ConfigNode under `GameData/DearImGuiKSP/`; (e) demo mod = code only, no assets. ImGui core widgets need no image assets.
- **Q33. Default font** — **ImGui's embedded ProggyClean is the MVP default** (no font file needed at all). Alternative font support is planned but deferred — the atlas/pipeline design must not hard-code assumptions that would block it later. Supersedes the Q31 plan of bundling a user-picked TTF.
- **Q34. Placeholder policy** — Agreed: the font may start as a placeholder (embedded ProggyClean) and be swapped before release with no API impact; nothing else may be placeholder at release.
- **Q35. String table** — Draft approved with **plain wording preferred**: popup title "DearImGui-KSP — Startup Failed"; body variants for missing/corrupt native component, unsupported graphics API, and managed/native version mismatch, each pointing to `KSP.log` and noting dependent mods will not work; log prefix `[DearImGuiKSP]`. To be finalized in the spec.
- **Q36. Voice / tone** — Confirmed: popup text is plain-language for players (what broke, what to do); technical detail goes to the log only.

## Phase 6 — Configuration, Compatibility & Distribution (answered 2026-07-29)

- **Q37. User-configurable settings** — Approved for MVP: `uiScale` (float, default 1.0, 0.5–2.0), `fontScale` (float, default 1.0, 0.5–2.0), `theme` (string, default `"dark"`, only valid value in MVP), verbose logging toggle, global enable/disable kill switch.
- **Q38. Settings persistence** — Confirmed: `GameData/DearImGuiKSP/settings.cfg` as a KSP `ConfigNode`; read at startup, written on change, no file-watching in MVP.
- **Q39. Distribution format** — Confirmed: release zip with `GameData/DearImGuiKSP/` containing `Plugins/DearImGuiKSP.dll`, `Plugins/DearImGuiKSPNative.dll`, `settings.cfg`, `LICENSE`, `README.md`, version file. Demo mod = separate zip (`GameData/DearImGuiKSPDemo/`). Install = drag into KSP root; CKAN metadata deferred to release time.
- **Q40. Compatibility guidance** — Documented environment to validate against: IMGUI mods, **Deferred**, **TUFX**, **Scatterer**, **Parallax**, plus user's own **Cinematic Shaders** and **Cinematic Recorder** (all present in the deployed location).
- **Q41. Versioning & updates** — User defers to agent judgment; policy to be proposed (SemVer + consumer breaking-change policy, pre-1.0 stability expectations, native/managed version lockstep).
- **Q42. Documentation needs** — Confirmed sufficient for MVP: consumer-facing API doc (README + XML doc comments), player-facing install/troubleshooting README, in-repo design/roadmap notes.

## Phase 7 — Architecture Justification (answered 2026-07-29)

- **Q41 (policy)** — Agent-proposed versioning policy accepted: SemVer mirrored in `KSPAssembly`/FileVersion/version file; managed+native lockstep; 0.x may break on minor bumps; 1.0+ freezes API within a major with `[Obsolete]` grace period; consumers guided to `KSPAssemblyDependencyEqualMajor`; ConfigNode carries a format version; per-release API changelog. Roadmap (non-MVP): consumer-compatibility smoke test, old-major support window.
- **Q43. Pattern selection** — Approved: Plugin/Mod Architecture (consumer registration), State Machine (library lifecycle), thin Repository (settings ConfigNode), composition-root-managed singleton (native render context). Rejected: Event Bus, Command, MVC/MVVM, Component-Based (no fit).
- **Q44. Layering** — Approved: Core = native DLL (ImGui context, frame lifecycle, draw-data→GPU, zero KSP/Unity knowledge); Application = managed (registration, frame-loop orchestration, input-lock policy, state machine, settings model); Infrastructure = managed (KSP addon entry, InputLockManager, ConfigNode I/O, logging, failure popup — the only KSP/Unity-touching layer).
- **Q45. Dependency direction** — Confirmed: Infrastructure → Application → Core (via stable C ABI); Core calls back only through Application-defined callback abstractions; only Infrastructure touches KSP/Unity APIs.
- **Q46. Anti-pattern risks** — Confirmed top three with mitigations: God Class addon (composition root only), Leaky Abstraction of cimgui pointers (public API = safe C# types), Premature Optimization (correctness first; torture-test benchmark gates optimization).
- **Q47. Next workflow** — Confirmed: route to `ProjectBootstrap.md` after spec approval; render-injection PoC is the first milestone.
