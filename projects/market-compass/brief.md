# Market Compass — Production Deployment Brief

**Project:** market-compass  
**Date:** 2026-07-11  
**Approver:** vision1068@gmail.com  
**Status:** Requirements intake complete — ready for Phase 1 (CEO)

---

## One-Line Goal

Deploy Market Compass as a public-facing stock research platform on Netlify with live PSX and Yahoo Finance data.

---

## Vision

Market Compass is a premium, personal stock-research tool for the Pakistan Stock Exchange (PSX). All market data is real and live—sourced from PSX Data Portal and Yahoo Finance—with auto-refresh every 60 seconds. This deployment makes it publicly available at `<project>.netlify.app` for retail and professional investors worldwide to research PSX companies.

---

## Functional Requirements

### FR-1: Production Deployment to Netlify
- App deploys to `<project>.netlify.app` (Netlify subdomain)
- Serverless functions in `/api/psx` and `/api/yahoo` route proxied requests upstream (CORS bypass)
- Dev-time Vite proxy mirrors behavior of production serverless functions

**Acceptance Criteria:**
- Site is live and publicly accessible at `<project>.netlify.app`
- All API routes (`/api/psx/*`, `/api/yahoo/*`) respond with proper headers

### FR-2: Live Market Data Preservation
- Stock prices, fundamentals, indices, and USD/PKR rate load and display without degradation from current (development) behavior
- 60-second refresh cycle maintained for TanStack Query cache

**Acceptance Criteria:**
- Company detail pages display live prices, P/E, market cap, charts
- Dashboard shows live indices (KSE-100, KMI-30)
- No data fields show `undefined` or fallback mock values

### FR-3: Smart Search Across Data Sources
- Global search works across PSX-listed companies (10 blue chips) and Yahoo Finance symbols
- Search results show source badges (PSX vs. Global)
- PSX results display live prices; global results show "No local quote"

**Acceptance Criteria:**
- Searching "OGDC" returns PSX result with live price
- Searching "Apple" returns 7+ global results, each marked "Global"
- No search results fabricate prices for unavailable symbols

### FR-4: Graceful Data Unavailability
- When PSX Data Portal or Yahoo Finance is down, UI shows "Data not available" state
- No stale/cached data served; no fallback to previous session prices
- User sees honest error state, not degraded data

**Acceptance Criteria:**
- Company detail page shows "Couldn't load live market data" when API fails
- Search results omit symbols if no data is available
- No cached data from >60 seconds ago is shown

### FR-5: Public Access Without Authentication
- All routes are publicly accessible
- No login, registration, or sign-in flow required
- Watchlists and portfolio data stored in browser localStorage (ephemeral, not persisted server-side)

**Acceptance Criteria:**
- Home page loads without redirect
- All pages accessible from public IP
- localStorage is used for watchlists; data is lost on browser clear

---

## Non-Functional Requirements

### NFR-1: Minimal Operations Model
- No uptime SLA (educational, not production-grade service)
- No monitoring, alerting, or on-call rotation required
- Manual redeploy via Netlify UI if needed

**Acceptance Criteria:**
- Deployment runbook (DEPLOY.md) documents manual redeploy steps
- No Sentry, DataDog, or monitoring platform required

### NFR-2: No Regulatory Compliance Burden
- Educational disclaimer already in UI ("not financial advice")
- No QCB approval required
- No data residency restrictions (data is read-only, from public PSX/Yahoo APIs)

**Acceptance Criteria:**
- Legal disclaimer visible on home page
- No compliance audit needed pre-deployment

### NFR-3: No Disaster Recovery
- No database; no user data to backup
- No disaster recovery plan (site can be redeployed from source in <5 minutes)
- localStorage data is browser-local and ephemeral

**Acceptance Criteria:**
- Brief acknowledgment that RTO/RPO is not applicable (stateless app)

### NFR-4: Performance & Bundle
- Lighthouse score ≥90 (or documented reason)
- Bundle size justified (React 19 + TanStack Query + Recharts is expected to be >500KB gzipped)

**Acceptance Criteria:**
- Lighthouse audit run and score recorded
- Bundle size analysis included in performance review

---

## Acceptance Criteria (Full List)

- **AC-1:** Site is live and publicly accessible at `<project>.netlify.app`
- **AC-2:** Home page loads without console errors in Chrome and Firefox
- **AC-3:** Company search (global + PSX) returns results with correct source badges
- **AC-4:** Company detail pages display live prices, market cap, P/E, and charts
- **AC-5:** When API is down, "Data not available" state displays (no stale data served)
- **AC-6:** All 56 unit tests pass post-deploy (no regressions)
- **AC-7:** Legal disclaimer ("not financial advice") visible on homepage
- **AC-8:** Deployment runbook (steps to redeploy) documented in DEPLOY.md
- **AC-9:** Lighthouse performance score measured and recorded
- **AC-10:** No secrets (API keys, tokens) in source or `.env`

---

## Scope

### In Scope
- Deploy frontend + serverless proxy functions to Netlify
- Public, unauthenticated access
- Live data + graceful failures
- Existing feature set (dashboard, markets, screener, watchlists, portfolio, search, live analysis)
- Unit tests + manual smoke test

### Out of Scope
- Monitoring / alerting / SLA
- User authentication or accounts
- Server-side portfolio persistence (localStorage only)
- Disaster recovery / backups
- QCB regulatory approval
- Post-deployment support contract
- Custom domain (use Netlify default subdomain)
- Payment processing (educational only)
- News feed or external data sources beyond PSX + Yahoo

---

## Success Metrics

1. ✅ Deployment succeeds without errors
2. ✅ Site accessible and functional from public internet
3. ✅ All AC-1 through AC-10 pass
4. ✅ No HIGH/CRITICAL security findings
5. ✅ Performance baseline recorded (Lighthouse)

---

## Timeline & Approvals

**Approver:** vision1068@gmail.com ✅ **Approved**  
**Timeline:** No rush—when ready  
**Next Step:** Hand brief to orchestrator for Phase 1 (CEO) → Phase 2 (Architect) → Phase 3 (Build verification) → Phase 4 (QA) → Phase 5 (Audit) → Phase 6 (CEO final) → Gate 6 (Production Readiness) → Deploy

---

## Assumptions & Constraints

- PSX Data Portal and Yahoo Finance APIs remain publicly accessible and rate-limited (not blocked)
- Netlify free tier or paid tier sufficient for traffic volume
- No custom domain required (use `<project>.netlify.app`)
- Browser localStorage sufficient for watchlist/portfolio storage (no server-side persistence)
