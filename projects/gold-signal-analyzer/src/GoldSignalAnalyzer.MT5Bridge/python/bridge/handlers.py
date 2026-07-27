"""
Read-only request handlers (FR-38). This module imports NO MetaTrader5 and names
NO ``mt5.*`` symbol -- it also uses no reflection/exec primitive. Each handler
receives an injected ``gateway`` (the sole SDK boundary) and calls ONLY read
methods on it, which return plain JSON-ready dicts/lists. There is no
order/trade/position method to reach even if a handler were reachable off the
allowlist.

account_info additionally SUPPRESSES monetary fields and MASKS the login, so no
balance/equity/profit figure and no full account number ever crosses the wire.
"""

# Monetary / balance fields stripped from account_info before it leaves the
# bridge -- a read-only market-data tool has no need to surface money figures.
_MONETARY_FIELDS = frozenset({
    "balance", "equity", "margin", "margin_free", "margin_level",
    "margin_so_call", "margin_so_so", "margin_initial", "margin_maintenance",
    "profit", "credit", "assets", "liabilities", "commission_blocked",
})

# Account-identifier fields masked to last-2 (never the full number).
_IDENTIFIER_FIELDS = frozenset({"login"})


def _mask_account(value):
    """Render an account identifier as last-2 only (e.g. '*******47')."""
    s = "" if value is None else str(value)
    if len(s) <= 2:
        return "*" * len(s)
    return "*" * (len(s) - 2) + s[-2:]


# --- handlers ---------------------------------------------------------------
# Every handler is (gateway, params) -> JSON-ready value. Gateway methods return
# plain dicts / lists-of-dicts (the gateway normalises SDK namedtuples).

def handle_health(gateway, params):
    """Heartbeat -- no SDK, no gateway read."""
    return {"status": "ok", "protocolVersion": "1.0"}


def handle_terminal_info(gateway, params):
    return gateway.terminal_info()


def handle_account_info(gateway, params):
    raw = gateway.account_info()
    if raw is None:
        return None
    cleaned = {}
    for key, value in raw.items():
        if key in _MONETARY_FIELDS:
            continue  # suppress monetary field entirely
        if key in _IDENTIFIER_FIELDS:
            cleaned[key] = _mask_account(value)
            continue
        cleaned[key] = value
    return cleaned


def handle_symbols_get(gateway, params):
    return gateway.symbols_get(params.get("group")) or []


def handle_symbol_info(gateway, params):
    return gateway.symbol_info(params["symbol"])


def handle_symbol_info_tick(gateway, params):
    return gateway.symbol_info_tick(params["symbol"])


def handle_copy_rates_from_pos(gateway, params):
    return gateway.copy_rates_from_pos(
        params["symbol"],
        params["timeframe"],
        int(params.get("start", 0)),
        int(params["count"]),
    ) or []


def handle_copy_rates_range(gateway, params):
    return gateway.copy_rates_range(
        params["symbol"],
        params["timeframe"],
        params["date_from"],
        params["date_to"],
    ) or []
