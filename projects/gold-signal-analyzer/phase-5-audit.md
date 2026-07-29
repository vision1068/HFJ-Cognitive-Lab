# Phase 5 — Auditor: Security, Compliance & Governance

Scope: the connectivity-bridge slice as built. Frameworks applied at the level
relevant to a local desktop data bridge (OWASP-aligned secure-coding protocol;
company Constitution standards 1–6; permanent product invariants).

## Gate 3 — Security
| Area | Finding | Status |
|------|---------|--------|
| Secrets in source (NFR-1) | Source grep for password/secret/apikey/connectionstring across `*.cs`/`*.py` returned only one benign doc-comment ("never asks for a trading password"). No secret literals. | PASS |
| Secret at rest | MT5 read-only credential (if ever used) only via `DpapiCredentialStore` (DPAPI CurrentUser, hashed filename, plaintext never written). Bridge token only from `GSA_BRIDGE_TOKEN` env. | PASS |
| Config carries no secret | `Mt5ConnectionOptions` has no password/pwd/secret/investor/pin/token/apikey member — reflection test fails the build if added. | PASS |
| Network exposure (NFR-2) | Bridge refuses non-loopback bind (both .NET `BridgeEndpoint` and Python `BridgeConfig`); rejects `0.0.0.0`, private ranges, hostnames. | PASS |
| AuthN (A07) | Token required on every request; missing/empty/wrong → 401; constant-time compare (`hmac.compare_digest`). Proven by live E2E test. | PASS |
| Injection (A03) | No SQL/eval/dynamic-regex; symbol strings only URL-escaped into a query, parsed as JSON. | PASS |
| Dependency risk (A06) | .NET: one Microsoft package (`System.Security.Cryptography.ProtectedData` 8.0.0), no known HIGH/CRITICAL. Python scaffold: **stdlib only**; live `MetaTrader5` dep is platform-gated and out of scope this cycle. | PASS |
| `.gitignore` (past lesson) | Created at project root before scaffolding is committed — covers `bin/ obj/ .env creds/ *.token *.secret __pycache__/`. | PASS |
| Logging (A09) | `BridgeHealth`/credential store never carry or log secret values. | PASS |

**No HIGH/CRITICAL findings open.** Gate 3 PASS.

## Constitution compliance
| Std | Check | Status |
|-----|-------|--------|
| 1 Spec-first | IDed FR/NFR/AC in brief.md before code; no unresolved `[NEEDS CLARIFICATION]`. | PASS |
| 2 Test-before-claim | 101 tests with real output; no "should work" claims. | PASS |
| 3 Traceability | FR→test matrix in phase-4; IDs carried through phase docs. | PASS |
| 4 Secure-by-construction | Invariants are construction-time failures (loopback, no-secret, positivity, veto). | PASS |
| 5 Blocking gates | Gates 1,2,3,5 evaluated in order and passed; Gate 4/6 addressed below. | PASS |
| 6 Diagram-first | Component, state, sequence, and decision Mermaid diagrams in phase-2. | PASS |

## Permanent invariants
- **INV-1 (no trade execution):** the Python source exposes read-only methods only; no `order_*` surface anywhere; `.NET` has no trading API. **Verified by inspection.** PASS.
- **INV-2 (no PA/withdrawal creds):** no credential stored by scaffold; only an OS-protected read-only credential seam exists; config has no secret field. PASS.
- **INV-3 (no scraping):** no HTTP client targets Exness/TradingView or any external/private endpoint; the only outbound calls are to a local loopback bridge. PASS.

## Data residency / governance
All data stays on-box: .NET ↔ loopback bridge ↔ (deferred) local terminal.
Nothing leaves the machine; no cloud dependency; no PII processed in this slice.

## Gate 4 — Performance
Not a web app; no Lighthouse/bundle target applies. Relevant perf property is
non-blocking reconnect (bounded, jittered backoff — `BackoffTests`). Full suite
runs in <1s, indicating the pure logic is cheap. Documented as "measured where
meaningful"; live-latency measurement deferred with live attach. **PASS (scoped).**

## HIGH items for Gate 6 closure
1. **Live attach remains OUT until explicit owner go-ahead** — this is the single
   most important governance control; do not enable `--live` / a real
   `MetaTrader5MarketDataProvider` wiring without a fresh decision.
2. When live is later enabled: rotate/scope the bridge token, confirm the
   read-only (investor) connection cannot place orders at the broker level too,
   and add live data-latency monitoring (Gate 4 re-run).

## Auditor verdict: **APPROVE (read-only scaffolding)** with the two HIGH items
carried forward as conditions on any future live-enable cycle.
