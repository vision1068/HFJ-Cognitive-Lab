import type { Currency, StatementSet } from "@/types";
import { LoadingState, DataUnavailable, orNA } from "@/components/ui/DataState";
import { formatCompactNumber, formatDate } from "@/lib/format";

// One row per line item, columns are fiscal periods (newest-first, as returned).
const ROWS: { label: string; key: keyof Omit<StatementSet["statements"][number], "fiscalPeriodEnd"> }[] = [
  { label: "Revenue", key: "revenue" },
  { label: "Net Income", key: "netIncome" },
  { label: "Total Assets", key: "totalAssets" },
  { label: "Operating Cash Flow", key: "operatingCashFlow" },
  { label: "Free Cash Flow", key: "freeCashFlow" },
];

/**
 * FR-7 financial-statements panel. Presentational — the caller runs
 * `useStatements(ticker)` and passes the result down so this stays trivially
 * testable. When `available:false` (every PSX symbol) it renders an explicit,
 * honest "Data not available" state with the source's `reason`. Numbers are
 * never fabricated — a null line item renders "N/A".
 */
export function FinancialStatements({
  data,
  isLoading,
  currency,
}: {
  data?: StatementSet;
  isLoading?: boolean;
  currency: Currency;
}) {
  if (isLoading) return <LoadingState label="Loading financial statements…" />;

  if (!data || !data.available) {
    return (
      <DataUnavailable
        title="Financial statements not available"
        reason={
          data?.reason ??
          "No free source publishes financial statements for this symbol, so they are shown as unavailable rather than fabricated."
        }
      />
    );
  }

  const periods = data.statements;
  const unit = data.currency ?? currency;

  return (
    <div className="space-y-3">
      <div className="overflow-x-auto">
        <table className="w-full text-sm min-w-[520px]">
          <thead>
            <tr className="text-xs text-text-secondary border-b border-border-subtle">
              <th className="text-left py-2.5 pr-3 font-medium">Line item ({unit})</th>
              {periods.map((p) => (
                <th key={p.fiscalPeriodEnd} className="text-right py-2.5 px-3 font-medium whitespace-nowrap">
                  {formatDate(p.fiscalPeriodEnd, { month: "short", year: "numeric" })}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {ROWS.map((row) => (
              <tr key={row.key} className="border-b border-border-subtle/50">
                <td className="py-2.5 pr-3 text-text-secondary whitespace-nowrap">{row.label}</td>
                {periods.map((p) => (
                  <td key={p.fiscalPeriodEnd} className="text-right px-3 text-text-primary tabular-nums">
                    {orNA(p[row.key], (v) => formatCompactNumber(v))}
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      {data.source && (
        <p className="text-[11px] text-text-secondary">Statements source: {data.source}</p>
      )}
    </div>
  );
}
