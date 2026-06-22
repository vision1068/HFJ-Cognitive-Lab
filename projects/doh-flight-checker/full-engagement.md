# DOH Flight Checker — Full Engagement Plan
HFJ-Cognitive-Lab | Date: 2026-06-22 | Status: APPROVED (Phase 1)

---

## PHASE 1 — CEO: Business Framing

### Business Objective
Deliver a lightweight, browser-based flight discovery tool enabling users to check and search flights departing from Qatar to any global destination — no framework, no backend, no live deployment until client explicitly approves each phase.

### Target Users
- Travelers based in or transiting through Qatar seeking outbound flight options
- Travel agents or coordinators managing Qatar-origin itineraries
- Occasional users wanting fast, no-login flight lookup

### Success Criteria
1. User can view a list of flights from Qatar within 3 seconds of page load
2. Search/filter by airline, country, or city updates results within 1 second
3. Manual data refresh works without full page reload
4. Renders correctly on Chrome, Firefox, Safari, Edge (desktop and mobile)
5. All phases reviewed and approved by client before any public deployment
6. Zero external framework dependencies — pure HTML/CSS/JS

### Constraints
- No live deployment until each phase receives explicit client sign-off
- No frameworks — vanilla HTML/CSS/JS only
- Data source TBD (free API, paid API, or mock JSON)
- No backend unless client approves
- Phased delivery — each increment delivered for review before proceeding

### Risks Flagged Early
| Risk | Impact | Mitigation |
|---|---|---|
| Data source not confirmed | Blocks development | Confirm before Phase 2 |
| Public API rate limits / costs | Cost overrun | Evaluate free tiers upfront |
| API keys exposed client-side | Security breach | Proxy layer required for any paid API |
| Scope creep (booking, accounts) | Delays and cost | Lock scope to read-only v1 |
| Arabic RTL not designed in | Expensive retrofit | Confirm language requirement before build |

---

## PHASE 2 — Architect: Technical Design

### Recommended Data Source
- Phase 1: Static `flights.json` (mock, 40 records) — zero cost, no API key, CORS-safe
- Phase 2: AviationStack API via Cloudflare Worker proxy (API key never client-side)
- Switch between phases is a single config variable change — no structural rewrite

### Application File Structure
```
/flight-checker/
├── index.html
├── assets/
│   ├── css/styles.css
│   └── img/logo.svg
├── data/
│   └── flights.json          (Phase 1 only)
├── js/
│   ├── config.js             (data source URL, airport code, refresh interval)
│   ├── data-service.js       (fetch abstraction — swappable)
│   ├── flight-renderer.js    (DOM rendering only)
│   ├── filter.js             (client-side search logic)
│   ├── refresh.js            (manual + optional auto-refresh)
│   └── app.js                (bootstrap, wires all modules)
└── CHANGELOG.md
```

### Pages / Screens
Single-page application. Four visual states:
1. Flight Board (default) — full departure list, sortable by time
2. Filtered View (inline) — live-filtered list, no navigation
3. Error State — fetch failed, retry button visible
4. Empty State — filter matched zero records, clear-filter link

### Data Flow
```
config.js → data-service.js → app.js → filter.js → flight-renderer.js (DOM)
```
Raw flight array is never mutated. Filter produces a new derived array on every keypress.

### API Key Security
Phase 1: No key needed.
Phase 2: Cloudflare Worker holds the key as a Worker Secret. Browser never sees it. Worker enforces origin to DOH, rate limits to 30 req/min per IP, and CORS allowlist.

### Browser Targets
Chrome 90+, Firefox 90+, Safari 14+, Edge 90+, Mobile Chrome (Android 10+), Mobile Safari (iOS 14+). No IE support.

---

## PHASE 3 — Backend: Data and API Layer

### Mock Data Schema (key fields)
```
id, flight_number, airline_code, airline_name,
origin_iata (always DOH), origin_city, origin_country,
destination_iata, destination_city, destination_country, destination_country_code,
scheduled_departure, estimated_departure, actual_departure,
scheduled_arrival, estimated_arrival, actual_arrival,
status (SCHEDULED|BOARDING|DEPARTED|IN_FLIGHT|LANDED|CANCELLED|DIVERTED|DELAYED),
terminal, gate, aircraft_type, duration_minutes,
is_codeshare, codeshare_partners[], data_source, fetched_at
```

### Mock Dataset
40 records: 8 airlines (18 Qatar Airways + 22 spread across 7 others), 15 destination countries, 20 distinct cities, all 8 statuses represented, departure times spread across 24 hours.

### DataService Contract
- `DataService.init(config)` — reads source URL, timeout, cache TTL
- `DataService.getFlights(filters)` — returns Promise<FlightResult> with success/error shape
- `DataService.refresh()` — bypasses cache, re-fetches
- Errors: NETWORK_ERROR, TIMEOUT, PARSE_ERROR, SCHEMA_INVALID, HTTP_ERROR, RATE_LIMITED, CONFIG_MISSING
- Filtering in Phase 1 is in-memory; in Phase 2 filters become proxy query params

### Phase 2 Proxy Contract
```
GET /api/v1/flights?origin=DOH&date=YYYY-MM-DD&limit=50&offset=0
```
Response: `{ meta: {...}, flights: [...normalized records...] }`
Proxy enforces origin=DOH server-side regardless of client input.

### Recommended Flight API
Primary: AviationStack (dep_iata=DOH, 100 calls/month free)
Secondary: AeroDataBox via RapidAPI (500 calls/month free, more fields)
Proxy handles normalization; client always receives internal schema.

---

## PHASE 3 — Frontend: UI/UX Design

### Layout
- Sticky header: app name + "Last updated" timestamp (right)
- Controls bar: search input (60% width, left) + Refresh button (right)
- Content area: table (desktop) or card stack (mobile), or Error/Empty state

### Table Columns (desktop)
Flight No. | Airline | Destination City | Country | Departure Time | Terminal | Status

### Search Input
Single input matching airline, city, and country simultaneously. Live filter on keypress (150ms debounce). Clear button (X) appears when input has content. Result count shown: "Showing N of 34 flights."

### Status Badge System
| Status | Background | Text | Label |
|---|---|---|---|
| SCHEDULED/ON TIME | #D1FAE5 | #065F46 | ON TIME |
| DELAYED | #FEF3C7 | #92400E | DELAYED |
| BOARDING | #DBEAFE | #1E40AF | BOARDING |
| DEPARTED/IN FLIGHT | #F3F4F6 | #374151 | DEPARTED |
| CANCELLED | #FEE2E2 | #991B1B | CANCELLED |

All pairs meet WCAG AA (4.5:1 contrast).

### Mobile (below 640px)
Table hidden; card stack shown. 2-column CSS Grid for flight fields. Full-width search input. Refresh button right-aligned below search. No horizontal scroll anywhere.

### Accessibility
role="grid" on table, aria-live="polite" on results container, aria-busy on refresh button during load, role="alert" on error/empty states, semantic HTML throughout, visible focus ring (2px solid #2563EB).

### Typography
System font stack (no web font). Neutral palette: white surfaces, #1F2937 body text, #2563EB accent. CSS custom properties on :root.

---

## PHASE 3 — Middleware: Integration Design

### Phase 1 (Mock)
DataService fetches ./data/flights.json via fetch(). Same-origin, no CORS, no server needed. flights.json is pre-normalized to internal schema. DATA_SOURCE flag = "mock".

### Phase 2 (Cloudflare Worker)
- Platform: Cloudflare Worker (zero cold start, 100K req/day free, KV cache built-in, edge proximity)
- Caching: KV Store keyed on `flights:DOH:{date}:{offset}`
  - Today: 5-minute TTL
  - Tomorrow: 15-minute TTL
  - Past dates: 60-minute TTL
  - Stale served on upstream failure with X-Cache-Status: STALE header
- AviationStack mapping: upstream flight_status → internal enum (e.g., "active" → "DEPARTED", "scheduled" + no delay → "ON_TIME")
- Fallback: AeroDataBox on AviationStack 5xx (ICAO OTHH = DOH)
- CORS: explicit allowlist, no wildcard, GET + OPTIONS only, 403 on unauthorized origin

### Mock-to-Live Upgrade
Two config changes only:
1. DATA_SOURCE flag: "mock" → "live"
2. Base URL: "./data/flights.json" → Worker URL

No application logic, rendering, or error handling changes required.

---

## PHASE 4 — QA: Test Strategy

### Scope
60+ test cases covering: functional (9 core), search edge cases (12), error states (6), refresh (6), cross-browser (7 browsers, 5 scenarios each), mobile (6 viewports, 6 scenarios each), accessibility (10), Phase 2 live API (10).

### Critical Test Cases
- TC-FUNC-001: 40 records on initial load, no blank fields
- TC-FUNC-007: Live filter on keypress (no submit needed)
- TC-EDGE-006: Special characters do not produce XSS or crash
- TC-ERR-001: Error state on 404 of flights.json
- TC-REF-004: No duplicate rows on rapid successive refresh

### Go-Live Acceptance Criteria (Phase 1)
- All 40 records render with no undefined/null fields
- All 8 status types visible and visually distinct
- Live filter, clear filter, empty state, error state, refresh all pass
- All P1 browsers pass 5 core test cases
- Mobile usable at 375px, touch targets 44x44px minimum
- Zero critical/serious Axe violations
- Lighthouse desktop Performance score >= 90
- No uncaught JS exceptions under any test scenario

---

## PHASE 5 — Auditor: Governance Review

### Summary
29 gaps identified across 8 domains. None block Phase 1 (static, no live deployment).

### Phase 2 Hard Blockers (6 items)
1. PDPL data residency — Cloudflare US processing of Qatar IPs requires region pinning or DPA review
2. API key governance — MFA on Cloudflare account, service account (not personal email), audit logs enabled
3. Key rotation procedure — documented, tested, zero-downtime runbook required
4. CORS hardened — no wildcard, explicit allowlist, 403 on unauthorized origin
5. CSP header — no unsafe-inline, no unsafe-eval, fetch restricted to known endpoints
6. DOM XSS guard — search input sanitized before any DOM insertion, verified by QA

### Regulatory Notes
- Qatar PDPL (Law No. 13 of 2016) applies to IP address processing of Qatar-based users
- AviationStack ToS must confirm public display of flight data is permitted
- If HFJ-Cognitive-Lab is QCB-regulated: vendor risk register entries required for AviationStack and Cloudflare
- Disclaimer of data accuracy required in UI before go-live

---

## PHASE 6 — CEO: Final Decision

### DECISION: APPROVED

Phase 1 (mock data, static, no live deployment) is approved to proceed to build immediately.
Phase 2 (live API, Cloudflare Worker) is conditionally approved pending 8 gates listed below.

### Pre-Build Client Questions (must answer before code is written)
1. Which airports? DOH only, or additional Qatari airports?
2. Which airlines? All departing airlines, or priority list?
3. Language? English only, or bilingual English + Arabic (RTL)?
4. Who is the intended user? Internal staff, specific client team, or public?
5. Hosting target for Phase 2? Cloudflare, QDB-managed, or client-managed?
6. AviationStack ToS reviewed? Who holds the contract?
7. Client's definition of "approval before go-live"? Named approver, UAT environment, written sign-off?

### Build Sequence
1. flights.json mock data (40 records, all statuses, 8 airlines, 15 countries)
2. DataService abstraction module (contract finalized at this step)
3. UI shell (header, table, cards, badges) — verified on all browsers/viewports
4. Search and filter logic — full QA functional and edge-case suite run
5. Refresh control — loading state, no duplicates, rapid-click safe
6. Accessibility pass — Axe, WCAG AA contrast, keyboard nav, screen reader labels
7. Client demo package — static build, written sign-off received

### Phase 2 Conditions (all 8 must be met before Phase 2 build begins)
1. PDPL data residency confirmed in writing (Cloudflare region or DPA review)
2. AviationStack ToS reviewed and contract signed by operating entity
3. API key governance in place (MFA, service account, audit logs, rotation runbook)
4. CORS policy hardened and tested (no wildcard, 403 on unauthorized origin confirmed)
5. CSP header implemented and verified (no unsafe-inline or unsafe-eval)
6. DOM XSS guard on search input verified by QA
7. Client written approval of Phase 1 demo received
8. Phase 2 hosting target confirmed and infrastructure procured

---

## CONSOLIDATED BUILD PLAN

### What We Are Building
A dependency-free, single-page HTML flight checker that displays outbound departures from Hamad International Airport (DOH) in Qatar. Users can search and filter flights in real time by airline, destination city, or country, and manually refresh the flight data. Phase 1 ships with a 40-record mock dataset to demonstrate the full user experience; Phase 2 introduces a live data feed via a Cloudflare Worker proxy to the AviationStack API, with the API key stored securely server-side and never exposed to the browser. The application is built in plain HTML, CSS, and JavaScript with no external frameworks, libraries, or build tools.

### Tech Stack
- HTML5 — semantic markup, single index.html entry point
- CSS3 — custom properties, Flexbox, CSS Grid, single breakpoint at 640px, no framework
- Vanilla JavaScript ES6+ — modular JS files, no bundler, no transpiler
- flights.json — static mock data file (Phase 1)
- Cloudflare Worker — thin proxy holding AviationStack API key (Phase 2 only)
- Cloudflare Workers KV — proxy-level cache (5-min TTL for today's flights, Phase 2 only)
- AviationStack API — upstream live flight data source (Phase 2 only)
- No npm, no node_modules, no build pipeline, no CDN dependencies

### Pages / Screens
1. Flight Board (default) — full list of DOH departures, sorted by scheduled time
2. Filtered View (inline state) — live-narrowed list as user types, no page change
3. Error State — data fetch failed, user-friendly message, retry button
4. Empty State — filter matched zero records, "No flights found for X", clear-filter link

### Data Source Recommendation
Phase 1: Static `data/flights.json` loaded via fetch() — same-origin, no server required, 40 hand-authored records covering all 8 status types, 8 airlines, 15 countries, 20 cities.
Phase 2: AviationStack free tier (primary) queried via Cloudflare Worker proxy. AeroDataBox via RapidAPI as fallback on upstream failure. Switch from mock to live is a two-variable config change in config.js; no application logic changes required.
Mock fallback: If Phase 2 live API becomes unavailable, reverting the two config variables restores the Phase 1 mock experience in under 60 seconds.

### Phase 3 Deliverables
- Backend: flights.json mock file (40 records, internal schema), DataService JS module with full error handling, proxy API contract specification
- Frontend: index.html + styles.css + all JS modules, status badge system, responsive table/card layout, accessible search input, refresh button with loading state
- Middleware: Cloudflare Worker configuration (wrangler.toml), KV cache logic, AviationStack normalization map, CORS allowlist, error propagation design

### Go-Live Gate
The following conditions must ALL be met and documented before any deployment to a live URL:

1. Client has answered the 7 pre-build questions in writing
2. Phase 1 mock version has been reviewed and approved by the named client approver
3. All Phase 1 QA go-live acceptance criteria pass (functional, browser, mobile, accessibility, performance)
4. PDPL data residency risk resolved in writing (Cloudflare region or DPA)
5. AviationStack ToS reviewed and signed by operating entity
6. API key governance in place (MFA, service account, rotation runbook, audit logs)
7. CORS policy tested: unauthorized origin returns 403
8. CSP header implemented with no unsafe directives
9. Search input DOM XSS guard verified by QA
10. Data accuracy disclaimer visible in the production UI
11. Phase 2 hosting environment procured and accessible
12. Vendor risk register entries created for AviationStack and Cloudflare
