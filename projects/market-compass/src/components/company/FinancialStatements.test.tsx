import { describe, it, expect } from "vitest";
import { render, screen } from "@testing-library/react";
import type { StatementSet } from "@/types";
import { FinancialStatements } from "./FinancialStatements";

describe("FinancialStatements (FR-7 honest N/A)", () => {
  it("renders an explicit 'not available' state with the source reason when available:false", () => {
    const psx: StatementSet = {
      symbol: "OGDC.KA",
      available: false,
      reason: "No free source publishes PSX financial statements.",
      currency: null,
      source: null,
      statements: [],
    };
    render(<FinancialStatements data={psx} currency="PKR" />);

    expect(screen.getByText(/Financial statements not available/i)).toBeInTheDocument();
    expect(screen.getByText(/No free source publishes PSX financial statements/i)).toBeInTheDocument();
    // No table rendered when unavailable.
    expect(screen.queryByRole("table")).not.toBeInTheDocument();
  });

  it("renders a multi-period table with real values, and N/A for null line items", () => {
    const set: StatementSet = {
      symbol: "AAPL",
      available: true,
      currency: "USD",
      source: "Yahoo Finance",
      statements: [
        {
          fiscalPeriodEnd: "2025-09-30",
          revenue: 416161000000,
          netIncome: 112010000000,
          totalAssets: null,
          operatingCashFlow: 120000000000,
          freeCashFlow: null,
        },
        {
          fiscalPeriodEnd: "2024-09-30",
          revenue: 391035000000,
          netIncome: 93736000000,
          totalAssets: 364980000000,
          operatingCashFlow: 118254000000,
          freeCashFlow: 108807000000,
        },
      ],
    };
    render(<FinancialStatements data={set} currency="PKR" />);

    expect(screen.getByRole("table")).toBeInTheDocument();
    expect(screen.getByText("Revenue")).toBeInTheDocument();
    expect(screen.getByText(/Statements source: Yahoo Finance/i)).toBeInTheDocument();
    // 416.16B revenue is formatted compactly.
    expect(screen.getByText("416.16B")).toBeInTheDocument();
    // A null line item is shown as N/A, never as 0.
    expect(screen.getAllByText("N/A").length).toBeGreaterThan(0);
  });
});
