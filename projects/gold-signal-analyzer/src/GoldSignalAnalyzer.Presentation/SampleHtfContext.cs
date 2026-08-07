using GoldSignalAnalyzer.Application.Scoring;
using GoldSignalAnalyzer.Domain;

namespace GoldSignalAnalyzer.Presentation;

/// <summary>
/// Cycle 9 (FR-42, D9-8): builds the <see cref="SignalContext"/> for the NON-LIVE sample
/// path with a REAL higher-timeframe confirmation, structurally identical to the live path —
/// replacing the old hardcoded <c>new SignalContext(HtfDirection: SignalDirection.Buy)</c>
/// (an unconditional pass that made the FR-21 guard meaningless in demo mode).
///
/// It steps the timeframe up the curated ladder, rebuilds the demo series at that higher
/// timeframe via <see cref="SampleCandleSeries"/>, and derives the lean with the SAME
/// <see cref="HigherTimeFrameAnalyzer"/> the live coordinator uses — so the demo exercises the
/// real confirmation logic. At the top of the ladder (D1) HTF confirmation is not applicable.
///
/// Pure and headlessly testable; it lives OUTSIDE <c>App.xaml.cs</c> so the WPF composition
/// root carries no logic (2026-07-31 / Cycle-8 rule). The demo data is explicitly NOT live
/// (INV-4) and a wall-clock recency bound is deliberately NOT applied to it (D9-11).
/// </summary>
public static class SampleHtfContext
{
    /// <summary>The sample-path signal context for the given lower timeframe, carrying a
    /// genuinely-derived <c>HtfDirection</c> (or not-applicable at D1).</summary>
    public static SignalContext For(TimeFrame lowerTimeFrame, int count = 80)
    {
        var htfTf = TimeFrameLadder.StepUp(lowerTimeFrame);
        if (htfTf is null)
            return new SignalContext(HtfConfirmationApplicable: false);

        var htfCandles = SampleCandleSeries.Build(htfTf.Value, count);
        var direction = new HigherTimeFrameAnalyzer().Confirm(NormalizedSymbol.Gold, htfTf.Value, htfCandles);
        return new SignalContext(HtfDirection: direction, HtfConfirmationApplicable: true);
    }
}
