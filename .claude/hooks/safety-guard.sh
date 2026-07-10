#!/bin/bash
# PreToolUse(Bash) hook — safety guard for destructive commands.
# Exit 2 blocks the command; exit 0 with output = warn but allow.
# Fail-soft: any parsing problem allows the command (never breaks work).
# Pattern from dralgorhythm/claude-agentic-framework safety hooks.

INPUT=$(cat)
# Extract the command, then strip heredoc bodies and quoted strings so
# prose (e.g. a commit message describing a dangerous command) never
# false-positives — only actual executable text is matched.
CMD=$(printf '%s' "$INPUT" | python3 -c "
import json, re, sys
try:
    cmd = json.load(sys.stdin).get('tool_input', {}).get('command', '')
except Exception:
    cmd = ''
# strip heredoc bodies: <<'TAG' or <<TAG ... TAG at line start
cmd = re.sub(r\"<<-?\s*'?\\\"?(\w+)'?\\\"?.*?^\1\s*$\", '<<HEREDOC', cmd, flags=re.S | re.M)
# strip double- and single-quoted string contents
cmd = re.sub(r'\\\"(?:[^\\\"\\\\]|\\\\.)*\\\"', '\"\"', cmd)
cmd = re.sub(r\"'(?:[^'\\\\]|\\\\.)*'\", \"''\", cmd)
print(cmd)
" 2>/dev/null)

[ -z "$CMD" ] && exit 0

# ---- HARD BLOCKS (exit 2) ----
if echo "$CMD" | grep -qE 'rm -rf +(/|~|\$HOME)( |$)'; then
  echo "BLOCKED by safety-guard: recursive delete of root/home. Name the exact directory instead." >&2
  exit 2
fi
if echo "$CMD" | grep -qE 'git push[^|;&]*--force( |$)[^|;&]*\b(main|master)\b|git push[^|;&]*\b(origin +)?(main|master)\b[^|;&]*--force( |$)'; then
  echo "BLOCKED by safety-guard: force-push to main/master. Use --force-with-lease on a feature branch, or get explicit user approval." >&2
  exit 2
fi
if echo "$CMD" | grep -qE '\bterraform +destroy\b|\bdrop +database\b'; then
  echo "BLOCKED by safety-guard: destructive infrastructure command. Requires explicit user approval first." >&2
  exit 2
fi

# ---- WARNINGS (allow, but remind) ----
if echo "$CMD" | grep -qE 'git add +(-A|--all|\.)( |$)'; then
  echo "[safety-guard] Warning: bulk 'git add' stages everything, including files you may not have reviewed. Prefer staging specific files; verify with 'git diff --cached --stat' before committing."
fi
if echo "$CMD" | grep -qE 'rm -rf '; then
  echo "[safety-guard] Warning: recursive delete. Double-check the target path is exactly what you intend."
fi
if echo "$CMD" | grep -qE 'git reset --hard'; then
  echo "[safety-guard] Warning: hard reset discards uncommitted work irreversibly. Confirm nothing unstaged is needed."
fi

exit 0
