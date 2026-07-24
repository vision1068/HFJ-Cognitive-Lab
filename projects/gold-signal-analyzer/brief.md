# Brief — Gold Signal Analyzer

**Project:** gold-signal-analyzer
**Type:** Windows 11 desktop application (WPF / .NET 8) — personal/internal trading-analysis tool
**Intake mode:** Abbreviated — drafted directly from the user's 44-section source specification (per engagement scope decision #1). No 3-round interview was run; genuine gaps are collected once, in Phase 6.
**This engagement builds:** Cycle 1 — Foundation (spec Section 41, Phase 1) ONLY. All other functional requirements below are specified now for traceability but are roadmap, deferred to later cycles.

---

## 1. Problem Statement

The user (a single trader, operating a personal Exness MetaTrader 5 demo/real account) needs a Windows 11 desktop application, **Gold Signal Analyzer**, that connects to a *locally installed* MT5 terminal, retrieves genuine live XAU/USD market data, computes technical indicators across multiple timeframes, and produces **transparent, auditable Buy / Sell / Neutral signals** scored 0–100 with every score contribution explained. The initial release is **analysis and paper-trading only** — it must never execute, modify, or close real trades. Success in plain language: a trader can see, at a glance and with full audit of *why*, whether current gold conditions are bullish, bearish, or unclear, backed by real broker data, never by mock prices or fabricated figures, and never claiming guaranteed profit.

Success for **Cycle 1 specifically**: a clean-architecture .NET 8 solution that builds and tests green, with all 12 projects, DI, logging, SQLite/EF Core, and an MVVM navigation shell in place — plus a credible, diagrammed roadmap for the remaining nine spec phases.

---

## 2. Scope

### In scope — THIS engagement (Cycle 1 — Foundation)
- Clean-architecture solution `GoldSignalAnalyzer.sln` with all 12 projects (spec §3) and correct project references.
- Microsoft DI container wiring (composition root).
- Serilog structured logging configured (console + rolling file), with sensitive-data masking discipline established.
- SQLite + EF Core `DbContext` with an initial migration (schema stubs for spec §34 tables acceptable this cycle).
- WPF MVVM shell window with basic navigation between placeholder screens (spec §30 screen list represented as navigable placeholders).
- xUnit test project with smoke tests: DI resolves the composition root, migration applies to a scratch SQLite file, and any pure logic present passes.
- `.gitignore` coverage for .NET build artefacts and any future local-secret path.
- Forward-planning roadmap covering spec phases 2–10 (documentation only, no code).

### Out of scope — THIS engagement (deferred to later cycles, specified for traceability)
- MT5 bridge, live connectivity, any real terminal communication (spec Phase 2 / §5–6).
- Market-data normalization, candle building, stale detection (Phase 3 / §8–10, §13).
- Indicator engine, multi-timeframe calc (Phase 4 / §14–15).
- Scoring, penalties, veto, confidence, explanations (Phase 5 / §16–24).
- Full dashboard, charts, connection wizard (Phase 6 / §27–30).
- Notifications, backtesting, paper trading, packaging (Phases 7–10 / §31–33, §41).

### Out of scope — PERMANENTLY (this release, all cycles)
- Live/real order execution, modification, or closing of trades (spec §1, §43).
- Scraping Exness or TradingView; private/reverse-engineered endpoints (spec §4, §43).
- Storing any Exness Personal Area password, email password, OTP, investor-withdrawal credential (spec §5, §35, §44).

---

## 3. Functional Requirements (full product — IDs carry across all cycles)

> Cycle column: **C1** = built this engagement; **R** = roadmap (specified now, built later).

### Foundation & platform
| ID | Requirement | Priority | Cycle | Acceptance Criteria |
|----|-------------|----------|-------|---------------------|
| FR-1 | Clean-architecture solution with the 12 projects of spec §3 and correct dependency direction (Desktop→Application→Domain; Infrastructure/adapters inward-facing) | Must | C1 | `dotnet build` succeeds; no project references violate the dependency rule (Domain references nothing outward) |
| FR-2 | DI composition root wiring all registered services | Must | C1 | A smoke test resolves the root service provider with no missing-registration exception |
| FR-3 | Serilog structured logging (console + rolling file sink) | Must | C1 | Log file is written on startup; log output is structured; masking helper exists for secrets/account numbers |
| FR-4 | SQLite persistence via EF Core `DbContext` + initial migration | Must | C1 | `dotnet ef migrations` produces a migration; applying it to a scratch DB creates the expected tables; a smoke test asserts this |
| FR-5 | WPF MVVM shell with navigation between the spec §30 screens as placeholders | Must | C1 | Shell project builds; navigation service switches the active view; each §30 screen exists as a navigable placeholder view+viewmodel |
| FR-6 | Configuration model for all spec §37 settings, with reset/export/import of **non-sensitive** settings only | Should | R (model stub C1) | Config object exists; export never emits any secret field |

### MT5 connectivity & data (roadmap)
| ID | Requirement | Priority | Cycle | Acceptance Criteria |
|----|-------------|----------|-------|---------------------|
| FR-7 | Connect to a locally installed MT5 terminal in Mode A (existing session) and Mode B (configured, read-only) per spec §5 | Must | R | Connects to running terminal without requesting trading password; Mode B never requests PA password |
| FR-8 | Local MT5 bridge (Python MetaTrader5 pkg + localhost service) bound to 127.0.0.1 only, token-authenticated (spec §6) | Must | R | Bridge binds loopback only; rejects unauthenticated calls; heartbeat + auto-reconnect |
| FR-9 | Gold symbol detection across broker variants (XAUUSD, GOLD, XAUUSDm, .a, .c, .pro, .raw…) with manual selection + mapping to normalized `XAU/USD` (spec §7) | Must | R | Lists matching symbols; user selects; mapping persisted; no hardcoded assumption of `XAUUSD` |
| FR-10 | `IMarketDataProvider` abstraction (spec §9 signature) with `MetaTrader5MarketDataProvider`, `CsvHistoricalMarketDataProvider`, `TestMarketDataProvider`; test provider unavailable in production builds | Must | R (interface may land C1 as stub) | Interface matches spec §9 exactly; test provider gated to dev/test env |
| FR-11 | Retrieve genuine live data fields of spec §8; never mock prices in live mode | Must | R | Live mode uses real MT5 data only; mock only in automated tests |
| FR-12 | Data-freshness/safety tracking + `DATA STALE — SIGNAL GENERATION PAUSED` and the veto conditions of spec §10 | Must | R | Signal suppressed under every §10 stale/invalid condition |

### Indicators, regime, scoring (roadmap)
| ID | Requirement | Priority | Cycle | Acceptance Criteria |
|----|-------------|----------|-------|---------------------|
| FR-13 | Compute all trend/momentum/volatility/volume indicators of spec §14 locally from MT5 candles, with volume type labelled (real/tick/estimated/unavailable) | Must | R | Each indicator matches a fixed-dataset expected output; tick volume never labelled as world volume |
| FR-14 | Multi-timeframe analysis (M1–D1, spec §12) using completed candles for confirmed signals; incomplete candle marked `Provisional` (spec §13) | Must | R | Confirmed signals use closed candles; provisional clearly marked |
| FR-15 | Avoid double-counting correlated indicators via category caps (spec §15, §18) | Must | R | Category score cannot exceed its cap regardless of how many correlated indicators agree |
| FR-16 | Market-regime classification (spec §16) adjusting scoring by regime | Must | R | Regime output ∈ spec §16 set; scoring weights adjust per regime rules |
| FR-17 | Independent BuyScore and SellScore (0–100 each) — never `Sell = 100 − Buy` (spec §17) | Must | R | Buy and Sell computed from independent evidence; unit test proves independence |
| FR-18 | Category-weighted scoring per spec §18–19 (Buy) and mirrored independent Sell rules | Must | R | Score breakdown sums per category caps; each contribution traceable |
| FR-19 | Configurable penalties, all visible in the explanation (spec §20) | Must | R | Every applied penalty appears in the explanation output |
| FR-20 | Hard veto rules forcing Neutral (spec §21) | Must | R | Any active veto forces Neutral; veto reason shown |
| FR-21 | Signal classification + thresholds/guards of spec §22 (winning margin ≥15, opposite-score caps, HTF confirmation, cooldown, dedupe) | Must | R | Classification obeys every §22 guard; `75` never rendered as "75% probability" |
| FR-22 | Confidence score computed separately from Buy/Sell per spec §23 | Must | R | Confidence independent of scores; reduced when evidence concentrated in one category |
| FR-23 | Deterministic, data-derived signal explanation (spec §24) — no LLM-invented facts | Must | R | Explanation generated from calculated values only |
| FR-24 | Risk plan (entry/SL/targets/R:R/position size) from real MT5 symbol specs (spec §25) | Must | R | Uses actual contract size/tick value/lot step; defaults: 0.5% risk, min R:R 1.5, one open position |
| FR-25 | Economic-news filter with configurable lockout (default ±30 min); `SIGNAL BLOCKED BY NEWS-RISK FILTER` (spec §26) | Should | R | Actionable alerts blocked in lockout; technicals still shown; manual events supported if no API |

### UI, notifications, backtest, paper trade, packaging (roadmap)
| ID | Requirement | Priority | Cycle | Acceptance Criteria |
|----|-------------|----------|-------|---------------------|
| FR-26 | Main dashboard fields of spec §27 (balance hidden unless enabled; passwords never shown) | Must | R (placeholder C1) | Dashboard shows §27 fields; account number masked; no password field anywhere |
| FR-27 | Advanced chart with overlays of spec §28 (toggleable) | Must | R (placeholder C1) | Overlays render and toggle |
| FR-28 | Indicator panel of spec §29 (value, TF, status, contribution, cap, explanation, candle status) | Must | R (placeholder C1) | Panel shows each field per indicator |
| FR-29 | All 17 screens of spec §30 exist and are reachable | Must | C1 (as placeholders) | Each screen navigable from the shell |
| FR-30 | Notifications (toast; optional sound/email/Telegram) gated by the spec §31 conditions | Should | R | Notify only when all §31 gates pass; dedupe + cooldown enforced |
| FR-31 | Backtesting engine (spec §32): chronological, look-ahead-safe, models spread/slippage/commission, full metric set, train/validation/out-of-sample + walk-forward, overfitting warning | Must | R | No future-candle access; metrics computed; look-ahead unit test passes |
| FR-32 | Paper trading (spec §33): genuine live data, simulated execution, full journal fields, never submits an MT5 order | Must | R | No MT5 order submitted; journal stores every §33 field |
| FR-33 | SQLite schema for all spec §34 tables + indexes + retention settings | Must | R (stub schema C1) | Tables + indexes exist; retention configurable |
| FR-34 | Packaging: MSIX/installer, update mechanism, diagnostic export, user guide (spec §41 Phase 10, §42 deliverables) | Should | R | Installer produced; diagnostics exportable |
| FR-35 | Disclaimer (spec §40) shown and acknowledged during initial setup | Must | R (text can land C1) | User must acknowledge before use; exact §40 wording |

---

## 4. Non-Functional Requirements

| ID | Category | Requirement | Cycle |
|----|----------|-------------|-------|
| NFR-1 | Security — secrets | No credential/secret in source, appsettings, XML, SQLite plaintext, env files, logs, or GitHub. MT5 credentials only via Windows Credential Manager / DPAPI (spec §5, §35, §43) | C1 discipline; R enforcement |
| NFR-2 | Security — bridge | Local bridge binds 127.0.0.1 only, token-authenticated, never publicly exposed (spec §6, §35) | R |
| NFR-3 | Security — UI | Account numbers masked in UI; passwords never displayed or logged; delete-credentials + disconnect options (spec §27, §35) | R (masking helper C1) |
| NFR-4 | Reliability | Auto-reconnect, exponential backoff, heartbeat, graceful shutdown, duplicate/out-of-order tick protection, UI-thread safety, DB recovery; stay Neutral while reconnecting (spec §36) | R |
| NFR-5 | Integrity / honesty | Never claim guaranteed profit; never present `75` as a 75% win probability; never label tick volume as world volume; never fabricate indicator values or market facts (spec §1, §14, §22, §43) | C1 disclaimer discipline; R enforcement |
| NFR-6 | Auditability | Every calculation transparent and traceable; penalties and veto reasons never hidden (spec §24, §43) | R (structure enables C1) |
| NFR-7 | Logging | Structured logs for the spec §39 event set; mask passwords, tokens, full account numbers, sensitive paths | C1 (framework) |
| NFR-8 | Performance | Indicator + score recalculation on candle close completes within one candle interval at the shortest supported timeframe; UI remains responsive | R |
| NFR-9 | Portability / config | Timestamps normalized to UTC internally, displayed in user TZ; configuration validated at startup, fail loud (spec §13, §37; secure-coding A04) | C1 (UTC + fail-loud config validation established) |
| NFR-10 | Testability | Pure calculation logic isolated so it is unit-testable without a live terminal; test provider gated out of production (spec §11, §38) | C1 (architecture enables); R (coverage) |

---

## 5. Architecture Decision Records (headline; detail in phase-2-arch.md)

- **ADR-1 — WPF + MVVM over WinUI 3.** Chosen per spec §2 preference (stability + charting-library compatibility for first release). Alternatives: WinUI 3 (newer, thinner ecosystem for the charting libs listed), Avalonia (cross-platform, unneeded — Windows-only target). Consequence: Windows-only, mature tooling.
- **ADR-2 — Clean architecture, Domain has zero outward references.** Enforces testable pure logic (NFR-10) and auditable calculations (NFR-6). Consequence: more projects/boilerplate, justified by the audit/traceability requirement.
- **ADR-3 — SQLite + EF Core.** Per spec §2/§34; single-user local desktop store. Alternative: LiteDB/raw SQLite — rejected for EF migration tooling + testability.
- **ADR-4 — MT5 bridge deferred to Cycle 2.** No live-connectivity code in Foundation; the `IMarketDataProvider` seam is defined so the bridge slots in without touching Domain. Consequence: Cycle 1 has no real market data and must not pretend to.

---

## 6. Test Criteria (Cycle 1)

- **Happy path:** `dotnet build` green across all 12 projects; `dotnet test` green; DI root resolves; migration applies to a scratch SQLite DB.
- **Edge cases (this cycle):** missing DI registration surfaces as a failing smoke test; config validation fails loud on invalid startup config.
- **Security test:** grep the scaffold + `.gitignore` — no secret-bearing file path introduced; `bin/`/`obj/`/`*.user` and any local secrets file are gitignored; no `appsettings` field that would hold a password.
- **Deferred (roadmap):** the full spec §38 indicator/integration test matrix — see phase-4-qa.md for the target.

---

## 7. Traceability

IDs above (FR-#/NFR-#) are assigned at spec time. Cycle-1 items (FR-1..FR-5, FR-29, plus NFR discipline items) must trace into: project/test names in phase-3-tech.md, the QA mapping in phase-4-qa.md, and the auditor's control checks in phase-5-audit.md. A Cycle-1 requirement not found in the test suite is treated as **unverified** regardless of appearance.

## 8. Open Questions / `[NEEDS CLARIFICATION]`

None block Cycle 1 (Foundation is fully determined by the spec). The non-secret MT5 environment inputs required before Cycle 2 (MT5 demo/real, install path, exact Exness server, account number, exact gold symbol, MT5-installed?, preferred connection mode, preferred timeframes, preferred Buy/Sell thresholds — spec §44) are collected once in **phase-6-ceo.md**, not guessed here.
