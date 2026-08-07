# Gold Signal Analyzer — MT5 Connectivity Bridge (read-only scaffolding)

.NET 8 clean-architecture solution + a localhost-only Python MT5 bridge scaffold.
**Read-only scope:** no live terminal attach and no order/trade capability exist.

## Layout
| Project | Role |
|---------|------|
| `GoldSignalAnalyzer.Domain` | Pure value objects & enums (no dependencies). |
| `GoldSignalAnalyzer.Application` | Abstractions (`IMarketDataProvider`, `IMt5BridgeClient`, `IClock`, `ICredentialStore`) + pure logic (`GoldSymbolResolver`, `DataFreshnessMonitor`, `SignalGate`, `ExponentialBackoffPolicy`, `BridgeEndpoint`). |
| `GoldSignalAnalyzer.Infrastructure` | Adapters: `CsvHistoricalMarketDataProvider`, `MetaTrader5MarketDataProvider` (seam), `HttpMt5BridgeClient`, `DpapiCredentialStore`, `SystemClock`. |
| `GoldSignalAnalyzer.Testing` | **Test-only** `TestMarketDataProvider`, `ManualClock`. Not referenced by any production project — physically absent from a Release build. |
| `GoldSignalAnalyzer.Tests` | 82 xUnit tests. |
| `bridge/` | Python localhost bridge (stdlib) + 19 unittest tests. |

## Build & test (.NET)
```
dotnet build GoldSignalAnalyzer.sln -c Release
dotnet test  GoldSignalAnalyzer.sln -c Release
```

## Run & test (Python bridge)
```
cd bridge
python -m unittest discover -s tests -p "test_*.py"

# run the scaffold (fake data source, loopback only):
#   PowerShell:
#     $env:GSA_BRIDGE_TOKEN = "<strong-random-token>"
#     python run_bridge.py
# then: GET http://127.0.0.1:9001/health  with header  X-Bridge-Token: <token>
```

## Safety invariants (enforced structurally, verified by tests)
- **No fabrication** — the live seam throws unless Connected + symbol-mapped; freshness defaults to not-fresh; future-dated ticks are Stale.
- **No secrets in source** — connection config has no secret field; bridge token from `GSA_BRIDGE_TOKEN` only; DPAPI at rest.
- **Loopback only** — `BridgeEndpoint` / `BridgeConfig` refuse any non-loopback host.
- **Authenticated** — every bridge request needs `X-Bridge-Token`; missing/wrong → 401 (constant-time compare).
- **Read-only** — no order/trade surface anywhere.

## Enabling live (future, owner-approved only)
Live attach is intentionally NOT wired. A future cycle would provide a real
`IMt5BridgeClient` pointed at `run_bridge.py --live` (which uses the guarded
`Mt5MarketDataSource` requiring the `MetaTrader5` package on a Windows host with
a running terminal). Do not enable without a named approver and the auditor's
carried-forward conditions closed.
