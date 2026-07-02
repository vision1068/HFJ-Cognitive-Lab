// Pure technical-analysis functions. Every function here takes real OHLCV
// candles (from FMP) as input and returns deterministic, calculated output.
// Nothing in this file generates, randomizes, or simulates a price or signal.

import type { FmpCandle } from "@/lib/fmpClient";

export interface IndicatorSeries {
  sma20: (number | null)[];
  sma50: (number | null)[];
  ema12: (number | null)[];
  ema26: (number | null)[];
  rsi14: (number | null)[];
  macd: (number | null)[];
  macdSignal: (number | null)[];
  macdHistogram: (number | null)[];
  bollingerUpper: (number | null)[];
  bollingerMiddle: (number | null)[];
  bollingerLower: (number | null)[];
  atr14: (number | null)[];
}

export interface SupportResistance {
  support: number[];
  resistance: number[];
}

export interface LiquiditySweep {
  index: number;
  date: string;
  type: "buy-side" | "sell-side"; // sweep of highs (buy-side liquidity) or lows (sell-side liquidity)
  sweptLevel: number;
  closedBack: boolean; // true = swept and reversed (classic liquidity grab)
}

export type MarketStructure = "uptrend" | "downtrend" | "ranging";

export type SignalStrength = "Strong Buy" | "Buy" | "Neutral" | "Sell" | "Strong Sell";

export interface TechnicalAnalysisResult {
  indicators: IndicatorSeries;
  supportResistance: SupportResistance;
  marketStructure: MarketStructure;
  liquiditySweeps: LiquiditySweep[];
  confidenceScore: number; // 0-100
  signal: SignalStrength;
  signalReasons: string[];
}

function sma(values: number[], period: number): (number | null)[] {
  const out: (number | null)[] = new Array(values.length).fill(null);
  let sum = 0;
  for (let i = 0; i < values.length; i++) {
    sum += values[i];
    if (i >= period) sum -= values[i - period];
    if (i >= period - 1) out[i] = sum / period;
  }
  return out;
}

function ema(values: number[], period: number): (number | null)[] {
  const out: (number | null)[] = new Array(values.length).fill(null);
  const k = 2 / (period + 1);
  let prev: number | null = null;
  for (let i = 0; i < values.length; i++) {
    if (i < period - 1) continue;
    if (prev === null) {
      // seed with SMA of the first `period` values
      const seed = values.slice(i - period + 1, i + 1).reduce((a, b) => a + b, 0) / period;
      prev = seed;
    } else {
      prev = values[i] * k + prev * (1 - k);
    }
    out[i] = prev;
  }
  return out;
}

function rsi(values: number[], period = 14): (number | null)[] {
  const out: (number | null)[] = new Array(values.length).fill(null);
  if (values.length < period + 1) return out;

  let gainSum = 0;
  let lossSum = 0;
  for (let i = 1; i <= period; i++) {
    const delta = values[i] - values[i - 1];
    if (delta >= 0) gainSum += delta;
    else lossSum -= delta;
  }
  let avgGain = gainSum / period;
  let avgLoss = lossSum / period;
  out[period] = avgLoss === 0 ? 100 : 100 - 100 / (1 + avgGain / avgLoss);

  for (let i = period + 1; i < values.length; i++) {
    const delta = values[i] - values[i - 1];
    const gain = delta > 0 ? delta : 0;
    const loss = delta < 0 ? -delta : 0;
    avgGain = (avgGain * (period - 1) + gain) / period;
    avgLoss = (avgLoss * (period - 1) + loss) / period;
    out[i] = avgLoss === 0 ? 100 : 100 - 100 / (1 + avgGain / avgLoss);
  }
  return out;
}

function macdFrom(closes: number[]) {
  const ema12 = ema(closes, 12);
  const ema26 = ema(closes, 26);
  const macdLine = closes.map((_, i) => {
    const a = ema12[i];
    const b = ema26[i];
    return a !== null && b !== null ? a - b : null;
  });
  const macdValuesForSignal = macdLine.filter((v): v is number => v !== null);
  const signalRaw = ema(macdValuesForSignal, 9);
  // re-align signal (which was computed on a compacted array) back to full-length series
  const signal: (number | null)[] = new Array(closes.length).fill(null);
  let si = 0;
  for (let i = 0; i < closes.length; i++) {
    if (macdLine[i] === null) continue;
    signal[i] = signalRaw[si] ?? null;
    si++;
  }
  const histogram = macdLine.map((v, i) => (v !== null && signal[i] !== null ? v - (signal[i] as number) : null));
  return { macdLine, signal, histogram };
}

function bollinger(closes: number[], period = 20, stdDevMultiplier = 2) {
  const middle = sma(closes, period);
  const upper: (number | null)[] = new Array(closes.length).fill(null);
  const lower: (number | null)[] = new Array(closes.length).fill(null);
  for (let i = period - 1; i < closes.length; i++) {
    const slice = closes.slice(i - period + 1, i + 1);
    const mean = middle[i] as number;
    const variance = slice.reduce((sum, v) => sum + (v - mean) ** 2, 0) / period;
    const stdDev = Math.sqrt(variance);
    upper[i] = mean + stdDevMultiplier * stdDev;
    lower[i] = mean - stdDevMultiplier * stdDev;
  }
  return { upper, middle, lower };
}

function atr(candles: FmpCandle[], period = 14): (number | null)[] {
  const out: (number | null)[] = new Array(candles.length).fill(null);
  const trueRanges: number[] = [];
  for (let i = 0; i < candles.length; i++) {
    if (i === 0) {
      trueRanges.push(candles[i].high - candles[i].low);
      continue;
    }
    const prevClose = candles[i - 1].close;
    const tr = Math.max(
      candles[i].high - candles[i].low,
      Math.abs(candles[i].high - prevClose),
      Math.abs(candles[i].low - prevClose)
    );
    trueRanges.push(tr);
  }
  let prevAtr: number | null = null;
  for (let i = 0; i < trueRanges.length; i++) {
    if (i < period - 1) continue;
    if (prevAtr === null) {
      prevAtr = trueRanges.slice(i - period + 1, i + 1).reduce((a, b) => a + b, 0) / period;
    } else {
      prevAtr = (prevAtr * (period - 1) + trueRanges[i]) / period;
    }
    out[i] = prevAtr;
  }
  return out;
}

/** Local swing highs/lows (fractal pivots) used as support/resistance levels. */
function findSupportResistance(candles: FmpCandle[], lookback = 3): SupportResistance {
  const support: number[] = [];
  const resistance: number[] = [];
  for (let i = lookback; i < candles.length - lookback; i++) {
    const window = candles.slice(i - lookback, i + lookback + 1);
    const isHigh = window.every((c) => candles[i].high >= c.high);
    const isLow = window.every((c) => candles[i].low <= c.low);
    if (isHigh) resistance.push(candles[i].high);
    if (isLow) support.push(candles[i].low);
  }
  // dedupe/cluster to the most recent, most significant levels
  const uniq = (arr: number[]) =>
    Array.from(new Set(arr.map((v) => Math.round(v * 100) / 100)))
      .sort((a, b) => a - b)
      .slice(-5);
  return { support: uniq(support), resistance: uniq(resistance) };
}

/** Higher-highs/higher-lows vs lower-highs/lower-lows over the recent swing window. */
function detectMarketStructure(candles: FmpCandle[]): MarketStructure {
  if (candles.length < 20) return "ranging";
  const recent = candles.slice(-20);
  const highs = recent.map((c) => c.high);
  const lows = recent.map((c) => c.low);
  const firstHalf = { high: Math.max(...highs.slice(0, 10)), low: Math.min(...lows.slice(0, 10)) };
  const secondHalf = { high: Math.max(...highs.slice(10)), low: Math.min(...lows.slice(10)) };

  const higherHigh = secondHalf.high > firstHalf.high;
  const higherLow = secondHalf.low > firstHalf.low;
  const lowerHigh = secondHalf.high < firstHalf.high;
  const lowerLow = secondHalf.low < firstHalf.low;

  if (higherHigh && higherLow) return "uptrend";
  if (lowerHigh && lowerLow) return "downtrend";
  return "ranging";
}

/** Detects candles that pierce a recent swing high/low then close back inside it (a liquidity grab). */
function detectLiquiditySweeps(candles: FmpCandle[], lookback = 10): LiquiditySweep[] {
  const sweeps: LiquiditySweep[] = [];
  for (let i = lookback; i < candles.length; i++) {
    const window = candles.slice(i - lookback, i);
    const priorHigh = Math.max(...window.map((c) => c.high));
    const priorLow = Math.min(...window.map((c) => c.low));
    const c = candles[i];

    if (c.high > priorHigh && c.close < priorHigh) {
      sweeps.push({ index: i, date: c.date, type: "sell-side", sweptLevel: priorHigh, closedBack: true });
    } else if (c.low < priorLow && c.close > priorLow) {
      sweeps.push({ index: i, date: c.date, type: "buy-side", sweptLevel: priorLow, closedBack: true });
    }
  }
  return sweeps.slice(-5);
}

function computeSignal(
  indicators: IndicatorSeries,
  structure: MarketStructure,
  liquiditySweeps: LiquiditySweep[],
  lastClose: number
): { signal: SignalStrength; confidenceScore: number; reasons: string[] } {
  const last = <T,>(arr: T[]) => arr[arr.length - 1];
  const rsiVal = last(indicators.rsi14);
  const macdVal = last(indicators.macd);
  const macdSig = last(indicators.macdSignal);
  const sma20 = last(indicators.sma20);
  const sma50 = last(indicators.sma50);
  const bbUpper = last(indicators.bollingerUpper);
  const bbLower = last(indicators.bollingerLower);

  let bullPoints = 0;
  let bearPoints = 0;
  const reasons: string[] = [];

  if (rsiVal !== null) {
    if (rsiVal < 30) {
      bullPoints += 2;
      reasons.push(`RSI(14) at ${rsiVal.toFixed(1)} indicates oversold conditions.`);
    } else if (rsiVal > 70) {
      bearPoints += 2;
      reasons.push(`RSI(14) at ${rsiVal.toFixed(1)} indicates overbought conditions.`);
    }
  }

  if (macdVal !== null && macdSig !== null) {
    if (macdVal > macdSig) {
      bullPoints += 1;
      reasons.push("MACD line is above the signal line (bullish momentum).");
    } else {
      bearPoints += 1;
      reasons.push("MACD line is below the signal line (bearish momentum).");
    }
  }

  if (sma20 !== null && sma50 !== null) {
    if (sma20 > sma50) {
      bullPoints += 1;
      reasons.push("SMA20 is above SMA50 (short-term uptrend vs. medium-term).");
    } else {
      bearPoints += 1;
      reasons.push("SMA20 is below SMA50 (short-term downtrend vs. medium-term).");
    }
  }

  if (bbUpper !== null && bbLower !== null) {
    if (lastClose <= bbLower) {
      bullPoints += 1;
      reasons.push("Price is at or below the lower Bollinger Band.");
    } else if (lastClose >= bbUpper) {
      bearPoints += 1;
      reasons.push("Price is at or above the upper Bollinger Band.");
    }
  }

  if (structure === "uptrend") {
    bullPoints += 1;
    reasons.push("Market structure shows higher highs and higher lows (uptrend).");
  } else if (structure === "downtrend") {
    bearPoints += 1;
    reasons.push("Market structure shows lower highs and lower lows (downtrend).");
  }

  const lastSweep = liquiditySweeps[liquiditySweeps.length - 1];
  if (lastSweep) {
    if (lastSweep.type === "buy-side") {
      bullPoints += 1;
      reasons.push(`Recent liquidity sweep below ${lastSweep.sweptLevel.toFixed(2)} closed back above — possible bullish reversal.`);
    } else {
      bearPoints += 1;
      reasons.push(`Recent liquidity sweep above ${lastSweep.sweptLevel.toFixed(2)} closed back below — possible bearish reversal.`);
    }
  }

  const totalPoints = bullPoints + bearPoints;
  const netScore = totalPoints === 0 ? 0 : (bullPoints - bearPoints) / totalPoints;
  const confidenceScore = Math.round(Math.min(100, (totalPoints / 7) * 60 + Math.abs(netScore) * 40));

  let signal: SignalStrength = "Neutral";
  if (netScore >= 0.6) signal = "Strong Buy";
  else if (netScore >= 0.2) signal = "Buy";
  else if (netScore <= -0.6) signal = "Strong Sell";
  else if (netScore <= -0.2) signal = "Sell";

  return { signal, confidenceScore, reasons };
}

/**
 * Runs the full technical analysis suite against real historical candles.
 * Requires at least 50 candles so SMA50/MACD/Bollinger have enough data to be meaningful;
 * callers should show "No Data Available" rather than call this with fewer.
 */
export function runTechnicalAnalysis(candles: FmpCandle[]): TechnicalAnalysisResult {
  if (candles.length < 50) {
    throw new Error("Insufficient historical data to calculate reliable indicators (minimum 50 candles required).");
  }

  const closes = candles.map((c) => c.close);

  const indicators: IndicatorSeries = {
    sma20: sma(closes, 20),
    sma50: sma(closes, 50),
    ema12: ema(closes, 12),
    ema26: ema(closes, 26),
    rsi14: rsi(closes, 14),
    ...(() => {
      const { macdLine, signal, histogram } = macdFrom(closes);
      return { macd: macdLine, macdSignal: signal, macdHistogram: histogram };
    })(),
    ...(() => {
      const bb = bollinger(closes, 20, 2);
      return { bollingerUpper: bb.upper, bollingerMiddle: bb.middle, bollingerLower: bb.lower };
    })(),
    atr14: atr(candles, 14),
  };

  const supportResistance = findSupportResistance(candles);
  const marketStructure = detectMarketStructure(candles);
  const liquiditySweeps = detectLiquiditySweeps(candles);
  const lastClose = closes[closes.length - 1];

  const { signal, confidenceScore, reasons } = computeSignal(indicators, marketStructure, liquiditySweeps, lastClose);

  return {
    indicators,
    supportResistance,
    marketStructure,
    liquiditySweeps,
    confidenceScore,
    signal,
    signalReasons: reasons,
  };
}
