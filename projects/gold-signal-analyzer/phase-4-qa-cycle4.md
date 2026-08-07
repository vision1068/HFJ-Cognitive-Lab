# Cycle 4 — QA Phase (Charts & Notifications)

**Project:** gold-signal-analyzer · **Date:** 2026-07-30 · **Spec:** `cycle4-spec.md`

Verified against **actual command output**, not assumption. WPF built **explicitly**
because `dotnet test <sln>` does not build the GUI entry project (Cycle 3 lesson).

## Commands run + results
| Gate | Command | Result |
|------|---------|--------|
| Full build (incl. WPF) | `dotnet build GoldSignalAnalyzer.sln -c Release` | **0 Warning / 0 Error**, all 7 projects |
| Test suite | `dotnet test GoldSignalAnalyzer.sln -c Release` | **Passed! Failed: 0, Passed: 178** (158→178, +20) |
| Explicit WPF build | `dotnet build GoldSignalAnalyzer.Wpf/... -c Release` | **0/0**, `GoldSignalAnalyzer.Wpf.exe` produced |
| Python bridge | `python -m unittest discover -s tests -p "test_*.py"` | **Ran 19 … OK** (unregressed) |

## AC coverage (every AC has a dedicated test)
- **FR-27 charts:** AC-27.1 `Build_maps_bars_and_price_extent`,
  `Build_overlays_are_aligned_ema_series_with_warmup_nulls` (EMA cross-checked vs the
  independent `Indicators.Ema`). AC-27.2 `Geometry_maps_price_to_inverted_scaled_pixels`
  + `Overlay_polyline_skips_warmup_and_aligns_points` (hand-computed coordinates).
  AC-27.3 `Bars_flag_up_and_down_direction`. AC-27.4 `Build_empty_or_single_yields_no_fabricated_bars`
  + `Empty_data_produces_no_geometry`. AC-27.5 `Price_range_label_is_real_and_not_a_probability`.
  Plus `Flat_series_maps_to_vertical_middle...` (divide-by-zero guard) and
  `Resizing_rebuilds_geometry`.
- **FR-30 notifications:** AC-30.1 `First_actionable_signal_raises_notification`.
  AC-30.2 `Duplicate_direction_within_cooldown_is_suppressed`,
  `Same_direction_after_cooldown_raises_again`, `Opposite_direction_raises_immediately`,
  `Neutral_raises_nothing_and_leaves_dedupe_state_intact`. AC-30.3
  `Notification_labels_scores_as_0_100_not_percent`. AC-30.4 newest-first asserted in
  `Opposite_direction_raises_immediately`; `Notifier_exposes_no_order_or_execution_command`.
  AC-30.5 `Stale_data_neutral_produces_no_alert`. Plus `MarkAllRead_clears_unread_but_keeps_history`.

## Re-verified invariants (regression guard on this UI slice)
- **WPF project builds explicitly** — not inferred from the test run (Cycle 3 lesson).
- **No order/execution surface introduced** — grep over new Presentation+Wpf `.cs`/`.xaml`
  for `OrderSend|order_send|PlaceOrder|ExecuteTrade|SubmitOrder|.Buy(|.Sell(|IMt5BridgeClient`
  = **0**; `SignalNotifier`/`JournalViewModel` reflection tests confirm no trade command.
- **Disclaimer banner still intact + visible** — banner `Border` remains docked Top,
  outside the new `ScrollViewer`, so it is always on screen; `Banner_is_non_empty_and_carries_key_phrase`
  passes within the 178.
- **No fabrication** — empty/one-candle chart draws no invented bars; overlay skips
  warmup nulls; notification carries only real classification values.
- **No probability framing** — chart labels + notification labels use `(0-100)`, grep
  for `%`/`probability` in new files = 0 (doc comments only).
- **No new dependency** — 0 `PackageReference` in Presentation/Wpf csproj; Testing.dll
  absent from Wpf Release output (deps.json ref = 0).

## Defects found + fixed inside the gate
1. **Namespace/class collision** — `Indicators.EmaSeries`/`Indicators.Ema` resolved to
   the *namespace* `…Application.Indicators`, not the class, → `CS0234`. Fixed with a
   `using IndicatorMath = …Indicators.Indicators;` alias in both the builder and its test.
2. **Stale call-site** — Cycle 3's `MainViewModel` 2-arg constructor was widened to 4;
   the existing banner test's inline `new MainViewModel(...)` was updated (would not
   have compiled otherwise — caught by the full build, not just the new tests).

## QA verdict: **PASS**
All ACs covered by passing tests; build/test/python evidence is real command output;
every safety invariant re-proven on the UI slice.
