# Phase 2 — Architect: Architecture & Technology

## Technology choices
| Concern | Choice | Justification |
|---|---|---|
| Platform | **.NET 8, C# 12** | Windows desktop target; long-term-support; strong async + records + nullable. |
| Style | **Clean Architecture** (Domain ← Application ← Infrastructure) | Pure calc/logic isolated → unit-testable with no terminal (NFR-10). Dependencies point inward only. |
| Bridge | **Python + stdlib `http.server`**, localhost-only | The `MetaTrader5` package is Python-only and Windows-only; isolating it out-of-process keeps MT5 out of the .NET process and lets the terminal integration evolve independently (FR-8). Stdlib means the scaffold runs with zero pip installs. |
| Transport | **HTTP/JSON over loopback**, token header | Simple, debuggable, easy to authenticate and to bind to 127.0.0.1 (NFR-2). |
| Secrets | **Windows DPAPI** via `ICredentialStore` | OS-protected at rest; nothing in source/config (NFR-1). |
| Tests | **xUnit** (.NET) + **unittest** (Python) | Native, no extra infra; deterministic via injected clock/RNG. |

## Component / layer view

```mermaid
flowchart TB
    subgraph DotNet[".NET 8 process (Gold Signal Analyzer)"]
        direction TB
        subgraph Domain["Domain (pure, no deps)"]
            D1[NormalizedSymbol / BrokerSymbol / SymbolMapping]
            D2[MarketTick / Candle]
            D3[ConnectionState / FreshnessStatus / TimeFrame]
        end
        subgraph App["Application (pure logic + abstractions)"]
            A1[IMarketDataProvider]
            A2[IMt5BridgeClient / BridgeEndpoint]
            A3[GoldSymbolResolver  ·FR-9]
            A4[DataFreshnessMonitor + SignalGate  ·FR-12]
            A5[ExponentialBackoffPolicy  ·NFR-4]
            A6[IClock / ICredentialStore]
        end
        subgraph Infra["Infrastructure (adapters)"]
            I1[CsvHistoricalMarketDataProvider]
            I2["MetaTrader5MarketDataProvider (SEAM only)"]
            I3[HttpMt5BridgeClient  ·loopback+token]
            I4[DpapiCredentialStore  ·Windows]
            I5[SystemClock]
        end
    end
    subgraph TestOnly["GoldSignalAnalyzer.Testing (NOT referenced by production)"]
        T1[TestMarketDataProvider  ·FR-10 gated]
        T2[ManualClock]
    end
    subgraph Bridge["Local MT5 bridge — Python, 127.0.0.1 only"]
        B1[server.py  ·token-auth /health /symbols /tick /candles]
        B2["MarketDataSource seam"]
        B3[FakeMarketDataSource  ·tests/demo]
        B4["Mt5MarketDataSource (guarded, READ-ONLY, not run this cycle)"]
    end
    Terminal["MT5 terminal (OUT OF SCOPE — not attached)"]

    App --> Domain
    Infra --> App
    I2 --> A2
    I3 -. "HTTP/JSON loopback + X-Bridge-Token" .-> B1
    B1 --> B2 --> B3
    B2 -. guarded .-> B4
    B4 -. "read-only, deferred" .-> Terminal
    TestOnly --> App
```

The only path to a real terminal runs **B4 → Terminal**, which is a guarded
seam not exercised this cycle. The .NET process never links MT5.

## Connect + heartbeat + reconnect state machine (NFR-4)

```mermaid
stateDiagram-v2
    [*] --> Disconnected
    Disconnected --> Connecting: ConnectAsync()
    Connecting --> Connected: bridge healthy & terminal connected
    Connecting --> Reconnecting: bridge healthy, terminal NOT attached
    Connecting --> Faulted: bridge unhealthy/unreachable
    Connected --> Reconnecting: heartbeat fails
    Reconnecting --> Connected: heartbeat recovers
    Reconnecting --> Faulted: give up / fatal
    Connected --> Disconnected: DisconnectAsync()
    Faulted --> Connecting: retry after backoff
    note right of Reconnecting
        While NOT Connected the SignalGate
        suppresses → product stays Neutral
    end note
```

## Latest-tick request flow (happy path, live seam)

```mermaid
sequenceDiagram
    participant App as Application
    participant P as MetaTrader5MarketDataProvider
    participant C as HttpMt5BridgeClient
    participant S as Bridge (Python, loopback)
    App->>P: GetLatestTickAsync(XAU/USD)
    P->>P: EnsureConnected() + RequireBrokerSymbolFor()
    alt not Connected OR no mapping
        P-->>App: throw (never fabricate — FR-11/NFR-5)
    else
        P->>C: GetTickAsync("XAUUSD", XAU/USD)
        C->>S: GET /tick?symbol=XAUUSD  (X-Bridge-Token)
        alt 401
            S-->>C: 401
            C-->>P: UnauthorizedAccessException
        else 404 (no tick)
            S-->>C: 404
            C-->>P: null (no fabricated price)
        else 200
            S-->>C: {bid,ask,time}
            C-->>P: MarketTick
            P-->>App: MarketTick
        end
    end
```

## Signal-gate veto decision (FR-12)

```mermaid
flowchart TD
    Start([Evaluate]) --> Conn{State == Connected?}
    Conn -- No --> S1[Suppress: CONNECTION NOT READY — SIGNAL GENERATION PAUSED]
    Conn -- Yes --> Map{Gold symbol mapped?}
    Map -- No --> S2[Suppress: NO GOLD SYMBOL MAPPED — SIGNAL GENERATION PAUSED]
    Map -- Yes --> Fresh{Freshness == Fresh?}
    Fresh -- No --> S3[Suppress: DATA STALE — SIGNAL GENERATION PAUSED]
    Fresh -- Yes --> Allow([Allowed])
```

## Boundaries & invariants enforced structurally
- **Fabrication (R-1/NFR-5):** live seam throws unless Connected + mapped; freshness defaults to `Unknown` (= not fresh); future-dated ticks → `Stale`.
- **Secrets (R-2/NFR-1):** `Mt5ConnectionOptions` has no secret member (test-enforced); token only from env; DPAPI at rest.
- **Loopback (NFR-2):** `BridgeEndpoint` and `BridgeConfig` refuse non-loopback at construction.
- **Test-provider gating (FR-10/NFR-10):** `TestMarketDataProvider` lives in a separate assembly the production projects do not reference — physically absent from a Release build.

## Gate 1 (Spec Consistency) — PASS
Every FR has an ID + acceptance criteria (brief.md); no `[NEEDS CLARIFICATION]`
open; scope in/out explicit; CEO objective and this architecture agree.
→ Proceed to Plan Review Gate (Phase 2.5).
