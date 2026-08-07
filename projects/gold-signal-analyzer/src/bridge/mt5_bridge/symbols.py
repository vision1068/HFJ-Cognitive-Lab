"""Gold symbol detection, mirroring the .NET GoldSymbolResolver (FR-9).

No hardcoded assumption of "XAUUSD": scores the discovered symbol list.
Silver/platinum/palladium (XAG/XPT/XPD) are hard-excluded.
"""

NON_GOLD_METALS = ("XAG", "XPT", "XPD", "SILVER", "PLATINUM", "PALLAD")
NORMALIZED_GOLD = "XAU/USD"


def score_gold(raw: str) -> float:
    if not raw or not raw.strip():
        return 0.0
    compact = (
        raw.upper().replace("/", "").replace(" ", "").replace("_", "").replace("-", "")
    )
    for metal in NON_GOLD_METALS:
        if metal in compact:
            return 0.0

    has_xau = "XAU" in compact
    has_gold = "GOLD" in compact
    has_usd = "USD" in compact
    if not has_xau and not has_gold:
        return 0.0

    if compact.startswith("XAUUSD"):
        score, core_len = 0.90, 6
    elif has_xau and has_usd:
        score, core_len = 0.75, 6
    elif compact.startswith("GOLD"):
        score, core_len = 0.70, 4
    elif has_gold:
        score, core_len = 0.55, 4
    else:
        return 0.0

    if compact == "XAUUSD":
        return 1.0

    extra = max(0, len(compact) - core_len)
    return max(0.0, min(1.0, score - 0.01 * extra))


def rank_gold(symbols):
    scored = [(s, score_gold(s)) for s in symbols]
    scored = [x for x in scored if x[1] > 0]
    scored.sort(key=lambda x: (-x[1], len(x[0]), x[0]))
    return scored
