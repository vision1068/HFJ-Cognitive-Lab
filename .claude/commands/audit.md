---
description: Run a full 11-dimension audit on a project against 9 compliance frameworks
---

Invoke the `auditor` agent in **Full Audit Mode** (not the lighter Phase 5
governance pass) on the project named in `$ARGUMENTS`.

If no project name is given, ask which project in `projects/` to audit.

Follow the Full Audit Mode section of `.claude/agents/auditor.md`:
score all 11 dimensions with evidence, check applicable frameworks from
the 9 listed, and produce the two-part Executive Memo + Engineering
Appendix output to `projects/<name>/AUDIT-REPORT.md`.
