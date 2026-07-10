#!/bin/bash
# PreToolUse(Write|Edit) hook — protects secret-bearing files.
# Blocks edits to .env and key files; .env.example is always allowed.
# Pattern from dralgorhythm/claude-agentic-framework protected-files hook.

INPUT=$(cat)
FILE=$(printf '%s' "$INPUT" | python3 -c "import json,sys
try:
    print(json.load(sys.stdin).get('tool_input',{}).get('file_path',''))
except Exception:
    pass" 2>/dev/null)

[ -z "$FILE" ] && exit 0

BASE=$(basename "$FILE")

case "$BASE" in
  .env.example|.env.sample|.env.template)
    exit 0 ;;
  .env|.env.local|.env.production|.env.development)
    echo "BLOCKED by file-guard: $BASE holds live secrets and is never edited by agents. Edit .env.example instead and let the user set real values." >&2
    exit 2 ;;
  id_rsa|id_ed25519|*.pem|*.key|*.pfx)
    echo "BLOCKED by file-guard: $BASE looks like a private key/certificate. Agents never write credential files." >&2
    exit 2 ;;
esac

exit 0
