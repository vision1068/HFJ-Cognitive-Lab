using GoldSignalAnalyzer.Domain;

namespace GoldSignalAnalyzer.Application.Symbols;

/// <summary>Broker symbol scored as a gold candidate.</summary>
public sealed record SymbolCandidate(BrokerSymbol Broker, double Score);

/// <summary>
/// FR-9: detects the broker's gold instrument across naming variants
/// (XAUUSD, GOLD, XAUUSDm, XAUUSD.a/.c/.pro/.raw/.ecn, micro, …) and maps the
/// chosen one to the normalized <c>XAU/USD</c>. There is NO hardcoded
/// assumption that the symbol is literally "XAUUSD" — detection is purely by
/// scoring the discovered symbol list, and the user can always override.
/// Pure calculation, no I/O — unit-testable without a terminal (NFR-10).
/// </summary>
public sealed class GoldSymbolResolver
{
    /// <summary>Instruments that contain USD but are NOT gold — hard-excluded
    /// so silver/platinum/palladium can never be misdetected as gold.</summary>
    private static readonly string[] NonGoldMetals = { "XAG", "XPT", "XPD", "SILVER", "PLATINUM", "PALLAD" };

    /// <summary>
    /// Scores a single raw broker symbol as a gold candidate on 0..1.
    /// 0 means "not gold". Higher is a better/cleaner gold match.
    /// </summary>
    public static double ScoreGold(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return 0;

        var compact = raw.ToUpperInvariant()
                         .Replace("/", "").Replace(" ", "").Replace("_", "").Replace("-", "");

        // Silver/platinum/palladium are never gold, even though they carry USD.
        foreach (var metal in NonGoldMetals)
            if (compact.Contains(metal)) return 0;

        bool hasXau = compact.Contains("XAU");
        bool hasGold = compact.Contains("GOLD");
        bool hasUsd = compact.Contains("USD");

        if (!hasXau && !hasGold) return 0;

        double score;
        int coreLen;
        if (compact.StartsWith("XAUUSD"))
        {
            score = 0.90; coreLen = 6;
        }
        else if (hasXau && hasUsd)
        {
            score = 0.75; coreLen = 6;
        }
        else if (compact.StartsWith("GOLD"))
        {
            score = 0.70; coreLen = 4;
        }
        else if (hasGold)
        {
            score = 0.55; coreLen = 4;
        }
        else
        {
            return 0;
        }

        // Exact, suffix-free base name is the strongest possible signal.
        if (compact == "XAUUSD") return 1.0;

        // Each extra character beyond the recognized core is a small, deterministic
        // penalty so a cleaner symbol outranks a suffixed one (XAUUSD > XAUUSDm).
        int extra = Math.Max(0, compact.Length - coreLen);
        score -= 0.01 * extra;
        return Math.Clamp(score, 0.0, 1.0);
    }

    /// <summary>All symbols that score above zero, ranked best-first.</summary>
    public IReadOnlyList<SymbolCandidate> RankGoldCandidates(IEnumerable<BrokerSymbol> symbols)
    {
        ArgumentNullException.ThrowIfNull(symbols);
        return symbols
            .Select(s => new SymbolCandidate(s, ScoreGold(s.Raw)))
            .Where(c => c.Score > 0)
            .OrderByDescending(c => c.Score)
            .ThenBy(c => c.Broker.Raw.Length)
            .ThenBy(c => c.Broker.Raw, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Auto-selects the best gold symbol if one clears <paramref name="minConfidence"/>,
    /// else null (caller must then ask the user to pick — FR-9).
    /// </summary>
    public SymbolMapping? AutoDetect(IEnumerable<BrokerSymbol> symbols, double minConfidence = 0.50)
    {
        var best = RankGoldCandidates(symbols).FirstOrDefault();
        if (best is null || best.Score < minConfidence) return null;
        return new SymbolMapping(best.Broker, NormalizedSymbol.Gold, SymbolMappingSource.AutoDetected, best.Score);
    }

    /// <summary>
    /// User explicitly maps a chosen broker symbol to normalized gold (FR-9).
    /// Confidence is 1.0 — the human overrides the detector.
    /// </summary>
    public SymbolMapping SelectManually(BrokerSymbol chosen)
    {
        ArgumentNullException.ThrowIfNull(chosen);
        return new SymbolMapping(chosen, NormalizedSymbol.Gold, SymbolMappingSource.ManualSelection, 1.0);
    }
}
