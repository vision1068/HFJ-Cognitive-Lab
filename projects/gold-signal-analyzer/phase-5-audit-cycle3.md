# Cycle 3 — Audit Phase (UI / Dashboard Shell)

**Project:** gold-signal-analyzer · **Date:** 2026-07-30
**Focus:** C-1 disclaimer prominence/compliance-shape; INV-1 no order surface
introduced; INV-4/5 no fabrication via the UI.

## Verdict: **PASS** — C-1 satisfied; no execution surface introduced

### C-1 (regulated-advice disclaimer) — CLOSED by this slice
A rendered directional BUY/SELL with a numeric score on real gold is an
advice-shaped surface. C-1 required the disclaimer to ship with the UI before any
user sees a signal. Verified three independent, tested controls:
1. **First-run acknowledgement gate (FR-35)** — the dashboard is not shown until the
   user explicitly acknowledges; the composition root `Shutdown()`s if the gate is
   cancelled. Acknowledgement persists (durable marker file), so it is genuinely
   first-run. Tests: `Disclaimer_gate_blocks_until_acknowledged`,
   `Disclaimer_acknowledgement_persists_across_fresh_store`.
2. **Persistent banner (FR-35.2)** — a high-contrast banner bound structurally into
   the main window chrome (not dismissible), always visible above the signal. Test:
   `Banner_is_non_empty_and_carries_key_phrase`.
3. **Single audited wording source** — `DisclaimerText` carries the required phrases:
   *not investment advice*, *educational*, *risk of loss*, *past performance*,
   *simulated*, *paper-trading*, *solely responsible*, and describes scores as
   *0-100* (not probabilities). Test: `DisclaimerText_contains_required_regulated_advice_phrases`.
   One governance-visible edit was made during the gate so the exact phrase "not
   investment advice" is literally present.

### INV-1 (no order/execution) — preserved; nothing introduced
- The shell is **read-only display**: no open/close/buy/sell/execute/submit/place
  command anywhere in Presentation or Wpf. Source grep = **0 matches**; reflection
  test `JournalVm_exposes_no_order_or_mutation_command` asserts no `*Command` with a
  trade verb. Presentation does not reference `PaperBroker` or `IMt5BridgeClient` at
  all (grep = 0).
- Even paper open/close is out of scope this slice — deliberately, to keep the
  disclaimer-closure slice free of any execution-shaped affordance.

### INV-4 / INV-5 (no fabrication) — preserved
- The shell renders only values the analytical core computed. The `SignalExplanation`
  lines are shown verbatim. A Neutral/vetoed signal shows the real veto reason and
  **no** risk levels; an actionable signal shows a plan only when `IsTradeable`, else
  the real reject reason (`SignalVm_neutral_shows_reject_reason_not_levels`).
- Journal rows render stored values only; an open trade shows blank exit/P&L
  (`JournalRow_open_trade_has_blank_exit_and_pnl`); the realised total sums closed
  trades only.
- Sample startup data is deterministic and explicitly non-live (Csv/Test grade,
  `IsLive=false`); the banner states all trades are simulation. No "live" price is
  claimed (INV-4).

### FR-22 (no probability framing) — preserved
Score labels are `score (0-100)` / `Confidence (0-100)`. Source grep for
`%`/`probability`/`:P` formatting of a score = **0** (only a doc comment matches).

### Secrets / egress — none added
Presentation is pure logic + one local file marker (acknowledgement). Wpf adds a
local SQLite file path and local sample data. No network egress, no secret member.

### Assembly-graph gate re-verified after new dependencies
`Testing.dll` is **absent** from the Wpf Release output — the test data provider
cannot reach the shipped app. Presentation references only Domain + Application.

## Conditions carried forward
- **C-3 (live seam / any order capability)** remains OUT — gated behind the Cycle-1
  named approver. This slice introduced nothing that touches it.
