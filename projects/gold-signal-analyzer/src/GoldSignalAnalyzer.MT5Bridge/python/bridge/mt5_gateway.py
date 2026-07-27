"""
mt5_gateway.py -- the SINGLE module that imports MetaTrader5 (FR-38 / ADR-9).

Structural read-only invariant (proven by tests/test_readonly_allowlist.py):
  * MetaTrader5 is imported LAZILY, once, inside connect(), as `import
    MetaTrader5 as mt5` -- the ONLY permitted import form. Module load performs
    NO SDK call (the module-level `mt5` below is a lazy-bound placeholder, not an
    import and not a call).
  * Every SDK access goes through the `mt5.` alias so the AST proof can enumerate
    it. The set of mt5.* symbols named here is a subset of the read-only allowlist
    (+ TIMEFRAME_* constants); NO order/trade/position symbol is named, and
    initialize() is called path-only (no login/password/server, positional or
    keyword) -- the bridge structurally cannot authenticate or mutate.

This module is imported only by run_bridge.py at live-run time; the test suite
reads it as TEXT and never imports it, so no MetaTrader5 install is needed to
prove the invariant.
"""

# Lazy-bound placeholder. This is NOT an import and NOT an mt5.* call, so module
# load touches no live terminal (AC / RM2 assertion 10). connect() rebinds it to
# the real SDK module via `global mt5; import MetaTrader5 as mt5`.
mt5 = None


class Mt5Gateway:
    """Read-only accessor over a locally-running, user-logged-in MT5 terminal.

    Mode A (default): attaches to an already-logged-in terminal via
    initialize(path=...). Collects/holds NO credential and never calls login().
    """

    def __init__(self, terminal_path=None):
        self._path = terminal_path
        self._connected = False

    # -- lifecycle ----------------------------------------------------------
    def connect(self):
        """Attach to the local terminal (path-only). Never authenticates."""
        global mt5
        import MetaTrader5 as mt5  # lazy, isolated, sole SDK import
        if self._path:
            ok = mt5.initialize(path=self._path)
        else:
            ok = mt5.initialize()
        if not ok:
            code, text = mt5.last_error()
            raise RuntimeError("mt5.initialize failed: %s %s" % (code, text))
        self._connected = True
        return {"connected": True, "version": mt5.version()}

    def disconnect(self):
        if mt5 is not None and self._connected:
            mt5.shutdown()
        self._connected = False
        return {"connected": False}

    # -- read-only reads ----------------------------------------------------
    def terminal_info(self):
        return self._to_dict(mt5.terminal_info())

    def account_info(self):
        return self._to_dict(mt5.account_info())

    def symbols_get(self, group=None):
        rows = mt5.symbols_get(group) if group else mt5.symbols_get()
        return [self._to_dict(r) for r in (rows or [])]

    def symbol_info(self, symbol):
        return self._to_dict(mt5.symbol_info(symbol))

    def symbol_info_tick(self, symbol):
        return self._to_dict(mt5.symbol_info_tick(symbol))

    def copy_rates_from_pos(self, symbol, timeframe, start, count):
        rates = mt5.copy_rates_from_pos(symbol, self._timeframe(timeframe), start, count)
        return self._rates_to_list(rates)

    def copy_rates_range(self, symbol, timeframe, date_from, date_to):
        rates = mt5.copy_rates_range(symbol, self._timeframe(timeframe), date_from, date_to)
        return self._rates_to_list(rates)

    # -- helpers (no SDK) ---------------------------------------------------
    def _timeframe(self, name):
        """Map a timeframe label to an SDK constant via an explicit table (no
        reflection). Only the brief's four timeframes are exposed this slice."""
        table = {
            "M15": mt5.TIMEFRAME_M15,
            "H1": mt5.TIMEFRAME_H1,
            "H4": mt5.TIMEFRAME_H4,
            "D1": mt5.TIMEFRAME_D1,
        }
        return table[name]

    @staticmethod
    def _to_dict(obj):
        if obj is None:
            return None
        if hasattr(obj, "_asdict"):
            return dict(obj._asdict())
        return dict(obj)

    @staticmethod
    def _rates_to_list(rates):
        if rates is None:
            return []
        names = rates.dtype.names
        return [dict(zip(names, [_py(v) for v in row])) for row in rates]


def _py(value):
    """Coerce a numpy scalar to a plain Python number/str for JSON."""
    item = value.item if hasattr(value, "item") else None
    return item() if item is not None else value
