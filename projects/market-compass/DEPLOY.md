# Market Compass — Deployment Runbook

This document covers manual deployment to Netlify. No CI/CD automation is configured (NFR-1: minimal ops, on-demand manual deployment).

---

## Prerequisites

- Node.js 18+ installed locally
- Netlify account (free tier sufficient)
- Netlify CLI installed: `npm install -g netlify-cli`

---

## Deploy via Netlify CLI

### First-Time Setup

1. **Build and test locally** (verify no errors):
   ```bash
   cd projects/market-compass
   npm install
   npm run build
   npm test
   ```

2. **Authenticate with Netlify**:
   ```bash
   netlify login
   ```
   This opens a browser for OAuth; approve and close when done.

3. **Create a new site on Netlify** (first deploy only):
   ```bash
   netlify deploy --prod
   ```
   When prompted:
   - **Directory to deploy:** Press Enter (defaults to `dist`, configured in `netlify.toml`)
   - **Site name:** Choose a unique subdomain (e.g., `market-compass-research`). The live URL will be `https://market-compass-research.netlify.app`

4. **Test the live site**:
   - Open the provided URL in your browser
   - Verify homepage loads, search works, company pages display live data
   - Test API endpoints: navigate to a company (should show live PSX data)

### Subsequent Deploys

After the first setup, redeploy with:
```bash
cd projects/market-compass
npm run build
netlify deploy --prod
```

---

## Deploy via Netlify Dashboard (Git-Linked)

If you prefer git-based auto-deploys (optional):

1. **Push this repo to GitHub** (if not already)
2. **Visit [Netlify.com](https://netlify.com)** and sign in
3. **New site → Import an existing project**
4. **Select your repo**, authorize Netlify
5. **Build settings:**
   - Build command: `npm run build`
   - Publish directory: `dist`
   - Functions directory: `netlify/functions`
   (These should auto-fill from `netlify.toml`)
6. **Deploy Site**

Now every push to the main branch auto-deploys. Subsequent deploys are automatic.

---

## Rollback Plan

Since this is a minimal-ops deployment (no server-side state, no database):

1. **Identify the broken deploy** — note the URL or deployment ID from Netlify dashboard
2. **Revert to the previous commit**:
   ```bash
   git revert HEAD
   git push
   ```
3. **Redeploy** (manual CLI or auto via git):
   ```bash
   npm run build && netlify deploy --prod
   ```

**RTO/RPO:** <5 minutes (rebuild + redeploy). No data loss (app is stateless; only browser localStorage is affected).

---

## Troubleshooting

### "npm run build" fails

- Check TypeScript errors: `npm run build` runs `tsc -b` first
- Check Tailwind CSS: ensure all class names are in templates (not generated)
- Review `dist/index.html` asset paths — they should be root-relative (e.g., `/assets/...`), not subpath-relative

### Site loads but CSS/JS are 404

- Verify `vite.config.ts` has `base: '/'` (not a subpath)
- Rebuild and redeploy: `npm run build && netlify deploy --prod`

### API calls return 502 (gateway error)

- Check Netlify Functions logs: `netlify functions:list` and `netlify logs`
- Verify `netlify.toml` redirects are in place (check `/api/psx/` and `/api/yahoo/`)
- Ensure PSX Data Portal (`dps.psx.com.pk`) and Yahoo Finance endpoints are reachable

### "Rate limit exceeded" from Yahoo proxy

- This is expected under high load (60 burst, 1 req/s sustained per IP)
- Recommend spreading requests across multiple user sessions
- For persistent high load, migrate to a Yahoo Finance API subscription

---

## Monitoring & Alerts (Not Configured)

Per NFR-1 (minimal ops), no uptime monitoring or alerting is configured. If issues arise:

1. **Manual status check**: Open the deployed URL in a browser every day
2. **Netlify dashboard**: Log in to see if any recent deploys failed
3. **Netlify build logs**: Review logs for errors in the "Deploys" tab

For production-grade monitoring, consider adding (not required for launch):
- Sentry.io for JavaScript errors
- Netlify Analytics for performance
- Uptime monitoring (Pingdom, StatusPage.io)

---

## Environment Variables

No secrets are needed for this deployment (all APIs are public or server-side proxied):
- `.env` is not required
- No API keys are stored in the repo or deployed code

---

## Notes

- **Data sources:** PSX Data Portal (`dps.psx.com.pk`) and Yahoo Finance API. If either is unavailable, the app shows "data not available" gracefully.
- **Deployment time:** ~1–2 minutes (Netlify Functions build + upload)
- **Built-in caching:** Netlify edge caching (45s) is configured on proxy responses; browser caching is handled by React Query (60s)

---

## Support

For issues:
- **Netlify:** [docs.netlify.com](https://docs.netlify.com)
- **PSX Data Portal:** Check [dps.psx.com.pk](https://dps.psx.com.pk) status
- **Yahoo Finance API:** Known to be rate-limited and undocumented; fallback to "data not available" is graceful
