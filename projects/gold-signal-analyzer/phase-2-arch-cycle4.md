# Cycle 4 — Architecture Phase (Charts & Notifications)

**Project:** gold-signal-analyzer · **Branch:** `agents/mt5-connectivity-bridge-gold-signal`
**Date:** 2026-07-30 · **Spec:** `cycle4-spec.md` · **Builds on:** Cycle 3 UI shell

Two architecture forks resolved up front, both grounded in "no new heavyweight
dependency + keep the testable-VM / dumb-XAML split" (Cycle 3's proven discipline).

## Fork 1 — Charting approach (FR-27)

| Option | Verdict |
|--------|---------|
| A charting library (LiveCharts2 / OxyPlot / ScottPlot) | **Rejected.** Each is a heavyweight NuGet dep needing a network restore that may be unavailable offline (see lessons-learned: packages only present if pre-cached); pulls WPF-coupled types into what should be testable logic; and buys interactivity this slice explicitly does not need (D4-1: static snapshot chart). |
| **Built-in WPF `Canvas` + `Polyline`/`Rectangle`, fed by a testable geometry view-model** | **CHOSEN.** Zero new dependencies. The pure geometry (price→pixel mapping, candle body/wick rectangles, overlay polylines) lives in a `net8.0` `ChartViewModel` that the existing test project asserts headlessly — exactly the Cycle 3 pattern. The XAML shell only draws supplied shapes via an `ItemsControl`/`Canvas`. |

**Layering (keeps indicator math out of the view):**
```
Application/Charting/ChartSeriesBuilder (pure)   ← indicator MATH (reuses Indicators.EmaSeries)
   → ChartData (OHLC bars + named overlay series)
Presentation/ChartViewModel (pure geometry)      ← price→viewport COORDINATES, no indicator math
   → CandleGeometry[] + ChartOverlayGeometry[]   ← plain (X, Y) numbers
Wpf/MainWindow.xaml (dumb)                        ← draws Rectangles + Polylines on a Canvas
```
Overlays are **EMA-fast + EMA-slow**, taken from the already-tested
`Indicators.EmaSeries` (a full aligned `decimal?[]`, null during warmup) — so the
chart introduces **no new indicator calculation** to verify (INV-5): it re-draws
values the core already computes. Price→Y is inverted (`y = (max-price)/(max-min)*H`)
and asserted against a hand-computed mapping (AC-27.2).

## Fork 2 — Notification delivery (FR-30)

| Option | Verdict |
|--------|---------|
| Email / SMS / push | **Rejected / out of scope.** Never specced; would add secrets, network egress and a delivery dependency — contradicts INV-2 and NFR-UI-4. |
| OS-level Windows toast | **Deferred.** Needs app-identity/AppUserModelID registration that belongs with packaging (FR-34), and is untestable headlessly. |
| **In-app notification (list panel + newest-first collection)** | **CHOSEN.** A `SignalNotifier` (Presentation) fed successive `SignalAnalysis` raises a passive `NotificationViewModel` for each **new actionable** signal, deduped by direction within the classifier cooldown window (reusing the `SignalClassifier` cooldown concept, driven by an injected `IClock` so it is `ManualClock`-testable). No secret, no egress, fully headless-testable. |

**Notification is passive by construction (INV-1):** a `NotificationViewModel` is a
read-only record (timestamp, direction, score/confidence labels, reason). The
notifier exposes `Notifications` + `UnreadCount` and **no** command of any kind — so
there is structurally no button that could place, simulate, or dismiss-into a trade.

## Dedupe/cooldown placement

The dedupe lives in the notifier (a *display* concern — "should we alert the user
again"), NOT in `SignalAnalysisService`, which stays deterministic-per-call (Cycle 3
contract: a fresh `SignalClassifier` each call). The notifier keeps its own
last-alerted (direction, time) and suppresses an identical direction inside the
cooldown — mirroring `ClassifierConfig.Cooldown` semantics without mutating the
analytical core.

## What changes, by project
- **Application** (+2 files): `Charting/ChartSeries.cs` (records), `Charting/ChartSeriesBuilder.cs`.
- **Presentation** (+3 files): `ChartViewModel.cs`, `NotificationViewModel.cs`,
  `SignalNotifier.cs`; `MainViewModel` gains `Chart` + `Notifier` children.
- **Wpf** (dumb): `MainWindow.xaml` gains a chart `Canvas` panel + a notifications
  list panel; `App.xaml.cs` builds `ChartData` and feeds the notifier once from the
  startup sample analysis. No csproj/package change.
- **Tests** (+3 files): `ChartSeriesBuilderTests`, `ChartViewModelTests`, `SignalNotifierTests`.

## Diagram — data flow (read-only, no execution seam)
```mermaid
flowchart LR
    C[Candle series\n(Csv/Test, IsLive=false)] --> SB[ChartSeriesBuilder\n(Application, pure)]
    C --> SAS[SignalAnalysisService]
    SB --> CD[ChartData\nbars + EMA overlays]
    CD --> CVM[ChartViewModel\n(geometry, pure)]
    SAS --> SA[SignalAnalysis]
    SA --> NOT[SignalNotifier\n(dedupe by dir+cooldown)]
    CVM --> XAML[MainWindow Canvas\n(dumb draw)]
    NOT --> NL[Notifications list\n(passive, no action)]
    XAML -.->|no order path| X((✗ execution))
    NL -.->|no action button| X
```
No branch of this flow reaches an order/bridge-trade path — INV-1 stays structural.
