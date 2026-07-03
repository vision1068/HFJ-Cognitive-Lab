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



## Project output structure
All outputs written to: projects/<name>/
  brief.md, phase-1-ceo.md, phase-2-arch.md, phase-3-tech.md,
  phase-4-qa.md, phase-5-audit.md, phase-6-ceo.md, full-engagement.md
