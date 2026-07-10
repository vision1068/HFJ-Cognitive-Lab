#!/bin/bash
# UserPromptSubmit hook — skill auto-activation.
# Reads the user's prompt, matches it against .claude/skills/skill-rules.json,
# and injects a suggestion so the right skill is never forgotten.
# Pattern from diet103/claude-code-infrastructure-showcase.

RULES="$(dirname "$0")/../skills/skill-rules.json"
[ -f "$RULES" ] || exit 0

INPUT=$(cat)
export HOOK_INPUT="$INPUT"
export RULES_PATH="$RULES"

python3 <<'PYEOF'
import json, os, re, sys

try:
    payload = json.loads(os.environ.get("HOOK_INPUT", "{}"))
    prompt = (payload.get("prompt") or "").lower()
    with open(os.environ["RULES_PATH"]) as f:
        rules = json.load(f)
except Exception:
    sys.exit(0)

if not prompt:
    sys.exit(0)

matches = []
for skill, cfg in rules.get("skills", {}).items():
    for kw in cfg.get("keywords", []):
        if re.search(r"\b" + re.escape(kw.lower()) + r"\b", prompt):
            matches.append((skill, cfg.get("hint", "")))
            break

if matches:
    lines = ["[skill-activation] Relevant company skills for this request:"]
    for skill, hint in matches[:3]:
        lines.append(f"  - {skill}: {hint}")
    lines.append("Apply them unless clearly inapplicable (the 1% Rule).")
    print("\n".join(lines))
PYEOF
exit 0
