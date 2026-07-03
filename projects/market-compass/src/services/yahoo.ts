import type { FxRate } from "@/types";
import { YAHOO_BASE, fetchJson } from "./http";

// Yahoo Finance is used ONLY for the USD/PKR exchange rate, which the PSX Data
// Portal does not publish. Everything else comes from PSX.
interface YahooChart {
  chart: {
    result: Array<{
      meta: {
        regularMarketPrice?: number;
        chartPreviousClose?: number;
        previousClose?: number;
        currency?: string;
      };
    }> | null;
    error: unknown;
  };
}

export async function fetchUsdPkr(): Promise<FxRate> {
  const json = await fetchJson<YahooChart>(`${YAHOO_BASE}/v8/finance/chart/USDPKR=X?interval=1d&range=5d`);
  const meta = json.chart?.result?.[0]?.meta;
  if (!meta || typeof meta.regularMarketPrice !== "number") {
    throw new Error("No USD/PKR data from Yahoo Finance");
  }
  const rate = meta.regularMarketPrice;
  const prev = meta.chartPreviousClose ?? meta.previousClose ?? rate;
  const changePercent = prev ? ((rate - prev) / prev) * 100 : 0;
  return { pair: "USD/PKR", rate, changePercent, source: "Yahoo Finance" };
}
