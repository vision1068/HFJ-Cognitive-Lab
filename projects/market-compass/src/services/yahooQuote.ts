import type { BatchQuote, BatchQuoteResult } from "@/types";
import { YAHOO_BASE, fetchJson } from "./http";

// FR-2 / FR-3: Yahoo v7 batch quote. Up to ~50 symbols per call; each row carries
// price, change, trailingPE, eps, marketCap, 52-week range, quoteType and currency.
// The crumb/cookie handshake is handled entirely server-side by the /api/yahoo
// proxy — the browser only ever calls the same-origin path below.

const MAX_SYMBOLS_PER_CALL = 50;
const MAX_CONCURRENT_CALLS = 4; // NFR: cap simultaneous upstream calls

// Runs async `fn` over `items` with at most `limit` in flight, preserving order.
async function mapLimit<T, R>(items: T[], limit: number, fn: (item: T) => Promise<R>): Promise<R[]> {
  const out: R[] = new Array(items.length);
  let next = 0;
  async function worker() {
    while (next < items.length) {
      const i = next++;
      out[i] = await fn(items[i]);
    }
  }
  const workers = Array.from({ length: Math.min(limit, items.length) }, worker);
  await Promise.all(workers);
  return out;
}

interface YahooV7Row {
  symbol?: string;
  shortName?: string;
  longName?: string;
  regularMarketPrice?: number;
  regularMarketChange?: number;
  regularMarketChangePercent?: number;
  trailingPE?: number;
  epsTrailingTwelveMonths?: number;
  marketCap?: number;
  regularMarketVolume?: number;
  fiftyTwoWeekHigh?: number;
  fiftyTwoWeekLow?: number;
  quoteType?: string;
  currency?: string;
  exchange?: string;
  market?: string;
  regularMarketTime?: number; // unix seconds
}

interface YahooV7Response {
  quoteResponse: { result: YahooV7Row[] | null; error: unknown };
}

function num(v: number | undefined): number | null {
  return typeof v === "number" && Number.isFinite(v) ? v : null;
}

/**
 * Decides whether a row's FUNDAMENTAL fields (P/E, EPS, market cap) may be shown
 * as an equity's, per Auditor condition B (never present a fund's numbers as an
 * equity's).
 *
 * VERIFIED real-data fact (captured fixture yahoo-v7-quote-ka.json): Yahoo tags
 * every PSX `.KA` security on the Karachi (KAR) feed as quoteType "MUTUALFUND"
 * even though they are the real listed equities (e.g. "Oil and Gas Development
 * Company Limited"). A literal quoteType==='EQUITY' filter would therefore null
 * 100% of real PSX fundamentals. So for the Karachi feed we pin on
 * currency === 'PKR' + exchange === 'KAR' (the true integrity guard: never
 * surface a non-PKR / wrong-exchange row as a PKR equity). For every other
 * (global) market we require a genuine quoteType==='EQUITY'.
 */
export function fundamentalsValid(row: {
  quoteType?: string;
  currency?: string;
  exchange?: string;
  market?: string;
}): boolean {
  const isKarachi = row.exchange === "KAR" || row.market === "pk_market";
  if (isKarachi) return row.currency === "PKR";
  return row.quoteType === "EQUITY";
}

export function mapV7Row(r: YahooV7Row): BatchQuote {
  const valid = fundamentalsValid(r);
  return {
    symbol: r.symbol ?? "",
    name: r.longName ?? r.shortName ?? null,
    price: num(r.regularMarketPrice),
    change: num(r.regularMarketChange),
    changePercent: num(r.regularMarketChangePercent),
    // Fundamentals are nulled when the row fails validation so a fund's numbers
    // are never rendered as an equity's.
    trailingPE: valid ? num(r.trailingPE) : null,
    eps: valid ? num(r.epsTrailingTwelveMonths) : null,
    marketCap: valid ? num(r.marketCap) : null,
    volume: num(r.regularMarketVolume),
    week52High: num(r.fiftyTwoWeekHigh),
    week52Low: num(r.fiftyTwoWeekLow),
    quoteType: r.quoteType ?? null,
    currency: r.currency ?? null,
    exchange: r.exchange ?? null,
    asOf: typeof r.regularMarketTime === "number" ? new Date(r.regularMarketTime * 1000).toISOString() : null,
    fundamentalsAvailable: valid,
  };
}

function chunk<T>(arr: T[], size: number): T[][] {
  const out: T[][] = [];
  for (let i = 0; i < arr.length; i += size) out.push(arr.slice(i, i + size));
  return out;
}

/**
 * FR-2: fetch quotes for many symbols in ≤ ceil(N/50) upstream calls (NFR-2).
 * FR-3 reconciliation: Yahoo silently drops unknown symbols, so we return the
 * exact set of requested symbols that were NOT returned — callers mark those
 * "data not available" rather than silently shrinking the universe.
 */
export async function fetchBatchQuotes(symbols: string[]): Promise<BatchQuoteResult> {
  const requested = symbols.filter((s) => s && s.trim().length > 0);
  if (requested.length === 0) return { quotes: [], missing: [] };

  const batches = chunk(requested, MAX_SYMBOLS_PER_CALL);
  const results = await mapLimit(batches, MAX_CONCURRENT_CALLS, async (batch) => {
    const url = `${YAHOO_BASE}/v7/finance/quote?symbols=${encodeURIComponent(batch.join(","))}`;
    const json = await fetchJson<YahooV7Response>(url);
    return json.quoteResponse?.result ?? [];
  });

  const rows = results.flat();
  const quotes = rows.map(mapV7Row);

  const returnedSet = new Set(quotes.map((q) => q.symbol.toUpperCase()));
  const missing = requested.filter((s) => !returnedSet.has(s.toUpperCase()));

  return { quotes, missing };
}
