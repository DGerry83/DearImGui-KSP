# Hot-Path Write Amplification — Persistence Belongs Behind a Debounce, Not in the Frame

**Source:** Opus review triage 2026-09-07, cross-cutting theme 3 (G3-08/09/14/16 — one fix closes all four). Related allocation items: G3-12/13/29.

## The rule

`reference\08-native-interop.md` Check 2 covers per-frame **heap allocation**, but the cost family is wider. On any per-frame or high-frequency-event path (slider drags, knob turns), also check for:

- **I/O per event** — `SettingsModel.cs:44` rewrites `settings.cfg` from scratch on *every changed slider frame*; `LibraryControlPanel.cs:105` does it **inside the held frame lock**.
- **Wholesale rebuilds per event** — `ThemeEngine.cs:65` rebuilds and re-applies the whole theme per uiScale drag frame.
- **Non-atomic writes** — `SettingsStore.cs:127` writes the config in place; a crash mid-write corrupts player settings. Persist via temp-file-then-rename.

## The pattern that fixes all of it

Dirty-flag + debounce + apply-on-commit: high-frequency input mutates in-memory state and sets a dirty flag; persistence and expensive re-application happen once when the gesture ends (or after a quiet interval), atomically, and never inside the frame lock. Apply-on-drag stays for *visual preview only* when the preview is cheap; theme rebuilds and disk writes are not cheap.

## Checklist for new settings/panel work

1. Does this handler run per drag-frame? If yes, what does it touch — memory only, or disk/theme/locks?
2. Is any write atomic (temp + rename)?
3. Is anything allocated per frame (`new HashSet` in `InputLockGateway.cs:20`, double byte-array in `ToUtf8` `ImGuiInternal.cs:708`, coroutine `WaitForEndOfFrame` alloc `DearImGuiKSPAddon.cs:62`)? Hoist or justify.
