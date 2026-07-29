"""Localhost-only HTTP server for the MT5 bridge (FR-8, NFR-2).

Stdlib-only (http.server) so it runs with no third-party install. Every
request must carry a valid X-Bridge-Token or gets 401. Routes:
  GET /health   -> {"status":"ok","terminalConnected":bool,"build":str}
  GET /symbols  -> {"symbols":[{"raw","description"}]}
  GET /tick     -> {"symbol","bid","ask","time"} or 404
  GET /candles  -> {"candles":[...]}
The server never fabricates data — it only returns what the source provides.
"""
from __future__ import annotations

import json
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from urllib.parse import urlparse, parse_qs

from . import auth
from .config import TOKEN_HEADER, BridgeConfig
from .market_source import MarketDataSource


def make_handler(config: BridgeConfig, source: MarketDataSource):
    class Handler(BaseHTTPRequestHandler):
        protocol_version = "HTTP/1.1"

        def log_message(self, *args):  # silence default stderr logging
            pass

        def _send(self, status: int, payload: dict):
            body = json.dumps(payload).encode("utf-8")
            self.send_response(status)
            self.send_header("Content-Type", "application/json")
            self.send_header("Content-Length", str(len(body)))
            self.end_headers()
            self.wfile.write(body)

        def _authorized(self) -> bool:
            return auth.token_is_valid(self.headers.get(TOKEN_HEADER), config.token)

        def do_GET(self):
            if not self._authorized():
                self._send(401, {"error": "unauthorized"})
                return
            parsed = urlparse(self.path)
            path = parsed.path.rstrip("/")
            qs = parse_qs(parsed.query)

            if path == "/health":
                self._send(200, {
                    "status": "ok",
                    "terminalConnected": source.terminal_connected(),
                    "build": "scaffold",
                })
            elif path == "/symbols":
                self._send(200, {"symbols": source.list_symbols()})
            elif path == "/tick":
                symbol = (qs.get("symbol") or [""])[0]
                tick = source.get_tick(symbol)
                if tick is None:
                    self._send(404, {"error": "no tick"})
                else:
                    self._send(200, tick)
            elif path == "/candles":
                symbol = (qs.get("symbol") or [""])[0]
                tf = int((qs.get("tf") or ["1"])[0])
                count = int((qs.get("count") or ["1"])[0])
                self._send(200, {"candles": source.get_candles(symbol, tf, count)})
            else:
                self._send(404, {"error": "not found"})

    return Handler


def build_server(config: BridgeConfig, source: MarketDataSource) -> ThreadingHTTPServer:
    handler = make_handler(config, source)
    # Binding to a loopback host is guaranteed by BridgeConfig's invariant.
    return ThreadingHTTPServer((config.host, config.port), handler)
