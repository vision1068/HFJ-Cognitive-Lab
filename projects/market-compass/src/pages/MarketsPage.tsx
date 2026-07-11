import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import type { Company } from "@/types";
import { PageHeader } from "@/components/ui/PageHeader";
import { PriceChange } from "@/components/ui/PriceChange";
import { CompanyLogo } from "@/components/ui/CompanyLogo";
import { Sparkline } from "@/components/ui/Sparkline";
import { LoadingState, ErrorState } from "@/components/ui/DataState";
import { useCompanies, useIndices, useUsdPkr } from "@/hooks/useMarketData";
import { formatCurrency, formatRelativeTime } from "@/lib/format";

const PAGE_SIZE = 50;

export function MarketsPage() {
  const { data: companies, isLoading, isError, error, refetch } = useCompanies();
  const { data: indices } = useIndices();
  const { data: usdPkr } = useUsdPkr();
  const list = useMemo(() => companies ?? [], [companies]);

  // "Load more" windowing keeps the DOM small even though the universe is
  // hundreds of symbols (most without a sparkline). NFR: avoid rendering
  // every row at once.
  const [visible, setVisible] = useState(PAGE_SIZE);

  const sectorPerf = useMemo(() => {
    const map = new Map<string, { total: number; count: number }>();
    list.forEach((c) => {
      const cur = map.get(c.sector) ?? { total: 0, count: 0 };
      map.set(c.sector, { total: cur.total + c.changePercent, count: cur.count + 1 });
    });
    return [...map.entries()]
      .map(([sector, v]) => ({ sector, avg: v.total / v.count }))
      .sort((a, b) => b.avg - a.avg);
  }, [list]);

  // Most recent quote timestamp across the universe → a single honest "as of".
  const asOf = useMemo(() => {
    const times = list.map((c) => c.asOf).filter((t): t is string => Boolean(t));
    if (times.length === 0) return null;
    return times.reduce((a, b) => (a > b ? a : b));
  }, [list]);

  const shown = list.slice(0, visible);

  return (
    <div className="p-4 md:p-6 max-w-[1400px] mx-auto">
      <PageHeader title="Markets" subtitle="Live Pakistan Stock Exchange indices, sector performance, and listings." />

      <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-3 mb-6">
        {indices?.map((idx) => (
          <div key={idx.name} className="card p-4">
            <p className="text-xs text-text-secondary">{idx.name}</p>
            <p className="text-lg font-semibold text-text-primary mt-1">
              {idx.value.toLocaleString(undefined, { maximumFractionDigits: 2 })}
            </p>
            <PriceChange percent={idx.changePercent} abs={idx.changeAbs} />
          </div>
        ))}
        {usdPkr && (
          <div className="card p-4">
            <p className="text-xs text-text-secondary">{usdPkr.pair}</p>
            <p className="text-lg font-semibold text-text-primary mt-1">
              {usdPkr.rate.toLocaleString(undefined, { maximumFractionDigits: 2 })}
            </p>
            <PriceChange percent={usdPkr.changePercent} />
          </div>
        )}
      </div>

      {isError && <ErrorState error={error} onRetry={() => refetch()} />}
      {isLoading && !isError && <LoadingState label="Loading live PSX listings…" />}

      {list.length > 0 && (
        <div className="grid lg:grid-cols-3 gap-5">
          <div className="card p-4 lg:col-span-1 h-fit">
            <h3 className="text-sm font-semibold text-text-primary mb-3">Sector Performance</h3>
            <div className="space-y-2.5">
              {sectorPerf.map((s) => (
                <div key={s.sector} className="flex items-center justify-between text-sm">
                  <span className="text-text-secondary">{s.sector}</span>
                  <span className={s.avg >= 0 ? "text-positive" : "text-negative"}>
                    {s.avg >= 0 ? "+" : ""}
                    {s.avg.toFixed(2)}%
                  </span>
                </div>
              ))}
            </div>
          </div>

          <div className="lg:col-span-2">
            <div className="flex items-center justify-between mb-2.5">
              <p className="text-xs text-text-secondary">
                Showing {shown.length} of {list.length} listings
              </p>
              {asOf && <p className="text-xs text-text-secondary">Prices as of {formatRelativeTime(asOf)}</p>}
            </div>
            <div className="space-y-2.5">
              {shown.map((c) => (
                <MarketRow key={c.ticker} c={c} />
              ))}
            </div>
            {visible < list.length && (
              <button
                onClick={() => setVisible((v) => v + PAGE_SIZE)}
                className="mt-4 w-full rounded-lg border border-border-subtle bg-bg-elevated py-2.5 text-sm font-medium text-text-secondary hover:bg-bg-hover transition-colors"
              >
                Load more ({list.length - visible} remaining)
              </button>
            )}
          </div>
        </div>
      )}
    </div>
  );
}

function MarketRow({ c }: { c: Company }) {
  const hasPrice = c.price > 0;
  const hasSparkline = c.sparkline.length > 0;

  return (
    <Link to={`/company/${c.ticker}`} className="card card-hover p-3.5 flex items-center gap-3">
      <CompanyLogo initials={c.logoInitials} color={c.logoColor} size={32} />
      <div className="min-w-0 flex-1">
        <p className="text-sm font-medium text-text-primary truncate">{c.name}</p>
        <p className="text-xs text-text-secondary truncate">
          {c.ticker} · {c.sector}
          {c.peRatio != null ? ` · P/E ${c.peRatio.toFixed(1)}x` : ""}
          {c.marketCap != null ? ` · ${formatCurrency(c.marketCap, c.currency, { compact: true })}` : ""}
        </p>
      </div>
      {hasSparkline && <Sparkline data={c.sparkline} positive={c.changePercent >= 0} width={70} height={26} />}
      <div className="text-right w-28 shrink-0">
        {hasPrice ? (
          <>
            <p className="text-sm font-medium text-text-primary">{formatCurrency(c.price, c.currency)}</p>
            <PriceChange percent={c.changePercent} />
          </>
        ) : (
          <p className="text-xs text-text-secondary">Data not available</p>
        )}
      </div>
    </Link>
  );
}
