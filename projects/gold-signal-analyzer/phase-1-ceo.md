# Phase 1 — CEO: Business Understanding & Success Criteria

## Business framing
The Gold Signal Analyzer's entire value rests on one thing: signals derived
from **genuine, fresh** market data. The connectivity bridge is the load-bearing
foundation — if the data path can silently go stale, fabricate a price, or leak
a credential, every downstream signal is worthless or dangerous, and the product
crosses into regulatory and safety risk. This slice therefore isn't "plumbing";
it's the layer where the product's trust guarantees are either constructed or lost.

The owner has scoped this cycle deliberately narrow — **read-only scaffolding,
no live attach** — precisely because the risky part (touching a real broker
terminal) must not be switched on before the safety machinery around it exists
and is proven. Build the seatbelts before the engine.

## Why this scope is the right business call
- De-risks the expensive/irreversible step (live broker connection) by proving
  the safety envelope first, cheaply, against fixtures.
- Produces a genuinely independent implementation the owner can compare against
  the sibling branch without cross-contamination — a real second opinion.
- Every safety invariant (no fabrication, no secrets, loopback-only, veto on
  stale) becomes a *structural* property with a test, not a promise.

## Success criteria (measurable)
| ID | Criterion | Met by |
|----|-----------|--------|
| SC-1 | All FR-7…FR-12 acceptance criteria implemented and covered by automated tests with real command output. | Phase 4 evidence |
| SC-2 | Zero live-attach code path reachable; no fabricated "live" data possible. | FR-11 tests; guarded MT5 source |
| SC-3 | Zero secrets in source; credentials only behind an OS-protected seam. | Security grep + NFR-1 test |
| SC-4 | Bridge cannot be exposed off-box or called unauthenticated. | FR-8 loopback + 401 tests |
| SC-5 | Signal generation provably pauses under every stale/invalid condition. | FR-12 gate tests |
| SC-6 | Clean-architecture layering; pure logic testable with no terminal. | Solution structure; NFR-10 |
| SC-7 | Independent build — nothing sourced from `C:/gsa-f`. | Fresh worktree; no cross-refs |

## Strategic risks identified (handed to Architect/Auditor)
- **R-1 Fabrication risk:** a "helpful" default that returns a last-known or
  synthetic price in live mode would be catastrophic. → Must be structurally
  impossible, not merely discouraged.
- **R-2 Credential leakage:** any password field on a config/DTO invites a
  secret in appsettings. → Config types must carry no secret member at all.
- **R-3 Silent staleness:** the worst failure is *looking* live while stale.
  → Freshness must default to "not fresh" and the veto must be conservative.
- **R-4 Regulatory:** even read-only, the product must never execute/modify
  trades or store withdrawal credentials (INV-1/INV-2).

## Decision
**Proceed** to Phase 2 (Architecture) under the cycle scope S-1…S-3 and
invariants INV-1…INV-3. Gate 6 sign-off is reserved for Phase 6.
