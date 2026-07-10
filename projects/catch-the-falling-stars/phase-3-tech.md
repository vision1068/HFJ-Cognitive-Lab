# Phase 3 — Implementation (Frontend)

Routing note: the deliverable is pure client-side; backend, middleware, and crm-developer had no scope in this engagement, so Phase 3 ran as a single frontend build track (orchestrator rule 2's parallelism applies only when multiple tracks have scope).

## Files
- `index.html` — semantic markup: canvas play field, aria-live HUD, three overlay dialogs (start/pause/gameover), toolbar buttons, storage-fallback note. No inline handlers.
- `css/style.css` — night-sky gradient theme, panel HUD, ≥44px buttons, `:focus-visible` 3px outline, tabular numerals for HUD.
- `js/game.js` — three layers: pure logic (exported for Node when `module` exists), engine (state machine, spawner, collisions, timer), adapter (canvas render, DOM HUD, input, storage wrapper, rAF loop).

## Key implementation decisions
- Delta-time rAF loop, dt clamped to 50ms; `lastFrame` nulled whenever not PLAYING and on visibilitychange (F-1).
- Timer evaluated before collisions; `endRound()` idempotent (F-2, D-2).
- `activeInput` arbitration between keys and mouse/touch; mouse uses eased target-follow, keys use 420px/s velocity (F-3, D-3).
- Storage adapter wraps read AND write in try/catch with in-memory fallback + visible "(this session)" note (F-4, NFR-5).
- Blur/visibility auto-pause only from PLAYING (F-6, NFR-4); single `resetRound()` for every restart path (F-7); focus moved to the visible overlay's primary button on each state change (F-8).
- Bomb behaviour per D-1: catch = −1 life, miss = free. Documented in brief.md.
- All dynamic DOM writes via `textContent`; no eval, no network, no innerHTML.
- Sprites drawn as canvas paths: 5-point stars (white / glowing gold), bomb as dark circle with red outline + fuse + spark — shape distinguishes types, not colour alone (NFR-6, NFR-3).

## Task ledger (rule 10 format)
- `[frontend] build markup & overlays | index.html | AC-1, AC-7 UI present, no inline handlers. Run: grep -cE 'on[a-z]+="' index.html` → 0 matches. DONE
- `[frontend] theme & a11y styles | css/style.css | buttons ≥44px, :focus-visible outline present. Run: grep -c 'min-height: 44px\|focus-visible' css/style.css` → both present. DONE
- `[frontend] game engine + pure logic | js/game.js | node --check passes; 21 logic tests pass. Run: node --check js/game.js && node scratchpad/test-game.js` → evidence in phase-4-qa.md. DONE
