---
name: ui-ux-design
description: >
  UI/UX design intelligence skill for Power Apps, model-driven apps,
  and web interfaces. Generates design specifications, user flows,
  wireframe descriptions, and accessibility guidance.
  Based on nextlevelbuilder/ui-ux-pro-max-skill and
  HermeticOrmus/LibreUIUX-Claude-Code.
---

# UI/UX Design Skill

## When to invoke
- Designing screen layouts for canvas or model-driven apps
- Creating user flow diagrams
- Reviewing UI for usability or accessibility issues
- Generating component specifications for PCF development
- Design system definition for a project

## Design principles enforced
1. **Role-appropriate** — show only what the user's role needs
2. **Progressive disclosure** — simple first, detail on demand
3. **Error prevention over error messages** — validate early
4. **Consistency** — same patterns for same actions across all screens
5. **Accessibility** — WCAG 2.1 AA minimum for QDB public-facing apps

## User flow template
```
[Entry point] → [Screen 1: List/Search]
  → [Screen 2: Detail View]
    → [Action: Edit] → [Screen 3: Form]
      → [Confirm] → [Success/Error feedback]
      → [Cancel] → [Screen 2]
```

## Screen specification format
**Screen: [Name]**
- Purpose: one sentence
- User role(s): [roles that see this]
- Key components: [Gallery / Form / Chart / Button bar]
- Primary action: [what the user does here]
- Data displayed: [table/view name]
- Navigation: [where user comes from / goes to]
- Validation: [rules enforced on this screen]
- Empty state: [what shows when no data]

## Color & typography (QDB brand defaults)
- Primary: #1B3A6B (QDB Navy)
- Accent: #C8962E (QDB Gold)
- Success: #2E7D32 | Warning: #F57F17 | Error: #C62828
- Font: Segoe UI (Power Apps default) — do not override unless approved

## Accessibility checklist
- [ ] All interactive elements keyboard-accessible
- [ ] Color contrast ratio ≥ 4.5:1 for text
- [ ] Labels on all form fields (not placeholder-only)
- [ ] Error messages describe what to fix, not just what went wrong
- [ ] Tab order logical and predictable

## Output format
Screen list → user flow (text diagram) → per-screen spec → 
component list → accessibility notes. No image generation.
