# Cycle 2 · Slice 1 — §9 Byte-Exact Correction (post-approval addendum)

**Project:** gold-signal-analyzer · **Date:** 2026-07-26
**Type:** Correctness fix to an already-CEO-approved slice. Not a new feature, not a new slice.
**Scope:** `IMarketDataProvider` and its immediate dependents only. No live MT5, no order execution,
no other slice touched. Nothing committed — working tree left for user review.

> **This document appends to the audit trail; it does not rewrite it.** `phase-2-cycle2-arch.md`
> (ADR-7) and `phase-6-cycle2-ceo.md` are left byte-for-byte unchanged so the original reasoning —
> including its explicitly-conditional nature — stays on the record. This addendum records what
> changed once the condition was discharged.

---

## 1. Why this correction exists

The Slice-1 close (`phase-6-cycle2-ceo.md`) was **APPROVED**, but its criterion #4 ("§9
`IMarketDataProvider` reconciled") and its non-blocking confirm-item #1 were both explicitly
**conditioned on a faithful reconstruction, pending the user pasting literal spec §9**:

- `phase-6-cycle2-ceo.md` (lines 25, 62–66): *"The reconciliation used a faithful reconstruction
  that preserves the read-only / no-order invariant. To make it byte-exact, paste the literal §9
  text; if it names a streaming member, the standing recommendation is to compose it above the
  pull-only port."*
- `phase-2-cycle2-arch.md` **ADR-7** (lines 111, 319, 351): *"§9 port is pull-only; streaming is
  composed above the port … Accepted, **pending byte-exact §9 confirmation**."*

The user has now provided the **literal §9 text** (authoritative ground truth). It reveals **real
mismatches, not just naming** — including two members the reconstruction had deliberately excluded.
The condition is therefore discharged, and the interface has been corrected to be **byte-exact**.

## 2. What the literal spec revealed (reconstruction → literal)

| Literal §9 member | Reconstruction had | Nature of the gap |
|---|---|---|
| `string ProviderName { get; }` | `string Name` | rename |
| `Task<ProviderConnectionResult> ConnectAsync(ct)` | `Task<ConnectionStatus> ConnectAsync(ct)` | return-type shape |
| `Task DisconnectAsync(ct)` | present | — |
| `Task<AccountSnapshot?> GetAccountSnapshotAsync(ct)` | **absent** | **missing member** |
| `Task<IReadOnlyList<MarketSymbol>> GetAvailableSymbolsAsync(ct)` | `IReadOnlyList<SymbolInfo>` | type rename |
| `Task<SymbolSpecification> GetSymbolSpecificationAsync(symbol, ct)` | `GetSymbolSpecAsync` → `SymbolSpec` | method + type rename |
| `Task<IReadOnlyList<Candle>> GetHistoricalCandlesAsync(symbol, tf, from, to, ct)` | `GetCandlesAsync(symbol, tf, int count, ct)` | **different name AND different parameter semantics** (date-range, not count) |
| `IAsyncEnumerable<MarketTick> StreamTicksAsync(symbol, ct)` | `Task<Tick> GetLatestTickAsync(symbol, ct)` | **wrong shape** — scalar poll where the spec is a stream |
| `IAsyncEnumerable<Candle> StreamCandlesAsync(symbol, tf, ct)` | **absent** | **missing member** |
| *(none — not an interface member)* | `bool IsAvailableInProduction` | **interface member the spec does not have** |

**ADR-7 was wrong on the merits, not just pending.** The prior reasoning ("streaming would be a
manufactured push over a poll-only source") misread `IAsyncEnumerable<T>` as a push primitive. It is
the idiomatic .NET **pull** stream: a live provider polls MT5 in an internal loop and `yield return`s
each value; the caller pulls. Streaming ticks/candles is fully **read-only** and does **not** violate
the no-live-execution invariant. ADR-7's conclusion (exclude streaming from the port) is hereby
**superseded**. The original ADR-7 text is preserved unedited in `phase-2-cycle2-arch.md`.

## 3. What changed (the correction)

**Interface — now byte-exact to literal §9** (param names `cancellationToken`/`symbol`/`timeframe`/
`from`/`to`, no default values, exactly 1 property + 8 methods):
- `src/GoldSignalAnalyzer.Application/Ports/IMarketDataProvider.cs` — rewritten.

**Domain / port types (renamed in place; no other dependents existed):**
- `Tick` → `MarketTick` (`Domain/ValueObjects/MarketTick.cs`)
- `SymbolSpec` → `SymbolSpecification` (`Domain/Entities/SymbolSpecification.cs`)
- `SymbolInfo` → `MarketSymbol` (`Application/Ports/MarketSymbol.cs`)
- **new** `ProviderConnectionResult` (`Application/Ports/ProviderConnectionResult.cs`) — wraps the
  retained `ConnectionStatus` enum + optional message.
- **new** `AccountSnapshot` (`Domain/ValueObjects/AccountSnapshot.cs`) — read-only balance figures
  only; no order/trade/position surface.

**FR-10 moved off the interface, onto DI.** `IsAvailableInProduction` is deleted from the interface
(literal §9 has no such member). The rule "the test provider must only be available in development
and automated-test environments" is now an **environment-conditional registration** in
`src/GoldSignalAnalyzer.MarketData/DependencyInjection.cs`: `AddGsaMarketData(IHostEnvironment)`
binds the production-safe `NullMarketDataProvider` always, and reserves a non-Production-only seam for
the roadmap `TestMarketDataProvider`. `GsaHost.cs` now passes `context.HostingEnvironment`.

**Implementers / consumers fixed:**
- `NullMarketDataProvider.cs` — implements the byte-exact interface; non-nullable scalar
  (`GetSymbolSpecificationAsync`) throws `NotConnectedException`, nullable `GetAccountSnapshotAsync`
  returns `null`, collections + both streams yield nothing — never fabricated data (NFR-5).
- `MarketData/DependencyInjection.cs`, `Desktop/GsaHost.cs`, `MarketData.csproj`
  (+`Microsoft.Extensions.Hosting.Abstractions` 8.0.0).

**Tests corrected:**
- `Architecture/MarketDataPortShapeTests.cs` — the AC-36.2 assertion that the port carries **no**
  `IAsyncEnumerable` member is **flipped**: the port now MUST expose
  `IAsyncEnumerable<MarketTick> StreamTicksAsync` and `IAsyncEnumerable<Candle> StreamCandlesAsync`.
  The read-only invariant is kept and strengthened (no order/trade/position/login/buy/sell/modify/
  deal/withdraw/send-shaped member; no push-based `IObservable`). A new
  `byte_exact_to_literal_spec_section_9` test asserts the exact 9-member set, each return type, and
  each parameter (type + name) via reflection.
- `NullMarketDataProviderTests.cs` — updated to the new members (scalar-throws, snapshot-null,
  empty collections, empty streams).

## 4. Verification evidence (test-before-claim; no incremental-build shortcut)

Per the newest `lessons-learned.md` entry (incremental builds hide warnings), the build below is
`--no-incremental`.

| Check | Command (from `projects/gold-signal-analyzer/src/`) | Result |
|---|---|---|
| Clean build | `dotnet build GoldSignalAnalyzer.sln --no-incremental` | **Build succeeded. 0 Warning(s), 0 Error(s)** (all 13 projects) |
| .NET tests | `dotnet test GoldSignalAnalyzer.sln --no-build` | **Passed! Failed: 0, Passed: 47, Skipped: 0** (was 45; +2 net new) |
| Python bridge (unchanged confirm) | `python -m unittest discover -s .` (from `…/MT5Bridge/python/`) | **Ran 19 tests … OK** — unaffected, as expected (.NET-only change) |

Notable: `DiCompositionTests` builds the host under `Hosting environment: Production` and still
resolves `IMarketDataProvider` to `NullMarketDataProvider`, confirming the FR-10 DI gate keeps the
production-safe default in Production and did not regress composition.

## 5. Status of the affected acceptance criteria

- **FR-10 / criterion #4 ("Interface matches spec §9 exactly"):** was *conditionally* MET (faithful
  reconstruction). It is now **genuinely MET** — the interface is byte-exact to the user-provided
  literal §9, proven by the reflection test above. The Phase-6 approval's confirm-item #1 is
  **closed**.
- **ADR-7:** **superseded** (streaming belongs on the read-only port as pull-based
  `IAsyncEnumerable`). Original text preserved for traceability.
- Read-only / no-live-execution invariant: **unchanged and still enforced** — streaming is read-only.

## 6. Spec §44 confirmation (separate confirm-item, now closed)

The user also pasted literal spec **§44** (the non-secret MT5 environment-input checklist + the
do-not-ask password list). It **matches** what is already captured in `brief.md` / `CYCLE2-RESUME.md`:
demo/real, install path, Exness server, account number, gold symbol, MT5-installed?, connection mode,
timeframes, Buy/Sell thresholds; do-not-ask covers trading/investor/PA/email passwords, OTP, and
withdrawal credentials. **No content change needed** — recorded here as **confirmed byte-exact**,
closing that open confirm-item.

## 7. Lesson

A "faithful reconstruction" of an external spec is **not** byte-exact and can diverge in
load-bearing ways (here: two missing members and one scalar-vs-stream shape error), not just naming.
An interface ratified against a reconstruction should carry an explicit `[NEEDS CLARIFICATION]` /
conditional-approval marker until the literal text is in hand — which is exactly what ADR-7 and the
Phase-6 confirm-item did, and why this correction is a clean discharge rather than a silent rewrite.
Also: `IAsyncEnumerable<T>` is a **pull** stream, not a push primitive — it does not conflict with a
poll-only source or a read-only invariant.
