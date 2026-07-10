# Phase 2.5 — Plan Review Gate (QA + Auditor, adversarial)

Two reviewers examined phase-2-arch.md to break it. Findings merged into a single report per orchestrator rule 11. Two iterations were needed.

## Iteration 1 findings

| # | Src | Finding | Resolution (plan amendment) |
|---|-----|---------|------------------------------|
| F-1 | QA | **Pause-during-fall drift:** naive `now - lastFrameTime` after a long pause produces a huge dt, teleporting stars past the basket (silent life loss). | Already partly covered by 50ms dt clamp; amendment: also reset `lastFrameTime` on PAUSED→PLAYING **and** on `visibilitychange` back to visible. ACCEPTED into arch (loop rules). |
| F-2 | QA | **Timer vs lives race (D-2):** if the final star is missed on the same frame the timer hits 0, which end path wins? Unspecified → flaky tests. | Amendment: evaluate timer first, then collisions; GAMEOVER transition is idempotent — first trigger wins, second is a no-op. Same screen either way, so player-visible behaviour identical. |
| F-3 | QA | **Key+mouse conflict (D-3):** holding ArrowLeft while moving mouse right could jitter. | Amendment: explicit `activeInput` flag; a keydown sets `'keys'`, a mousemove sets `'mouse'`; only the active source is applied per frame. Keyup with no keys held keeps `'keys'` but velocity 0 (basket stops, doesn't snap to stale mouse x). |
| F-4 | Aud | **localStorage in private browsing:** Safari private mode throws on `setItem`; plan said try/catch on read only. | Amendment: wrap **read and write** in one storage adapter with in-memory fallback + HUD "(this session)" note. |
| F-5 | Aud | **High-score injection:** stored value rendered into DOM — if a hostile page on the same origin poisoned the key, `innerHTML` render would be XSS. | Plan already mandates `textContent` + `sanitizeHighScore` numeric coercion. Verified adequate. CLOSED. |
| F-6 | QA | **Window blur while on GAMEOVER/START:** blur handler must not force PAUSED from non-PLAYING states. | Amendment: blur auto-pause only applies when state == PLAYING. |
| F-7 | QA | **Restart during PAUSED** leaving stale entities/timer. | Amendment: `resetRound()` is the single reset path used by every Restart edge; clears entities, score, lives=3, timer=60. |
| F-8 | Aud | **Focus trap after overlay swap:** hidden Start button retaining focus breaks keyboard users. | Amendment: on each state change, move focus to the primary button of the visible overlay. |

## Iteration 2
Re-review of the amended plan. No new High findings. One Low noted and accepted as-is: rAF timer granularity means the "60s" round is accurate to ±1 frame — immaterial.

## Verdict
**PASS-WITH-CONDITIONS** — conditions F-1..F-4, F-6..F-8 are binding on the Phase 3 implementation and become explicit QA checks in Phase 4. Proceed to build.
