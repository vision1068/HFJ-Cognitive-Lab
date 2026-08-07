# Phase 4 — QA Gate — Cycle 8 (Runtime Timeframe Selector)

**Verdict: PASS.**

Adversarial re-run of the full suite against the actual working tree (baseline re-confirmed
before any code was written — not trusting the Cycle-7 phase doc's "218 green" claim), plus the
new FR-39/FR-40 tests. Every command below was executed this cycle.

## Test evidence (real command output)

| Suite | Command | Result |
|-------|---------|--------|
| C# unit/integration (Release) | `dotnet test GoldSignalAnalyzer.sln -c Release` | **Failed: 0, Passed: 230, Skipped: 0** |
| WPF entry build (Release) | `dotnet build GoldSignalAnalyzer.Wpf.csproj -c Release` | **Build succeeded. 0 Warning(s), 0 Error(s)** |
| Python bridge | `python -m unittest discover -s tests` | **Ran 32 tests … OK** |

Baseline independently re-confirmed at **218** C# on the working tree before starting; **+12
net-new** FR-39/FR-40 tests → **230**. Python is unchanged at **32** — no bridge change was
needed, because `market_source.py`'s `_TIMEFRAME_ATTR_BY_MINUTES` already maps every curated
timeframe (M1…D1). The GUI project was built explicitly (standing lesson: `dotnet test` does not
build the WinExe entry project).

## What was tested (traceability)

- **FR-39** (selector + sample recalc):
  - `Selector_offers_curated_set_default_h1` (AC-39.1) — seven choices in order, default H1, labels are enum names (not `%`).
  - `Selecting_new_timeframe_raises_changed_once` + `Selecting_same_timeframe_is_noop` (AC-39.2) — event fires once on a real change, never on a no-op.
  - `Sample_series_uses_selected_timeframe_spacing_and_label` (AC-39.3, Theory over M1/H1/D1) — candles carry the selected timeframe as both label and timestamp spacing (1 / 60 / 1440 min).
  - `Analyze_runs_for_every_curated_timeframe` (AC-39.4) — `SignalAnalysisService.Analyze` yields a real signal for all seven timeframes.
  - `Selector_exposes_no_order_member` (AC-39.5) — reflection: no order/command affordance.
- **FR-40** (live switch through the gate):
  - `SetTimeFrame_repulls_candles_at_new_timeframe` (AC-40.1) — after `SetTimeFrame(D1)`, the next `RefreshAsync` requests candles at D1 (`TestMarketDataProvider.LastRequestedTimeFrame == D1`) and still analyses.
  - `SetTimeFrame_does_not_bypass_stale_suppression` (AC-40.2) — a stale feed still suppresses (null analysis, exact `DATA STALE` banner) after a switch; the switch did re-pull at the new TF, then vetoed.
  - `Refresh_uses_timeframe_snapshotted_at_start` (AC-40.3) — the load-bearing INV-4 test: a `SetTimeFrame` fired **while a poll is mid-flight** (gated provider) does not change the timeframe that poll pulled — it stays H1 — while `CurrentTimeFrame` advances to D1 for the next poll.
  - `MarkRecalculating_shows_paused_recalc_banner` (AC-40.4) — the live strip shows a **suppressed** "Recalculating at {TF}" banner during the switch gap.
- **Regression:** all Cycle-1..7 tests (including `LiveSignalCoordinatorTests`, `LiveStatusViewModelTests`, `LiveSignalAuditLogTests`, the no-secret and Testing.dll-absent guards) still green — the coordinator's `_options` change and the new members did not regress the Cycle-7 reflection guard (`settimeframe`/`currenttimeframe` contain no forbidden substring).

## Edge cases explicitly covered

- **No-op selection** raises no event (no spurious recompute).
- **Mid-poll timeframe switch** (the core INV-4 risk): the in-flight poll's result is bound to
  the timeframe it started with (snapshot), and the composition root additionally **discards** a
  superseded poll rather than painting/auditing it under the wrong label.
- **Switch never bypasses suppression** — freshness is tick-recency-keyed, proven unaffected by a
  timeframe change (a 3h-old tick still vetoes after `SetTimeFrame`).
- **Every curated timeframe** is exercised through the analytical core (not just H1), so a D1 or
  M1 selection cannot silently throw.

## Structural gates (not just green tests)

- Order/execution verbs in production C#/Python: **0**.
- `#if DEBUG` / `[Conditional]` anywhere in production: **0** (Debug/Release safety parity).
- `GoldSignalAnalyzer.Testing.dll` in WPF Release output: **ABSENT** (assembly-graph gate intact).
- New `PackageReference` this cycle: **0**. `SetupProfile` and its persisted JSON: **unchanged**
  (the selector adds no persisted state — D8-6).

## Not covered here (routed / deferred, not hidden)

- **Manual wiped-profile launch of the built exe** exercising the ComboBox end-to-end (select
  D1 → signal/chart repaint; live session → strip shows "Recalculating" then the new-TF result).
  This is exactly the class of check the 2026-07-31 WPF lesson says headless tests **cannot**
  reach (ComboBox binding-mode behaviour, `App.OnStartup` composition). The new XAML binds
  `SelectedValue` TwoWay to a genuinely settable property (correct) and read-only labels
  `Mode=OneWay` (lesson honoured), but a manual run is still required. **Owner manual step.**
- **Real MT5 terminal E2E** of a live timeframe switch — no MT5/broker in this environment.
  Validated via injected-fake tests only. **Owner manual step** (carried from Cycle 7).
