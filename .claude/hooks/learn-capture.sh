#!/bin/bash
# Stop hook — auto-captures learning into company memory.
# Any assistant output containing a line starting with "[LEARN]" gets
# appended (deduplicated) to .claude/memory/lessons-learned.md.
# Fail-soft: any error exits 0 so the session is never blocked.
# Pattern from rohitg00/pro-workflow's learn-capture.js.

INPUT=$(cat)
cd "$(dirname "$0")/../.." || exit 0
MEMORY=".claude/memory/lessons-learned.md"
[ -f "$MEMORY" ] || exit 0

export HOOK_INPUT="$INPUT"
export MEMORY_PATH="$MEMORY"

python3 <<'PYEOF' 2>/dev/null
import json, os, re, sys
from datetime import date

try:
    payload = json.loads(os.environ.get("HOOK_INPUT", "{}"))
    transcript = payload.get("transcript_path", "")
    memory = os.environ["MEMORY_PATH"]
    if not transcript or not os.path.exists(transcript):
        sys.exit(0)

    learns = []
    with open(transcript, errors="ignore") as f:
        for line in f:
            try:
                entry = json.loads(line)
            except Exception:
                continue
            msg = entry.get("message") or {}
            if msg.get("role") != "assistant":
                continue
            content = msg.get("content")
            texts = []
            if isinstance(content, str):
                texts.append(content)
            elif isinstance(content, list):
                texts += [b.get("text", "") for b in content if isinstance(b, dict)]
            for t in texts:
                for m in re.findall(r"^\[LEARN\]\s*(.+)$", t, re.MULTILINE):
                    learns.append(m.strip())

    if not learns:
        sys.exit(0)

    with open(memory) as f:
        existing = f.read()

    new = [l for l in dict.fromkeys(learns) if l and l not in existing]
    if not new:
        sys.exit(0)

    with open(memory, "a") as f:
        for l in new:
            f.write(f"\n### {date.today().isoformat()} — auto-captured — {l[:60]}\n")
            f.write(f"- **Lesson:** {l}\n- **Source:** [LEARN] block auto-captured by learn-capture hook\n")
except Exception:
    pass
PYEOF
exit 0
