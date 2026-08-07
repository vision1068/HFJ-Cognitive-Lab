# Phase 6 — CEO Decision — Cycle 9 (Real Higher-Timeframe Confirmation)

**Date:** 2026-08-05
**Owner request:** vision1068 (human) — explicitly authorized this fix cycle for the HTF-confirmation bug.
**Decision: APPROVED WITH CONDITIONS.** (Code slice only — NOT a go-live; the built exe was NOT run
against a real terminal.)

## What was fixed

The FR-21 higher-timeframe confirmation guard performed **no real check** anywhere:
- **Live path** — `SignalContext.HtfDirection` was never populated (`new SignalContext()` in
  `App.StartLiveFeed`), so `HtfDirection` stayed `null`; since `null != Buy/Sell` always, the guard
  forced Neutral on **every** live poll. The live BUY/SELL surface on a real broker instrument had
  therefore **never once emitted an actionable signal**.
- **Sample path** — `HtfDirection` was hardcoded to `Buy` — an unconditional pass.

Cycle 9 derives a **genuine** higher-timeframe direction from a stepped-up-timeframe candle series
through the existing tested core (indicators → regime → independent score lean) on **both** paths,
fails safe to Neutral when a real HTF read is unavailable (empty/failed/tie/frozen), disables the
guard only at the top of the ladder (D1, where no higher curated timeframe exists), and records HTF
provenance on the live audit trail. Spec: `cycle9-spec.md` (FR-41/FR-42/FR-43, D9-1..D9-11).

## Why approve

1. **The reported bug is provably closed** — `Live_refresh_confirms_with_real_htf` yields an
   actionable Buy; `Live_refresh_blocks_on_conflicting_htf` blocks on a genuinely-different bearish
   HTF series (independently re-run by Phase-4 QA).
2. **Fail-safe is real, not fabricated** — unobtainable/empty/tie/frozen HTF → Neutral, never an
   invented direction (D9-4, D9-11; three-state audit provenance, no fabricated field on suppression).
3. **No new seam** — the HTF read reuses `GetCandlesAsync` only; no new order verb/credential/
   transport/persistence/bridge endpoint. INV-1..INV-5 confirmed intact in shipped code (Phase 5).
   C-3b permanently closed.
4. **Traceability + gates** — 265 C# tests pass / 0 fail (230 baseline + 35 new), Python 32 OK, WPF
   Release 0/0, Testing.dll absent, 0 new packages, 0 conditional compilation, 0 real order verbs.
5. **No fabricated authorization** — grep for `phase-6-ceo-cycle9` in code = 0 (IC-7).

## This is an ACTIVATION EVENT (recorded, not buried)

Before Cycle 9 the live advisory surface never advised once. After it, the surface emits actionable
BUY/SELL on a real broker instrument. This is not a routine code tweak — it is the moment the
advisory capability becomes live. Every condition below gates that reality, not the code quality.

## Conditions carried to the owner (all must be satisfied before live use)

1. **C-A (CC-1) — Activation acknowledgement + disclaimer revisit.** The owner is on record that this
   cycle turns on a surface that has never fired. Cycle-7 CF-1..CF-5 (disclaimer, audit trail,
   no-fabrication) are **provisionally** re-affirmed on the evidence, with final re-affirmation
   dependent on C-C. **Cycle-7 condition C-C (disclaimer clause) is RE-OPENED:** before live use the
   owner must confirm the disclaimer explicitly covers that the tool now emits actionable BUY/SELL,
   that these are non-advisory informational leans only. **Owner step.**
2. **C-B (CC-2) — D1 fires without HTF confirmation, by design.** At the top of the curated ladder
   there is no higher curated timeframe, so the HTF guard is a deliberate no-op (D9-3), explicit in
   code via `HtfConfirmationApplicable=false` (not the old accidental null). Accepted as a **named,
   owner-visible** condition: at D1, signals fire subject to all OTHER guards but with no
   higher-timeframe confirmation. Must never be silent.
3. **C-C (CC-3) — Owner-manual reality checks (strengthened).** (a) Real-terminal live E2E —
   strengthened to require confirming a genuine live **HTF-confirmed** actionable BUY/SELL actually
   appears (not merely that suppression works); (b) the Cycle-6 wiped-profile published-exe launch,
   because the composition root (`App.OnStartup`/`StartLiveFeed` + the sample call site) changed
   (2026-07-31 WPF lesson). **Owner steps** — cannot be run in this environment.

**C-D removed (2026-08-07):** this is the owner's personal project, unrelated to QDB or QCB — the
regulatory-dependency condition does not apply and has been struck at the owner's explicit
instruction.

## Standing conditions status

- **C-1** (disclaimer) — intact; **re-opened for review** per C-A above now that signals fire.
- **C-2** (durable journal) — intact.
- **C-3a** — authorized (Cycle 7, read-only). **C-3b** — permanently closed (INV-1).

## Not a blanket go-live

This approves the **code slice** for real HTF confirmation. It does not assert the built exe was run
end-to-end (C-C), nor that a live account was exercised, nor does it close the disclaimer re-review
(C-A) or the regulatory dependency (C-D). **Per the owner's standing instruction, nothing is committed
or pushed** — all changes stay staged in the working tree for the owner to review and commit, exactly
as Cycles 6-8 arrived.
