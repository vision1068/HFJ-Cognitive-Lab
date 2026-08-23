#!/bin/bash
# UserPromptSubmit hook — memory retrieval-by-relevance.
# Greps lessons-learned.md for entries whose title/lesson text matches
# the user's prompt keywords, and injects only those entries instead of
# forcing every agent to re-read the whole (and growing) file.
# This is the RETRIEVE stage of the Memory Learning Loop
# (RETRIEVE -> JUDGE -> DISTILL -> CONSOLIDATE), adapted from
# ruvnet/ruflo's intelligence pipeline without any vector DB — plain
# keyword matching is sufficient at this company's memory size.

MEMORY="$(dirname "$0")/../memory/lessons-learned.md"
[ -f "$MEMORY" ] || exit 0

INPUT=$(cat)
export HOOK_INPUT="$INPUT"
export MEMORY_PATH="$MEMORY"

python3 <<'PYEOF'
import json, os, re, sys

try:
    payload = json.loads(os.environ.get("HOOK_INPUT", "{}"))
    prompt = (payload.get("prompt") or "").lower()
    with open(os.environ["MEMORY_PATH"]) as f:
        text = f.read()
except Exception:
    sys.exit(0)

if not prompt:
    sys.exit(0)

# Split into entries on "### " headers (skip the template block above the "---").
body = text.split("\n---\n", 1)
entries_text = body[1] if len(body) > 1 else text
entries = re.split(r"\n(?=### )", entries_text)

# Keywords = significant, non-generic words from the prompt (4+ chars).
stop = {"the","and","for","with","that","this","from","into","have","will",
        "your","what","when","does","about","should","please","need","want",
        "there","hello","hell","here","help","them","then","than","were",
        "just","like","some","been","being","doing","make","made"}
words = [w for w in re.findall(r"[a-z][a-z0-9\-]{3,}", prompt) if w not in stop]
if not words:
    sys.exit(0)

hits = []
for entry in entries:
    if not entry.strip().startswith("### "):
        continue
    low = entry.lower()
    score = sum(1 for w in set(words) if re.search(r"\b" + re.escape(w) + r"\b", low))
    # require at least 2 distinct keyword hits so a single common word
    # (e.g. a project name that also appears everywhere) can't trigger noise
    if score >= 2:
        title = entry.splitlines()[0].lstrip("# ").strip()
        status_match = re.search(r"\*\*Status:\*\*\s*(\S+)", entry)
        status = status_match.group(1) if status_match else "Unconfirmed"
        hits.append((score, title, status))

if not hits:
    sys.exit(0)

hits.sort(key=lambda h: -h[0])
lines = ["[memory-retrieve] Relevant company memory for this request:"]
for score, title, status in hits[:4]:
    lines.append(f"  - [{status}] {title}")
lines.append("Read the full entry in .claude/memory/lessons-learned.md before proceeding if it applies.")
print("\n".join(lines))
PYEOF
exit 0
