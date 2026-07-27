"""
Gold Signal Analyzer — local MT5 read-only bridge (Cycle 2, Slice 1).

RM2 invariant (phase-6-ceo.md condition #1): this bridge is read-only. No
order/trade/position API of the MetaTrader5 SDK is reachable from any request.
Enforcement is STRUCTURAL, not policy:

  * commands.py    - fixed ALLOWED_COMMANDS + DISPATCH table. No reflection.
  * handlers.py    - read-only handlers; names no mt5.* symbol; gateway-injected.
  * mt5_gateway.py - the ONLY module that imports MetaTrader5 (lazily), and it
                     names read-only SDK functions exclusively.
  * server.py      - loopback-only HTTP, token via stdin, per-command audit log.

The no-order property is proven by tests/test_readonly_allowlist.py, which runs
on stdlib unittest and imports neither MetaTrader5 nor pytest.
"""
