using System.IO;
using System.Windows;
using GoldSignalAnalyzer.Application.Analysis;
using GoldSignalAnalyzer.Application.Charting;
using GoldSignalAnalyzer.Application.Scoring;
using GoldSignalAnalyzer.Domain;
using GoldSignalAnalyzer.Infrastructure.Persistence;
using GoldSignalAnalyzer.Infrastructure.Time;
using GoldSignalAnalyzer.Presentation;
using GoldSignalAnalyzer.Presentation.Setup;

namespace GoldSignalAnalyzer.Wpf;

/// <summary>
/// Composition root and the ONLY place startup wiring lives. It:
///  1. enforces the first-run disclaimer gate (FR-35) before showing the dashboard;
///  2. opens the durable SQLite paper journal;
///  3. runs the pure SignalAnalysisService over demo candles (Csv/Test-grade sample
///     data, never live — INV-4) to populate the signal panel.
/// There is no order/execution path anywhere in this shell (INV-1).
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

        // --- current signal from sample (non-live) data ------------------------
        var candles = BuildSampleCandles();
        var analysis = new SignalAnalysisService(SystemClock.Instance).Analyze(
            NormalizedSymbol.Gold,
            TimeFrame.H1,
            candles,
            SymbolSpec.Gold(),
            // Use the balance captured by the setup wizard (falls back to a sample).
            accountBalance: profile?.AccountBalance ?? 10_000m,
            // Sample higher-timeframe context is up; this is clearly-labelled demo
            // data, not a live feed.
            context: new SignalContext(HtfDirection: SignalDirection.Buy),
            hasOpenPosition: journalStore.GetOpen() is not null);

        var signalVm = new SignalViewModel();
        signalVm.Load(analysis);
        var journalVm = new JournalViewModel(journalStore);

        // --- FR-27: candle chart + EMA overlays over the SAME (non-live) candles ---
        var chartVm = new ChartViewModel();
        chartVm.Load(new ChartSeriesBuilder().Build(candles));

        // --- FR-30: in-app notification for the current actionable signal ----------
        var notifier = new SignalNotifier(SystemClock.Instance);
        notifier.Observe(analysis);   // raises only if the classification is actionable

        var mainVm = new MainViewModel(signalVm, journalVm, chartVm, notifier);

        MainWindow = new MainWindow { DataContext = mainVm };
        ShutdownMode = ShutdownMode.OnMainWindowClose;
        MainWindow.Show();
    }

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
