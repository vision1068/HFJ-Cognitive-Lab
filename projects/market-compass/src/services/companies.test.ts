import { describe, it, expect, vi, beforeEach } from "vitest";
import type { BatchQuote } from "@/types";
import type { FmpCandle } from "@/lib/fmpClient";

// Mock every data source so fetchCompanyDetail runs with NO network.
const fetchBatchQuotes = vi.fn();
const fetchGlobalCandles = vi.fn();
const fetchPsxEod = vi.fn();
const fetchPsxCompany = vi.fn();
const getFallbackUniverse = vi.fn();
const fetchUniverse = vi.fn();

vi.mock("./yahooQuote", () => ({ fetchBatchQuotes: (s: string[]) => fetchBatchQuotes(s) }));
vi.mock("./globalQuote", () => ({ fetchGlobalCandles: (s: string, i?: string, r?: string) => fetchGlobalCandles(s, i, r) }));
vi.mock("./psx", () => ({
  fetchPsxEod: (s: string) => fetchPsxEod(s),
  fetchPsxCompany: (s: string) => fetchPsxCompany(s),
}));
vi.mock("./universe", () => ({
  getFallbackUniverse: () => getFallbackUniverse(),
  fetchUniverse: () => fetchUniverse(),
}));

import { fetchCompanyDetail } from "./companies";

const CANDLES: FmpCandle[] = [
  { date: "2024-01-01", open: 1.0, high: 2.0, low: 0.5, close: 1.5, volume: 100 },
  { date: "2024-01-02", open: 1.5, high: 2.5, low: 1.0, close: 2.0, volume: 120 },
];

function ubl(): BatchQuote {
  return {
    symbol: "UBL.KA", name: "United Bank Limited (Yahoo longName)", price: 250, change: 2, changePercent: 0.8,
    trailingPE: 5, eps: 40, marketCap: 1e11, volume: null, week52High: 300, week52Low: 200,
    quoteType: "MUTUALFUND", currency: "PKR", exchange: "KAR", asOf: "2024-01-02T00:00:00.000Z", fundamentalsAvailable: true,
  };
}
function aapl(): BatchQuote {
  return {
    symbol: "AAPL", name: "Apple Inc.", price: 315, change: -1, changePercent: -0.3,
    trailingPE: 38, eps: 8.25, marketCap: 4e12, volume: 29000000, week52High: 317, week52Low: 201,
    quoteType: "EQUITY", currency: "USD", exchange: "NMS", asOf: "2024-01-02T00:00:00.000Z", fundamentalsAvailable: true,
  };
}

beforeEach(() => {
  fetchBatchQuotes.mockReset();
  fetchGlobalCandles.mockReset();
  fetchPsxEod.mockReset().mockRejectedValue(new Error("no portal"));
  fetchPsxCompany.mockReset().mockResolvedValue(null);
  getFallbackUniverse.mockReset().mockReturnValue([
    { symbol: "UBL", name: "United Bank Limited", sector: "COMMERCIAL BANKS" },
  ]);
  fetchUniverse.mockReset();
});

describe("FR-1 fetchCompanyDetail resolves a non-curated PSX symbol from Yahoo", () => {
  it("builds a PSX Company from the Karachi quote + v8 candles", async () => {
    fetchBatchQuotes.mockImplementation(async (s: string[]) =>
      s[0] === "UBL.KA" ? { quotes: [ubl()], missing: [] } : { quotes: [], missing: s }
    );
    fetchGlobalCandles.mockResolvedValue(CANDLES);

    const c = await fetchCompanyDetail("UBL");
    expect(c.ticker).toBe("UBL");
    expect(c.isPsx).toBe(true);
    expect(c.exchange).toBe("PSX");
    expect(c.currency).toBe("PKR");
    expect(c.name).toBe("United Bank Limited"); // from universe entry, not Yahoo longName
    expect(c.sector).toBe("COMMERCIAL BANKS");
    expect(c.price).toBe(250);
    expect(c.peRatio).toBe(5);
    expect(c.eps).toBe(40);
    expect(c.fundamentalsAvailable).toBe(true);
    expect(c.priceHistory).toHaveLength(2);
    expect(c.sparkline).toEqual([1.5, 2.0]);
    expect(c.statementsSource).toBeNull(); // PSX -> no free statements
    expect(fetchPsxEod).not.toHaveBeenCalled(); // Yahoo satisfied it; no portal call
  });
});

describe("FR-5/FR-6 fetchCompanyDetail resolves a global symbol", () => {
  it("falls through PSX and builds a global Company from Yahoo v7 + v8", async () => {
    fetchBatchQuotes.mockImplementation(async (s: string[]) => {
      if (s[0] === "AAPL.KA") return { quotes: [], missing: s }; // not a PSX security
      if (s[0] === "AAPL") return { quotes: [aapl()], missing: [] };
      return { quotes: [], missing: s };
    });
    fetchGlobalCandles.mockImplementation(async (sym: string) => (sym === "AAPL" ? CANDLES : []));

    const c = await fetchCompanyDetail("AAPL");
    expect(c.isPsx).toBe(false);
    expect(c.exchange).toBe("NMS");
    expect(c.currency).toBe("USD");
    expect(c.name).toBe("Apple Inc.");
    expect(c.price).toBe(315);
    expect(c.statementsSource).toBe("Yahoo Finance"); // global -> statements queried from Yahoo
    expect(c.sparkline).toEqual([1.5, 2.0]);
  });
});

describe("no fabrication: an unresolvable symbol still throws", () => {
  it("throws when a symbol resolves nowhere (PSX nor global)", async () => {
    fetchBatchQuotes.mockResolvedValue({ quotes: [], missing: ["NONSENSE"] });
    fetchGlobalCandles.mockResolvedValue([]);
    await expect(fetchCompanyDetail("NONSENSE")).rejects.toThrow(/Unknown ticker/);
  });
});
