# Investigation Log: Window Docking (ISSUES #011)
## Date: 2026-09-08
## Status: Scope Checked
## Type: Feature

### Symptom / Scope Profile
- **Primary Goal**: Users can dock DearImGui-KSP windows to each other (ISSUES #011, filed 2026-09-04 as an upgrade; deferred behind OpenGL per D37, which shipped as 1.1.0 on 2026-09-08).
- **Request boundaries**: enable ImGui docking for windows rendered by the library; expose enough API for consumers to use it; demonstrate it in the demo mod. Work happens on branch `feature-Docking`, PR-merged at the end.
- **Success criteria** (draft, to be frozen as gates in Phase 1):
  - Windows can be docked to each other / into a dockspace in-game on D3D11 and OpenGL Core.
  - No regression to non-docking behavior (default window flow, input shields, clamping, theming).
  - Public API addition is idiomatic C# per project conventions (XML docs, strong types, IDisposable where Begin/End pairs exist).
  - xUnit suite green; docs updated; demo showcases docking.
- **Reproduction Steps**: N/A (feature).
- **Frequency**: N/A.

### Key Findings (research, 2026-09-08)

1. **The base-build switch has already happened.** The issue text says docking "requires switching to the ImGui docking branch" — but `check_build_env.bat` (committed f27cef3, 2026-09-07, OpenGL session C05) already pins cimgui @ `b705b24` (its `docking_inter` branch) and imgui @ `b334d19` = `v1.92.9-docking-1` (the fix for the 1.92.9 `ImDrawData::CmdListsCount` regression). The sibling clone sits on exactly those commits, and the fail-loud pin check means **1.1.0 itself shipped built against the docking branch**. The riskiest part of #011 (the pin swap) is therefore already absorbed and in-game-validated for non-docking use.
   - Documentation drift to fix in this session: `vendor/PIN_RECORD.md` and the native README still say "pinned imgui 1.92.9" without the `-docking` qualifier.
2. **Docking API is already exported.** The compiled-in cimgui exposes `igDockSpace`, `igDockSpaceOverViewport`, and the full `igDockBuilder*` family (16 symbols). No regeneration needed.
3. **Docking is compiled in but never enabled.** No reference to `ImGuiConfigFlags_DockingEnable` anywhere in native or managed code. Enabling it is a one-line native change (`io.ConfigFlags |= ImGuiConfigFlags_DockingEnable` in `ContextHost.cpp`), optionally gated by a settings value following the existing live-settings pattern (D34, e.g. `SetUiScale`).
4. **No imgui.ini** (`io.IniFilename = nullptr`, ContextHost.cpp:166 — "window state belongs to consumers", D7, spec §5.4/§6.2). Docking layout persistence in stock ImGui flows through imgui.ini, so a persistence policy decision is required (see Candidates).
5. **Handshake is at v8** (`NativeBridge.ExpectedNativeVersion = 8`, native `return 8`). Any new managed→native export means v9 in lockstep (D17).

### Data Loss / Risk Assessment
- **Corruption Risk**: No. Docking is runtime UI state; nothing persisted by the library beyond settings.cfg.
- **Affected State**: Per-frame window layout behavior; input-capture shields; window clamping (D21).
- **Change Risk Level**: Medium. Small native delta, but broad behavioral surface once `DockingEnable` is on (every window becomes dockable; drag behavior changes).
- **Interaction watchlist** (for Phase 1 gates):
  - `clampWindowsToViewport` (D21) vs docked windows (docked windows are positioned by the dock node, not the consumer).
  - uGUI raycast blocker / IMGUI `GUIUtility` capture grab (D22/D23) — capture math must still match during dock drags and tab drags.
  - Multi-viewports: the docking branch also carries platform-viewport support. **Intent: `ImGuiConfigFlags_ViewportsEnable` stays OFF** — an embedded Unity plugin cannot sanely spawn OS windows; docked windows cannot leave the game window. Recorded as a decision candidate for the user.
  - Both render backends (D3D11 + OpenGL Core) — docking itself is backend-agnostic (draw-list level), but in-game validation covers both per the D35 environment setup.

### Fix / Feature Candidates
- **Candidate 1 — Always-on docking**: enable `DockingEnable` unconditionally; expose dockspace/dock-builder bindings; consumers opt windows out with `NoDocking` where needed. | **Pros**: smallest surface, no new settings, no handshake bump for a toggle | **Cons**: players can't turn it off; accidental docks possible on every consumer window; a global behavior change for all existing consumers (library is at 1.x stability).
- **Candidate 2 — Settings-gated docking (recommended)**: new `docking` key in the library's settings.cfg (D34 pattern), default ON, applied live via a new native export (`SetDockingEnabled`) → handshake v9; plus the managed docking API. | **Pros**: player-visible opt-out consistent with theme/uiScale; failure-handling friendly; follows existing live-settings plumbing | **Cons**: one more export + handshake bump; slightly larger surface.
- **Candidate 3 — Full persistence via imgui.ini**: additionally set `IniFilename` to a library-owned path so dock layouts persist across sessions. | **Pros**: free layout persistence | **Cons**: violates "window state belongs to consumers" (spec §6.2, D7) — ini would also capture window positions/sizes for all consumers. **Rejected**; persistence, if wanted, belongs in a consumer-facing DockBuilder save/load helper (deferred unless the user asks).

### Open Questions for User (Phase 1 inputs)
1. Docking default: settings-gated default-ON (Cand. 2) vs always-on (Cand. 1)?
2. API depth: minimal (enable + `DockSpaceOverViewport` + `NoDocking` window option) vs fuller (`DockBuilder*` programmatic layout API)? Recommendation: enable + dockspace + dock-builder subset — the builder is what makes docking useful without ini persistence.
3. Demo showcase extent: add a dockable layout to the telemetry demo, or a small dedicated docking demo window?
