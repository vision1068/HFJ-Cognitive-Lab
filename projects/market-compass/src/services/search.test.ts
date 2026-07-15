import { describe, it, expect, vi, beforeEach } from "vitest";
import frozenUniverse from "@/test/fixtures/universe-frozen.json";
import v1Search from "@/test/fixtures/yahoo-v1-search.json";
import type { UniverseSymbol } from "@/types";

const fetchJson = vi.fn();
vi.mock("@/services/http", () => ({
  YAHOO_BASE: "/api/yahoo",
  fetchJson: (url: string) => fetchJson(url),
}));

import { searchLocal, searchCompanies, normalize } from "./search";

const universe = frozenUniverse as UniverseSymbol[];

beforeEach(() => fetchJson.mockReset());

describe("FR-5 local fuzzy ranking (frozen universe)", () => {
  it.each([
    ["meezan", "MEBL"], // name (word) match
    ["OGD", "OGDC"], // ticker prefix (OGDC beats OGTI on alpha tie)
    ["hbl", "HBL"], // exact ticker
    ["lucky", "LUCK"], // name prefix
  ])("query %s -> top ticker %s", (q, expected) => {
    const results = searchLocal(q, universe);
    expect(results[0]?.ticker).toBe(expected);
  });

  it("returns nothing for a no-match query", () => {
    expect(searchLocal("zzzzzz", universe)).toEqual([]);
  });

  it("breaks ties deterministically by ticker (BANKX before BANKY)", () => {
    const results = searchLocal("bank", universe).filter((r) => r.ticker.startsWith("BANK"));
    expect(results.map((r) => r.ticker)).toEqual(["BANKX", "BANKY"]);
  });

  it("is diacritic-insensitive", () => {
    expect(normalize("Peñoles")).toBe("penoles");
  });
});

describe("FR-5 merged local + global search", () => {
  it("ranks local PSX hits above global Yahoo hits", async () => {
    fetchJson.mockResolvedValueOnce(v1Search);
    const results = await searchCompanies("meezan", universe);
    expect(results[0].ticker).toBe("MEBL");
    expect(results[0].source).toBe("psx");
  });

  it("surfaces global equities when there is no local match", async () => {
    fetchJson.mockResolvedValueOnce(v1Search);
    const results = await searchCompanies("apple", universe);
    expect(results.length).toBeGreaterThan(0);
    expect(results[0].ticker).toBe("AAPL");
    expect(results[0].source).toBe("yahoo");
  });

  it("degrades gracefully to local-only when the global path fails", async () => {
    fetchJson.mockRejectedValueOnce(new Error("yahoo down"));
    const results = await searchCompanies("hbl", universe);
    expect(results[0].ticker).toBe("HBL");
  });
});
