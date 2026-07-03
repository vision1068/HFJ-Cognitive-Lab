# Quality Gates — HFJ-Cognitive-Lab

Six sequential gates. Each is blocking, not advisory — a project does not
advance past a gate it fails. Adapted from ConnectSW's multi-gate system
(github.com/Tamoura/Claude-Code-creates-the-SW-company), scaled to our size.

Gates run in this order. Earlier gates are cheaper to fail at than later
ones — that's why they're ordered this way.

---

## Gate 1 — Spec Consistency

**Runs:** before Phase 3 (implementation) begins
**Owner:** orchestrator, using the `spec-driven-dev` skill
**Checks:**
- [ ] Every functional requirement has an ID (FR-#) and an acceptance criterion
- [ ] No `[NEEDS CLARIFICATION]` markers remain unresolved
- [ ] Scope (in/out) is explicit — no implicit scope creep possible
- [ ] Prior phase outputs (CEO objective, Architect design) don't contradict the spec

**Fails if:** any requirement is ambiguous enough that two engineers would implement it differently.

---

## Gate 2 — Functional / Browser-First

**Runs:** after Phase 3 implementation, before Phase 4 QA
**Owner:** whichever engineer agent built the feature, verified by `codex-rescuer`
**Checks:**
- [ ] The thing actually runs — dev server starts, app opens, no console errors on load
- [ ] The primary user flow (the reason this was built) works end-to-end at least once, actually executed
- [ ] For web apps: works in at least one real browser, not just "should render"

**Fails if:** the feature has not been actually run and observed working by the agent claiming completion. "Should work" is not a pass.

---

## Gate 3 — Security

**Runs:** before any PR/deploy
**Owner:** `auditor`, checklist from `.claude/protocols/secure-coding.md`
**Checks:**
- [ ] Security self-review checklist (secure-coding.md) fully checked, not skimmed
- [ ] No HIGH/CRITICAL dependency vulnerabilities (`npm audit` or equivalent)
- [ ] No secrets in source, `.env` correctly gitignored
- [ ] Auth/authorization checks verified server-side, not just assumed from UI

**Fails if:** any HIGH/CRITICAL finding is open with no CEO-approved, time-boxed exception.

---

## Gate 4 — Performance

**Runs:** before staging/production deploy
**Owner:** engineer agent that built it, spot-checked by `codex-rescuer`
**Checks (web apps):**
- [ ] Lighthouse performance score ≥ 90 where applicable, or a documented reason it can't be measured yet
- [ ] Bundle size reasonable for the app's purpose (flag anything > 500KB gzipped and justify it)
**Checks (Power Platform):**
- [ ] Gallery/list queries are delegable (no silent 500-record cap)
- [ ] App load time measured, not assumed

**Fails if:** no measurement was actually taken — "it feels fast" is not a pass.

---

## Gate 5 — Testing

**Runs:** before CEO checkpoint (Phase 6 or equivalent)
**Owner:** `qa`, verified by `codex-rescuer`
**Checks:**
- [ ] Happy path, at least one boundary condition, and at least one failure path all have a test or a verified manual check
- [ ] Every acceptance criterion from the spec maps to something that was actually checked
- [ ] Verification evidence exists per `.claude/protocols/quality-verification.md` (Part 4) — actual command output, not a claim

**Fails if:** any P1 defect is open, or verification evidence is missing for a claimed-complete task.

---

## Gate 6 — Production Readiness

**Runs:** immediately before go-live
**Owner:** `devops` + named human approver (never auto-approved)
**Checks:**
- [ ] Rollback plan exists and is documented, not just assumed possible
- [ ] Monitoring/error visibility exists (even minimal — a log a human will actually check)
- [ ] Named approver has explicitly signed off in writing
- [ ] Data residency / compliance requirements from the Auditor's Phase 5 review are closed, not just noted

**Fails if:** there is no named human approver, or any Auditor-flagged HIGH item is still open.

---

## How this integrates with existing agents

This does not replace the 6-phase engagement (CEO → Architect → Build →
QA → Audit → CEO) — it's the enforcement layer underneath it:

| Engagement phase | Corresponding gate(s) |
|---|---|
| Phase 2 (Architect) → Phase 3 (Build) | Gate 1 — Spec Consistency |
| End of Phase 3 (Build) | Gate 2 — Functional/Browser-First |
| Phase 5 (Audit) | Gate 3 — Security |
| Phase 5 (Audit) | Gate 4 — Performance |
| Phase 4 (QA) | Gate 5 — Testing |
| Phase 6 (CEO Final Decision) | Gate 6 — Production Readiness |
