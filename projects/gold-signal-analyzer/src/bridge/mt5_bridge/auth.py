"""Token authentication for the bridge (NFR-2).

The bridge rejects every unauthenticated call. Token comparison is
constant-time (hmac.compare_digest) to avoid a timing side-channel.
"""
from __future__ import annotations

import hmac


def token_is_valid(provided: str | None, expected: str | None) -> bool:
    if not provided or not expected:
        return False
    return hmac.compare_digest(str(provided), str(expected))
