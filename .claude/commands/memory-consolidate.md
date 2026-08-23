---
description: Consolidate company memory — mark contradicted/superseded lessons, keep the log honest as it grows
---

Read the complete `.claude/memory/lessons-learned.md`. This is the
CONSOLIDATE stage of the Memory Learning Loop (RETRIEVE → JUDGE →
DISTILL → CONSOLIDATE, adapted from ruvnet/ruflo's intelligence
pipeline as plain-text scanning — no vector DB needed at this scale).

Do the following, in order:

1. **Find contradictions.** Two entries that give opposite or
   incompatible guidance on the same situation. Mark the OLDER one
   `Status: Superseded by <newer entry's date/title>` — do not delete
   or rewrite it; the history of what was believed and why it changed
   is itself valuable (see the gold-signal-analyzer
   fabricated-authorization entries for the pattern: a correction is a
   new entry, not an edit).

2. **Find redundant near-duplicates.** Two or more entries that teach
   essentially the same rule from different incidents. Keep the most
   complete/recent one live; mark the earlier ones `Status: Superseded
   by <the kept entry>`.

3. **Find entries that are still `Unconfirmed` after a long time** (a
   rough heuristic: several engagements have happened since, on
   related work, and never invoked the rule). Leave these as
   `Unconfirmed` — do not guess a status — but flag them in your
   report as "never yet tested" so the user knows which parts of
   company memory are unverified assumptions rather than proven rules.

4. **Never touch `Confirmed` or `Contradicted` entries** except to add
   a superseding note if step 1 or 2 applies — their evidence trail
   stays intact.

Report back: how many entries scanned, how many marked Superseded (with
old → new mapping), how many flagged as long-Unconfirmed, and the
resulting file's total line count before/after (should be similar —
this marks entries, it does not shrink the file).
