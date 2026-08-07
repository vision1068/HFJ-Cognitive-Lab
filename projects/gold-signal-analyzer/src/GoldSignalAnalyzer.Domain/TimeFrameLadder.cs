namespace GoldSignalAnalyzer.Domain;

/// <summary>
/// Cycle 9 (FR-41): the curated timeframe ladder used for higher-timeframe (HTF)
/// confirmation. <see cref="StepUp"/> returns the next-higher curated timeframe, or
/// <c>null</c> at the top of the ladder (D1 — no higher curated timeframe exists) and
/// for any value not on the ladder. Pure and deterministic; introduces no new timeframe
/// (nothing above D1) so no <see cref="TimeFrame"/> enum or bridge-map change is needed.
/// </summary>
public static class TimeFrameLadder
{
    /// <summary>The curated set, ascending. Mirrors the runtime selector (Cycle 8, FR-39)
    /// and the Python bridge's supported map.</summary>
    private static readonly TimeFrame[] Ascending =
    {
        TimeFrame.M1, TimeFrame.M5, TimeFrame.M15, TimeFrame.M30,
        TimeFrame.H1, TimeFrame.H4, TimeFrame.D1
    };

    /// <summary>
    /// The next-higher curated timeframe above <paramref name="timeFrame"/>, or
    /// <c>null</c> when it is the top of the ladder (D1) or not on the ladder.
    /// A <c>null</c> result means "HTF confirmation is not applicable" (top of ladder) —
    /// the caller marks the context accordingly (see <c>SignalContext.HtfConfirmationApplicable</c>).
    /// </summary>
    public static TimeFrame? StepUp(TimeFrame timeFrame)
    {
        int i = Array.IndexOf(Ascending, timeFrame);
        if (i < 0 || i == Ascending.Length - 1) return null;
        return Ascending[i + 1];
    }
}
