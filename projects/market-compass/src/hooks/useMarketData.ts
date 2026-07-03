import { useQuery } from "@tanstack/react-query";
import { fetchCompanyList, fetchCompanyListDetailed, fetchCompanyDetail } from "@/services/companies";
import { fetchPsxIndices } from "@/services/psx";
import { fetchUsdPkr } from "@/services/yahoo";

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
