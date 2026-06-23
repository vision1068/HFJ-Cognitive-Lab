---
name: bi-dashboard-styles
description: >
  10 BI/Analytics Dashboard styles for Power BI embedded reports and
  canvas app dashboards. Use when designing or reviewing any QDB
  Power BI report, executive dashboard, operational monitor, or
  analytics screen. Produces layout spec, chart choices, colour
  assignments, and KPI card design for each style.
  Invoke with: /bi-dashboard-styles [dashboard purpose]
---

# 10 BI/Analytics Dashboard Styles — QDB Power BI Edition
*Source: nextlevelbuilder/ui-ux-pro-max-skill BI styles — adapted for Power BI / QDB brand*

## How to use
1. Invoke with `/bi-dashboard-styles [purpose]`
2. Match your dashboard to one of the 10 styles below
3. Apply the layout, chart types, and colour assignments specified
4. Use QDB brand tokens throughout (no custom colours without approval)

---

## Style 1 — Data-Dense Dashboard
**Best for:** Operations teams, processing queues, multi-metric monitoring

### Characteristics
- Maximum information density — 8-16 widgets per screen
- Minimal padding (8px between widgets)
- Compact KPI cards (no large numbers — show value + label + trend in small card)
- Heavy use of tables and matrices
- Colour used sparingly — only for status

### Layout
```
┌──┬──┬──┬──┐
│K1│K2│K3│K4│  ← KPI strip (compact, 60px height)
├──┴──┼──┴──┤
│Table│ Bar │  ← Primary data (60% / 40% split)
│     │Chart│
├─────┼─────┤
│Matrix│Line│  ← Secondary data
└─────┴─────┘
```

### Chart Types
- KPI Cards (compact), Tables, Matrices, Bar charts, Line charts
- NO pie/donut — too little space for labels

### QDB Colour Rules
- Background: #F4F6F9
- Card background: #FFFFFF
- Header accent: #1B3A6B
- Status indicators only: use semantic colours (green/amber/red)
- Data series: --qdb-chart-1 through --qdb-chart-4

### Anti-Patterns
- No large hero numbers (wastes density)
- No decorative images
- No gradient backgrounds

---

## Style 2 — Heat Map & Matrix
**Best for:** Portfolio concentration, sector exposure, risk mapping, geographical spread

### Characteristics
- Colour-coded grid/matrix as primary visual
- Data intensity shown by colour gradient
- Navy → White → Gold gradient for neutral ranges
- Red for breach/risk; Green for target-achieved

### Layout
```
┌─────────────────────────┐
│ [Filter bar + Title]    │
├─────────────────────────┤
│                         │
│   HEAT MAP (primary)    │  ← 70% of canvas
│   Sector × Quarter      │
│                         │
├────────────┬────────────┤
│ Legend     │ Top 5 List │  ← Supporting context
└────────────┴────────────┘
```

### Chart Types
- Matrix visual (conditional formatting by value)
- Table (sorted, top/bottom 5)
- Colour legend slicer

### QDB Colour Rules
- Low intensity: #EFF6FF (light blue)
- Mid intensity: #1B3A6B (navy)
- High intensity: #C8962E (gold)
- Breach/risk: #991B1B (red)
- Target met: #166534 (green)

### Anti-Patterns
- No more than 5 colour steps in gradient
- Always include a legend — colour alone is inaccessible
- Never use rainbow gradients

---

## Style 3 — Executive Dashboard
**Best for:** Board packs, CEO/CFO view, monthly portfolio summary, QDB leadership reports

### Characteristics
- 4-6 large KPI cards dominate the top
- Minimal detail — summary only, no raw data tables
- Strong use of trend indicators (▲ ▼ →)
- One primary chart below KPIs
- Clean white space — never crowded

### Layout
```
┌──────┬──────┬──────┬──────┐
│ KPI  │ KPI  │ KPI  │ KPI  │  ← Large cards (100px height)
│ QAR  │  %   │  #   │  %   │
│ 2.4B │ 94%  │ 247  │ 12%  │
└──────┴──────┴──────┴──────┘
┌─────────────────────────────┐
│  Primary Chart (full width) │  ← Line or column chart
│  e.g. Portfolio Growth YTD  │
└─────────────────────────────┘
┌──────────────┬──────────────┐
│ Donut Chart  │ Commentary   │  ← Optional insight text
│ (Sector Mix) │ box          │
└──────────────┴──────────────┘
```

### Chart Types
- KPI Cards (large, bold number), Line chart, Column chart, Donut chart
- NO tables — executives do not read raw data in this view

### QDB Colour Rules
- KPI card number: #1B3A6B (navy) or #C8962E (gold for key metric)
- Trend up: #166534 (green) with ▲
- Trend down: #991B1B (red) with ▼
- Trend neutral: #6B7280 (grey) with →
- Chart series 1: #1B3A6B, series 2: #C8962E

### Anti-Patterns
- No more than 6 KPI cards on one screen
- No drill-through on executive view (they don't want it)
- No small text — minimum 14px everywhere

---

## Style 4 — Real-Time Monitoring
**Best for:** Operations centre, daily processing status, queue monitoring, system health

### Characteristics
- Auto-refresh (30s–5min depending on data)
- Status indicators (RAG — Red/Amber/Green) prominent
- Alert/exception count always visible
- Timestamps on every metric
- Dark or neutral background to reduce eye strain on long viewing

### Layout
```
┌──────────────────────────────────┐
│ [Status: ● LIVE] [Updated: 14:32]│
├────────┬────────┬────────┬───────┤
│ ● OK   │ ● OK   │ ● WARN │ ● ERR │  ← System status strip
├────────┴────────┴────────┴───────┤
│ Queue Depth (line, last 2h)       │
├─────────────────┬─────────────────┤
│ Exceptions List │ Throughput/hr   │
│ (auto-refresh)  │ (gauge/bar)     │
└─────────────────┴─────────────────┘
```

### Chart Types
- Status cards (RAG), Line chart (time series), Gauge, Table (exceptions, auto-sorted)

### QDB Colour Rules
- OK / Normal: #166534 (green) + ● dot
- Warning / At-risk: #92400E (amber) + ● dot
- Error / Breach: #991B1B (red) + ● dot
- Live indicator: animated ● in green

### Anti-Patterns
- Never cache data more than 5 minutes for a live monitor
- No static screenshots in a live dashboard
- Alert count must be visible without scrolling

---

## Style 5 — Drill-Down Analytics
**Best for:** Loan portfolio analysis, sector deep-dives, client relationship analytics

### Characteristics
- Hierarchical navigation: summary → category → record
- Breadcrumb trail shows current drill level
- "Back to overview" always visible
- Filters persist across drill levels
- Detail level shows tables with export

### Layout
```
┌───────────────────────────────────┐
│ Home > Sector > Corporate Banking │  ← Breadcrumb
├───────────────────────────────────┤
│ [Sector filter] [Period] [Status] │
├────────────────┬──────────────────┤
│ Parent Chart   │ Drilled Detail   │
│ (clickable     │ (updates on      │
│  to drill)     │  click)          │
├────────────────┴──────────────────┤
│ Detail Table (sortable, paginated)│
└───────────────────────────────────┘
```

### Chart Types
- Treemap or Stacked bar (drill source), Bar/Column (drill target), Table (record level)

### QDB Colour Rules
- Selected/drilled element: #C8962E (gold highlight)
- Unselected: #1B3A6B at 40% opacity
- Detail table: standard QDB table tokens

### Anti-Patterns
- Always show what level you are at — never drill without breadcrumb
- Preserve filter selections on drill-back
- Never open drill in a new page — update in-canvas

---

## Style 6 — Comparative Analysis
**Best for:** Period-over-period, sector benchmarking, A/B comparison, budget vs actual

### Characteristics
- Two-column or overlay comparison layout
- Period selector prominent (This Year / Last Year / Budget)
- Variance clearly shown (▲ +12% or ▼ -8%)
- Consistent colour coding: current = navy, prior = gold, budget = grey

### Layout
```
┌──────────────────────────────────┐
│ [Period A] vs [Period B] toggle  │
├──────────────┬───────────────────┤
│ Period A     │ Period B          │
│ Metric Cards │ Metric Cards      │
├──────────────┴───────────────────┤
│ Clustered Bar (side-by-side)     │
├──────────────────────────────────┤
│ Variance Table (+ / - by row)    │
└──────────────────────────────────┘
```

### Chart Types
- Clustered Bar/Column, Line (overlay), Table with conditional formatting on variance

### QDB Colour Rules
- Current period: #1B3A6B (navy)
- Prior period: #C8962E (gold)
- Budget/target: #6B7280 (grey)
- Positive variance: #166534 (green)
- Negative variance: #991B1B (red)

### Anti-Patterns
- Never compare more than 3 periods on one chart — too cluttered
- Always label which series is which — no guessing from colour alone
- Never use 3D charts for comparison — distorts values

---

## Style 7 — Predictive Analytics
**Best for:** Portfolio forecasts, NPL projections, capacity planning, stress testing

### Characteristics
- Historical line + forecast line clearly distinguished
- Confidence interval shown as shaded band
- Scenario lines (Base / Optimistic / Pessimistic)
- "As of date" marker on time axis
- Model assumptions accessible but not dominant

### Layout
```
┌────────────────────────────────────┐
│ [Model: Base] [As of: Jun 2026]    │
├────────────────────────────────────┤
│ Historical ──── | ---- Forecast    │
│ (solid line)  Today  (dashed)      │
│              [confidence band]     │
├──────────────┬─────────────────────┤
│ Scenario     │ Key Assumptions     │
│ Summary      │ (collapsible)       │
└──────────────┴─────────────────────┘
```

### Chart Types
- Line chart (historical + forecast), Area chart (confidence band), Table (scenario comparison)

### QDB Colour Rules
- Historical (actual): #1B3A6B (navy, solid line)
- Forecast base: #1B3A6B (navy, dashed)
- Confidence band: #1B3A6B at 10% opacity
- Optimistic scenario: #166534 (green, dashed)
- Pessimistic scenario: #991B1B (red, dashed)

### Anti-Patterns
- Always label "FORECAST" clearly — never present projections as actuals
- Always show model assumptions — hidden assumptions are a governance risk
- Confidence intervals are mandatory — never show forecast without range

---

## Style 8 — User Behaviour Analytics
**Best for:** Portal usage, digital adoption, process funnel analysis, CRM activity tracking

### Characteristics
- Funnel visualization as primary element
- Drop-off rates at each stage
- User flow / journey diagrams
- Conversion rates prominent
- Segmentation filters (team, role, region)

### Layout
```
┌─────────────────────────────────┐
│ [Segment filter] [Date range]   │
├─────────────────────────────────┤
│ FUNNEL CHART (primary)          │
│ Step 1: 1,200 users             │
│ Step 2: 840 users  (▼ 30%)      │
│ Step 3: 610 users  (▼ 27%)      │
│ Step 4: 422 users  (▼ 31%)      │
├────────────────┬────────────────┤
│ Drop-off table │ Top paths      │
└────────────────┴────────────────┘
```

### Chart Types
- Funnel chart, Sankey/flow diagram, Table (drop-off analysis), Line (trend over time)

### QDB Colour Rules
- Funnel fill: #1B3A6B gradient top to bottom (darkest at top)
- Drop-off highlight: #991B1B
- Conversion success: #166534
- Neutral steps: #6B7280

### Anti-Patterns
- Never omit drop-off percentages — they are the point
- Never show funnel without sample size (N=)
- Always segment — aggregate funnel hides important patterns

---

## Style 9 — Financial Dashboard
**Best for:** P&L summary, revenue tracking, budget vs actual, QDB financial performance

### Characteristics
- Revenue, cost, margin as primary trio
- Waterfall chart for variance explanation
- Currency formatting throughout (QAR)
- Period selectors: MTD, QTD, YTD, Custom
- Conservative, high-trust aesthetic

### Layout
```
┌──────┬──────┬──────┐
│ Rev  │ Cost │Margin│  ← Primary KPI trio
│ QAR  │ QAR  │  %   │
│ 2.4B │ 1.8B │ 25%  │
├──────┴──────┴──────┤
│ Waterfall Chart    │  ← Budget to Actual variance
├────────┬───────────┤
│ P&L    │ Trend     │
│ Table  │ Line YTD  │
└────────┴───────────┘
```

### Chart Types
- KPI Cards, Waterfall chart, Table (P&L matrix), Line chart (monthly trend)

### QDB Colour Rules
- Revenue (positive): #1B3A6B (navy)
- Cost (negative in waterfall): #991B1B (red)
- Net/Margin: #C8962E (gold)
- Favourable variance: #166534 (green)
- Unfavourable variance: #991B1B (red)
- All values: "QAR " prefix, "#,###" format

### Anti-Patterns
- Never show financial data without period label
- Waterfall bridges must be grey — not coloured
- Never mix currencies without explicit labelling

---

## Style 10 — Sales Intelligence
**Best for:** Lending pipeline, deal tracking, relationship manager performance, sector targets

### Characteristics
- Pipeline funnel by stage
- Sales rep / RM leaderboard
- Territory / sector performance map or table
- Win rate and average deal size always visible
- Activity metrics (calls, visits, proposals)

### Layout
```
┌──────┬──────┬──────┬──────┐
│Pipeline│Win  │Avg   │Close │
│Value   │Rate │Deal  │Rate  │
├────────┴──────┴──────┴─────┤
│ Pipeline by Stage (funnel) │
├────────────┬───────────────┤
│ RM Leader- │ Sector Target │
│ board      │ vs Actual     │
└────────────┴───────────────┘
```

### Chart Types
- KPI Cards, Funnel chart, Table (leaderboard, sortable), Bar chart (target vs actual by sector)

### QDB Colour Rules
- On/above target: #166534 (green)
- Close to target (>80%): #C8962E (gold)
- Below target (<80%): #991B1B (red)
- Pipeline stages: sequential tint of #1B3A6B (100% → 20%)
- Leaderboard rank 1: #C8962E (gold highlight)

### Anti-Patterns
- Never show a leaderboard without a date range — context is everything
- Win rate without deal count is meaningless — always show N=
- Never use 3D funnel — distorts stage proportions

---

## Output Format

When `/bi-dashboard-styles [purpose]` is invoked:

1. **Recommended Style** — which of the 10 applies and why
2. **Layout Wireframe** — text diagram of the recommended layout
3. **Chart Type List** — exact Power BI visuals to use
4. **Colour Assignment** — which QDB token goes on which element
5. **KPI Card Spec** — title / value format / trend indicator for each card
6. **Anti-Pattern Checklist** — confirm the 3 anti-patterns for this style are avoided
7. **Power BI Design Checklist** — page size, theme file reference, accessibility note
