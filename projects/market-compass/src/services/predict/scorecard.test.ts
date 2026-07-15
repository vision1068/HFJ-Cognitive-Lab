// AC-score: scorecard math. N/A categories are excluded and weights renormalized
// (a missing input does NOT zero-deflate the total); coverage is exact on a
// fixture; verdict bands hit their boundaries; computeScorecard is deterministic.

import { describe, it, expect } from "vitest";
import {
  assembleScorecard,
  verdictFor,
  computeScorecard,
  type RawCategoryScores,
} from "./scorecard";
import type { CategoryKey } from "./durationPolicy";
import type { PredictContext } from "./dataBinding";
import type { Company, PricePoint } from "@/types";

function weights(
  v: number, s: number, m: number, p: number, f: number, cf: number, d: number,
): Record<CategoryKey, number> {
  return {
    Valuation: v, SizeLiquidity: s, Momentum: m,
    Profitability: p, FinancialStrength: f, CashFlow: cf, Dividend: d,
  };
}

function scores(partial: Partial<RawCategoryScores>): RawCategoryScores {
  return {
    Valuation: null, SizeLiquidity: null, Momentum: null,
    Profitability: null, FinancialStrength: null, CashFlow: null, Dividend: null,
    ...partial,
  };
}

describe("AC-score assembleScorecard: renormalization over scorable weights", () => {
  it("a single scored category is not zero-deflated by absent categories", () => {
    const sc = assembleScorecard(scores({ Valuation: 8 }), weights(25, 20, 35, 8, 5, 4, 3));
    // Only Valuation scored -> total is purely its 8/10, regardless of its weight.
    expect(sc.total100).toBe(80);
    expect(sc.coverage).toBe(0.25); // 25 / 100
    expect(sc.lowConfidence).toBe(true);
    // N/A categories carry a reason, not a zero score.
    const prof = sc.categories.find((c) => c.key === "Profitability");
    expect(prof?.score10).toBeNull();
    expect(prof?.naReason).toBeTruthy();
  });

  it("computes an exact weighted total + coverage over three scorable categories", () => {
    const sc = assembleScorecard(
      scores({ Valuation: 8, SizeLiquidity: 6, Momentum: 4 }),
      weights(25, 20, 35, 8, 5, 4, 3),
    );
    // (0.8*25 + 0.6*20 + 0.4*35) / 80 * 100 = 46/80*100 = 57.5
    expect(sc.total100).toBe(57.5);
    expect(sc.coverage).toBe(0.8); // 80 / 100
    expect(sc.lowConfidence).toBe(false);
    expect(sc.verdict).toBe("Hold");
  });

  it("returns total 0, zero coverage, and Not Rated (not Strong Sell) when nothing is scorable", () => {
    const sc = assembleScorecard(scores({}), weights(25, 20, 35, 8, 5, 4, 3));
    expect(sc.total100).toBe(0);
    expect(sc.coverage).toBe(0);
    expect(sc.verdict).toBe("Not Rated");
  });
});

describe("AC-score verdictFor: band boundaries", () => {
  it.each([
    [80, "Strong Buy"],
    [79, "Buy"],
    [65, "Buy"],
    [64, "Hold"],
    [45, "Hold"],
    [44, "Sell"],
    [30, "Sell"],
    [29, "Strong Sell"],
    [0, "Strong Sell"],
  ] as const)("total %i -> %s", (total, verdict) => {
    expect(verdictFor(total)).toBe(verdict);
  });
});

// ---- deterministic integration fixture -------------------------------------

function makeHistory(n: number, start: number, step: number): PricePoint[] {
  const out: PricePoint[] = [];
  const base = new Date("2020-01-01").getTime();
  for (let i = 0; i < n; i++) {
    const close = start + i * step;
    out.push({
      date: new Date(base + i * 86_400_000).toISOString().slice(0, 10),
      open: close, high: close * 1.01, low: close * 0.99, close, volume: 1_000_000,
    });
  }
  return out;
}

function makeCompany(): Company {
  return {
    ticker: "TEST", psxSymbol: "TEST", name: "Test Co", logoInitials: "TC", logoColor: "#000",
    exchange: "PSX", country: "Pakistan", isPsx: true, sector: "Banking", industry: "Commercial Banking",
    currency: "PKR", description: "", website: "", ceo: "",
    price: 150, previousClose: 149, changeAbs: 1, changePercent: 0.67,
    dayOpen: 149, dayHigh: 151, dayLow: 148, volume: 2_000_000,
    week52High: 160, week52Low: 100,
    marketCap: 300e9, peRatio: 6, eps: 25, sharesOutstanding: 2e9, psxSector: "COMMERCIAL BANKS",
    sparkline: [], priceHistory: makeHistory(300, 100, 0.167),
    priceSource: "PSX Data Portal", fundamentalsSource: "PSX Data Portal",
    lastUpdated: "2026-07-15T09:00:00.000Z", asOf: "2026-07-15T09:00:00.000Z",
    quoteType: "EQUITY", fundamentalsAvailable: true, statementsSource: null,
  };
}

function makeCtx(): PredictContext {
  const company = makeCompany();
  const last = company.priceHistory[company.priceHistory.length - 1].date;
  return {
    ticker: "TEST", duration: "1Y", company,
    peers: [], peerMedianPE: 9, peerMedianMarketCap: 250e9,
    priceAsOf: last, snapshotAsOf: company.lastUpdated,
    priceSource: "PSX Data Portal", snapshotSource: "PSX Data Portal (snapshot)",
  };
}

describe("AC-score computeScorecard: deterministic + only PSX-scorable categories", () => {
  it("produces identical output on repeated calls (no Date.now in the math)", () => {
    const a = computeScorecard(makeCtx());
    const b = computeScorecard(makeCtx());
    expect(a).toEqual(b);
  });

  it("scores Valuation/Size/Momentum and marks the rest N/A", () => {
    const sc = computeScorecard(makeCtx());
    const byKey = Object.fromEntries(sc.categories.map((c) => [c.key, c]));
    expect(byKey.Valuation.score10).not.toBeNull();
    expect(byKey.SizeLiquidity.score10).not.toBeNull();
    expect(byKey.Momentum.score10).not.toBeNull();
    expect(byKey.Profitability.score10).toBeNull();
    expect(byKey.CashFlow.score10).toBeNull();
    expect(byKey.Dividend.score10).toBeNull();
    // 1Y scorable weight = 28+17+25 = 70 -> coverage 0.7
    expect(sc.coverage).toBe(0.7);
  });
});
