# Phase 2.5 — Plan Review Gate · Cycle 2, Slice 1 (Read-Only Bridge Seam)

**Project:** gold-signal-analyzer · **Date:** 2026-07-24
**Under review:** `phase-2-cycle2-arch.md`
**Why adversarial:** this plan gates connectivity to the user's **LIVE / REAL Exness money account**. The gate's job is to find what is wrong, not to approve.
**Reviewers:** adversarial pass conducted by the orchestrator inline (QA-lens + Audit-lens). Two specialist reviewer agents were launched but terminated on transient mid-stream API errors; to conserve the user's session budget the review was completed inline rather than re-spawned a third time. The findings below hold the plan to live-money severity.

## Single synthesized verdict: **PASS-WITH-CONDITIONS**

The structural RM2 design (fixed `DISPATCH` table, no `getattr` reflection path, SDK import isolated + lazy in one gateway module, AST-based no-order proof runnable without the MT5 SDK) is the right shape. But the no-order proof **as specified in Phase 2 only inspects `mt5_gateway.py`** and asserts the "sole importer" / "no reflection" / "no subprocess" properties in prose rather than in the test. For a live-money launch gate those are bypass holes. The conditions below must be built into the RM2 test before the gate can be trusted.

---

## Findings (each with the required change)

### Group A — RM2 no-order proof completeness (the launch gate)

**A1 [BLOCKER] — Prove the property over the ENTIRE bridge package, not just the gateway.**
The Phase-2 AST test parses only `mt5_gateway.py`. The invariant "the gateway is the SOLE module importing MetaTrader5" is prose, not tested. A future edit adding `import MetaTrader5` + `order_send` to `handlers.py` or a new module would pass.
*Required:* the test walks EVERY `.py` under the bridge package; asserts **exactly one** file imports MetaTrader5 (the gateway); runs the allowed/denied-symbol AST proof over **every** file that references an `mt5` alias; asserts no `import MetaTrader5` / `from MetaTrader5 import` appears outside the gateway.

**A2 [BLOCKER] — Close reflection/dynamic-dispatch bypass.**
The AST proof collects literal `mt5.<name>` accesses; `getattr(mt5, s)()`, `__import__`, `importlib`, `eval`/`exec`, `globals()[...]`, or aliasing (`m = mt5; m.order_send()`) reach order functions without a literal node. Phase 2 says "no `getattr(mt5, command)`" but does not test it.
*Required:* the test asserts the bridge source contains no `getattr` / `__import__` / `importlib` / `eval` / `exec` / `globals()[` / `vars(` targeting the SDK, and no second alias binding of the mt5 module beyond the single `import MetaTrader5 as mt5`.

**A3 [BLOCKER] — Forbid subprocess/exec escape.**
Read-only-to-mt5 ≠ no-order-capability if the process can shell out to an order-capable script.
*Required:* assert no `subprocess`, `os.system`, `os.popen`, `os.exec*`, `pty` usage anywhere in the bridge source. A read-only market-data bridge has no legitimate need to spawn processes.

**A4 [MAJOR] — Enumerate gateway `def` names; none may be order-shaped.**
The FakeGateway runtime test (AC-38.6) proves *dispatch* is closed, not that the *real* gateway lacks an order method. Since we can't import the real gateway (no SDK), inspect it via AST.
*Required:* AST-enumerate all `def` names in `mt5_gateway.py`; assert none matches `/order|send|trade|position|deal|modify|close|buy|sell|login/i`.

**A5 [MAJOR] — Cover ALL `initialize()` and `login` sites, not one.**
AC-38.5 finds "the `initialize(...)` node" (singular). Assert over **every** `mt5.initialize` call node (kwargs ⊆ {path, portable, timeout}); assert `mt5.login` referenced **zero** times; add `login` to `DENIED_SDK_SYMBOLS` explicitly (Phase 2 listed it only as an excluded kwarg).

### Group B — testability in THIS environment (no MT5, no pytest)

**B1 [MAJOR] — The http-server auth/bind/protocol tests must run with a fake/None gateway.**
AC-37.2 (bind 127.0.0.1), AC-37.3 (401 on wrong token), AC-39.x (protocol) are producible here only if the server module imports without MetaTrader5. It does (SDK isolated in the gateway). *Required:* the server accepts an injected gateway; tests start it on `127.0.0.1:0` with a FakeGateway and hit it via `urllib` — real green output, no SDK, no live terminal.

**B2 [MAJOR] — DPAPI package.** `System.Security.Cryptography.ProtectedData` is a separate NuGet package (not in the net8.0 BCL). The build must add it; the Set/Get/Delete round-trip is headless-runnable on Windows. Verify with real `dotnet test` output.

**B3 [MINOR] — Constant-time token compare must be asserted** (`hmac.compare_digest`, not `==`) via AST.

### Group C — credential & governance (audit-lens)

**C1 [MAJOR] — Prefer stdin handshake over child env var for the token.** A child process env block is readable by same-user processes/crash dumps on Windows. Pass the token via the stdin readiness handshake; never a command-line arg (already forbidden). If env is used, document the residual risk.

**C2 [MAJOR] — Change-control wrapper on the allowlist (ott-app lesson: a switch without change-control is theatre).** The expected-set test locks the allowlist, but a future commit could edit the allowlist AND its expected set together. *Required:* add `CODEOWNERS` covering the bridge python dir + the RM2 test, and run the RM2 test inside the blocking build/test gate so a red RM2 test blocks the whole suite (not just documented intent).

**C3 [MAJOR] — No live call may exist this slice; prove it.** Enforcement that a live `initialize()` cannot ship while RM2 is red must be structural, not a note. This slice defers live connect entirely; *required:* a test asserts the gateway performs **no** MT5 call at import/module-load, and the live-connect PR of the next slice is explicitly blocked on RM2 green.

**C4 [MINOR] — Bridge-command audit logging** (who/what/when) and a disconnect/kill-switch are required controls for the *live* slice; named now, deferred with the live adapter.

### Group D — secrets-by-construction

**D1 [MAJOR] — Extend Cycle-1 posture to all new surface.** Add `bridgetoken` to `SecretDenylist` (masking enricher covers it); `.gitignore` must cover `__pycache__/`, `*.pyc`, `.venv/`; no DTO/config/wizard field name may match the denylist; a reflection test asserts the wizard model has no `/password|pwd|investor|otp|withdraw/i` member.

---

## Conditions to clear before the gate is GREEN (carried into the build)

1. RM2 test strengthened per A1–A5 (package-wide AST, single-importer, no-reflection, no-subprocess, gateway def-name enumeration, all initialize/login sites, `login` in DENY set).
2. Server auth/bind/protocol tests run here with a FakeGateway (B1); DPAPI round-trip green with the ProtectedData package (B2).
3. Token via stdin handshake (C1); `bridgetoken` in denylist + `.gitignore` python coverage (D1).
4. A test asserts the gateway makes no live MT5 call this slice (C3); CODEOWNERS + RM2-in-blocking-gate (C2).
5. §9 reconciliation, wizard-model no-secret reflection test as specified.

**Most important single change:** the RM2 no-order-API test must prove the no-order property over the **entire bridge package** with the reflection / subprocess / second-importer holes closed — an AST pass over `mt5_gateway.py` alone is not sufficient evidence for a live-money gate.

---

## Reviewer corroboration (both specialist agents completed on retry — both FAIL)

After this inline gate was drafted, the QA and Auditor specialist agents (which had failed on transient stream errors) completed on retry. **Both returned FAIL**, independently converging on the same core blocker (whole-package / sole-importer / form-restricted AST proof) and adding two critical holes this inline pass had MISSED — both now folded into the build conditions:

- **`from MetaTrader5 import order_send` → bare `order_send(...)` [BLOCKER, QA].** A `Name` call node, invisible to an `mt5.`-attribute scan. The test MUST forbid any `from MetaTrader5 import …` (allow only `import MetaTrader5 as mt5`) and any rebinding of the `mt5` name (`m = mt5`).
- **`initialize()` POSITIONAL auth bypass [BLOCKER, QA].** MT5's real signature is `initialize(path, login=, password=, server=, …)` — `login/password/server` can be passed **positionally**: `mt5.initialize(path, 12345, "pw", "Exness-Real")` has zero keywords and passes a keyword-only check. The test MUST assert every `initialize` call site carries **no positional arg beyond `path`** and none of the `login/password/server` keywords.
- **AC-37.2 loopback [MAJOR, QA].** Must assert `server.server_address[0] == "127.0.0.1"` by introspection; "a loopback client got a response" would pass even against a `0.0.0.0` LAN-exposed bind. The non-loopback-peer-reject claim is not provable this slice and is honestly deferred.
- **Constant-time compare [MAJOR, QA].** Assert via AST that the token path uses `hmac.compare_digest` and contains no `==`/`!=` on the token.
- **Token transport [BLOCKER, Audit].** Mandate the **stdin handshake as the SOLE transport**; prohibit the env-var route (child env block is readable by same-user processes and captured in WER crash dumps).
- **Change-control wrapper [BLOCKER, Audit].** `CODEOWNERS` on `commands.py` / `mt5_gateway.py` / the RM2 test + a merge-blocking CI check that runs the RM2 test on Python 3.9 and 3.14 and fails if red **or missing** — the switch without the wrapper is theatre (ott-app lesson).
- **Secrets-by-construction [BLOCKER, Audit].** Extend the recurring secret-scan to `python/**` + `ConnectionOptions`; `bridgetoken` in `SecretDenylist`; Python-side must never write the token to stderr/exception text.
- **Per-command audit log [MAJOR, Audit].** The dispatcher emits a structured allow/deny record (who/what/when, masked) for every command.
- **Live-call prevention [BLOCKER, Audit].** A test asserts the gateway makes NO live MT5 call at import/module load this slice; the next slice's live-connect is CI-blocked on RM2 green.
- **AC-36.3 reclassified [QA].** The §9 marker edit is a non-test build item (comment change), not counted as a green-tested requirement.

**Merged single verdict across all three passes: PASS-WITH-CONDITIONS** — the read-only architecture is sound; it advances to build **only** with every condition above built into the RM2 test and its governance wrapper. The two agent FAILs are subsumed: their blockers become the build's acceptance criteria, which the Phase-3 test must demonstrate green.

Iteration 1 of ≤3. Conditions are concrete and buildable; no re-loop of the design needed — proceed to Phase 3 building them in, and re-verify at Phase 4 that the RM2 test actually asserts each one.
