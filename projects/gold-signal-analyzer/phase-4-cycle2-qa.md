# Phase 4 — QA · Cycle 2, Slice 1 (Read-Only Bridge Seam)

**Project:** gold-signal-analyzer · **Date:** 2026-07-26
**Method:** independent re-run on both interpreters **plus** reading what each
assertion actually checks and mapping it to the Phase 2.5 blocker it must close.
Green output alone was NOT accepted as evidence (lessons-learned 2026-07-24: an
untested control with a plausible-but-false coverage claim sails through unless
someone reads the assertion). **Verdict: PASS.**

---

## 1. Re-run evidence

- Python 3.9.13 and 3.14.6: `unittest discover` → **19/19 OK** on both.
- .NET: `dotnet test` → **45/45**; filtered new-class run → **9/9**.

## 2. RM2 10-assertion audit — each mapped to the Phase 2.5 condition it closes

I read `test_readonly_allowlist.py` line by line and confirmed each assertion
tests the property it claims, over the surface it claims (whole package vs.
gateway-only), and that a plausible bypass would actually trip it.

| RM2 test | What it actually asserts (verified) | Closes |
|---|---|---|
| `test_01` | Walks **every** `.py` in the package; exactly `{mt5_gateway.py}` imports MetaTrader5. A new module importing the SDK breaks it. | **A1** (whole-package, sole importer) |
| `test_02` | Over every file: forbids `from MetaTrader5 import …` (lines 140-141) → catches the **bare `order_send()`** blocker; requires `import MetaTrader5 as mt5`; forbids `m = mt5` rebind (148-151). | **A1 + A2 + QA bare-import blocker** |
| `test_03` | AST over every `mt5.<attr>`: `∈ ALLOWED_SDK_SYMBOLS ∪ TIMEFRAME_*`; DENY symbols 0×; no `TRADE_ACTION_*`. `login` sits in `DENIED_SDK_SYMBOLS`, so any `mt5.login` reference fails here. | **A1 (form-restricted proof) + A5** |
| `test_04` | Forbids `getattr/setattr/delattr/eval/exec/vars/globals/__import__/compile` as call names and `importlib` imports in source. `globals()[…]` trips because `globals()` is a Call with `func.id=="globals"`. | **A2** (reflection/dynamic-dispatch bypass) |
| `test_05` | Forbids `subprocess`/`pty` imports and `os.system`/`os.popen`/`os.exec*` in source. | **A3** (subprocess/exec escape) |
| `test_06` | Over **every** `mt5.initialize` site: `len(node.args) ≤ 1` (line 219) → catches the **positional-auth bypass** `initialize(path, 12345, "pw", "srv")`; no `login/password/server` kwarg; kwargs ⊆ `{path,portable,timeout}`. | **A5 + QA positional-auth blocker** |
| `test_07` | AST-enumerates every `def` in `mt5_gateway.py`; none matches `/order|send|trade|position|deal|modify|close|buy|sell|login/i`. | **A4** (real gateway has no order-shaped method) |
| `test_08` | `ALLOWED_COMMANDS == EXPECTED_READONLY_SET`; `DISPATCH.keys() == ALLOWED_COMMANDS`; `DENIED ∩ ALLOWED == ∅`. | allowlist lock (**C2 anchor**) |
| `test_09` | Runtime: `dispatch("order_send", …)` → `ok:false, error:"command_not_allowed"`, `gateway.calls == []`; `copy_rates_from_pos` routes to fake read exactly once. | runtime dispatch closed (**AC-38.6 / C3 runtime**) |
| `test_10` | AST: no `mt5.*` call runs at module load (function bodies = call-time, excluded; class bodies recursed). `mt5 = None` is an `Assign`, not a call → passes correctly. | **C3** (no live call this slice) |

Server-protocol suite corroborates the B-group conditions:
- `test_binds_loopback_only` — `server_address[0] == "127.0.0.1"` **and** `!= "0.0.0.0"` by introspection → **AC-37.2 (QA MAJOR)**, closes the "0.0.0.0 would still answer a loopback client" hole.
- `test_wrong_token_is_401` / `test_absent_token_is_401` — gateway untouched → **AC-37.3**; runs with a FakeGateway → **B1**.
- `test_token_compare_is_constant_time` — AST proves `_authorized` uses `hmac.compare_digest` and contains **no `==`/`!=`** on the token → **B3**.
- `test_protocol_version_mismatch_is_rejected` → **AC-39.2**.

.NET port-shape gate (`MarketDataPortShapeTests`) locks the §9 reconciliation:
no order/trade/position/send member; no `IObservable<>`/`IAsyncEnumerable<>`;
`SubscribeTicks` absent; the exact 6-method + 2-property shape is asserted.

## 3. Honest coverage boundary (stated, not hidden)

The RM2 proof reasons over **source text via AST** — it proves the *shipped source*
cannot name or reach an order API, on 3.9 and 3.14, without the MT5 SDK installed.
It does **not** (and cannot here) exercise the real gateway against a live terminal.
That runtime layer is correctly deferred to the live-connect slice; `test_07`
(def-name enumeration) and `test_10` (no load-time call) are the structural
stand-ins until then. Same honest posture as ott-app's no-SDK harness.

## 4. One QA note carried forward (not a blocker)

`EXPECTED_READONLY_SET` is duplicated inside the test file. A single coordinated
commit editing `commands.py` **and** the test's expected set together would pass
CI — which is exactly why `test_08`'s lock is only trustworthy behind the
**CODEOWNERS review on both files** (now created) once branch protection is armed.
The wrapper exists; it must be *armed* by the owner (Phase 6 condition). Until then
the lock is real but the change-control around it is declared-not-binding.

**Phase 4 verdict: PASS.** Every Phase 2.5 blocker (A1-A5, B1-B3, C3) maps to a
named assertion whose logic I have read and confirmed non-trivial.
