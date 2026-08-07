"""Bridge configuration (NFR-1, NFR-2).

Invariants enforced at construction:
  * host MUST be loopback (127.0.0.1 / ::1 / localhost) — the bridge is never
    exposed off-box.
  * a token MUST be present — sourced from the environment, never hardcoded.
"""
import ipaddress
import os

TOKEN_HEADER = "X-Bridge-Token"


def is_loopback(host: str) -> bool:
    if host is None:
        return False
    if host.lower() == "localhost":
        return True
    try:
        return ipaddress.ip_address(host).is_loopback
    except ValueError:
        return False


class BridgeConfig:
    def __init__(self, host: str, port: int, token: str):
        if not is_loopback(host):
            raise ValueError(
                f"Bridge must bind loopback only (127.0.0.1/::1/localhost); '{host}' refused (NFR-2)."
            )
        if not (0 <= int(port) <= 65535):
            # 0 = let the OS assign an ephemeral loopback port.
            raise ValueError("Port must be 0..65535.")
        if not token:
            raise ValueError("Bridge token is required; set GSA_BRIDGE_TOKEN (NFR-1/NFR-2).")
        self.host = host
        self.port = int(port)
        self.token = token

    @classmethod
    def from_env(cls) -> "BridgeConfig":
        return cls(
            host=os.environ.get("GSA_BRIDGE_HOST", "127.0.0.1"),
            port=int(os.environ.get("GSA_BRIDGE_PORT", "9001")),
            token=os.environ.get("GSA_BRIDGE_TOKEN", ""),
        )
