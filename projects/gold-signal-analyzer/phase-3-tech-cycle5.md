# Cycle 5 — Technical Phase Doc (First-Run Setup Wizard, FR-28)

**Project:** gold-signal-analyzer · **Branch:** `agents/mt5-connectivity-bridge-gold-signal`
**Date:** 2026-07-31 · **Spec:** `cycle5-spec.md` · **Builds on:** Cycle 4 charts + notifications

Adds a guided **first-run setup wizard** that captures, validates and persists the app's
**non-secret** configuration (data source, gold symbol, connection settings, account
balance) and gates the dashboard on first launch. **Configuration capture only** — no live
connection (D5-5), no secret collected (D5-3), no order/execution surface anywhere
(INV-1 / NFR-SETUP-2). Same testable-VM / dumb-XAML split as Cycle 3/4; zero new NuGet packages.

## What was built

### Application — reused, no new files (FR-28, pure)
- `Bridge/BridgeEndpoint.cs` — reused verbatim for the Connection step's loopback-only
  validation (`Parse` surfaces the exact loopback/port error messages).
- `Symbols/GoldSymbolResolver.cs` — reused for the Symbol step (`RankGoldCandidates` builds
  the choice list; `SelectManually` produces the normalised gold mapping). No new scoring code.
- `Bridge/Mt5ConnectionOptions.cs` (`Mt5ConnectionMode`) — reused for Mode A / Mode B.

### Presentation — Setup (net8.0, TESTABLE — no WPF ref)
- `Setup/SetupProfile.cs` — `DataSourceKind` (`SampleDemo`/`CsvFile` selectable, `Mt5Live`
  blocked) and `SetupProfile`, an immutable `sealed record` of **non-secret** config only
  (data source, CSV path, symbol raw/normalised/confidence, connection mode, loopback
  host/port, non-secret Mode-B login/server, logical `CredentialKey` *name*, `decimal`
  `AccountBalance`). No password/token/secret/OTP member (reflection-proven).
- `Setup/ISetupProfileStore.cs` — the persistence seam + `InMemorySetupProfileStore` +
  `JsonFileSetupProfileStore` (in-framework `System.Text.Json`, no new package). `HasProfile`
  = file exists ⇒ the first-run gate fires exactly once. Mirrors `IAcknowledgementStore`
  file-for-file.
- `Setup/SetupWizardViewModel.cs` — the linear step machine
  `Welcome→DataSource→Symbol→Connection→Account→Review` with per-step validation,
  `CurrentError`, `CanGoNext`/`CanGoBack`/`AllStepsValid`, `NextCommand`/`BackCommand`/
  `FinishCommand`, and a `Completed` event. Builds an immutable `SetupProfile` on Finish and
  persists it. Also defines `DataSourceOption` (precomputed `IsSelectable`/`BlockReason`, so
  XAML needs no converter) and `SymbolChoice` (real 0..1 match confidence, labelled, never a `%`).

### WPF shell (net8.0-windows, DUMB)
- `SetupWizardWindow.xaml` (+`.cs`) — a **hidden-header `TabControl` bound to
  `CurrentStepIndex`** (TabItem headers collapsed via `ItemContainerStyle`; the selected
  step's content still renders — no custom value converter), one TabItem per step, Back/Next/
  Finish bound to the VM commands, and a red `CurrentError` line. The window closes with a
  positive result only when the VM raises `Completed`. No logic beyond binding.
- `App.xaml.cs` composition root — inserts the `NeedsSetup` gate (`JsonFileSetupProfileStore`
  → `SetupWizardWindow.ShowDialog()`) **after the disclaimer gate and before the journal/
  dashboard build**; on cancel it `Shutdown()`s without opening the dashboard. Loads the saved
  profile and feeds its `AccountBalance` into the sample analysis (captured config is really
  consumed). Adds a `SampleBrokerSymbols()` config list (names, not prices — `IsLive=false`).

## Diagram
See `phase-2-arch-cycle5.md` (data-flow with the no-execution/no-secret seam + the three forks).

## Verification evidence (Constitution §2 — test-before-claim)
```
dotnet build src/GoldSignalAnalyzer.sln -c Release
  → Build succeeded. 0 Warning(s), 0 Error(s)   (all 7 projects incl. Wpf net8.0-windows)

dotnet test  src/GoldSignalAnalyzer.sln -c Release
  → (first run) Failed: 1, Passed: 199  — a TEST defect (CurrentError asserted on the
     Welcome step, not the DataSource step); fixed in-gate, see phase-4.
  → (rerun)     Passed!  Failed: 0, Passed: 200, Skipped: 0, Total: 200   (178 → 200, +22)

dotnet build src/GoldSignalAnalyzer.Wpf/... -c Release   (explicit — test run does NOT build it)
  → Build succeeded; GoldSignalAnalyzer.Wpf.exe produced (141,312 bytes)

python -m unittest discover -s tests -p "test_*.py"   (from src/bridge)
  → Ran 19 tests … OK   (unregressed)
```
Safety checks (with manual triage of grep hits):
```
order/exec grep (OrderSend|order_send|PlaceOrder|ExecuteTrade|SubmitOrder|.Buy(|.Sell(|IMt5BridgeClient)
   over new production files → 1 hit, and it is a DOC COMMENT asserting the absence
   ("holds no <c>IMt5BridgeClient</c>"); real order surface = 0.
percent/probability grep over new setup files → 3 hits, all NEGATIVE assertions in
   comments/UI text ("never a %", "not a probability of profit"); displayed label is
   "match confidence 0.90" — no % anywhere.
PackageReference diff (Presentation/Wpf/Tests csproj) → 0 new packages; csproj files untouched.
Testing.dll in Wpf Release output → ABSENT; deps.json "GoldSignalAnalyzer.Testing" refs → 0.
```

## Net new tests this cycle: 22 xUnit (178 → 200)
`SetupWizardViewModelTests` (17: step machine, Next-only-when-valid, Back, CurrentError,
data-source selectability + exact C-3 block, symbol ranking + 0..1 confidence, loopback/port/
Mode-A/B connection, balance>0, Finish gating + persist + Completed, no-execution reflection,
no-bridge-client), `JsonFileSetupProfileStoreTests` (4: NeedsSetup gate, fresh-instance
durability with exact `12345.67890123m` decimal, no-secret-in-JSON, overwrite), plus the
`ProductionGatingTests` no-secret guard converted `[Fact]`→`[Theory]` to also cover
`SetupProfile` (+1). AC-28.x each map to a named test (see phase-4-qa-cycle5.md).

## Not built this cycle (honest status — deferred)
FR-34 (packaging/installer), OS-level toast, real-time streaming/auto-refresh, any
paper-execution ACTION surface, an in-app **Settings/edit-existing-profile** screen (re-running
setup = delete the profile file), secret/credential capture (deferred to the future C-3 connect
flow via the untouched DPAPI seam), and any live MT5 attach or "test connection" — all out of
scope. **C-3 (live MT5 attach / orders) remains gated behind the named approver, untouched.**
