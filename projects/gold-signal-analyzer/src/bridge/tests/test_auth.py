import os
import sys
import unittest

sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), "..")))
from mt5_bridge import auth  # noqa: E402


class AuthTests(unittest.TestCase):
    def test_valid_token_matches(self):
        self.assertTrue(auth.token_is_valid("abc123", "abc123"))

    def test_wrong_token_rejected(self):
        self.assertFalse(auth.token_is_valid("wrong", "abc123"))

    def test_missing_token_rejected(self):
        self.assertFalse(auth.token_is_valid(None, "abc123"))
        self.assertFalse(auth.token_is_valid("", "abc123"))

    def test_missing_expected_rejected(self):
        self.assertFalse(auth.token_is_valid("abc123", ""))


if __name__ == "__main__":
    unittest.main()
