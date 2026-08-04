"""Entrypoint: start the local MT5 bridge (FR-8, Cycle 7 FR-36).

Usage (PowerShell) — READ-ONLY live data from an already-logged-in terminal:
    # 1. Open MetaTrader5 and log into your broker (e.g. Exness demo) first.
    # 2. Then, in the same Windows session:
    $env:GSA_BRIDGE_TOKEN = "<a strong random token>"
    python run_bridge.py --live

By default (no --live) this serves the FakeMarketDataSource (no terminal needed).
With --live it attaches to the running MetaTrader5 terminal in Mode A (attach
existing session): NO login/password is passed — the bridge reads only. It never
places, modifies, or closes an order (INV-1) and stores no credential (INV-2/3).
"""
import argparse

from mt5_bridge.config import BridgeConfig
from mt5_bridge.market_source import FakeMarketDataSource, Mt5MarketDataSource
from mt5_bridge.server import build_server


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--live", action="store_true", help="Use the read-only MetaTrader5 source.")
    args = parser.parse_args()

    config = BridgeConfig.from_env()
    if args.live:
        source = Mt5MarketDataSource()
        # Mode A attach: connect to the terminal the user already logged in.
        # No credentials are passed. Fail fast with a clear message if the
        # terminal isn't open/logged-in rather than serving nothing silently.
        source.initialize()
    else:
        source = FakeMarketDataSource()
    server = build_server(config, source)
    mode = "LIVE read-only terminal" if args.live else "fake sample source"
    print(f"MT5 bridge listening on http://{config.host}:{config.port} (loopback only) — {mode}")
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        server.shutdown()


if __name__ == "__main__":
    main()
