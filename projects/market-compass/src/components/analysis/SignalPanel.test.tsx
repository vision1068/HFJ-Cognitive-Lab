import { describe, it, expect } from "vitest";
import { render, screen } from "@testing-library/react";
import { SignalPanel } from "./SignalPanel";

describe("SignalPanel (D3 auditor blocker)", () => {
  it("renders the neutral setup label, never imperative Buy/Sell wording", () => {
    render(
      <SignalPanel
        signal="Strong Buy"
        confidenceScore={82}
        marketStructure="uptrend"
        signalReasons={["MACD line is above the signal line (bullish momentum)."]}
      />
    );

    expect(screen.getByText("Strong Bullish setup")).toBeInTheDocument();
    // The raw imperative label must not appear anywhere in the panel.
    expect(screen.queryByText(/strong buy/i)).not.toBeInTheDocument();
    expect(document.body.textContent).not.toMatch(/\bbuy\b|\bsell\b/i);
  });

  it("shows a coarse confidence band, not a precise x/100 score", () => {
    render(
      <SignalPanel signal="Neutral" confidenceScore={82} marketStructure="ranging" signalReasons={[]} />
    );
    expect(screen.getByText(/Indicator agreement: High/)).toBeInTheDocument();
    expect(document.body.textContent).not.toMatch(/82\s*\/\s*100/);
  });

  it("carries the non-dismissible not-financial-advice note on the panel", () => {
    render(
      <SignalPanel signal="Sell" confidenceScore={30} marketStructure="downtrend" signalReasons={[]} />
    );
    const note = screen.getByRole("note");
    expect(note).toHaveTextContent(/not financial advice/i);
    expect(note).toHaveTextContent(/not a recommendation/i);
  });
});
