"""Cycle 7 (FR-36) — the read-only live MT5 source, proven with an INJECTED fake
MetaTrader5 module (no real terminal needed). Verifies:
  * candle conversion: epoch->ISO-8601 UTC, OHLCV shape, newest-`count` semantics;
  * timeframe minutes -> MT5 TIMEFRAME_* constant mapping (and rejection of unknown);
  * tick conversion (bid/ask/ISO time);
  * initialize() Mode A: called with NO credentials; raises on failure;
  * READ-ONLY contract: the source calls only read APIs and exposes NO order verb;
  * NFR-5: not-connected / None rows never fabricate a price.
"""
import os
import sys
import unittest

sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), "..")))
from mt5_bridge.market_source import Mt5MarketDataSource  # noqa: E402


class _Tick:
    def __init__(self, bid, ask, time):
        self.bid, self.ask, self.time = bid, ask, time


class _Sym:
    def __init__(self, name, description=""):
        self.name, self.description = name, description


class _TermInfo:
    def __init__(self, connected):
        self.connected = connected


class FakeMt5:
    """Records every call so we can prove the read-only contract and the
    exact arguments (esp. that initialize() gets NO credentials)."""
    # TIMEFRAME_* constants mirror the real module's distinct opaque values.
    TIMEFRAME_M1 = 1
    TIMEFRAME_M5 = 5
    TIMEFRAME_M15 = 15
    TIMEFRAME_M30 = 30
    TIMEFRAME_H1 = 16385
    TIMEFRAME_H4 = 16388
    TIMEFRAME_D1 = 16408

    def __init__(self, connected=True, init_ok=True, rows=None, tick=None, symbols=None):
        self._connected = connected
        self._init_ok = init_ok
        self._rows = rows
        self._tick = tick
        self._symbols = symbols or []
        self.calls = []

    def initialize(self, *args, **kwargs):
        self.calls.append(("initialize", args, kwargs))
        return self._init_ok

    def last_error(self):
        return (-1, "fake error")

    def terminal_info(self):
        self.calls.append(("terminal_info", (), {}))
        return _TermInfo(self._connected)

    def symbols_get(self):
        self.calls.append(("symbols_get", (), {}))
        return self._symbols

    def symbol_select(self, symbol, enable):
        self.calls.append(("symbol_select", (symbol, enable), {}))
        return True

    def symbol_info_tick(self, symbol):
        self.calls.append(("symbol_info_tick", (symbol,), {}))
        return self._tick

    def copy_rates_from_pos(self, symbol, timeframe, start_pos, count):
        self.calls.append(("copy_rates_from_pos", (symbol, timeframe, start_pos, count), {}))
        return self._rows


class Mt5LiveSourceTests(unittest.TestCase):
    def test_is_live_true(self):
        self.assertTrue(Mt5MarketDataSource(mt5=FakeMt5()).is_live)

    def test_initialize_mode_a_passes_no_credentials(self):
        fake = FakeMt5(init_ok=True)
        Mt5MarketDataSource(mt5=fake).initialize()
        init_calls = [c for c in fake.calls if c[0] == "initialize"]
        self.assertEqual(len(init_calls), 1)
        # Mode A: NO positional and NO keyword args (no login/password/server).
        self.assertEqual(init_calls[0][1], ())
        self.assertEqual(init_calls[0][2], {})

    def test_initialize_failure_raises_actionable_error(self):
        src = Mt5MarketDataSource(mt5=FakeMt5(init_ok=False))
        with self.assertRaises(RuntimeError) as ctx:
            src.initialize()
        self.assertIn("terminal", str(ctx.exception).lower())

    def test_terminal_connected_reflects_terminal_info(self):
        self.assertTrue(Mt5MarketDataSource(mt5=FakeMt5(connected=True)).terminal_connected())
        self.assertFalse(Mt5MarketDataSource(mt5=FakeMt5(connected=False)).terminal_connected())

    def test_get_candles_converts_epoch_to_iso_utc_and_ohlcv(self):
        # 2026-07-31T12:00:00Z == epoch 1785499200
        rows = [
            {"time": 1785499200, "open": 3300.0, "high": 3305.5, "low": 3298.0,
             "close": 3302.5, "tick_volume": 1234, "real_volume": 0, "spread": 20},
        ]
        src = Mt5MarketDataSource(mt5=FakeMt5(rows=rows))
        out = src.get_candles("XAUUSD", 60, 1)
        self.assertEqual(len(out), 1)
        c = out[0]
        self.assertEqual(c["time"], "2026-07-31T12:00:00Z")
        self.assertEqual((c["open"], c["high"], c["low"], c["close"]), (3300.0, 3305.5, 3298.0, 3302.5))
        self.assertEqual(c["volume"], 1234.0)  # tick_volume, not real_volume

    def test_get_candles_maps_timeframe_minutes_to_mt5_constant(self):
        fake = FakeMt5(rows=[])
        Mt5MarketDataSource(mt5=fake).get_candles("XAUUSD", 240, 10)  # H4
        rate_calls = [c for c in fake.calls if c[0] == "copy_rates_from_pos"]
        self.assertEqual(rate_calls[0][1][1], FakeMt5.TIMEFRAME_H4)  # constant, not 240

    def test_get_candles_rejects_unsupported_timeframe(self):
        with self.assertRaises(ValueError):
            Mt5MarketDataSource(mt5=FakeMt5(rows=[])).get_candles("XAUUSD", 7, 10)

    def test_get_candles_none_rows_returns_empty_not_fabricated(self):
        # NFR-5: terminal returned nothing -> empty, never an invented candle.
        self.assertEqual(Mt5MarketDataSource(mt5=FakeMt5(rows=None)).get_candles("XAUUSD", 60, 5), [])

    def test_get_tick_converts_bid_ask_and_time(self):
        tick = _Tick(bid=3300.10, ask=3300.30, time=1785499200)
        out = Mt5MarketDataSource(mt5=FakeMt5(tick=tick)).get_tick("XAUUSD")
        self.assertEqual(out["bid"], 3300.10)
        self.assertEqual(out["ask"], 3300.30)
        self.assertEqual(out["time"], "2026-07-31T12:00:00Z")
        self.assertEqual(out["symbol"], "XAUUSD")

    def test_get_tick_none_returns_none(self):
        self.assertIsNone(Mt5MarketDataSource(mt5=FakeMt5(tick=None)).get_tick("XAUUSD"))

    def test_list_symbols_shape(self):
        fake = FakeMt5(symbols=[_Sym("XAUUSD", "Gold"), _Sym("EURUSD", "Euro")])
        out = Mt5MarketDataSource(mt5=fake).list_symbols()
        self.assertEqual(out[0], {"raw": "XAUUSD", "description": "Gold"})

    def test_source_calls_only_read_only_mt5_apis(self):
        # Drive every data path, then assert the fake was NEVER asked to trade.
        rows = [{"time": 1785499200, "open": 1, "high": 2, "low": 0.5, "close": 1.5, "tick_volume": 3}]
        fake = FakeMt5(rows=rows, tick=_Tick(1, 2, 1785499200), symbols=[_Sym("XAUUSD")])
        src = Mt5MarketDataSource(mt5=fake)
        src.initialize()
        src.terminal_connected()
        src.list_symbols()
        src.get_tick("XAUUSD")
        src.get_candles("XAUUSD", 60, 1)
        called = {c[0] for c in fake.calls}
        allowed = {"initialize", "terminal_info", "symbols_get",
                   "symbol_select", "symbol_info_tick", "copy_rates_from_pos"}
        self.assertTrue(called.issubset(allowed), f"unexpected MT5 calls: {called - allowed}")

    def test_source_exposes_no_order_verb(self):
        # INV-1: no attribute name hints at placing/modifying/closing an order.
        forbidden = ("order", "send", "buy", "sell", "trade", "position", "deal", "close")
        for name in dir(Mt5MarketDataSource):
            if name.startswith("_"):
                continue
            lname = name.lower()
            self.assertFalse(
                any(tok in lname for tok in forbidden),
                f"member '{name}' looks order-adjacent (INV-1)",
            )


class _Clock:
    """Injectable monotonic clock so the reconnect throttle is testable without
    wall-clock waits."""
    def __init__(self, t=0.0):
        self.t = t

    def __call__(self):
        return self.t

    def advance(self, dt):
        self.t += dt


class ReconnectFakeMt5:
    """Fake MT5 module whose connection can be flipped at runtime to simulate the
    terminal64.exe process dying/restarting under a new PID. A successful
    initialize() models a real Mode A re-attach: it flips the terminal back to
    connected. Counts initialize() calls and records their args so the tests can
    prove credential-free reconnect + throttling."""
    TIMEFRAME_H1 = 16385

    def __init__(self, connected=True, init_ok=True):
        self.connected = connected            # mutable: False == PID died / stale handle
        self.init_ok = init_ok                # whether a reconnect initialize() succeeds
        self.init_raises = False              # dead pipe: initialize() itself throws
        self.terminal_info_raises = False     # stale handle: terminal_info() throws
        # Models the REAL MT5 hazard: symbol_info_tick / copy_rates_from_pos /
        # symbols_get keep returning last-cached data even while disconnected. When
        # True the API is "dishonest" and only the source's _raw_connected() gate
        # can keep NFR-6 (this is the adversarial case F1/C1 asks for).
        self.serve_cached_while_disconnected = False
        self.init_calls = 0
        self.init_args = []

    def _serving(self):
        return self.connected or self.serve_cached_while_disconnected

    def initialize(self, *args, **kwargs):
        self.init_calls += 1
        self.init_args.append((args, kwargs))
        if self.init_raises:
            raise RuntimeError("dead pipe")
        if self.init_ok:
            self.connected = True             # successful re-attach → terminal connected
        return self.init_ok

    def last_error(self):
        return (-1, "fake error")

    def terminal_info(self):
        if self.terminal_info_raises:
            raise RuntimeError("stale handle")
        return _TermInfo(self.connected)

    def symbols_get(self):
        return [_Sym("XAUUSD", "Gold")] if self._serving() else []

    def symbol_select(self, symbol, enable):
        return True

    def symbol_info_tick(self, symbol):
        if not self._serving():
            return None
        return {"bid": 3300.1, "ask": 3300.3, "time": 1785499200}

    def copy_rates_from_pos(self, symbol, timeframe, start_pos, count):
        if not self._serving():
            return None
        return [{"time": 1785499200, "open": 1.0, "high": 2.0, "low": 0.5,
                 "close": 1.5, "tick_volume": 3}]


class Mt5AutoReconnectTests(unittest.TestCase):
    """Cycle 11 — FR-41 / FR-42 / NFR-6."""

    def _initialized_source(self, fake, clock, interval=5.0):
        src = Mt5MarketDataSource(mt5=fake, clock=clock, reconnect_min_interval_s=interval)
        src.initialize()  # startup Mode A attach (fake starts connected)
        return src

    def test_reconnects_when_stale_and_recovers(self):
        # AC-41.1: initialized, terminal dies, next access re-inits and recovers.
        clock = _Clock(0.0)
        fake = ReconnectFakeMt5(connected=True, init_ok=True)
        src = self._initialized_source(fake, clock)
        self.assertEqual(fake.init_calls, 1)          # startup only, so far
        fake.connected = False                         # terminal PID died -> stale handle
        self.assertTrue(src.terminal_connected())      # triggers a reconnect that succeeds
        self.assertEqual(fake.init_calls, 2)           # exactly one reconnect attempt

    def test_reconnect_initialize_passes_no_credentials(self):
        # AC-41.2 (INV-2/INV-3): the reconnect initialize() carries no args at all.
        clock = _Clock(0.0)
        fake = ReconnectFakeMt5(connected=True, init_ok=True)
        src = self._initialized_source(fake, clock)
        fake.connected = False
        src.terminal_connected()
        self.assertEqual(fake.init_args[-1], ((), {}))  # Mode A: no login/password/server

    def test_no_reconnect_before_explicit_initialize(self):
        # AC-41.3: a never-initialized source never auto-opens a session.
        clock = _Clock(0.0)
        fake = ReconnectFakeMt5(connected=False, init_ok=True)
        src = Mt5MarketDataSource(mt5=fake, clock=clock)   # NOTE: no initialize()
        self.assertFalse(src.terminal_connected())
        self.assertEqual(fake.init_calls, 0)

    def test_reconnect_is_throttled_within_interval(self):
        # AC-42.1: many accesses in one window -> at most one reconnect attempt.
        clock = _Clock(0.0)
        fake = ReconnectFakeMt5(connected=True, init_ok=True)
        src = self._initialized_source(fake, clock)
        fake.init_ok = False                           # reconnects will now fail...
        fake.connected = False                         # ...and terminal is down
        src.terminal_connected()                       # attempt #1 (init_calls -> 2)
        src.terminal_connected()                       # throttled (same clock)
        src.get_tick("XAUUSD")                          # throttled
        src.get_candles("XAUUSD", 60, 5)                # throttled
        self.assertEqual(fake.init_calls, 2)           # 1 startup + 1 reconnect only

    def test_reconnect_retries_after_interval(self):
        # AC-42.2: once the throttle window elapses, a fresh attempt is made.
        clock = _Clock(0.0)
        fake = ReconnectFakeMt5(connected=True, init_ok=True)
        src = self._initialized_source(fake, clock)
        fake.init_ok = False
        fake.connected = False
        src.terminal_connected()                       # attempt #1 at t=0 (init_calls -> 2)
        clock.advance(5.0)                              # window elapses
        src.terminal_connected()                       # attempt #2 at t=5 (init_calls -> 3)
        self.assertEqual(fake.init_calls, 3)

    def test_failed_reconnect_stays_honest_no_fabrication(self):
        # AC-NFR6.1: while disconnected, nothing connected/no tick/no candle is faked.
        clock = _Clock(0.0)
        fake = ReconnectFakeMt5(connected=True, init_ok=True)
        src = self._initialized_source(fake, clock)
        fake.init_ok = False
        fake.connected = False
        self.assertFalse(src.terminal_connected())
        self.assertIsNone(src.get_tick("XAUUSD"))
        self.assertEqual(src.get_candles("XAUUSD", 60, 5), [])

    def test_reconnect_initialize_raising_is_swallowed(self):
        # AC-NFR6.2: a reconnect initialize() that throws never surfaces to callers.
        clock = _Clock(0.0)
        fake = ReconnectFakeMt5(connected=True, init_ok=True)
        src = self._initialized_source(fake, clock)
        fake.connected = False
        fake.init_raises = True
        self.assertFalse(src.terminal_connected())     # no exception raised
        self.assertEqual(fake.init_calls, 2)           # attempt was made and swallowed

    def test_terminal_info_raising_reads_as_disconnected(self):
        # NFR-6 robustness: a stale handle whose terminal_info() throws reads as
        # not-connected, never as an error to the caller.
        clock = _Clock(0.0)
        fake = ReconnectFakeMt5(connected=True, init_ok=True)
        src = self._initialized_source(fake, clock)
        fake.terminal_info_raises = True
        self.assertFalse(src.terminal_connected())

    def test_disconnected_never_serves_cached_data(self):
        # C1/F1 (structural NFR-6): even when the MT5 API keeps handing back
        # last-cached ticks/candles/symbols while disconnected, the source must
        # gate on terminal_info().connected and return None/[]/[]. This test FAILS
        # against a source that trusts the API's null-behavior — that's the point.
        clock = _Clock(0.0)
        fake = ReconnectFakeMt5(connected=True, init_ok=True)
        src = self._initialized_source(fake, clock)
        fake.init_ok = False                       # reconnect can't restore the link
        fake.connected = False                     # terminal genuinely disconnected
        fake.serve_cached_while_disconnected = True  # ...but the API still serves cache
        self.assertIsNone(src.get_tick("XAUUSD"))
        self.assertEqual(src.get_candles("XAUUSD", 60, 5), [])
        self.assertEqual(src.list_symbols(), [])

    def test_reconnect_single_attempt_under_concurrency(self):
        # F2: under ThreadingHTTPServer many requests share one source; the lock +
        # double-check must keep reconnect to exactly ONE initialize() per outage.
        import threading as _t
        clock = _Clock(0.0)
        fake = ReconnectFakeMt5(connected=True, init_ok=True)
        src = self._initialized_source(fake, clock)   # init_calls == 1 (startup)
        fake.connected = False                         # terminal died
        results = []
        start = _t.Barrier(12)

        def worker():
            start.wait()                               # release all at once → force the race
            results.append(src.terminal_connected())

        threads = [_t.Thread(target=worker) for _ in range(12)]
        for th in threads:
            th.start()
        for th in threads:
            th.join()
        self.assertEqual(fake.init_calls, 2)           # 1 startup + exactly 1 reconnect
        self.assertTrue(all(results))                  # every caller ends up seeing connected


if __name__ == "__main__":
    unittest.main()
