// AC-3 / AC-9: horizon policy is versioned and internally consistent. Short vs
// long horizons differ in both in-scope sections and weight columns, and every
// duration's weight column sums to the fixed constant.

import { describe, it, expect } from "vitest";
import {
  categoryWeights,
  inScopeSections,
  WEIGHT_TOTAL,
  POLICY_VERSION,
  CATEGORY_ORDER,
} from "./durationPolicy";
import type { Duration } from "./types";

const DURATIONS: Duration[] = ["1M", "1Y", "3Y", "5Y", "20Y"];

describe("AC-9 durationPolicy: every weight column sums to the constant", () => {
  it.each(DURATIONS)("%s weights sum to WEIGHT_TOTAL", (d) => {
    const w = categoryWeights(d);
    const sum = CATEGORY_ORDER.reduce((acc, k) => acc + w[k], 0);
    expect(sum).toBe(WEIGHT_TOTAL);
  });

  it("exposes a stable policy version", () => {
    expect(POLICY_VERSION).toBe("1.0.0");
  });
});

describe("AC-3 durationPolicy: 1M and 20Y differ", () => {
  it("in-scope section sets differ between 1M and 20Y", () => {
    const short = inScopeSections("1M");
    const long = inScopeSections("20Y");
    expect(short).not.toEqual(long);
    // long horizon adds the statement / forecast sections
    expect(long.length).toBeGreaterThan(short.length);
    expect(long).toContain(17); // Forward Guidance in scope for 20Y
    expect(short).not.toContain(17); // ...but not for a 1-month view
  });

  it("weight columns differ: 1M leans Momentum, 20Y leans Profitability", () => {
    const short = categoryWeights("1M");
    const long = categoryWeights("20Y");
    expect(short).not.toEqual(long);
    expect(short.Momentum).toBeGreaterThan(long.Momentum);
    expect(long.Profitability).toBeGreaterThan(short.Profitability);
  });

  it("short-horizon scorable weight is higher than long-horizon (coverage intent)", () => {
    const short = categoryWeights("1M");
    const long = categoryWeights("20Y");
    const scorable = (w: Record<string, number>) => w.Valuation + w.SizeLiquidity + w.Momentum;
    expect(scorable(short)).toBeGreaterThan(scorable(long));
  });
});
