import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import type { Company } from "@/types";
import { PageHeader } from "@/components/ui/PageHeader";
import { CompanyLogo } from "@/components/ui/CompanyLogo";
import { PriceChange } from "@/components/ui/PriceChange";
import { LoadingState, ErrorState } from "@/components/ui/DataState";
import { useCompaniesDetailed } from "@/hooks/useMarketData";
import { formatCurrency } from "@/lib/format";

interface ReadyScreen {
  name: string;
  description: string;
  filter: (c: Company) => boolean;
}

const READY_SCREENS: ReadyScreen[] = [
  { name: "Gainers Today", description: "PSX stocks up on the day", filter: (c) => c.changePercent > 0 },
  { name: "Decliners Today", description: "PSX stocks down on the day", filter: (c) => c.changePercent < 0 },
  { name: "Low P/E (< 10x)", description: "Trading below 10x trailing earnings", filter: (c) => c.peRatio != null && c.peRatio < 10 },
  { name: "Large Cap (> Rs 300B)", description: "Market cap above Rs 300 billion", filter: (c) => c.marketCap != null && c.marketCap > 300e9 },
  { name: "Near 52-Week High", description: "Within 10% of the 52-week high", filter: (c) => c.week52High != null && c.price >= c.week52High * 0.9 },
];

export function ScreenerPage() {
  const { data: companies, isLoading, isError, error, refetch } = useCompaniesDetailed();
  const list = companies ?? [];

  const [activeScreen, setActiveScreen] = useState<string | null>(null);
  const [minMarketCap, setMinMarketCap] = useState(0);
  const [maxPE, setMaxPE] = useState(100);
  const [sector, setSector] = useState("All");

  const sectors = useMemo(() => ["All", ...Array.from(new Set(list.map((c) => c.sector))).sort()], [list]);

  const results = useMemo(() => {
    let out = list;
    const screen = READY_SCREENS.find((s) => s.name === activeScreen);
    if (screen) out = out.filter(screen.filter);
    out = out.filter(
      (c) =>
        (c.marketCap ?? 0) >= minMarketCap &&
        (c.peRatio == null || c.peRatio <= maxPE) &&
        (sector === "All" || c.sector === sector)
    );
    return [...out].sort((a, b) => (b.marketCap ?? 0) - (a.marketCap ?? 0));
  }, [list, activeScreen, minMarketCap, maxPE, sector]);

  return (
    <div className="p-4 md:p-6 max-w-[1400px] mx-auto">
      <PageHeader title="Stock Screener" subtitle="Filter PSX companies by live price, valuation, and sector." />

      <div className="mb-5">
        <p className="text-xs font-medium text-text-secondary mb-2">Ready-Made Screeners</p>
        <div className="flex flex-wrap gap-1.5">
          {READY_SCREENS.map((s) => (
            <button
              key={s.name}
              onClick={() => setActiveScreen(activeScreen === s.name ? null : s.name)}
              title={s.description}
              className={`rounded-full px-3 py-1.5 text-xs font-medium transition-colors ${
                activeScreen === s.name ? "bg-brand-500 text-white" : "bg-bg-elevated text-text-secondary hover:bg-bg-hover"
              }`}
            >
              {s.name}
            </button>
          ))}
        </div>
      </div>

      <div className="card p-4 mb-5 grid sm:grid-cols-2 lg:grid-cols-3 gap-4">
        <FilterRange label="Min Market Cap" value={minMarketCap} max={800e9} step={10e9} onChange={setMinMarketCap} format={(v) => `Rs ${(v / 1e9).toFixed(0)}B`} />
        <FilterRange label="Max P/E Ratio" value={maxPE} max={100} step={1} onChange={setMaxPE} format={(v) => `${v}x`} />
        <div>
          <label className="text-xs text-text-secondary mb-1.5 block">Sector</label>
          <select value={sector} onChange={(e) => setSector(e.target.value)} className="w-full bg-bg-elevated border border-border-subtle rounded-lg px-3 py-2 text-sm text-text-primary outline-none">
            {sectors.map((s) => (
              <option key={s} value={s}>{s}</option>
            ))}
          </select>
        </div>
      </div>

      {isError && <ErrorState error={error} onRetry={() => refetch()} />}
      {isLoading && !isError && <LoadingState label="Loading live PSX fundamentals…" />}

      {list.length > 0 && (
        <>
          <p className="text-xs text-text-secondary mb-3">{results.length} companies match your criteria</p>
          <div className="overflow-x-auto card">
            <table className="w-full text-sm">
              <thead>
                <tr className="text-xs text-text-secondary border-b border-border-subtle">
                  <th className="text-left py-3 px-4">Company</th>
                  <th className="text-right py-3 px-3">Price</th>
                  <th className="text-right py-3 px-3">Change</th>
                  <th className="text-right py-3 px-3">Mkt Cap</th>
                  <th className="text-right py-3 px-3">P/E</th>
                  <th className="text-right py-3 px-4">EPS</th>
                </tr>
              </thead>
              <tbody>
                {results.map((c) => (
                  <tr key={c.ticker} className="border-b border-border-subtle/50 hover:bg-bg-hover">
                    <td className="py-2.5 px-4">
                      <Link to={`/company/${c.ticker}`} className="flex items-center gap-2.5">
                        <CompanyLogo initials={c.logoInitials} color={c.logoColor} size={26} />
                        <div>
                          <p className="text-text-primary font-medium">{c.ticker}</p>
                          <p className="text-[11px] text-text-secondary">{c.sector}</p>
                        </div>
                      </Link>
                    </td>
                    <td className="text-right px-3 text-text-primary">{formatCurrency(c.price, c.currency)}</td>
                    <td className="text-right px-3"><PriceChange percent={c.changePercent} /></td>
                    <td className="text-right px-3 text-text-secondary">{c.marketCap != null ? formatCurrency(c.marketCap, c.currency, { compact: true }) : "N/A"}</td>
                    <td className="text-right px-3 text-text-secondary">{c.peRatio != null ? `${c.peRatio.toFixed(1)}x` : "N/A"}</td>
                    <td className="text-right px-4 text-text-secondary">{c.eps != null ? c.eps.toFixed(2) : "N/A"}</td>
                  </tr>
                ))}
              </tbody>
            </table>
            {results.length === 0 && <p className="text-center text-sm text-text-secondary py-10">No companies match these filters.</p>}
          </div>
        </>
      )}
    </div>
  );
}

function FilterRange({ label, value, max, step, onChange, format }: { label: string; value: number; max: number; step: number; onChange: (v: number) => void; format: (v: number) => string }) {
  return (
    <div>
      <div className="flex items-center justify-between mb-1.5">
        <label className="text-xs text-text-secondary">{label}</label>
        <span className="text-xs font-medium text-text-primary">{format(value)}</span>
      </div>
      <input type="range" min={0} max={max} step={step} value={value} onChange={(e) => onChange(Number(e.target.value))} className="w-full accent-brand-500" />
    </div>
  );
}
