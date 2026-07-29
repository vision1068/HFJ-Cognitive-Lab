"""Market-data sources for the bridge.

MarketDataSource is the seam. Two implementations:
  * FakeMarketDataSource — deterministic, used by tests and demos. Explicitly
    NOT live (is_live == False) so its data can never be mistaken for a quote.
  * Mt5MarketDataSource — the real terminal seam. It imports the MetaTrader5
    package lazily and is READ-ONLY (copy_rates / symbol_info_tick only; no
    order_send). It is never constructed or exercised by the test suite in
    this cycle (no live terminal in scope). NFR-5: never fabricates data.
"""
from __future__ import annotations

from abc import ABC, abstractmethod


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


class Mt5MarketDataSource(MarketDataSource):
    """Read-only real terminal seam (FR-7/FR-8). Guarded import; the test
    suite never instantiates this. Deliberately exposes NO trading calls."""
    is_live = True

    def __init__(self):
        try:
            import MetaTrader5 as mt5  # noqa: F401  (optional, Windows + terminal only)
        except ImportError as exc:  # pragma: no cover - depends on live env
            raise RuntimeError(
                "MetaTrader5 package not available. Install it and run on a host "
                "with an MT5 terminal. This bridge is READ-ONLY."
            ) from exc
        self._mt5 = mt5
        # NOTE: intentionally NOT calling mt5.initialize() here — live attach is
        # out of scope for this cycle and requires explicit owner go-ahead.

    def terminal_connected(self) -> bool:  # pragma: no cover - live only
        info = self._mt5.terminal_info()
        return bool(info and getattr(info, "connected", False))

    def list_symbols(self):  # pragma: no cover - live only
        return [{"raw": s.name, "description": s.description} for s in (self._mt5.symbols_get() or [])]

    def get_tick(self, broker_symbol: str):  # pragma: no cover - live only
        t = self._mt5.symbol_info_tick(broker_symbol)
        if t is None:
            return None
        return {"symbol": broker_symbol, "bid": t.bid, "ask": t.ask, "time": t.time}

    def get_candles(self, broker_symbol: str, timeframe_minutes: int, count: int):  # pragma: no cover - live only
        raise NotImplementedError("Live candle retrieval is out of scope for this read-only cycle.")
