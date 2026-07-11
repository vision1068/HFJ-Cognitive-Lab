import { describe, it, expect, vi } from "vitest";
import v10Success from "@/test/fixtures/yahoo-v10-quoteSummary-AAPL.json";
import v10NotFound from "@/test/fixtures/yahoo-v10-quoteSummary-404.json";

vi.mock("@/services/http", () => ({
  YAHOO_BASE: "/api/yahoo",
  fetchJson: vi.fn(),
}));

import { parseStatements, isPsxSymbol, fetchStatements } from "./statements";

describe("FR-7 global statements parsing (real fixture)", () => {
  it("parses multi-year revenue + net income newest-first", () => {
    const set = parseStatements("AAPL", v10Success as any);
    expect(set.available).toBe(true);
    expect(set.source).toBe("Yahoo Finance");
    expect(set.statements.length).toBe(4);
    // Newest period first.
    expect(set.statements[0].fiscalPeriodEnd).toBe("2025-09-30");
    expect(set.statements[0].revenue).toBe(416161000000);
    expect(set.statements[0].netIncome).toBe(112010000000);
    expect(set.statements[3].fiscalPeriodEnd).toBe("2022-09-30");
    expect(set.statements[3].revenue).toBe(394328000000);
  });
});

describe("FR-7 honest N/A (never fabricated)", () => {
  it("returns available:false for the 404 'No fundamentals' body", () => {
    const set = parseStatements("OGDC.KA", v10NotFound as any);
    expect(set.available).toBe(false);
    expect(set.statements).toEqual([]);
    expect(set.reason).toBeTruthy();
  });

  it("short-circuits PSX .KA symbols to honest N/A without a network call", async () => {
    expect(isPsxSymbol("OGDC.KA")).toBe(true);
    expect(isPsxSymbol("AAPL")).toBe(false);
    const set = await fetchStatements("OGDC.KA");
    expect(set.available).toBe(false);
    expect(set.reason).toMatch(/PSX/i);
  });
});
