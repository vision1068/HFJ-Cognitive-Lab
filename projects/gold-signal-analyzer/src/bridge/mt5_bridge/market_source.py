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

import threading
import time
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
    fabricating when the terminal is unavailable.

    Cycle 11 (FR-41/FR-42/NFR-6) — AUTO-RECONNECT. The MT5 IPC handle bound by
    initialize() goes stale if the terminal64.exe process restarts under a new
    PID (auto-update / crash-relaunch). Rather than requiring a human to restart
    the whole bridge, every data access first calls `_reconnect_if_stale()`: if
    the source was initialized but the terminal now reads disconnected, it
    re-attempts a credential-free Mode A `initialize()` (INV-2/INV-3), throttled
    to at most one attempt per `reconnect_min_interval_s`. This NEVER fabricates
    (NFR-6): while disconnected — including during/after a failed reconnect — the
    source keeps reporting not-connected and returns None/[] for ticks/candles;
    connected state is reported only when `terminal_info().connected` is truly
    true again. The `clock` (monotonic seconds) is injectable so the throttle is
    unit-testable without wall-clock waits."""
    is_live = True

    def __init__(self, mt5=None, clock=None, reconnect_min_interval_s: float = 5.0):
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
        # Injected monotonic clock (seconds) so the reconnect throttle is testable.
        self._clock = clock or time.monotonic
        self._reconnect_min_interval_s = reconnect_min_interval_s
        self._last_reconnect_attempt = None  # monotonic seconds of the last attempt
        # The bridge runs under ThreadingHTTPServer sharing ONE source instance, so
        # concurrent /health + /tick could both enter the reconnect path. This lock
        # serializes the throttle-check-and-attempt so initialize() fires at most
        # once per interval (FR-42) and the MT5 library is never re-entered.
        self._reconnect_lock = threading.Lock()

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

    def _raw_connected(self) -> bool:
        """True iff the terminal reports connected RIGHT NOW. A dead/stale IPC
        handle may make terminal_info() return None or raise — both read as
        not-connected (NFR-6: never surface a live read as an error to callers,
        and never report connected when it isn't)."""
        try:
            info = self._mt5.terminal_info()
        except Exception:
            return False
        return bool(info and getattr(info, "connected", False))

    def _reconnect_if_stale(self) -> None:
        """FR-41/FR-42: if this source was initialized but the terminal now reads
        disconnected (PID restarted → stale handle), re-attempt a credential-free
        Mode A initialize() against the currently-running terminal — throttled to
        at most one attempt per reconnect_min_interval_s. Failures are swallowed:
        the source stays HONESTLY disconnected (NFR-6) and retries next window.
        Never auto-initializes a session a human never opened (guard: _initialized)."""
        if not self._initialized:
            return  # FR-41 AC-41.3: only re-attach; never open the first session.
        if self._raw_connected():
            return  # cheap fast-path: skip the lock while healthy (common case).
        with self._reconnect_lock:
            # Double-checked: another thread may have reconnected while we waited.
            if self._raw_connected():
                return
            now = self._clock()
            if (self._last_reconnect_attempt is not None
                    and now - self._last_reconnect_attempt < self._reconnect_min_interval_s):
                return  # FR-42: throttle — don't hammer initialize() on a down terminal.
            self._last_reconnect_attempt = now
            try:
                # Mode A: NO credentials (INV-2/INV-3). Rebinds the IPC handle to the
                # currently-running terminal the human is logged into.
                self._mt5.initialize()
            except Exception:
                pass  # NFR-6: stay disconnected + honest; re-attempt after the interval.

    def terminal_connected(self) -> bool:
        self._reconnect_if_stale()
        return self._raw_connected()

    def list_symbols(self):
        self._reconnect_if_stale()
        if not self._raw_connected():
            return []  # NFR-6: never serve a cached symbol universe while disconnected.
        return [
            {"raw": s.name, "description": getattr(s, "description", "")}
            for s in (self._mt5.symbols_get() or [])
        ]

    def get_tick(self, broker_symbol: str):
        self._reconnect_if_stale()
        # NFR-6 (structural): the MT5 API can return a LAST-CACHED tick after the
        # broker link drops, so gate on the live connection state — never serve a
        # stale-but-real-looking price during a genuine outage.
        if not self._raw_connected():
            return None
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
        self._reconnect_if_stale()
        # NFR-6 (structural): copy_rates_from_pos reads MT5's LOCAL rate cache and
        # routinely returns the last-known bars even while disconnected. Gate on the
        # live connection so a genuine outage yields [] rather than stale candles.
        if not self._raw_connected():
            return []
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
