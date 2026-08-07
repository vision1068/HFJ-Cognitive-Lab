"""Local MT5 bridge (FR-8).

A localhost-only, token-authenticated HTTP service that the .NET client
(HttpMt5BridgeClient) talks to. The real terminal integration lives here in
Python (via the MetaTrader5 package), NOT in the .NET process — so the .NET
side never links MT5 and stays testable without a terminal.

Read-only scope for this cycle: NO order execution, modification, or closing.
The MetaTrader5-backed source is a guarded seam and is never exercised by the
test suite (no live terminal in scope).
"""

__all__ = ["auth", "config", "symbols", "market_source", "server"]
