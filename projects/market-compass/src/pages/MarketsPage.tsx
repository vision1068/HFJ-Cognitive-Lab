import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { marketIndices } from "@/data/markets";
import { companies } from "@/data/companies";
import type { MarketRegion } from "@/types";
import { PageHeader } from "@/components/ui/PageHeader";
import { PriceChange } from "@/components/ui/PriceChange";
import { CompanyLogo } from "@/components/ui/CompanyLogo";
import { Sparkline } from "@/components/ui/Sparkline";
import { SignalBadge } from "@/components/ui/Badge";
import { formatCurrency } from "@/lib/format";

const REGIONS: (MarketRegion | "All")[] = ["All", "Pakistan", "United States", "United Kingdom", "Japan", "Europe", "Global"];

export function MarketsPage() {
  const [region, setRegion] = useState<MarketRegion | "All">("All");

  const indices = region === "All" ? marketIndices : marketIndices.filter((i) => i.region === region);
  const list = region === "All" ? companies : companies.filter((c) => c.region === region);

  const sectorPerf = useMemo(() => {
    const map = new Map<string, { total: number; count: number }>();
    list.forEach((c) => {
      const cur = map.get(c.sector) ?? { total: 0, count: 0 };
      map.set(c.sector, { total: cur.total + c.changePercent, count: cur.count + 1 });
    });
    return [...map.entries()].map(([sector, v]) => ({ sector, avg: v.total / v.count })).sort((a, b) => b.avg - a.avg);
  }, [list]);

  return (
    <div className="p-4 md:p-6 max-w-[1400px] mx-auto">
      <PageHeader title="Markets" subtitle="Indices, sector performance, and company listings by region." />

      <div className="flex flex-wrap gap-1.5 mb-5">
        {REGIONS.map((r) => (
          <button
            key={r}
            onClick={() => setRegion(r)}
            className={`rounded-full px-3.5 py-1.5 text-xs font-medium transition-colors ${
              region === r ? "bg-brand-500 text-white" : "bg-bg-elevated text-text-secondary hover:bg-bg-hover"
            }`}
          >
            {r === "All" ? "All Markets" : r}
          </button>
        ))}
      </div>

      <div className="grid sm:grid-cols-2 lg:grid-cols-4 gap-3 mb-6">
        {indices.map((idx) => (
          <div key={idx.name} className="card p-4">
            <p className="text-xs text-text-secondary">{idx.name}</p>
            <p className="text-lg font-semibold text-text-primary mt-1">{idx.value.toLocaleString()}</p>
            <PriceChange percent={idx.changePercent} abs={idx.changeAbs} />
          </div>
        ))}
      </div>

      <div className="grid lg:grid-cols-3 gap-5">
        <div className="card p-4 lg:col-span-1">
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

        <div className="lg:col-span-2 space-y-2.5">
          {list.map((c) => (
            <Link key={c.ticker} to={`/company/${c.ticker}`} className="card card-hover p-3.5 flex items-center gap-3">
              <CompanyLogo initials={c.logoInitials} color={c.logoColor} size={32} />
              <div className="min-w-0 flex-1">
                <p className="text-sm font-medium text-text-primary truncate">{c.name}</p>
                <p className="text-xs text-text-secondary">{c.ticker} · {c.sector}</p>
              </div>
              <Sparkline data={c.sparkline} positive={c.changePercent >= 0} width={70} height={26} />
              <div className="text-right w-24 shrink-0">
                <p className="text-sm font-medium text-text-primary">{formatCurrency(c.price, c.currency)}</p>
                <PriceChange percent={c.changePercent} />
              </div>
              <SignalBadge signal={c.scoreBreakdown.signal} className="hidden md:inline-flex" />
            </Link>
          ))}
        </div>
      </div>
    </div>
  );
}
