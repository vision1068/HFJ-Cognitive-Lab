import os
import sys
import unittest

sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), "..")))
from mt5_bridge.symbols import rank_gold, score_gold  # noqa: E402


class SymbolTests(unittest.TestCase):
    def test_gold_variants_detected(self):
        for raw in ("XAUUSD", "GOLD", "XAUUSDm", "XAUUSD.a", "XAUUSD.pro", "GOLDmicro", "XAU/USD"):
            self.assertGreater(score_gold(raw), 0, raw)

    def test_non_gold_rejected(self):
        for raw in ("EURUSD", "XAGUSD", "XPTUSD", "US30", ""):
            self.assertEqual(score_gold(raw), 0.0, raw)

    def test_exact_xauusd_highest(self):
        self.assertEqual(score_gold("XAUUSD"), 1.0)
        self.assertGreater(score_gold("XAUUSD"), score_gold("XAUUSDm"))

    def test_rank_prefers_clean_symbol(self):
        ranked = rank_gold(["XAUUSD.pro", "XAUUSDm", "XAUUSD", "EURUSD", "GOLD", "XAGUSD"])
        self.assertEqual(ranked[0][0], "XAUUSD")
        names = [r[0] for r in ranked]
        self.assertNotIn("EURUSD", names)
        self.assertNotIn("XAGUSD", names)

    def test_detects_when_only_gold_exists(self):
        ranked = rank_gold(["GOLD", "EURUSD"])
        self.assertEqual(ranked[0][0], "GOLD")


if __name__ == "__main__":
    unittest.main()
