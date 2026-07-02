import { useQuery } from "@tanstack/react-query";
import { fetchQuote, fetchHistoricalCandles, hasApiKey, describeFmpError, type Timeframe } from "@/lib/fmpClient";
import { runTechnicalAnalysis } from "@/lib/indicators";

export type DataConnectionStatus =
  | "no-key" // key missing/placeholder — show setup screen
  | "loading" // "Loading Market Data"
  | "connected" // "Live Data Connected"
  | "error" // "FMP API Error" (includes rate-limited, invalid key, network)
  | "no-data"; // "No Data Available for This Symbol or Timeframe"

export function useLiveQuote(symbol: string) {
  return useQuery({
    queryKey: ["fmp", "quote", symbol],
    queryFn: () => fetchQuote(symbol),
    enabled: hasApiKey() && symbol.length > 0,
    staleTime: 15_000,
    refetchInterval: 15_000, // real-time-ish polling within FMP's free-tier limits
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

/**
 * Combined hook: real quote + real candles + calculated technical analysis.
 * Every field returned here traces back to an FMP API response — this hook
 * never fabricates a value. If real data isn't available, `status` reflects
 * that and `analysis`/`quote`/`candles` are left undefined for the UI to gate on.
 */
export function useMarketAnalysis(symbol: string, timeframe: Timeframe) {
  const quoteQuery = useLiveQuote(symbol);
  const candlesQuery = useHistoricalCandles(symbol, timeframe);

  const keyPresent = hasApiKey();

  let status: DataConnectionStatus;
  let errorMessage: string | undefined;

  if (!keyPresent) {
    status = "no-key";
  } else if (quoteQuery.isLoading || candlesQuery.isLoading) {
    status = "loading";
  } else if (quoteQuery.isError || candlesQuery.isError) {
    const err = quoteQuery.error ?? candlesQuery.error;
    const { kind, message } = describeFmpError(err);
    status = kind === "no_data" ? "no-data" : "error";
    errorMessage = message;
  } else if (!quoteQuery.data || !candlesQuery.data || candlesQuery.data.length < 50) {
    status = "no-data";
    errorMessage = "Not enough historical data was returned to calculate reliable indicators.";
  } else {
    status = "connected";
  }

  let analysis: ReturnType<typeof runTechnicalAnalysis> | undefined;
  if (status === "connected" && candlesQuery.data) {
    try {
      analysis = runTechnicalAnalysis(candlesQuery.data);
    } catch (e) {
      status = "no-data";
      errorMessage = e instanceof Error ? e.message : "Could not calculate indicators from the returned data.";
    }
  }

  const lastUpdated = quoteQuery.dataUpdatedAt ? new Date(quoteQuery.dataUpdatedAt) : undefined;

  return {
    status,
    errorMessage,
    quote: quoteQuery.data,
    candles: candlesQuery.data,
    analysis,
    lastUpdated,
    refetch: () => {
      quoteQuery.refetch();
      candlesQuery.refetch();
    },
  };
}
