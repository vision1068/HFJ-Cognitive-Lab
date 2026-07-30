# Cycle 5 — CEO Sign-off (First-Run Setup Wizard, FR-28)

**Project:** gold-signal-analyzer · **Date:** 2026-07-31 · **Decision by:** CEO agent role
**Scope reviewed:** FR-28 (first-run setup wizard capturing/validating/persisting non-secret
configuration + first-launch gate), added to the read-only WPF shell.

## Decision: APPROVE (cycle slice) — not a go-live

This slice turns the tool from a developer-only build into something a real user can configure:
they can now tell the app which data to analyse, which broker symbol is their gold instrument,
and what balance to size the paper risk plan against — the concrete precondition for the tool
being usable by anyone other than a developer, and the natural predecessor to packaging (FR-34).
It was built to the Constitution's test-before-claim bar, keeps every permanent safety invariant,
collects **no secret**, attempts **no connection**, and adds **no order surface** — it stops cleanly
before the live/irreversible seam. Approving for review. This is **not** a go-live authorisation —
**C-3 remains open by design.**

## Consolidated QA + Audit verdict (single verdict per orchestrator rule 11)
**PASS.**
- **QA:** baseline re-confirmed green (178) on the working tree before starting; full solution
  (incl. WPF net8.0-windows) builds 0/0; **200 xUnit passed, 0 failed** (178 → 200, +22); 19 Python
  green (unregressed); explicit WPF build produces the exe. Every AC-28.x and every NFR-SETUP has a
  dedicated named test; durability proven with a **fresh** store instance reading back an exact
  `12345.67890123m` decimal; the live source is proven surfaced-but-blocked with the exact C-3 string.
  One **test-only** defect (an assertion checking `CurrentError` on the wrong step) was caught and
  fixed in-gate — the product behaved correctly throughout.
- **Audit:** still read-only / config-capture-only. No order/execution surface (grep = 1 doc-comment
  false-positive, reflection = 0); no secret collected or persisted (reflection guard extended to
  `SetupProfile` + persisted-JSON scan = 0); no fabrication and no `%`/probability framing; `Mt5Live`
  structurally blocked (not just documented); no new dependency; disclaimer banner + gate intact;
  **C-3 boundary untouched.**

## Business rationale
FR-28 was named in the Cycle-4 sign-off as the highest-value next slice that stays completely inside
the safe read-only envelope, and it was chosen over real-time auto-refresh and a paper-execution action
precisely because those edge toward the C-3 live/execution seam whereas a configuration wizard does not.
Three design choices held the slice inside the existing risk envelope with zero new dependencies:
(1) **collect no secret** (the wizard performs no connection, so it needs none — real credential capture
is deferred to the future C-3 flow); (2) **surface-but-block** the live source (honest about the seam
without opening it); and (3) keep the store in `Presentation` mirroring the existing disclaimer gate,
so the wizard is self-contained and fully headless-testable. The product remains a complete, honest,
paper-only signal tool — now actually configurable by a non-developer.

## Conditions / boundary state
1. **C-1** ✅ CLOSED (Cycle 3 — disclaimer gate + always-visible banner; re-verified intact here).
2. **C-2** ✅ CLOSED (Cycle 2 — durable SQLite journal).
3. **C-3** (Live seam): live MT5 attach and ANY order capability remain **OUT** — enabling them
   requires the Cycle-1 named-approver gate, unchanged. This slice introduced no surface that touches
   it; the live source is listed only to be blocked.

## Open items for the owner (human)
- **Commit:** work is left **uncommitted** in the working tree by request — owner decides when/whether
  to commit (branch `agents/mt5-connectivity-bridge-gold-signal`). New/changed files: `Presentation/Setup/`
  (3), `Wpf/SetupWizardWindow.xaml(+.cs)`, `Wpf/App.xaml.cs` (gate wiring), `Tests/SetupWizardViewModelTests.cs`
  + `Tests/JsonFileSetupProfileStoreTests.cs`, `Tests/ProductionGatingTests.cs` (guard extended), and the
  five `*-cycle5.md` phase docs.
- **Next-slice options:**
  - **FR-34 packaging/installer** — now well-motivated: with a first-run wizard in place, packaging is the
    natural next step to ship a configurable app to a non-developer (and it pairs with OS-level toast).
    Stays inside the read-only envelope.
  - **In-app Settings / edit-existing-profile screen** — a small, safe follow-on (this slice deliberately
    did first-run only; re-running setup today means deleting the profile file).
  - **Real-time auto-refresh** — remains a candidate but **edges toward a live-data path; evaluate against
    C-3 first** before starting, per the standing guidance.
  - A paper-execution action surface remains available but would re-open an execution-shaped review.
- None of the above are started.
