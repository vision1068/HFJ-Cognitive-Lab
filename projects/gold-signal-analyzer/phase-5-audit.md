# Phase 5 — Audit (Security, Compliance, Governance)

**Project:** gold-signal-analyzer · **Cycle:** 1 (Foundation) · **Date:** 2026-07-24
**Mandate:** Independent re-verification of the security posture against actual code + real command output. Confirm the 3 coordinator-mandated checks; confirm RM1–RM5 are deferred (recorded, not dropped).

---

## Verdict: **PASS**

Re-verified against source and a self-run build/test (0 warnings/0 errors, 33/33 at review time; 36/36 after the QA R6 fix). All three mandated checks **CONFIRMED**. No blocking governance gap. Residual items are correctly-deferred roadmap conditions plus non-blocking Cycle-2 advisories. B3c audit-honesty wording is accurate — no over-claim.

## (a) No credentials anywhere — CONFIRMED

Full-tree grep (`password|apikey|secret|bottoken|otp|withdrawal|pwd|credential|accesstoken|refreshtoken|connectionstring|investorpassword`) matched **only** no-secret-posture doc comments, `SecretDenylist` itself, and the throwing `ICredentialStore` binding. **Zero secret fields, columns, or config keys.**
- `AppConfiguration` — no secret property; notification channels are `bool` only; `Mt5TerminalPath`/`ServerName` non-secret. Fixed-schema → **structurally incapable**.
- `ConfigExportService`/`ConfigExportDto` — whitelist projection, no secret property to emit.
- `AppSettingsRepository` — KV table rejects denylisted keys at **write time** → **convention-enforced + test-guarded** (a KV table can physically hold any key; wording is honest, not "structural").
- `NotImplementedCredentialStore` — every member throws; no null/empty no-op (A07). **Now also test-guarded** (Phase-4 R6 fix).
- **No committed `appsettings*.json` exists at all** — strongest posture; config binds from host sections / non-committed local files.
- **B3c honesty check: CLEAN** — fixed-schema models say "incapable" (true); the KV path says "rejects denylisted keys" (convention+test). No residual over-claim.

## (b) `.gitignore` covers secrets AND .NET/db/log artefacts — CONFIRMED

`git check-ignore` from repo root, all IGNORED (exit 0): `.env`, `secrets.json`, `*.pem`, `*.key` (secrets) and `bin/ obj/ *.db *.db-shm *.db-wal *.db-journal *.user .vs/ TestResults/ **/logs/` (.NET/db/log). Phase-2.5 R4 PARTIAL is now **closed**.

## (c) Roadmap defers ALL live MT5 connectivity — CONFIRMED

- No `MetaTrader5` package (csproj scan: only MS.Extensions, Serilog, EF Core Sqlite, CommunityToolkit.Mvvm, xunit). No socket/HTTP client lib.
- Grep for `Socket|TcpClient|NamedPipe|order_send|OrderSend|PlaceOrder|NetworkStream|HttpClient|WebSocket|Process.Start` → only comments + UI placeholder labels. No executable connect/order code.
- `IMarketDataProvider` exposes **no order/trade method**; `Ports/` has no `IOrderExecutor`/`ITradeExecutor` — no-execution invariant is structural in the .NET graph.
- `NullMarketDataProvider` scalar methods throw `NotConnectedException` (no fabricated Tick/SymbolSpec, NFR-5/R5); collections empty; no terminal I/O.
- Stub-shell projects are literal `AddGsa* => services` no-ops; `MT5Bridge` explicitly "No code here opens a socket."

## Masking enricher (backstop) — CONFIRMED
Recurses `StructureValue`/`SequenceValue`/`DictionaryValue`; exact case-insensitive match via `SecretDenylist.IsSecret` (not substring) → `ServerName`/`Mt5TerminalPath` not over-masked (§39 auditability). Proven by `MaskingEnricherTests` (B2).

## RM1–RM5 — ALL RECORDED, NONE DROPPED, NONE PREMATURELY BUILT

| RM | Status | Evidence |
|----|--------|----------|
| RM1 bridge token → Credential Mgr/DPAPI | Deferred, recorded | `ICredentialStore` doc; binding throws. |
| RM2 Python bridge read-only allowlist | Deferred, recorded | `MT5Bridge/Marker.cs` names it; **the .NET no-order-port does NOT bind the Python side** — owed at Roadmap Phase 2. |
| RM3 permanent Exness credential prohibition | Deferred, recorded | `ConnectionMode` "both modes read-only; neither requests a trading password." |
| RM4 disclaimer = blocking versioned audited ack gate | Deferred, recorded | screen 17 is a nav placeholder only; no `DisclaimerAcknowledgement` persistence built. |
| RM5 SignalDisclosure wrapper | Deferred, recorded | no signal rendering in C1; owed when signals land. |

## OWASP Foundation-scope spot checks
- **A01** (auth bypass / R6) — PASS (store throws on every member; no bypass surface).
- **A02** (plaintext secret at rest / B3–B4) — PASS (no secret column/property; KV rejects; no committed config).
- **A04** (fail-loud config / ConfigValidationTests) — PASS (`OptionsValidationException` on bad config; `ValidateOnBuild + ValidateScopes`).
- **A09** (logging masks secrets / B2) — PASS (recursive exact-match enricher, test-proven).

## Residual risk / advisories (NON-BLOCKING — Cycle 2, do not gate go-live)
1. B4 scan is key-name based; a secret pasted as a **value** under a benign key, or in a non-`.json/.config` committed file, is not caught. Widen when Cycle-2 adds committed config.
2. `SecretDenylist` has broad exact tokens (`account`/`login`); exact-match keeps ops fields safe today but revisit granularity as connection/notification settings expand.
3. **RM2 is the single highest-value future control** — the strongest C1 invariant (no order path) is .NET-structural and does **not** propagate to the future `order_send`-capable Python bridge. Cycle 2 must land the read-only API allowlist **with a test asserting no order/trade API is reachable** before any bridge ships. Flagged now so it is not lost in build momentum.

**Bottom line:** Foundation is secure-by-construction as claimed; audit-honesty (B3c) wording is accurate; all deferrals documented not dropped; every mandated check confirmed against real code and real command output. **PASS.**
