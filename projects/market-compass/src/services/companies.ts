import type { Company, PricePoint } from "@/types";
import { psxTickers, psxTickerBySymbol, type PsxTicker } from "@/data/psxTickers";
import { fetchPsxEod, fetchPsxCompany, type EodSeries } from "./psx";

function week52(history: PricePoint[]): { high: number | null; low: number | null } {
  const slice = history.slice(-252);
  if (slice.length === 0) return { high: null, low: null };
  return {
    high: Math.max(...slice.map((p) => p.close)),
    low: Math.min(...slice.map((p) => p.close)),
  };
}

function baseCompany(meta: PsxTicker, eod: EodSeries): Company {
  const changeAbs = eod.latest - eod.previous;
  const changePercent = eod.previous ? (changeAbs / eod.previous) * 100 : 0;
  const { high, low } = week52(eod.history);
  return {
    ticker: meta.ticker,
    psxSymbol: meta.psxSymbol,
    name: meta.name,
    logoInitials: meta.logoInitials,
    logoColor: meta.logoColor,
    exchange: "PSX",
    country: "Pakistan",
    sector: meta.sector,
    industry: meta.industry,
    currency: "PKR",
    description: meta.description,
    website: meta.website,
    ceo: meta.ceo,

    price: eod.latest,
    previousClose: eod.previous,
    changeAbs,
    changePercent,
    dayOpen: null,
    dayHigh: null,
    dayLow: null,
    volume: eod.history[eod.history.length - 1]?.volume ?? null,
    week52High: high,
    week52Low: low,

    marketCap: null,
    peRatio: null,
    eps: null,
    sharesOutstanding: null,
    psxSector: null,

    sparkline: eod.history.slice(-30).map((p) => p.close),
    priceHistory: eod.history,

    priceSource: "PSX Data Portal",
    fundamentalsSource: null,
    lastUpdated: new Date().toISOString(),
  };
}

// A PSX symbol's EOD feed can freeze (e.g. after a corporate action reassigns
// the symbol) while the company still trades live. Treat anything older than
// 4 calendar days as stale rather than silently showing an outdated price.
const STALE_AFTER_MS = 4 * 24 * 60 * 60 * 1000;

function isStale(eod: EodSeries): boolean {
  const latestDate = eod.history[eod.history.length - 1]?.date;
  if (!latestDate) return true;
  return Date.now() - new Date(latestDate).getTime() > STALE_AFTER_MS;
}

// List view: price, change, sparkline and history for every covered PSX company.
// Fundamentals (P/E, market cap, EPS) are left null here and loaded on the detail
// page — keeps the list to one lightweight request per ticker, except when the
// EOD feed is stale, in which case we fall back to the live quote for price only.
export async function fetchCompanyList(): Promise<Company[]> {
  const results = await Promise.allSettled(
    psxTickers.map(async (meta) => {
      const eod = await fetchPsxEod(meta.psxSymbol);
      const company = baseCompany(meta, eod);
      if (isStale(eod)) {
        const quote = await fetchPsxCompany(meta.psxSymbol).catch(() => null);
        if (!quote?.price) throw new Error(`Stale PSX data for ${meta.psxSymbol} and no live quote available`);
        company.price = quote.price;
        if (quote.ldcp != null) {
          company.previousClose = quote.ldcp;
          company.changeAbs = company.price - quote.ldcp;
          company.changePercent = quote.ldcp ? (company.changeAbs / quote.ldcp) * 100 : 0;
        }
        if (quote.volume != null) company.volume = quote.volume;
      }
      return company;
    })
  );
  const companies = results
    .filter((r): r is PromiseFulfilledResult<Company> => r.status === "fulfilled")
    .map((r) => r.value);
  if (companies.length === 0) throw new Error("Unable to load any PSX company data");
  return companies;
}

// Detailed list: every covered company WITH parsed fundamentals (P/E, market cap,
// EPS). Used by the screener. One EOD + one company-page request per ticker.
export async function fetchCompanyListDetailed(): Promise<Company[]> {
  const results = await Promise.allSettled(psxTickers.map((meta) => fetchCompanyDetail(meta.ticker)));
  const companies = results
    .filter((r): r is PromiseFulfilledResult<Company> => r.status === "fulfilled")
    .map((r) => r.value);
  if (companies.length === 0) throw new Error("Unable to load PSX company fundamentals");
  return companies;
}

// Detail view: EOD series + parsed fundamentals from the PSX company page.
export async function fetchCompanyDetail(ticker: string): Promise<Company> {
  const meta = psxTickerBySymbol[ticker];
  if (!meta) throw new Error(`Unknown ticker: ${ticker}`);

  const [eod, fundamentals] = await Promise.all([
    fetchPsxEod(meta.psxSymbol),
    fetchPsxCompany(meta.psxSymbol).catch(() => null),
  ]);

  const company = baseCompany(meta, eod);

  if (fundamentals) {
    // Prefer the live quote price/prev-close from the company page when present.
    if (fundamentals.price != null) company.price = fundamentals.price;
    if (fundamentals.ldcp != null) {
      company.previousClose = fundamentals.ldcp;
      company.changeAbs = company.price - fundamentals.ldcp;
      company.changePercent = fundamentals.ldcp ? (company.changeAbs / fundamentals.ldcp) * 100 : 0;
    }
    company.dayOpen = fundamentals.open;
    company.dayHigh = fundamentals.high;
    company.dayLow = fundamentals.low;
    if (fundamentals.volume != null) company.volume = fundamentals.volume;
    company.marketCap = fundamentals.marketCap;
    company.peRatio = fundamentals.peRatio;
    company.eps = fundamentals.eps;
    company.sharesOutstanding = fundamentals.sharesOutstanding;
    company.psxSector = fundamentals.psxSector;
    company.fundamentalsSource = "PSX Data Portal";
  }

  return company;
}
