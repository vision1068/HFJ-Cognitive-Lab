import type { Company, PricePoint } from "@/types";
import { psxTickers, psxTickerBySymbol, type PsxTicker } from "@/data/psxTickers";
import { fetchPsxEod, fetchPsxCompany, type EodSeries, type PsxFundamentals } from "./psx";

// Helper: Extract 52-week high/low from historical data
function week52(history: PricePoint[]): { high: number | null; low: number | null } {
  const slice = history.slice(-252); // ~1 year of trading days
  if (slice.length === 0) return { high: null, low: null };
  return {
    high: Math.max(...slice.map((p) => p.close)),
    low: Math.min(...slice.map((p) => p.close)),
  };
}


/**
 * Build Company object from PSX fundamentals and historical data
 * Maps ALL available PSX Data Portal fields to Company type
 */
function buildCompanyFromPsx(meta: PsxTicker, eod: EodSeries, fundamentals: PsxFundamentals): Company {
  const { high, low } = week52(eod.history);
  const price = fundamentals.price ?? eod.latest;
  const previousClose = fundamentals.ldcp ?? eod.previous;
  const changeAbs = price - previousClose;
  const changePercent = previousClose ? (changeAbs / previousClose) * 100 : 0;

  return {
    // Identifier
    ticker: meta.ticker,
    psxSymbol: meta.psxSymbol,
    name: fundamentals.name || meta.name,
    logoInitials: meta.logoInitials,
    logoColor: meta.logoColor,

    // Classification
    exchange: "PSX",
    country: "Pakistan",
    isPsx: true,
    sector: fundamentals.psxSector || meta.sector,
    industry: meta.industry,
    currency: "PKR",

    // Company Info
    description: meta.description,
    website: meta.website,
    ceo: meta.ceo,

    // Price Data (From PSX Fundamentals + EOD)
    price,
    previousClose,
    changeAbs,
    changePercent,

    // Intraday Price (From PSX - EOD only)
    dayOpen: fundamentals.open,
    dayHigh: fundamentals.high,
    dayLow: fundamentals.low,
    volume: fundamentals.volume,

    // 52-Week Range (From PSX Historical)
    week52High: high,
    week52Low: low,

    // Fundamentals (From PSX)
    marketCap: fundamentals.marketCap,
    peRatio: fundamentals.peRatio,
    eps: fundamentals.eps,
    sharesOutstanding: fundamentals.sharesOutstanding,
    psxSector: fundamentals.psxSector,

    // Price History (From PSX EOD)
    sparkline: eod.history.slice(-30).map((p) => p.close),
    priceHistory: eod.history,

    // Data Source
    priceSource: "PSX Data Portal",
    fundamentalsSource: "PSX Data Portal",
    lastUpdated: new Date().toISOString(),
    asOf: price ? new Date().toISOString() : null,

    // Type Info
    quoteType: "EQUITY",
    fundamentalsAvailable: !!(fundamentals.peRatio || fundamentals.eps || fundamentals.marketCap),
    statementsSource: null, // PSX has no financial statements API
  };
}

/**
 * Main Dashboard List - All active PSX companies with fundamentals
 * Data source: PSX Data Portal only (dps.psx.com.pk)
 * Updates: End-of-day (after market closes at 3 PM)
 */
export async function fetchCompanyList(): Promise<Company[]> {
  const companies: Company[] = [];

  // Fetch PSX data for all curated top-10 companies
  const curatedResults = await Promise.allSettled(
    psxTickers.map(async (meta) => {
      const [eod, fundamentals] = await Promise.all([fetchPsxEod(meta.psxSymbol), fetchPsxCompany(meta.psxSymbol)]);

      return buildCompanyFromPsx(meta, eod, fundamentals);
    })
  );

  // Collect successful results
  curatedResults.forEach((result) => {
    if (result.status === "fulfilled" && result.value) {
      companies.push(result.value);
    }
  });

  if (companies.length === 0) {
    throw new Error("Unable to load PSX company data from dps.psx.com.pk");
  }

  return companies;
}

/**
 * Detailed List - Full fundamentals + history for screener/analysis
 * All data from PSX Data Portal only
 */
export async function fetchCompanyListDetailed(): Promise<Company[]> {
  const results = await Promise.allSettled(
    psxTickers.map(async (meta) => {
      const [eod, fundamentals] = await Promise.all([
        fetchPsxEod(meta.psxSymbol),
        fetchPsxCompany(meta.psxSymbol),
      ]);

      return buildCompanyFromPsx(meta, eod, fundamentals);
    })
  );

  const companies = results
    .filter((r): r is PromiseFulfilledResult<Company> => r.status === "fulfilled")
    .map((r) => r.value);

  if (companies.length === 0) {
    throw new Error("Unable to load PSX company fundamentals from dps.psx.com.pk");
  }

  return companies;
}

/**
 * Company Detail - Full details for any PSX symbol
 * Data from PSX Data Portal only
 */
export async function fetchCompanyDetail(ticker: string): Promise<Company> {
  // Check if it's a curated top-10
  const meta = psxTickerBySymbol[ticker];
  if (meta) {
    const [eod, fundamentals] = await Promise.all([
      fetchPsxEod(meta.psxSymbol),
      fetchPsxCompany(meta.psxSymbol),
    ]);

    return buildCompanyFromPsx(meta, eod, fundamentals);
  }

  // For non-curated symbols, try PSX portal directly
  const [eod, fundamentals] = await Promise.all([
    fetchPsxEod(ticker),
    fetchPsxCompany(ticker),
  ]);

  // Build company from PSX fundamentals only
  const pseudoMeta: PsxTicker = {
    ticker,
    psxSymbol: ticker,
    name: fundamentals.name || ticker,
    logoInitials: ticker.slice(0, 2).toUpperCase(),
    logoColor: `hsl(${(ticker.charCodeAt(0) * 31) % 360}, 55%, 45%)`,
    sector: (fundamentals.psxSector || "Fertilizer") as any,
    industry: "PSX Listed",
    description: "",
    website: "",
    ceo: "",
  };

  return buildCompanyFromPsx(pseudoMeta, eod, fundamentals);
}
