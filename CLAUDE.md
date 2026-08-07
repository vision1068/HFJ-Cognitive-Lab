# AI -Cognitive-Lab —AI Company

## Default behavior
When the user types any instruction, delegate to @agent-orchestrator.
Do not respond directly for business or technical problems.
Exceptions: simple file operations, project setup, Claude Code questions.

## Approval policy
Only the final CEO decision (Phase 6 in a full engagement, or the CEO's
sign-off in any pattern that ends with one) is an approval gate.
Do not pause mid-engagement to ask the user to approve intermediate
phases or orchestrator-level routing decisions (architect, backend,
frontend, middleware, qa, auditor, etc.) — run them through and present
the consolidated result. Only stop early to ask the user something if
intent itself is ambiguous (per orchestrator Pattern classification) or
the user explicitly asks to review a plan before implementation.

## Available agents
orchestrator
ceo
business-analyst
architect
backend
frontend
mobile
devops
middleware
crm-onprem
power-platform
fo-developer
agent-developer
qa
auditor
codex-rescuer



## Project output structure
All outputs written to: projects/<name>/
  brief.md, phase-1-ceo.md, phase-2-arch.md, phase-3-tech.md,
  phase-4-qa.md, phase-5-audit.md, phase-6-ceo.md, full-engagement.md

## Requirements intake & company memory
New projects start with the requirements-intake skill (3-round
interview → IDed brief.md), never from a one-liner. Before any
engagement, read .claude/memory/lessons-learned.md; after Phase 6,
append a retrospective entry to it (orchestrator rules 7-9).

## Constitution — 7 non-negotiable standards

Scaled down from ConnectSW's 14-article constitution
(github.com/Tamoura/Claude-Code-creates-the-SW-company) to what
actually matters at our size. Every agent that writes or reviews code
follows these; the orchestrator verifies compliance at each checkpoint.

1. **Spec-first.** No implementation output before a spec exists with
   IDed requirements (FR-#/NFR-#/AC-#). Ambiguity gets a
   `[NEEDS CLARIFICATION]` marker, never a guess.
   See `.claude/skills/spec-driven-dev.md`.

2. **Test before claim.** No task is marked complete without the
   5-step Verification-Before-Completion gate — actual command output,
   not "should work now." See `.claude/protocols/quality-verification.md`.

3. **Traceability.** Requirement IDs carry through commits, tests, PRs,
   and the QA/Audit phase outputs. If an ID can't be found in the test
   suite, treat that requirement as unverified.

4. **Secure by construction.** OWASP Top 10 patterns are followed at
   write-time, not caught later in review. See
   `.claude/protocols/secure-coding.md`.

5. **Quality gates are blocking.** The 6 gates in
   `.claude/quality-gates/checklist.md` (Spec Consistency → Functional
   → Security → Performance → Testing → Production Readiness) are
   sequential and blocking — a project does not skip ahead.

6. **Diagram-first.** Anything that can be drawn (architecture,
   integration flow, multi-step business process) must be drawn in
   Mermaid, not just described in prose.

7. **Minimal code.** Before implementing, climb the 7-rung decision
   ladder (necessity → reuse → standard library → native platform →
   installed dependencies → one-liner → implement) — write only the
   code that has to exist. Never cut validation, security, or
   accessibility to save lines. See `.claude/skills/minimal-code.md`.

Amendments to this Constitution require explicit user approval — no
agent may loosen these standards on its own judgment.
