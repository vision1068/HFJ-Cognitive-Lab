# Cycle 5 — QA Phase (First-Run Setup Wizard, FR-28)

**Project:** gold-signal-analyzer · **Date:** 2026-07-31 · **Spec:** `cycle5-spec.md`

Verified against **actual command output**, not assumption. WPF built **explicitly** because
`dotnet test <sln>` does not build the GUI entry project (Cycle 3 lesson). The baseline green
state (178) was independently re-run on the working tree before any code was added (Cycle 2
lesson) — it held.

## Commands run + results
| Gate | Command | Result |
|------|---------|--------|
| Baseline (pre-change) | `dotnet test src/GoldSignalAnalyzer.sln -c Release` | **Passed! 178** (confirmed green before starting) |
| Full build (incl. WPF) | `dotnet build src/GoldSignalAnalyzer.sln -c Release` | **0 Warning / 0 Error**, all 7 projects |
| Test suite | `dotnet test src/GoldSignalAnalyzer.sln -c Release` | **Passed! Failed: 0, Passed: 200** (178→200, +22) |
| Explicit WPF build | `dotnet build src/GoldSignalAnalyzer.Wpf/... -c Release` | **0/0**, `GoldSignalAnalyzer.Wpf.exe` produced (141,312 B) |
| Python bridge | `python -m unittest discover -s tests -p "test_*.py"` | **Ran 19 … OK** (unregressed) |

## AC coverage (every AC-28.x has a dedicated named test)
- **AC-28.1** linear step machine / Next-only-when-valid / Back / CurrentError →
  `Next_advances_only_when_current_step_is_valid`,
  `Back_is_disabled_on_first_step_and_enabled_thereafter`,
  `CurrentError_exposes_the_current_step_validation_message`.
- **AC-28.2** DataSource selectability + live block →
  `SampleDemo_data_source_is_valid`, `CsvFile_requires_a_non_empty_path`,
  `Selecting_Mt5Live_data_source_is_invalid_with_C3_reason` (asserts the **exact** C-3 string
  and that Next cannot advance).
- **AC-28.3** symbol ranking + confidence (0..1, never %) →
  `Symbol_choices_are_ranked_gold_candidates_by_confidence` (non-gold `EURUSD` filtered out;
  `XAUUSD` ranks first), `No_symbol_selected_is_invalid`,
  `Selected_gold_symbol_confidence_is_zero_to_one_and_not_a_percent`.
- **AC-28.4** connection loopback-only + port + Mode A/B, no connection →
  `Connection_rejects_non_loopback_host` (reuses `BridgeEndpoint`),
  `Connection_rejects_out_of_range_port`, `ModeB_requires_login_and_server_but_ModeA_requires_neither`.
- **AC-28.5** account balance > 0 → `Account_balance_must_be_positive`.
- **AC-28.6** Finish gated on all-valid; builds+persists profile; raises Completed →
  `Finish_is_disabled_until_all_steps_valid`, `Finish_persists_profile_and_raises_Completed`.
- **AC-28.7** first-run gate (`NeedsSetup` == `!HasProfile`, flips once) →
  `NeedsSetup_is_true_until_a_profile_is_saved`.
- **NFR-SETUP-1** no secret (reflection + persisted JSON) →
  `Config_records_have_no_secret_field` [Theory: `Mt5ConnectionOptions`, `SetupProfile`],
  `Persisted_profile_json_contains_no_secret_token`.
- **NFR-SETUP-2** no execution / no bridge client →
  `Wizard_exposes_no_order_or_execution_command`, `Wizard_holds_no_bridge_client`.
- **NFR-SETUP-3** durability (fresh instance, exact decimal) + no new dependency →
  `Profile_survives_store_reopen_with_every_field_intact` (asserts `12345.67890123m` exactly),
  `Save_overwrites_a_prior_profile`; package diff = 0 (see phase-3 evidence).

## Re-verified invariants (regression guard on this config slice)
- **WPF project builds explicitly** — not inferred from the test run (Cycle 3 lesson).
- **No order/execution surface introduced** — grep over new production files = 1 doc-comment
  false-positive (0 real); `SetupWizardViewModel` reflection test confirms no
  open/close/buy/sell/execute/submit/order/trade/place member and no `IMt5BridgeClient` field/property.
- **Live seam surfaced but blocked** — `Mt5Live` is listed, `IsSelectable=false`, and selecting
  it yields the exact named-approver reason and blocks `Next` (C-3 untouched, D5-2).
- **No secret collected or persisted** — `SetupProfile` reflection guard + persisted-JSON scan
  both clean; the wizard exposes no secret-valued input.
- **No fabrication / no probability framing** — symbol confidence is the real
  `GoldSymbolResolver` 0..1 score shown as `match confidence 0.90`; grep for `%`/`probability`
  in new files = 3 negative-assertion hits only.
- **Disclaimer banner still intact + visible** — the gate is inserted before the dashboard is
  built; the always-visible banner `Border` and its test (`Banner_is_non_empty_and_carries_key_phrase`)
  are unchanged and green within the 200.
- **No new dependency** — 0 `PackageReference` added; Testing.dll absent from Wpf Release output.

## Defects found + fixed inside the gate
1. **Test defect (not product):** `Selecting_Mt5Live_data_source_is_invalid_with_C3_reason`
   asserted `CurrentError == LiveBlockedReason` while the wizard was still on the **Welcome**
   step (whose validation is null by design), so `CurrentError` was null → first run showed
   **1 failed / 199 passed**. `CurrentError` correctly reflects the *current* step; the direct
   `ValidateStep(StepDataSource)` assertion already passed. Fixed by navigating to the DataSource
   step before asserting. Rerun → **200/200**. (The product behaved correctly throughout — the
   fix hardened the test, and it now also asserts `Next` cannot advance past a blocked source.)

## QA verdict: **PASS**
All ACs (AC-28.1…AC-28.7) and NFRs (NFR-SETUP-1/2/3) are covered by named, passing tests;
build/test/python evidence is real command output; every safety invariant is re-proven on this
config slice; the one defect was a test-only issue, fixed in-gate with the suite green at 200/0.
