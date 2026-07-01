# Market Compass

A premium, personal stock-research and investment decision-support platform covering the Pakistan Stock Exchange (PSX) and major international markets (NYSE, NASDAQ, LSE, TSE, and European exchanges).

> Market Compass provides market research and educational insights only. It is not financial advice. Always conduct your own research or consult a licensed financial adviser before investing.

## Stack

- React 19 + TypeScript + Vite
- Tailwind CSS v4
- React Router
- TanStack Query
- Zustand (with localStorage persistence) for watchlists, portfolio, alerts, and preferences
- Recharts for price, financial, and allocation charts
- lucide-react icons

## Getting started

```bash
npm install
npm run dev
```

## Project structure

```
src/
  components/      Layout shell, reusable UI primitives, company-page widgets
  data/             Mock data generation (seed companies + deterministic generator)
  lib/              Formatting, currency conversion, signal/scoring helpers
  pages/            One file per route
  store/            Zustand app store
  types/            Shared domain types (Company, FinancialPeriod, ScoreBreakdown, ...)
```

## Mock data & live data adapters

All market data is currently generated deterministically from a small set of seed companies (`src/data/seedCompanies.ts` + `src/data/generateCompany.ts`) so the app looks fully populated without any API keys. Data shapes mirror what a real provider integration would return, so the mock layer in `src/data/` can be swapped for live adapters (Alpha Vantage, Finnhub, Financial Modeling Prep, Polygon.io, Twelve Data, PSX feeds, news/economic-calendar APIs) by implementing the same `Company`/`MarketIndex`/`NewsItem` shapes from `src/types/index.ts` behind a fetch layer, without touching page components.

## Notes

- Portfolio values are converted into a selectable base currency using illustrative static FX rates (`src/lib/currency.ts`) — replace with a live FX feed for production use.
- The platform is not connected to any brokerage; portfolio holdings are entered manually.
