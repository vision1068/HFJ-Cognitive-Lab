# Cycle 9 — Phase 4 QA (Independent Verification)

**Date:** 2026-08-05 · **Verdict: PASS**

Independent re-run (not implementer self-claims):

| Command | Claimed | Reproduced |
|---|---|---|
| `dotnet test -c Release` (from `src`) | 265 / 0 | **265 passed, 0 failed, 0 skipped** |
| `dotnet build GoldSignalAnalyzer.Wpf.csproj -c Release` | 0/0 | **0 Warning, 0 Error** |
| `python -m unittest discover -s tests` (from `src/bridge`) | 32 OK | **Ran 32, OK** |

Structural (independently grepped): production-C# order verbs = 0; `#if DEBUG`/`[Conditional]` = 0;
`GoldSignalAnalyzer.Testing.dll` absent from WPF Release output.

## Reported bug genuinely closed
- `Live_refresh_confirms_with_real_htf` — bullish LTF(H1)+bullish HTF(H4) → **actionable Buy**, and
  asserts `RequestedTimeFrames == [H1, H4]` (two separate pulls at different timeframes).
- `Live_refresh_blocks_on_conflicting_htf` — bullish LTF(H1)+**bearish** HTF(H4) → Neutral/ReasonHtf,
  `Htf.Direction==Sell`. Only constructible because per-timeframe candles are wired (IC-1) — **not
  hollow**. Directly refutes the never-populated-null live defect.

## Every Plan-Review condition implemented AND tested (non-hollow)
IC-1 per-tf provider + ordered recording · IC-2/D9-10 warmup fail-safe (n=8→null, n=50→Buy) ·
IC-3/AC-42.5 ordered `[H1,H4]` snapshot proof after mid-poll switch · IC-4 per-rung theory
(false only at D1) + coordinator D1-no-op (`RequestedTimeFrames==[D1]`, no HTF pull) · AC-42.4
throw→Neutral/ReasonHtf, SignalAllowed stays true, no exception · D9-11 frozen HTF→unavailable ·
IC-8 HTF pull never feeds freshness · FR-43 three audit states + null/null on suppression.
AC-41.2 independent-lean assertion genuinely recomputes indicators→regime→score and asserts
`actual == sign(BuyScore−SellScore)`. AC-42.1 both-directions theory.

## No regression
`ScoringAndVetoTests.Htf_not_confirmed_is_neutral` (HtfDirection null) stays Neutral/ReasonHtf under
the new `HtfConfirmationApplicable=true` default. Field appended last (positional-arg safe).

## Minor findings (naming only — NOT blocking)
1. AC-41.1 split into `StepUp_maps_each_rung` (theory) + `StepUp_returns_null_at_top_and_for_unknown`
   (incl. `(TimeFrame)999→null`). Identical coverage.
2. IC-4's named `Live_refresh_below_top_keeps_htf_guard_active` does not exist by that name; the
   behaviour is covered by `Live_refresh_blocks_on_conflicting_htf`. Reconciled in cycle9-spec.md.
