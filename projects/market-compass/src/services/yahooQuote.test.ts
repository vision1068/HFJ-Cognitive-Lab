import { describe, it, expect, vi, beforeEach } from "vitest";
import kaFixture from "@/test/fixtures/yahoo-v7-quote-ka.json";
import globalFixture from "@/test/fixtures/yahoo-v7-quote-global.json";
import filterFixture from "@/test/fixtures/yahoo-v7-quote-filter.json";

// Mock the same-origin proxy fetch so no network is touched.
const fetchJson = vi.fn();
vi.mock("@/services/http", () => ({
  YAHOO_BASE: "/api/yahoo",
  fetchJson: (url: string) => fetchJson(url),
}));

import { mapV7Row, fundamentalsValid, fetchBatchQuotes } from "./yahooQuote";

const kaRows = (kaFixture as any).quoteResponse.result;
const filterRows = (filterFixture as any).quoteResponse.result;
const globalRows = (globalFixture as any).quoteResponse.result;

beforeEach(() => fetchJson.mockReset());

describe("FR-2 mapV7Row mapper correctness (exact values from committed fixture)", () => {
  it("maps the OGDC.KA row field-for-field", () => {
    const ogdc = mapV7Row(kaRows[0]);
    expect(ogdc.symbol).toBe("OGDC.KA");
    expect(ogdc.name).toBe("Oil and Gas Development Company Limited");
    expect(ogdc.price).toBe(131.87);
    expect(ogdc.changePercent).toBeCloseTo(1.641737, 4);
    expect(ogdc.trailingPE).toBeCloseTo(3.6468472, 4);
    expect(ogdc.eps).toBe(36.16);
    expect(ogdc.marketCap).toBe(567163420672);
    expect(ogdc.week52High).toBe(352.0);
    expect(ogdc.week52Low).toBe(220.99);
    expect(ogdc.quoteType).toBe("MUTUALFUND");
    expect(ogdc.currency).toBe("PKR");
    expect(ogdc.exchange).toBe("KAR");
    // v7 PSX rows carry no regularMarketVolume -> null (never fabricated).
    expect(ogdc.volume).toBeNull();
    // asOf derived from regularMarketTime (1721764800).
    expect(ogdc.asOf).toBe(new Date(1721764800 * 1000).toISOString());
  });
});

describe("FR-3 EQUITY/PKR fundamentals filter", () => {
  it("accepts PSX Karachi PKR rows even though Yahoo tags them MUTUALFUND", () => {
    // Real-data fact: every PSX .KA row is quoteType MUTUALFUND; we pin on KAR+PKR.
    expect(fundamentalsValid({ quoteType: "MUTUALFUND", currency: "PKR", exchange: "KAR" })).toBe(true);
    const ogdc = mapV7Row(kaRows[0]);
    expect(ogdc.fundamentalsAvailable).toBe(true);
    expect(ogdc.trailingPE).not.toBeNull();
  });

  it("nulls fundamentals for a global non-EQUITY (ETF) row", () => {
    const spy = filterRows.find((r: any) => r.symbol === "SPY");
    const mapped = mapV7Row(spy);
    expect(mapped.fundamentalsAvailable).toBe(false);
    expect(mapped.trailingPE).toBeNull();
    expect(mapped.eps).toBeNull();
    expect(mapped.marketCap).toBeNull();
    // Price/change are still valid — only fundamentals are gated.
    expect(mapped.price).toBe(500.12);
  });

  it("accepts a genuine global EQUITY row", () => {
    const aapl = mapV7Row(globalRows[0]);
    expect(aapl.quoteType).toBe("EQUITY");
    expect(aapl.fundamentalsAvailable).toBe(true);
    expect(aapl.volume).toBe(29556052);
  });

  it("rejects a non-PKR Karachi-less row", () => {
    expect(fundamentalsValid({ quoteType: "MUTUALFUND", currency: "USD", exchange: "NMS" })).toBe(false);
  });
});

describe("FR-3 requested-vs-returned reconciliation", () => {
  it("reports requested symbols Yahoo silently dropped", async () => {
    fetchJson.mockResolvedValueOnce({
      quoteResponse: { result: [kaRows[0], kaRows[1]], error: null },
    });
    const { quotes, missing } = await fetchBatchQuotes(["OGDC.KA", "MEBL.KA", "ZZZZ.KA"]);
    expect(quotes.map((q) => q.symbol)).toEqual(["OGDC.KA", "MEBL.KA"]);
    expect(missing).toEqual(["ZZZZ.KA"]);
  });
});

describe("FR-2 / NFR-2 batch call-count bound (<= ceil(N/50))", () => {
  // Yahoo receives at most one call per 50 requested symbols.
  it.each([
    [0, 0],
    [1, 1],
    [50, 1],
    [51, 2],
  ])("N=%i -> %i upstream call(s)", async (n, expectedCalls) => {
    fetchJson.mockImplementation(async () => ({ quoteResponse: { result: [], error: null } }));
    const symbols = Array.from({ length: n }, (_, i) => `SYM${i}.KA`);
    await fetchBatchQuotes(symbols);
    expect(fetchJson).toHaveBeenCalledTimes(expectedCalls);
  });
});
