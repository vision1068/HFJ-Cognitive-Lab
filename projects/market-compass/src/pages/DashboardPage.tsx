import { useMemo } from "react";
import { Link } from "react-router-dom";
import { TrendingUp, TrendingDown, Flame } from "lucide-react";
import type { Company } from "@/types";
import { CompanyLogo } from "@/components/ui/CompanyLogo";
import { PriceChange } from "@/components/ui/PriceChange";
import { Disclaimer } from "@/components/ui/Disclaimer";
import { LoadingState, ErrorState, DataUnavailable } from "@/components/ui/DataState";
import { useCompanies, useIndices } from "@/hooks/useMarketData";
import { formatCurrency, formatCompactNumber } from "@/lib/format";
import { useAppStore } from "@/store/useAppStore";

export function DashboardPage() {
  const { data: companies, isLoading, isError, error, refetch } = useCompanies();
  const { data: indices } = useIndices();

  const list = companies ?? [];
  const gainers = useMemo(() => [...list].sort((a, b) => b.changePercent - a.changePercent).slice(0, 5), [list]);
  const losers = useMemo(() => [...list].sort((a, b) => a.changePercent - b.changePercent).slice(0, 5), [list]);
  const mostActive = useMemo(
    () => [...list].filter((c) => c.volume != null).sort((a, b) => (b.volume ?? 0) - (a.volume ?? 0)).slice(0, 5),
    [list]
  );

  const sectorPerf = useMemo(() => {
    const map = new Map<string, { total: number; count: number }>();
    list.forEach((c) => {
      const cur = map.get(c.sector) ?? { total: 0, count: 0 };
      map.set(c.sector, { total: cur.total + c.changePercent, count: cur.count + 1 });
    });
    return [...map.entries()].map(([sector, v]) => ({ sector, avg: v.total / v.count })).sort((a, b) => b.avg - a.avg);
  }, [list]);

  const watchlists = useAppStore((s) => s.watchlists);
  const watchedTickers = useMemo(() => new Set(watchlists.flatMap((w) => w.entries.map((e) => e.ticker))), [watchlists]);
  const watchedCompanies = list.filter((c) => watchedTickers.has(c.ticker));

  return (
    <div className="p-4 md:p-6 max-w-[1400px] mx-auto space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-text-primary tracking-tight">Dashboard</h1>
        <p className="text-sm text-text-secondary mt-1">Live Pakistan Stock Exchange (PSX) overview. Auto-refreshes every 60 seconds.</p>
      </div>

      <Disclaimer compact />

      {isError && <ErrorState error={error} onRetry={() => refetch()} />}

      {/* Market overview cards */}
      <section>
        <h2 className="text-sm font-semibold text-text-primary mb-3">Market Overview</h2>
        <div className="grid grid-cols-2 lg:grid-cols-3 gap-3">
          {indices?.map((idx) => (
            <div key={idx.name} className="card p-3.5">
              <p className="text-xs text-text-secondary truncate">{idx.name}</p>
              <p className="text-base font-semibold text-text-primary mt-1">
                {idx.value.toLocaleString(undefined, { maximumFractionDigits: 2 })}
              </p>
              <PriceChange percent={idx.changePercent} abs={idx.changeAbs} />
            </div>
          ))}
          {!indices && !isError && <div className="col-span-full"><LoadingState label="Loading live PSX indices…" /></div>}
        </div>
      </section>

      {isLoading && !isError && <LoadingState label="Loading live PSX company data…" />}

      {list.length > 0 && (
        <>
          <div className="grid lg:grid-cols-3 gap-4">
            <PanelList title="Top Gainers" icon={TrendingUp} iconTone="text-positive" companies={gainers} />
            <PanelList title="Top Losers" icon={TrendingDown} iconTone="text-negative" companies={losers} />
            <PanelList title="Most Active" icon={Flame} iconTone="text-warning" companies={mostActive} showVolume />
          </div>

          <div className="card p-4">
            <h3 className="text-sm font-semibold text-text-primary mb-3">Sector Performance</h3>
            <div className="space-y-2">
              {sectorPerf.map((s) => (
                <div key={s.sector} className="flex items-center gap-3">
                  <span className="text-xs text-text-secondary w-32 shrink-0 truncate">{s.sector}</span>
                  <div className="flex-1 h-2 rounded-full bg-bg-elevated overflow-hidden relative">
                    <div
                      className={`h-full rounded-full ${s.avg >= 0 ? "bg-positive" : "bg-negative"}`}
                      style={{ width: `${Math.min(Math.abs(s.avg) * 18, 100)}%`, marginLeft: s.avg < 0 ? "auto" : undefined }}
                    />
                  </div>
                  <span className={`text-xs font-medium w-14 text-right shrink-0 ${s.avg >= 0 ? "text-positive" : "text-negative"}`}>
                    {s.avg >= 0 ? "+" : ""}
                    {s.avg.toFixed(2)}%
                  </span>
                </div>
              ))}
            </div>
          </div>

          {/* Watchlist summary */}
          <section className="card p-4">
            <div className="flex items-center justify-between mb-4">
              <h3 className="text-sm font-semibold text-text-primary">Watchlist Summary</h3>
              <Link to="/watchlists" className="text-xs text-brand-400 hover:underline">Manage watchlists →</Link>
            </div>
            <div className="grid md:grid-cols-2 gap-4">
              <div>
                <p className="text-xs text-text-secondary mb-2">Biggest Gainers</p>
                <div className="space-y-2">
                  {[...watchedCompanies].sort((a, b) => b.changePercent - a.changePercent).slice(0, 3).map((c) => (
                    <CompanyRow key={c.ticker} company={c} />
                  ))}
                  {watchedCompanies.length === 0 && <p className="text-xs text-text-secondary">No watchlist companies yet.</p>}
                </div>
              </div>
              <div>
                <p className="text-xs text-text-secondary mb-2">Biggest Losers</p>
                <div className="space-y-2">
                  {[...watchedCompanies].sort((a, b) => a.changePercent - b.changePercent).slice(0, 3).map((c) => (
                    <CompanyRow key={c.ticker} company={c} />
                  ))}
                  {watchedCompanies.length === 0 && <p className="text-xs text-text-secondary">No watchlist companies yet.</p>}
                </div>
              </div>
            </div>
          </section>

          {/* Analytics that free PSX data cannot support */}
          <section className="card p-5">
            <h3 className="text-sm font-semibold text-text-primary mb-3">Signals, Sentiment &amp; Daily Briefing</h3>
            <DataUnavailable
              title="Not available on free PSX data"
              reason="Signal scores, market sentiment, analyst commentary, and the daily briefing require paid research feeds. Only live prices, indices, and PSX-reported fundamentals are shown in this build."
            />
          </section>
        </>
      )}
    </div>
  );
}

function CompanyRow({ company }: { company: Company }) {
  return (
    <Link to={`/company/${company.ticker}`} className="flex items-center justify-between text-sm hover:bg-bg-hover rounded-lg px-1.5 py-1 -mx-1.5 transition-colors">
      <span className="text-text-primary">{company.ticker}</span>
      <PriceChange percent={company.changePercent} />
    </Link>
  );
}

function PanelList({
  title,
  icon: Icon,
  iconTone,
  companies,
  showVolume,
}: {
  title: string;
  icon: typeof TrendingUp;
  iconTone: string;
  companies: Company[];
  showVolume?: boolean;
}) {
  return (
    <div className="card p-4">
      <div className="flex items-center gap-2 mb-3">
        <Icon className={`h-4 w-4 ${iconTone}`} />
        <h3 className="text-sm font-semibold text-text-primary">{title}</h3>
      </div>
      <div className="space-y-3">
        {companies.map((c) => (
          <Link key={c.ticker} to={`/company/${c.ticker}`} className="flex items-center gap-3 hover:bg-bg-hover -mx-1.5 px-1.5 py-1 rounded-lg transition-colors">
            <CompanyLogo initials={c.logoInitials} color={c.logoColor} size={28} />
            <div className="flex-1 min-w-0">
              <p className="text-sm font-medium text-text-primary truncate">{c.ticker}</p>
              <p className="text-[11px] text-text-secondary truncate">
                {showVolume ? `Vol ${c.volume != null ? formatCompactNumber(c.volume) : "N/A"}` : c.sector}
              </p>
            </div>
            <div className="text-right shrink-0">
              <p className="text-sm font-medium text-text-primary">{formatCurrency(c.price, c.currency)}</p>
              <PriceChange percent={c.changePercent} />
            </div>
          </Link>
        ))}
      </div>
    </div>
  );
}
