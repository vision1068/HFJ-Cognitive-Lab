using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Threading;
using GoldSignalAnalyzer.Application.Analysis;
using GoldSignalAnalyzer.Application.Bridge;
using GoldSignalAnalyzer.Application.Charting;
using GoldSignalAnalyzer.Application.Freshness;
using GoldSignalAnalyzer.Application.Live;
using GoldSignalAnalyzer.Application.Scoring;
using GoldSignalAnalyzer.Application.Symbols;
using GoldSignalAnalyzer.Domain;
using GoldSignalAnalyzer.Infrastructure.Bridge;
using GoldSignalAnalyzer.Infrastructure.Persistence;
using GoldSignalAnalyzer.Infrastructure.Providers;
using GoldSignalAnalyzer.Infrastructure.Time;
using GoldSignalAnalyzer.Presentation;
using GoldSignalAnalyzer.Presentation.Setup;

namespace GoldSignalAnalyzer.Wpf;

/// <summary>
/// Composition root and the ONLY place startup wiring lives. It:
///  1. enforces the first-run disclaimer gate (FR-35) before showing the dashboard;
///  2. opens the durable SQLite paper journal;
///  3. populates the signal panel either from demo candles (Csv/Test-grade sample data)
///     OR, when the user chose the live MT5 source, from the Cycle-7 read-only live feed
///     (FR-36/FR-37) — authorized by the owner in cycle7-spec.md / phase-6-ceo-cycle7.md.
/// Nothing is ever labelled live unless it came from the live provider through the
/// freshness/veto gate (INV-4). There is no order/execution path anywhere (INV-1).
/// </summary>
public partial class App // base System.Windows.Application supplied by the XAML-generated partial
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // The disclaimer/setup gate dialogs below are shown via ShowDialog() before
        // MainWindow exists. With the default ShutdownMode (OnLastWindowClose), closing
        // whichever gate dialog happens to be the sole open window tears the
        // Application down before MainWindow.Show() runs. Stay explicit until the
        // dashboard is actually up, then hand shutdown back to the main window closing.
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        var dataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "GoldSignalAnalyzer");
        Directory.CreateDirectory(dataDir);

        // --- FR-35: first-run disclaimer gate -----------------------------------
        var ackStore = new FileAcknowledgementStore(Path.Combine(dataDir, "disclaimer.ack"));
        var disclaimerVm = new DisclaimerViewModel(ackStore);
        if (disclaimerVm.MustPrompt)
        {
            var dlg = new DisclaimerWindow(disclaimerVm);
            var ok = dlg.ShowDialog();
            if (ok != true || !ackStore.HasAcknowledged)
            {
                // Disclaimer not acknowledged — do not open the dashboard.
                Shutdown();
                return;
            }
        }

        // --- FR-28: first-run setup wizard gate --------------------------------
        // Captures/validates/persists non-secret config on first launch. No live
        // connection, no secret, no order surface (INV-1..INV-5). Runs right after
        // the disclaimer gate and before the dashboard is built.
        var setupStore = new JsonFileSetupProfileStore(Path.Combine(dataDir, "setup.json"));
        if (!setupStore.HasProfile)
        {
            var wizardVm = new SetupWizardViewModel(setupStore, SampleBrokerSymbols());
            var wiz = new SetupWizardWindow(wizardVm);
            var done = wiz.ShowDialog();
            if (done != true || !setupStore.HasProfile)
            {
                // Setup not completed — do not open the dashboard.
                Shutdown();
                return;
            }
        }
        var profile = setupStore.Load();

        // --- durable paper journal ---------------------------------------------
        var journalStore = new SqliteJournalStore(Path.Combine(dataDir, "journal.db"));
        var journalVm = new JournalViewModel(journalStore);

        // The SAME view-models are populated by either the sample path or the live
        // feed — the dashboard is source-agnostic (INV-4: nothing labelled live unless
        // it came from the live provider through the freshness/veto gate).
        var signalVm = new SignalViewModel();
        var chartVm = new ChartViewModel();
        var notifier = new SignalNotifier(SystemClock.Instance);
        var liveStatusVm = new LiveStatusViewModel();

        if (profile is not null && profile.DataSource == DataSourceKind.Mt5Live)
        {
            // --- Cycle 7 (FR-36/FR-37/FR-38): LIVE read-only feed --------------
            // FR-38: every live refresh (allowed OR suppressed) is written to a durable,
            // append-only, per-user audit trail so there is a record of what the advisory
            // surface showed and when.
            var auditLog = new JsonlFileLiveSignalAuditLog(
                Path.Combine(dataDir, "live-signal-audit.jsonl"));
            StartLiveFeed(profile, journalStore, signalVm, chartVm, notifier, liveStatusVm, auditLog);
        }
        else
        {
            // --- non-live sample path (unchanged) ------------------------------
            var candles = BuildSampleCandles();
            var analysis = new SignalAnalysisService(SystemClock.Instance).Analyze(
                NormalizedSymbol.Gold,
                TimeFrame.H1,
                candles,
                SymbolSpec.Gold(),
                accountBalance: profile?.AccountBalance ?? 10_000m,
                context: new SignalContext(HtfDirection: SignalDirection.Buy),
                hasOpenPosition: journalStore.GetOpen() is not null);

            signalVm.Load(analysis);
            chartVm.Load(new ChartSeriesBuilder().Build(candles));
            notifier.Observe(analysis);   // raises only if the classification is actionable
        }

        var mainVm = new MainViewModel(signalVm, journalVm, chartVm, notifier, liveStatusVm);

        MainWindow = new MainWindow { DataContext = mainVm };
        ShutdownMode = ShutdownMode.OnMainWindowClose;
        MainWindow.Show();
    }

    /// <summary>
    /// Cycle 7 (FR-36/FR-37): drive the dashboard from the LIVE read-only MT5 feed.
    /// It reads through the out-of-process Python bridge (loopback + token) and
    /// re-runs the existing analytical core on a timer. It NEVER fabricates: on any
    /// unsafe condition (bridge/terminal not ready, no gold mapping, stale/future data)
    /// the FR-12 veto gate suppresses the signal and the status strip shows the exact
    /// banner while the signal panel stays Neutral. There is NO order surface (INV-1)
    /// and NO trading credential is read or stored (INV-2/INV-3 — Mode A attach only).
    /// The bridge token is read from the environment (GSA_BRIDGE_TOKEN), never persisted.
    /// </summary>
    private void StartLiveFeed(
        SetupProfile profile,
        SqliteJournalStore journalStore,
        SignalViewModel signalVm,
        ChartViewModel chartVm,
        SignalNotifier notifier,
        LiveStatusViewModel liveStatusVm,
        ILiveSignalAuditLog auditLog)
    {
        liveStatusVm.MarkLive();
        signalVm.Load(null); // no signal until the gate allows one (no fabrication)

        var token = Environment.GetEnvironmentVariable("GSA_BRIDGE_TOKEN");
        if (string.IsNullOrWhiteSpace(token))
        {
            liveStatusVm.Update(NotReady(
                "Live selected, but GSA_BRIDGE_TOKEN is not set. Start MetaTrader5 (logged into your "
                + "broker), then run: python run_bridge.py --live  with GSA_BRIDGE_TOKEN set, and relaunch."));
            return;
        }

        BridgeEndpoint endpoint;
        MetaTrader5MarketDataProvider provider;
        LiveSignalCoordinator coordinator;
        NormalizedSymbol liveSymbol = NormalizedSymbol.Gold;
        const TimeFrame liveTimeFrame = TimeFrame.H1;
        try
        {
            endpoint = BridgeEndpoint.Loopback(profile.EndpointPort);
            var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var client = new HttpMt5BridgeClient(http, endpoint, token);
            provider = new MetaTrader5MarketDataProvider(client);

            var mapping = new GoldSymbolResolver().SelectManually(new BrokerSymbol(profile.SymbolRaw, null));
            provider.UseSymbolMapping(mapping);
            liveSymbol = mapping.Normalized;

            var options = new LiveSignalOptions(
                mapping.Normalized, liveTimeFrame, CandleCount: 120,
                SymbolSpec.Gold(), profile.AccountBalance, new SignalContext());
            var freshness = new DataFreshnessMonitor(SystemClock.Instance, TimeSpan.FromSeconds(90));
            coordinator = new LiveSignalCoordinator(
                provider, new SignalAnalysisService(SystemClock.Instance),
                new SignalGate(), freshness, options, hasSymbolMapping: true);
        }
        catch (Exception ex)
        {
            liveStatusVm.Update(NotReady($"Live feed could not be configured: {ex.Message}"));
            return;
        }

        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        bool busy = false;
        timer.Tick += async (_, _) =>
        {
            if (busy) return;   // never overlap a slow poll
            busy = true;
            try
            {
                if (provider.State != ConnectionState.Connected)
                    await provider.ConnectAsync();

                var result = await coordinator.RefreshAsync(
                    hasOpenPosition: journalStore.GetOpen() is not null);

                liveStatusVm.Update(result);

                // FR-38: record what this refresh showed (or why it was paused), append-only.
                auditLog.Record(LiveSignalAuditEntry.From(
                    result, liveSymbol, liveTimeFrame, SystemClock.Instance.UtcNow));

                if (result.SignalAllowed && result.Analysis is not null)
                {
                    signalVm.Load(result.Analysis);
                    notifier.Observe(result.Analysis);
                }
                else
                {
                    signalVm.Load(null); // suppressed → Neutral, never a stale/fabricated signal
                }

                // The bars are real broker data; safe to chart even while paused
                // (the status strip states the pause). Empty pull → leave prior chart.
                if (result.Candles.Count > 0)
                    chartVm.Load(new ChartSeriesBuilder().Build(result.Candles));
            }
            catch (Exception ex)
            {
                var errorResult = NotReady($"Live feed error: {ex.Message}");
                liveStatusVm.Update(errorResult);
                auditLog.Record(LiveSignalAuditEntry.From(
                    errorResult, liveSymbol, liveTimeFrame, SystemClock.Instance.UtcNow));
                signalVm.Load(null);
            }
            finally
            {
                busy = false;
            }
        };
        timer.Start();
    }

    /// <summary>A suppressed live result carrying an operator-facing reason — no data, no signal.</summary>
    private static LiveRefreshResult NotReady(string message) => new(
        new SignalGateDecision(SignalGateStatus.Suppressed, message),
        Analysis: null,
        Candles: Array.Empty<Candle>(),
        Freshness: new FreshnessAssessment(FreshnessStatus.Unknown, TimeSpan.Zero, null),
        ConnectionState: ConnectionState.Disconnected);

    /// <summary>
    /// A small, representative broker symbol list offered to the setup wizard so the
    /// user can pick their gold instrument. This is a demo/config symbol list (names,
    /// not prices) — it is NOT live market data and claims none (INV-4). The wizard
    /// ranks it with the real <c>GoldSymbolResolver</c>.
    /// </summary>
    private static IReadOnlyList<BrokerSymbol> SampleBrokerSymbols() => new[]
    {
        new BrokerSymbol("XAUUSD", "Gold vs US Dollar"),
        new BrokerSymbol("GOLD", "Gold spot"),
        new BrokerSymbol("XAUUSDm", "Gold micro"),
        new BrokerSymbol("XAUUSD.pro", "Gold (pro account)"),
        new BrokerSymbol("EURUSD", "Euro vs US Dollar"),
        new BrokerSymbol("XAGUSD", "Silver vs US Dollar"),
    };

    /// <summary>
    /// Deterministic rising sample series so the shell has something to display.
    /// This is explicitly NOT live data (the analytical core treats it as Csv/Test
    /// grade). It fabricates no "live" price and claims none.
    /// </summary>
    private static IReadOnlyList<Candle> BuildSampleCandles()
    {
        var list = new List<Candle>();
        var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        decimal basePrice = 1900m;
        for (int i = 0; i < 80; i++)
        {
            decimal open = basePrice + i * 1.5m;
            decimal close = open + 1.0m;
            decimal high = close + 0.5m;
            decimal low = open - 0.5m;
            list.Add(new Candle(
                NormalizedSymbol.Gold, TimeFrame.H1, start.AddHours(i),
                open, high, low, close, volume: 1000 + i));
        }
        return list;
    }
}
