# Phase 6 — CEO Decision — Cycle 8 (Runtime Timeframe Selector)

**Date:** 2026-08-05
**Owner request:** vision1068 (human) — explicitly asked for a runtime timeframe option that
recalculates the signal/chart accordingly.
**Decision: APPROVE-WITH-CONDITIONS.**

## What was authorized / built

A **runtime timeframe selector** on the dashboard (FR-39/FR-40): the user can switch the analysis
timeframe among the curated standard set (M1 / M5 / M15 / M30 / H1 / H4 / D1, default H1) at any
time, and the signal + chart recalculate at the selected timeframe — for **both** the non-live
sample path and the Cycle-7 live MT5 path. It replaces the two hardcoded `TimeFrame.H1` sites in
`App.xaml.cs`.

- No new seam is opened. **C-3a** (live read-only) stays as authorized in Cycle 7; **C-3b**
  (order/execution) stays **permanently closed** (INV-1). The selector only chooses a candle
  bucket size.

## Why approve

1. **Directly what the owner asked for.** The request was a timeframe option with recalculation;
   that is exactly what shipped, extended to the full standard set (D8-1) since the bridge already
   supports all seven.
2. **Spec-first satisfied.** `cycle8-spec.md` carries IDed FR-39/FR-40 + NFR-6..8, a Mermaid
   recalculation/safety flow, and the D8-1..6 decision record — written before the implementation
   claim.
3. **Safety envelope intact (QA + Audit converged).** INV-1..INV-5 verified structurally; **230**
   C# tests (218 baseline + 12 new) + **32** Python green; WPF build **0/0**; `Testing.dll`
   absent; **0** order verbs; **0** conditional compilation; **0** new packages; `SetupProfile`
   unchanged.
4. **The one real risk of this feature is closed.** A timeframe switch could have shown a
   prior-timeframe signal under the new label (INV-4). It cannot: the signal is cleared on switch,
   an in-flight poll is snapshotted to its start-of-refresh timeframe, a superseded poll is
   discarded, and the audit trail records the timeframe the refresh actually used. Proven by
   `Refresh_uses_timeframe_snapshotted_at_start` and the composition-root guard.
5. **No governance surface added.** No credential/transport/persistence/order path is touched;
   the audit trail already covers the (unchanged-in-nature) advisory surface.

## Conditions (carried to the owner — enumerated, not skipped)

1. **C-A — Manual wiped-profile E2E of the built exe (this cycle's specific manual gate).** Run
   the built app and exercise the new selector: in a sample session, pick D1 and confirm the
   signal + chart recalculate and are labeled D1; if a live terminal is available, switch
   timeframe and confirm the strip shows "Recalculating…" then a fresh new-timeframe result,
   never a stale one. This is WPF-runtime behaviour that headless tests structurally cannot reach
   (2026-07-31 lesson) — **owner manual step.**
2. **C-B — Real-terminal live E2E** of a timeframe switch (demo account first). Carried from
   Cycle 7 — no MT5/credentials in the build environment. **Owner manual step.**
3. **C-C — Cycle-6 AC-34.3** (published-exe launch from a wiped profile) remains open. **Owner
   manual step.**
4. **C-D — Re-publish the self-contained exe** so the owner receives a build that includes the
   selector, then hand it over. Do **not** disturb the owner's currently-running exe/bridge; the
   owner swaps in the fresh build themselves.

## Standing conditions status

- **C-1** (disclaimer) ✅ intact, docked and visible above the new selector in every session.
- **C-2** (durable journal) ✅ intact.
- **C-3a** ✅ authorized (Cycle 7, read-only). **C-3b** ⛔ permanently closed.

## Not a blanket go-live

This approves the **code slice** for the timeframe selector and its live-recalculation behaviour.
It does **not** assert the built exe has been run end-to-end (C-A), nor that a live account was
exercised (C-B), nor does it close packaging (C-C). Per the owner's standing instruction, **nothing
is committed or pushed** — the changes stay staged in the working tree for the coordinating session
to review and commit, exactly as Cycles 6-7 arrived.
