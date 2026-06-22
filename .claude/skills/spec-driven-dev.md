---
name: spec-driven-dev
description: >
  Spec-Driven Development skill. Use before any implementation to produce
  a complete specification: requirements, architecture decisions, API
  contracts, and test criteria — all agreed before code is written.
  Based on github/spec-kit.
---

# Spec-Driven Development Skill

## When to invoke
- Starting any new feature, integration, or service
- Before delegating to backend, frontend, or middleware agents
- When stakeholder alignment is needed before build begins
- Reviewing whether existing code matches its original spec

## Spec structure

### 1. Problem Statement (1 paragraph)
What business problem are we solving? Who is affected? What does
success look like in plain language?

### 2. Scope
**In scope:**
- [explicit list]

**Out of scope:**
- [explicit list — prevents scope creep]

### 3. Functional Requirements
| ID   | Requirement                        | Priority | Acceptance Criteria       |
|------|------------------------------------|----------|---------------------------|
| FR-1 | [What the system must do]          | Must     | [How we verify it's done] |
| FR-2 | ...                                | Should   | ...                       |

### 4. Non-Functional Requirements
| ID    | Category      | Requirement                        |
|-------|---------------|------------------------------------|
| NFR-1 | Performance   | API response < 2s at P95           |
| NFR-2 | Security      | All endpoints require auth token   |
| NFR-3 | Availability  | 99.5% uptime during business hours |

### 5. Architecture Decision Record (ADR)
**Decision:** [What was chosen]
**Alternatives considered:** [What was rejected and why]
**Consequences:** [Trade-offs accepted]

### 6. API / Integration Contract (if applicable)
Endpoint, method, request schema, response schema, error codes.
Agreed and frozen before implementation begins.

### 7. Test Criteria
- Happy path: [describe]
- Edge cases: [list]
- Performance benchmark: [metric]
- Security test: [what to verify]

## Process rules
1. Spec is written BEFORE any agent produces implementation output
2. All stakeholders (CEO, Architect, BA) sign off on scope before Phase 3
3. No requirement added after spec is frozen without a change request
4. Every FR has a corresponding test in QA phase

## Output format
Full spec document written to projects/<name>/brief.md.
Flag any ambiguous requirements as [OPEN QUESTION] for stakeholder resolution.
