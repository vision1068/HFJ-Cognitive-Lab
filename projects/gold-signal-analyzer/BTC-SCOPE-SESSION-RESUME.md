# BTC Scope Amendment — Session Resume / Context Record

**Saved:** 2026-07-26 · **Updated:** 2026-07-27 · **Project:** gold-signal-analyzer
**Purpose:** Snapshot so this thread can be resumed cleanly after Claude is closed.
**Nature of work across both sessions:** SPEC-ONLY. No code changed. No git add/commit performed.

---

## 1. What the 2026-07-26 session did (DONE)

Resolved the four `[NEEDS CLARIFICATION]` markers in `BTC-INSTRUMENT-SCOPE-ADDENDUM.md` using the
user's answers, and produced the BTC disclaimer draft. All changes were **non-destructive** (original
text preserved verbatim; only additive `→ RESOLVED` pointers + a new §10 resolution log).

### Files changed that session (only these two):
1. **`BTC-INSTRUMENT-SCOPE-ADDENDUM.md`** — edited (additive only): §7 markers 1–4 resolved, §1 CEO
   note appended, FR-43/FR-44/FR-45/FR-24 amendment notes appended, new §10 Clarification Resolutions.
2. **`BTC-DISCLAIMER-DRAFT.md`** — NEW file. The disclaimer draft, banner-labeled NOT APPROVED (at the time).

### The four user answers applied that session:
- **Q1 CONCURRENT vs MODE-SWITCH → CONCURRENT** (user overrode CEO's MODE-SWITCH). Both instruments analyzed at once.
- **Q2 BTC disclaimer → architect drafts for review** (delivered as `BTC-DISCLAIMER-DRAFT.md`).
- **Q3 BTC risk defaults → SAME AS GOLD** (0.5% risk, min 1.5 R:R, per-instrument).
- **Q4 BTC symbol → `BTC/USD` = normalized identity confirmed**; exact broker string still pending.

---

## 2. What the 2026-07-27 session did (DONE) — the two remaining open items are now CLOSED

1. **FR-24 position-cap default under CONCURRENT → RESOLVED: `per-instrument`, final.**
   User confirmed the tentative default. Max 1 gold position **and** max 1 BTC position may be open
   simultaneously (not a combined total). Recorded in addendum **§10.1a** (updated) and the §5 FR-24
   amendment note.

2. **BTC disclaimer draft → RESOLVED: APPROVED as-is, no edits.**
   `BTC-DISCLAIMER-DRAFT.md` is now the **authoritative BTC source text** — the §40 analogue for BTC.
   File banner updated from DRAFT to APPROVED. FR-44's acceptance criteria (AC-44.1/AC-44.4) may now
   be verified against it once the disclaimer surface is built (spec Phase 6). Recorded in addendum
   **§10.2** (updated), plus the FR-44 resolution note in §4.

### Files changed this session (only these two, both additive):
1. **`BTC-INSTRUMENT-SCOPE-ADDENDUM.md`** — §10.1a and §10.2 status updated to RESOLVED; §7 status
   banner, §5 FR-24 note, and §4 FR-44 resolution note updated with `→ RESOLVED 2026-07-27` pointers.
   Original 2026-07-26 text preserved verbatim throughout (same non-destructive pattern).
2. **`BTC-DISCLAIMER-DRAFT.md`** — banner changed from DRAFT/NOT APPROVED to APPROVED; reviewer notes
   section updated to show each item resolved (approved as-is).

---

## 3. STILL OPEN — the only remaining item

1. **Exact BTC broker symbol string — PENDING (non-blocking).**
   User will supply from their MT5 Market Watch (e.g. `BTCUSDm`, `BTCUSD.a`). Deferred-collection
   pattern per spec §44; collect in `phase-6-ceo.md`. Needed before BTC live-read, not before more spec.
   See addendum §10.4.

All four `[NEEDS CLARIFICATION]` markers and both follow-on sub-questions from §7/§10 are now closed
except this single non-blocking, deferred-collection item.

---

## 4. Verified state at this session's close

- `brief.md` and all prior phase docs (phase-1…phase-6, phase-2.5, cycle2 docs) remain **untouched**.
- **No `.cs` files edited.** The modified code files shown in `git status` are pre-existing
  in-progress Cycle-2 work, unrelated to this spec task.
- Nothing committed. Branch: `gold-signal-analyzer-foundation`.
- Everything in the addendum remains **Cycle = R (Roadmap)** — no implementation authorized by any of
  this spec work, including the two new resolutions.

---

## 5. To resume

Read, in order: `BTC-INSTRUMENT-SCOPE-ADDENDUM.md` (esp. §10), then `BTC-DISCLAIMER-DRAFT.md`. The
only remaining action is the user supplying the exact BTC broker symbol string from their MT5 Market
Watch when they're ready to connect BTC live data (§10.4) — non-blocking for further spec work.
