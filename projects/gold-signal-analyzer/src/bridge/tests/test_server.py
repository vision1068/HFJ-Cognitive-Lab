import json
import os
import sys
import threading
import unittest
import urllib.error
import urllib.request

sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), "..")))
from mt5_bridge.config import BridgeConfig, TOKEN_HEADER  # noqa: E402
from mt5_bridge.market_source import FakeMarketDataSource  # noqa: E402
from mt5_bridge.server import build_server  # noqa: E402

TOKEN = "test-token-e2e"


class ServerE2ETests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        source = FakeMarketDataSource(
            symbols=[{"raw": "XAUUSD", "description": "Gold"}],
            tick={"symbol": "XAUUSD", "bid": 2400.1, "ask": 2400.3, "time": "2026-07-30T12:00:00Z"},
        )
        cls.config = BridgeConfig(host="127.0.0.1", port=0, token=TOKEN)  # port 0 = ephemeral
        cls.server = build_server(cls.config, source)
        cls.port = cls.server.server_address[1]
        cls.thread = threading.Thread(target=cls.server.serve_forever, daemon=True)
        cls.thread.start()

    @classmethod
    def tearDownClass(cls):
        cls.server.shutdown()

    def _get(self, path, token=TOKEN):
        req = urllib.request.Request(f"http://127.0.0.1:{self.port}{path}")
        if token is not None:
            req.add_header(TOKEN_HEADER, token)
        return urllib.request.urlopen(req, timeout=5)

    def test_unauthenticated_call_is_rejected(self):
        with self.assertRaises(urllib.error.HTTPError) as ctx:
            self._get("/health", token=None)
        self.assertEqual(ctx.exception.code, 401)

    def test_wrong_token_is_rejected(self):
        with self.assertRaises(urllib.error.HTTPError) as ctx:
            self._get("/health", token="nope")
        self.assertEqual(ctx.exception.code, 401)

    def test_health_ok_with_token(self):
        resp = self._get("/health")
        body = json.loads(resp.read())
        self.assertEqual(body["status"], "ok")
        self.assertIn("terminalConnected", body)

    def test_symbols_and_tick(self):
        symbols = json.loads(self._get("/symbols").read())
        self.assertEqual(symbols["symbols"][0]["raw"], "XAUUSD")
        tick = json.loads(self._get("/tick?symbol=XAUUSD").read())
        self.assertEqual(tick["bid"], 2400.1)

    def test_bound_to_loopback_only(self):
        # server_address host must be a loopback address
        self.assertEqual(self.server.server_address[0], "127.0.0.1")


if __name__ == "__main__":
    unittest.main()
