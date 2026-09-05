# Chunk Contract: Docs Polish (User Friction Pass)
## Plan: DearImGui-KSP Pre-Release Feature Wave
## Date: 2026-09-04
## Chunk ID: C26
## Advances Milestone: Wave polish (post-M7, pre-M8) — user-confirmed friction fixes

### Scope
M7's docs passed the modder-from-zero dry run (agent sandboxed to docs/ only —
zero API misuses). The user's own read-through found four friction points. All
fixes are docs-only; no code changes.

1. **Strip milestone headers** — every doc file opens with
   `> Authored in milestone M7 of the pre-release feature wave (spec §8.2, D31).`
   (7 files: 00, 10, 20, 30, 40, 50, 60, 70 — verify with grep). Delete these
   lines entirely; history lives in git, not in the docs.

2. **Installation clarity** (`docs/00-getting-started.md`, and anywhere else
   install/distribution is mentioned — grep for "install", "distribute",
   "bundle", "ship"): make explicit that (a) DearImGui-KSP is a shared library
   the **player installs separately** from its own release zip, (b) consumer
   mods must **not** redistribute/bundle DearImGuiKSP.dll or
   DearImGuiKSPNative.dll in their own downloads — they declare the dependency
   (`KSPAssemblyDependencyEqualMajor`) and point players at the library's
   release, (c) why: one shared copy, lockstep-managed versioning. The existing
   line "Your players install this folder once; many mods can depend on the
   same copy" is the seed — strengthen it into an explicit do/don't.

3. **Purge spec/process references** — user-facing docs must stand alone:
   no "see D17", "spec §5.4", "D33", "D32", milestone numbers, or pointers to
   notes/design artifacts. Known locations (re-grep to be sure):
   `00-getting-started.md:44,63` (D17), `70-troubleshooting.md:13` (spec §5.4),
   `:19` (D33), `:26,29` (D17, spec §5.4), `:77,79` (D32), `:98` (D33).
   Where the referenced decision carried real information, inline the fact in
   plain words (e.g. "managed and native DLLs always release together in
   lockstep — never mix DLLs from different releases"; "OpenGL support is not
   shipped yet; it may come in a later release"). Drop pure provenance
   parentheticals with no reader value.

4. **Plain-language skim for human modders** — the dry run proved an agent can
   follow the docs; the audience is human KSP modders. Read
   `00-getting-started.md` and `10-api-fundamentals.md` in full and lightly
   de-jargon where a competent-but-new modder would stumble (spell out acronyms
   on first use, prefer plain verbs, keep sentences short). This is a skim, not
   a rewrite: do not restructure, do not change technical content, do not touch
   the other six files beyond points 1–3 unless a sentence is genuinely
   confusing.

### Inputs (must exist before starting)
- `docs/*.md` final content (C22/C23 committed `95f6da8`, C24 link-checked).

### Outputs (must be created/changed)
- `docs/*.md` edits per the four points (minimal diffs).
- No other files. NO code, NO signature changes.

### Constraints
- D31: no emojis/symbol glyphs.
- Do not reword content that is already accurate and plain (per C24's "do not
  reword accurate docs" rule — this chunk's mandate is the four points only).
- Cross-links between docs must still resolve after edits (C24 verified 69/69 —
  re-run the same check).
- Do not delete technical facts a modder needs (handshake version, vertex
  limit, D3D11-only) — strip the *references*, keep the *facts*.

### Verification
- Grep proof: zero hits for `Authored in milestone`, `\bD[0-9][0-9]\b`,
  `spec §`, `DESIGN_SPEC`, `notes/`, `DECISION_LOG` across `docs/`.
- Docs link check re-run: every relative .md link resolves.
- `dotnet build DearImGui-KSP.slnx` still 0/0 (docs shouldn't touch it, but
  confirm nothing else drifted); `dotnet test` 90/90.
- Read `00-getting-started.md` end-to-end once after editing: a modder should
  learn "install the library separately, never ship it yourself" without any
  ambiguity.

### Rollback
- Revert the touched docs files.
