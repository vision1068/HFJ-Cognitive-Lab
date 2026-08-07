namespace GoldSignalAnalyzer.Presentation;

/// <summary>
/// FR-35 / CEO condition C-1: the single, audited source of the "not investment
/// advice" wording. Both the first-run acknowledgement gate and the persistent
/// banner draw from HERE — there is exactly one place to review the regulated-advice
/// language, and a test asserts the required phrases are present. Changing this text
/// is a governance-visible change.
/// </summary>
public static class DisclaimerText
{
    public const string Headline = "NOT INVESTMENT ADVICE";

    /// <summary>Short, always-visible chrome banner (FR-35.2).</summary>
    public const string Banner =
        "NOT INVESTMENT ADVICE — educational/informational tool only. All trades shown are "
        + "paper-trading simulation, never live orders. Trading gold carries a substantial risk of loss.";

    /// <summary>Full text shown at the first-run acknowledgement gate (FR-35).</summary>
    public const string Full =
        "Gold Signal Analyzer is an educational and informational tool only — it is NOT investment advice.\n\n"
        + "Nothing it displays is investment advice, a recommendation, or a solicitation to buy or "
        + "sell any financial instrument.\n\n"
        + "It does not, and cannot, place, modify, or close any live order. Every trade shown is a "
        + "simulated (paper-trading) fill on historical or test data.\n\n"
        + "Signals and scores may be derived from live market data, but this tool still never places, "
        + "modifies, or closes any order — it only observes, analyses, and displays. Live data may inform "
        + "what you see on screen; it never results in a trade being executed on your behalf.\n\n"
        + "Trading gold and other leveraged instruments carries a substantial risk of loss. Past "
        + "performance and simulated or backtested results are not indicative of future results.\n\n"
        + "Signal scores are relative internal scores on a 0-100 scale — they are not probabilities, "
        + "and not a guarantee of any outcome.\n\n"
        + "You are solely responsible for your own trading decisions. Consult a licensed financial "
        + "adviser before making any trading decision.\n\n"
        + "By continuing you acknowledge that you have read and understood this disclaimer.";
}
