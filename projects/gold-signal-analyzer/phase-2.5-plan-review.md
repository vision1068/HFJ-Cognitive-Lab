# Phase 2.5 — Plan Review Gate (adversarial: QA + Auditor)

Per orchestrator rule 7, the architecture was reviewed **to find what's wrong**
before any implementation, from two hostile angles. Findings were merged into a
single verdict (rule 11). All were resolved in the design below before Phase 3
coding began.

## QA angle — "how would this let bad data through?"
| # | Finding | Resolution folded into the design |
|---|---------|-----------------------------------|
| Q-1 | If freshness starts as "Fresh" or "Unknown-but-allowed", the very first evaluation could emit a signal on no data. | `FreshnessStatus.Unknown` is the default AND the gate treats non-`Fresh` as suppress. Test: `Gate_suppresses_when_freshness_unknown`. |
| Q-2 | A tick with a future timestamp (broker/clock skew) would compute a *negative* age and read as "fresh forever". | Future-dated beyond tolerance → `Stale`. Test: `Future_dated_tick_is_treated_as_stale_not_fresh`. |
| Q-3 | "Bridge up but terminal not attached" could be treated as Connected and serve empty/again-fabricated data. | That state maps to `Reconnecting` (not Connected); data calls throw. Tests in `Mt5ProviderSeamTests`. |
| Q-4 | Backoff without a cap or jitter → thundering-herd / unbounded delay. | `ExponentialBackoffPolicy` caps at `Max` and applies bounded jitter; deterministic via injected RNG. `BackoffTests`. |
| Q-5 | CSV provider could be mistaken for live data downstream. | `IsLive == false` on Csv and Test providers; `IsLive` asserted per provider. |

## Auditor angle — "how does this leak or overreach?"
| # | Finding | Resolution |
|---|---------|-----------|
| A-1 | A `Password`/`InvestorPassword` field on a config record is the classic route to a secret-in-appsettings. | `Mt5ConnectionOptions` carries **no** secret member; reflection test `Connection_options_have_no_secret_field` fails the build if one is added. |
| A-2 | Bridge bound to `0.0.0.0` (a common default) would expose broker data to the LAN. | Loopback enforced at construction in **both** `BridgeEndpoint` (.NET) and `BridgeConfig` (Python). Tests reject `0.0.0.0`, `10.x`, `192.168.x`, hostnames. |
| A-3 | Unauthenticated localhost calls (any local process) could read market data. | Token required on every request; missing/empty/wrong → 401; constant-time compare. E2E test proves 401 without token. |
| A-4 | The real MT5 source could accidentally expose an order/trade call (INV-1). | `Mt5MarketDataSource` exposes read-only methods only; `get_candles` raises `NotImplementedError` this cycle; no `order_*` surface exists. |
| A-5 | Test provider could ship in production and be used to inject fake prices. | Separate assembly, unreferenced by production; verified physically absent from the Release output and via reflection. |

## Iteration log
- **Iteration 1:** findings above raised against the initial design.
- **Revision:** design amended (Unknown-default gate, future-skew rule, Reconnecting state, no-secret config, dual loopback guard, read-only Python surface, separate test assembly).
- **Iteration 2:** re-review — no open HIGH findings; all findings have a named test.

## Merged verdict: **PASS-WITH-CONDITIONS**
Conditions (all must appear as passing tests in Phase 4, else this gate reverts to FAIL):
1. Freshness default + future-skew tests (Q-1, Q-2).
2. Not-connected / unmapped refusal tests (Q-3, FR-11).
3. Backoff cap + jitter tests (Q-4).
4. No-secret-field reflection test (A-1).
5. Loopback-refusal tests both sides (A-2).
6. 401-without-token E2E test (A-3).
7. Test-provider-gated-out evidence (A-5).

→ Conditions were met in Phase 4 (see phase-4-qa.md). Proceed to Phase 3.
