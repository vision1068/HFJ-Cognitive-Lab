import { useQuery } from "@tanstack/react-query";
import { fetchCompanyList, fetchCompanyListDetailed, fetchCompanyDetail } from "@/services/companies";
import { fetchPsxIndices } from "@/services/psx";
import { fetchUniverse } from "@/services/universe";
import { searchCompanies } from "@/services/search";
import { fetchStatements } from "@/services/statements";
import { fetchQuote, fetchHistoricalCandles, hasApiKey, describeFmpError, type Timeframe } from "@/lib/fmpClient";

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

