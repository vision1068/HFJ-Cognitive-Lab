using GoldSignalAnalyzer.Application.Live;
using GoldSignalAnalyzer.Presentation.Mvvm;

namespace GoldSignalAnalyzer.Presentation;

/// <summary>
/// Cycle 7 (FR-37): the always-visible live-data status strip. It reflects the LIVE
/// feed's health from each <see cref="LiveRefreshResult"/> — connected/fresh vs. a
/// suppression banner (CONNECTION NOT READY / NO GOLD SYMBOL MAPPED / DATA STALE).
///
/// It is a passive read-only surface: it has NO command and NO order affordance
/// (INV-1). When the gate suppresses a signal, <see cref="IsSuppressed"/> is true and
/// <see cref="Banner"/> carries the exact reason — the UI shows this instead of a
/// (possibly stale) signal, so a paused feed can never look like an actionable call
/// (INV-4). Fully unit-testable (no WPF).
/// </summary>
public sealed class LiveStatusViewModel : ViewModelBase
{
    private bool _isLive;
    private bool _isSuppressed;
    private string _banner = "Live feed not started.";
    private string _freshness = "—";

    /// <summary>True when this session is running against the live MT5 source.</summary>
    public bool IsLive
    {
        get => _isLive;
        private set => SetField(ref _isLive, value);
    }

    /// <summary>True when the veto gate is currently pausing signal generation.</summary>
    public bool IsSuppressed
    {
        get => _isSuppressed;
        private set => SetField(ref _isSuppressed, value);
    }

    /// <summary>Operator-facing status/veto banner text.</summary>
    public string Banner
    {
        get => _banner;
        private set => SetField(ref _banner, value);
    }

    /// <summary>Human-readable freshness (e.g. "Fresh", "Stale", "Unknown").</summary>
    public string Freshness
    {
        get => _freshness;
        private set => SetField(ref _freshness, value);
    }

    /// <summary>Mark the session as live before the first refresh completes.</summary>
    public void MarkLive() { IsLive = true; Banner = "Connecting to live MT5 feed…"; }

    /// <summary>Apply the outcome of one live refresh.</summary>
    public void Update(LiveRefreshResult result)
    {
        if (result is null) throw new ArgumentNullException(nameof(result));
        IsLive = true;
        Freshness = result.Freshness.Status.ToString();
        if (result.SignalAllowed)
        {
            IsSuppressed = false;
            Banner = "LIVE — feed connected and fresh.";
        }
        else
        {
            IsSuppressed = true;
            // The exact FR-12 banner reason (never a fabricated "all good").
            Banner = result.SuppressionReason ?? "SIGNAL GENERATION PAUSED";
        }
    }
}
