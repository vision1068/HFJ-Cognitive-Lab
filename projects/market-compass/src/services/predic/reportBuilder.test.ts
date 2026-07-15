// AC-1 / AC-2 / AC-8: the assembled report. Every rendered value carries source
// + asOf (walk it, assert zero unguarded numbers); the statement/governance
// sections render as honest na; the report is stamped with both versions and a
// generatedAt.

import { describe, it, expect, vi, beforeEach } from "vitest";
import type { Company, PricePoint } from "@/types";
import type { PredicContext } from "./dataBinding";

const buildContext = vi.fn();
vi.mock("./dataBinding", () => ({
  buildContext: (t: string, d: string) => buildContext(t, d),
}));

import { generatePredicReport } from "./reportBuilder";
import { isValue } from "./realDataGuard";

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

function makeCompany(): Company {
  return {
    ticker: "OGDC", psxSymbol: "OGDC", name: "Oil & Gas Development Company",
    logoInitials: "OG", logoColor: "#b45309", exchange: "PSX", country: "Pakistan",
    isPsx: true, sector: "Oil & Gas", industry: "Exploration & Production", currency: "PKR",
    description: "", website: "ogdcl.com", ceo: "Ahmed Hayat Lak",
    price: 150, previousClose: 149, changeAbs: 1, changePercent: 0.67,
    dayOpen: 149, dayHigh: 151, dayLow: 148, volume: 2_000_000,
    week52High: 160, week52Low: 100,
    marketCap: 300e9, peRatio: 6, eps: 25, sharesOutstanding: 2e9, psxSector: "OIL & GAS",
    sparkline: [], priceHistory: makeHistory(300),
    priceSource: "PSX Data Portal", fundamentalsSource: "PSX Data Portal",
    lastUpdated: "2026-07-15T09:00:00.000Z", asOf: "2026-07-15T09:00:00.000Z",
    quoteType: "EQUITY", fundamentalsAvailable: true, statementsSource: null,
  };
}

function makeCtx(): PredicContext {
  const company = makeCompany();
  const last = company.priceHistory[company.priceHistory.length - 1].date;
  return {
    ticker: "OGDC", duration: "1Y", company,
    peers: [{ ticker: "PPL", name: "Pakistan Petroleum", sector: "Oil & Gas", price: 120, peRatio: 8, marketCap: 250e9, asOf: company.lastUpdated }],
    peerMedianPE: 8, peerMedianMarketCap: 250e9,
    priceAsOf: last, snapshotAsOf: company.lastUpdated,
    priceSource: "PSX Data Portal", snapshotSource: "PSX Data Portal (snapshot)",
  };
}

beforeEach(() => {
  buildContext.mockReset().mockImplementation(async () => makeCtx());
});

describe("AC-1 report: no rendered value figure lacks source + asOf", () => {
  it("every value figure across the whole report is guarded", async () => {
    const report = await generatePredicReport("OGDC", "1Y");
    let valueCount = 0;
    for (const section of report.sections) {
      for (const { figure } of section.figures) {
        if (isValue(figure)) {
          valueCount++;
          expect(figure.source.trim().length).toBeGreaterThan(0);
          expect(Number.isFinite(Date.parse(figure.asOf))).toBe(true);
          expect(Number.isFinite(figure.value)).toBe(true);
        }
      }
    }
    // Sanity: the fixture yields real, sourced figures (not an all-na report).
    expect(valueCount).toBeGreaterThan(0);
  });

  it("price-derived figures are dated by the EOD date, not fetch time", async () => {
    const report = await generatePredicReport("OGDC", "1Y");
    const overview = report.sections.find((s) => s.id === 1);
    const lastPrice = overview?.figures.find((f) => f.label === "Last Price")?.figure;
    expect(lastPrice && isValue(lastPrice)).toBe(true);
    if (lastPrice && isValue(lastPrice)) {
      expect(lastPrice.source).toBe("PSX Data Portal");
      // asOf is the last priceHistory date (a plain yyyy-mm-dd), not the ISO fetch time.
      expect(lastPrice.asOf).toMatch(/^\d{4}-\d{2}-\d{2}$/);
    }
  });
});

describe("AC-2 report: statement/governance sections render honest na", () => {
  it("renders sections 3, 4, 5, 6, 9 as na", async () => {
    const report = await generatePredicReport("OGDC", "1Y");
    for (const id of [3, 4, 5, 6, 9]) {
      const s = report.sections.find((sec) => sec.id === id);
      expect(s, `section ${id} present`).toBeDefined();
      expect(s?.status).toBe("na");
    }
    // ...and they appear in the naSections index with reasons.
    const naIds = report.naSections.map((n) => n.id);
    for (const id of [3, 4, 5, 6, 9]) expect(naIds).toContain(id);
    for (const n of report.naSections) expect(n.reason.length).toBeGreaterThan(0);
  });
});

describe("AC-8 report: carries registry + policy versions and generatedAt", () => {
  it("stamps versions and a valid ISO generatedAt", async () => {
    const report = await generatePredicReport("OGDC", "1Y");
    expect(report.registryVersion).toBe("1.0.0");
    expect(report.policyVersion).toBe("1.0.0");
    expect(Number.isFinite(Date.parse(report.generatedAt))).toBe(true);
    expect(report.scorecard.total100).toBeGreaterThanOrEqual(0);
    expect(report.coverage).toBe(report.scorecard.coverage);
  });
});
