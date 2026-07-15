// durationPolicy — versioned, per-Duration configuration.
//
// Encodes (a) which sections are in scope for a horizon, (b) the scorecard
// category base weights per horizon, and (c) the price lookback / SMA windows
// used by momentum and technical figures. All "business rules" (weights,
// windows, in-scope maps) live here as data, never inline in the compute code —
// Constitution: config-driven, and versioned so a change is traceable.

import type { Duration, SectionId } from "./types";

export const POLICY_VERSION = "1.0.0";

// Every duration's full weight column must sum to this constant.
export const WEIGHT_TOTAL = 100;

export type CategoryKey =
  | "Valuation"
  | "SizeLiquidity"
  | "Momentum"
  | "Profitability"
  | "FinancialStrength"
  | "CashFlow"
  | "Dividend";

// The three categories that CAN be scored from real PSX data.
export const SCORABLE_CATEGORIES: CategoryKey[] = [
  "Valuation",
  "SizeLiquidity",
  "Momentum",
];

// Categories with no free PSX source — always N/A, never invented.
export const NA_CATEGORIES: CategoryKey[] = [
  "Profitability",
  "FinancialStrength",
  "CashFlow",
  "Dividend",
];

export const CATEGORY_ORDER: CategoryKey[] = [
  ...SCORABLE_CATEGORIES,
  ...NA_CATEGORIES,
];

// Weights rotate by horizon. Short horizons (1M/1Y) lean on
// Momentum + Valuation + Liquidity (things PSX price data CAN answer), so their
// scorable coverage is high. Long horizons (5Y/20Y) lean on
// Profitability + FinancialStrength + CashFlow + Dividend (things PSX does NOT
// publish), so long-horizon coverage is intentionally LOW and the report is
// honest about its low confidence rather than inventing fundamentals.
const WEIGHTS: Record<Duration, Record<CategoryKey, number>> = {
  "1M": { Valuation: 25, SizeLiquidity: 20, Momentum: 35, Profitability: 8, FinancialStrength: 5, CashFlow: 4, Dividend: 3 },
  "1Y": { Valuation: 28, SizeLiquidity: 17, Momentum: 25, Profitability: 12, FinancialStrength: 8, CashFlow: 6, Dividend: 4 },
  "3Y": { Valuation: 22, SizeLiquidity: 12, Momentum: 15, Profitability: 20, FinancialStrength: 16, CashFlow: 10, Dividend: 5 },
  "5Y": { Valuation: 18, SizeLiquidity: 10, Momentum: 10, Profitability: 24, FinancialStrength: 18, CashFlow: 12, Dividend: 8 },
  "20Y": { Valuation: 12, SizeLiquidity: 8, Momentum: 5, Profitability: 28, FinancialStrength: 22, CashFlow: 15, Dividend: 10 },
};

export function categoryWeights(duration: Duration): Record<CategoryKey, number> {
  // Return a copy so callers cannot mutate the policy table.
  return { ...WEIGHTS[duration] };
}

// Section scope. Core (price + snapshot driven) sections apply to every horizon.
// Statement / dividend / governance / forecast sections only render for horizons
// of 1Y or longer — a 1-month trading view does not surface multi-year sections.
const CORE_SECTIONS: SectionId[] = [1, 2, 7, 8, 12, 13, 14, 15, 16];
const LONG_HORIZON_SECTIONS: SectionId[] = [3, 4, 5, 6, 9, 10, 11, 17];

export function inScopeSections(duration: Duration): SectionId[] {
  const ids =
    duration === "1M"
      ? [...CORE_SECTIONS]
      : [...CORE_SECTIONS, ...LONG_HORIZON_SECTIONS];
  return ids.sort((a, b) => a - b);
}

// Price lookback (in trading days) for returns / drawdown per horizon.
const LOOKBACK_DAYS: Record<Duration, number> = {
  "1M": 21,
  "1Y": 252,
  "3Y": 756,
  "5Y": 1260,
  "20Y": 5040,
};

// Simple-moving-average window per horizon.
const SMA_WINDOW: Record<Duration, number> = {
  "1M": 10,
  "1Y": 50,
  "3Y": 100,
  "5Y": 200,
  "20Y": 200,
};

export function lookbackDays(duration: Duration): number {
  return LOOKBACK_DAYS[duration];
}

export function smaWindow(duration: Duration): number {
  return SMA_WINDOW[duration];
}
