# Cycle 2 Spec — Gold Signal Analyzer: Analytical Core (FR-11 … FR-35)

**Project:** gold-signal-analyzer
**Cycle / slice:** Analytical core — normalization → indicators → scoring → veto → explanation → risk plan → backtest → paper trading
**Branch / worktree:** `agents/mt5-connectivity-bridge-gold-signal`
**Date:** 2026-07-30
**Owner:** vision1068 (human)

> Extends Cycle 1 (`brief.md`, FR-7…FR-12 read-only MT5 bridge scaffold). Same
> independent-build / no-live-connectivity / no-order-execution constraints
> apply verbatim (S-1…S-3, INV-1…INV-3). All data-consuming logic runs against
> `CsvHistoricalMarketDataProvider` / `TestMarketDataProvider` — never fabricated
> "live" values. Spec-first per the Constitution: no implementation lands before
> its FR/AC IDs exist here.

## Permanent invariants (re-asserted, verified each phase)
- **INV-1** No order execution/modification/closing — the codebase contains **no**
  order-submission code path at all. `IMt5BridgeClient` has no trade methods.
- **INV-2** No storing of PA password / email password / OTP / withdrawal credential.
- **INV-3** No scraping of Exness/TradingView/private endpoints.
- **INV-4** No fabricated market data presented as live (`IsLive` only on a live
  seam that throws when not Connected).
- **INV-5** Every score / penalty / veto is traceable to a real calculated value
  surfaced in the explanation output — no invented facts.

---

## Phase 3 — Market Data Normalization (FR-11, FR-12 extension)

| ID | Requirement | Acceptance criteria |
|----|-------------|---------------------|
| **FR-11.3** | Build OHLCV candles from raw ticks for a requested timeframe, aligned to timeframe boundaries. | AC-11.3a Ticks aggregate into candles whose open time is the aligned bucket start. AC-11.3b OHLC computed correctly (open=first, high=max, low=min, close=last, volume=sum). AC-11.3c Empty buckets produce no candle (no fabricated bars). |
| **FR-11.4** | Aggregate lower-timeframe candles into a higher timeframe (M1→M5→M15→M30→H1→H4→D1). | AC-11.4a Target TF must be an integer multiple of source TF else reject. AC-11.4b Aggregated OHLCV correct across the bucket. AC-11.4c D1 aligned to UTC midnight; H4 to 00/04/08/12/16/20 UTC. |
| **FR-11.5** | Distinguish **completed** vs **provisional** (still-forming) candles. | AC-11.5a The current bucket that `asOf` falls inside is marked `Provisional`. AC-11.5b All earlier buckets are `Completed`. AC-11.5c A completed-only view excludes provisional bars. |
| **FR-12.4** | Multi-timeframe set exposes freshness/staleness veto already built (Cycle 1). | AC-12.4 Reuses `DataFreshnessMonitor` / `SignalGate` unchanged; exact banner `DATA STALE — SIGNAL GENERATION PAUSED`. |

## Phase 4 — Indicator Engine (FR-13, FR-14, FR-15)

| ID | Requirement | Acceptance criteria |
|----|-------------|---------------------|
| **FR-13** | Trend/momentum/volatility/volume indicators computed **locally** from candles (never from a broker "signal" feed). | AC-13.1 SMA, EMA, MACD, ADX (trend); RSI, Stochastic (momentum); ATR, Bollinger (volatility); OBV, Volume-SMA (volume) implemented. AC-13.2 Each verified against a fixed dataset with hand-computed expected output (not merely "does not throw"). |
| **FR-14** | Multi-timeframe: confirmed signals use **completed candles only**; a value derived from the still-forming candle is marked `Provisional`. | AC-14.1 Indicator computed over completed candles is `Completed`. AC-14.2 If the input series includes the provisional candle, the result is flagged `Provisional`. |
| **FR-15** | Category caps to avoid double-counting correlated indicators. | AC-15.1 Each indicator carries an `IndicatorCategory`. AC-15.2 The engine caps total contribution per category to a configured maximum, so N correlated trend indicators cannot dominate. AC-15.3 The cap applied is visible in output. |

## Phase 5 — Regime, Scoring & Veto (FR-16 … FR-25)

| ID | Requirement | Acceptance criteria |
|----|-------------|---------------------|
| **FR-16** | Independent BuyScore and SellScore, each 0–100. | AC-16.1 Scores drawn from independent evidence pools. AC-16.2 A test proves `Sell != 100 - Buy` (e.g. both low in a choppy tape). |
| **FR-17** | Regime classification (TrendingUp/Down, Ranging, Volatile) adjusting category weights. | AC-17.1 ADX/EMA-slope drive trend; ATR/BB-width drive volatile. AC-17.2 Weights differ by regime and the applied weight-set is visible in output. |
| **FR-18** | Category-weighted scoring with **visible** caps. | AC-18.1 Each contribution shows raw points, cap, capped points, weight. |
| **FR-19** | Configurable penalties, all visible in explanation. | AC-19.1 Penalties (e.g. conflicting-timeframes, wide-spread, low-volume) subtract with a named reason surfaced in output. |
| **FR-20** | Hard veto rules force Neutral. | AC-20.1 Any hard veto (stale data, spread too wide, no HTF confirmation when required) forces classification `Neutral` regardless of scores. |
| **FR-21** | Classification thresholds/guards: winning margin ≥ 15, opposite-score cap, HTF confirmation, cooldown, dedupe. | AC-21.1 Margin < 15 → Neutral. AC-21.2 Opposite score above a cap → Neutral. AC-21.3 Cooldown suppresses a repeat signal within the window. AC-21.4 Dedupe suppresses an identical consecutive signal. |
| **FR-22** | Never render a raw score as a probability. | AC-22.1 No output path formats a score as "N% probability"; scores are labelled `score (0–100)`. |
| **FR-23** | Confidence score computed **independently** of the buy/sell score. | AC-23.1 Confidence derives from agreement/regime-fit/data-quality, not from the winning score value; a test shows a high score with low confidence. |
| **FR-24** | Deterministic, data-derived explanation (no invented facts). | AC-24.1 Every explanation line maps to a real contribution/penalty/veto. AC-24.2 Same inputs → identical explanation text. |
| **FR-25** | Risk plan: entry/SL/targets/R:R/position size from real symbol specs; default 0.5% risk, min R:R 1.5, one open position. | AC-25.1 SL/targets derive from ATR and real `SymbolSpec`. AC-25.2 Position size = f(risk %, account balance, SL distance, contract size). AC-25.3 R:R below 1.5 → plan rejected (no trade). AC-25.4 Only one open position permitted at a time. |

## Phase 8 — Backtesting (FR-31)

| ID | Requirement | Acceptance criteria |
|----|-------------|---------------------|
| **FR-31** | Chronological, look-ahead-safe backtest engine with cost modelling and metrics. | AC-31.1 A test proves no future candle is ever accessible during a bar's evaluation. AC-31.2 Spread/slippage/commission applied to fills. AC-31.3 Metrics: net P/L, win rate, profit factor, max drawdown, expectancy, trade count. AC-31.4 Train/validation/out-of-sample split + walk-forward; overfitting warning when validation degrades vs train beyond a threshold. |

## Phase 9 — Paper Trading (FR-32, FR-33)

| ID | Requirement | Acceptance criteria |
|----|-------------|---------------------|
| **FR-32** | Simulated execution on Csv/Test providers only; full journal persisted. | AC-32.1 Fills simulated from provider candles (never a live order). AC-32.2 Journal captures entry/exit/size/reason/R:R/P&L/regime/confidence. AC-32.3 One open position enforced. |
| **FR-33** | Structurally incapable of submitting a real MT5 order. | AC-33.1 No code path reaches any trade method (none exist on `IMt5BridgeClient`). AC-33.2 A guard test asserts the bridge client type exposes no order/trade member. |

## `[NEEDS CLARIFICATION]`
None outstanding — Cycle 1 owner decisions (S-1…S-3) resolve the known fork
points; live attach and order execution remain deliberately absent by design.
