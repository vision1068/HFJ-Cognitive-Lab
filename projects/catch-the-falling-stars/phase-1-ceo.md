# Phase 1 — CEO: Business Understanding

## Business objective
"Catch the Falling Stars" is a portfolio/demo product: a small, complete, polished browser game that proves the HFJ-Cognitive-Lab delivery pipeline end-to-end (intake → architecture → adversarial plan review → build → verified QA → audit → sign-off) on a zero-dependency artifact anyone can open and judge in 10 seconds.

## Success criteria
- **SC-1** All 12 FRs and 7 NFRs in brief.md implemented; every AC demonstrably met or covered by a manual test case.
- **SC-2** Zero external dependencies — the deliverable is exactly `index.html`, `css/style.css`, `js/game.js` and runs from `file://`.
- **SC-3** QA phase contains actual command output (constitution art. 2), not assertions.
- **SC-4** Audit finds no High-severity security or accessibility gaps.
- **SC-5** The game feels finished: night-sky polish, smooth motion, coherent HUD, no console errors.

## Strategic risks
- **R-1** Scope creep (particles, sound, levels) delaying a demo piece — mitigation: FR list is frozen; extras rejected.
- **R-2** "Works on my machine" animation/timing bugs — mitigation: delta-time loop (NFR-2) and explicit pause/blur edge cases in the Plan Review Gate.
- **R-3** Untestable monolithic JS undermining SC-3 — mitigation: NFR-7 mandates exportable pure logic.
- **R-4** Accessibility treated as afterthought — mitigation: ux-guidelines items are ACs, audited in Phase 5.

## Go decision
Proceed to Phase 2. Effort is small and the pipeline-proof value is high.
