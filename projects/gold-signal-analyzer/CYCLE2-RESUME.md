# Cycle 2 — Resume Checkpoint (Slice 1: Read-Only Bridge Seam)

**Paused:** 2026-07-24, mid Phase 3 build, at user's usage-limit reset. Session-tokens-only mode (no subagents).
**Baseline (verified this session):** `dotnet build` 0/0, `dotnet test` 36/36 green. Branch `gold-signal-analyzer-foundation` (Cycle-1 commit 670f004). Nothing new committed yet.

## Done this session
- **Phase 2 (Architect):** `phase-2-cycle2-arch.md` — §9 reconciliation decision (ratify 8-member read-only port, exclude `SubscribeTicks`), MT5 bridge boundary (child Python proc, loopback HTTP, ephemeral port, token via DPAPI), RM2 structural allowlist design, 12 machine-checkable build tasks, 2 Mermaid diagrams. Read in full and validated.
- **Phase 2.5 (Plan Review Gate — adversarial):** `phase-2.5-cycle2-review-gate.md`. Merged verdict **PASS-WITH-CONDITIONS**. Inline review + BOTH specialist agents (QA + Auditor) completed on retry, both FAIL, all blockers folded into build conditions. Critical holes to close (below).
- **Build started:** `src\GoldSignalAnalyzer.MT5Bridge\python\bridge\__init__.py` created (module docstring stating the RM2 invariant). Nothing else built yet.

## RM2 test — MANDATORY conditions (the launch gate; build these exactly)
The no-order test (`python\bridge\tests\test_readonly_allowlist.py`, stdlib `unittest`, no MetaTrader5, no pytest, run on 3.9 AND 3.14) must assert:
1. Walk EVERY `.py` in the bridge package; assert exactly ONE file (`mt5_gateway.py`) imports MetaTrader5.
2. Forbid `from MetaTrader5 import …` anywhere (only `import MetaTrader5 as mt5` allowed); forbid rebinding the `mt5` name (`m = mt5`). **(catches bare `order_send()`)**
3. AST over every file referencing the `mt5` alias: accessed `mt5.*` symbols ⊆ ALLOWED_SDK read set; DENY symbols (`order_send/check/calc_*`, `positions_*`, `history_*`, `market_book_*`, `login`) referenced 0×; no `TRADE_ACTION_*`.
4. Forbid reflection/exec primitives in bridge source: `getattr`, `__import__`, `importlib`, `eval`, `exec`, `globals()[`, `vars(`.
5. Forbid process-spawn: `subprocess`, `os.system`, `os.popen`, `os.exec*`, `pty`.
6. Every `mt5.initialize(...)` call: NO positional arg beyond `path` AND none of `login/password/server` kwargs. **(catches positional auth bypass)**
7. Enumerate `def` names in `mt5_gateway.py`; none matches `/order|send|trade|position|deal|modify|close|buy|sell|login/i`.
8. `ALLOWED_COMMANDS == EXPECTED_READONLY_SET` (locked); `set(DISPATCH.keys()) == ALLOWED_COMMANDS`; `DENY ∩ ALLOW == ∅`.
9. Runtime: FakeGateway; `command="order_send"` → `ok:false, error:"command_not_allowed"`, `gateway.calls == []`; `copy_rates_from_pos` → routes to fake read once.
10. Gateway makes NO live MT5 call at import/module load (module-level has no `mt5.*` call).

## Remaining build tasks (priority order — RM2 gate first)
1. Python bridge package: `commands.py` (ALLOWED_COMMANDS frozenset + DENIED_SDK_SYMBOLS + DISPATCH), `handlers.py` (read-only, gateway-injected, account_info monetary-field suppressed), `mt5_gateway.py` (lazy `import MetaTrader5 as mt5` inside `connect()`; read-only methods; `initialize(path=...)` path-only), `server.py` (ThreadingHTTPServer bind `127.0.0.1:0`, token via STDIN sole transport, `hmac.compare_digest`, per-command audit log, injectable gateway), `audit.py`, `run_bridge.py` (stdin token → start server → stdout `{"ready":true,"port":N}`).
2. `tests/test_readonly_allowlist.py` (the 10 assertions above) — **THE GATE**.
3. `tests/test_server_protocol.py` — FakeGateway; allow round-trip, deny 4xx, malformed, wrong-token 401, protocolVersion mismatch, `server_address[0]=="127.0.0.1"` introspection.
4. Run: `python -m unittest discover -s src\GoldSignalAnalyzer.MT5Bridge\python` on 3.9 and 3.14 — capture real output.
5. §9 reconcile: replace `[NEEDS CLARIFICATION]` in `IMarketDataProvider.cs` with resolved note; add `MarketDataPortShapeTests.cs` (no order/trade member, no `IObservable`/`IAsyncEnumerable`). Run `dotnet test`.
6. `WindowsCredentialStore` (DPAPI) replacing `NotImplementedCredentialStore` — needs NuGet `System.Security.Cryptography.ProtectedData`; register in Infrastructure DI; Set/Get/Delete round-trip test (headless Windows). Add `bridgetoken` to `SecretDenylist` + masking test.
7. Governance: `CODEOWNERS` on the 3 RM2 paths; CI workflow running the RM2 test on 3.9/3.14 (merge-blocking, fails if red OR missing) — **flag to user: enabling branch protection is a repo-Settings toggle they must do (see market-compass lesson)**. Extend recurring secret-scan to `python/**`. `.gitignore`: `__pycache__/`, `*.pyc`, `.venv/`.

## Deferred to later Cycle-2 slices (honest, with reason)
- **.NET `Mt5BridgeClient : IMarketDataProvider`** (loopback HTTP client + child-proc lifetime + reject-non-loopback-host). Larger; not the launch gate (RM2 is Python-side). FR-37 .NET half.
- **Connection Wizard model (FR-40)** — a "Should", not the gate.
- **Live `MetaTrader5MarketDataProvider`** (real `initialize()`/reads) — GATED on RM2 green + user confirms. Untestable here (no SDK).
- Indicators / scoring / freshness veto / reconnect runtime loop — downstream of a live read path.

## Open confirm-items for the user (non-blocking; surfaced already)
1. **Byte-exact §9:** `brief.md` does NOT contain literal §9 (only references it); the 44-section source spec isn't in the repo. Reconciliation used the faithful reconstruction (read-only preserved). To make it truly byte-exact, **paste the literal §9 text**.
2. **No-password / Mode A default:** the LIVE-safe posture is the tool holds no MT5 password and cannot `login()`; you log into the MT5 terminal yourself and the bridge attaches read-only. Confirm acceptable.

## Then: Phase 4 QA (re-run + read what each RM2 assertion actually checks), Phase 5 Audit (RM1/RM2/RM3 + governance wrapper), Phase 6 CEO sign-off for the slice, Phase 7 retrospective → append to `.claude\memory\lessons-learned.md`.
