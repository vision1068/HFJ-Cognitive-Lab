using System.Reflection;
using GoldSignalAnalyzer.Application.Abstractions;
using GoldSignalAnalyzer.Application.Bridge;
using GoldSignalAnalyzer.Domain;
using GoldSignalAnalyzer.Presentation.Setup;
using Xunit;

namespace GoldSignalAnalyzer.Tests;

/// <summary>
/// Cycle 5 (FR-28) — the first-run setup wizard step machine + validation. One test per
/// AC-28.x transition/rejection case. The wizard captures NO secret (D5-3), attempts NO
/// connection (D5-5), surfaces-but-blocks the live source (D5-2), and exposes NO
/// order/execution affordance (INV-1 / NFR-SETUP-2). All exercised headlessly.
/// </summary>
public class SetupWizardViewModelTests
{
    private static IReadOnlyList<BrokerSymbol> Symbols() => new[]
    {
        new BrokerSymbol("XAUUSD", "Gold vs USD"),
        new BrokerSymbol("GOLD", "Gold spot"),
        new BrokerSymbol("XAUUSDm", "Gold micro"),
        new BrokerSymbol("EURUSD", "Euro vs USD"), // non-gold: must be filtered out
    };

    private static SetupWizardViewModel New(ISetupProfileStore? store = null)
        => new(store ?? new InMemorySetupProfileStore(), Symbols());

    private static DataSourceOption Option(SetupWizardViewModel vm, DataSourceKind kind)
        => vm.DataSourceOptions.First(o => o.Kind == kind);

    /// <summary>Drives the wizard's data to a fully-valid state (connection defaults are already valid).</summary>
    private static void DriveValid(SetupWizardViewModel vm)
    {
        vm.SelectedDataSourceOption = Option(vm, DataSourceKind.SampleDemo);
        vm.SelectedSymbol = vm.SymbolChoices[0];
        vm.AccountBalance = 10_000m;
    }

    // ---- AC-28.1: linear step machine, Next-only-when-valid, Back, CurrentError ----

    [Fact] // AC-28.1: Next advances only when the current step is valid
    public void Next_advances_only_when_current_step_is_valid()
    {
        var vm = New();
        Assert.Equal(SetupWizardViewModel.StepWelcome, vm.CurrentStepIndex);

        vm.NextCommand.Execute(null); // Welcome is always valid → DataSource
        Assert.Equal(SetupWizardViewModel.StepDataSource, vm.CurrentStepIndex);

        // Nothing selected → step invalid → Next is a no-op
        Assert.False(vm.NextCommand.CanExecute(null));
        vm.NextCommand.Execute(null);
        Assert.Equal(SetupWizardViewModel.StepDataSource, vm.CurrentStepIndex);

        vm.SelectedDataSourceOption = Option(vm, DataSourceKind.SampleDemo);
        Assert.True(vm.NextCommand.CanExecute(null));
        vm.NextCommand.Execute(null);
        Assert.Equal(SetupWizardViewModel.StepSymbol, vm.CurrentStepIndex);
    }

    [Fact] // AC-28.1: Back is disabled on the first step, enabled on every later step
    public void Back_is_disabled_on_first_step_and_enabled_thereafter()
    {
        var vm = New();
        Assert.False(vm.BackCommand.CanExecute(null));
        vm.NextCommand.Execute(null);
        Assert.True(vm.BackCommand.CanExecute(null));
        vm.BackCommand.Execute(null);
        Assert.Equal(SetupWizardViewModel.StepWelcome, vm.CurrentStepIndex);
    }

    [Fact] // AC-28.1: CurrentError exposes the current step's validation message
    public void CurrentError_exposes_the_current_step_validation_message()
    {
        var vm = New();
        Assert.Null(vm.CurrentError); // Welcome
        vm.NextCommand.Execute(null); // DataSource, nothing selected
        Assert.NotNull(vm.CurrentError);
        vm.SelectedDataSourceOption = Option(vm, DataSourceKind.SampleDemo);
        Assert.Null(vm.CurrentError);
    }

    // ---- AC-28.2: data-source selectability + live block ----

    [Fact] // AC-28.2: SampleDemo is selectable and valid with no extra input
    public void SampleDemo_data_source_is_valid()
    {
        var vm = New();
        vm.SelectedDataSourceOption = Option(vm, DataSourceKind.SampleDemo);
        Assert.Null(vm.ValidateStep(SetupWizardViewModel.StepDataSource));
    }

    [Fact] // AC-28.2: CsvFile requires a non-empty path
    public void CsvFile_requires_a_non_empty_path()
    {
        var vm = New();
        vm.SelectedDataSourceOption = Option(vm, DataSourceKind.CsvFile);
        Assert.NotNull(vm.ValidateStep(SetupWizardViewModel.StepDataSource));
        vm.CsvPath = @"C:\data\xauusd_h1.csv";
        Assert.Null(vm.ValidateStep(SetupWizardViewModel.StepDataSource));
    }

    [Fact] // Cycle 7 (AC-36.1): C-3a lifted — the live source is now SELECTABLE and read-only-valid.
    public void Selecting_Mt5Live_data_source_is_now_selectable_and_valid()
    {
        var vm = New();
        vm.NextCommand.Execute(null); // navigate to the DataSource step so CurrentError reflects it
        Assert.Equal(SetupWizardViewModel.StepDataSource, vm.CurrentStepIndex);

        var live = Option(vm, DataSourceKind.Mt5Live);
        Assert.True(live.IsSelectable); // no longer blocked (owner authorized C-3a read-only attach)

        vm.SelectedDataSourceOption = live;
        // Live needs no extra field at wizard time (Mode A default) → the step is valid.
        Assert.Null(vm.ValidateStep(SetupWizardViewModel.StepDataSource));
        Assert.Null(vm.CurrentError);
        Assert.True(vm.NextCommand.CanExecute(null)); // can advance

        // The read-only guidance is surfaced, and it commits to no-orders (INV-1 stays closed).
        Assert.Equal(SetupWizardViewModel.Mt5LiveGuidance, vm.LiveGuidance);
        Assert.Contains("read-only", vm.LiveGuidance, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("never places", vm.LiveGuidance, StringComparison.OrdinalIgnoreCase);
    }

    [Fact] // Cycle 7: a completed Mt5Live profile persists the live kind (Mode A → no login/secret).
    public void Finish_persists_Mt5Live_profile_without_secret()
    {
        var store = new InMemorySetupProfileStore();
        var vm = New(store);
        vm.SelectedDataSourceOption = Option(vm, DataSourceKind.Mt5Live);
        vm.SelectedSymbol = vm.SymbolChoices[0];
        vm.AccountBalance = 10_000m;
        Assert.True(vm.AllStepsValid);
        vm.FinishCommand.Execute(null);

        var p = store.Load()!;
        Assert.Equal(DataSourceKind.Mt5Live, p.DataSource);
        Assert.Equal(Mt5ConnectionMode.AttachExistingSession, p.ConnectionMode); // Mode A
        Assert.Null(p.AccountLogin);   // Mode A → no login
        Assert.Null(p.CredentialKey);  // no secret captured (INV-2/INV-3)
    }

    // ---- AC-28.3: symbol ranking + confidence (0..1, never a %) ----

    [Fact] // AC-28.3: choices are gold candidates ranked by real resolver confidence; non-gold excluded
    public void Symbol_choices_are_ranked_gold_candidates_by_confidence()
    {
        var vm = New();
        Assert.Equal("XAUUSD", vm.SymbolChoices[0].Raw); // exact base scores highest (1.0)
        Assert.DoesNotContain(vm.SymbolChoices, c => c.Raw == "EURUSD");
        for (int i = 1; i < vm.SymbolChoices.Count; i++)
            Assert.True(vm.SymbolChoices[i - 1].MatchConfidence >= vm.SymbolChoices[i].MatchConfidence);
    }

    [Fact] // AC-28.3: no symbol selected is invalid
    public void No_symbol_selected_is_invalid()
    {
        var vm = New();
        Assert.NotNull(vm.ValidateStep(SetupWizardViewModel.StepSymbol));
        vm.SelectedSymbol = vm.SymbolChoices[0];
        Assert.Null(vm.ValidateStep(SetupWizardViewModel.StepSymbol));
    }

    [Fact] // AC-28.3 / INV-5: confidence is a 0..1 match score, labelled, never a percentage
    public void Selected_gold_symbol_confidence_is_zero_to_one_and_not_a_percent()
    {
        var vm = New();
        var choice = vm.SymbolChoices[0];
        Assert.InRange(choice.MatchConfidence, 0.0, 1.0);
        Assert.DoesNotContain("%", choice.MatchConfidenceLabel);
        Assert.DoesNotContain("probability", choice.MatchConfidenceLabel, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("match confidence", choice.MatchConfidenceLabel);
    }

    // ---- AC-28.4: connection loopback-only, port range, Mode A/B, no connection ----

    [Fact] // AC-28.4: a non-loopback host is rejected (reusing BridgeEndpoint's invariant)
    public void Connection_rejects_non_loopback_host()
    {
        var vm = New();
        vm.EndpointHost = "8.8.8.8";
        var err = vm.ValidateStep(SetupWizardViewModel.StepConnection);
        Assert.NotNull(err);
        Assert.Contains("loopback", err, StringComparison.OrdinalIgnoreCase);
    }

    [Fact] // AC-28.4: an out-of-range port is rejected
    public void Connection_rejects_out_of_range_port()
    {
        var vm = New();
        vm.EndpointPort = 70000;
        Assert.Equal("Port must be 1..65535.", vm.ValidateStep(SetupWizardViewModel.StepConnection));
    }

    [Fact] // AC-28.4: Mode B additionally requires a non-secret login + server; Mode A requires neither
    public void ModeB_requires_login_and_server_but_ModeA_requires_neither()
    {
        var vm = New();
        // Mode A (default): loopback defaults are already valid, no login/server needed
        Assert.Null(vm.ValidateStep(SetupWizardViewModel.StepConnection));

        vm.IsModeB = true;
        Assert.NotNull(vm.ValidateStep(SetupWizardViewModel.StepConnection)); // login missing
        vm.AccountLogin = 5_123_456;
        Assert.NotNull(vm.ValidateStep(SetupWizardViewModel.StepConnection)); // server missing
        vm.ServerName = "MyBroker-Live";
        Assert.Null(vm.ValidateStep(SetupWizardViewModel.StepConnection));
    }

    // ---- AC-28.5: account balance ----

    [Fact] // AC-28.5: account balance must be > 0
    public void Account_balance_must_be_positive()
    {
        var vm = New();
        Assert.NotNull(vm.ValidateStep(SetupWizardViewModel.StepAccount)); // default 0
        vm.AccountBalance = 0m;
        Assert.NotNull(vm.ValidateStep(SetupWizardViewModel.StepAccount));
        vm.AccountBalance = 2_500m;
        Assert.Null(vm.ValidateStep(SetupWizardViewModel.StepAccount));
    }

    // ---- AC-28.6: Finish gated on all-valid; builds+persists profile; raises Completed ----

    [Fact] // AC-28.6: Finish is disabled until every step is valid
    public void Finish_is_disabled_until_all_steps_valid()
    {
        var vm = New();
        Assert.False(vm.FinishCommand.CanExecute(null));
        DriveValid(vm);
        Assert.True(vm.AllStepsValid);
        Assert.True(vm.FinishCommand.CanExecute(null));
    }

    [Fact] // AC-28.6: Finish builds an immutable profile, persists it, and raises Completed
    public void Finish_persists_profile_and_raises_Completed()
    {
        var store = new InMemorySetupProfileStore();
        var vm = New(store);
        DriveValid(vm);

        var raised = false;
        vm.Completed += (_, _) => raised = true;
        vm.FinishCommand.Execute(null);

        Assert.True(raised);
        Assert.True(store.HasProfile);
        var p = store.Load()!;
        Assert.Equal(DataSourceKind.SampleDemo, p.DataSource);
        Assert.Equal("XAUUSD", p.SymbolRaw);
        Assert.Equal("XAU/USD", p.NormalizedSymbol);
        Assert.InRange(p.SymbolConfidence, 0.0, 1.0); // the REAL detector score, not the manual 1.0
        Assert.Equal(10_000m, p.AccountBalance);
        Assert.Null(p.CredentialKey);   // no secret, and no logical key was entered
        Assert.Null(p.AccountLogin);    // Mode A → no login persisted
    }

    // ---- NFR-SETUP-2: passive surface (no execution), no bridge client ----

    [Fact] // NFR-SETUP-2 / INV-1: the wizard exposes NO trade/execution command
    public void Wizard_exposes_no_order_or_execution_command()
    {
        var members = typeof(SetupWizardViewModel).GetMembers().Select(m => m.Name.ToLowerInvariant()).ToList();
        foreach (var forbidden in new[] { "open", "close", "buy", "sell", "execute", "submit", "order", "trade", "place" })
            Assert.DoesNotContain(members, m => m.Contains(forbidden));
    }

    [Fact] // NFR-SETUP-2: the wizard holds no IMt5BridgeClient field/property (no connect seam)
    public void Wizard_holds_no_bridge_client()
    {
        var vm = typeof(SetupWizardViewModel);
        var fields = vm.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        Assert.DoesNotContain(fields, f => f.FieldType == typeof(IMt5BridgeClient));
        var props = vm.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        Assert.DoesNotContain(props, p => p.PropertyType == typeof(IMt5BridgeClient));
    }
}
