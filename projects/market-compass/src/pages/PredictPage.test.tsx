import { describe, it, expect, beforeEach, vi } from "vitest";
import { render, screen, within, waitFor, fireEvent } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter } from "react-router-dom";
import type { Mock } from "vitest";
import type { Duration, PredictReport } from "@/services/predict/types";

// Mock the ENGINE — the page/report UI is what is under test here, not the
// report builder (which has its own 35 green tests).
vi.mock("@/services/predict/reportBuilder", () => ({
  generatePredictReport: vi.fn(),
}));

import { generatePredictReport } from "@/services/predict/reportBuilder";
import { PredictPage } from "./PredictPage";

const mockGenerate = generatePredictReport as unknown as Mock;

// A report that mixes a real (full) section with an explicit na section so the
// "honest N/A, never a number" behaviour is exercisable.
function makeReport(duration: Duration): PredictReport {
  return {
    ticker: "ENGROH",
    companyName: "Engro Holdings Limited",
    duration,
    generatedAt: "2026-07-15T10:00:00.000Z",
    registryVersion: "r1",
    policyVersion: "p1",
    sections: [
      {
        id: 1,
        title: "Price Performance",
        status: "full",
        figures: [
          {
            label: "Return",
            figure: {
              kind: "value",
              value: 12.5,
              unit: "%",
              source: "PSX Data Portal",
              asOf: "2026-07-14",
              provenance: "derived",
            },
          },
        ],
      },
      {
        id: 7,
        title: "Dividends",
        status: "na",
        // A value figure is intentionally present; the na-status SectionCard must
        // NOT leak this number — it renders the reason instead.
        figures: [
          {
            label: "Dividend yield",
            figure: {
              kind: "value",
              value: 999,
              unit: "%",
              source: "should-not-render",
              asOf: "2026-07-14",
              provenance: "fact",
            },
          },
        ],
        note: "PSX Data Portal does not publish a dividend history feed.",
      },
    ],
    scorecard: {
      total100: 62,
      verdict: "Buy",
      categories: [
        { key: "momentum", label: "Momentum", score10: 7.4, weight: 1 },
        { key: "dividends", label: "Dividends", score10: null, naReason: "No dividend feed", weight: 1 },
      ],
      coverage: 0.5,
      lowConfidence: false,
    },
    coverage: 0.5,
    naSections: [
      { id: 7, title: "Dividends", reason: "PSX Data Portal does not publish a dividend history feed." },
    ],
  };
}

function renderPage() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={client}>
      <MemoryRouter>
        <PredictPage />
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe("PredictPage (Predict panel — honest N/A + disclaimer)", () => {
  beforeEach(() => {
    mockGenerate.mockReset();
    mockGenerate.mockImplementation((_t: string, d: Duration) => Promise.resolve(makeReport(d)));
  });

  it("(a) renders an na section as 'Data not available' and does NOT leak its figure number", async () => {
    renderPage();

    const naSection = await screen.findByRole("region", { name: "Dividends" });
    // Explicit unavailable text is present…
    expect(within(naSection).getByText(/Data not available/i)).toBeInTheDocument();
    // …and the underlying figure value is never rendered as a stand-in.
    expect(within(naSection).queryByText(/999/)).not.toBeInTheDocument();
  });

  it("(b) shows the not-financial-advice disclaimer in the same view as the verdict", async () => {
    renderPage();

    expect(await screen.findByText(/This is not investment advice/i)).toBeInTheDocument();
    // The verdict label renders in the same rendered view.
    expect(screen.getByText("Buy")).toBeInTheDocument();
  });

  it("(c) re-queries the engine when the duration changes", async () => {
    renderPage();

    // Initial load at the default 1Y horizon.
    await waitFor(() => expect(mockGenerate).toHaveBeenCalledWith("ENGROH", "1Y"));

    // Change the horizon dropdown to 3 Years.
    fireEvent.change(screen.getByLabelText("Horizon"), { target: { value: "3Y" } });

    await waitFor(() => expect(mockGenerate).toHaveBeenCalledWith("ENGROH", "3Y"));
  });
});
