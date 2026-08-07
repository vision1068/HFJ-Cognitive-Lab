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

## Addendum — manual end-to-end run (post-gate, real product defects found + fixed)

The 200/0 xUnit result above exercises view-model and store logic headlessly; it does not render
actual XAML bindings or drive the real `App.OnStartup` composition root. Running the built
`GoldSignalAnalyzer.Wpf.exe` end-to-end (fresh profile, no `disclaimer.ack`/`setup.json`) via
Windows UI Automation surfaced **three real defects that the automated suite could not catch**,
all pure WPF plumbing bugs — the wizard's own validation/navigation logic behaved correctly at
every step once these were fixed:

1. **Crash on wizard open.** `SetupWizardWindow.xaml` bound `TabControl.SelectedIndex` to
   `CurrentStepIndex` with WPF's default `TwoWay` mode, but `CurrentStepIndex` has a `private set`
   — `ShowDialog()` threw `InvalidOperationException` immediately. Fixed: `Mode=OneWay` (the VM,
   not the TabControl, owns step navigation; headers are hidden so there is no user-driven
   `SelectedIndex` change to receive).
2. **App silently exits after the wizard (or the disclaimer alone, on a truly fresh machine) closes,
   before the dashboard ever shows.** Neither `App.xaml` nor `App.xaml.cs` set a `ShutdownMode`, so
   the default `OnLastWindowClose` tore the `Application` down the moment the sole open gate dialog
   closed — before the subsequent `MainWindow.Show()` call in `OnStartup` ran. This is a pre-existing
   gap, not new to this cycle: it would have broken the Cycle 3 disclaimer-only gate too on a
   genuinely fresh profile; it went uncaught because no prior cycle's QA ran the exe interactively
   from a clean `%LOCALAPPDATA%` state. Fixed: `ShutdownMode = OnExplicitShutdown` at the top of
   `OnStartup`, switched to `OnMainWindowClose` right before `MainWindow.Show()`.
3. **Crash when the dashboard renders an actionable notification.** `MainWindow.xaml`'s
   `<Run Text="{Binding Headline}"/>` / `Time` bindings (Cycle 4) hit the same read-only-property
   trap as #1 — `Run.Text` also binds `TwoWay` by default, and `NotificationViewModel.Headline`/
   `Time` are get-only. Fixed: `Mode=OneWay` on both. This was a **Cycle 4 defect**, latent until
   this cycle's manual run was the first to actually reach the dashboard with an actionable signal.

All three were reproduced, fixed, and **re-verified by manually driving the full fresh-install
flow twice** (disclaimer → wizard Welcome→DataSource(Mt5Live confirmed blocked, SampleDemo
selected)→Symbol(XAUUSD ranked 1.00)→Connection(loopback validated live, non-loopback rejected
live)→Account→Review→Finish → dashboard renders signal/chart/notification/journal correctly →
close → relaunch → both gates correctly skipped straight to the dashboard). `dotnet test` was
rerun after all three fixes: still **200/0**. No AC, NFR, or invariant required a behavior change —
these were binding-mode/shutdown-mode defects orthogonal to the wizard's design.

**Process gap to carry forward:** add a manual "run the actual exe from a clean profile" pass to
the QA gate for any cycle touching `.xaml` bindings or `App.OnStartup`, since headless xUnit tests
structurally cannot exercise XAML binding-mode or `Application.ShutdownMode` behavior. Recorded in
`.claude/memory/lessons-learned.md`.

## QA verdict: **PASS**
All ACs (AC-28.1…AC-28.7) and NFRs (NFR-SETUP-1/2/3) are covered by named, passing tests;
build/test/python evidence is real command output; every safety invariant is re-proven on this
config slice; the one xUnit-suite defect was a test-only issue, fixed in-gate with the suite green
at 200/0. **Three additional real WPF defects** (binding-mode + shutdown-mode) were found and fixed
via manual end-to-end execution of the built exe, per the addendum above — the shipped app now
actually completes the fresh-install flow end-to-end, confirmed by direct interactive verification,
not build success alone.
