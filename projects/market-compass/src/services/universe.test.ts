import { describe, it, expect, vi, beforeEach } from "vitest";
import symbolsFixture from "@/test/fixtures/psx-symbols.json";

const fetchJson = vi.fn();
vi.mock("@/services/http", () => ({
  PSX_BASE: "/api/psx",
  fetchJson: (url: string) => fetchJson(url),
}));

import { parsePsxSymbols, getFallbackUniverse, fetchUniverse } from "./universe";

beforeEach(() => fetchJson.mockReset());

describe("FR-4 PSX /symbols parser", () => {
  it("turns the committed /symbols fixture into correctly-shaped equity rows", () => {
    const rows = parsePsxSymbols(symbolsFixture);
    expect(rows.length).toBeGreaterThanOrEqual(1);
    const ogdc = rows.find((r) => r.symbol === "OGDC");
    expect(ogdc).toEqual({
      symbol: "OGDC",
      name: "Oil & Gas Development Company Limited",
      sector: "OIL & GAS EXPLORATION COMPANIES",
    });
    // ETFs, debt and futures (e.g. "MEBL-OCT") are filtered out.
    expect(rows.some((r) => r.symbol.includes("-"))).toBe(false);
    expect(rows.some((r) => r.symbol === "MZNPETF")).toBe(false); // isETF
    expect(rows.some((r) => r.symbol === "NBP")).toBe(false); // isDebt
  });

  it("returns [] for a non-array / garbage payload", () => {
    expect(parsePsxSymbols({ nope: true })).toEqual([]);
    expect(parsePsxSymbols(null)).toEqual([]);
  });
});

describe("FR-4 committed fallback universe", () => {
  it("has >= 400 rows with the correct shape", () => {
    const fallback = getFallbackUniverse();
    expect(fallback.length).toBeGreaterThanOrEqual(400);
    for (const row of fallback.slice(0, 20)) {
      expect(typeof row.symbol).toBe("string");
      expect(typeof row.name).toBe("string");
      expect(typeof row.sector).toBe("string");
    }
  });
});

describe("FR-4 loader fallback behaviour", () => {
  it("falls back to the committed seed when the live endpoint parse fails", async () => {
    fetchJson.mockResolvedValueOnce({ garbage: "not an array" });
    const universe = await fetchUniverse();
    expect(universe.length).toBeGreaterThanOrEqual(400);
    expect(universe).toEqual(getFallbackUniverse());
  });

  it("falls back when the live endpoint throws", async () => {
    fetchJson.mockRejectedValueOnce(new Error("network down"));
    const universe = await fetchUniverse();
    expect(universe).toEqual(getFallbackUniverse());
  });
});
