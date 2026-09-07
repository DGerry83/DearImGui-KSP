# Hand-Mirrored ABI Constants Need Mechanical Pins

**Source:** Opus review triage 2026-09-07, cross-cutting theme 5 (I18, N13/T19, I23, T18/T23). First flagged 2026-09-03 (`EXTERNAL_REVIEW_2026-09-03.md`, "version handshake magic number") and still unpinned.

## The rule

Any value hand-maintained on both sides of the managed↔native boundary (or in two managed files) must have a **mechanical agreement check** — a pin test, a compile-time assert, or generation from a single source. Hand-mirroring without a pin drifts silently; the failure mode is a release-build ABI mismatch, the worst kind to diagnose.

## Known unpinned mirrors (as of 1.0.0)

| Value | Locations | Check today |
|---|---|---|
| Native handshake version constant | managed `ExpectedNativeVersion` + native export | none — hand-copied (I18) |
| `ImGuiCol` / `ImGuiStyleVar` ordinal tables | managed enums vs imgui.h | none (N13); enum pin tests T18/T23 missing |
| Render-event id literal `0` | `DearImGuiKSPAddon.cs:138` vs `NativeBridge.cs:132` | none (I23) |
| cimgui/imgui pinned commit | `PIN_RECORD` fail-loud rule vs `build_release.bat` | not enforced at build (G2-02) |

## When adding a mirrored value

1. Prefer single-source: generate one side from the other (header parse, codegen, shared include) when the tooling cost is proportionate.
2. Otherwise add a pin test that fails the build on drift: for enums, assert managed ordinal == native value (round-trip through a native export or a static table); for versions/ids, a unit test comparing both sides' constants.
3. If neither is proportionate, record the mirror as an accepted risk in the chunk contract with the rationale — never leave it silent.
4. Vendor pin records are only as good as their enforcement: a fail-loud rule that no build script checks is documentation, not a gate (G2-02).
