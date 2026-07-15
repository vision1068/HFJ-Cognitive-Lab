import { describe, it, expect } from "vitest";
import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import type { Company, SearchResult } from "@/types";
import { SearchResultList } from "./SearchResultList";

function makeCompany(over: Partial<Company>): Company {
  return {
    ticker: "OGDC",
    psxSymbol: "OGDC",
    name: "Oil & Gas Development Company",
    logoInitials: "OG",
    logoColor: "#123456",
    exchange: "PSX",
    country: "Pakistan",
    sector: "Oil & Gas",
    industry: "Oil & Gas",
    currency: "PKR",
    description: "",
    website: "",
    ceo: "",
    price: 210.5,
    previousClose: 208,
    changeAbs: 2.5,
    changePercent: 1.2,
    dayOpen: null,
    dayHigh: null,
    dayLow: null,
    volume: null,
    week52High: null,
    week52Low: null,
    marketCap: null,
    peRatio: null,
    eps: null,
    sharesOutstanding: null,
    psxSector: null,
    sparkline: [],
    priceHistory: [],
    priceSource: "Yahoo Finance",
    fundamentalsSource: null,
    lastUpdated: new Date().toISOString(),
    asOf: null,
    quoteType: null,
    fundamentalsAvailable: false,
    statementsSource: null,
    ...over,
  };
}

describe("SearchResultList (F3 Smart Search source badges)", () => {
  it("renders a PSX badge for local hits and a Global badge for Yahoo hits", () => {
    const results: SearchResult[] = [
      { ticker: "OGDC", name: "Oil & Gas Development Company", exchange: "PSX", source: "psx", score: 1000 },
      { ticker: "AAPL", name: "Apple Inc.", exchange: "NASDAQ", source: "yahoo", score: 350 },
    ];
    const map = new Map<string, Company>([["OGDC", makeCompany({})]]);

    render(
      <MemoryRouter>
        <SearchResultList results={results} companyByTicker={map} />
      </MemoryRouter>
    );

    expect(screen.getByText("PSX")).toBeInTheDocument();
    expect(screen.getByText("Global")).toBeInTheDocument();
    // The PSX hit is enriched with a live price; the global hit has no local quote.
    expect(screen.getByText("Rs210.50")).toBeInTheDocument();
    expect(screen.getByText("No local quote")).toBeInTheDocument();
    // No "AI" wording anywhere in the result rows.
    expect(document.body.textContent).not.toMatch(/\bAI\b/);
  });
});
