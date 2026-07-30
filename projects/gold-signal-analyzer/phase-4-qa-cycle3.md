# Cycle 3 — QA Phase (UI / Dashboard Shell)

**Project:** gold-signal-analyzer · **Date:** 2026-07-30

## Verdict: **PASS** — verified by command output, not assertion

All 158 xUnit + 19 Python tests green in Release; full solution (incl. the WPF app)
builds 0/0. Every Cycle-3 acceptance criterion has a dedicated test.

## Coverage of acceptance criteria

| AC | Test | Result |
|----|------|--------|
| AC-26.1 signal display | `Facade_rising_series_produces_actionable_buy`, `SignalVm_exposes_direction_scores_and_explanation` | ✅ |
| AC-26.2 no %/probability (FR-22) | `SignalVm_never_formats_score_as_percent_or_probability` (+ source grep = 0) | ✅ |
| AC-26.3 stale→Neutral w/ exact banner | `Facade_stale_data_forces_neutral_with_exact_banner` (`DATA STALE — SIGNAL GENERATION PAUSED`) | ✅ |
| AC-26.4 risk plan only when actionable/tradeable | `Facade_actionable_signal_has_tradeable_risk_plan`, `SignalVm_neutral_shows_reject_reason_not_levels` | ✅ |
| AC-26.2a/d journal rows + closed-only total | `JournalVm_loads_rows_and_totals_closed_pnl_only` | ✅ |
| AC-26.2b no mutation/trade command | `JournalVm_exposes_no_order_or_mutation_command` (reflection) | ✅ |
| AC-26.2c open trade blank exit/P&L | `JournalRow_open_trade_has_blank_exit_and_pnl` | ✅ |
| AC-35.1 disclaimer wording | `DisclaimerText_contains_required_regulated_advice_phrases` | ✅ |
| AC-35.2 gate blocks until ack | `Disclaimer_gate_blocks_until_acknowledged` | ✅ |
| AC-35.2a banner present | `Banner_is_non_empty_and_carries_key_phrase` | ✅ |
| AC-35.3 persistence across fresh store | `Disclaimer_acknowledgement_persists_across_fresh_store` | ✅ |
| empty/degenerate inputs | `Facade_rejects_empty_candles`, `SignalVm_empty_state_is_safe_placeholder` | ✅ |

## Findings surfaced and resolved during the gate (real, not cosmetic)
1. **Disclaimer text lacked the literal phrase "not investment advice"** (it read
   "…is investment advice" negated by context). Fixed the wording to carry the exact
   phrase rather than weaken the test. Re-ran: green.
2. **`dotnet test <sln>` did NOT build the WPF app** (the exe is not a dependency of
   the test project), masking a real compile error (`Application` identifier collided
   with the `GoldSignalAnalyzer.Application` namespace). Caught only by an explicit
   `dotnet build` of the WPF project. Fixed and re-verified. → **QA rule for this repo:
   the WPF/entry project must be built explicitly; a green `dotnet test` is not proof
   the app compiles.**

## Not covered (out of scope / honest)
No automated rendering/screenshot test of the live WPF window (no logic lives there;
VMs are fully tested). Interactive charts, wizard, notifications, packaging — deferred.
