---
name: ux-guidelines
description: >
  99 UX Guidelines adapted for Power Platform, model-driven apps, canvas apps,
  and Power BI. Use when reviewing or designing any QDB UI to check for
  violations before build or go-live. Covers navigation, forms, accessibility,
  feedback, responsiveness, touch, performance, and data display.
  Invoke with: /ux-guidelines [screen or feature to review]
---

# 99 UX Guidelines — QDB Power Platform Edition
*Source: nextlevelbuilder/ui-ux-pro-max-skill — adapted for Power Apps / CRM / Power BI*

## Severity Key
- 🔴 **High** — Blocks go-live. Must fix before release.
- 🟡 **Medium** — Fix in this sprint. Degrades usability significantly.
- 🟢 **Low** — Fix when time allows. Minor polish item.

---

## Category 1 — Navigation (6 rules)

| # | Rule | Power Apps Context | Severity |
|---|---|---|---|
| N1 | Active state visible on current screen/tab | Highlight active tab in nav bar with gold underline | 🟡 |
| N2 | Back navigation works predictably | Canvas app Back() function must go to previous screen, not Home | 🔴 |
| N3 | Deep links work — URL reflects state | Model-driven: record URL should open that exact form | 🟡 |
| N4 | Breadcrumbs on 3+ level hierarchies | Show Account > Facility > Loan path on nested forms | 🟢 |
| N5 | Skip to main content link | Required for keyboard-accessible model-driven apps | 🟡 |
| N6 | Mobile back button preserved | Power Apps mobile: never use Navigate() to replace history | 🔴 |

---

## Category 2 — Animation & Motion (7 rules)

| # | Rule | Power Apps Context | Severity |
|---|---|---|---|
| A1 | Max 1-2 animated elements per screen | Canvas apps: avoid stacking transitions | 🟡 |
| A2 | Transitions 150-300ms only | Use Transition.Fade or Transition.Cover — never custom delays > 300ms | 🟡 |
| A3 | Respect prefers-reduced-motion | Power Apps respects OS setting — do not override | 🔴 |
| A4 | Always show loading states | Use loading overlay or spinner during Patch() / async calls | 🔴 |
| A5 | No continuous decorative animation | No looping animations on non-loading elements | 🟡 |
| A6 | Hover ≠ tap (touch devices) | Do not rely on OnHover for critical actions — use OnSelect | 🔴 |
| A7 | Ease-out for entering, ease-in for exiting | Match Power Apps default transition curves | 🟢 |

---

## Category 3 — Layout (8 rules)

| # | Rule | Power Apps Context | Severity |
|---|---|---|---|
| L1 | Reserve space for async content | Show skeleton cards while galleries load | 🔴 |
| L2 | No content jumping on load | Set fixed Height on gallery items — never Auto during load | 🔴 |
| L3 | Viewport units on mobile correct | Canvas apps: use App.Width / App.Height, not fixed px | 🟡 |
| L4 | Max content width for readability | Wrap long form sections — max 800px effective width | 🟡 |
| L5 | Z-index / layering consistent | Overlapping controls in canvas: set Z-order explicitly | 🟡 |
| L6 | Overflow handled | Long text in labels: use Overflow = Scroll or EllipsisEnd | 🟡 |
| L7 | Fixed header/footer doesn't obscure content | Sticky command bar: add top padding to scrollable body | 🟡 |
| L8 | Content wider than viewport prevented | Canvas app: Width of containers ≤ App.Width | 🔴 |

---

## Category 4 — Touch & Mobile (6 rules)

| # | Rule | Power Apps Context | Severity |
|---|---|---|---|
| T1 | Touch targets minimum 44×44px | All buttons and icons: Height ≥ 44, Width ≥ 44 | 🔴 |
| T2 | 8px minimum gap between touch targets | Spacing between buttons ≥ 8px | 🟡 |
| T3 | No tap delay | Power Apps mobile handles this natively — don't add timers | 🟡 |
| T4 | Primary action is tap, not hover | OnSelect for all primary actions | 🔴 |
| T5 | Pull-to-refresh intentional only | Disable where not needed with overscroll containment | 🟢 |
| T6 | Haptic feedback for confirmations | Use Notify() with sound for approval actions on mobile | 🟢 |

---

## Category 5 — Interaction States (8 rules)

| # | Rule | Power Apps Context | Severity |
|---|---|---|---|
| I1 | Visible focus rings on all interactive elements | Gold outline (#C8962E) on focused controls | 🔴 |
| I2 | Hover state on clickable elements | FocusedBorderColor on inputs; HoverFill on buttons | 🟡 |
| I3 | Active/pressed state visual change | PressedFill slightly darker than HoverFill | 🟡 |
| I4 | Disabled state clearly distinct | Disabled controls: 50% opacity + DisplayMode.Disabled | 🟡 |
| I5 | Loading state on buttons during async | Set button DisplayMode.Disabled + show spinner on submit | 🔴 |
| I6 | Error feedback clear and near problem | Red border + error label below each failing field | 🔴 |
| I7 | Success feedback after actions | Notify("Saved successfully", NotificationType.Success) | 🟡 |
| I8 | Confirmation dialog before destructive actions | Confirm() before Delete / Reject / Cancel | 🔴 |

---

## Category 6 — Accessibility (10 rules)

| # | Rule | Power Apps Context | Severity |
|---|---|---|---|
| AC1 | Color contrast ≥ 4.5:1 for text | QDB Navy (#1B3A6B) on white = 9.8:1 ✅ | 🔴 |
| AC2 | Status never conveyed by colour alone | Status badges: always include text label + colour | 🔴 |
| AC3 | Alt text on all images | Set AccessibleLabel on Image controls | 🔴 |
| AC4 | Heading hierarchy logical | Use consistent font size hierarchy — don't skip levels | 🟡 |
| AC5 | ARIA labels on icon-only buttons | Set AccessibleLabel on icon Button controls | 🔴 |
| AC6 | Keyboard navigation logical | Tab order matches visual top-to-bottom left-to-right flow | 🔴 |
| AC7 | Form labels always visible | Never use placeholder as the only label | 🔴 |
| AC8 | Error messages announced | Use Notify() with NotificationType.Error for screen readers | 🔴 |
| AC9 | Motion sensitivity respected | OS reduced-motion setting honoured automatically | 🔴 |
| AC10 | Semantic structure | Model-driven: use proper form sections and tabs, not flat layout | 🟡 |

---

## Category 7 — Performance (8 rules)

| # | Rule | Power Apps Context | Severity |
|---|---|---|---|
| P1 | Images optimised | Compress images before uploading to SharePoint/Dataverse | 🟡 |
| P2 | Lazy load below-fold content | Use gallery pagination — never load all records on open | 🔴 |
| P3 | Cache repeat requests | Use collections to cache lookup data — don't re-fetch on every screen | 🟡 |
| P4 | Delegable queries only | All gallery Filter() must be delegable to avoid 500-record limit | 🔴 |
| P5 | App start time < 3 seconds | Move OnStart logic to named formulas — measure load time | 🔴 |
| P6 | Minimise connector calls on screen open | Batch calls; avoid calling 5+ connectors on a single screen | 🟡 |
| P7 | Named formulas for reusable data | App.Formulas instead of repeating ClearCollect in OnStart | 🟡 |
| P8 | Concurrency for parallel calls | Concurrent() for independent data loads on screen open | 🟡 |

---

## Category 8 — Forms (10 rules)

| # | Rule | Power Apps Context | Severity |
|---|---|---|---|
| F1 | Every input has a visible label above it | Label control above each TextInput — never placeholder-only | 🔴 |
| F2 | Errors appear below the related field | Error label (red, 12px) directly below failing control | 🟡 |
| F3 | Validate on blur not only submit | Validate field OnChange or when focus leaves | 🟡 |
| F4 | Correct input types used | TextInput Mode: Email, Password, Number where applicable | 🟡 |
| F5 | Required fields clearly marked | Asterisk (*) in red beside required field labels | 🔴 |
| F6 | Submit button shows loading state | Disable + spinner during Patch() / SubmitForm() | 🔴 |
| F7 | Inputs look interactive | Border on all TextInput controls — never borderless | 🟡 |
| F8 | Number keyboard on mobile for number fields | TextInput.Format = Number; InputMode = Numeric | 🟡 |
| F9 | Long forms use sections/tabs | Split forms with >8 fields into tabbed sections | 🟡 |
| F10 | Autosave or warn before navigation | Warn user if leaving form with unsaved changes | 🟡 |

---

## Category 9 — Feedback & States (7 rules)

| # | Rule | Power Apps Context | Severity |
|---|---|---|---|
| FB1 | Loading spinner for operations > 300ms | Show UpdateContext({isLoading:true}) overlay during Patch | 🔴 |
| FB2 | Empty state guides the user | Gallery empty: show "No records found. Create one." with button | 🟡 |
| FB3 | Error recovery provided | Error screen: show message + Retry button + contact helpdesk link | 🟡 |
| FB4 | Multi-step progress indicated | Show "Step 2 of 4" or progress bar in wizard flows | 🟡 |
| FB5 | Toast auto-dismisses in 3-5 seconds | Notify() handles this — do not use persistent banners | 🟡 |
| FB6 | Success confirmed | After Patch(): Notify("Saved", NotificationType.Success) | 🟡 |
| FB7 | Bulk actions available for list screens | Checkbox column + action bar for multi-record operations | 🟢 |

---

## Category 10 — Responsive / Multi-Device (9 rules)

| # | Rule | Power Apps Context | Severity |
|---|---|---|---|
| R1 | Design for tablet first, then phone | Target 1024px wide; test at 375px minimum | 🟡 |
| R2 | Test at multiple viewport sizes | 375, 768, 1024, 1366, 1920 | 🟡 |
| R3 | Touch-friendly on mobile layout | Increase button Height to 48px on phone form factor | 🔴 |
| R4 | Body text ≥ 14px everywhere | No text smaller than 12px (captions); body ≥ 14px | 🔴 |
| R5 | Viewport meta set | Canvas app handles this — verify in Power Apps mobile player | 🟡 |
| R6 | No horizontal scroll | Canvas app: all containers ≤ App.Width | 🔴 |
| R7 | Images scale with container | Image control: ImagePosition = Fit | 🟡 |
| R8 | Tables handle mobile | On phone: switch gallery from table to card layout | 🟡 |
| R9 | Line height 1.5-1.75 for body text | Power Apps Label LineHeight property ≥ 1.5 | 🟡 |

---

## Category 11 — Data Display (10 rules)

| # | Rule | Power Apps Context | Severity |
|---|---|---|---|
| D1 | Truncate long text gracefully | Label: Overflow = EllipsisEnd; Tooltip with full text | 🟡 |
| D2 | Format dates in locale | Text(date, "dd mmm yyyy") — never raw ISO strings | 🟡 |
| D3 | Format large numbers | Text(value, "#,###") for financial figures | 🟡 |
| D4 | Use realistic sample data in dev | Never show "Lorem ipsum" in demos to stakeholders | 🟢 |
| D5 | Empty tables show empty state | Not just a blank table — show message and CTA | 🟡 |
| D6 | Column headers always visible | Freeze header row on scrollable galleries | 🟡 |
| D7 | Sortable columns indicated | Arrow icon on sortable column headers | 🟢 |
| D8 | Currency formatted with symbol | "QAR " & Text(value, "#,###.00") | 🟡 |
| D9 | Percentages to 1 decimal place | Text(value, "0.0%") | 🟢 |
| D10 | Boolean shown as readable label | "Yes"/"No" or "Active"/"Inactive" — never true/false | 🟡 |

---

## How to Use This Skill

When invoked with `/ux-guidelines [screen name]`:

1. Ask for a description of the screen (or read the existing design)
2. Run through all 11 categories
3. Flag every violation with its severity (🔴/🟡/🟢)
4. Produce a **Violation Report**:

```
## UX Review: [Screen Name]
### 🔴 Blockers (X found — must fix before release)
- [Rule #] [Description] → [Specific fix]

### 🟡 Major Issues (X found — fix this sprint)
- [Rule #] [Description] → [Specific fix]

### 🟢 Minor Polish (X found — fix when time allows)
- [Rule #] [Description] → [Specific fix]

### ✅ Passing (X rules checked, no violation)
```
