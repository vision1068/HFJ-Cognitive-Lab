"""Entrypoint: start the local MT5 bridge (FR-8).

Usage (PowerShell):
    $env:GSA_BRIDGE_TOKEN = "<a strong random token>"
    python run_bridge.py

By default this serves the FakeMarketDataSource (no live terminal). To use the
real read-only terminal seam, pass --live (requires the MetaTrader5 package and
a running terminal; out of scope for the current cycle without owner go-ahead).
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
    source = Mt5MarketDataSource() if args.live else FakeMarketDataSource()
    server = build_server(config, source)
    print(f"MT5 bridge listening on http://{config.host}:{config.port} (loopback only)")
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        server.shutdown()


if __name__ == "__main__":
    main()
