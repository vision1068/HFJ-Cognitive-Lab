---
description: Focused QCB/governance compliance check on a project or decision
---

Invoke the `auditor` agent in its standard Phase 5 governance mode
(not the full 11-dimension `/audit`) on the project or document
named in `$ARGUMENTS`.

Focus specifically on:
- QCB supervisory requirement alignment
- Data residency / Qatar sovereignty requirements
- Audit trail completeness (can every decision be explained from the log alone?)
- Service account privilege scope (least-privilege verified, not assumed)
- Versioning/immutability — is it actually legally defensible, or just "probably fine"

Output the standard auditor format: flagged gaps ranked by severity,
each with a specific mitigation. Over-flagging is correct behavior here
— do not suppress a borderline finding to keep the report shorter.
