// AC-7: the Article-1 backstop. A raw / unsourced / non-finite figure is coerced
// to `na`; a fully-sourced finite value passes. This is the load-bearing test —
// it proves a rendered number cannot exist without source + asOf.

import { describe, it, expect } from "vitest";
import { sourced, na, guard, isValue, isNa } from "./realDataGuard";
import type { SourcedFigure } from "./types";

const VALID_ASOF = "2026-07-14";

describe("AC-7 realDataGuard: sourced() requires a real source + asOf", () => {
  it("passes a fully-sourced finite value", () => {
    const f = sourced(12.5, { source: "PSX Data Portal", asOf: VALID_ASOF, provenance: "fact" });
    expect(isValue(f)).toBe(true);
    if (isValue(f)) {
      expect(f.value).toBe(12.5);
      expect(f.source).toBe("PSX Data Portal");
      expect(f.asOf).toBe(VALID_ASOF);
    }
  });

  it("coerces an empty-string source to na", () => {
    const f = sourced(10, { source: "", asOf: VALID_ASOF, provenance: "fact" });
    expect(isNa(f)).toBe(true);
  });

  it("coerces a whitespace-only source to na", () => {
    const f = sourced(10, { source: "   ", asOf: VALID_ASOF, provenance: "fact" });
    expect(isNa(f)).toBe(true);
  });

  it("coerces an empty-string asOf to na", () => {
    const f = sourced(10, { source: "PSX Data Portal", asOf: "", provenance: "fact" });
    expect(isNa(f)).toBe(true);
  });

  it("coerces an invalid asOf date to na", () => {
    const f = sourced(10, { source: "PSX Data Portal", asOf: "not-a-date", provenance: "fact" });
    expect(isNa(f)).toBe(true);
  });

  it("coerces NaN / Infinity values to na", () => {
    expect(isNa(sourced(NaN, { source: "PSX", asOf: VALID_ASOF, provenance: "fact" }))).toBe(true);
    expect(isNa(sourced(Infinity, { source: "PSX", asOf: VALID_ASOF, provenance: "fact" }))).toBe(true);
  });
});

describe("AC-7 guard(): coerces a hand-built unsourced value figure", () => {
  it("demotes a number-only value figure missing source/asOf", () => {
    // Simulates a figure built by bypassing `sourced` (the failure mode guard exists for).
    const raw = { kind: "value", value: 42, source: "", asOf: "", provenance: "fact" } as SourcedFigure;
    const g = guard(raw);
    expect(isNa(g)).toBe(true);
  });

  it("is idempotent on an na figure", () => {
    const n = na("no PSX source");
    expect(guard(n)).toEqual(n);
  });
});
