using System.Windows.Input;
using GoldSignalAnalyzer.Application.Bridge;
using GoldSignalAnalyzer.Application.Symbols;
using GoldSignalAnalyzer.Domain;
using GoldSignalAnalyzer.Presentation.Mvvm;

namespace GoldSignalAnalyzer.Presentation.Setup;

/// <summary>A selectable data-source option; selectability is precomputed so the XAML
/// carries no value converter (NFR-SETUP-3). The live kind is listed but not selectable.</summary>
public sealed record DataSourceOption(DataSourceKind Kind, string Label, bool IsSelectable, string? BlockReason);

/// <summary>A ranked gold-symbol choice. <see cref="MatchConfidenceLabel"/> shows the real
/// <see cref="GoldSymbolResolver"/> score as an unlabelled 0..1 match confidence — never a
/// probability and never a <c>%</c> (INV-5 / AC-28.3).</summary>
public sealed record SymbolChoice(BrokerSymbol Broker, double MatchConfidence)
{
    public string Raw => Broker.Raw;
    public string MatchConfidenceLabel => $"match confidence {MatchConfidence:0.00}";
}

/// <summary>
/// FR-28: the first-run setup wizard as a linear, validated step machine
/// (Welcome → DataSource → Symbol → Connection → Account → Review). Each step validates
/// before the user may advance; <see cref="NextCommand"/> advances only when the current
/// step is valid; <see cref="FinishCommand"/> is enabled only when EVERY step is valid;
/// <see cref="CurrentError"/> exposes the current step's validation message.
///
/// All logic lives here in net8.0 Presentation (no WPF ref) so the test project exercises
/// it headlessly. The wizard collects NO secret (D5-3), attempts NO connection (D5-5), and
/// exposes NO order/execution affordance (INV-1 / NFR-SETUP-2 — reflection-proven): it has
/// no trade command and holds no <c>IMt5BridgeClient</c>. The live source is surfaced but
/// blocked (D5-2). Persistence is via <see cref="ISetupProfileStore"/>.
/// </summary>
public sealed class SetupWizardViewModel : ViewModelBase
{
    /// <summary>The exact C-3 reason surfaced when the user selects the live source (AC-28.2).</summary>
    public const string LiveBlockedReason =
        "Live MT5 data requires named-approver authorization (C-3) and is not enabled in this build.";

    public const int StepWelcome = 0;
    public const int StepDataSource = 1;
    public const int StepSymbol = 2;
    public const int StepConnection = 3;
    public const int StepAccount = 4;
    public const int StepReview = 5;
    public const int LastStep = StepReview;

    private static readonly string[] StepTitles =
    {
        "Welcome", "Data Source", "Gold Symbol", "Connection", "Account", "Review"
    };

    private readonly ISetupProfileStore _store;
    private readonly GoldSymbolResolver _resolver = new();
    private readonly RelayCommand _next;
    private readonly RelayCommand _back;
    private readonly RelayCommand _finish;

    private int _currentStepIndex;
    private string? _currentError;

    // step data ----------------------------------------------------------------
    private DataSourceOption? _selectedDataSourceOption;
    private string? _csvPath;
    private SymbolChoice? _selectedSymbol;
    private bool _isModeB;
    private string _endpointHost = "127.0.0.1";
    private int _endpointPort = 8080;
    private long? _accountLogin;
    private string? _serverName;
    private string? _credentialKey;
    private decimal _accountBalance;

    public SetupWizardViewModel(ISetupProfileStore store, IReadOnlyList<BrokerSymbol> availableSymbols)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        ArgumentNullException.ThrowIfNull(availableSymbols);

        DataSourceOptions = new[]
        {
            new DataSourceOption(DataSourceKind.SampleDemo, "Sample / demo data (built-in)", true, null),
            new DataSourceOption(DataSourceKind.CsvFile, "Historical CSV file", true, null),
            new DataSourceOption(DataSourceKind.Mt5Live, "Live MT5 terminal (blocked)", false, LiveBlockedReason),
        };

        // Only gold candidates (score > 0), ranked best-first, become choices (AC-28.3).
        SymbolChoices = _resolver.RankGoldCandidates(availableSymbols)
            .Select(c => new SymbolChoice(c.Broker, c.Score))
            .ToList();

        _next = new RelayCommand(GoNext, () => CanGoNext);
        _back = new RelayCommand(GoBack, () => CanGoBack);
        _finish = new RelayCommand(Finish, () => AllStepsValid);

        Revalidate();
    }

    // options -------------------------------------------------------------------
    public IReadOnlyList<DataSourceOption> DataSourceOptions { get; }
    public IReadOnlyList<SymbolChoice> SymbolChoices { get; }

    // navigation ----------------------------------------------------------------
    public int CurrentStepIndex
    {
        get => _currentStepIndex;
        private set { if (SetField(ref _currentStepIndex, value)) AfterStepChange(); }
    }

    public string CurrentStepTitle => StepTitles[_currentStepIndex];
    public string StepProgressLabel => $"Step {_currentStepIndex + 1} of {StepTitles.Length} — {CurrentStepTitle}";

    /// <summary>The current step's validation message, or null when the step is valid.</summary>
    public string? CurrentError
    {
        get => _currentError;
        private set => SetField(ref _currentError, value);
    }

    public bool CanGoBack => _currentStepIndex > StepWelcome;
    public bool CanGoNext => _currentStepIndex < LastStep && ValidateStep(_currentStepIndex) is null;

    /// <summary>True only when steps 1..4 are ALL valid — gates Finish (AC-28.6).</summary>
    public bool AllStepsValid =>
        ValidateStep(StepDataSource) is null &&
        ValidateStep(StepSymbol) is null &&
        ValidateStep(StepConnection) is null &&
        ValidateStep(StepAccount) is null;

    public ICommand NextCommand => _next;
    public ICommand BackCommand => _back;
    public ICommand FinishCommand => _finish;

    /// <summary>Raised once a profile is saved so the shell can close the wizard (AC-28.6).</summary>
    public event EventHandler? Completed;

    // step data (bound directly; WPF's built-in type conversion only — no custom converters)
    public DataSourceOption? SelectedDataSourceOption
    {
        get => _selectedDataSourceOption;
        set { if (SetField(ref _selectedDataSourceOption, value)) Revalidate(); }
    }

    public string? CsvPath
    {
        get => _csvPath;
        set { if (SetField(ref _csvPath, value)) Revalidate(); }
    }

    public SymbolChoice? SelectedSymbol
    {
        get => _selectedSymbol;
        set { if (SetField(ref _selectedSymbol, value)) Revalidate(); }
    }

    /// <summary>False = Mode A (attach existing session, needs nothing more);
    /// true = Mode B (configured read-only, needs a non-secret login + server).</summary>
    public bool IsModeB
    {
        get => _isModeB;
        set { if (SetField(ref _isModeB, value)) Revalidate(); }
    }

    public string EndpointHost
    {
        get => _endpointHost;
        set { if (SetField(ref _endpointHost, value)) Revalidate(); }
    }

    public int EndpointPort
    {
        get => _endpointPort;
        set { if (SetField(ref _endpointPort, value)) Revalidate(); }
    }

    public long? AccountLogin
    {
        get => _accountLogin;
        set { if (SetField(ref _accountLogin, value)) Revalidate(); }
    }

    public string? ServerName
    {
        get => _serverName;
        set { if (SetField(ref _serverName, value)) Revalidate(); }
    }

    /// <summary>Logical credential key NAME only — never a secret value (D5-3).</summary>
    public string? CredentialKey
    {
        get => _credentialKey;
        set { if (SetField(ref _credentialKey, value)) Revalidate(); }
    }

    public decimal AccountBalance
    {
        get => _accountBalance;
        set { if (SetField(ref _accountBalance, value)) Revalidate(); }
    }

    /// <summary>Human-readable review of every captured (non-secret) setting.</summary>
    public string ReviewSummary => string.Join(Environment.NewLine, new[]
    {
        $"Data source : {SelectedDataSourceOption?.Kind}"
            + (SelectedDataSourceOption?.Kind == DataSourceKind.CsvFile ? $"  ({CsvPath})" : ""),
        $"Gold symbol : {SelectedSymbol?.Raw}  ({SelectedSymbol?.MatchConfidenceLabel})",
        $"Connection  : {(IsModeB ? "Mode B (configured read-only)" : "Mode A (attach existing)")}"
            + $"  {EndpointHost}:{EndpointPort}",
        IsModeB ? $"Account     : login {AccountLogin}, server {ServerName}" : "Account     : (none needed for Mode A)",
        $"Balance     : {AccountBalance}",
    });

    // --- validation ------------------------------------------------------------

    /// <summary>Returns the validation error for a step, or null when the step is valid.</summary>
    public string? ValidateStep(int index) => index switch
    {
        StepWelcome => null,
        StepDataSource => ValidateDataSource(),
        StepSymbol => ValidateSymbol(),
        StepConnection => ValidateConnection(),
        StepAccount => ValidateAccount(),
        StepReview => AllStepsValid ? null : FirstStepError(),
        _ => null,
    };

    private string? ValidateDataSource()
    {
        if (_selectedDataSourceOption is null) return "Select a data source to continue.";
        if (!_selectedDataSourceOption.IsSelectable)
            return _selectedDataSourceOption.BlockReason ?? "This data source is not available.";
        if (_selectedDataSourceOption.Kind == DataSourceKind.CsvFile && string.IsNullOrWhiteSpace(_csvPath))
            return "A CSV file path is required for the historical CSV source.";
        return null;
    }

    private string? ValidateSymbol()
    {
        if (_selectedSymbol is null)
            return SymbolChoices.Count == 0
                ? "No gold symbol was detected in the available instruments."
                : "Select the gold instrument to analyse.";
        // Choices are built only from gold candidates (score > 0), but guard defensively.
        if (_selectedSymbol.MatchConfidence <= 0) return "The selected instrument is not a gold symbol.";
        return null;
    }

    private string? ValidateConnection()
    {
        if (_endpointPort is < 1 or > 65535) return "Port must be 1..65535.";
        try
        {
            // Reuse BridgeEndpoint's loopback-only invariant; surface its message verbatim.
            BridgeEndpoint.Parse($"http://{_endpointHost}:{_endpointPort}/");
        }
        catch (ArgumentException ex)
        {
            return ex.Message;
        }
        if (_isModeB)
        {
            if (_accountLogin is null or <= 0) return "Mode B requires a positive account login number.";
            if (string.IsNullOrWhiteSpace(_serverName)) return "Mode B requires a broker server name.";
        }
        return null; // NB: settings are validated only — no connection is attempted (D5-5).
    }

    private string? ValidateAccount()
        => _accountBalance > 0 ? null : "Account balance must be greater than zero.";

    private string? FirstStepError()
    {
        foreach (var step in new[] { StepDataSource, StepSymbol, StepConnection, StepAccount })
        {
            var err = ValidateStep(step);
            if (err is not null) return $"{StepTitles[step]}: {err}";
        }
        return null;
    }

    // --- transitions -----------------------------------------------------------

    private void GoNext()
    {
        if (!CanGoNext) return;
        CurrentStepIndex = _currentStepIndex + 1;
    }

    private void GoBack()
    {
        if (!CanGoBack) return;
        CurrentStepIndex = _currentStepIndex - 1;
    }

    private void Finish()
    {
        if (!AllStepsValid) return;

        var mapping = _resolver.SelectManually(_selectedSymbol!.Broker); // normalised gold
        var profile = new SetupProfile
        {
            DataSource = _selectedDataSourceOption!.Kind,
            CsvPath = _selectedDataSourceOption.Kind == DataSourceKind.CsvFile ? _csvPath : null,
            SymbolRaw = _selectedSymbol.Raw,
            NormalizedSymbol = mapping.Normalized.Value,
            SymbolConfidence = _selectedSymbol.MatchConfidence, // real detector score, not the manual 1.0
            ConnectionMode = _isModeB ? Mt5ConnectionMode.ConfiguredReadOnly : Mt5ConnectionMode.AttachExistingSession,
            EndpointHost = _endpointHost,
            EndpointPort = _endpointPort,
            AccountLogin = _isModeB ? _accountLogin : null,
            ServerName = _isModeB ? _serverName : null,
            CredentialKey = string.IsNullOrWhiteSpace(_credentialKey) ? null : _credentialKey,
            AccountBalance = _accountBalance,
            SavedAtUtc = DateTimeOffset.UtcNow.ToString("O"),
        };

        _store.Save(profile);
        Completed?.Invoke(this, EventArgs.Empty);
    }

    // --- change plumbing -------------------------------------------------------

    private void AfterStepChange()
    {
        OnPropertyChanged(nameof(CurrentStepTitle));
        OnPropertyChanged(nameof(StepProgressLabel));
        OnPropertyChanged(nameof(ReviewSummary));
        Revalidate();
    }

    /// <summary>Recomputes derived validation state and refreshes command availability.</summary>
    private void Revalidate()
    {
        CurrentError = ValidateStep(_currentStepIndex);
        OnPropertyChanged(nameof(CanGoBack));
        OnPropertyChanged(nameof(CanGoNext));
        OnPropertyChanged(nameof(AllStepsValid));
        OnPropertyChanged(nameof(ReviewSummary));
        _next.RaiseCanExecuteChanged();
        _back.RaiseCanExecuteChanged();
        _finish.RaiseCanExecuteChanged();
    }
}
