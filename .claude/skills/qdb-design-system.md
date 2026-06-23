---
name: qdb-design-system
description: >
  QDB Brand Design System Generator. Use when starting any new Power App,
  model-driven form, Power BI report, PCF component, or portal page.
  Generates a complete, consistent design system: colors, typography,
  spacing, component tokens, and anti-patterns — all aligned to QDB brand.
  Invoke with: /qdb-design-system [app or feature name]
---

# QDB Brand Design System Generator
*Adapted from nextlevelbuilder/ui-ux-pro-max-skill — Finance/Gov industry rules*

## When to invoke
- Starting any new Power App (canvas or model-driven)
- Designing a new Power BI embedded report or dashboard
- Building a PCF component that needs consistent styling
- Reviewing an existing UI for brand inconsistency

---

## Step 1 — Classify the Application Type

Before generating the design system, identify which type applies:

| Type | Examples |
|---|---|
| **Approval Flow App** | Loan approval, credit review, compliance sign-off |
| **Data Entry Form** | Customer onboarding, facility registration, collateral input |
| **Executive Dashboard** | Portfolio KPIs, sector performance, risk summary |
| **Operational Dashboard** | Daily processing status, queue depth, alerts |
| **Lookup / Reference App** | Entity search, document retrieval, rate lookup |
| **Mobile Field App** | Site inspection, relationship manager visits |

---

## Step 2 — QDB Brand Tokens (use these in every app, no exceptions)

### Color Palette
```css
/* Primary Brand */
--qdb-navy:        #1B3A6B;   /* Primary actions, headers, active states */
--qdb-gold:        #C8962E;   /* Accent, highlights, key metrics */
--qdb-white:       #FFFFFF;   /* Backgrounds, cards */
--qdb-light-gray:  #F4F6F9;   /* Page backgrounds */
--qdb-mid-gray:    #DDE2EA;   /* Borders, dividers */
--qdb-text:        #1A202C;   /* Body text */
--qdb-text-muted:  #6B7280;   /* Secondary text, labels */

/* Semantic — Status & Feedback */
--qdb-success:     #166534;   /* Approved, completed, on-track */
--qdb-success-bg:  #DCFCE7;
--qdb-warning:     #92400E;   /* Pending review, at-risk */
--qdb-warning-bg:  #FEF3C7;
--qdb-error:       #991B1B;   /* Rejected, overdue, breach */
--qdb-error-bg:    #FEE2E2;
--qdb-info:        #1E40AF;   /* Informational, scheduled */
--qdb-info-bg:     #DBEAFE;

/* Data Visualisation (Power BI) */
--qdb-chart-1:     #1B3A6B;   /* Series 1 — Navy */
--qdb-chart-2:     #C8962E;   /* Series 2 — Gold */
--qdb-chart-3:     #2E7D9A;   /* Series 3 — Teal */
--qdb-chart-4:     #6B4E9B;   /* Series 4 — Purple */
--qdb-chart-5:     #2E7D32;   /* Series 5 — Green */
--qdb-chart-6:     #C0392B;   /* Series 6 — Red */
```

### Typography
```
Display:  Segoe UI Semibold  — 28px / 32px  — Page titles, KPI values
Heading:  Segoe UI Semibold  — 20px / 24px  — Section headers, card titles
Body:     Segoe UI Regular   — 14px / 20px  — Form fields, table rows
Caption:  Segoe UI Regular   — 12px / 16px  — Labels, timestamps, help text
Mono:     Consolas           — 13px / 18px  — Reference numbers, IDs, codes
```

### Spacing Scale
```
4px   — Icon internal padding
8px   — Control internal padding (compact)
12px  — Control internal padding (standard)
16px  — Card padding, section gap
24px  — Section spacing
32px  — Page section separator
48px  — Major page section break
```

### Border & Shadow
```
Border radius:  4px (controls)  8px (cards)  2px (table rows)
Border color:   var(--qdb-mid-gray)
Card shadow:    0 1px 3px rgba(0,0,0,0.10), 0 1px 2px rgba(0,0,0,0.06)
Focus ring:     3px solid var(--qdb-gold), offset 2px
```

---

## Step 3 — Component Token Map (Power Apps)

### Buttons
| State | Fill | Text | Border |
|---|---|---|---|
| Primary | #1B3A6B | White | None |
| Primary Hover | #16305A | White | None |
| Secondary | White | #1B3A6B | #1B3A6B 1.5px |
| Destructive | #991B1B | White | None |
| Disabled | #DDE2EA | #6B7280 | None |

### Form Controls
| Element | Font | Size | Border |
|---|---|---|---|
| Input (empty) | Segoe UI | 14px | #DDE2EA 1.5px |
| Input (focus) | Segoe UI | 14px | #1B3A6B 2px |
| Input (error) | Segoe UI | 14px | #991B1B 2px |
| Dropdown | Segoe UI | 14px | #DDE2EA 1.5px |
| Required label | Segoe UI | 12px | — add asterisk (*) in #991B1B |

### Status Badges (for record status fields)
| Status | Background | Text |
|---|---|---|
| Approved | #DCFCE7 | #166534 |
| Pending | #FEF3C7 | #92400E |
| Rejected | #FEE2E2 | #991B1B |
| Draft | #F4F6F9 | #1A202C |
| In Review | #DBEAFE | #1E40AF |
| Cancelled | #F3F4F6 | #6B7280 |

---

## Step 4 — Layout Templates

### Approval Flow (model-driven form)
```
┌─────────────────────────────────────────┐
│ [QDB Header — Navy #1B3A6B]             │
├──────────────┬──────────────────────────┤
│ Summary Card │ Main Form (scrollable)   │
│ (left 30%)   │ Section 1: Applicant     │
│ • Key fields │ Section 2: Financial     │
│ • Status     │ Section 3: Documents     │
│ • Timeline   │ Section 4: Decision      │
├──────────────┴──────────────────────────┤
│ [Action Bar: Approve | Return | Reject] │
└─────────────────────────────────────────┘
```

### Executive Dashboard (Power BI)
```
┌─────────────────────────────────────────┐
│ [Title + Date Filter + Export]          │
├───────┬───────┬───────┬─────────────────┤
│ KPI 1 │ KPI 2 │ KPI 3 │ KPI 4           │
│ (Navy)│ (Gold)│ (Teal)│ (Purple)        │
├───────┴───────┼───────┴─────────────────┤
│ Bar/Line Chart│ Donut / Pie Chart       │
│ (primary)     │ (secondary)             │
├───────────────┼─────────────────────────┤
│ Table / Matrix│ Trend Sparklines        │
└───────────────┴─────────────────────────┘
```

---

## Step 5 — Anti-Patterns (NEVER do these in QDB apps)

| Anti-Pattern | Why Banned |
|---|---|
| Red text on red background | Fails WCAG AA — invisible to colour-blind users |
| Placeholder-only labels | Disappears on input — fails accessibility audit |
| Multiple font families | Breaks brand consistency |
| Bright saturated accent colours | Not aligned to QDB conservative finance brand |
| Inline styles (hardcoded hex) | Use environment variables / themes instead |
| Status conveyed by colour only | Must also use icon or text label |
| Buttons without disabled state | Allows double-submission on approval actions |
| Tables without hover states | Reduces scannability on dense data screens |
| Missing required field indicators | Causes form abandonment and re-work |
| Fixed widths on responsive containers | Breaks tablet / mobile layouts |

---

## Output Format

When this skill is invoked, produce:

1. **App Classification** — which template applies
2. **Color Token Block** — CSS/Power Apps variables to use
3. **Typography Spec** — font, size, weight per text type
4. **Component List** — every component needed with token assignments
5. **Layout Wireframe** — text-based layout diagram
6. **Anti-Pattern Checklist** — confirm none of the banned patterns are present
7. **Power Apps Theme JSON snippet** — ready to paste into the app theme editor
