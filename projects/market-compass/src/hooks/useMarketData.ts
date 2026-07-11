import { useQuery } from "@tanstack/react-query";
import { fetchCompanyList, fetchCompanyListDetailed, fetchCompanyDetail } from "@/services/companies";
import { fetchPsxIndices } from "@/services/psx";
import { fetchUsdPkr } from "@/services/yahoo";
import { fetchUniverse } from "@/services/universe";
import { searchCompanies } from "@/services/search";
import { fetchStatements } from "@/services/statements";
import { fetchGlobalQuote, fetchGlobalCandles, hasRealOHLC } from "@/services/globalQuote";
import { fetchQuote, fetchHistoricalCandles, hasApiKey, describeFmpError, type Timeframe } from "@/lib/fmpClient";
import { runTechnicalAnalysis } from "@/lib/indicators";

const REFRESH_MS = 60_000; // 60s live refresh, per approved plan

const common = {
  refetchInterval: REFRESH_MS,
  staleTime: REFRESH_MS,
  refetchOnWindowFocus: false,
  retry: 1,
} as const;

export function useCompanies() {
  return useQuery({ queryKey: ["psx", "companies"], queryFn: fetchCompanyList, ...common });
}

export function useCompaniesDetailed() {
  return useQuery({ queryKey: ["psx", "companies", "detailed"], queryFn: fetchCompanyListDetailed, ...common });
}

export function useCompany(ticker: string | undefined) {
  return useQuery({
    queryKey: ["psx", "company", ticker],
    queryFn: () => fetchCompanyDetail(ticker as string),
    enabled: Boolean(ticker),
    ...common,
  });
}

export function useIndices() {
  return useQuery({ queryKey: ["psx", "indices"], queryFn: fetchPsxIndices, ...common });
}

export function useUsdPkr() {
  return useQuery({ queryKey: ["fx", "usdpkr"], queryFn: fetchUsdPkr, ...common });
}

// FR-4: the full PSX universe (live with committed-seed fallback). Rarely
// changes, so it is cached far longer than the 60s quote refresh.
export function useUniverse() {
  return useQuery({
    queryKey: ["psx", "universe"],
    queryFn: fetchUniverse,
    staleTime: 24 * 60 * 60_000,
    refetchOnWindowFocus: false,
    retry: 1,
  });
}

// FR-5: merged local + global search. Runs against the live universe when
// available, else the fallback seed baked into searchCompanies.
export function useSearch(q: string) {
  const universeQuery = useUniverse();
  return useQuery({
    queryKey: ["search", q, universeQuery.data ? "live" : "seed"],
    queryFn: () => searchCompanies(q, universeQuery.data),
    enabled: q.trim().length > 0,
    staleTime: 30_000,
    refetchOnWindowFocus: false,
  });
}

// FR-7: multi-year statements (global real, PSX honest N/A).
export function useStatements(symbol: string | undefined) {
  return useQuery({
    queryKey: ["statements", symbol],
    queryFn: () => fetchStatements(symbol as string),
    enabled: Boolean(symbol),
    staleTime: 24 * 60 * 60_000,
    refetchOnWindowFocus: false,
  });
}

export type DataConnectionStatus =
  | "no-key" // key missing/placeholder — show setup screen (legacy FMP path only)
  | "loading" // "Loading Market Data"
  | "connected" // "Live Data Connected"
  | "error" // upstream error (network, rate-limited)
  | "no-data"; // "No Data Available for This Symbol or Timeframe"

// --- Optional legacy FMP hooks (augmentation only; the app is fully keyless) ---
export function useLiveQuote(symbol: string) {
  return useQuery({
    queryKey: ["fmp", "quote", symbol],
    queryFn: () => fetchQuote(symbol),
    enabled: hasApiKey() && symbol.length > 0,
    staleTime: 15_000,
    refetchInterval: 15_000,
    retry: (failureCount, error) => {
      const { kind } = describeFmpError(error);
      if (kind === "invalid_key" || kind === "missing_key" || kind === "no_data") return false;
      return failureCount < 2;
    },
  });
}

export function useHistoricalCandles(symbol: string, timeframe: Timeframe) {
  return useQuery({
    queryKey: ["fmp", "candles", symbol, timeframe],
    queryFn: () => fetchHistoricalCandles(symbol, timeframe),
    enabled: hasApiKey() && symbol.length > 0,
    staleTime: timeframe === "1day" ? 5 * 60_000 : 30_000,
    retry: (failureCount, error) => {
      const { kind } = describeFmpError(error);
      if (kind === "invalid_key" || kind === "missing_key" || kind === "no_data") return false;
      return failureCount < 2;
    },
  });
}

// FR-8: map an FMP-style timeframe onto a keyless Yahoo v8 chart interval+range
// that returns enough candles (≥50) for the indicator suite.
function timeframeToYahoo(tf: Timeframe): { interval: string; range: string } {
  switch (tf) {
    case "1min":
      return { interval: "1m", range: "5d" };
    case "5min":
      return { interval: "5m", range: "1mo" };
    case "15min":
      return { interval: "15m", range: "1mo" };
    case "30min":
      return { interval: "30m", range: "1mo" };
    case "1hour":
      return { interval: "60m", range: "3mo" };
    case "4hour":
      return { interval: "60m", range: "6mo" };
    case "1day":
    default:
      return { interval: "1d", range: "1y" };
  }
}

export function useGlobalCandles(symbol: string, timeframe: Timeframe) {
  const { interval, range } = timeframeToYahoo(timeframe);
  return useQuery({
    queryKey: ["yahoo", "candles", symbol, timeframe],
    queryFn: () => fetchGlobalCandles(symbol, interval, range),
    enabled: symbol.length > 0,
    staleTime: timeframe === "1day" ? 5 * 60_000 : 30_000,
    refetchOnWindowFocus: false,
    retry: 1,
  });
}

export function useGlobalQuote(symbol: string) {
  return useQuery({
    queryKey: ["yahoo", "gquote", symbol],
    queryFn: () => fetchGlobalQuote(symbol),
    enabled: symbol.length > 0,
    staleTime: 15_000,
    refetchInterval: 15_000,
    refetchOnWindowFocus: false,
    retry: 1,
  });
}

// Quote view exposed to the analysis UI. Field names are kept FMP-compatible so
// existing consumers work unchanged, but every value is sourced keyless from
// Yahoo (v7 quote + v8 chart) — no FMP key required.
export interface AnalysisQuoteView {
  symbol: string;
  price: number;
  change: number;
  changePercentage: number;
  dayLow: number;
  dayHigh: number;
  volume: number;
  asOf: string | null;
  currency: string | null;
}

/**
 * FR-8 + Auditor B4: keyless real quote + real candles + technical analysis.
 * Works with NO FMP key. Technical indicators run ONLY when the candle series
 * carries genuine OHLC — a close-only (degenerate) series yields "no-data"
 * instead of a meaningless signal.
 */
export function useMarketAnalysis(symbol: string, timeframe: Timeframe) {
  const quoteQuery = useGlobalQuote(symbol);
  const candlesQuery = useGlobalCandles(symbol, timeframe);

  const candles = candlesQuery.data;

  let status: DataConnectionStatus;
  let errorMessage: string | undefined;

  if (quoteQuery.isLoading || candlesQuery.isLoading) {
    status = "loading";
  } else if (quoteQuery.isError || candlesQuery.isError) {
    status = "error";
    const err = candlesQuery.error ?? quoteQuery.error;
    errorMessage = err instanceof Error ? err.message : "Could not reach the market-data source.";
  } else if (!candles || candles.length < 50) {
    status = "no-data";
    errorMessage = "Not enough historical data was returned to calculate reliable indicators.";
  } else if (!hasRealOHLC(candles)) {
    // Integrity guard: refuse to compute indicators on a close-only series.
    status = "no-data";
    errorMessage = "This symbol only publishes close prices (no intraday range), so technical indicators cannot be computed.";
  } else {
    status = "connected";
  }

  let analysis: ReturnType<typeof runTechnicalAnalysis> | undefined;
  if (status === "connected" && candles) {
    try {
      analysis = runTechnicalAnalysis(candles);
    } catch (e) {
      status = "no-data";
      errorMessage = e instanceof Error ? e.message : "Could not calculate indicators from the returned data.";
    }
  }

  const bq = quoteQuery.data ?? null;
  const lastCandle = candles?.[candles.length - 1];
  const prevCandle = candles?.[candles.length - 2];
  const derivedChange =
    lastCandle && prevCandle ? lastCandle.close - prevCandle.close : 0;
  const quote: AnalysisQuoteView | undefined =
    status === "connected"
      ? {
          symbol,
          price: bq?.price ?? lastCandle?.close ?? 0,
          change: bq?.change ?? derivedChange,
          changePercentage:
            bq?.changePercent ??
            (prevCandle && prevCandle.close ? (derivedChange / prevCandle.close) * 100 : 0),
          dayLow: lastCandle?.low ?? 0,
          dayHigh: lastCandle?.high ?? 0,
          volume: lastCandle?.volume ?? bq?.volume ?? 0,
          asOf: bq?.asOf ?? null,
          currency: bq?.currency ?? null,
        }
      : undefined;

  const lastUpdated = quoteQuery.dataUpdatedAt ? new Date(quoteQuery.dataUpdatedAt) : undefined;

  return {
    // Cast keeps the public status type as the full DataConnectionStatus union
    // (the keyless path no longer emits "no-key", but consumers still handle it).
    status: status as DataConnectionStatus,
    errorMessage,
    quote,
    candles,
    analysis,
    lastUpdated,
    refetch: () => {
      quoteQuery.refetch();
      candlesQuery.refetch();
    },
  };
}
