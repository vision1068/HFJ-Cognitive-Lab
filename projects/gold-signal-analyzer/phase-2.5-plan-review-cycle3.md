# Cycle 3 — Plan Review Gate (adversarial QA + Audit)

**Project:** gold-signal-analyzer · **Date:** 2026-07-30
**Gate:** orchestrator rule 7 — find what is wrong with the plan before code exists.
Single consolidated verdict per rule 11.

## Consolidated verdict: **PASS-WITH-CONDITIONS** (proceed to implementation)

The plan is sound and the testable-VM split is the right call. Seven findings were
raised adversarially; all are addressed in the conditions below (folded into the
spec/architecture before any code was written), so implementation may proceed.

## Findings raised (adversarial)

**QA lens**
- **F-1 (blocking→resolved): net8.0 test project cannot reference a net8.0-windows
  WPF project.** If VMs lived in the WPF project, they would be untestable in the
  existing suite. → Resolved by Decision 2: all VMs in a plain `net8.0` Presentation
  project. *Condition: the WPF project must contain no testable logic.*
- **F-2: `ICommand`/`INotifyPropertyChanged` availability in plain net8.0 is
  assumed.** If wrong, Presentation won't compile. → Both are in the base framework
  (`System.ObjectModel` / `System.ComponentModel`); *Condition: prove it by an actual
  `dotnet build` of Presentation, not by assertion (Verification-Before-Completion).*
- **F-3: adding a `net8.0-windows` project could break `dotnet test
  GoldSignalAnalyzer.sln` on this box.** → *Condition: run the FULL solution build +
  test after adding the WPF project and show the count did not regress from 143.*
- **F-4: FR-22 regression risk.** A UI that shows scores is exactly where a "78%
  probability" label sneaks in. → *Condition: a VM test asserts score labels are
  `score (0-100)` and greps the Presentation/Wpf source for `%`/`probability`
  formatting of a score = 0 hits.*

**Audit lens**
- **F-5 (the whole point): C-1 must be genuinely prominent, not a checkbox.** A
  disclaimer that can be skipped, is empty, or is buried does not close C-1. →
  *Condition: (a) first-run gate blocks the dashboard until explicit acknowledgement;
  (b) a SEPARATE always-visible banner independent of the gate; (c) a single audited
  wording constant containing the required regulated-advice phrases; (d) tests prove
  all three.*
- **F-6 (INV-1 the top invariant): a UI is the easiest place to accidentally add an
  order/execution surface** (even a paper Buy/Sell button reads as execution). →
  *Condition: this slice is read-only display — NO open/close/trade command anywhere
  in Presentation/Wpf; assert absence by test + grep.*
- **F-7: fabrication via the UI (INV-4/5).** A dashboard is tempted to fill blanks
  (e.g. show a P&L for an open trade, or invent risk levels for a Neutral signal). →
  *Condition: journal rows render stored values only (blank exit/P&L when open);
  risk plan shown only when actionable + tradeable, else its real reject reason.*

## Iteration
Round 1 raised F-1…F-7; all fold into spec `cycle3-spec.md` (AC-26.*, FR-35.*,
NFR-UI-2) and `phase-2-arch-cycle3.md` without contested trade-offs. No second round
needed; no disagreement to escalate to the user. Proceed.
