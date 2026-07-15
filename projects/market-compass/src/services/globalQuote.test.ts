import { describe, it, expect, vi, beforeEach } from "vitest";
import aaplChart from "@/test/fixtures/yahoo-v8-chart-AAPL.json";
import ogdcChart from "@/test/fixtures/yahoo-v8-chart-OGDC.KA.json";
import { runTechnicalAnalysis } from "@/lib/indicators";
import type { FmpCandle } from "@/lib/fmpClient";

const fetchJson = vi.fn();
vi.mock("@/services/http", () => ({
  YAHOO_BASE: "/api/yahoo",
  fetchJson: (url: string) => fetchJson(url),
}));

import { mapChartToCandles, hasRealOHLC, fetchGlobalCandles } from "./globalQuote";

beforeEach(() => fetchJson.mockReset());

describe("FR-8 v8 chart -> OHLCV candle mapping (real fixture)", () => {
  it("maps the AAPL chart to indicator-ready candles", () => {
    const candles = mapChartToCandles(aaplChart as any);
    expect(candles.length).toBeGreaterThanOrEqual(50);
    const c0 = candles[0];
    expect(c0.high).toBeGreaterThanOrEqual(c0.low);
    expect(typeof c0.date).toBe("string");
    expect(typeof c0.volume).toBe("number");
  });
});

describe("Auditor B4: technical indicators never run on a close-only series", () => {
  it("passes the guard for a real-OHLC series (AAPL and PSX OGDC.KA via Yahoo v8)", () => {
    expect(hasRealOHLC(mapChartToCandles(aaplChart as any))).toBe(true);
    expect(hasRealOHLC(mapChartToCandles(ogdcChart as any))).toBe(true);
  });

  it("trips the guard for a flattened close-only series (PSX-portal EOD shape)", () => {
    const real = mapChartToCandles(aaplChart as any);
    const flattened: FmpCandle[] = real.map((c) => ({ ...c, open: c.close, high: c.close, low: c.close }));
    expect(hasRealOHLC(flattened)).toBe(false);
  });

  it("runs the full analysis on a real-OHLC series but callers must gate on the guard first", () => {
    const real = mapChartToCandles(aaplChart as any);
    const analysis = runTechnicalAnalysis(real);
    expect(analysis.signal).toBeTruthy();
    expect(analysis.indicators.rsi14.length).toBe(real.length);
  });
});

describe("FR-8 keyless analysis inputs (no FMP key required)", () => {
  it("fetches candles purely from the /api/yahoo proxy with no VITE_FMP_API_KEY", async () => {
    // No FMP key is read anywhere on this path — it hits the same-origin proxy only.
    delete (import.meta as any).env?.VITE_FMP_API_KEY;
    fetchJson.mockResolvedValueOnce(aaplChart);
    const candles = await fetchGlobalCandles("AAPL", "1d", "1y");
    expect(candles.length).toBeGreaterThanOrEqual(50);
    expect(fetchJson).toHaveBeenCalledOnce();
    expect(fetchJson.mock.calls[0][0]).toContain("/api/yahoo/v8/finance/chart/AAPL");
  });
});
