# Recording Latent / Unreachable Findings So They Aren't Re-Litigated

**Source:** Opus review triage 2026-09-07, cross-cutting theme 6. A large share of triage NOTEs are "real mechanism, unreachable today" (N4, N9, N10, I9, I10, A23, I52, …). Without a standard record, every future review re-derives and re-triages them.

## Record format

Any audit, review triage, or chunk contract that classifies a finding as latent/accepted must record four fields:

1. **Mechanism** — what would happen, one sentence.
2. **Why unreachable today** — the specific condition that prevents it (e.g. "only caller is game-thread-only", "callee is a no-op stub", "no consumer can construct this state").
3. **Activation condition** — the change that would make it live (e.g. "if Fail() becomes callable mid-frame", "if a second render backend lands", "if consumers gain async callbacks").
4. **Where recorded** — this note, an ISSUES entry, or the chunk contract.

If you can't state field 3 concretely, the finding is probably not latent — it's live and mis-triaged.

## Examples of the shape (from the 2026-09-07 triage)

- **N10** — five native exports mutate state outside the frame lock. Unreachable because all callers are game-thread-only today. Activates if any consumer-facing async entry point is added.
- **I9** — Logger→Settings→Store recursion. Unreachable because no Debug-level logging exists on the load path. Activates if load-path debug logging is added.
- **I10 / I45** — `Fail()` never releases input locks. Unreachable because Fail() is only invoked pre-frame. Activates if Fail() becomes callable mid-frame (already tracked as backlog P1 — cross-link, don't duplicate).

## Housekeeping

When doing work that touches an activation condition, grep this note and `ISSUES\TRACKER.md` for the entry before assuming the latent path is still unreachable. When a latent item goes live, move it from NOTE to WORK in the tracker at that moment — not after the first bug report.
