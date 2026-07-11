import type { BatchQuote } from "@/types";
import type { FmpCandle } from "@/lib/fmpClient";
import { YAHOO_BASE, fetchJson } from "./http";
import { fetchBatchQuotes } from "./yahooQuote";

// FR-8: keyless global quote + candles via Yahoo, so useMarketAnalysis works with
// NO FMP key. The v8 chart is keyless and carries true OHLC (VERIFIED for both
// global symbols and PSX `.KA`); candles are mapped to the FmpCandle shape that
// indicators.ts already consumes.

interface YahooChartResponse {
  chart: {
    result: Array<{
      timestamp?: number[];
      indicators: {
        quote?: Array<{
          open?: (number | null)[];
          high?: (number | null)[];
          low?: (number | null)[];
          close?: (number | null)[];
          volume?: (number | null)[];
        }>;
      };
    }> | null;
    error: unknown;
  };
}

export function mapChartToCandles(raw: YahooChartResponse): FmpCandle[] {
  const result = raw.chart?.result?.[0];
  const ts = result?.timestamp;
  const q = result?.indicators?.quote?.[0];
  if (!result || !Array.isArray(ts) || !q) return [];
  const candles: FmpCandle[] = [];
  for (let i = 0; i < ts.length; i++) {
    const open = q.open?.[i];
    const high = q.high?.[i];
    const low = q.low?.[i];
    const close = q.close?.[i];
    // Yahoo interleaves null entries (holidays / halts) — skip incomplete rows.
    if (open == null || high == null || low == null || close == null) continue;
    candles.push({
      date: new Date(ts[i] * 1000).toISOString().slice(0, 10),
      open,
      high,
      low,
      close,
      volume: q.volume?.[i] ?? 0,
    });
  }
  return candles;
}

/**
 * CRITICAL INTEGRITY GUARD (Auditor B4): technical indicators must NEVER run on
 * a close-only series where high===low===close across the window (as the PSX
 * portal EOD feed produces). Such a series has no real intraday range, so ATR,
 * Bollinger width, liquidity sweeps and swing highs/lows would all be meaningless.
 * Returns true only when a majority of candles carry a genuine high>low range.
 */
export function hasRealOHLC(candles: FmpCandle[]): boolean {
  if (candles.length === 0) return false;
  const withRange = candles.reduce((n, c) => n + (c.high > c.low ? 1 : 0), 0);
  return withRange / candles.length >= 0.5;
}

export async function fetchGlobalCandles(
  symbol: string,
  interval: string = "1d",
  range: string = "6mo"
): Promise<FmpCandle[]> {
  const raw = await fetchJson<YahooChartResponse>(
    `${YAHOO_BASE}/v8/finance/chart/${encodeURIComponent(symbol)}?interval=${encodeURIComponent(interval)}&range=${encodeURIComponent(range)}`
  );
  return mapChartToCandles(raw);
}

/** Single-symbol quote via the batch endpoint (keyless, crumb handled by proxy). */
export async function fetchGlobalQuote(symbol: string): Promise<BatchQuote | null> {
  const { quotes } = await fetchBatchQuotes([symbol]);
  return quotes[0] ?? null;
}
