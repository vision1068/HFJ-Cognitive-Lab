import { useMemo, useState } from "react";
import { useSearchParams, Link } from "react-router-dom";
import { Search as SearchIcon, X } from "lucide-react";
import type { Sector } from "@/types";
import { CompanyLogo } from "@/components/ui/CompanyLogo";
import { PriceChange } from "@/components/ui/PriceChange";
import { Sparkline } from "@/components/ui/Sparkline";
import { PageHeader } from "@/components/ui/PageHeader";
import { LoadingState, ErrorState } from "@/components/ui/DataState";
import { useCompanies } from "@/hooks/useMarketData";
import { formatCurrency } from "@/lib/format";

export function SearchPage() {
  const { data: companies, isLoading, isError, error, refetch } = useCompanies();
  const list = companies ?? [];

  const [params, setParams] = useSearchParams();
  const [query, setQuery] = useState(params.get("q") ?? "");
  const [sector, setSector] = useState<Sector | "All">("All");
  const [sortBy, setSortBy] = useState<"relevance" | "change" | "price">("relevance");

  const sectors = useMemo(() => ["All", ...Array.from(new Set(list.map((c) => c.sector))).sort()] as (Sector | "All")[], [list]);

  const results = useMemo(() => {
    let out = list;
    if (query.trim()) {
      const q = query.toLowerCase();
      out = out.filter((c) => [c.name, c.ticker, c.sector, c.industry].some((f) => f.toLowerCase().includes(q)));
    }
    if (sector !== "All") out = out.filter((c) => c.sector === sector);

    const sorted = [...out];
    if (sortBy === "change") sorted.sort((a, b) => b.changePercent - a.changePercent);
    if (sortBy === "price") sorted.sort((a, b) => b.price - a.price);
    return sorted;
  }, [list, query, sector, sortBy]);

  return (
    <div className="p-4 md:p-6 max-w-[1400px] mx-auto">
      <PageHeader title="Company Search" subtitle="Search PSX-listed companies by name, ticker, sector, or industry." />

      <div className="card p-4 mb-5 space-y-3">
        <div className="relative">
          <SearchIcon className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-text-secondary" />
          <input
            value={query}
            onChange={(e) => {
              setQuery(e.target.value);
              setParams(e.target.value ? { q: e.target.value } : {});
            }}
            placeholder="Search companies, tickers, sectors..."
            className="w-full bg-bg-elevated border border-border-subtle rounded-lg pl-9 pr-9 py-2.5 text-sm outline-none focus:border-brand-500 transition-colors"
          />
          {query && (
            <button onClick={() => { setQuery(""); setParams({}); }} className="absolute right-3 top-1/2 -translate-y-1/2 text-text-secondary">
              <X className="h-4 w-4" />
            </button>
          )}
        </div>

        <div className="flex flex-wrap items-center gap-3">
          <select
            value={sector}
            onChange={(e) => setSector(e.target.value as Sector | "All")}
            className="bg-bg-elevated border border-border-subtle rounded-lg px-3 py-1.5 text-xs text-text-primary outline-none"
          >
            {sectors.map((s) => (
              <option key={s} value={s}>{s === "All" ? "All Sectors" : s}</option>
            ))}
          </select>
          <select
            value={sortBy}
            onChange={(e) => setSortBy(e.target.value as typeof sortBy)}
            className="bg-bg-elevated border border-border-subtle rounded-lg px-3 py-1.5 text-xs text-text-primary outline-none"
          >
            <option value="relevance">Sort: Relevance</option>
            <option value="change">Sort: Daily Change</option>
            <option value="price">Sort: Price</option>
          </select>
          <span className="text-xs text-text-secondary ml-auto">{results.length} companies</span>
        </div>
      </div>

      {isError && <ErrorState error={error} onRetry={() => refetch()} />}
      {isLoading && !isError && <LoadingState label="Loading live PSX companies…" />}

      <div className="space-y-2.5">
        {results.map((c) => (
          <Link key={c.ticker} to={`/company/${c.ticker}`} className="card card-hover p-4 flex items-center gap-4 flex-wrap sm:flex-nowrap">
            <CompanyLogo initials={c.logoInitials} color={c.logoColor} size={40} />
            <div className="min-w-0 flex-1">
              <div className="flex items-center gap-2">
                <p className="font-medium text-text-primary truncate">{c.name}</p>
                <span className="text-xs text-text-secondary">{c.ticker}</span>
              </div>
              <p className="text-xs text-text-secondary truncate">{c.exchange} · {c.country} · {c.sector}</p>
            </div>
            <Sparkline data={c.sparkline} positive={c.changePercent >= 0} width={90} height={30} />
            <div className="text-right w-28 shrink-0">
              <p className="font-medium text-text-primary">{formatCurrency(c.price, c.currency)}</p>
              <PriceChange percent={c.changePercent} />
            </div>
          </Link>
        ))}
        {!isLoading && results.length === 0 && <p className="text-center text-text-secondary text-sm py-12">No companies match your filters.</p>}
      </div>
    </div>
  );
}
