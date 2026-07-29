# Phase 4 — QA: Test Strategy & Evidence

## Strategy
Every FR/NFR maps to at least one automated test with real, re-runnable
command output — happy path + at least one boundary + at least one failure path.
Determinism is achieved with an injected clock (`ManualClock`) and injected RNG
(backoff), a stub `HttpMessageHandler` (bridge client), and an in-process
loopback HTTP server (Python E2E). No test requires a real MT5 terminal (NFR-10).

## Evidence — actual command output

**.NET (Release):**
```
> dotnet test GoldSignalAnalyzer.sln -c Release
Passed!  - Failed: 0, Passed: 82, Skipped: 0, Total: 82 - GoldSignalAnalyzer.Tests.dll (net8.0)
Build: 0 Warning(s), 0 Error(s)
```

**Python bridge:**
```
> python -m unittest discover -s tests -p "test_*.py"
Ran 19 tests in 0.5s
OK
```

**Production-gating (physical):**
```
> ls GoldSignalAnalyzer.Infrastructure/bin/Release/net8.0/  | grep GoldSignalAnalyzer
GoldSignalAnalyzer.Application.dll
GoldSignalAnalyzer.Domain.dll
GoldSignalAnalyzer.Infrastructure.dll        # <-- NO GoldSignalAnalyzer.Testing.dll
> grep -c Testing ...Infrastructure.deps.json
0
```

Total: **101 tests green** (82 .NET + 19 Python).

## Requirement → test traceability
| Req | Tests (file → cases) |
|-----|----------------------|
| FR-7 | `ProductionGatingTests.Connection_options_have_no_secret_field`; `Mt5ConnectionMode` (A/B) modelled |
| FR-8 | `BridgeEndpointTests` (loopback accept/reject); `HttpBridgeClientTests` (token on every call, 401, health); Python `test_config` (non-loopback bind refused), `test_server` (401 no-token / 200 with token) |
| FR-9 | `GoldSymbolResolverTests` (11 variants, metal exclusions, GOLD-only detection, manual override); Python `test_symbols` |
| FR-10 | `CsvProviderTests`, `TestProviderTests`, `Mt5ProviderSeamTests`; `ProductionGatingTests` (test provider absent from prod) |
| FR-11 | `Mt5ProviderSeamTests` (refuse when not connected / unmapped; returns real bridge tick only when connected); `HttpBridgeClientTests.Tick_404_returns_null_not_a_fabricated_price`; `Only_the_live_provider_is_flagged_live` |
| FR-12 | `FreshnessAndGateTests` (Unknown default, fresh→stale threshold, future-skew, exact banner, suppress on not-connected/unmapped/unknown) |
| NFR-1 | `Connection_options_have_no_secret_field`; source secret-grep clean; DPAPI store |
| NFR-2 | `BridgeEndpointTests`, Python `test_config`/`test_server` |
| NFR-4 | `BackoffTests` (grow, cap, jitter band, cap-under-jitter); `Mt5ProviderSeamTests` (Reconnecting state) |
| NFR-5 | fabrication-refusal tests (FR-11 row) + freshness future/unknown rules |
| NFR-10 | entire suite runs with no terminal; pure Domain/Application |

## Edge cases explicitly covered
- First evaluation before any tick (Unknown → suppressed).
- Clock skew / future-dated tick → Stale, not "fresh forever".
- Bridge reachable but terminal detached → Reconnecting, data refused.
- Bridge unreachable → heartbeat returns unhealthy, does not throw.
- Wrong / missing token → 401 (both .NET client handling and live Python server).
- Silver/platinum/palladium (contain USD) → not detected as gold.
- Malformed CSV row → rejected with `FormatException`.

## Plan-Review conditions (Phase 2.5) — all satisfied
Q-1…Q-5 and A-1…A-5 each have a named passing test (see traceability). The
PASS-WITH-CONDITIONS verdict is therefore upheld.

## Gate 5 (Testing) — PASS
Happy/boundary/failure covered; every AC maps to an executed check; evidence is
real command output. No P1 defects open.
