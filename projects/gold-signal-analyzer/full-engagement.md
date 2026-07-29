# Full Engagement — Gold Signal Analyzer: MT5 Connectivity Bridge (read-only)

**Pattern A (full engagement), cycle-scoped.** Independent, from-scratch build
in worktree `agents/mt5-connectivity-bridge-gold-signal`. No code sourced from
the sibling `gold-signal-analyzer-foundation` / `C:/gsa-f`. No live MT5 attach.

## [CEO] (Phase 1)
Connectivity is the trust foundation of the product — build the safety envelope
(no fabrication, no secrets, loopback-only, veto-on-stale) before switching on a
real broker connection. 7 success criteria set. Proceed. → `phase-1-ceo.md`

## [Architect] (Phase 2)
.NET 8 clean architecture (Domain ← Application ← Infrastructure) + a separate
test-only assembly; out-of-process Python loopback bridge holding the MT5 seam.
Component/state/sequence/decision diagrams in Mermaid. Gate 1 PASS. → `phase-2-arch.md`

## [QA + Auditor] (Phase 2.5 — Plan Review Gate, adversarial)
10 findings (Q-1…Q-5, A-1…A-5) raised against the plan and folded into the design
before coding: Unknown-default gate, future-skew rule, Reconnecting state, no-secret
config, dual loopback guard, read-only Python surface, separate test assembly.
Single merged verdict **PASS-WITH-CONDITIONS**. → `phase-2.5-plan-review.md`

## [Backend] + [Middleware] (Phase 3)
Implemented: Domain value objects; `GoldSymbolResolver` (FR-9); `DataFreshnessMonitor`
+ `SignalGate` (FR-12); `ExponentialBackoffPolicy` (NFR-4); `CsvHistoricalMarketDataProvider`
(full) + `MetaTrader5MarketDataProvider` (seam) + `TestMarketDataProvider` (gated);
`HttpMt5BridgeClient` (loopback+token+heartbeat) + `BridgeEndpoint`; `DpapiCredentialStore`;
and the Python bridge (`config/auth/symbols/market_source/server`). Frontend & CRM: N/A.
Gate 2 PASS (bridge really starts and serves over loopback). → `phase-3-tech.md`

## [QA] (Phase 4)
101 tests green (82 .NET Release + 19 Python), full FR/NFR→test traceability,
happy/boundary/failure each covered, all Phase-2.5 conditions satisfied.
Gate 5 PASS. → `phase-4-qa.md`

## [Auditor] (Phase 5)
Secret grep clean; no-secret config; loopback + token enforced both sides;
read-only Python surface (no order calls); test provider physically absent from
production. INV-1/2/3 upheld. Gate 3 PASS. Two HIGH items carried forward onto
any future live-enable cycle. → `phase-5-audit.md`

## [CEO Final Decision] (Phase 6)
**APPROVE** the read-only scaffolding slice. Independent validation spot-check
re-ran the tests and the gating `ls`. Gate 6 intentionally reserved — nothing
goes live this cycle. → `phase-6-ceo.md`

## Deliverables map
```
projects/gold-signal-analyzer/
  brief.md, phase-1..6, phase-2.5-plan-review.md, full-engagement.md
  .gitignore
  src/
    GoldSignalAnalyzer.sln
    GoldSignalAnalyzer.Domain/         value objects, enums
    GoldSignalAnalyzer.Application/    IMarketDataProvider, IMt5BridgeClient,
                                       BridgeEndpoint, GoldSymbolResolver,
                                       DataFreshnessMonitor, SignalGate, backoff
    GoldSignalAnalyzer.Infrastructure/ Csv + MT5-seam providers, HttpMt5BridgeClient,
                                       DpapiCredentialStore, SystemClock
    GoldSignalAnalyzer.Testing/        TestMarketDataProvider, ManualClock (test-only)
    GoldSignalAnalyzer.Tests/          82 xUnit tests
    bridge/                            Python localhost bridge scaffold + 19 tests
    README.md
```

## How to verify (commands)
```
cd projects/gold-signal-analyzer/src
dotnet test GoldSignalAnalyzer.sln -c Release           # 82 passed
cd bridge && python -m unittest discover -s tests       # 19 passed
```

## Explicitly deferred (needs a future owner-approved cycle)
- Real MT5 terminal attach + live market data (live `MetaTrader5MarketDataProvider` wiring, bridge `--live`).
- Signal-generation strategy, desktop UI shell, installer/packaging, CI/CD.
