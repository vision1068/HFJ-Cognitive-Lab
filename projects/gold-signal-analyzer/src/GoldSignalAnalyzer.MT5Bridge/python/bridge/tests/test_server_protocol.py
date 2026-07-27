"""
Bridge server auth + wire-protocol round-trip test (FR-37 / FR-39, AC-37.2/37.3,
AC-39.1/39.2). Starts the real server on 127.0.0.1:0 with a FakeGateway and hits
it over loopback via urllib. No MetaTrader5, no pytest; stdlib unittest, runs on
Python 3.9 and 3.14.

Covers:
  * loopback bind introspection: server_address[0] == "127.0.0.1" (AC-37.2)
  * allow-command round-trip -> 200 ok:true (AC-39.1)
  * deny-command -> 403 command_not_allowed, gateway untouched (AC-39.1)
  * malformed JSON -> 400 (AC-39.1)
  * wrong / absent token -> 401 (AC-37.3)
  * protocolVersion mismatch -> 400 (AC-39.2)
  * constant-time token compare (hmac.compare_digest, no ==/!= on token) (B3)
"""

import ast
import json
import os
import unittest
import urllib.error
import urllib.request

import bridge
from bridge import commands
from bridge import server as server_mod

TOKEN = "test-bridge-token-0123456789abcdef"
SERVER_PATH = os.path.join(os.path.dirname(os.path.abspath(bridge.__file__)), "server.py")


class FakeGateway:
    def __init__(self):
        self.calls = []

    def copy_rates_from_pos(self, symbol, timeframe, start, count):
        self.calls.append("copy_rates_from_pos")
        return [{"time": 1, "open": 1.0, "high": 2.0, "low": 0.5, "close": 1.5}]


def _request(port, method="POST", body=None, token=TOKEN, path="/"):
    url = "http://127.0.0.1:%d%s" % (port, path)
    data = None
    if body is not None:
        data = body if isinstance(body, bytes) else json.dumps(body).encode("utf-8")
    req = urllib.request.Request(url, data=data, method=method)
    if token is not None:
        req.add_header("Authorization", "Bearer " + token)
    if data is not None:
        req.add_header("Content-Type", "application/json")
    try:
        with urllib.request.urlopen(req, timeout=5) as resp:
            return resp.status, json.loads(resp.read().decode("utf-8"))
    except urllib.error.HTTPError as err:
        try:
            payload = err.read().decode("utf-8")
            try:
                payload = json.loads(payload)
            except ValueError:
                pass
            return err.code, payload
        finally:
            err.close()


class ServerProtocolTest(unittest.TestCase):

    def setUp(self):
        self.gateway = FakeGateway()
        self.server = server_mod.BridgeServer(self.gateway, TOKEN)
        self.server.start_background()
        self.port = self.server.port

    def tearDown(self):
        self.server.shutdown()

    def test_binds_loopback_only(self):
        self.assertEqual(self.server.server_address[0], "127.0.0.1")
        self.assertNotEqual(self.server.server_address[0], "0.0.0.0")
        self.assertGreater(self.port, 0)

    def test_allow_command_round_trip(self):
        status, payload = _request(self.port, body={
            "protocolVersion": commands.PROTOCOL_VERSION,
            "command": "copy_rates_from_pos",
            "params": {"symbol": "XAUUSD", "timeframe": "H1", "count": 5},
        })
        self.assertEqual(status, 200)
        self.assertTrue(payload["ok"])
        self.assertEqual(self.gateway.calls, ["copy_rates_from_pos"])

    def test_health_get_round_trip(self):
        status, payload = _request(self.port, method="GET", path="/health")
        self.assertEqual(status, 200)
        self.assertTrue(payload["ok"])
        self.assertEqual(payload["data"]["status"], "ok")

    def test_deny_command_is_4xx_and_gateway_untouched(self):
        status, payload = _request(self.port, body={
            "protocolVersion": commands.PROTOCOL_VERSION,
            "command": "order_send",
            "params": {"symbol": "XAUUSD"},
        })
        self.assertEqual(status, 403)
        self.assertFalse(payload["ok"])
        self.assertEqual(payload["error"], "command_not_allowed")
        self.assertEqual(self.gateway.calls, [])

    def test_malformed_json_is_400(self):
        status, payload = _request(self.port, body=b"{not valid json")
        self.assertEqual(status, 400)
        self.assertEqual(payload["error"], "malformed_request")

    def test_wrong_token_is_401(self):
        status, payload = _request(self.port, token="wrong-token", body={
            "protocolVersion": commands.PROTOCOL_VERSION,
            "command": "copy_rates_from_pos",
            "params": {"symbol": "XAUUSD", "timeframe": "H1", "count": 5},
        })
        self.assertEqual(status, 401)
        self.assertEqual(self.gateway.calls, [])

    def test_absent_token_is_401(self):
        status, _payload = _request(self.port, token=None, body={
            "protocolVersion": commands.PROTOCOL_VERSION,
            "command": "copy_rates_from_pos",
            "params": {"symbol": "XAUUSD", "timeframe": "H1", "count": 5},
        })
        self.assertEqual(status, 401)

    def test_protocol_version_mismatch_is_rejected(self):
        status, payload = _request(self.port, body={
            "protocolVersion": "9.9",
            "command": "copy_rates_from_pos",
            "params": {"symbol": "XAUUSD", "timeframe": "H1", "count": 5},
        })
        self.assertEqual(status, 400)
        self.assertEqual(payload["error"], "protocol_version_mismatch")

    def test_token_compare_is_constant_time(self):
        """AST: the _authorized token check uses hmac.compare_digest and performs
        no ==/!= comparison on the token (Phase 2.5 cond B3)."""
        with open(SERVER_PATH, "r", encoding="utf-8") as fh:
            tree = ast.parse(fh.read())
        auth_fn = None
        for node in ast.walk(tree):
            if isinstance(node, ast.FunctionDef) and node.name == "_authorized":
                auth_fn = node
        self.assertIsNotNone(auth_fn, "_authorized method not found in server.py")

        uses_compare_digest = False
        for node in ast.walk(auth_fn):
            if (isinstance(node, ast.Call) and isinstance(node.func, ast.Attribute)
                    and node.func.attr == "compare_digest"):
                uses_compare_digest = True
            if isinstance(node, ast.Compare):
                for op in node.ops:
                    self.assertNotIsInstance(op, (ast.Eq, ast.NotEq),
                                             "token check must not use ==/!= (use hmac.compare_digest)")
        self.assertTrue(uses_compare_digest, "_authorized must use hmac.compare_digest")


if __name__ == "__main__":
    unittest.main()
