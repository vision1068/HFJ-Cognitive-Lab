# Market Compass

A premium, personal stock-research and decision-support platform for the **Pakistan Stock Exchange (PSX)**. All market data shown is **real and live** — sourced from the PSX Data Portal and Yahoo Finance — and auto-refreshes every 60 seconds. There is no mock/generated market data.

> Market Compass provides market research and educational insights only. It is not financial advice. Always conduct your own research or consult a licensed financial adviser before investing.

## Stack

- React 19 + TypeScript + Vite
- Tailwind CSS v4
- React Router
- TanStack Query (live data fetching, caching, 60s refresh)
- Zustand (localStorage persistence) for watchlists, portfolio, and alerts
- Recharts for price and allocation charts
- lucide-react icons

## Real data sources

| Data | Source | Endpoint (via proxy) |
| --- | --- | --- |
| Stock prices & price history | PSX Data Portal | `/api/psx/timeseries/eod/<SYMBOL>` |
| Fundamentals (P/E, EPS, market cap, shares, sector, day OHLC) | PSX Data Portal (company page) | `/api/psx/company/<SYMBOL>` |
| Indices (KSE-100, KMI-30) | PSX Data Portal | `/api/psx/timeseries/eod/{KSE100,KMI30}` |
| USD/PKR exchange rate | Yahoo Finance | `/api/yahoo/v8/finance/chart/USDPKR=X` |

Covered companies (PSX blue chips): ENGRO, HBL, MCB, MEBL, OGDC, PPL, LUCK, FFC, SYS, HUBC — defined in `src/data/psxTickers.ts` (descriptive metadata only; every price/valuation is fetched live).

### CORS / proxy

The PSX Data Portal and Yahoo Finance block direct cross-origin calls from the browser, so all requests are routed through same-origin `/api/*` paths:

- **Development:** Vite dev-server proxy (`vite.config.ts` → `server.proxy`) forwards `/api/psx/*` and `/api/yahoo/*` upstream with a browser User-Agent.
- **Production:** serverless functions in `api/psx/[...path].js` and `api/yahoo/[...path].js` (Vercel / Netlify Node runtime) mirror the dev proxy and add short-lived edge caching. Deploy to a host that runs `/api` functions — a purely static host (e.g. plain GitHub Pages) cannot proxy these requests.

## What is "data not available"

Per the project requirement, anything that no free data source can provide is shown as an explicit **"data not available"** state — never fabricated. This includes:

- Financial statements (revenue / net profit / cash flow / ROE by period)
- AI signal scores, financial-health breakdowns, fair-value & growth/risk ratings
- Per-company news & sentiment
- Economic calendar
- Dividend yield / estimated dividend income

Trailing EPS, P/E, and market cap **are** shown where PSX reports them.

## Getting started

```bash
npm install
npm run dev      # http://localhost:5173 (dev proxy handles CORS)
npm run build    # tsc -b && vite build
npm run lint     # oxlint
```

## Project structure

```
api/              Serverless proxy functions (production CORS proxy)
src/
  components/     Layout shell, reusable UI primitives, price chart, data-state helpers
  data/           psxTickers.ts — static company metadata registry (no market data)
  services/       Live data adapters: psx.ts, yahoo.ts, companies.ts, http.ts
  hooks/          useMarketData.ts — TanStack Query hooks (60s refresh)
  lib/            Formatting helpers
  pages/          One file per route
  store/          Zustand app store (watchlists, portfolio, alerts)
  types/          Shared domain types (PSX-only)
```

## Notes

- Portfolio holdings are entered manually in PKR and valued at live PSX prices. Not connected to any brokerage.
- PSX unofficial endpoints and Yahoo Finance are undocumented and rate-limited; the app degrades to loading/error/"data not available" states rather than showing stale or fake values.
