"""
RM2 no-order-API test -- THE LAUNCH GATE (FR-38, phase-6-ceo cond #1).

Proves, WITHOUT importing MetaTrader5 and WITHOUT pytest, that no order / trade /
position API of the MT5 SDK is reachable from the bridge. Runs on stdlib unittest
on Python 3.9 and 3.14. Reads mt5_gateway.py as TEXT (never imports it).

The 10 mandatory assertions (CYCLE2-RESUME.md), each a separate test method:

  1. Exactly ONE file (mt5_gateway.py) imports MetaTrader5.
  2. No `from MetaTrader5 import ...`; only `import MetaTrader5 as mt5`; no
     rebinding of the `mt5` name.
  3. Accessed mt5.* symbols  subset of  allowed read set; DENY symbols 0x; no
     TRADE_ACTION_*.
  4. No reflection/exec primitives (getattr/__import__/importlib/eval/exec/
     globals()/vars()) in bridge source.
  5. No process-spawn (subprocess/os.system/os.popen/os.exec*/pty) in source.
  6. Every mt5.initialize(...) is path-only (no positional beyond path, no
     login/password/server kwarg).
  7. No gateway def name is order-shaped.
  8. ALLOWED_COMMANDS == expected locked set; DISPATCH keys == ALLOWED_COMMANDS;
     DENY  intersect  ALLOW == empty.
  9. Runtime: order_send -> rejected, gateway untouched; copy_rates_from_pos ->
     routed to the fake read exactly once.
 10. Gateway performs NO mt5.* call at import/module load.
"""

import ast
import os
import re
import unittest

import bridge
from bridge import commands


# --- locate bridge source (never import mt5_gateway) ------------------------
BRIDGE_DIR = os.path.dirname(os.path.abspath(bridge.__file__))


def _all_py_files():
    files = []
    for root, _dirs, names in os.walk(BRIDGE_DIR):
        for name in names:
            if name.endswith(".py"):
                files.append(os.path.join(root, name))
    return sorted(files)


def _source_py_files():
    """Bridge source modules -- excludes the tests directory."""
    return [f for f in _all_py_files()
            if os.path.basename(os.path.dirname(f)) != "tests"]


def _read(path):
    with open(path, "r", encoding="utf-8") as fh:
        return fh.read()


def _parse(path):
    return ast.parse(_read(path), filename=path)


GATEWAY_PATH = os.path.join(BRIDGE_DIR, "mt5_gateway.py")


def _imports_metatrader5(tree):
    for node in ast.walk(tree):
        if isinstance(node, ast.Import):
            for alias in node.names:
                if alias.name == "MetaTrader5" or alias.name.startswith("MetaTrader5."):
                    return True
        if isinstance(node, ast.ImportFrom) and (node.module or "").startswith("MetaTrader5"):
            return True
    return False


def _mt5_attr_accesses(tree):
    """All attr names accessed as ``mt5.<attr>`` in this tree."""
    out = []
    for node in ast.walk(tree):
        if (isinstance(node, ast.Attribute)
                and isinstance(node.value, ast.Name)
                and node.value.id == "mt5"):
            out.append(node.attr)
    return out


# The exact expected read-only allowlist (locked here; a change to
# commands.ALLOWED_COMMANDS must be mirrored here under review + CODEOWNERS).
EXPECTED_READONLY_SET = frozenset({
    "health",
    "terminal_info",
    "account_info",
    "symbols_get",
    "symbol_info",
    "symbol_info_tick",
    "copy_rates_from_pos",
    "copy_rates_range",
})


class FakeGateway:
    """Records read-method calls; has NO order/trade method to reach."""

    def __init__(self):
        self.calls = []

    def copy_rates_from_pos(self, symbol, timeframe, start, count):
        self.calls.append("copy_rates_from_pos")
        return [{"time": 0, "open": 1.0, "high": 1.0, "low": 1.0, "close": 1.0}]

    def terminal_info(self):
        self.calls.append("terminal_info")
        return {"company": "x"}

    def account_info(self):
        self.calls.append("account_info")
        return {"login": 12345678, "balance": 999.0, "currency": "USD"}


class Rm2ReadOnlyAllowlistTest(unittest.TestCase):

    # 1 --------------------------------------------------------------------
    def test_01_exactly_one_file_imports_metatrader5(self):
        importers = set()
        for path in _all_py_files():
            if _imports_metatrader5(_parse(path)):
                importers.add(os.path.basename(path))
        self.assertEqual(importers, {"mt5_gateway.py"},
                         "MetaTrader5 must be imported by exactly one file (the gateway); "
                         "got: %s" % sorted(importers))

    # 2 --------------------------------------------------------------------
    def test_02_only_aliased_import_no_rebind(self):
        for path in _all_py_files():
            tree = _parse(path)
            for node in ast.walk(tree):
                if isinstance(node, ast.ImportFrom) and (node.module or "").startswith("MetaTrader5"):
                    self.fail("`from MetaTrader5 import ...` is forbidden in %s" % path)
                if isinstance(node, ast.Import):
                    for alias in node.names:
                        if alias.name == "MetaTrader5":
                            self.assertEqual(alias.asname, "mt5",
                                             "MetaTrader5 must be imported as `mt5` in %s" % path)
                # forbid rebinding the mt5 module to another name (m = mt5)
                if isinstance(node, ast.Assign) and isinstance(node.value, ast.Name) and node.value.id == "mt5":
                    self.fail("rebinding the mt5 alias is forbidden in %s" % path)
                if isinstance(node, ast.AnnAssign) and isinstance(node.value, ast.Name) and node.value.id == "mt5":
                    self.fail("rebinding the mt5 alias is forbidden in %s" % path)

    # 3 --------------------------------------------------------------------
    def test_03_accessed_mt5_symbols_are_read_only(self):
        allowed = set(commands.ALLOWED_SDK_SYMBOLS)
        denied = set(commands.DENIED_SDK_SYMBOLS)
        for path in _all_py_files():
            accessed = _mt5_attr_accesses(_parse(path))
            for attr in accessed:
                with self.subTest(file=os.path.basename(path), attr=attr):
                    self.assertFalse(attr.startswith("TRADE_ACTION_"),
                                     "TRADE_ACTION_* reference forbidden: mt5.%s" % attr)
                    self.assertNotIn(attr, denied,
                                     "denied SDK symbol referenced: mt5.%s" % attr)
                    self.assertTrue(attr in allowed or attr.startswith("TIMEFRAME_"),
                                    "mt5.%s is not in the allowed read set" % attr)

    # 4 --------------------------------------------------------------------
    def test_04_no_reflection_or_exec_primitives(self):
        forbidden_calls = {"getattr", "setattr", "delattr", "eval", "exec",
                           "vars", "globals", "__import__", "compile"}
        for path in _source_py_files():
            tree = _parse(path)
            for node in ast.walk(tree):
                if isinstance(node, ast.Call) and isinstance(node.func, ast.Name):
                    self.assertNotIn(node.func.id, forbidden_calls,
                                     "reflection/exec primitive %s() forbidden in %s"
                                     % (node.func.id, path))
                if isinstance(node, ast.Import):
                    for alias in node.names:
                        self.assertNotEqual(alias.name.split(".")[0], "importlib",
                                            "importlib import forbidden in %s" % path)
                if isinstance(node, ast.ImportFrom):
                    self.assertNotEqual((node.module or "").split(".")[0], "importlib",
                                        "importlib import forbidden in %s" % path)

    # 5 --------------------------------------------------------------------
    def test_05_no_process_spawn(self):
        banned_imports = {"subprocess", "pty"}
        for path in _source_py_files():
            tree = _parse(path)
            for node in ast.walk(tree):
                if isinstance(node, ast.Import):
                    for alias in node.names:
                        self.assertNotIn(alias.name.split(".")[0], banned_imports,
                                         "process-spawn import %s forbidden in %s"
                                         % (alias.name, path))
                if isinstance(node, ast.ImportFrom):
                    self.assertNotIn((node.module or "").split(".")[0], banned_imports,
                                     "process-spawn import forbidden in %s" % path)
                # os.system / os.popen / os.exec*
                if (isinstance(node, ast.Attribute)
                        and isinstance(node.value, ast.Name) and node.value.id == "os"):
                    self.assertFalse(node.attr in ("system", "popen") or node.attr.startswith("exec"),
                                     "os.%s forbidden in %s" % (node.attr, path))

    # 6 --------------------------------------------------------------------
    def test_06_initialize_is_path_only(self):
        tree = _parse(GATEWAY_PATH)
        auth_kwargs = {"login", "password", "server"}
        found = 0
        for node in ast.walk(tree):
            if (isinstance(node, ast.Call)
                    and isinstance(node.func, ast.Attribute)
                    and isinstance(node.func.value, ast.Name)
                    and node.func.value.id == "mt5"
                    and node.func.attr == "initialize"):
                found += 1
                self.assertLessEqual(len(node.args), 1,
                                     "mt5.initialize takes no positional arg beyond path")
                kw = {k.arg for k in node.keywords if k.arg is not None}
                self.assertEqual(kw & auth_kwargs, set(),
                                 "mt5.initialize must carry no login/password/server kwarg")
                self.assertTrue(kw <= {"path", "portable", "timeout"},
                                "mt5.initialize kwargs must be a subset of {path,portable,timeout}; got %s" % kw)
        self.assertGreaterEqual(found, 1, "expected at least one mt5.initialize call site")

    # 7 --------------------------------------------------------------------
    def test_07_no_gateway_def_is_order_shaped(self):
        tree = _parse(GATEWAY_PATH)
        pattern = re.compile(r"order|send|trade|position|deal|modify|close|buy|sell|login", re.I)
        for node in ast.walk(tree):
            if isinstance(node, (ast.FunctionDef, ast.AsyncFunctionDef)):
                self.assertIsNone(pattern.search(node.name),
                                  "gateway def name is order-shaped: %s" % node.name)

    # 8 --------------------------------------------------------------------
    def test_08_allowlist_locked_and_dispatch_closed_and_deny_disjoint(self):
        self.assertEqual(set(commands.ALLOWED_COMMANDS), set(EXPECTED_READONLY_SET))
        self.assertEqual(set(commands.DISPATCH.keys()), set(commands.ALLOWED_COMMANDS))
        self.assertEqual(set(commands.DENIED_COMMAND_NAMES) & set(commands.ALLOWED_COMMANDS), set())
        self.assertEqual(set(commands.DENIED_COMMAND_NAMES) & set(commands.DISPATCH.keys()), set())

    # 9 --------------------------------------------------------------------
    def test_09_runtime_dispatch_is_closed(self):
        # denied command: rejected before any gateway call
        fake = FakeGateway()
        reply = commands.dispatch("order_send", {"symbol": "XAUUSD"}, fake)
        self.assertFalse(reply["ok"])
        self.assertEqual(reply["error"], "command_not_allowed")
        self.assertEqual(fake.calls, [])

        # allowed read command: routed to the fake read exactly once
        fake2 = FakeGateway()
        reply2 = commands.dispatch(
            "copy_rates_from_pos",
            {"symbol": "XAUUSD", "timeframe": "H1", "count": 10},
            fake2)
        self.assertTrue(reply2["ok"])
        self.assertEqual(fake2.calls, ["copy_rates_from_pos"])

    # 10 -------------------------------------------------------------------
    def test_10_no_mt5_call_at_module_load(self):
        tree = _parse(GATEWAY_PATH)

        def load_time_nodes(body):
            for node in body:
                if isinstance(node, (ast.FunctionDef, ast.AsyncFunctionDef)):
                    continue  # function body is call-time, not load-time
                if isinstance(node, ast.ClassDef):
                    # class-body statements run at load; recurse (skip methods)
                    for sub in load_time_nodes(node.body):
                        yield sub
                    continue
                yield node

        for node in load_time_nodes(tree.body):
            for inner in ast.walk(node):
                if (isinstance(inner, ast.Call)
                        and isinstance(inner.func, ast.Attribute)
                        and isinstance(inner.func.value, ast.Name)
                        and inner.func.value.id == "mt5"):
                    self.fail("gateway performs a module-load mt5.%s() call" % inner.func.attr)


if __name__ == "__main__":
    unittest.main()
