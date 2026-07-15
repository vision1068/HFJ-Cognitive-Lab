// priceMath — pure, deterministic analytics over a PSX EOD close series.
//
// `priceHistory` is oldest -> newest (per the Company data contract). Every
// function returns `null` when there is not enough real data rather than
// fabricating a figure. No Date.now, no randomness — same input, same output.

import type { PricePoint } from "@/types";

export function median(nums: number[]): number | null {
  const arr = nums.filter((n) => Number.isFinite(n)).slice().sort((a, b) => a - b);
  if (arr.length === 0) return null;
  const mid = Math.floor(arr.length / 2);
  return arr.length % 2 === 1 ? arr[mid] : (arr[mid - 1] + arr[mid]) / 2;
}

/** Percentage return over the last `lookback` trading days (clamped to series). */
export function periodReturnPct(history: PricePoint[], lookback: number): number | null {
  if (history.length < 2) return null;
  const last = history[history.length - 1].close;
  const startIdx = Math.max(0, history.length - 1 - lookback);
  const start = history[startIdx].close;
  if (!Number.isFinite(start) || start === 0 || !Number.isFinite(last)) return null;
  return ((last - start) / start) * 100;
}

/** Simple moving average of the last `window` closes. */
export function sma(history: PricePoint[], window: number): number | null {
  if (history.length === 0 || window <= 0) return null;
  const slice = history.slice(-window);
  if (slice.length === 0) return null;
  const sum = slice.reduce((a, p) => a + p.close, 0);
  return sum / slice.length;
}

/** Max peak-to-trough drawdown (negative %) over the last `lookback` days. */
export function maxDrawdownPct(history: PricePoint[], lookback: number): number | null {
  const slice = history.slice(-Math.max(2, lookback));
  if (slice.length < 2) return null;
  let peak = slice[0].close;
  let maxDd = 0;
  for (const p of slice) {
    if (p.close > peak) peak = p.close;
    if (peak > 0) {
      const dd = (p.close - peak) / peak; // <= 0
      if (dd < maxDd) maxDd = dd;
    }
  }
  return maxDd * 100;
}

/** Annualized volatility (%) of daily returns over the last `lookback` days. */
export function annualizedVolPct(history: PricePoint[], lookback: number): number | null {
  const slice = history.slice(-Math.max(2, lookback + 1));
  if (slice.length < 3) return null;
  const rets: number[] = [];
  for (let i = 1; i < slice.length; i++) {
    const prev = slice[i - 1].close;
    const cur = slice[i].close;
    if (prev > 0) rets.push((cur - prev) / prev);
  }
  if (rets.length < 2) return null;
  const mean = rets.reduce((a, b) => a + b, 0) / rets.length;
  const variance = rets.reduce((a, r) => a + (r - mean) ** 2, 0) / (rets.length - 1);
  const daily = Math.sqrt(variance);
  return daily * Math.sqrt(252) * 100;
}

/** Where price sits in the 52-week range, 0 (at low) .. 1 (at high). */
export function pricePosition52wk(
  price: number,
  low: number | null,
  high: number | null,
): number | null {
  if (low == null || high == null || high <= low || !Number.isFinite(price)) return null;
  return (price - low) / (high - low);
}
