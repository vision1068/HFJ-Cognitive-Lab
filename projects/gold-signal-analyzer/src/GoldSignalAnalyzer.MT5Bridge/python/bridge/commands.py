"""
Fixed command routing table + read-only allowlist (RM2 enforcement point).

FR-38 / ADR-9. This module imports NO MetaTrader5. Routing is a dict lookup on a
FIXED table (``DISPATCH``) keyed by the request string -- there is deliberately
NO ``getattr(mt5, command)`` reflection path. An unknown or denied command misses
the table and is rejected before any gateway/handler is touched, so the SDK is
never reached on a denied path.

The invariant is proven by ``tests/test_readonly_allowlist.py`` (stdlib unittest,
no MetaTrader5, no pytest, runs on Python 3.9 and 3.14).
"""

from bridge import handlers

# Wire-protocol version. Every request must carry a matching protocolVersion.
PROTOCOL_VERSION = "1.0"

# ---------------------------------------------------------------------------
# The ALLOW set (read-only). Locked by the RM2 test against EXPECTED_READONLY_SET.
# Adding a command is a reviewed edit here AND in the test's expected set AND is
# CODEOWNERS-gated -- the allowlist cannot silently grow (Phase 2.5 cond C2).
# ---------------------------------------------------------------------------
ALLOWED_COMMANDS = frozenset({
    "health",              # heartbeat / readiness (no SDK)
    "terminal_info",       # terminal state (read)
    "account_info",        # read-only account fields (monetary suppressed, login masked)
    "symbols_get",         # FR-9 broker-variant detection
    "symbol_info",         # FR-24 contract specs
    "symbol_info_tick",    # latest tick
    "copy_rates_from_pos", # candles by position
    "copy_rates_range",    # candles by time range
})

# ---------------------------------------------------------------------------
# SDK symbols the read-only gateway is permitted to name. TIMEFRAME_* constants
# are additionally allowed by prefix (see the RM2 AST proof). Anything not here
# -- and every DENIED symbol below -- must be referenced ZERO times in the
# gateway source (AC-38.4).
# ---------------------------------------------------------------------------
ALLOWED_SDK_SYMBOLS = frozenset({
    "initialize",   # path-only attach; NO login/password/server (AC-38.5)
    "shutdown",     # lifecycle
    "last_error",   # diagnostics (read)
    "version",      # diagnostics (read)
    "terminal_info",
    "account_info",
    "symbols_get",
    "symbol_info",
    "symbol_info_tick",
    "copy_rates_from_pos",
    "copy_rates_range",
})

# ---------------------------------------------------------------------------
# The DENY set (must be structurally unreachable -- asserted zero references).
# MT5 has no order_modify/order_delete; every mutation routes through order_send
# with a TRADE_ACTION_* (the AST proof also forbids any TRADE_ACTION_* symbol).
# ``login`` is denied outright (Phase 2.5 A5) -- the bridge cannot authenticate.
# ---------------------------------------------------------------------------
DENIED_SDK_SYMBOLS = frozenset({
    "order_send",
    "order_check",
    "order_calc_margin",
    "order_calc_profit",
    "positions_get",
    "positions_total",
    "history_orders_get",
    "history_orders_total",
    "history_deals_get",
    "history_deals_total",
    "market_book_add",
    "market_book_get",
    "market_book_release",
    "login",
})

# Command-name spellings that must never appear in the allowlist / dispatch table
# (deny-disjointness assertion, AC-38.3).
DENIED_COMMAND_NAMES = frozenset({
    "order_send",
    "order_check",
    "order_calc_margin",
    "order_calc_profit",
    "positions_get",
    "positions_total",
    "history_orders_get",
    "history_deals_get",
    "market_book_add",
    "market_book_get",
    "login",
})

# ---------------------------------------------------------------------------
# The ONLY routing table. command string -> read-only handler callable.
# Keys MUST equal ALLOWED_COMMANDS (AC-38.2). No orphan handler, no unhandled
# command. Every handler takes (gateway, params) and calls only READ methods.
# ---------------------------------------------------------------------------
DISPATCH = {
    "health": handlers.handle_health,
    "terminal_info": handlers.handle_terminal_info,
    "account_info": handlers.handle_account_info,
    "symbols_get": handlers.handle_symbols_get,
    "symbol_info": handlers.handle_symbol_info,
    "symbol_info_tick": handlers.handle_symbol_info_tick,
    "copy_rates_from_pos": handlers.handle_copy_rates_from_pos,
    "copy_rates_range": handlers.handle_copy_rates_range,
}


def dispatch(command, params, gateway, audit=None):
    """Route a command through the fixed table.

    Denied/unknown commands are rejected BEFORE any handler or gateway call, so
    the SDK is never touched on a denied path. Returns a discriminated reply:
    ``{"ok": True, "data": ...}`` or ``{"ok": False, "error": <code>}``.
    """
    if command not in ALLOWED_COMMANDS:
        if audit is not None:
            audit.record(command, allowed=False, params=params)
        return {"ok": False, "error": "command_not_allowed"}

    if audit is not None:
        audit.record(command, allowed=True, params=params)

    handler = DISPATCH[command]
    try:
        data = handler(gateway, params or {})
    except Exception as exc:  # never leak the token/secrets in error text
        return {"ok": False, "error": "handler_error", "detail": type(exc).__name__}
    return {"ok": True, "data": data}
