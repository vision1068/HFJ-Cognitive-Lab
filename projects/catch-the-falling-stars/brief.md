# Brief — Catch the Falling Stars

Client spec was fully detailed; the 3-round intake interview was compressed into direct ID extraction per orchestrator instruction. Decisions the spec delegated to us are documented as D-#.

## Product
Small browser game, pure HTML + CSS + JavaScript. Zero frameworks, zero CDN, runs from `file://` by opening `index.html`.

## Functional Requirements
- **FR-1** Player controls a basket at the bottom of the play area.
- **FR-2** Basket moves with left/right arrow keys AND mouse (both must work; last input wins — see D-3).
- **FR-3** Stars fall from the top at randomized x positions.
- **FR-4** Catching a normal star = +1 point.
- **FR-5** Missing a star (it exits the bottom uncaught) = -1 life.
- **FR-6** Game over when 3 lives are lost.
- **FR-7** Fall speed increases as score increases (documented difficulty formula, phase-2-arch.md).
- **FR-8** 60-second round timer; timer expiry ends the round (Game Over screen with final score; see D-2).
- **FR-9** High score persisted in `localStorage`, survives reloads, updated when beaten.
- **FR-10** Golden star (rarer) worth +5 points.
- **FR-11** Bombs fall and must be avoided. **D-1: catching a bomb costs 1 life** (not instant game over) — consistent with the miss penalty, keeps rounds recoverable and tension incremental. Missing a bomb costs nothing.
- **FR-12** Start, Pause (toggle to Resume), Restart buttons.

## Non-Functional Requirements
- **NFR-1** No frameworks, no CDN, no network calls; works offline over `file://`; CSP-friendly (no inline handlers, no eval).
- **NFR-2** Smooth animation via `requestAnimationFrame`, delta-time based (frame-rate independent).
- **NFR-3** Accessibility per ux-guidelines: visible focus states, buttons ≥ 44px touch targets, status conveyed by text + icon (never colour alone), live-region score/lives updates.
- **NFR-4** Auto-pause on window blur so the player never loses lives while tabbed away.
- **NFR-5** localStorage failure (private browsing / disabled) degrades gracefully: game fully playable, high score shown as session-only.
- **NFR-6** Polished night-sky visual theme; star-shaped sprites; no colour-only signalling.
- **NFR-7** Pure game logic (collision, difficulty, scoring) is unit-testable in Node via conditional `module.exports`.

## Acceptance Criteria
- **AC-1** Opening `index.html` from disk shows a start screen; Start begins a 60s round. (FR-8, FR-12, NFR-1)
- **AC-2** Arrow keys and mouse both move the basket during play; input ignored while paused. (FR-2)
- **AC-3** Catch star → score +1 announced in HUD; catch golden star → +5. (FR-4, FR-10)
- **AC-4** Miss star or catch bomb → lives -1 with text+icon feedback; 3rd loss → Game Over. (FR-5, FR-6, FR-11)
- **AC-5** Fall speed measurably higher at score 20 than score 0 per the documented formula. (FR-7)
- **AC-6** Timer reaching 0 ends the round; final score compared to stored high score; new record persists across reload. (FR-8, FR-9)
- **AC-7** Pause freezes movement and timer; Resume continues exactly; Restart resets score/lives/timer. (FR-12)
- **AC-8** With localStorage blocked, the game still runs and never throws. (NFR-5)
- **AC-9** `node --check js/game.js` passes and the pure-logic test harness passes. (NFR-7)

## Decisions
- **D-1** Bomb = -1 life (rationale above, FR-11).
- **D-2** Two end conditions coexist: lives=0 OR timer=0, whichever first → same Game Over screen.
- **D-3** Input arbitration: keyboard sets velocity, mouse sets target x; the most recent input source controls the basket.
- **D-4** localStorage key: `ctfs.highScore` (single numeric value, validated on read).

## Open items
- Hosting target: **[NEEDS CLARIFICATION]** — delivered as static files; any static host or file:// works.
- Named business approver on client side: **[NEEDS CLARIFICATION]** — internal CEO sign-off used for Phase 6.
