// AC-10: horizon-coverage honesty. A duration's nominal lookback (e.g. 5040
// trading days for 20Y) can exceed the PSX price history actually available.
// Horizon-based sections (13-16) must disclose that mismatch rather than
// silently labeling a short series as if it covered the full horizon.

import { describe, it, expect } from "vitest";
import type { Company, PricePoint } from "@/types";
import type { PredictContext } from "./dataBinding";
import { SECTION_BUILDERS } from "./sectionRegistry";

function makeHistory(n: number): PricePoint[] {
  const out: PricePoint[] = [];
  const base = new Date("2020-01-01").getTime();
  for (let i = 0; i < n; i++) {
    const close = 100 + i * 0.167;
    out.push({
      date: new Date(base + i * 86_400_000).toISOString().slice(0, 10),
      open: close, high: close * 1.01, low: close * 0.99, close, volume: 1_000_000,
    });
  }
  return out;
}

function makeCompany(historyDays: number): Company {
  return {
    ticker: "OGDC", psxSymbol: "OGDC", name: "Oil & Gas Development Company",
    logoInitials: "OG", logoColor: "#b45309", exchange: "PSX", country: "Pakistan",
    isPsx: true, sector: "Oil & Gas", industry: "Exploration & Production", currency: "PKR",
    description: "", website: "ogdcl.com", ceo: "Ahmed Hayat Lak",
    price: 150, previousClose: 149, changeAbs: 1, changePercent: 0.67,
    dayOpen: 149, dayHigh: 151, dayLow: 148, volume: 2_000_000,
    week52High: 160, week52Low: 100,
    marketCap: 300e9, peRatio: 6, eps: 25, sharesOutstanding: 2e9, psxSector: "OIL & GAS",
    sparkline: [], priceHistory: makeHistory(historyDays),
    priceSource: "PSX Data Portal", fundamentalsSource: "PSX Data Portal",
    lastUpdated: "2026-07-15T09:00:00.000Z", asOf: "2026-07-15T09:00:00.000Z",
    quoteType: "EQUITY", fundamentalsAvailable: true, statementsSource: null,
  };
}

function makeCtx(duration: PredictContext["duration"], historyDays: number): PredictContext {
  const company = makeCompany(historyDays);
  const last = company.priceHistory[company.priceHistory.length - 1].date;
  return {
    ticker: "OGDC", duration, company,
    peers: [], peerMedianPE: 8, peerMedianMarketCap: 250e9,
    priceAsOf: last, snapshotAsOf: company.lastUpdated,
    priceSource: "PSX Data Portal", snapshotSource: "PSX Data Portal (snapshot)",
  };
}

describe("AC-10 horizon coverage disclosure: sections 13-16", () => {
  it("discloses truncated history on a 20Y horizon with only ~300 days of data", () => {
    const ctx = makeCtx("20Y", 300); // 299 available days vs. 5040 nominal for 20Y
    for (const id of [13, 14, 15, 16] as const) {
      const section = SECTION_BUILDERS[id](ctx);
      expect(section.note, `section ${id} note`).toMatch(/only 299 day\(s\) of PSX price history/);
      expect(section.note).toMatch(/not the full 20Y window/);
    }
  });

  it("adds no coverage caveat on a 1Y horizon when 300 days of data comfortably cover it", () => {
    const ctx = makeCtx("1Y", 300); // 299 available days vs. 252 nominal for 1Y
    const s13 = SECTION_BUILDERS[13](ctx);
    const s16 = SECTION_BUILDERS[16](ctx);
    expect(s13.note).not.toMatch(/only \d+ day\(s\) of PSX price history/);
    expect(s16.note).toBeUndefined();
  });
});
