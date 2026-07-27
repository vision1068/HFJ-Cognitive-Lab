"""
Per-command audit log (Phase 2.5 cond C4 / Audit-lens). Emits one structured
allow/deny record per dispatched command (who-less local bridge: what/when/allowed
/param-keys), with secret-shaped values masked. Imports NO MetaTrader5.

The token is NEVER passed to or logged by this module. Only parameter KEY names
(and masked values for secret-shaped keys) are recorded -- never raw secrets.
"""

import json
import sys
import time

# Case-insensitive secret-shaped key names -- values are masked, never emitted
# raw. Mirrors the .NET SecretDenylist (incl. bridgetoken).
_SECRET_KEYS = frozenset({
    "password", "pwd", "token", "bridgetoken", "apikey", "apitoken",
    "accesstoken", "refreshtoken", "secret", "investorpassword", "account",
    "accountnumber", "login", "connectionstring", "credential",
    "telegrambottoken", "emailpassword",
})

_MASK = "***MASKED***"


def _mask_params(params):
    """Return a shallow, mask-applied copy of param KEYS only (never raw values
    of secret-shaped keys)."""
    if not isinstance(params, dict):
        return {}
    out = {}
    for key, value in params.items():
        if str(key).lower() in _SECRET_KEYS:
            out[key] = _MASK
        else:
            out[key] = value
    return out


class AuditLog:
    """Writes JSON-line audit records to a stream (default stderr). Injectable so
    tests can capture records without touching the real log."""

    def __init__(self, stream=None):
        self._stream = stream if stream is not None else sys.stderr

    def record(self, command, allowed, params=None):
        entry = {
            "ts": time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime()),
            "command": command,
            "decision": "allow" if allowed else "deny",
            "params": _mask_params(params),
        }
        self._stream.write(json.dumps(entry) + "\n")
        self._stream.flush()
        return entry
