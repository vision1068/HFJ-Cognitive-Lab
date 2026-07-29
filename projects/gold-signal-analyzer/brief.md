# Brief — Gold Signal Analyzer: MT5 Connectivity Bridge (read-only scaffolding)

**Project:** gold-signal-analyzer
**Cycle / slice:** Foundation + MT5 connectivity bridge (spec Phase 2 / §5–6, FR-7…FR-12)
**Branch / worktree:** `agents/mt5-connectivity-bridge-gold-signal`
**Date:** 2026-07-30
**Owner:** vision1068 (human)

> This is **not** a fresh 44-section product spec. The product has a
> pre-existing source specification; this brief is **cycle-scoped** to the
> connectivity-bridge slice and reuses the relevant FR/NFR text verbatim from
> the source spec for traceability. This is an **independent, from-scratch**
> implementation — no code was read from or based on the sibling
> `gold-signal-analyzer-foundation` branch (owner's "Independent build" choice).

## Scope decisions confirmed by the owner (binding)

| # | Decision |
|---|---|
| S-1 | **Independent build.** No reading/copying/reuse of `C:/gsa-f`. |
| S-2 | **No live connectivity.** Read-only *scaffolding* only: architecture, interfaces, local-bridge design, .NET client seam, symbol detection/mapping, tests — all against fixtures/mocks/a test provider. **No real MT5 terminal attach** is implemented or wired. |
| S-3 | **Move fast.** Run all phases through without pausing for intermediate approval; consolidate at the end. |

### Permanent product invariants (apply regardless of cycle)
- **INV-1** No live order execution, modification, or closing of trades — ever.
- **INV-2** No storing of any Exness Personal-Area password, email password, OTP, or investor-withdrawal credential.
- **INV-3** No scraping of Exness/TradingView or private/reverse-engineered endpoints.

## In scope (this slice)
- `IMarketDataProvider` abstraction + `CsvHistoricalMarketDataProvider` and `TestMarketDataProvider` (fully built); `MetaTrader5MarketDataProvider` as a **structural seam** delegating to a local bridge client (no terminal attach).
- Local **MT5 bridge** design + runnable scaffold (Python, localhost-only, token-auth, heartbeat) using a fake data source; the real `MetaTrader5`-backed source is a **guarded seam**, never exercised.
- **Gold symbol detection** across broker variants + normalization to `XAU/USD`, with manual override.
- **Data-freshness / signal-veto** logic (`DATA STALE — SIGNAL GENERATION PAUSED`).
- **Auto-reconnect** math (exponential backoff + heartbeat) and connection state machine.
- Credential seam (Windows DPAPI) so no secret ever lives in source/config.

## Out of scope (this slice)
- Real MT5 terminal attach / live market data (declined by owner — S-2).
- The signal-generation strategy itself, UI/desktop shell, packaging/installer, CI/CD pipeline.
- Everything in the 44-section product spec not listed under "In scope".

## Requirements (reused verbatim from source spec)

| ID | Requirement | Acceptance criteria (this slice) |
|----|-------------|----------------------------------|
| **FR-7** | Connect to a local MT5 terminal in Mode A (existing session) and Mode B (configured, read-only); never requests trading password; Mode B never requests PA password. | AC-7.1 Two modes modelled (`Mt5ConnectionMode`). AC-7.2 Config type carries **no** password/secret field (enforced by test). |
| **FR-8** | Local MT5 bridge (Python MetaTrader5 pkg + localhost service) bound to 127.0.0.1 only, token-authenticated; heartbeat + auto-reconnect. | AC-8.1 Bridge refuses non-loopback bind. AC-8.2 Rejects unauthenticated calls (401). AC-8.3 Heartbeat endpoint + client health probe. |
| **FR-9** | Gold symbol detection across broker variants (XAUUSD, GOLD, XAUUSDm, .a/.c/.pro/.raw…) + manual selection + mapping to normalized `XAU/USD`; no hardcoded `XAUUSD`. | AC-9.1 Detects all listed variants. AC-9.2 Excludes silver/platinum/palladium. AC-9.3 Works when only `GOLD` exists. AC-9.4 Manual override → confidence 1.0. |
| **FR-10** | `IMarketDataProvider` with MetaTrader5/Csv/Test providers; test provider gated out of production builds. | AC-10.1 Interface + Csv + Test built. AC-10.2 MT5 provider is a safe seam. AC-10.3 Test provider physically absent from production build. |
| **FR-11** | Retrieve genuine live data fields; never mock prices in live mode (mock only in tests). | AC-11.1 Only the live provider is `IsLive`. AC-11.2 Live seam never returns a price when not Connected (throws). AC-11.3 No fabricated "live" path exists. |
| **FR-12** | Data-freshness/safety tracking + `DATA STALE — SIGNAL GENERATION PAUSED` and veto — signals suppressed under every stale/invalid condition. | AC-12.1 Fresh/Stale/Unknown tracked vs. injected clock. AC-12.2 Exact banner string. AC-12.3 Gate suppresses on not-connected, no-mapping, and not-fresh. |

| NFR | Requirement | How addressed |
|-----|-------------|---------------|
| **NFR-1** | No credential/secret in source/appsettings/logs; MT5 creds only via Windows Credential Manager/DPAPI. | `ICredentialStore` + `DpapiCredentialStore`; options type has no secret field; bridge token from env only. |
| **NFR-2** | Bridge binds 127.0.0.1 only, token-authenticated, never public. | `BridgeEndpoint` loopback invariant; `BridgeConfig` refuses non-loopback; token on every request. |
| **NFR-4** | Auto-reconnect, exponential backoff, heartbeat, graceful shutdown; stay Neutral while reconnecting. | `ExponentialBackoffPolicy`; `ConnectionState` machine; gate suppresses while not Connected. |
| **NFR-5** | Never fabricate market facts/prices. | Live seam throws when not Connected; providers flagged `IsLive`; freshness treats future/unknown as not-fresh. |
| **NFR-10** | Pure calc/logic isolated and unit-testable without a live terminal; test provider gated out of production. | Domain/Application are pure; test provider in a separate assembly not referenced by production. |

## `[NEEDS CLARIFICATION]`
None outstanding — scope was fixed by the owner (S-1…S-3). Live-attach behaviour is deliberately deferred, not ambiguous.
