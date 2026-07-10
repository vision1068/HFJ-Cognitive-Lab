#!/bin/bash
# SessionStart hook — briefs every new session on company state.
# Pattern from diet103/claude-code-infrastructure-showcase (context priming).
cd "$(dirname "$0")/../.." || exit 0

echo "=== HFJ-Cognitive-Lab — Session Briefing ==="
echo ""
echo "Active projects:"
for d in projects/*/; do
  [ -d "$d" ] || continue
  name=$(basename "$d")
  phases=$(ls "$d" 2>/dev/null | grep -c "^phase-")
  echo "  - $name (phase files: $phases)"
done

if [ -f .claude/memory/lessons-learned.md ]; then
  lessons=$(grep -c "^### " .claude/memory/lessons-learned.md 2>/dev/null || echo 0)
  echo ""
  echo "Company memory: $lessons lesson(s) in .claude/memory/lessons-learned.md — read before starting engagement work."
fi

echo ""
echo "Constitution: 6 standards in CLAUDE.md are non-negotiable (spec-first, test-before-claim, traceability, secure-by-construction, blocking gates, diagram-first)."
exit 0
