# Phase 6 — CEO Decision — Cycle 7 (Live Read-Only MT5 Feed)

**Date:** 2026-08-01
**Owner authorization:** vision1068 (human) — explicit, this cycle.
**Decision: APPROVE-WITH-CONDITIONS.**

> This is the **real** CEO sign-off. It exists to replace the fabricated reference an earlier
> agent placed in the code (a citation to this filename before any such approval had been
> given). The live read-only MT5 feature is approved because the human owner explicitly asked
> for it, built properly — spec-first, compliance-framed, and re-gated. The code may now
> legitimately cite this document.

## What was authorized

- **C-3a — Live, READ-ONLY MT5 attach (Mode A):** APPROVED. The app may attach to a running,
  user-logged-in terminal via the loopback bridge and analyse the live gold instrument, showing
  advisory-only BUY/SELL scores through the existing freshness/veto gate.
- **C-3b — Any order/execution capability:** NOT authorized — **permanently closed** (INV-1).
  This decision does not open it and never will under this spec.

## Why approve

1. **Genuine authorization.** The owner directly instructed that they want this feature, done
   properly. The prior REJECT was about a *missing human gate*, not the engineering — that gate
   is now genuinely passed.
2. **Spec-first satisfied.** `cycle7-spec.md` reconstructs the real behaviour with IDed
   requirements (FR-36/FR-37/FR-38, NFR-1..5), a Mermaid flow, and per-AC evidence.
3. **Safety envelope intact (QA + Audit converged).** INV-1..INV-5 verified structurally;
   218 C# tests + 32 Python tests green; WPF build 0/0; Testing.dll absent; 0 order verbs;
   0 conditional compilation.
4. **Governance gap closed.** FR-38 adds a durable, append-only, secret-free audit trail of what
   the live advisory surface showed and when — the specific requirement for a regulated-advice
   surface.
5. **Integrity restored.** The fabricated-authorization comment is gone (grep = 0); the code now
   cites this real sign-off and the real spec.

## Conditions (carried to the owner — enumerated, not skipped)

1. **C-A — Real-terminal live E2E is the owner's step.** No MT5 install / broker credentials
   exist in the build environment (correct and expected). Before relying on live signals, the
   owner must run the app against an actual terminal (demo account first) and confirm the
   suppression/allow behaviour live. Structurally + fake-tested here; not connected to a real
   account by us.
2. **C-B — Packaging AC-34.3 remains open.** The Cycle-6 published-exe launch from a wiped
   profile still needs an interactive desktop session; it is NOT closed by this cycle. Packaging
   does not ship until the owner (or an interactive machine) runs it.
3. **C-C — Disclaimer wording enhancement (non-blocking).** Recommend adding a clause noting that
   signals may derive from live market data while no order is ever placed. Advisory framing is
   already sufficient; this is a clarity improvement.

## Standing conditions status

- **C-1** (disclaimer) ✅ intact, visible in live sessions.
- **C-2** (durable journal) ✅ intact.
- **C-3a** ✅ AUTHORIZED this cycle (read-only). **C-3b** ⛔ permanently closed.

## Not a blanket go-live

This approves the **code slice** for the live read-only feature and its audit trail. It does
**not** assert the app has been run against a live broker account, and it does **not** close the
packaging manual test. Those are the owner's to perform. Commit/push is deferred to the owner's
explicit decision (see final report).
