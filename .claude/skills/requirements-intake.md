---
name: requirements-intake
description: >
  Structured requirements-gathering interview. Use BEFORE any full
  engagement when the user brings a new project, app idea, or vague
  requirement. Produces a complete, IDed brief.md through a short
  step-by-step interview instead of guessing at intent. Adapted from
  metaswarm's brainstorming/issue-creation flow (github.com/dsifry/metaswarm).
---

# Requirements Intake Skill

## When to invoke
- User says "I want to build...", "new project", "new app", or describes
  a business problem with unclear scope
- The orchestrator classifies a request as Pattern A but the request is
  a single sentence — one sentence is never enough to build from
- Invoked directly via `/new-project`

## The interview — 3 rounds maximum

Keep it fast. Ask in batches (use AskUserQuestion with options where
possible), never one long questionnaire. Stop early if answers are clear.

### Round 1 — The problem (always ask)
1. **Who uses it?** (internal staff / public / specific role)
2. **What's the one thing it must do well?** (the core job, in the user's words)
3. **What exists today?** (nothing / spreadsheet / legacy system being replaced)

### Round 2 — Shape and constraints (ask what's still unknown)
4. **Platform preference** — web app, Power Apps, mobile, no preference
5. **Data** — where does the data come from? Real API/system, user-entered, or needs mock data first?
6. **Must-haves vs nice-to-haves** — force a split; everything can't be priority 1
7. **Go-live constraints** — deadline, approver name, hosting preference

### Round 3 — Confirm understanding (always do)
Play back a draft summary: "Building X for Y so they can Z. In scope: A, B.
Out of scope: C. Success looks like: measurable criterion."
Get an explicit yes/adjustment before writing the brief.

## Rules
- Never invent an answer the user didn't give — unknowns become
  `[NEEDS CLARIFICATION]` markers in the brief, per the Constitution
- Never skip Round 3 — the playback catches more errors than the questions
- If the user says "just decide" on an item, record the decision AND
  that it was delegated ("Platform: web app — delegated to company judgment")
- Apply `spec-driven-dev` formatting to the output: FR-#/NFR-#/AC-# IDs

## Output
Write `projects/<name>/brief.md` containing:
1. Problem Statement (from Round 1, in the user's own words where possible)
2. Users & Context
3. Scope: In / Out (from Round 2 item 6)
4. Functional Requirements table (FR-# / priority / acceptance criteria AC-#)
5. Non-Functional Requirements (NFR-#: performance, security, hosting)
6. Constraints & Go-live gates (deadline, named approver, hosting)
7. Open Questions (`[NEEDS CLARIFICATION]` items)

Then hand off to the orchestrator for the standard 6-phase engagement,
with the brief as Phase 1 input.
