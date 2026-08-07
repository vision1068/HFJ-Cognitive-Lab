# Cycle 4 — Audit Phase (Charts & Notifications)

**Project:** gold-signal-analyzer · **Date:** 2026-07-30 · **Reviewer:** auditor role
**Scope reviewed:** FR-27 (chart), FR-30 (in-app notifications) added to the read-only
WPF shell. Governance question: does adding a chart + alerts change the risk posture?

## Verdict: **PASS** — still read-only / paper-only; C-3 boundary untouched.

Adding a passive chart and passive alerts does **not** move the product across any
regulated or irreversible line. Both are read-only projections of values the
analytical core already computed and already displayed as text.

## Invariant re-verification (evidence, not assertion)
| Invariant | How re-checked this cycle | Result |
|-----------|---------------------------|--------|
| **INV-1** no order/execution surface | grep `OrderSend\|order_send\|PlaceOrder\|ExecuteTrade\|SubmitOrder\|.Buy(\|.Sell(\|IMt5BridgeClient` over new Presentation+Wpf files; reflection test: `SignalNotifier` has no member containing open/close/buy/sell/execute/submit/order/trade/place | **0 / pass** — a notification is a read-only record with no command; a chart is a drawing |
| **INV-2 / INV-3** no secret, no scraping | new code adds no config, no network egress, no HTTP; 0 `PackageReference` added | **pass** |
| **INV-4** no fabricated "live" data | chart renders the SAME Csv/Test-grade candles (`IsLive=false`); empty/short series draws nothing; overlay skips warmup nulls | **pass** |
| **INV-5** every value traceable | overlays = existing `Indicators.EmaSeries`; each notification carries the real classification's direction/score/reason verbatim — no invented text | **pass** |
| **FR-22** score never a probability | chart + notification labels use `(0-100)`; grep for `%`/`probability` in new files = 0 (doc comments only) | **pass** |
| **C-1** disclaimer intact | banner `Border` remains docked Top outside the new `ScrollViewer` → always visible; first-run gate unchanged; banner test green | **pass** |
| **C-3** live/order boundary | no live-attach code, no bridge trade path, no order affordance added; live seam remains unwired behind the named-approver gate | **untouched** |

## Governance notes
- **In-app-only was the right call.** Rejecting email/SMS/push (would add secrets +
  egress, contradicting INV-2) and deferring OS toast (needs app-identity registration,
  belongs with FR-34 packaging) keeps this slice dependency-free and inside the
  established risk envelope. Documented as scope decision D4-2, not a silent omission.
- **A signal alert is not a solicitation.** The notification restates the same
  Buy/Sell *score-based* classification already shown on the dashboard, under the same
  always-visible "not investment advice" banner; it adds no call-to-action and no
  execution path. It does not convert the tool into an advisory/brokerage surface.
- **No new data leaves the machine.** Chart + notifier operate entirely on
  already-loaded in-memory values.

## Conditions
- No new conditions. **C-3 remains the only open condition**, unchanged and untouched.
