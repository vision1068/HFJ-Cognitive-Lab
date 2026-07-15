# Phase 2 — Deployment Architecture

**Date:** 2026-07-11  
**Project:** Market Compass  
**Phase:** 2 (Architect)  
**Status:** ✅ COMPLETE  

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│ User Browser (localhost:5173 dev / something.netlify.app prod)  │
└────────────────┬────────────────────────────────────────────────┘
                 │
      React Router + TanStack Query
      Same-origin API calls: /api/psx/*, /api/yahoo/*
                 │
    ┌────────────┴────────────┐
    │ Development (Vite)      │ Production (Netlify)
    │                         │
    │ vite.config.ts proxy:   │ netlify.toml redirects:
    │  • /api/psx/ →          │  • /api/psx/ →
    │    dps.psx.com.pk       │    /.netlify/functions/psx
    │  • /api/yahoo/ →        │  • /api/yahoo/ →
    │    (custom middleware)  │    /.netlify/functions/yahoo
    │                         │
    └────────────┬────────────┘
                 │
        ┌────────┴──────────┐
        ▼                   ▼
   ┌─────────────┐     ┌──────────────┐
   │ PSX Data    │     │ Yahoo        │
   │ Portal      │     │ Finance      │
   │ dps.psx..pk │     │ query1/2.fina
   └─────────────┘     └──────────────┘
```

---

## Component Design

### Frontend (dist/)
- React 19 SPA, pre-built by `npm run build`
- Published to Netlify CDN
- All requests proxied through same-origin API paths (no CORS)
- Asset paths are root-relative (e.g., `/assets/index-ABC.js`), not subpath-relative

### Netlify Functions (netlify/functions/)

#### `psx.mts`
- **Endpoint:** `/.netlify/functions/psx` → redirected to via `netlify.toml`
- **Request:** `/api/psx/*` (from browser) → mapped to `/.netlify/functions/psx/:splat`
- **Logic:** Validate path, enforce host-pin to `dps.psx.com.pk`, add User-Agent header, pass through
- **Response:** PSX JSON (prices, fundamentals, indices)
- **Security:** Path allowlist, hostile-encoding rejection, no logging of sensitive data

#### `yahoo.mts`
- **Endpoint:** `/.netlify/functions/yahoo` → redirected to via `netlify.toml`
- **Request:** `/api/yahoo/*` (from browser) → mapped to `/.netlify/functions/yahoo/:splat`
- **Logic:** Validate path against allowlist, handle crumb handshake (server-side), cache cookie for 30min, append crumb to downstream requests, rate-limit per IP
- **Response:** Yahoo Finance JSON (search, quote, chart, quoteSummary)
- **Security:** Path allowlist, host allowlist (query1/query2), per-IP token bucket (60 burst, 1/sec sustained), crumb/cookie never sent to browser, redacted logging

### Configuration (netlify.toml)
- **Build:** `npm run build` → `dist/`
- **Publish:** `dist/`
- **Functions:** `netlify/functions/`
- **Redirects:**
  - `/api/psx/*` → `/.netlify/functions/psx/:splat` (200)
  - `/api/yahoo/*` → `/.netlify/functions/yahoo/:splat` (200)
  - `/*` → `/index.html` (200) — SPA fallback for React Router

### Vite Config (dev-time only)
- **Base path:** `'/'` (root-relative, not subpath)
- **Dev proxy for PSX:** Simple reverse proxy to `dps.psx.com.pk` (mirrors production function logic)
- **Dev proxy for Yahoo:** Custom middleware running `_yahooProxy.js` logic in-process (same crumb handshake, rate limit, host-pin as production)

---

## Data Flow

### Happy Path: Search for "OGDC" (PSX Company)
1. User types "OGDC" in search bar → TanStack Query debounced fetch
2. Browser calls `GET /api/yahoo/v1/finance/search?q=OGDC`
3. Netlify redirects to `/.netlify/functions/yahoo?q=OGDC`
4. `yahoo.mts` validates path, performs crumb handshake (cached for 30min), appends crumb, forwards to `query1.finance.yahoo.com/v1/finance/search?q=OGDC&crumb=XXX`
5. Yahoo responds with search results (includes OGDC.KA, OGDC.N, etc.)
6. Frontend displays results with source badge "Global"
7. User clicks OGDC (local) result → navigates to `/company/OGDC`
8. Browser calls `GET /api/psx/company/OGDC`
9. Netlify redirects to `/.netlify/functions/psx` with path `/company/OGDC`
10. `psx.mts` validates, forwards to `dps.psx.com.pk/company/OGDC`
11. PSX responds with live price, P/E, market cap, etc.
12. Frontend displays company detail page with live data

### Failure Case: PSX API Down
1. Browser calls `GET /api/psx/timeseries/eod/OGDC`
2. Netlify functions forwards to `dps.psx.com.pk/timeseries/eod/OGDC`
3. PSX server is down → 502 or timeout
4. `psx.mts` catches error, returns 502 to browser
5. Frontend's `useMarketData` hook sees 502 → displays "Couldn't load live market data" (no retry, no stale data)

---

## Security Posture

| Control | Implementation | Rationale |
|---------|-----------------|-----------|
| SSRF / Host Pin | `dps.psx.com.pk` hardcoded; Yahoo redirects through allowlist | Prevent proxying arbitrary upstream hosts |
| Path Allowlist | Predefined prefixes (v8, v7, v10, v1 for Yahoo; company, timeseries for PSX) | Prevent path traversal, restrict what endpoints are exposed |
| Rate Limit | Per-IP token bucket (60 burst, 1/sec sustained) on Yahoo; no explicit limit on PSX | Prevent abuse; PSX rate-limits itself server-side |
| Logging | Request method + path only; redact crumb, cookie, query string | Never leak secrets to logs |
| Hostile Encoding | Reject `%2f`, `%5c`, `..`, `\`, `\r`, `\n` in input | Prevent null-byte injection, path traversal encoding |
| User-Agent | Hardcoded UA string matching browser standard | Prevent IAM rules that block headless clients |
| Browser Isolation | Crumb + cookie kept server-side; never sent to browser | Prevent XSS leading to cookie theft |

---

## Deployment Target

**Netlify Free or Paid Tier**
- Serverless functions included
- Edge caching built-in (configured to 45s on proxy responses)
- Auto-HTTPS, auto-compression
- Subdomain: `something.netlify.app` (or custom domain if configured by user later)
- No additional configuration needed beyond `netlify.toml`

---

## Known Limitations

1. **Per-instance rate limiting** — Serverless instances are ephemeral and region-specific; rate-limit state is per-instance. Under extreme horizontal scaling, abuse could slip through. Acceptable for personal/educational use; document for Auditor.
2. **30-min crumb cache** — Yahoo crumb handshake is expensive; caching reduces overhead but means crumb is valid across multiple requests. Acceptable; single re-handshake on invalid crumb handles expiry.
3. **No persistent logging** — Function logs are ephemeral; no central logging service (Datadog, Sentry). For minimal-ops deployment, manual inspection of Netlify's function logs is sufficient.
4. **No uptime monitoring** — No external monitoring (Pingdom, StatusPage). Manual health check (visit the deployed URL daily) is adequate per NFR-1.

---

## Migration from GitHub Pages

Market Compass was initially built for GitHub Pages (hence the `/HFJ-Cognitive-Lab/market-compass/` base-path bug and Vercel-style function signatures). Netlify deployment required:

1. **Remove subpath routing** → `base: '/'`
2. **Rewrite serverless functions** → Netlify v2 `Request`→`Response` signature
3. **Add Netlify configuration** → `netlify.toml` with build + redirect rules
4. **Preserve security logic** → No simplification; all hardening copied exactly from original

---

## Next Steps

**Phase 3 (Build):** Verify functions work by exercising them locally with mock requests. Phase 4 (QA) will run the smoke test suite and manual API checks.
