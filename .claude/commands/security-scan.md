---
description: Run the security self-review checklist and dependency audit against a project
---

Invoke `codex-rescuer` to run a focused security pass on the project
named in `$ARGUMENTS` (ask which project if not given), using
`.claude/protocols/secure-coding.md` as the checklist.

Steps:
1. Run the actual dependency audit for that stack (`npm audit` for
   Node/React projects; note the equivalent check for Power
   Platform/CRM projects if applicable) and paste the real output.
2. Grep the codebase for the Forbidden Patterns table in
   secure-coding.md — report every match with file:line.
3. Walk the Security Self-Review Checklist from secure-coding.md
   item by item — mark each Pass/Fail/N-A with evidence, not assumption.
4. Check `.gitignore` actually excludes `.env`/`.env.local`/credentials.

Output: Pass/Fail per checklist item, plus a ranked list of findings
(HIGH/MEDIUM/LOW) each with the concrete failure scenario it enables.
Do not mark anything Pass without having actually run the check.
