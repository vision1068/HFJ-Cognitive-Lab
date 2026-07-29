import os
import sys
import unittest

sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), "..")))
from mt5_bridge.config import BridgeConfig, is_loopback  # noqa: E402


class ConfigTests(unittest.TestCase):
    def test_loopback_hosts_accepted(self):
        for host in ("127.0.0.1", "::1", "localhost"):
            self.assertTrue(is_loopback(host), host)

    def test_non_loopback_hosts_rejected(self):
        for host in ("0.0.0.0", "10.0.0.5", "192.168.1.20", "example.com"):
            self.assertFalse(is_loopback(host), host)

    def test_config_refuses_non_loopback_bind(self):
        with self.assertRaises(ValueError):
            BridgeConfig(host="0.0.0.0", port=9001, token="t")

    def test_config_requires_token(self):
        with self.assertRaises(ValueError):
            BridgeConfig(host="127.0.0.1", port=9001, token="")

    def test_valid_config(self):
        cfg = BridgeConfig(host="127.0.0.1", port=9001, token="t")
        self.assertEqual(cfg.host, "127.0.0.1")
        self.assertEqual(cfg.port, 9001)


if __name__ == "__main__":
    unittest.main()
