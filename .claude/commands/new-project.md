---
description: Start a new project — structured requirements intake, then the full 6-phase engagement
---

Kick off a new project for the request in `$ARGUMENTS` (ask for a
one-line description if empty).

1. Run the `requirements-intake` skill: the 3-round interview producing
   `projects/<name>/brief.md` with FR-#/NFR-#/AC-# IDs and any
   `[NEEDS CLARIFICATION]` markers.
2. Once the user confirms the Round-3 playback, hand the brief to the
   orchestrator as Pattern A (Full Engagement) input — Phase 1 (CEO)
   starts from the brief, not from the original one-liner.
3. The Plan Review Gate (orchestrator rule) runs before Phase 3 as usual.

Do not skip the interview even if the request seems clear — one
sentence is never a spec (Constitution article 1).
