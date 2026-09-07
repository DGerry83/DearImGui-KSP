# Chunk Contract: C05 — Native build tooling hardening
## Plan: Opus Review Remediation (`notes\plans\opus-review-remediation-contracts.md`)
## Date: 2026-09-07
## Chunk ID: C05
## Advances Milestone: Post-1.0.0 remediation wave (native build items G2-02, G2-03, G3-02)

### Scope
- **G2-02 (cimgui/imgui pin enforcement)**: new shared `DearImGuiKSPNative\check_build_env.bat` holds the pinned commits (cimgui clone `b705b2465a17428a3ab6b19c893b452a70dcbb14`; its imgui submodule `b334d19b667958ed970000073644d911fae17e57`, v1.92.9-docking) and verifies both via `git rev-parse HEAD` before any compile, per PIN_RECORD.md's "build must fail loud on drift" rule. Mismatch, missing clone, missing git, or an uninitialized submodule each abort with exit 1 and a message naming the pinned vs actual commit. The pin constant lives in exactly one file; the three build scripts call the helper.
- **G2-03 (release PDB)**: `build_release.bat` gains `/Zi` (compile) and `/DEBUG` (link) plus `/Fd:build\`, emitting `build\DearImGuiKSPNative.pdb` next to the release DLL so shipped-DLL crash addresses can be symbolised. `/O2 /DNDEBUG` and all other flags unchanged; the PDB is not copied into GameData (packaging scope, not this contract).
- **G3-02 (vcvars64 fallback + error checks)**: the helper resolves vcvars64.bat via the existing hard-coded VS 18 Community path first, then `vswhere -latest -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64`; not-found and vcvars-failure both abort with clear messages. All three scripts now check `errorlevel` after the environment step (previously none did — a failed vcvars silently fell through to a missing cl.exe).

### Inputs
- Triage sweep verdicts G2-02 (PARTIAL — fail-loud rule extended to the sibling clone), G2-03 (VALID), G3-02 (VALID): `notes\active\2026-09-07_Bug_OpusReviewTriage\TRIAGE_SWEEP.md`.
- `DearImGuiKSPNative\vendor\PIN_RECORD.md` pin row (`pinned cimgui clone @ b705b24 (imgui 1.92.9)`); submodule commit read from the clone's gitlink (`git ls-tree HEAD imgui`).

### Outputs
- New: `DearImGuiKSPNative\check_build_env.bat` (vcvars discovery + pin enforcement, called by all three scripts).
- Changed: `DearImGuiKSPNative\build.bat`, `build_release.bat`, `build_harness.bat` (call the helper; release gains `/Zi` + link `/DEBUG`).

### Constraints
- Plain cl.exe scripts stay (no CMake/vcxproj); no new third-party dependencies (vswhere ships with the VS installer, used only as a fallback when present).
- Optimization flags untouched; the release DLL's codegen is identical apart from debug-info emission.
- §5.9 native-interop checklist: not applicable (build tooling only, no runtime code).

### Verification
- `cmd //c build.bat` from Git Bash: PASS (debug DLL built, copied to GameData PluginData).
- `cmd //c build_release.bat`: PASS; `build\DearImGuiKSPNative.pdb` (48 MB) emitted next to the release DLL.
- `cmd //c build_harness.bat` + `build\harness.exe`: PASS ("HARNESS PASS").
- Fail-loud demonstrated: pin constant temporarily set to all-zeros → `build.bat` printed `ERROR: cimgui pin mismatch (PIN_RECORD.md fail-loud rule)` with pinned/actual commits and exited 1 before any compile; constant then restored (verified by grep).
- vcvars default path still resolves on this machine, so the vswhere fallback is compile-checked but not exercised live (it is a standard `vswhere -property installationPath` query).

### Rollback
- Revert the three build scripts and delete `check_build_env.bat` (`git checkout -- DearImGuiKSPNative/build*.bat`).
