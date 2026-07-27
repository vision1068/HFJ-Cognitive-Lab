# BTC/USD Disclaimer — APPROVED (Authoritative BTC Source Text)

> # ✅ APPROVED 2026-07-27 — this is now the authoritative BTC/USD disclaimer text.
>
> The user approved the draft below **as-is, no edits requested**. It is now the **BTC analogue of
> gold's spec §40** (FR-35): authoritative source text for the BTC/USD disclaimer surface. FR-44's
> acceptance criteria (AC-44.1 / AC-44.4) **may now be verified against this text** when that phase is
> built. It remains **Cycle = R (Roadmap)** — approval of the *text* authorizes no implementation; the
> disclaimer still ships only when spec Phase 6 (onboarding/UI) is built.
>
> Original draft banner (2026-07-26, preserved for the audit trail) read: *"THIS IS A DRAFT. IT IS NOT
> APPROVED AND NOT SPEC-FINAL... It must not be shipped, embedded in code, or displayed to any user
> [until approved]."* That condition is now satisfied.

**Project:** gold-signal-analyzer · **Date drafted:** 2026-07-26 · **Date approved:** 2026-07-27
**Status:** APPROVED — authoritative BTC/USD source text (parallel to gold §40)
**Traceability:** FR-44, NFR-5, NFR-11; parallels FR-35 / spec §40 (gold); strategic risk (a) in `phase-1-ceo.md`.
**Scope:** BTC/USD only. Gold retains its **exact §40 wording** unchanged — this draft does **not** touch gold.

---

## Why BTC needs its own disclaimer (rationale — not part of the displayed text)

Gold's §40 warning was written for a **session-gapped precious metal**. BTC/USD on an Exness account is
a **crypto CFD traded through a forex-style broker**: it is a 24/7 market with **no session close**,
**materially more volatile** than gold, and carries **leverage, overnight swap/funding charges, and CFD
counterparty risk** that a gold-worded disclaimer does not honestly cover. Showing gold's (or a generic)
disclaimer while BTC is analyzed would violate NFR-5 / NFR-11. The draft below keeps **all** of gold's
honesty principles and **adds** the BTC/crypto-CFD-specific risks.

---

## PROPOSED DISPLAYED TEXT (draft — pre-use acknowledgement screen for BTC/USD)

### Before you use BTC/USD analysis — please read and acknowledge

**This is an analysis tool, not financial advice.**
Gold Signal Analyzer produces **educational, decision-support** analysis of BTC/USD only. It does
**not** give investment advice, recommendations, or solicitations to trade. Every signal is a
transparent, auditable read of current market conditions — a *second opinion* to help you think, never
an instruction to act. You alone are responsible for your trading decisions.

**No guaranteed profit. Ever.**
This tool cannot and does not promise, imply, or guarantee any profit or outcome. Trading BTC/USD can
and does lose money, potentially rapidly and in full.

**A score is not a win-probability.**
BTC/USD signals are scored 0–100. **That number is a measure of how strongly current conditions align
with the analysis — it is NOT a percentage chance of winning.** A score of `75` does **not** mean a
"75% chance the trade wins." Buy and Sell scores are computed independently; neither is a probability.

**Real data, no fabrication.**
Signals are computed from your own genuine MT5 broker data for BTC/USD. The tool never fabricates
prices, indicator values, or market facts, and never labels tick volume as world volume.

**Analysis only — no orders.**
This tool never places, modifies, or closes real trades. It cannot execute orders on your account.

---

#### BTC/USD carries risks that gold does not — you must understand these:

**1. A 24/7 market with no "closed" safety window.**
Unlike gold — which has overnight and weekend session gaps that naturally pause the market — **BTC/USD
trades 24 hours a day, 7 days a week, including weekends and holidays.** There is **no "market closed,
come back later" period** to protect you. Price can move sharply at any hour, including while you sleep.

**2. Much higher volatility than gold.**
BTC/USD is **materially more volatile** than gold. Price swings are larger and faster; stop-losses can
be hit quickly, and slippage can be severe during fast moves.

**3. Leveraged CFD via a forex-style broker — you are trading a contract, not owning bitcoin.**
BTC/USD here is a **leveraged Contract for Difference (CFD)** offered through a forex-style broker
(e.g. Exness). You do **not** own any actual cryptocurrency. Leverage **magnifies both gains and
losses** — losses can exceed what you might expect from the price move alone, and can accumulate faster
and larger than in an unleveraged position.

**4. Overnight funding / swap charges.**
Holding a BTC/USD CFD position open overnight typically incurs **swap / funding charges** that recur for
as long as the position is held. These costs accrue continuously in a 24/7 market and can meaningfully
erode a position over time.

**5. CFD counterparty risk.**
A CFD is a contract **with your broker**, not a holding on an exchange. Your position depends on the
broker's terms, pricing, execution, and financial standing (**counterparty risk**).

---

**By continuing, you acknowledge that you have read and understood this BTC/USD disclaimer, that BTC/USD
analysis is educational decision-support only, that no profit is guaranteed, that a score is not a
win-probability, and that BTC/USD trading involves leverage, 24/7 exposure, high volatility, funding
charges, and CFD counterparty risk.**

`[ I understand and acknowledge — enable BTC/USD analysis ]`

---

## Notes for the reviewer (user) — resolved 2026-07-27

1. ~~**Approve / edit / reject** the displayed text above.~~ **RESOLVED: approved as-is, no edits.**
   This is now the authoritative BTC source text (the §40 analogue for BTC); FR-44 may be verified
   against it when that phase is built.
2. **Acknowledgement is per instrument** (FR-44 / AC-44.3): acknowledging gold does **not** acknowledge
   BTC. Under the confirmed **CONCURRENT** model (addendum §10.1), the BTC panel stays **gated/locked**
   until this is acknowledged, while gold may stream independently — this behavior is confirmed correct
   and unchanged by the approval.
3. **Jurisdiction / legal wording.** Not supplied — the approved text stands as honest/clear wording,
   not vetted legal language. If jurisdiction-specific phrasing is needed later, it can be incorporated
   as a future amendment without reopening this approval.
4. **Tone/length.** No changes requested — approved at current length.
