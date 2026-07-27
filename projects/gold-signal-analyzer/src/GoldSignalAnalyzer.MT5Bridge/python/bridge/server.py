"""
Loopback HTTP transport for the read-only bridge (FR-37 / FR-39 / ADR-8).

Security posture (all testable here with a FakeGateway, no MetaTrader5):
  * Binds 127.0.0.1 on an OS-assigned ephemeral port (never 0.0.0.0 / a public
    interface). The bound address is introspectable via ``server_address``.
  * Every request must carry ``Authorization: Bearer <token>``; the token is
    compared in CONSTANT TIME (hmac.compare_digest) -- never ``==`` on the token.
  * Requests are routed through the FIXED allowlist table in ``commands`` -- there
    is no reflection path to the SDK. Denied commands get a 4xx before any read.

This module imports NO MetaTrader5; the gateway is injected, so the server runs
under test with a fake/None gateway.
"""

import hmac
import json
import threading
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer

from bridge import commands


class _Handler(BaseHTTPRequestHandler):
    _BEARER = "Bearer "

    def _authorized(self):
        """Constant-time bearer-token check. Uses hmac.compare_digest only;
        performs no ``==``/``!=`` comparison on the token."""
        header = self.headers.get("Authorization", "")
        if not header.startswith(self._BEARER):
            return False
        provided = header[len(self._BEARER):]
        return hmac.compare_digest(provided, self.server.bridge_token)

    def _send(self, status, payload):
        body = json.dumps(payload).encode("utf-8")
        self.send_response(status)
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)

    def do_GET(self):
        if not self._authorized():
            self._send(401, {"ok": False, "error": "unauthorized"})
            return
        if self.path == "/health":
            result = commands.dispatch(
                "health", {}, self.server.bridge_gateway, self.server.bridge_audit)
            self._send(200, result)
            return
        self._send(404, {"ok": False, "error": "not_found"})

    def do_POST(self):
        if not self._authorized():
            self._send(401, {"ok": False, "error": "unauthorized"})
            return
        try:
            length = int(self.headers.get("Content-Length", 0) or 0)
        except (TypeError, ValueError):
            length = 0
        raw = self.rfile.read(length) if length else b""
        try:
            request = json.loads(raw.decode("utf-8"))
        except (ValueError, UnicodeDecodeError):
            self._send(400, {"ok": False, "error": "malformed_request"})
            return
        if not isinstance(request, dict):
            self._send(400, {"ok": False, "error": "malformed_request"})
            return
        if request.get("protocolVersion") != commands.PROTOCOL_VERSION:
            self._send(400, {"ok": False, "error": "protocol_version_mismatch"})
            return

        command = request.get("command")
        params = request.get("params") or {}
        result = commands.dispatch(
            command, params, self.server.bridge_gateway, self.server.bridge_audit)

        if result.get("ok"):
            self._send(200, result)
        elif result.get("error") == "command_not_allowed":
            self._send(403, result)
        else:
            self._send(400, result)

    def log_message(self, *args):
        # Silence the default stderr access log; the audit log is authoritative
        # and never records the token.
        return


class BridgeServer:
    """Wraps a ThreadingHTTPServer bound to loopback with an injected gateway."""

    def __init__(self, gateway, token, audit=None, host="127.0.0.1", port=0):
        self._httpd = ThreadingHTTPServer((host, port), _Handler)
        self._httpd.bridge_gateway = gateway
        self._httpd.bridge_token = token
        self._httpd.bridge_audit = audit
        self._thread = None

    @property
    def server_address(self):
        """The bound (host, port) tuple -- introspectable to assert loopback."""
        return self._httpd.server_address

    @property
    def host(self):
        return self._httpd.server_address[0]

    @property
    def port(self):
        return self._httpd.server_address[1]

    def start_background(self):
        self._thread = threading.Thread(target=self._httpd.serve_forever, daemon=True)
        self._thread.start()
        return self

    def serve_forever(self):
        self._httpd.serve_forever()

    def shutdown(self):
        self._httpd.shutdown()
        self._httpd.server_close()
