---
description: Run Gate 6 (Production Readiness) checklist before any go-live
---

Before deploying the project named in `$ARGUMENTS` (ask if not given),
walk `.claude/quality-gates/checklist.md` Gate 6 — Production Readiness —
item by item:

- [ ] Rollback plan exists and is documented (not just assumed possible)
- [ ] Monitoring/error visibility exists — even minimal, a log a human will check
- [ ] Named human approver has explicitly signed off in writing
- [ ] Every HIGH-severity item from the Auditor's last review is closed, not just noted

Also confirm Gates 1–5 in the same checklist have all passed for this
project — do not run Gate 6 in isolation if earlier gates were skipped.

Report each gate as Pass/Fail with evidence. If any gate fails, state
plainly that deployment should not proceed and name exactly what's
missing — do not soften a Fail into "mostly ready."
