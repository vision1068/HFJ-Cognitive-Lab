# Cycle 4 — Technical Phase Doc (Charts & Notifications)

**Project:** gold-signal-analyzer · **Branch:** `agents/mt5-connectivity-bridge-gold-signal`
**Date:** 2026-07-30 · **Spec:** `cycle4-spec.md` · **Builds on:** Cycle 3 UI shell

Adds a candle chart with EMA overlays (FR-27) and an in-app signal notifier (FR-30)
to the read-only shell. **Read-only display only** — no order/paper-execution surface
anywhere. Same testable-VM / dumb-XAML split as Cycle 3; zero new NuGet packages.

## What was built

### Application — chart data (FR-27, pure)
- `Charting/ChartSeries.cs` — `ChartBar` (OHLC projection + `IsUp`),
  `ChartOverlaySeries` (name + null-during-warmup aligned values), `ChartData`
  (bars + overlays + price Min/Max; `Empty` for no data).
- `Charting/ChartSeriesBuilder.cs` — turns a `Candle` series into `ChartData`,
  overlays = EMA-fast/EMA-slow via the **existing** `Indicators.EmaSeries` (no new
  indicator math). Empty/null input → `ChartData.Empty` (no invented bars). A
  `MaxBars` window keeps the most-recent N in chronological order.

### Presentation (net8.0, TESTABLE — no WPF ref)
- `ChartViewModel.cs` — maps `ChartData` → pixel geometry: `CandleGeometry`
  (body/wick top-bottom, X, `IsUp`, plus precomputed `BodyHeight`/`WickHeight`/`WickX`
  so the XAML binds directly — no value converters), `ChartOverlayGeometry`
  (polyline points + ready-to-bind `PointsString`). Price→Y inverted & scaled;
  flat series (range 0) maps to mid-height (no divide-by-zero); empty data draws
  nothing. `Width`/`Height` are live inputs that rebuild geometry.
- `NotificationViewModel.cs` — a passive alert record: direction, `Buy/Sell score
  (0-100)` (never `%` — FR-22), `Confidence (0-100)`, reason, regime. No command.
- `SignalNotifier.cs` — `Observe(SignalAnalysis)` raises a notification only for a
  new **actionable** signal, deduped by direction inside a cooldown window (injected
  `IClock`; default 15 min = `ClassifierConfig.Cooldown`). Neutral raises nothing and
  does not disturb dedupe state. Exposes `Notifications` (newest-first) + `UnreadCount`
  + `MarkAllRead()` — and **no** trade/execute/order command (INV-1, reflection-proven).
- `MainViewModel.cs` — gains `Chart` + `Notifier` children (still composition-only).

### WPF shell (net8.0-windows, DUMB)
- `MainWindow.xaml` — adds a chart panel (a fixed `Canvas` drawing candle
  `Rectangle`s + EMA `Polyline`s; up/down colour via a declarative `DataTrigger`,
  no code) and a notifications list panel; wrapped the body in a `ScrollViewer`.
  The persistent red **disclaimer banner stays docked Top, outside the scroll —
  always visible** (FR-35.2 intact).
- `App.xaml.cs` composition root — builds `ChartData` from the same non-live sample
  candles and feeds the notifier once from the startup analysis. No logic beyond wiring.

## Diagram
See `phase-2-arch-cycle4.md` (data-flow + the two rejected-option forks).

## Verification evidence (Constitution §2 — test-before-claim)
```
dotnet build GoldSignalAnalyzer.sln -c Release
  → Build succeeded. 0 Warning(s), 0 Error(s)   (all 7 projects incl. Wpf net8.0-windows)
dotnet test  GoldSignalAnalyzer.sln -c Release
  → Passed!  Failed: 0, Passed: 178, Skipped: 0, Total: 178   (158 → 178, +20 net)
dotnet build GoldSignalAnalyzer.Wpf/... -c Release   (explicit — test run does NOT build it)
  → Build succeeded; GoldSignalAnalyzer.Wpf.exe produced
python -m unittest discover -s tests -p "test_*.py"   (from src/bridge)
  → Ran 19 tests … OK   (unregressed)
```
Safety checks: order-surface grep over new Presentation+Wpf = 0; percent/probability
grep = 0 (only doc comments + a `ToUpperInvariant` false positive); Testing.dll absent
from Wpf Release output + deps.json ref = 0; **no PackageReference added** to
Presentation/Wpf (0/0); disclaimer banner test still green.

## Net new tests this cycle: 20 xUnit (158 → 178)
`ChartSeriesBuilderTests` (6), `ChartViewModelTests` (7), `SignalNotifierTests` (10),
minus a 3-test overlap folded in; net +20 including the updated `MainViewModel`
call-site. Chart geometry asserted against hand-computed pixel coordinates; EMA
overlay cross-checked against the independent `Indicators.Ema`; notifier dedupe proven
with a `ManualClock` (duplicate suppressed, opposite raised, Neutral inert, cooldown-expiry re-raises).

## Not built this cycle (honest status — deferred)
FR-28 (setup wizard), FR-34 (installer/packaging), external notification channels
(email/SMS/push), OS-level toast, real-time streaming/auto-refresh, and any
paper-execution ACTION surface — all out of scope. C-3 (live MT5 attach / orders)
remains gated behind the named approver, untouched.
