---
name: quality-verification
description: >
  The 1% Rule, anti-rationalization table, and 5-step Verification-Before-
  Completion gate. Every agent that writes or fixes code (backend, frontend,
  middleware, crm-developer, power-platform, mobile, fo-developer,
  codex-rescuer) must apply this before marking any task complete.
  Adapted from the ConnectSW constitution's Article XI
  (github.com/Tamoura/Claude-Code-creates-the-SW-company).
---

# Quality Verification Protocol

## Part 1 — The 1% Rule

If there is even a 1% chance a check, test, or gate applies to the current
task, run it. Running an unnecessary check costs minutes. Skipping a
necessary one costs hours — usually discovered by the client, not by us.

## Part 2 — Anti-Rationalization Table

Agents rationalize skipping quality steps in predictable ways. When you
catch yourself thinking one of these, apply the counter instead of the
shortcut.

| # | Rationalization | Counter |
|---|---|---|
| 1 | "This is too simple to need a test" | Simple code has the highest test ROI — 30 seconds to write, catches every future regression for free |
| 2 | "I'll add tests after it works" | Test-first is required. Tests written after tend to describe what the code does, not what it should do |
| 3 | "There's probably already a test for this" | Cite the exact test name and file path, or write one — "probably" is not evidence |
| 4 | "It's just a refactor, behavior didn't change" | Run the full suite before AND after — that's the only proof behavior didn't change |
| 5 | "It's just config/setup" | Config changes break things silently. Run the full suite |
| 6 | "It's just UI/styling" | Every component needs a render check; every user-facing page needs an E2E pass |
| 7 | "I'm blocked on a dependency" | Write the test now with a `// TODO: unblock when X ready` — don't skip it entirely |
| 8 | "Test infrastructure isn't set up yet" | Test infrastructure is the first task, not something blocking the "real" work |
| 9 | "It's a one-time migration script" | One-time scripts break production once. Test forward AND verify (and rollback if destructive) |
| 10 | "It's just a prototype" | Prototypes become production more often than anyone admits — test the core behavior at minimum |
| 11 | "We're under time pressure" | Skipping now costs roughly 10x later in debugging. Time pressure is the argument FOR testing, not against |
| 12 | "It's just glue/integration code" | Integration code is exactly where untested assumptions break — it always needs an integration test |
| P1 | "I don't need to check what already exists" | A 30-second look at existing skills/agents/code prevents hours of rebuilding something that's already there |
| P2 | "I'll add the diagram/spec later" | Diagrams and specs are first-class deliverables, not post-work — write them before or during, not after |
| P3 | "The requirement is close enough, I'll just build it" | Ambiguous requirements get a `[NEEDS CLARIFICATION]` marker and go back to the requester — never guessed at |
| P4 | "I'll add traceability IDs at the end" | IDs (FR-#, AC-#) are added at creation time in the spec — retrofitting them is how traceability silently breaks |

## Part 3 — Pre-Implementation Checklist

Before writing implementation code, confirm:

1. Is there a test for this, or will there be one before the code is written?
2. Am I rationalizing skipping a test right now? (Check the table above.)
3. Have I checked whether this already exists (skill, prior project, existing component)?
4. Have I verified this isn't already implemented elsewhere in the project?
5. Does this need a diagram to be understandable? (Apply the 1% Rule.)

## Part 4 — Verification-Before-Completion (5-Step Gate)

No agent marks a task complete without this. "Should work now" is never
an acceptable completion claim — only re-run evidence is.

1. **Identify** — what command, test, or manual check actually proves this works?
2. **Execute** — run it. Do not predict the output.
3. **Read** — look at the actual output, not the output you expected.
4. **Compare** — does it match every acceptance criterion, not just the main one?
5. **Claim** — report completion WITH the evidence attached (command run + actual output).

### Required evidence by task type

| Task type | Must show |
|---|---|
| Backend/API code | Tests pass (paste output) + one real request/response + error case handled |
| Frontend/UI code | Component renders (screenshot or dev-server check) + build succeeds + no console errors |
| Database/schema change | Migration runs clean + rollback tested + constraints verified |
| Integration/plugin (CRM, Power Automate) | Triggered end-to-end at least once + failure path tested |
| Deploy/CI pipeline | Pipeline run completed (paste run status) — not "should deploy fine" |
| Bug fix | A failing test written BEFORE the fix, now passing after |

### Escalation

If verification genuinely cannot be run (no environment access, blocked
dependency), say so explicitly and name exactly what's missing — do not
substitute confident language for missing proof.

## Part 5 — Systematic Debugging (no brute force)

When something is broken, follow this order, not random changes:

1. **Investigate** — reproduce the failure and read the actual error/output
2. **Pattern analysis** — has this failure shape appeared before in this project?
3. **Hypothesis** — form one specific theory of the cause
4. **Test the hypothesis** — the smallest possible change that would confirm or kill it
5. **Implement** — only once the hypothesis is confirmed

This is `codex-rescuer`'s primary operating protocol, and applies to any
engineer agent debugging their own code before handing it off.
