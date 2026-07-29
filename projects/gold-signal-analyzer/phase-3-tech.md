# Phase 3 — Implementation

Phase 3 normally fans out to backend/frontend/middleware/crm-developer in
parallel. For this slice only **Backend** and **Middleware** are in play;
**Frontend** and **CRM Developer** are **N/A** (no UI/Dataverse in a
connectivity bridge) and are recorded as such for traceability.

## Task ledger (machine-checkable format, rule 10)
| Task | Files | Acceptance | Run |
|------|-------|-----------|-----|
| [backend] Domain value objects with construction-time invariants | `GoldSignalAnalyzer.Domain/*.cs` | invalid tick/candle/symbol rejected | `dotnet test --filter DomainTests` |
| [backend] Gold symbol detection + mapping (FR-9) | `Application/Symbols/GoldSymbolResolver.cs` | variants detected, metals excluded, manual override | `dotnet test --filter GoldSymbolResolverTests` |
| [backend] Freshness + veto gate (FR-12) | `Application/Freshness/*.cs` | exact stale banner; suppress on every bad condition | `dotnet test --filter FreshnessAndGateTests` |
| [backend] Backoff (NFR-4) | `Application/Connection/ExponentialBackoffPolicy.cs` | grows, caps, bounded jitter | `dotnet test --filter BackoffTests` |
| [backend] Providers: Csv (full), MT5 (seam) (FR-10/11) | `Infrastructure/Providers/*.cs` | Csv replays; MT5 refuses when not connected | `dotnet test --filter "CsvProviderTests|Mt5ProviderSeamTests"` |
| [backend] DPAPI credential seam (NFR-1) | `Infrastructure/Security/DpapiCredentialStore.cs` | Windows-guarded; no plaintext at rest | build (Windows-only type) |
| [middleware] Bridge client: loopback + token + heartbeat (FR-8/NFR-2) | `Infrastructure/Bridge/HttpMt5BridgeClient.cs`, `Application/Bridge/BridgeEndpoint.cs` | token on every call; 401 handling; health degrades | `dotnet test --filter "HttpBridgeClientTests|BridgeEndpointTests"` |
| [middleware] Python bridge scaffold (FR-8/NFR-2/NFR-5) | `src/bridge/**` | loopback-only, token-auth, read-only source | `python -m unittest discover -s tests` |

## Backend — key implementation notes
- **Money is `decimal`, never `double`.** `MarketTick`/`Candle` validate positivity and OHLC ordering at construction so a corrupt bar cannot enter the calc layer.
- **`GoldSymbolResolver.ScoreGold`** is a pure static scorer (0..1). No hardcoded `XAUUSD`: it scores the discovered list, hard-excludes `XAG/XPT/XPD/SILVER/PLATINUM/PALLAD`, and deterministically prefers the cleanest symbol (`XAUUSD` = 1.0, then suffixed variants). Mirrored in Python `symbols.py` so both sides agree.
- **`MetaTrader5MarketDataProvider` is a seam, not an attach.** It only orchestrates `IMt5BridgeClient` and enforces: refuse data unless `Connected` + symbol mapped (throws, never fabricates). "Bridge up, terminal detached" → `Reconnecting`, never `Connected`.
- **`DataFreshnessMonitor`** uses an injected `IClock`; `Unknown` before first tick; future-dated ticks → `Stale`. **`SignalGate`** is a pure function returning the exact banner `DATA STALE — SIGNAL GENERATION PAUSED`.

## Middleware — key implementation notes
- **`BridgeEndpoint`** cannot be constructed for a non-loopback host. **`HttpMt5BridgeClient`** re-checks loopback (defense in depth), attaches `X-Bridge-Token` to every request, throws on 401 for data calls, and its heartbeat **never throws** for an unreachable/unhealthy bridge (returns unhealthy `BridgeHealth`).
- **Python bridge** (`src/bridge`): stdlib `http.server`, `BridgeConfig` refuses non-loopback bind and requires a token from `GSA_BRIDGE_TOKEN`; `auth.token_is_valid` uses `hmac.compare_digest`. The real `MetaTrader5` source is a **guarded, read-only** seam (`# pragma: no cover`), never instantiated by tests; it exposes no order/trade calls (INV-1).

## Gate 2 (Functional / run-it-once) — PASS
- .NET: full solution builds Debug **and** Release with **0 warnings / 0 errors**.
- Python: the bridge **actually starts** on an ephemeral loopback port and serves real HTTP in `test_server.py` — a real request without a token returns **401**, with the token returns **200 + JSON**. This is executed, not asserted in prose.

## Frontend — N/A (no UI in this slice)
## CRM Developer — N/A (no Dataverse/CRM in this slice)
