---
description: Quick status summary across all active HFJ-Cognitive-Lab projects
---

Read every folder under `projects/` and summarize, without calling any
specialist agent (this is Pattern F — Memory/status query):

For each project:
- Name and one-line purpose (inferred from `brief.md` or `full-engagement.md`)
- Which phases have outputs on disk (phase-1 through phase-6)
- Last CEO decision if `phase-6-ceo.md` or equivalent exists (Approved/Rejected/Revised)
- Any open items mentioned in the most recent phase file (unresolved
  `[NEEDS CLARIFICATION]` markers, flagged HIGH-severity audit items, failing gates)

Present as a compact table: Project | Status | Last Decision | Open Items.
If `$ARGUMENTS` names a specific project, report on that one only, in full detail.
