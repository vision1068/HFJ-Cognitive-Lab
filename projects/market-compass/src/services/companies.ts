import type { BatchQuote, Company, PricePoint, UniverseSymbol } from "@/types";
import { psxTickers, psxTickerBySymbol, type PsxTicker } from "@/data/psxTickers";
import { fetchPsxEod, fetchPsxCompany, type EodSeries, type PsxFundamentals } from "./psx";
import { fetchUniverse, getFallbackUniverse } from "./universe";
import { fetchBatchQuotes } from "./yahooQuote";
import { fetchGlobalCandles } from "./globalQuote";

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
    isPsx: true,
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

    asOf: eod.history[eod.history.length - 1]?.date
      ? new Date(eod.history[eod.history.length - 1].date).toISOString()
      : null,
    quoteType: null,
    fundamentalsAvailable: false,
    statementsSource: null, // PSX has no free statements source
  };
}

// List view over the FULL PSX universe. One Yahoo v7 batch request per ≤50
// symbols (NFR-2) supplies live price + change + fundamentals for every symbol;
// the curated top-10 additionally get real sparklines/history from the PSX
// portal, and any curated symbol Yahoo dropped falls back to the PSX EOD feed.
// Reconciliation: symbols Yahoo returns with no usable price are omitted from
// the list rather than shown with a fabricated zero.
export async function fetchCompanyList(): Promise<Company[]> {
  const universe = await fetchUniverse();
  const symbols = universe.map((u) => `${u.symbol}.KA`);
  const { quotes } = await fetchBatchQuotes(symbols);

  const quoteBySymbol = new Map<string, BatchQuote>();
  for (const q of quotes) quoteBySymbol.set(q.symbol.toUpperCase(), q);

  const companies = new Map<string, Company>();
  for (const u of universe) {
    const q = quoteBySymbol.get(`${u.symbol}.KA`.toUpperCase());
    if (!q || q.price == null) continue; // missing/pricenull -> per-symbol fallback (curated) or omit
    companies.set(u.symbol, companyFromQuote(u, q, psxTickerBySymbol[u.symbol]));
  }

  // Bounded PSX-portal fallback + sparklines for the curated top-10.
  await enrichCuratedFromPsx(companies).catch(() => undefined);

  const list = Array.from(companies.values());
  if (list.length === 0) throw new Error("Unable to load any PSX company data");
  return list;
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

// Detail view: resolves ANY symbol so every list row / search hit opens a real
// detail page — curated PSX, non-curated PSX (full universe), or a global symbol.
// Only a symbol that resolves nowhere throws, so ErrorState shows honestly.
export async function fetchCompanyDetail(ticker: string): Promise<Company> {
  const meta = psxTickerBySymbol[ticker];
  if (meta) return fetchCuratedPsxDetail(meta);

  // Non-curated PSX symbol (Karachi feed) first, then a global symbol.
  const psx = await resolvePsxDetail(ticker).catch(() => null);
  if (psx) return psx;

  const global = await resolveGlobalDetail(ticker).catch(() => null);
  if (global) return global;

  throw new Error(`Unknown ticker: ${ticker}`);
}

// Curated top-10: PSX portal EOD + HTML fundamentals (unchanged behavior).
async function fetchCuratedPsxDetail(meta: PsxTicker): Promise<Company> {
  const [eod, fundamentals] = await Promise.all([
    fetchPsxEod(meta.psxSymbol),
    fetchPsxCompany(meta.psxSymbol).catch(() => null),
  ]);
  const company = baseCompany(meta, eod);
  if (fundamentals) applyPortalFundamentals(company, fundamentals);
  return company;
}

// Applies PSX-portal-page fundamentals onto a Company (shared by the curated
// path and the non-curated PSX portal fallback).
function applyPortalFundamentals(company: Company, f: PsxFundamentals): void {
  if (f.price != null) company.price = f.price;
  if (f.ldcp != null) {
    company.previousClose = f.ldcp;
    company.changeAbs = company.price - f.ldcp;
    company.changePercent = f.ldcp ? (company.changeAbs / f.ldcp) * 100 : 0;
  }
  company.dayOpen = f.open;
  company.dayHigh = f.high;
  company.dayLow = f.low;
  if (f.volume != null) company.volume = f.volume;
  company.marketCap = f.marketCap;
  company.peRatio = f.peRatio;
  company.eps = f.eps;
  company.sharesOutstanding = f.sharesOutstanding;
  company.psxSector = f.psxSector;
  company.fundamentalsSource = "PSX Data Portal";
  company.fundamentalsAvailable = f.peRatio != null || f.eps != null || f.marketCap != null;
}

interface AssembleInput {
  ticker: string;
  psxSymbol: string;
  name: string;
  sector: string;
  industry: string;
  isPsx: boolean;
  exchange: string;
  country: string;
  currency: string;
  q: BatchQuote | null;
  history: PricePoint[];
  priceSource: string;
}

// Shared Company builder for non-curated PSX + global detail. Price/change come
// from the Yahoo quote when present, else are derived from the candle history;
// nothing is fabricated (all-null inputs yield null/0 fields, not fake values).
function assembleFromQuote(inp: AssembleInput): Company {
  const { q, history } = inp;
  const lastClose = history.length ? history[history.length - 1].close : null;
  const prevClose = history.length > 1 ? history[history.length - 2].close : null;
  const price = q?.price ?? lastClose ?? 0;
  const previousClose = q?.price != null && q?.change != null ? q.price - q.change : prevClose ?? price;
  const changeAbs = q?.change ?? (lastClose != null && prevClose != null ? lastClose - prevClose : 0);
  const changePercent = q?.changePercent ?? (previousClose ? (changeAbs / previousClose) * 100 : 0);
  const wk = week52(history);
  const lastDate = history.length ? history[history.length - 1].date : null;
  return {
    ticker: inp.ticker,
    psxSymbol: inp.psxSymbol,
    name: inp.name,
    logoInitials: inp.ticker.replace(/[^A-Z0-9]/gi, "").slice(0, 2).toUpperCase() || "?",
    logoColor: logoColorFor(inp.ticker),
    exchange: inp.exchange,
    country: inp.country,
    isPsx: inp.isPsx,
    sector: inp.sector,
    industry: inp.industry,
    currency: inp.currency,
    description: "",
    website: "",
    ceo: "",

    price,
    previousClose,
    changeAbs,
    changePercent,
    dayOpen: null,
    dayHigh: null,
    dayLow: null,
    volume: q?.volume ?? (history.length ? history[history.length - 1].volume : null),
    week52High: q?.week52High ?? wk.high,
    week52Low: q?.week52Low ?? wk.low,

    marketCap: q?.marketCap ?? null,
    peRatio: q?.trailingPE ?? null,
    eps: q?.eps ?? null,
    sharesOutstanding: null,
    psxSector: inp.isPsx ? inp.sector : null,

    sparkline: history.slice(-30).map((p) => p.close),
    priceHistory: history,

    priceSource: inp.priceSource,
    fundamentalsSource: q?.fundamentalsAvailable ? "Yahoo Finance" : null,
    lastUpdated: new Date().toISOString(),

    asOf: q?.asOf ?? (lastDate ? new Date(lastDate).toISOString() : null),
    quoteType: q?.quoteType ?? null,
    fundamentalsAvailable: q?.fundamentalsAvailable ?? false,
    // PSX has no free statements source; global equities are queried from Yahoo.
    statementsSource: inp.isPsx ? null : q?.fundamentalsAvailable ? "Yahoo Finance" : null,
  };
}

// Non-curated PSX symbol: Yahoo v7 quote for `${symbol}.KA` (Karachi feed) +
// v8 chart for history/sparkline, falling back to the PSX portal when Yahoo
// lacks it. Returns null when the symbol is not a resolvable PSX security.
async function resolvePsxDetail(symbol: string): Promise<Company | null> {
  const yh = `${symbol}.KA`;
  const uni = getFallbackUniverse().find((u) => u.symbol.toUpperCase() === symbol.toUpperCase());

  const { quotes } = await fetchBatchQuotes([yh]).catch(() => ({ quotes: [] as BatchQuote[], missing: [] as string[] }));
  const q = quotes[0] ?? null;

  if (q && q.price != null && q.exchange === "KAR") {
    let history: PricePoint[] = await fetchGlobalCandles(yh, "1d", "1y").catch(() => [] as PricePoint[]);
    if (history.length === 0) {
      const eod = await fetchPsxEod(symbol).catch(() => null);
      if (eod) history = eod.history;
    }
    return assembleFromQuote({
      ticker: symbol,
      psxSymbol: symbol,
      name: uni?.name ?? q.name ?? symbol,
      sector: uni?.sector ?? "UNCLASSIFIED",
      industry: uni?.sector ?? "UNCLASSIFIED",
      isPsx: true,
      exchange: "PSX",
      country: "Pakistan",
      currency: "PKR",
      q,
      history,
      priceSource: "Yahoo Finance",
    });
  }

  // Yahoo has no Karachi quote — try the PSX portal, but only for symbols we
  // actually believe are PSX-listed (avoids a wasted portal call for globals).
  if (!uni) return null;
  const eod = await fetchPsxEod(symbol).catch(() => null);
  if (!eod) return null;
  const fundamentals = await fetchPsxCompany(symbol).catch(() => null);
  const company = assembleFromQuote({
    ticker: symbol,
    psxSymbol: symbol,
    name: uni.name ?? fundamentals?.name ?? symbol,
    sector: fundamentals?.psxSector ?? uni.sector ?? "UNCLASSIFIED",
    industry: uni.sector ?? "UNCLASSIFIED",
    isPsx: true,
    exchange: "PSX",
    country: "Pakistan",
    currency: "PKR",
    q: null,
    history: eod.history,
    priceSource: "PSX Data Portal",
  });
  if (fundamentals) applyPortalFundamentals(company, fundamentals);
  return company;
}

// Global symbol (e.g. a Yahoo search hit): v7 quote + v8 chart. Returns null when
// neither a quote nor candles exist for the symbol.
async function resolveGlobalDetail(symbol: string): Promise<Company | null> {
  const { quotes } = await fetchBatchQuotes([symbol]).catch(() => ({ quotes: [] as BatchQuote[], missing: [] as string[] }));
  const q = quotes[0] ?? null;
  const history: PricePoint[] = await fetchGlobalCandles(symbol, "1d", "1y").catch(() => [] as PricePoint[]);
  if ((!q || q.price == null) && history.length === 0) return null;
  return assembleFromQuote({
    ticker: symbol,
    psxSymbol: symbol,
    name: q?.name ?? symbol,
    sector: "",
    industry: "",
    isPsx: false,
    exchange: q?.exchange ?? "Global",
    country: "",
    currency: q?.currency ?? "USD",
    q,
    history,
    priceSource: "Yahoo Finance",
  });
}

// ---------------------------------------------------------------------------
// FR-1 / FR-2 / FR-3: full-universe list built from Yahoo v7 batch quotes.
// ---------------------------------------------------------------------------

// Deterministic logo color for universe symbols without curated branding.
function logoColorFor(symbol: string): string {
  let hash = 0;
  for (let i = 0; i < symbol.length; i++) hash = (hash * 31 + symbol.charCodeAt(i)) >>> 0;
  const hue = hash % 360;
  return `hsl(${hue}, 55%, 45%)`;
}

// Builds a Company from a universe entry + its Yahoo batch quote. Curated
// metadata (logo/description/ceo/website) is used when the symbol is one of the
// top-10; otherwise minimal universe metadata is used and sparkline/history stay
// empty (the list view does not need per-symbol history for the whole universe).
function companyFromQuote(u: UniverseSymbol, q: BatchQuote, meta: PsxTicker | undefined): Company {
  const price = q.price ?? 0;
  const change = q.change ?? 0;
  const previousClose = q.price != null && q.change != null ? q.price - q.change : price;
  return {
    ticker: u.symbol,
    psxSymbol: u.symbol,
    name: meta?.name ?? q.name ?? u.name,
    logoInitials: meta?.logoInitials ?? u.symbol.slice(0, 2).toUpperCase(),
    logoColor: meta?.logoColor ?? logoColorFor(u.symbol),
    exchange: "PSX",
    country: "Pakistan",
    isPsx: true,
    sector: meta?.sector ?? u.sector,
    industry: meta?.industry ?? u.sector,
    currency: "PKR",
    description: meta?.description ?? "",
    website: meta?.website ?? "",
    ceo: meta?.ceo ?? "",

    price,
    previousClose,
    changeAbs: change,
    changePercent: q.changePercent ?? 0,
    dayOpen: null,
    dayHigh: null,
    dayLow: null,
    volume: q.volume,
    week52High: q.week52High,
    week52Low: q.week52Low,

    marketCap: q.marketCap,
    peRatio: q.trailingPE,
    eps: q.eps,
    sharesOutstanding: null,
    psxSector: u.sector,

    sparkline: [],
    priceHistory: [],

    priceSource: "Yahoo Finance",
    fundamentalsSource: q.fundamentalsAvailable ? "Yahoo Finance" : null,
    lastUpdated: new Date().toISOString(),

    asOf: q.asOf,
    quoteType: q.quoteType,
    fundamentalsAvailable: q.fundamentalsAvailable,
    statementsSource: null,
  };
}

// Bounded PSX-portal enrichment for the curated top-10: attaches real sparkline +
// priceHistory (Yahoo v7 batch carries no history) and fills price as a per-symbol
// fallback when the Yahoo quote was missing. Concurrency is capped at 4.
async function enrichCuratedFromPsx(companies: Map<string, Company>): Promise<void> {
  const LIMIT = 4;
  const queue = [...psxTickers];
  async function worker() {
    while (queue.length > 0) {
      const meta = queue.shift();
      if (!meta) break;
      const eod = await fetchPsxEod(meta.psxSymbol).catch(() => null);
      if (!eod) continue;
      const existing = companies.get(meta.ticker);
      if (existing) {
        existing.sparkline = eod.history.slice(-30).map((p) => p.close);
        existing.priceHistory = eod.history;
        if (existing.price === 0) {
          existing.price = eod.latest;
          existing.previousClose = eod.previous;
          existing.changeAbs = eod.latest - eod.previous;
          existing.changePercent = eod.previous ? ((eod.latest - eod.previous) / eod.previous) * 100 : 0;
        }
      } else {
        // Curated company entirely missing from Yahoo — build it from PSX EOD.
        companies.set(meta.ticker, baseCompany(meta, eod));
      }
    }
  }
  await Promise.all(Array.from({ length: LIMIT }, worker));
}
