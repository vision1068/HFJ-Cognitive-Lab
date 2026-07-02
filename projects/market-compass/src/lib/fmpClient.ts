// Real-data-only client for the Financial Modeling Prep API.
// No mock data, no fallback candles, no simulated prices anywhere in this file.

const FMP_BASE_URL = "https://financialmodelingprep.com/stable";

export type FmpErrorKind = "missing_key" | "invalid_key" | "rate_limited" | "no_data" | "network" | "unknown";

export class FmpError extends Error {
  kind: FmpErrorKind;
  constructor(kind: FmpErrorKind, message: string) {
    super(message);
    this.kind = kind;
    this.name = "FmpError";
  }
}

export interface FmpQuote {
  symbol: string;
  name: string;
  price: number;
  changePercentage: number;
  change: number;
  dayLow: number;
  dayHigh: number;
  yearHigh: number;
  yearLow: number;
  marketCap: number;
  volume: number;
  avgVolume: number;
  open: number;
  previousClose: number;
  eps: number | null;
  pe: number | null;
  timestamp: number; // unix seconds, when FMP captured this quote
}

export interface FmpCandle {
  date: string; // "YYYY-MM-DD" or "YYYY-MM-DD HH:mm:ss" for intraday
  open: number;
  high: number;
  low: number;
  close: number;
  volume: number;
}

export type Timeframe = "1min" | "5min" | "15min" | "30min" | "1hour" | "4hour" | "1day";

function getApiKey(): string {
  const key = import.meta.env.VITE_FMP_API_KEY as string | undefined;
  if (!key || key.trim() === "" || key === "your_real_fmp_api_key_here") {
    throw new FmpError("missing_key", "FMP API key is not configured. Set VITE_FMP_API_KEY in your .env file.");
  }
  return key;
}

/** Returns true if a usable API key is present (does not validate against the API). */
export function hasApiKey(): boolean {
  try {
    getApiKey();
    return true;
  } catch {
    return false;
  }
}

async function fmpFetch<T>(path: string, params: Record<string, string> = {}): Promise<T> {
  const apiKey = getApiKey();
  const url = new URL(`${FMP_BASE_URL}${path}`);
  for (const [k, v] of Object.entries(params)) url.searchParams.set(k, v);
  url.searchParams.set("apikey", apiKey);

  let res: Response;
  try {
    res = await fetch(url.toString());
  } catch {
    throw new FmpError("network", "Could not reach Financial Modeling Prep. Check your internet connection.");
  }

  if (res.status === 401 || res.status === 403) {
    throw new FmpError("invalid_key", "The configured FMP API key was rejected. Verify VITE_FMP_API_KEY is correct.");
  }
  if (res.status === 429) {
    throw new FmpError(
      "rate_limited",
      "Market data is temporarily unavailable due to API rate limits. Please refresh after a moment."
    );
  }
  if (!res.ok) {
    throw new FmpError("unknown", `FMP API returned an error (HTTP ${res.status}).`);
  }

  const data = (await res.json()) as unknown;

  // FMP returns {"Error Message": "..."} on some invalid-symbol / invalid-param cases with HTTP 200.
  if (data && typeof data === "object" && "Error Message" in (data as Record<string, unknown>)) {
    throw new FmpError("no_data", String((data as Record<string, unknown>)["Error Message"]));
  }
  if (Array.isArray(data) && data.length === 0) {
    throw new FmpError("no_data", "No data available for this symbol or timeframe.");
  }

  return data as T;
}

/** Latest real-time (or last-close, when market is shut) quote for a symbol. */
export async function fetchQuote(symbol: string): Promise<FmpQuote> {
  const raw = await fmpFetch<
    Array<{
      symbol: string;
      name: string;
      price: number;
      changePercentage: number;
      change: number;
      dayLow: number;
      dayHigh: number;
      yearHigh: number;
      yearLow: number;
      marketCap: number;
      volume: number;
      avgVolume: number;
      open: number;
      previousClose: number;
      eps: number | null;
      pe: number | null;
      timestamp: number;
    }>
  >("/quote", { symbol });

  const q = raw[0];
  if (!q) throw new FmpError("no_data", `No quote data available for ${symbol}.`);
  return q;
}

/** Real historical OHLCV candles for a symbol at the given timeframe. */
export async function fetchHistoricalCandles(symbol: string, timeframe: Timeframe, limit = 300): Promise<FmpCandle[]> {
  const path = timeframe === "1day" ? "/historical-price-eod/full" : `/historical-chart/${timeframe}`;
  const raw = await fmpFetch<FmpCandle[]>(path, { symbol });

  if (!Array.isArray(raw) || raw.length === 0) {
    throw new FmpError("no_data", `No historical data available for ${symbol} at ${timeframe}.`);
  }

  // FMP returns newest-first; normalize to oldest-first for indicator math and charts.
  const chronological = [...raw].reverse();
  return chronological.slice(-limit);
}

/** Human-readable status derived from an FmpError, for the UI status badge. */
export function describeFmpError(err: unknown): { kind: FmpErrorKind; message: string } {
  if (err instanceof FmpError) return { kind: err.kind, message: err.message };
  return { kind: "unknown", message: err instanceof Error ? err.message : "Unknown error fetching market data." };
}
