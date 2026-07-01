import { useMemo, useState } from "react";
import { useSearchParams, Link } from "react-router-dom";
import { Search as SearchIcon, X } from "lucide-react";
import { companies } from "@/data/companies";
import type { MarketRegion, Sector } from "@/types";
import { CompanyLogo } from "@/components/ui/CompanyLogo";
import { PriceChange } from "@/components/ui/PriceChange";
import { Sparkline } from "@/components/ui/Sparkline";
import { SignalBadge } from "@/components/ui/Badge";
import { PageHeader } from "@/components/ui/PageHeader";
import { formatCurrency } from "@/lib/format";

const REGIONS: (MarketRegion | "All")[] = ["All", "Pakistan", "United States", "United Kingdom", "Japan", "Europe", "Global"];

export function SearchPage() {
  const [params, setParams] = useSearchParams();
  const [query, setQuery] = useState(params.get("q") ?? "");
  const [region, setRegion] = useState<MarketRegion | "All">("All");
  const [sector, setSector] = useState<Sector | "All">("All");
  const [sortBy, setSortBy] = useState<"relevance" | "marketCap" | "change" | "dividend" | "risk">("relevance");

  const sectors = useMemo(() => ["All", ...Array.from(new Set(companies.map((c) => c.sector))).sort()] as (Sector | "All")[], []);

  const results = useMemo(() => {
    let list = companies;
    if (query.trim()) {
      const q = query.toLowerCase();
      list = list.filter((c) => [c.name, c.ticker, c.country, c.sector, c.industry, c.region].some((f) => f.toLowerCase().includes(q)));
    }
    if (region !== "All") list = list.filter((c) => c.region === region);
    if (sector !== "All") list = list.filter((c) => c.sector === sector);

    const sorted = [...list];
    if (sortBy === "marketCap") sorted.sort((a, b) => b.marketCap - a.marketCap);
    if (sortBy === "change") sorted.sort((a, b) => b.changePercent - a.changePercent);
    if (sortBy === "dividend") sorted.sort((a, b) => b.dividendYield - a.dividendYield);
    if (sortBy === "risk") sorted.sort((a, b) => b.health.riskScore - a.health.riskScore);
    return sorted;
  }, [query, region, sector, sortBy]);

  return (
    <div className="p-4 md:p-6 max-w-[1400px] mx-auto">
      <PageHeader title="Company Search" subtitle="Search by name, ticker, country, market, sector, or industry." />

      <div className="card p-4 mb-5 space-y-3">
        <div className="relative">
          <SearchIcon className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-text-secondary" />
          <input
            value={query}
            onChange={(e) => {
              setQuery(e.target.value);
              setParams(e.target.value ? { q: e.target.value } : {});
            }}
            placeholder="Search companies, tickers, sectors, countries..."
            className="w-full bg-bg-elevated border border-border-subtle rounded-lg pl-9 pr-9 py-2.5 text-sm outline-none focus:border-brand-500 transition-colors"
          />
          {query && (
            <button onClick={() => { setQuery(""); setParams({}); }} className="absolute right-3 top-1/2 -translate-y-1/2 text-text-secondary">
              <X className="h-4 w-4" />
            </button>
          )}
        </div>

        <div className="flex flex-wrap items-center gap-1.5">
          {REGIONS.map((r) => (
            <button
              key={r}
              onClick={() => setRegion(r)}
              className={`rounded-full px-3 py-1.5 text-xs font-medium transition-colors ${
                region === r ? "bg-brand-500 text-white" : "bg-bg-elevated text-text-secondary hover:bg-bg-hover"
              }`}
            >
              {r === "All" ? "All Markets" : r}
            </button>
          ))}
        </div>

        <div className="flex flex-wrap items-center gap-3">
          <select
            value={sector}
            onChange={(e) => setSector(e.target.value as Sector | "All")}
            className="bg-bg-elevated border border-border-subtle rounded-lg px-3 py-1.5 text-xs text-text-primary outline-none"
          >
            {sectors.map((s) => (
              <option key={s} value={s}>
                {s === "All" ? "All Sectors" : s}
              </option>
            ))}
          </select>
          <select
            value={sortBy}
            onChange={(e) => setSortBy(e.target.value as typeof sortBy)}
            className="bg-bg-elevated border border-border-subtle rounded-lg px-3 py-1.5 text-xs text-text-primary outline-none"
          >
            <option value="relevance">Sort: Relevance</option>
            <option value="marketCap">Sort: Market Cap</option>
            <option value="change">Sort: Daily Change</option>
            <option value="dividend">Sort: Dividend Yield</option>
            <option value="risk">Sort: Risk Score</option>
          </select>
          <span className="text-xs text-text-secondary ml-auto">{results.length} companies</span>
        </div>
      </div>

      <div className="space-y-2.5">
        {results.map((c) => (
          <Link key={c.ticker} to={`/company/${c.ticker}`} className="card card-hover p-4 flex items-center gap-4 flex-wrap sm:flex-nowrap">
            <CompanyLogo initials={c.logoInitials} color={c.logoColor} size={40} />
            <div className="min-w-0 flex-1">
              <div className="flex items-center gap-2">
                <p className="font-medium text-text-primary truncate">{c.name}</p>
                <span className="text-xs text-text-secondary">{c.ticker}</span>
              </div>
              <p className="text-xs text-text-secondary truncate">
                {c.exchange} · {c.country} · {c.sector}
              </p>
            </div>
            <Sparkline data={c.sparkline} positive={c.changePercent >= 0} width={90} height={30} />
            <div className="text-right w-28 shrink-0">
              <p className="font-medium text-text-primary">{formatCurrency(c.price, c.currency)}</p>
              <PriceChange percent={c.changePercent} />
            </div>
            <SignalBadge signal={c.scoreBreakdown.signal} className="shrink-0" />
          </Link>
        ))}
        {results.length === 0 && <p className="text-center text-text-secondary text-sm py-12">No companies match your filters.</p>}
      </div>
    </div>
  );
}
