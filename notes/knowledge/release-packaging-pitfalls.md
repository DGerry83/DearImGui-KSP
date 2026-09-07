# Release Packaging & Distribution Pitfalls

**Source:** Opus review triage 2026-09-07 (G2-16, G3-02, G3-04, G3-05, G3-16, G3-37, G3-38, G3-42, G3-44, G3-45, G3-46). Machine- and KSP-distribution-specific; not workflow-generic.

## Zip contents

- **Never ship `settings.cfg` (or any player-state file) in a release zip** — extracting an upgrade silently resets player settings (G2-16, `package_release.bat:49`). Ship a defaults template only if the loader tolerates its absence.
- Ship `License.txt`/`Readme.txt` in every zip (G3-37, G3-42 — release-gate step 4 omits License.txt and its `docs\*.md` check is unreachable by construction).
- Demo zip must be named from the **demo's** own version property, not the library's (G3-45 — latent until the demo revs independently).

## KSP-AVC / .version metadata

- `.version` files need an explicit `KSP_VERSION` (min/max) — KSPBuildTools defaults advertise 1.8–1.12 compatibility the mod never had (G3-04, G3-38).
- The AVC `URL` must point at a **raw** .version file, not a repo HTML page (G3-05).
- Assembly metadata consistency: `AssemblyCopyright` "DGerry" vs LICENSE/README "DGerry83"; Authors/Company unset (G3-46).

## Script robustness (this machine's toolchain)

- `Compress-Archive` per-entry errors are **non-terminating and exit 0** — an errorlevel-only guard reports success on a truncated zip (G3-44). Verify archive integrity explicitly (entry count / test-extract) or use `$ErrorActionPreference = 'Stop'` plus try/catch.
- Native build scripts hard-code one `vcvars64` path with no fallback or errorlevel check (G3-02) — fail loudly with a "run from a VS developer prompt" message.
- Release native build emits **no PDB** (G3-03-class diagnostics: shipped-DLL crash addresses are unsymbolisable, G2-03) — emit PDBs in release and keep them per-release even if not shipped.

## Gate consequence

Any milestone that ships a distributable artifact needs a packaging gate: contents listing reviewed (no player state, licenses present), metadata eyeball-checked, archive integrity verified — code-level gates see none of this.
