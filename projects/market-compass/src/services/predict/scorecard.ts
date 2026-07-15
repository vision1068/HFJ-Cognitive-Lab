// scorecard — deterministic numeric scorecard + verdict from the PSX context.
//
// Only Valuation, SizeLiquidity and Momentum are scorable from real PSX data;
// Profitability, FinancialStrength, CashFlow and Dividend are N/A (no PSX
// source) and return score10:null. Scores are renormalized over the SCORABLE
// weights only, so a missing category does NOT zero-deflate the total. Pure &
// deterministic: no Date.now, fixed category ordering.

import type { CategoryScore, Scorecard, Verdict } from "./types";
import type { CategoryKey } from "./durationPolicy";
import {
  CATEGORY_ORDER,
  categoryWeights,
  lookbackDays,
  smaWindow,
} from "./durationPolicy";
import type { PredictContext } from "./dataBinding";
import {
  maxDrawdownPct,
  periodReturnPct,
  pricePosition52wk,
  sma,
} from "./priceMath";

// Scoring thresholds live as versioned config, not magic numbers inline.
export const SCORING_CONFIG = {
  version: "1.0.0",
  earningsYieldFull: 0.2, // earnings yield at which the yield sub-score hits 10
  momentumReturnFullPct: 30, // lookback return that maps to a full momentum sub-score
  smaPremiumFullPct: 15, // price premium over SMA that maps to a full sub-score
  drawdownFloorPct: -50, // drawdown at which the drawdown sub-score hits 0
  marketCapTiersPkr: [
    { min: 500e9, score: 10 },
    { min: 200e9, score: 8 },
    { min: 75e9, score: 6 },
    { min: 20e9, score: 4 },
    { min: 0, score: 2 },
  ],
  volumeTiers: [
    { min: 5_000_000, score: 10 },
    { min: 1_000_000, score: 8 },
    { min: 250_000, score: 6 },
    { min: 50_000, score: 4 },
    { min: 0, score: 2 },
  ],
} as const;

const CATEGORY_LABELS: Record<CategoryKey, string> = {
  Valuation: "Valuation",
  SizeLiquidity: "Size & Liquidity",
  Momentum: "Momentum",
  Profitability: "Profitability",
  FinancialStrength: "Financial Strength",
  CashFlow: "Cash Flow",
  Dividend: "Dividend",
};

const NA_REASONS: Partial<Record<CategoryKey, string>> = {
  Profitability: "No PSX source publishes ROE / ROIC / margins",
  FinancialStrength: "No PSX source publishes balance-sheet ratios",
  CashFlow: "No PSX source publishes cash-flow statements",
  Dividend: "No PSX source publishes dividend history",
};

const SCORABLE_INSUFFICIENT = "Insufficient PSX price / fundamental data to score";

export type RawCategoryScores = Record<CategoryKey, number | null>;

function clamp(x: number, lo: number, hi: number): number {
  return Math.max(lo, Math.min(hi, x));
}

function mean(vals: (number | null)[]): number | null {
  const f = vals.filter((v): v is number => v != null && Number.isFinite(v));
  if (f.length === 0) return null;
  return f.reduce((a, b) => a + b, 0) / f.length;
}

function round2(x: number): number {
  return Math.round(x * 100) / 100;
}

function tierScore(v: number, tiers: ReadonlyArray<{ min: number; score: number }>): number {
  for (const t of tiers) {
    if (v >= t.min) return t.score;
  }
  return 0;
}

// ---- Category scoring (each returns 0..10 or null) --------------------------

export function valuationScore(ctx: PredictContext): number | null {
  const c = ctx.company;
  const pe = c.peRatio;
  const peerMed = ctx.peerMedianPE;

  let peSub: number | null = null;
  if (pe != null && pe > 0 && peerMed != null && peerMed > 0) {
    peSub = clamp(5 + 5 * ((peerMed - pe) / peerMed), 0, 10);
  }

  const ey =
    c.eps != null && c.eps > 0 && c.price > 0
      ? c.eps / c.price
      : pe != null && pe > 0
        ? 1 / pe
        : null;
  const eySub = ey != null ? clamp((ey / SCORING_CONFIG.earningsYieldFull) * 10, 0, 10) : null;

  const pos = pricePosition52wk(c.price, c.week52Low, c.week52High);
  // Value orientation: cheaper (lower in range) scores higher.
  const posSub = pos != null ? clamp((1 - pos) * 10, 0, 10) : null;

  return mean([peSub, eySub, posSub]);
}

export function sizeLiquidityScore(ctx: PredictContext): number | null {
  const c = ctx.company;
  const mcSub = c.marketCap != null && c.marketCap > 0
    ? tierScore(c.marketCap, SCORING_CONFIG.marketCapTiersPkr)
    : null;
  const volSub = c.volume != null && c.volume >= 0
    ? tierScore(c.volume, SCORING_CONFIG.volumeTiers)
    : null;
  return mean([mcSub, volSub]);
}

export function momentumScore(ctx: PredictContext): number | null {
  const c = ctx.company;
  const h = c.priceHistory;
  const lb = lookbackDays(ctx.duration);
  const sw = smaWindow(ctx.duration);

  const ret = periodReturnPct(h, lb);
  const retSub =
    ret != null ? clamp(5 + (ret / SCORING_CONFIG.momentumReturnFullPct) * 5, 0, 10) : null;

  const smaV = sma(h, sw);
  const smaSub =
    smaV != null && smaV > 0 && c.price > 0
      ? clamp(5 + (((c.price - smaV) / smaV) * 100 / SCORING_CONFIG.smaPremiumFullPct) * 5, 0, 10)
      : null;

  const dd = maxDrawdownPct(h, lb);
  const ddSub = dd != null ? clamp(10 * (1 - dd / SCORING_CONFIG.drawdownFloorPct), 0, 10) : null;

  return mean([retSub, smaSub, ddSub]);
}

// ---- Verdict banding --------------------------------------------------------

export function verdictFor(total100: number): Verdict {
  if (total100 >= 80) return "Strong Buy";
  if (total100 >= 65) return "Buy";
  if (total100 >= 45) return "Hold";
  if (total100 >= 30) return "Sell";
  return "Strong Sell";
}

// ---- Assembly (pure, exact math — the unit-tested core) ---------------------

/**
 * Combine raw category scores with base weights into a Scorecard.
 * Renormalizes over scorable (non-null) weights only, so an N/A category is
 * excluded from the average rather than counted as zero.
 */
export function assembleScorecard(
  scores: RawCategoryScores,
  weights: Record<CategoryKey, number>,
): Scorecard {
  const categories: CategoryScore[] = [];
  let scorableWeight = 0;
  let totalWeight = 0;
  let weightedScoreSum = 0; // Σ (score10/10 * weight) over scorable categories

  for (const key of CATEGORY_ORDER) {
    const weight = weights[key];
    const score10 = scores[key];
    totalWeight += weight;

    if (score10 == null) {
      categories.push({
        key,
        label: CATEGORY_LABELS[key],
        score10: null,
        naReason: NA_REASONS[key] ?? SCORABLE_INSUFFICIENT,
        weight,
      });
    } else {
      scorableWeight += weight;
      weightedScoreSum += (score10 / 10) * weight;
      categories.push({
        key,
        label: CATEGORY_LABELS[key],
        score10: round2(score10),
        weight,
      });
    }
  }

  const total100 = scorableWeight > 0 ? round2((weightedScoreSum / scorableWeight) * 100) : 0;
  const coverage = totalWeight > 0 ? round2(scorableWeight / totalWeight) : 0;

  // Zero scorable weight means nothing was actually evaluated — "Strong Sell"
  // would misrepresent an absence of data as a negative judgement.
  const verdict: Verdict = scorableWeight > 0 ? verdictFor(total100) : "Not Rated";

  return {
    total100,
    verdict,
    categories,
    coverage,
    lowConfidence: coverage < 0.5,
  };
}

/** Compute the full scorecard from a bound PSX context. */
export function computeScorecard(ctx: PredictContext): Scorecard {
  const scores: RawCategoryScores = {
    Valuation: valuationScore(ctx),
    SizeLiquidity: sizeLiquidityScore(ctx),
    Momentum: momentumScore(ctx),
    Profitability: null,
    FinancialStrength: null,
    CashFlow: null,
    Dividend: null,
  };
  return assembleScorecard(scores, categoryWeights(ctx.duration));
}
