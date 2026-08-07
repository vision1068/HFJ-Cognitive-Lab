# Cycle 4 Spec — Gold Signal Analyzer: Charts & Notifications (FR-27, FR-30)

**Project:** gold-signal-analyzer
**Cycle / slice:** Add a price/candle **chart with indicator overlays** and an
**in-app signal notification** surface to the read-only WPF dashboard shell.
**Branch / worktree:** `agents/mt5-connectivity-bridge-gold-signal`
**Date:** 2026-07-30
**Owner:** vision1068 (human)

> Extends Cycle 1 (`brief.md`, read-only MT5 bridge scaffold), Cycle 2
> (`cycle2-spec.md`, analytical core) and Cycle 3 (`cycle3-spec.md`, UI shell).
> Same independent-build / no-live-connectivity / no-order-execution constraints
> apply verbatim (S-1…S-3, INV-1…INV-5). This slice adds **presentation only** — it
> renders values the Application layer already computes (candles + indicator series)
> and alerts on classifications the analytical core already produces. It computes
> **no new market facts** and, critically, introduces **no order/execution surface of
> any kind** (no Buy/Sell/execute affordance — the shell stays read-only display).

## Why this slice exists (business framing — CEO Phase 1)

The Cycle 3 shell shows the current signal as text + a read-only journal, but a
trader reasoning about a gold signal expects to *see the price action* the signal
was computed from, and to be *alerted* when a fresh actionable signal appears rather
than re-reading a static panel. Charts (FR-27) and notifications (FR-30) were named
in `phase-6-ceo-cycle3.md` as candidate next-slice items. This slice adds both while
keeping every safety property: it is still read-only, still paper/educational, and
still stops cleanly before the live/irreversible seam. It deliberately does **not**
add the setup wizard (FR-28), packaging (FR-34), or any execution surface.

## Permanent invariants (re-asserted, verified this phase)
- **INV-1** No order execution/modification/closing — and this slice adds **no UI
  affordance that could place or simulate placing an order**. A notification is a
  passive alert with no action button; a chart is a passive drawing.
- **INV-2 / INV-3** No stored PA/email/OTP/withdrawal credential; no scraping.
- **INV-4** No fabricated market data presented as live — the chart renders only the
  same Csv/Test-grade candles (`IsLive=false`) the analytical core consumed; an empty
  series draws nothing (no invented bars).
- **INV-5** Every drawn overlay value is a real computed indicator value from the
  existing `Indicators` calculators; every notification carries the real
  classification's direction/score/reason verbatim — no invented commentary.

## Scope decisions confirmed for this slice (binding)
- **D4-1 Charts = candle (OHLC) chart with indicator overlays**, rendered inside the
  existing WPF shell, sourced from the candle series already fed to
  `SignalAnalysisService` and the existing `Indicators` calculators. Not an
  interactive/zoomable trading terminal — a clear static snapshot chart.
- **D4-2 Notifications = IN-APP only.** A notification is raised when a **new
  actionable signal** (Buy/Sell) is classified, deduplicated so the same direction
  inside the cooldown window does not re-alert. **External channels (email / SMS /
  push) and OS-level toast are explicitly OUT of scope** — the spec history has never
  called for them (only "notifications (FR-30)" and a note that the dedupe/cooldown
  gate in `SignalClassifier` can be reused). OS toast is deferred with FR-34
  (packaging) because it needs app-identity registration; in-app keeps the slice
  dependency-free and headlessly testable.
- **D4-3 No new heavyweight dependency.** The chart is drawn with built-in WPF
  `Canvas`/`Path`/`Polyline` primitives fed by a testable geometry view-model — no
  charting library (see architect rationale). Zero new NuGet packages.

## Requirements (this slice)

| ID | Requirement | Acceptance criteria |
|----|-------------|---------------------|
| **FR-27** | A price **candle chart** of the analysed series with indicator overlays (EMA-fast / EMA-slow), rendered in the dashboard. Chart *data* prep and coordinate geometry are computed in testable layers; the XAML only draws supplied shapes. | AC-27.1 A pure Application `ChartSeriesBuilder` turns a `Candle` series into a `ChartData` (OHLC bars + named overlay series) using the existing `Indicators.EmaSeries` — no new indicator math, overlays aligned to bars (null during warmup). AC-27.2 A `ChartViewModel` (Presentation, no WPF ref) maps `ChartData` to viewport geometry: each candle → body top/bottom + wick top/bottom + X; each overlay → an ordered list of (X,Y) points skipping warmup nulls. Price→Y is inverted (higher price = smaller Y) and scaled to the supplied height; a hand-computed mapping is asserted. AC-27.3 An up candle (close>open) and a down candle (close<open) are distinguishable (`IsUp` flag). AC-27.4 Empty/one-candle input yields **no** fabricated geometry (empty bar list), never invented bars. AC-27.5 Axis labels show the real min/max price and bar count from the data, formatted invariant, never as a probability. |
| **FR-30** | An **in-app notification** raised when a new actionable signal is classified, deduplicated by direction within the classifier cooldown window; a Neutral/vetoed classification raises nothing. Notifications are passive alerts (no action button). | AC-30.1 A `SignalNotifier` (Presentation), given successive `SignalAnalysis` results and an injected `IClock`, raises a `NotificationViewModel` only for an **actionable** (Buy/Sell) classification. AC-30.2 The same direction within the cooldown window does **not** raise a duplicate; the opposite direction **does**; a Neutral/vetoed result raises nothing (dedupe mirrors the `SignalClassifier` cooldown concept, driven by `ManualClock` in tests). AC-30.3 Each notification carries the real direction, Buy/Sell score (labelled `score (0-100)`, never `%`), confidence and primary reason from the classification — no invented text. AC-30.4 The notifier exposes an ordered `Notifications` collection (newest first) + an `UnreadCount`; it has **no** trade/execute/dismiss-to-order command (INV-1). AC-30.5 A stale/`DATA STALE — SIGNAL GENERATION PAUSED` (vetoed→Neutral) classification produces no alert. |
| **NFR-UI-3** | Chart geometry + notification logic are unit-testable without a display; the XAML shell carries no logic. | `ChartSeriesBuilder` lives in Application (net8.0); `ChartViewModel`, `SignalNotifier`, `NotificationViewModel` live in the net8.0 `Presentation` project referenced by the existing net8.0 test project; a `dotnet test` exercises them headlessly. The `Wpf` shell only binds/draws. |
| **NFR-UI-4** | The chart + notification surface introduce no new secret, no network egress, and no reference to any order/bridge-trade path. | Grep/reflection: new Presentation + Wpf files contain no `OrderSend`/`order_send`/`PlaceOrder`/`ExecuteTrade`/`Buy(`/`Sell(`-action affordance and no bridge-client trade reference. No new NuGet package added (verified in csproj diff). |

## Out of scope (this slice)
- FR-28 (setup wizard), FR-34 (installer/packaging) — deferred.
- External notification channels (email/SMS/push) and OS-level toast (D4-2).
- Any live data path, live MT5 attach, or order/paper-execution **action** (C-3).
- Real-time streaming/auto-refresh — the chart and notifier operate over the
  supplied/loaded snapshot(s), consistent with Cycle 3's no-streaming tightening.
  (The notifier mechanism is fully built + tested against successive inputs; the
  live feed that would drive repeated notifications remains C-3-gated.)
- Interactive chart features (zoom/pan/crosshair/tooltips) — a static snapshot chart.

## `[NEEDS CLARIFICATION]`
None outstanding. "Charts" and "notifications" are pinned concretely above (D4-1,
D4-2) from the existing spec history — the spec has never named external channels or
an interactive terminal, so choosing in-app + a static candle chart is a documented
scope decision, not a guess. The read-only / no-execution framing is unchanged and
unambiguous. Flagged here for the CEO gate rather than assumed silently.
