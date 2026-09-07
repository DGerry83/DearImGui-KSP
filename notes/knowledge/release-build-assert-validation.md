# Release Build Strips Upstream Asserts — Managed Layer Owns Boundary Validation

**Source:** Opus review triage 2026-09-07 (`notes\active\2026-09-07_Bug_OpusReviewTriage\TRIAGE_SWEEP.md`, cross-cutting theme 1; items G2-10, G3-22/23/24/30, O18).

## The rule

The shipped native DLL is built `/DNDEBUG`, which compiles out **all** ImGui/ImPlot internal asserts. Any unvalidated enum, ordinal, count, or size forwarded across the P/Invoke ABI is therefore **undefined behavior in release**, not a debug assert. The managed layer is the only validation layer that ships. Treat every public API parameter that crosses the ABI as untrusted input.

## Guard family (where this has already bitten)

- **Enum ordinals forwarded unchecked:** `PushStyleColor` accepts the public `COUNT` enum member → OOB write past `style.Colors` (`ImGuiStyleEnums.cs:141`); `PushStyleVar` same shape (`DearImGuiKSP.cs:458`). No guard even in debug.
- **Counts forwarded unvalidated:** `BeginSubplots` rows/cols — ImPlot's only guard is compiled out (`ImGuiPlot.cs:85`).
- **Capacity ignored:** `InputText` uses the shared buffer's grown length, not the caller's capacity (`ImGuiInternal.cs:144`).
- **Missing clamp flag:** `SliderFloat` omits `AlwaysClamp`; type-in entry reachable via Ctrl (`ImGuiInternal.cs:68`).
- **Docs promised an end-of-frame assert that /DNDEBUG removes** (`docs/10-api-fundamentals.md:110`) — never document a guard that only exists in debug builds.

## When adding a new binding

For each forwarded parameter ask: *what does the upstream assert say, and where is that check now?* If the answer is "compiled out", add a managed-side guard (range/ordinal check, throw or clamp per the widget contract) or record an explicit trust decision in the chunk contract. When in doubt, read the upstream header's `IM_ASSERT` for the function being bound — that is the validation spec you inherit.
