---
name: token-efficient
description: >
  Token efficiency skill. Use when prompts are getting long, context
  is bloated, or you want to compress agent outputs without losing
  information. Based on JuliusBrussee/caveman.
---

# Token Efficiency Skill

## When to invoke
- Agent outputs are verbose and hard to scan
- Context window is filling up across a long engagement
- You want a compressed summary of a phase output
- Prompt is being passed to multiple agents and needs trimming

## Compression rules

### For agent outputs
1. Remove all preamble ("As the Backend Developer, I will now...")
2. Remove all sign-off text ("Please let me know if you need...")
3. Replace prose paragraphs with bullet points or tables
4. Merge redundant points — say it once
5. Replace example code with pseudocode unless exact syntax is required
6. Cut any content not directly actionable by the next agent

### For prompts passed between agents
- Pass IDs and references, not full re-stated context
- Use "See phase-2-arch.md for full context" rather than pasting it
- State only what the receiving agent needs to act, not all background

### Compression ratio targets
| Content type        | Target reduction |
|---------------------|-----------------|
| Agent prose output  | 40-60%          |
| Phase summary       | 60-70%          |
| Error messages      | 70-80%          |
| Code comments       | Remove unless non-obvious WHY |

## Caveman summary format
When compressing, use this structure:
```
WHAT: [one line]
WHY: [one line]  
HOW: [bullet list, max 5 items]
RISK: [one line or "none"]
NEXT: [one line — what happens next]
```

## Output format
Return compressed version only. Do not explain what was removed.
