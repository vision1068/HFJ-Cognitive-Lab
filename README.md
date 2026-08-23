# HFJ-Cognitive-Lab — AI Company

**HFJ-Cognitive-Lab** is an AI-native software company that runs entirely inside Claude Code. Instead of a team of human specialists, every function of a real software company — business strategy, architecture, development, QA, and governance — is handled by a dedicated AI agent with its own responsibilities, standards, and guardrails.

You talk to one person: the **Orchestrator**. It reads your request, decides which specialists are needed, runs them in the right order, and hands you back a finished result.

---

## How the Company Works

### 1. You never talk to specialists directly
Every instruction — a business problem, a design request, a bug, a question — goes to `@agent-orchestrator` first. The orchestrator classifies your intent and delegates. This is enforced in [`CLAUDE.md`](./CLAUDE.md):

> When the user types any instruction, delegate to @agent-orchestrator.
> Do not respond directly for business or technical problems.

**Exceptions** (handled directly, no delegation): simple file operations, project setup, Claude Code questions.

### 2. The Orchestrator classifies intent into 6 patterns

| Pattern | Triggers | What happens |
|---|---|---|
| **A — Full Engagement** | "build", "design", "create a system", "we need a solution" | Runs all 6 phases below, in order |
| **B — Single Specialist** | "what does the architect think", "backend design for X" | Calls one agent, returns its output directly |
| **C — Phase Revision** | "revise", "the architect missed", "update the QA section" | Re-reads the existing phase file, re-runs only that agent |
| **D — Parallel Consultation** | "what do backend and QA think" | Calls named agents in parallel, synthesizes the results |
| **E — Audit/Governance Check** | "audit this", "is this secure", "compliance requirements" | Calls the Auditor only |
| **F — Memory/Status Query** | "what did we decide", "what phase are we on" | Reads from `projects/`, summarizes — no agents called |
| **G — Broken Build / Code Rescue** | "this is broken", "tests are failing", "review this code" | Calls Codex Rescuer only — reproduces before diagnosing |

If your request is ambiguous between two patterns, the orchestrator asks **one** clarifying question rather than guessing.

### 3. Full Engagement — the 6-phase pipeline

When you describe a business problem from scratch, the orchestrator runs a complete engagement:

```
Phase 1 → CEO           Business objective + success criteria
Phase 2 → Architect     System design + technology stack
Phase 3 → 4 agents IN PARALLEL:
             Backend · Frontend · Middleware · CRM Developer
Phase 4 → QA            Test strategy + test cases
Phase 5 → Auditor       Security, compliance, governance review
Phase 6 → CEO           Final Approved / Rejected / Revised decision
```

Phase 3 always runs in parallel — the orchestrator never serializes backend, frontend, middleware, and CRM work. It waits for all four before moving to QA.

Every phase writes its output to a project folder:
```
projects/<project-name>/
  brief.md              ← original problem statement / spec
  phase-1-ceo.md
  phase-2-arch.md
  phase-3-tech.md        (backend + frontend + middleware + crm combined)
  phase-4-qa.md
  phase-5-audit.md
  phase-6-ceo.md
  full-engagement.md      ← consolidated final output
```

**Real example:** `projects/doh-flight-checker/` — a full 6-phase engagement that took a one-line request ("build a flight checker app") through CEO framing → architecture → parallel build specs → QA test plan → security audit → final approval, before a single line of code was written.

---

## The Team

### Orchestrator
The only agent you talk to directly. Routes every request, enforces the phase order, never lets a weak agent output cascade forward — if an output is incomplete, it re-calls that agent with a tighter prompt instead of accepting it.

### CEO — *Phases 1 & 6*
Defines the business objective in plain language, sets measurable success criteria, and makes the final **Approved / Rejected / Revised** call with business justification. Never touches architecture or code — stays entirely in the business layer.

### Architect — *Phase 2*
Designs system boundaries, integration contracts, and justifies every technology choice — no assumptions allowed. Enforces: configuration-driven design, async over synchronous, thin plugins/heavy services, versioning from day one.

### Backend — *Phase 3*
API contracts, data models, C# implementation patterns. Standards: every entity has `created_by/modified_by/created_on/modified_on`, all IDs are GUIDs, audit logs are append-only, business rules load from configuration — never hardcoded.

### Frontend — *Phase 3*
UI/UX, model-driven app forms, PCF components, Power Apps canvas design, Power BI dashboard layout. Constrained to Unified Interface compliance and model-driven apps as the primary surface (CRM on-premise — no portals unless explicitly scoped).

### Middleware — *Phase 3*
API contracts, message queue payloads, transformation logic. Hard rule: queue messages carry IDs only (never full entity payloads) — services always re-fetch live data to avoid stale-data risk.

### CRM Developer — *Phase 3*
Dynamics CRM on-premise: plugins, entity schemas, security roles, Power Automate. Hard constraint: 2-minute plugin sandbox limit enforces async handoff to services; on-premise means Organization Service SDK, not Dataverse Web API.

### QA — *Phase 4*
Test strategy and Given/When/Then test cases across 7 categories: functional correctness, boundary conditions, CRM plugin constraints, audit trail integrity, high-volume/performance, regression, and security.

### Auditor — *Phase 5*
Security risk assessment, regulatory/governance compliance (project-specific, never assumed by default), data residency, audit trail validation. Operating principle: **over-flagging is better than missing a risk** — every governance gap is flagged before go-live, no exceptions.

### Business Analyst
Requirements gathering, user stories (As a / I want / So that), AS-IS → TO-BE process mapping, gap analysis. Bridges business stakeholders and the technical agents before Phase 2 begins.

### Power Platform Developer
Power Automate flows, canvas/model-driven app configuration, Dataverse schema, Power BI reports, and Power Platform ALM (Dev → Test → UAT → Prod pipelines, managed solutions only in production).

### F&O Developer
Dynamics 365 Finance & Operations: X++ extensions (extension-only, never overlayering base objects), data entities, financial workflows, batch processing.

### Mobile Developer
Power Apps mobile, offline-capable canvas apps, responsive layouts for field staff and approvers.

### DevOps
Azure DevOps pipelines, Power Platform Build Tools, environment promotion, rollback planning — no manual deployments to Production, ever.

### Agent Developer
Meta-role: designs and maintains the other agents themselves — writes agent definition files, configures skills and MCP integrations, engineers prompts for reliability.

### Codex Rescuer
Called in when something is broken — a failing build, a red test suite, a deploy stuck in a loop — or when another agent's code needs an independent second pass before it ships. Operating rule: **reproduce before diagnosing**. Never explains a failure from reading code alone if it can run the thing and read the actual output; never marks a fix "done" without re-running the original failing scenario. Root-causes rather than brute-force retrying the same change hoping for a different result.

---

## Skills Library

Beyond the specialist agents, the company maintains a shared **skills library** (`.claude/skills/`) — reusable domain knowledge any agent can invoke mid-task:

| Skill | Purpose |
|---|---|
| `qdb-design-system` | Brand design system generator — colors, typography, spacing, component tokens, layout templates, adapted to QDB's Navy/Gold brand |
| `ux-guidelines` | 99 UX rules across 11 categories (Navigation, Forms, Accessibility, Performance, etc.), each mapped to a Power Apps implementation with severity ratings |
| `bi-dashboard-styles` | 10 BI/Analytics dashboard styles (Executive, Financial, Real-Time Monitoring, Predictive, etc.) with layout wireframes and QDB color assignments |
| `power-platform-dev` | Canvas app, model-driven app, and Power Automate build patterns + ALM checklist |
| `canvas-apps-tools` | Paste-ready Power Fx formula patterns (filtering, offline sync, error handling) |
| `dataverse-schema` | Table/column/relationship design standards, required audit columns, business rule checklist |
| `dynamics365-mcp` | MCP server configuration for querying live Dynamics 365 / Dataverse data from Claude Code |
| `ui-ux-design` | Screen specs, user flow templates, accessibility checklist, QDB brand colors |
| `spec-driven-dev` | Spec-before-code discipline — functional/non-functional requirements, ADRs, frozen scope before Phase 3 begins, plus end-to-end requirement traceability (spec ID → commit → test → PR → QA/Audit) |
| `token-efficient` | Output compression rules for long engagements — trims agent prose, merges redundant points |
| `minimal-code` | 7-rung decision ladder applied before writing any implementation code (necessity → reuse → standard library → native platform → installed deps → one-liner → implement) — write only the code that has to exist |

---

## Constitution

Six non-negotiable standards every agent follows, scaled down from [ConnectSW's 14-article constitution](https://github.com/Tamoura/Claude-Code-creates-the-SW-company) to what actually matters at our size. Full text in [`CLAUDE.md`](./CLAUDE.md):

1. **Spec-first** — no implementation before an IDed spec exists (FR-#/NFR-#/AC-#); ambiguity gets `[NEEDS CLARIFICATION]`, never a guess
2. **Test before claim** — no task marked complete without actual command output proving it, not "should work now"
3. **Traceability** — requirement IDs carry through commits → tests → PRs → QA/Audit output
4. **Secure by construction** — OWASP patterns followed at write-time, not caught later
5. **Quality gates are blocking** — sequential, not advisory; a project doesn't skip ahead
6. **Diagram-first** — anything drawable (architecture, flow, process) gets a Mermaid diagram, not just prose

Amendments require explicit user approval — no agent loosens these on its own judgment.

---

## Protocols (`.claude/protocols/`)

Shared discipline any agent applies mid-task, adapted from ConnectSW:

| Protocol | Purpose |
|---|---|
| `quality-verification.md` | The 1% Rule, the anti-rationalization table (16 exact excuses agents use to skip quality steps, each with a counter), and the 5-step Verification-Before-Completion gate |
| `secure-coding.md` | OWASP Top 10 mapped to concrete patterns for our two stacks (Power Platform/CRM on-premise, and Node/React/TypeScript) — forbidden-pattern table and a security self-review checklist |

The anti-rationalization table is the single highest-value idea borrowed from ConnectSW: it documents the exact excuses ("it's just a prototype," "existing tests probably cover it," "time pressure") an agent reaches for to skip a test or check, with a scripted counter for each — turning a vague rule ("write tests") into something enforceable in the moment.

---

## Quality Gates (`.claude/quality-gates/checklist.md`)

Six sequential, blocking gates — a project does not advance past one it fails:

| Gate | Runs when | Owner |
|---|---|---|
| 1. Spec Consistency | Before Phase 3 implementation | orchestrator + `spec-driven-dev` |
| 2. Functional / Browser-First | After Phase 3, before Phase 4 | engineer agent, verified by Codex Rescuer |
| 3. Security | Before any PR/deploy | Auditor, via `secure-coding.md` |
| 4. Performance | Before staging/production | engineer agent, spot-checked by Codex Rescuer |
| 5. Testing | Before CEO checkpoint | QA, verified by Codex Rescuer |
| 6. Production Readiness | Immediately before go-live | DevOps + named human approver |

Each gate maps onto the existing 6-phase engagement — it's the enforcement layer underneath the phases, not a separate process.

---

## Commands (`.claude/commands/`)

Invocable shortcuts for common cross-project operations:

| Command | What it does |
|---|---|
| `/audit <project>` | Full 11-dimension score (Security, Architecture, Test Coverage, Code Quality, Performance, DevOps, Runability + Accessibility/Privacy/Observability/API Design) against 9 frameworks (OWASP, WCAG, GDPR, ISO 25010, DORA...). Two-part output: Executive Memo + Engineering Appendix |
| `/status [project]` | Quick status across all projects, or deep-dive one — phases completed, last CEO decision, open items |
| `/security-scan <project>` | Runs the `secure-coding.md` checklist and dependency audit, item by item, with evidence |
| `/pre-deploy <project>` | Walks Gate 6 (Production Readiness) plus confirms Gates 1–5 already passed |
| `/compliance-check <project>` | Focused governance/compliance pass — the standard Auditor Phase 5 mode |
| `/new-project <idea>` | Structured 3-round requirements intake interview → IDed `brief.md` → full 6-phase engagement |
| `/memory-consolidate` | Scans company memory for contradicted/redundant lessons, marks them Superseded (never deletes) — the CONSOLIDATE stage of the Memory Learning Loop |

---

## Hooks (`.claude/hooks/`)

Automation that fires on session events — the company reacts without being asked. Pattern from [diet103/claude-code-infrastructure-showcase](https://github.com/diet103/claude-code-infrastructure-showcase):

| Hook | Fires on | What it does |
|---|---|---|
| `session-start.sh` | SessionStart | Briefs every new session: active projects, phase progress, company memory count, constitution reminder |
| `skill-activation.sh` | UserPromptSubmit | Matches your prompt against `skill-rules.json` keyword rules and injects the relevant skill suggestions — so the right skill is applied even when nobody remembers to invoke it |
| `safety-guard.sh` | PreToolUse (Bash) | **Blocks** destructive commands (recursive delete of root/home, force-push to main/master, `terraform destroy`); **warns** on risky ones (bulk `git add`, `rm -rf`, hard reset) — from dralgorhythm's safety hooks |
| `file-guard.sh` | PreToolUse (Write/Edit) | Blocks agent edits to secret-bearing files (`.env`, private keys, certificates) — `.env.example` stays editable |
| `learn-capture.sh` | Stop | Auto-captures any `[LEARN] ...` line from agent output into company memory, deduplicated — corrections compound without anyone remembering to file them (from pro-workflow) |
| `memory-retrieve.sh` | UserPromptSubmit | Greps `lessons-learned.md` for entries matching the prompt's keywords (2+ distinct hits required to avoid noise) and surfaces only the relevant ones with their Status — the RETRIEVE stage of the Memory Learning Loop, see below |

`skill-rules.json` maps keywords → skills (e.g. "dashboard" → `bi-dashboard-styles`, "new app" → `requirements-intake`). Add a keyword rule whenever a new skill is created. All hooks are fail-soft: a hook error never blocks legitimate work.

---

## Memory Learning Loop

A file-based, no-infrastructure version of the RETRIEVE → JUDGE → DISTILL → CONSOLIDATE self-learning pipeline (concept from [ruvnet/ruflo](https://github.com/ruvnet/ruflo)'s intelligence plugin — theirs runs on a vector database with HNSW indexing and neural adapter training; at this company's memory size, `grep` does the same job without the infrastructure):

| Stage | How it works here |
|---|---|
| **RETRIEVE** | `memory-retrieve.sh` hook — keyword-matches your prompt against every lesson, surfaces only the relevant ones instead of forcing a full-file read |
| **JUDGE** | Every lesson carries a `Status:` field (`Unconfirmed` → `Confirmed` / `Contradicted`). The orchestrator updates it during Phase 7 when a lesson is actually put to the test — memory stops being write-only |
| **DISTILL** | Already your company's native format — every retrospective already writes `Lesson:` + `Rule going forward:`. Nothing new needed here. |
| **CONSOLIDATE** | `/memory-consolidate` command — finds contradicted or redundant entries and marks the older one `Superseded by <newer entry>`. Never deletes or rewrites — a correction is always a new entry, matching the append-only discipline already proven in the gold-signal-analyzer fabricated-authorization lessons. |

`codex-rescuer`'s diff-risk check (from `ruflo-jujutsu`) closes the loop: before a merge, it checks the touched paths against memory and surfaces a matching prior incident automatically (e.g. touching `.github/workflows/**` surfaces the Pages first-deploy lesson).

---

## Requirements Intake

New projects no longer start from a one-liner. The `requirements-intake` skill (adapted from [metaswarm](https://github.com/dsifry/metaswarm)'s brainstorming flow) runs a **3-round interview** — problem → shape & constraints → confirmation playback — and produces a proper `brief.md` with FR-#/NFR-#/AC-# IDs before Phase 1 begins. Unknowns become `[NEEDS CLARIFICATION]` markers, never guesses. Invoke with `/new-project`.

---

## Plan Review Gate & Independent Validation

Two orchestrator rules adapted from metaswarm:

- **Plan Review Gate (Phase 2.5):** before any implementation agent is spawned, QA and the Auditor adversarially review the architecture in parallel — their job is to find what's wrong, not to approve. Max 3 revise-and-re-review iterations; unresolved disagreement goes to the user, not forced through.
- **Independent Validation:** the orchestrator never trusts a subagent's own "done" claim. Every completion needs Verification Evidence (actual command output), and the orchestrator spot-checks by re-running at least one claimed command before accepting the phase.

Plus three delegation rules adapted from the AWS agent team and ciscoittech framework:

- **Machine-checkable task format:** every delegated task reads `"[role] <verb> <what> | <file paths> | <acceptance>. Run: <command>"` — a task with no file scope, acceptance criterion, or Run: command isn't delegable. The Run: command is exactly what Independent Validation re-executes.
- **Single-verdict synthesis:** parallel reviewers merge into one report with one verdict (PASS / PASS-WITH-CONDITIONS / FAIL) — never three overlapping reports for the user to reconcile.
- **Model/cost routing:** status queries and summaries never spawn heavyweight agents; full-strength agents are reserved for design, implementation, and review.

---

## Company Memory (`.claude/memory/`)

`lessons-learned.md` is an append-only log: after every engagement, a Phase 7 retrospective records what happened, the lesson, and the rule going forward. The orchestrator reads it at the start of every new engagement — the company never pays for the same mistake twice. Seeded with three real lessons already learned in production (the `.gitignore`/`.env` miss, the GitHub Pages first-deploy settings trap, and shared-branch push discipline).

---

## MCP Servers (`.mcp.json`)

| Server | Purpose |
|---|---|
| `playwright` | Real browser verification for Gate 2 (Functional/Browser-First) — Codex Rescuer loads the actual page rather than inferring "should render" |
| `context7` | Live, current library/framework documentation during implementation |

GitHub access is provided natively by this environment's built-in GitHub MCP integration.

---

## Governance Model

Every full engagement is subject to hard gates before anything goes live:

1. **CEO approval required** at both Phase 1 (business case) and Phase 6 (final release decision)
2. **Auditor sign-off required** — Phase 5 flags every security, compliance, and data residency gap; nothing proceeds with an open High-severity finding
3. **QA acceptance criteria must pass** before any go-live — functional, accessibility (WCAG AA), performance (Lighthouse ≥ 90), and cross-browser/device testing
4. **A named human approver** is required for every go-live — the orchestrator never deploys without one

This was proven end-to-end on the `doh-flight-checker` project: 102 automated QA tests were run and passed before deployment, and the release was gated on written approval from a named approver before going live.

---

## Project Output Convention

Every engagement — regardless of which pattern triggered it — produces a folder under `projects/<name>/`. This is the company's institutional memory: Pattern F ("what did we decide?") works by reading back from these files rather than re-deriving the answer.

---

## Example Engagements

| Project | Pattern | Outcome |
|---|---|---|
| [`doh-flight-checker`](./projects/doh-flight-checker/) | A — Full Engagement | Public flight-checker web app for Qatar airports. 6 phases run, 102/102 QA tests passed, approved and deployed to [Flight-Checker](https://github.com/vision1068/Flight-Checker) |

---

## Getting Started

Just type your request. Don't address a specific agent — the Orchestrator decides who's needed:

```
"Build a customer onboarding portal for loan applicants"
→ Pattern A: full 6-phase engagement, output in projects/customer-onboarding/

"What does the architect think about using Cloudflare Workers here?"
→ Pattern B: architect responds directly

"The QA phase missed mobile Safari testing, add it"
→ Pattern C: QA re-run with the existing test plan + your addition

"Audit this integration for compliance"
→ Pattern E: auditor only

"The deploy keeps failing, can you fix it"
→ Pattern G: codex-rescuer reproduces the failure, root-causes, fixes, re-verifies
```

---

## Prior Art

Structural ideas in this company are adapted from the best open-source AI-company frameworks, scaled to our size:

- [Tamoura/Claude-Code-creates-the-SW-company (ConnectSW)](https://github.com/Tamoura/Claude-Code-creates-the-SW-company) — the anti-rationalization protocol, 6-gate quality system, 11-dimension `/audit` structure, and constitution format. We skipped its component/port registries (built for 14 simultaneous products) and proprietary indexing tooling.
- [dsifry/metaswarm](https://github.com/dsifry/metaswarm) — the requirements-intake flow, the adversarial Plan Review Gate with a 3-iteration cap, the "never trust subagent self-reports" independent-validation rule, and the post-engagement retrospective feeding company memory.
- [diet103/claude-code-infrastructure-showcase](https://github.com/diet103/claude-code-infrastructure-showcase) — the hooks architecture: SessionStart context priming and `skill-rules.json`-driven skill auto-activation.
- [rohitg00/pro-workflow](https://github.com/rohitg00/pro-workflow) — the compounding, append-only lessons-learned memory pattern and the `[LEARN]`-block auto-capture Stop hook.
- [DietrichGebert/ponytail](https://github.com/DietrichGebert/ponytail) — the `minimal-code` 7-rung decision ladder (necessity → reuse → standard library → native platform → installed deps → one-liner → implement), wired into every implementation agent plus a review trigger in `codex-rescuer` and the `/audit` Code Quality dimension. Benchmarked ~54% average token/cost reduction upstream, without cutting validation, security, or accessibility.
- [dralgorhythm/claude-agentic-framework](https://github.com/dralgorhythm/claude-agentic-framework) — the safety hooks (destructive-command blocking, protected secret files) and the "keep the skill catalog lean" design principle.
- [aws-samples/sample-claude-code-agent-team](https://github.com/aws-samples/sample-claude-code-agent-team) — the machine-checkable task format (`[role] verb what | files | acceptance. Run: command`) and single-verdict review synthesis.
- [ciscoittech/claude-agent-framework](https://github.com/ciscoittech/claude-agent-framework) — effort-based model/cost routing.
- [ChrisWiles/claude-code-showcase](https://github.com/ChrisWiles/claude-code-showcase) — hook lifecycle reference (PreToolUse/PostToolUse/Stop patterns) informing our hook design.
- [ruvnet/ruflo](https://github.com/ruvnet/ruflo) — the RETRIEVE→JUDGE→DISTILL→CONSOLIDATE self-learning pipeline shape (`ruflo-intelligence`), rebuilt as plain grep + a Status field instead of their vector-DB/neural-training stack, and the diff-risk-scoring concept (`ruflo-jujutsu`) added to `codex-rescuer`. We explicitly skipped the other ~90% of Ruflo — swarm/consensus/federation/AgentDB/SONA — as infrastructure built for autonomous multi-machine fleets, a scale mismatch for a 16-agent single-repo company (the same reasoning that excluded ConnectSW's 14-product registries).
