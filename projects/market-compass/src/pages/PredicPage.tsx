import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { PageHeader } from "@/components/ui/PageHeader";
import { LoadingState, ErrorState } from "@/components/ui/DataState";
import { Disclaimer } from "@/components/ui/Disclaimer";
import { DurationSelect } from "@/components/predic/DurationSelect";
import { ReportView } from "@/components/predic/ReportView";
import { psxTickers } from "@/data/psxTickers";
import { generatePredicReport } from "@/services/predic/reportBuilder";
import type { Duration } from "@/services/predic/types";

export function PredicPage() {
  const [ticker, setTicker] = useState<string>(psxTickers[0]?.ticker ?? "");
  const [duration, setDuration] = useState<Duration>("1Y");

  const {
    data: report,
    isLoading,
    isError,
    error,
    refetch,
  } = useQuery({
    queryKey: ["predic", ticker, duration],
    queryFn: () => generatePredicReport(ticker, duration),
    enabled: Boolean(ticker),
    refetchOnWindowFocus: false,
    retry: 1,
  });

  return (
    <div className="p-4 md:p-6 max-w-[1200px] mx-auto">
      <PageHeader
        title="Predic"
        subtitle="A mechanical, source-cited read on a PSX company. Every figure is either real PSX Data Portal data or an explicit N/A — nothing in between."
      />

      {/* Selectors */}
      <div className="card p-4 mb-5 grid sm:grid-cols-2 gap-4">
        <div>
          <label htmlFor="predic-company" className="text-xs text-text-secondary mb-1.5 block">
            Company (PSX)
          </label>
          <select
            id="predic-company"
            aria-label="Company"
            value={ticker}
            onChange={(e) => setTicker(e.target.value)}
            className="w-full bg-bg-elevated border border-border-subtle rounded-lg px-3 py-2 text-sm text-text-primary outline-none"
          >
            {psxTickers.map((t) => (
              <option key={t.ticker} value={t.ticker}>
                {t.name} ({t.ticker})
              </option>
            ))}
          </select>
        </div>
        <DurationSelect value={duration} onChange={setDuration} />
      </div>

      {/* States */}
      {!ticker ? (
        <div className="card p-6 text-center text-sm text-text-secondary">
          Select a PSX company to generate a Predic report.
        </div>
      ) : isLoading ? (
        <LoadingState label={`Building Predic report for ${ticker}…`} />
      ) : isError ? (
        <ErrorState error={error ?? new Error("Could not build the Predic report.")} onRetry={() => refetch()} />
      ) : report ? (
        <ReportView report={report} />
      ) : (
        <div className="card p-6 text-center text-sm text-text-secondary">No report to display.</div>
      )}

      {/* A trailing generic disclaimer too — the prominent advice one lives above the verdict */}
      <div className="mt-6">
        <Disclaimer compact />
      </div>
    </div>
  );
}
