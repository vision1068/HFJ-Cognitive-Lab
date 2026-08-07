# Phase 5 — Audit / Governance Gate — Cycle 8 (Runtime Timeframe Selector)

**Verdict: PASS-WITH-CONDITIONS.** The change is a UI/UX + live-refresh feature on the
already-authorized (Cycle-7) read-only seam. It opens **no new seam** — no credential, transport,
persistence, or order surface is touched — so it introduces **no new governance surface**. The
permanent invariants hold structurally, and the one novel risk this feature could have created (a
prior-timeframe signal shown under a new label) is closed in both the type/coordinator layer and
the composition root. Remaining conditions are the two standing owner manual steps carried from
Cycle 7, plus a routine re-publish.

## 1. Authorization / chain of custody — CLEAN (no gate touched)

- This cycle does **not** open, lift, or cite any named-approver gate. C-3a (live read-only)
  stays as authorized in `phase-6-ceo-cycle7.md`; **C-3b (order/execution) stays permanently
  closed** — the selector only chooses a candle bucket size.
- No code comment in this cycle asserts an approval. The 2026-08-01 fabricated-authorization
  defect class is not re-introduced: **grep for "has been lifted" / "see phase-6-ceo-cycleN" that
  do not exist = 0**; the only sign-off this cycle produces is this Phase-5 doc + `phase-6-ceo-cycle8.md`,
  and no code cites a not-yet-existing file.
- Spec-first honoured: `cycle8-spec.md` (FR-39/FR-40, NFR-6..8, Mermaid flow, D8-1..6) exists and
  precedes the implementation claim.

## 2. Regulated-advice surface — UNCHANGED, still ADEQUATE

The live feed already renders advisory BUY/SELL on a real account (Cycle 7). Cycle 8 lets the
user change the timeframe of that advice; it does not change the advice's *nature*. Controls
re-verified:

- **CF-1 Advisory-only / no auto-execution:** INV-1 structural — 0 order verbs; the new
  `SetTimeFrame`/selector expose no order/command member (reflection guards pass). **PASS.**
- **CF-2 Disclaimer adjacency:** the always-visible "NOT INVESTMENT ADVICE" banner (C-1) is
  docked **outside** the ScrollViewer and unchanged; the new selector sits below it, also always
  visible. Scores stay 0-100; the selector shows timeframe **labels** (M1…D1), never a `%`
  (INV-5). **PASS.**
- **CF-3 No fabricated / mislabeled live data (the cycle's key risk):** a timeframe switch clears
  the signal to Neutral *before* any new-timeframe result appears (D8-3); an in-flight poll is
  bound to its start-of-refresh timeframe (snapshot, AC-40.3); and a poll whose timeframe was
  superseded mid-flight is **discarded**, never painted or audited. A switch cannot bypass the
  freshness/veto gate (freshness is tick-recency-keyed, not candle-timeframe-keyed — AC-40.2).
  **PASS.**
- **CF-4 Auditability (FR-38):** the durable JSONL trail now records the timeframe **the refresh
  actually used** (`tfThisPoll`, not a const), so the trail cannot mislabel which timeframe a
  displayed/suppressed signal belonged to. No fabricated numbers on suppression; no secret.
  **PASS.**
- **CF-5 No credential stored:** untouched — no credential, no transport, no `SetupProfile`
  change this cycle. **PASS.**

## 3. Data residency / security

- No new persisted state (D8-6: the selected timeframe is runtime-only). `SetupProfile` and its
  on-disk JSON are byte-for-byte unchanged; the no-secret reflection guard and persisted-JSON
  scan are unchanged and still green. **PASS.**
- No new dependency, no new network surface, no new file written. Attack surface is unchanged
  from Cycle 7. **PASS.**

## 4. Conditions (carried / routine — none are new code defects)

| # | Condition | Type | Blocking? |
|---|-----------|------|-----------|
| A | **Manual wiped-profile E2E of the built exe** exercising the new ComboBox: sample session (pick D1 → signal + chart recalc, labeled D1) and, where possible, a live session (switch shows "Recalculating…" then the new-TF result, never a stale one). This is the WPF-runtime behaviour headless tests structurally cannot reach (2026-07-31 lesson). | Owner manual step — needs interactive desktop. | Blocks *the owner's own* confidence run; NOT a code defect. |
| B | **Real-terminal live E2E** of a timeframe switch against an actual MT5 terminal (demo first). | Owner manual step — no MT5/credentials in this environment. | Carried from Cycle 7; not a code defect. |
| C | **Cycle-6 AC-34.3** (published-exe launch from a wiped profile) remains open. | Owner manual step. | Carried from Cycle 7 / Cycle 6. |
| D | **Re-publish the self-contained exe** after approval so the owner gets a build that includes the selector. | Routine build step. | Not blocking approval; do before handing the owner a new build. |

## 5. Summary

A low-risk UI/live-refresh feature on an already-authorized seam. It adds **no** governance
surface, touches **no** credential/transport/order path, and the one invariant it could have
threatened (INV-4, timeframe mislabel) is closed structurally and in the composition root.
INV-1..INV-5 hold. Ship the slice through the CEO gate; conditions A-C are the standing owner
manual steps (enumerated, not skipped) and D is a routine re-publish.
