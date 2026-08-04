"""Market-data sources for the bridge.

MarketDataSource is the seam. Two implementations:
  * FakeMarketDataSource — deterministic, used by tests and demos. Explicitly
    NOT live (is_live == False) so its data can never be mistaken for a quote.
  * Mt5MarketDataSource — the real terminal seam (Cycle 7, FR-7/FR-36). It reads
    from an already-running MetaTrader5 terminal that the USER has logged into
    their broker (e.g. Exness demo) — "Mode A / attach existing session". It is
    strictly READ-ONLY: it calls only initialize / terminal_info / symbols_get /
    symbol_select / symbol_info_tick / copy_rates_from_pos. It exposes and calls
    NO order function (no order_send / order_check / positions_*) — INV-1. It
    never fabricates data (NFR-5): if the terminal is not connected it raises or
    returns None rather than inventing a price.

The MetaTrader5 module is INJECTED (constructor arg) so the whole live source is
unit-testable with a fake module — proving the candle/tick conversion and the
read-only contract without a real terminal (the real-terminal E2E is the owner's
manual step, since Claude Code cannot install/log-in a live terminal).
"""
from __future__ import annotations

from abc import ABC, abstractmethod
from datetime import datetime, timezone


class MarketDataSource(ABC):
    is_live: bool = False

    @abstractmethod
    def terminal_connected(self) -> bool: ...

    @abstractmethod
    def list_symbols(self): ...

    @abstractmethod
    def get_tick(self, broker_symbol: str): ...

    @abstractmethod
    def get_candles(self, broker_symbol: str, timeframe_minutes: int, count: int): ...


class FakeMarketDataSource(MarketDataSource):
    is_live = False

    def __init__(self, symbols=None, tick=None, candles=None, connected=True):
        self._symbols = symbols or [{"raw": "XAUUSD", "description": "Gold vs USD"}]
        self._tick = tick
        self._candles = candles or []
        self._connected = connected

    def terminal_connected(self) -> bool:
        return self._connected

    def list_symbols(self):
        return list(self._symbols)

    def get_tick(self, broker_symbol: str):
        return self._tick

    def get_candles(self, broker_symbol: str, timeframe_minutes: int, count: int):
        return list(self._candles[-count:])


# Minutes-per-candle -> the MetaTrader5 module's TIMEFRAME_* constant NAME.
# Resolved against the (injected) module so tests can supply a fake module.
# Mirrors GoldSignalAnalyzer.Domain.TimeFrame (M1/M5/M15/M30/H1/H4/D1).
_TIMEFRAME_ATTR_BY_MINUTES = {
    1: "TIMEFRAME_M1",
    5: "TIMEFRAME_M5",
    15: "TIMEFRAME_M15",
    30: "TIMEFRAME_M30",
    60: "TIMEFRAME_H1",
    240: "TIMEFRAME_H4",
    1440: "TIMEFRAME_D1",
}


def _epoch_to_iso_utc(epoch_seconds) -> str:
    """MT5 timestamps are POSIX seconds in UTC. Emit ISO-8601 with a 'Z' suffix
    so the .NET client parses it as universal (NFR-5: no ambiguous local time)."""
    dt = datetime.fromtimestamp(int(epoch_seconds), tz=timezone.utc)
    return dt.isoformat().replace("+00:00", "Z")


def _field(row, name):
    """Read a named field from a copy_rates row. Works for a numpy structured
    array row (row["time"]) and for a plain dict/mapping (fake module in tests)."""
    try:
        return row[name]
    except (KeyError, IndexError, TypeError):
        return getattr(row, name)


class Mt5MarketDataSource(MarketDataSource):
    """Read-only live terminal seam (Cycle 7, FR-36). Attaches to an
    already-logged-in terminal (Mode A) and reads market data only.

    Deliberately exposes NO trading/order method. The injected `mt5` module is
    used only for: initialize, terminal_info, symbols_get, symbol_select,
    symbol_info_tick, copy_rates_from_pos. NFR-5: raises/returns-None instead of
    fabricating when the terminal is unavailable."""
    is_live = True

    def __init__(self, mt5=None):
        if mt5 is None:  # pragma: no cover - import path exercised only on a live host
            try:
                import MetaTrader5 as mt5  # noqa: F811  (optional, Windows + terminal only)
            except ImportError as exc:
                raise RuntimeError(
                    "MetaTrader5 package not available. Install it (pip install MetaTrader5) "
                    "and run on Windows with an MT5 terminal already logged into your broker. "
                    "This bridge is READ-ONLY."
                ) from exc
        self._mt5 = mt5
        self._initialized = False

    def initialize(self) -> None:
        """Mode A attach: connect to the running terminal WITHOUT credentials.
        The user must already be logged into their broker (e.g. Exness demo) in
        the MT5 terminal. No login/password is passed — INV-2/INV-3 (the bridge
        stores and transmits no trading credential)."""
        ok = self._mt5.initialize()
        if not ok:
            err = None
            last_error = getattr(self._mt5, "last_error", None)
            if callable(last_error):
                err = last_error()
            raise RuntimeError(
                f"MT5 initialize() failed ({err}). Open the MetaTrader5 terminal and log "
                "into your broker account first (Mode A: attach existing session)."
            )
        self._initialized = True

    def terminal_connected(self) -> bool:
        info = self._mt5.terminal_info()
        return bool(info and getattr(info, "connected", False))

    def list_symbols(self):
        return [
            {"raw": s.name, "description": getattr(s, "description", "")}
            for s in (self._mt5.symbols_get() or [])
        ]

    def get_tick(self, broker_symbol: str):
        # Ensure the symbol is selected in Market Watch (read-only side effect).
        self._mt5.symbol_select(broker_symbol, True)
        t = self._mt5.symbol_info_tick(broker_symbol)
        if t is None:
            return None
        return {
            "symbol": broker_symbol,
            "bid": _field_attr(t, "bid"),
            "ask": _field_attr(t, "ask"),
            "time": _epoch_to_iso_utc(_field_attr(t, "time")),
        }

    def get_candles(self, broker_symbol: str, timeframe_minutes: int, count: int):
        if count <= 0:
            return []
        attr = _TIMEFRAME_ATTR_BY_MINUTES.get(int(timeframe_minutes))
        if attr is None:
            raise ValueError(
                f"Unsupported timeframe {timeframe_minutes} minutes "
                f"(supported: {sorted(_TIMEFRAME_ATTR_BY_MINUTES)})."
            )
        tf_const = getattr(self._mt5, attr)
        self._mt5.symbol_select(broker_symbol, True)
        # copy_rates_from_pos(symbol, timeframe, start_pos=0, count) — newest `count` bars.
        rows = self._mt5.copy_rates_from_pos(broker_symbol, tf_const, 0, count)
        if rows is None:
            return []
        candles = []
        for r in rows:
            candles.append({
                "time": _epoch_to_iso_utc(_field(r, "time")),
                "open": float(_field(r, "open")),
                "high": float(_field(r, "high")),
                "low": float(_field(r, "low")),
                "close": float(_field(r, "close")),
                # MT5 exposes tick_volume for FX/metals (real_volume is often 0).
                "volume": float(_field(r, "tick_volume")),
            })
        return candles


def _field_attr(obj, name):
    """Read a field from a tick object (attribute) or a mapping (test fake)."""
    if isinstance(obj, dict):
        return obj[name]
    return getattr(obj, name)
