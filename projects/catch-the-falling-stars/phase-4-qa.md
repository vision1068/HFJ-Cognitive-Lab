# Phase 4 — QA: Verification

## Verification Evidence (actual command output — constitution art. 2)

### 1. Syntax check
Command: `node --check js/game.js`
```
SYNTAX OK   (exit 0)
```

### 2. Pure-logic test harness (Node, 21 assertions)
Harness: scratchpad `test-game.js`, `require()`s `js/game.js` via its conditional `module.exports` (NFR-7). Command: `node test-game.js`
```
ok 1 - rectsOverlap: overlapping rects
ok 2 - rectsOverlap: touching edges do not overlap
ok 3 - rectsOverlap: disjoint rects
ok 4 - fallSpeed: base at score 0 is 140
ok 5 - fallSpeed: score 20 (320) > score 0 (140)
ok 6 - fallSpeed: capped at 460
ok 7 - spawnInterval: tightens with score, floor 450
ok 8 - catch star: +1 point, lives unchanged
ok 9 - catch golden: +5 points
ok 10 - catch bomb: -1 life, score unchanged (D-1)
ok 11 - miss star: -1 life
ok 12 - miss bomb: no penalty
ok 13 - clampBasketX: left wall
ok 14 - clampBasketX: right wall
ok 15 - clampBasketX: in-bounds passthrough
ok 16 - sanitizeHighScore: valid string
ok 17 - sanitizeHighScore: garbage -> 0
ok 18 - sanitizeHighScore: null/negative/NaN -> 0
ok 19 - roundOver: timer wins over lives on same frame
ok 20 - roundOver: lives
ok 21 - roundOver: still playing
1..21 — all 21 tests passed
```

### 3. Unsafe-pattern scan
Command: `grep -noE 'eval\(|innerHTML|on[a-z]+=' index.html js/game.js`
```
index.html:5:ontent=      ← false positive: <meta ... content="..."> attribute
js/game.js:3:innerHTML    ← false positive: header comment stating innerHTML is NOT used
```
No real inline handlers, eval, or innerHTML usage. PASS.

## Traceability (art. 3)
FR-4/FR-10/FR-11 → tests 8-10 · FR-5 → 11-12 · FR-7 → 4-6 · FR-9/NFR-5 → 16-18 · D-2/F-2 → 19-21 · FR-1/FR-2 clamp → 13-15 · collision core → 1-3. FR-2/FR-3/FR-6/FR-8/FR-12 + NFR-2/3/4/6 → manual cases below (DOM/canvas, not Node-coverable).

## Manual browser test cases (Given/When/Then)
- **MT-1 (AC-1)** Given index.html opened via file://, When page loads, Then start overlay shows and canvas renders night sky with no console errors.
- **MT-2 (AC-2)** Given a running round, When ArrowLeft/ArrowRight held, Then basket moves and stops at walls; When mouse moves over canvas, Then basket follows mouse (last input wins).
- **MT-3 (AC-3/AC-4)** Given falling entities, When basket touches star/golden/bomb, Then HUD shows +1/+5/−1 life respectively with icon+text.
- **MT-4 (AC-4)** Given 1 life left, When a star is missed, Then Game Over overlay appears with "Out of lives" and final score.
- **MT-5 (AC-6)** Given a survivable round, When timer reaches 0, Then Game Over shows "Time's up"; When score > stored best, Then "New high score" shows and persists after reload.
- **MT-6 (AC-7 / F-1)** Given PLAYING, When Pause pressed and 30s elapse, When Resume pressed, Then entities continue from exact positions (no teleport) and timer resumes where it stopped.
- **MT-7 (NFR-4/F-6)** Given PLAYING, When window loses focus, Then game auto-pauses; Given START screen, When window blurs, Then state stays START.
- **MT-8 (AC-8/F-4)** Given a private-browsing window with storage blocked, When a round is played, Then no errors and the "(this session)" note is visible.
- **MT-9 (F-8/NFR-3)** Given keyboard-only navigation, When state changes, Then focus lands on the visible primary button and focus ring is clearly visible.
- **MT-10 (F-7)** Given PAUSED mid-round, When Restart pressed, Then score=0, lives=3, timer=60, field cleared.

## QA verdict
**PASS** — all automatable checks green with evidence; manual suite defined for browser-only behaviour. Independent Validation note: the syntax check and full harness were executed in this session by the orchestrator (rule 8), not taken on claim.
