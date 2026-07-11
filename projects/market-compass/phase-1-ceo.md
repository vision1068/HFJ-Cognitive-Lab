# Phase 1 — CEO Objectives & Business Case

**Date:** 2026-07-11  
**Project:** Market Compass  
**Phase:** 1 (CEO business decision)  
**Status:** ✅ APPROVED  

---

## Business Objective

Deploy Market Compass as a public-facing stock research platform accessible at `<project>.netlify.app`. The platform provides real-time PSX and global stock data for retail and professional investors, powered by live data from PSX Data Portal and Yahoo Finance. No authentication, no server-side state — stateless web app with minimal operations overhead.

---

## Success Criteria

1. **Live deployment** — Site accessible at `<project>.netlify.app` (or chosen Netlify subdomain) within budget
2. **Feature parity** — All existing functionality works identically (dashboard, search, company pages, charts, watchlists)
3. **Data integrity** — Live PSX prices, fundamentals, and Yahoo Finance data display correctly; graceful "data not available" when APIs fail
4. **Uptime expectation** — Educational platform (no SLA commitment); if down, manual redeploy in <5 minutes
5. **Security posture** — All existing proxy hardening (SSRF host-pin, keyless crumb, per-IP rate limit) preserved byte-for-byte; no secrets in repo

---

## Business Impact

**Positive:**
- Opens Market Compass to global audience (free market research tool)
- Demonstrates PSX data accessibility to international investors
- Establishes brand presence in stock research space

**Neutral:**
- No monetization (educational only)
- No revenue, no cost (free tier Netlify + open-source dependencies)
- Manual deployment; no CI/CD automation (low ops overhead as a tradeoff)

**Risk:**
- Public proxy endpoints may attract abuse (mitigation: per-IP rate limit, host allowlist)
- Dependent on PSX + Yahoo uptime (no SLA with either; graceful failure handled)
- No monitoring/alerting (mitigation: documented manual check procedure in DEPLOY.md)

---

## Constraint Clarification

**Approved by:** vision1068@gmail.com ✅  
**Timeline:** No rush — when ready  
**Audience:** Public (any internet user can access)  
**Deployment platform:** Netlify (serverless functions required for CORS proxy)  
**Data failure mode:** Show "data not available" (not cached/stale data)

---

## Known Issues Found During Phase 1

The orchestrator verified actual repo state and surfaced three engineering blockers that were not obvious from the brief:

### Issue 1: Serverless Function Convention Mismatch
- **Finding:** `api/psx/[...path].js` and `api/yahoo/[...path].js` use Vercel-style `export default async handler(req, res)` with raw Node `res.setHeader/res.end`
- **Problem:** Netlify Functions v2 use `export default async (req: Request, context: Context) => Response` (W3C Request-Response standard)
- **Resolution:** Created `netlify/functions/psx.mts` and `netlify/functions/yahoo.mts` porting both handlers to Netlify's convention while preserving all security controls

### Issue 2: Build Base-Path Bug
- **Finding:** `vite.config.ts:31` hardcodes `base: '/HFJ-Cognitive-Lab/market-compass/'` (GitHub Pages subpath)
- **Problem:** On Netlify root domain, every asset URL becomes `/HFJ-Cognitive-Lab/market-compass/assets/...` → 404s
- **Resolution:** Changed to `base: '/'` unconditionally (Netlify serves from domain root)

### Issue 3: Missing Deploy Scaffolding
- **Finding:** No `netlify.toml`, no `netlify/functions/`, no `DEPLOY.md`
- **Problem:** Prevents Netlify from finding functions, redirecting API calls, or understanding build setup
- **Resolution:** Created `netlify.toml` with build config + API redirects, wrote `DEPLOY.md` runbook

---

## Phase Outcome

✅ **Approved to proceed to Phase 2** (Architect).

- Business case validated
- Risks identified and documented
- Blockers surfaced and resolved during Phase 1 verification (not cascaded to later phases)
- All existing security hardening understood and protected

**Next:** Architect phase will design the deployment architecture, finalize API contracts between frontend and Netlify Functions, and prepare the work for Phase 3 (Build verification).
