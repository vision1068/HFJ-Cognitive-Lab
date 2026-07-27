"""
Live entrypoint for the read-only bridge (FR-37). Started by the .NET
Mt5BridgeClient (a later slice) as a child process:

    1. The parent writes the bearer token as the first line of STDIN
       (the SOLE token transport -- never a command-line arg, never an env var:
       a child env block is readable by same-user processes and captured in WER
       crash dumps -- Phase 2.5 cond C1 / Audit-lens).
    2. This script binds the loopback server on an ephemeral port and prints a
       one-line readiness handshake ``{"ready": true, "port": N}`` to STDOUT.
    3. It then serves requests until shut down.

SLICE BOUNDARY: this entrypoint does NOT call gateway.connect() -- it does not
attach to a live terminal. Live connect (real mt5.initialize + reads) is a later
slice, gated on the RM2 test being green and explicit user confirmation.

This module imports mt5_gateway, but mt5_gateway performs its MetaTrader5 import
lazily inside connect(), so importing this module does not require the SDK.
"""

import json
import sys

from bridge import audit as audit_mod
from bridge import mt5_gateway
from bridge import server as server_mod


def main(stdin=None, stdout=None):
    stdin = stdin if stdin is not None else sys.stdin
    stdout = stdout if stdout is not None else sys.stdout

    token = stdin.readline().strip()
    if not token:
        sys.stderr.write("bridge: no token on stdin\n")
        return 2

    gateway = mt5_gateway.Mt5Gateway(terminal_path=None)
    srv = server_mod.BridgeServer(gateway, token, audit=audit_mod.AuditLog())

    stdout.write(json.dumps({"ready": True, "port": srv.port}) + "\n")
    stdout.flush()

    try:
        srv.serve_forever()
    except KeyboardInterrupt:
        pass
    finally:
        srv.shutdown()
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
