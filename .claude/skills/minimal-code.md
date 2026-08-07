---
name: minimal-code
description: >
  The 7-rung decision ladder applied BEFORE writing any implementation
  code. Write only the code that has to exist — reuse, standard library,
  and native platform features beat custom code every time. Cuts tokens,
  cost, review surface, and future maintenance. Adapted from
  github.com/DietrichGebert/ponytail ("the best code is the code you
  never wrote"; benchmarked ~54% avg token/cost reduction).
---

# Minimal Code Skill — the Decision Ladder

## Philosophy

Lazy about the SOLUTION, never about READING. Understand the problem
completely first — then write the least code that solves it. This is
deliberate constraint, not negligent abbreviation.

## The ladder — climb in order, stop at the first rung that solves it

1. **Necessity** — Does this need to exist at all? Is the requirement
   real (FR-# in the spec) or imagined ("might need it later")?
2. **Reuse** — Does it already exist in this codebase or a prior
   project? (Check before building — Anti-Rationalization P1.)
3. **Standard library** — Does the language already do this?
   (`Intl.NumberFormat` over a formatting util; `URLSearchParams` over
   a query-string parser; `crypto.randomUUID()` over a uuid helper.)
4. **Native platform** — Does the browser/OS/framework do this?
   (`<input type="date">` over a date-picker component; `<dialog>` over
   a modal library; CSS `position: sticky` over scroll listeners;
   Dataverse built-in auditing over custom audit tables; Power Automate
   built-in approvals over custom approval flows.)
5. **Installed dependencies** — Does a package we ALREADY have do this?
   (Never add a new dependency to avoid 10 lines of code.)
6. **One-liner** — Can it be a line or a small function instead of a
   class/module/abstraction layer?
7. **Implement** — Only now build it, and build the minimum that passes
   the acceptance criteria. No speculative parameters, no "flexible"
   abstractions for futures nobody specified.

## Declaration requirement

Before implementing anything non-trivial, state the rung you stopped
at in one line, e.g.:
`Ladder: rung 4 — native <dialog>, no modal code needed.`
`Ladder: rung 7 — no existing/native option; building minimal version.`

## Safety floor — never cut these to save lines

- Input validation and error handling
- Security patterns (secure-coding.md is not negotiable)
- Accessibility (ux-guidelines still apply)
- The tests (quality-verification.md still applies — minimal code
  still gets its happy-path, boundary, and failure-path tests)

## Review trigger (codex-rescuer / auditor)

Flag as a **rung violation** in any review: custom code where a
standard-library or native feature exists (rungs 3-4), a new dependency
added for something an installed one already does (rung 5), or an
abstraction with a single caller and no specified second use (rung 7
overshoot). Severity: same as a code-quality finding — it blocks a
clean PASS.
