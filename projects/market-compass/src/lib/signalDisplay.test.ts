import { describe, it, expect } from "vitest";
import type { SignalStrength } from "@/lib/indicators";
import { signalLabel, signalTone, confidenceBand, SIGNAL_ADVICE_NOTE } from "./signalDisplay";

describe("D3 signal display mapping (no imperative wording)", () => {
  const all: SignalStrength[] = ["Strong Buy", "Buy", "Neutral", "Sell", "Strong Sell"];

  it("maps every raw signal to a neutral 'setup' label with no Buy/Sell words", () => {
    for (const s of all) {
      const label = signalLabel(s);
      expect(label).not.toMatch(/buy|sell/i);
    }
    expect(signalLabel("Strong Buy")).toBe("Strong Bullish setup");
    expect(signalLabel("Buy")).toBe("Bullish setup");
    expect(signalLabel("Neutral")).toBe("Neutral");
    expect(signalLabel("Sell")).toBe("Bearish setup");
    expect(signalLabel("Strong Sell")).toBe("Strong Bearish setup");
  });

  it("assigns bullish tone positive, bearish tone negative", () => {
    expect(signalTone("Strong Buy")).toBe("positive");
    expect(signalTone("Sell")).toBe("negative");
    expect(signalTone("Neutral")).toBe("neutral");
  });

  it("collapses a precise score into a coarse Low/Moderate/High band", () => {
    expect(confidenceBand(10)).toBe("Low");
    expect(confidenceBand(39)).toBe("Low");
    expect(confidenceBand(40)).toBe("Moderate");
    expect(confidenceBand(69)).toBe("Moderate");
    expect(confidenceBand(70)).toBe("High");
    expect(confidenceBand(100)).toBe("High");
  });

  it("advice note is present and non-advisory", () => {
    expect(SIGNAL_ADVICE_NOTE).toMatch(/not financial advice/i);
    expect(SIGNAL_ADVICE_NOTE).toMatch(/not a recommendation/i);
  });
});
