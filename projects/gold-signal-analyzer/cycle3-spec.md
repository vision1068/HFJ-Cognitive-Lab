# Cycle 3 Spec — Gold Signal Analyzer: UI / Dashboard Shell (FR-26, FR-35)

**Project:** gold-signal-analyzer
**Cycle / slice:** UI/dashboard shell — display the analytical core's signals, scores and paper journal, and surface the mandatory "not investment advice" disclaimer that closes CEO condition C-1.
**Branch / worktree:** `agents/mt5-connectivity-bridge-gold-signal`
**Date:** 2026-07-30
**Owner:** vision1068 (human)

> Extends Cycle 1 (`brief.md`, read-only MT5 bridge scaffold) and Cycle 2
> (`cycle2-spec.md`, analytical core). Same independent-build / no-live-connectivity /
> no-order-execution constraints apply verbatim (S-1…S-3, INV-1…INV-5). This slice
> adds a **presentation layer only** — it reads what the Application layer already
> produces and renders it. It computes no new market facts and, critically,
> introduces **no order/execution surface of any kind** (not even a paper
> Buy/Sell button — the shell is read-only display for this slice).

## Why this slice exists (business framing carried from CEO)

Cycle 2 sign-off (`phase-6-ceo-cycle2.md`) left **C-1 open by design**: a rendered
directional BUY/SELL with a numeric score on a real instrument (gold) is a
regulated-advice-shaped surface, so a first-run disclaimer (FR-35) plus a
persistent "not investment advice" banner **must ship with the UI before any user
sees a signal**. There was no UI for that disclaimer to live in — Cycle 1/2 were
headless. This slice builds the minimum UI shell that (a) makes the analytical
core visible and (b) gives C-1 a home. C-3 (live MT5 attach / any order capability)
stays **explicitly out of scope**, gated behind the Cycle-1 named-approver.

## Permanent invariants (re-asserted, verified this phase)
- **INV-1** No order execution/modification/closing — and this slice adds **no UI
  affordance that could place or simulate placing an order** (read-only display).
- **INV-2 / INV-3** No stored PA/email/OTP/withdrawal credential; no scraping.
- **INV-4** No fabricated market data presented as live — the shell renders only
  values the Application layer computed from Csv/Test providers (`IsLive=false`).
- **INV-5** Every number shown is traceable to a real computed contribution/penalty/
  veto — the shell shows the existing `SignalExplanation` lines verbatim, inventing
  no commentary of its own.

---

## Architecture fork resolved up front (see phase-2-arch-cycle3.md)

The CEO/owner framing assumes **WPF**. WPF is confirmed feasible on this machine
(.NET 8 SDK 8.0.100 + `Microsoft.WindowsDesktop.App 8.0.0` runtime present) and is
the right choice for a Windows-only, MT5-adjacent desktop tool. **We keep WPF** —
no switch. To keep the existing `net8.0` test project able to test UI logic
headlessly (the analytical core's whole discipline is command-output-verifiable
logic), all view-model logic lives in a **plain `net8.0` `Presentation` project**;
the WPF project (`net8.0-windows`) is a thin XAML shell with no logic to test.

## Requirements (this slice)

| ID | Requirement | Acceptance criteria |
|----|-------------|---------------------|
| **FR-26** | A dashboard that displays the current signal: direction (Buy/Sell/Neutral), independent Buy score and Sell score (each labelled `score (0-100)`, never a %), regime, confidence, primary reason, and the deterministic explanation lines — all sourced from the Application layer's `SignalClassification` / `SignalExplanation`. | AC-26.1 A `SignalViewModel` exposes Direction, BuyScore, SellScore, Regime, Confidence, PrimaryReason and ExplanationLines from a supplied `SignalClassification`. AC-26.2 Score labels are `score (0-100)` / `Confidence (0-100)`; no code path formats any score as `N%`/`probability`. AC-26.3 A Neutral/vetoed classification renders the veto reason (e.g. the exact `DATA STALE — SIGNAL GENERATION PAUSED` string) and shows no actionable call. AC-26.4 The risk plan (entry/SL/target/R:R/size) is shown when, and only when, the signal is actionable and the plan `IsTradeable`; a rejected plan shows its reason, not fabricated levels. |
| **FR-26.2** | A paper-trading journal view backed by the existing `IJournalStore` (durable `SqliteJournalStore` or in-memory). | AC-26.2a A `JournalViewModel` loads `IJournalStore.All()` into read-only rows (symbol, direction, status, entry/exit time+price, size, R:R, regime, confidence, gross/net P&L). AC-26.2b The view is display-only: it exposes **no** open/close/modify command. AC-26.2c Rows render P&L from the stored value (no recomputation/fabrication); an open trade shows blank exit/P&L, not a guessed one. AC-26.2d A totals line sums realised Net P&L over closed trades only. |
| **FR-35** | First-run **disclaimer acknowledgement** gate: on first launch the user must see and explicitly acknowledge a "not investment advice" disclaimer before reaching the dashboard; acknowledgement is persisted so it is genuinely first-run. | AC-35.1 A single, auditable disclaimer text constant contains the regulated-advice wording: not investment advice, educational/informational only, gold trading carries risk of loss, past/simulated performance is not indicative of future results, paper-trading only (no live orders), user is solely responsible. AC-35.2 A `DisclaimerViewModel` starts un-acknowledged, exposes `AcknowledgeCommand`, and only sets `HasAcknowledged` after the command runs; acknowledgement persists via an injected `IAcknowledgementStore` so a second launch does not re-prompt. AC-35.3 A test proves the gate blocks until acknowledged and that persistence is honoured across a fresh store instance. |
| **FR-35.2** | Persistent, always-visible "not investment advice" banner in the main window chrome (independent of the first-run gate). | AC-35.2a `MainViewModel` exposes a non-empty `DisclaimerBanner` string drawn from the same disclaimer constant source. AC-35.2b The banner is structurally always present (bound into the main window layout, not a dismissible popup). A test asserts the banner text is present and carries the "not investment advice" phrase. |
| **NFR-UI-1** | UI logic is unit-testable without a display; the XAML shell carries no logic. | All view-models live in a `net8.0` project referenced by the existing `net8.0` test project; a `dotnet test` of the solution exercises them headlessly. |
| **NFR-UI-2** | The presentation layer introduces no new secret, no network egress, and no reference to any order/bridge-trade path. | Grep/reflection: Presentation + Wpf projects contain no `OrderSend`/`order_send`/`PlaceOrder`/`Open(`-trade/`Close(`-trade affordance and no bridge-client trade reference (there is none to reference — INV-1 structural). |

## Out of scope (this slice)
- FR-27 (interactive charts / candlestick rendering), FR-28 (setup wizard),
  FR-30 (notifications), FR-34 (installer/packaging) — deferred to later slices.
- Any live data path, live MT5 attach, or order/paper-execution **action** (C-3).
- Real-time streaming/auto-refresh — the shell renders a supplied/loaded snapshot.

## `[NEEDS CLARIFICATION]`
None outstanding. The WPF assumption is confirmed (not ambiguous). The decision to
make this slice **read-only display** (no paper Buy/Sell buttons) is a deliberate
scope tightening to keep the disclaimer-closure slice free of any execution-shaped
surface — flagged here for the CEO gate rather than guessed.
