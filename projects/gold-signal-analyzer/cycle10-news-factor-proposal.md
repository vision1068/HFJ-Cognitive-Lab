# Proposal (not yet authorized) — Volume finding + a possible "news" factor

**Date:** 2026-08-07 · **Status:** PROPOSAL ONLY — owner has not decided; no code written.
**Owner ask:** "check the volume as well" + "is there a way to add news (Fed, Trump, Iran) as a
scoring point — if yes share details, if no tell me, I'll decide."

---

## 1. Volume — finding

`IndicatorEngine` computes **OBV** and **VOL_SMA** every run (`IndicatorEngine.cs:82-83`) and
`ScoringConfig` reserves a **Volume** category cap (10 pts) and regime weight for it
(`ScoringConfig.cs:26,33`; `ScoringEngine.cs:98`). But **no rule in `ScoringEngine` ever writes
into the Volume category** — the six scoring rules (R1-R6) only cover EMA cross, MACD, ADX/DI, RSI,
Stochastic, and Bollinger position. Volume is computed, displayed, capped, and weighted — and then
**silently contributes 0 to every signal, every timeframe, always.**

This isn't a bug (nothing crashes, nothing is fabricated) but it is dead configuration — the
`VolumeCap`/`Volume` weight fields currently do nothing. If you want volume to actually matter,
that's a small, low-risk addition (one new `Rule(...)` call in `ScoringEngine.cs`, e.g. "OBV rising
+ price rising = confirmation" or "volume below its SMA on a breakout = weak move, no points") —
happy to spec that separately from the news question below; it doesn't touch any of the app's
invariants.

## 2. Current gold-relevant news (as researched 2026-08-07)

- Spot gold ~$4,055/oz in early August 2026, up ~0.4%, moving on Iran headlines.
- **Iran:** Trump held off on fresh strikes after a weekend pause in US-Iran fighting; oil fell ~6%
  on hopes of a diplomatic deal, which eased inflation fears and pulled some safe-haven bid out of
  gold. Headlines have been swinging both ways day-to-day (deal "over" on some days, "optimism" on
  others) — this is a genuinely noisy, fast-moving input, not a stable signal.
- **Fed:** leaning **hawkish**, not toward cuts — markets were pricing ~67% odds of a **September
  hike**, not a cut, on inflation concerns. Bank of America cut its 2026 average gold forecast ~14%
  to $4,360 on this hawkish repricing.
- Net effect described in coverage: reduced geopolitical tension (bearish for gold) has been roughly
  offsetting the hawkish-Fed repricing (also bearish for gold) — i.e., two bearish forces partly
  cancelling, not a clean directional read.

Sources: [Gold drifts lower after Trump says deal with Iran 'over'](https://www.cnbc.com/2026/07/08/gold-wavers-as-investors-weigh-us-strikes-on-iran-await-fed-minutes.html) ·
[Gold rises as oil retreats on pause in U.S.-Iran strikes; Fed decision in focus](https://www.cnbc.com/2026/07/27/gold-gains-on-pause-in-us-iran-fighting-fed-decision-looms.html) ·
[Gold struggles to hold gains amid mixed US-Iran headlines, hawkish Fed](https://www.fxstreet.com/news/gold-struggles-to-hold-gains-amid-mixed-us-iran-headlines-hawkish-fed-202608031058) ·
[Gold firms as oil prices slump after Trump holds off on Iran attack](https://www.cnbc.com/amp/2026/08/03/gold-firms-as-oil-prices-slump-after-trump-holds-off-on-iran-attack.html) ·
[Gold Holds Gain as Trump Sounds Optimism Over US-Iran Talks](https://www.bloomberg.com/news/articles/2026-07-27/gold-holds-gain-as-trump-sounds-optimism-over-us-iran-talks) ·
[The Same Force That Crushed Gold All Year Just Flipped](https://goldsilver.com/industry-news/goldsilver-news/gold-price-iran-oil-drop-august-2026/)

**This is a one-time manual pull, not a live feed** — it goes stale the moment you close this
document. That fact drives the design conversation below.

## 3. Can news be added to the app? Three options, different risk levels

The app's whole design rests on invariants that a naive "news score" would break: **INV-4** (never
fabricate/label data — a headline interpretation is inherently a *judgment*, not a measured fact),
**INV-5** (no invented commentary, full traceability), and **NFR-11** (determinism — same inputs →
same output, which "what does this headline mean for gold" structurally cannot guarantee). So "can
we add news" splits into three genuinely different proposals with very different risk:

### Option A — Economic-calendar blackout veto (low risk, fits existing invariants)
Pull a real, timestamped economic calendar (FOMC decisions, CPI, NFP, scheduled Fed speakers) — not
sentiment, just **facts with timestamps**. If "now" falls inside a blackout window around a
high-impact scheduled release, veto to Neutral with a reason, exactly like the existing freshness/HTF
guards (`SignalClassifier.cs:79`, `ReasonStale`/`ReasonHtf` pattern). Deterministic (calendar times
don't change), traceable (a specific event ID drives the veto), fail-safe (no calendar data → no
blackout applied, same as HTF's fail-open-to-Neutral-only-when-required philosophy). **This is the
one I'd build first if you want anything news-related** — it's a same-shape addition to a pattern the
app already trusts, roughly a half-cycle of work (new `IEconomicCalendarProvider` seam, one new
veto rule, tests).

### Option B — Informational headline panel (low risk, doesn't touch the score)
Show a small "Recent gold-relevant headlines" panel next to the signal — pulled from a news
API/RSS, dated, sourced, read-only — **without it ever touching BuyScore/SellScore**. You read the
headlines and factor them in yourself; the deterministic technical score stays exactly as
traceable as it is today. This is the safest way to get "Fed/Iran/Trump context in the app" you
asked for, because it doesn't ask the app to *interpret* anything — it just surfaces what a human
would otherwise Google.

### Option C — News sentiment as a scoring input (high risk — do not do this casually)
An LLM or NLP model reads headlines and outputs a directional lean that adds points to Buy/Sell,
the way an indicator does today. This is a fundamentally different kind of claim than "EMA fast >
slow" — it's an *opinion* about what a headline means, not a measured fact, and it can flip
run-to-run as headlines change mid-session (breaks NFR-11 determinism) or be wrong in a way that
*looks* as authoritative as a real indicator (the exact fabrication risk INV-4/INV-5 exist to
prevent). If you want this, it needs a full spec → architecture → adversarial plan-review → audit
cycle (per the company's spec-first/secure-by-construction/blocking-gates rules) before any code —
not a quick add. My honest recommendation: **don't**, or at minimum keep it as a clearly-labeled
"advisory sentiment, not a fact" contribution capped very small, never enough on its own to flip a
Neutral into an actionable signal.

## Recommendation

Build **Option B** now if you want news in the app quickly (informational only, zero invariant
risk). Consider **Option A** as a real Cycle 10 if you want news to actually *gate* signals, in the
same deterministic/fail-safe style as the rest of the app. Skip or heavily fence **Option C** —
it's the one that can genuinely mislead you into trusting a "score" that's actually a language
model's opinion of a headline.

**Owner decision needed:** which option (A/B/C, or none) before any implementation starts.
