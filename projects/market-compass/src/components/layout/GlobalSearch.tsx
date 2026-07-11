import { useEffect, useMemo, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { Search, X } from "lucide-react";
import type { Company } from "@/types";
import { CompanyLogo } from "@/components/ui/CompanyLogo";
import { PriceChange } from "@/components/ui/PriceChange";
import { SourceBadge } from "@/components/market/SourceBadge";
import { useCompanies, useSearch } from "@/hooks/useMarketData";
import { formatCurrency } from "@/lib/format";

export function GlobalSearch() {
  const [open, setOpen] = useState(false);
  const [query, setQuery] = useState("");
  const inputRef = useRef<HTMLInputElement>(null);
  const navigate = useNavigate();
  const { data: companies = [] } = useCompanies();
  // Smart Search (FR-5): fuzzy match across the FULL universe + global symbols.
  // Deterministic keyword search — NOT "AI".
  const { data: hits = [] } = useSearch(query.trim());

  const companyByTicker = useMemo(() => {
    const m = new Map<string, Company>();
    for (const c of companies) m.set(c.ticker, c);
    return m;
  }, [companies]);

  useEffect(() => {
    function onKeyDown(e: KeyboardEvent) {
      if ((e.metaKey || e.ctrlKey) && e.key === "k") {
        e.preventDefault();
        setOpen(true);
      }
      if (e.key === "Escape") setOpen(false);
    }
    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, []);

  useEffect(() => {
    if (open) setTimeout(() => inputRef.current?.focus(), 50);
    else setQuery("");
  }, [open]);

  const results = query.trim() ? hits.slice(0, 8) : [];

  function goToCompany(ticker: string) {
    setOpen(false);
    navigate(`/company/${ticker}`);
  }

  function goToSearch() {
    setOpen(false);
    navigate(`/search?q=${encodeURIComponent(query)}`);
  }

  return (
    <>
      <button
        onClick={() => setOpen(true)}
        className="flex items-center gap-2 w-full max-w-md rounded-lg border border-border-subtle bg-bg-elevated px-3 py-2 text-sm text-text-secondary hover:border-border-default transition-colors"
      >
        <Search className="h-4 w-4 shrink-0" />
        <span className="flex-1 text-left">Search PSX companies, tickers, or global symbols...</span>
        <kbd className="hidden sm:inline-flex items-center gap-0.5 rounded border border-border-default px-1.5 py-0.5 text-[10px] font-mono text-text-secondary">
          ⌘K
        </kbd>
      </button>

      {open && (
        <div className="fixed inset-0 z-50 flex items-start justify-center pt-24 px-4 bg-black/60 backdrop-blur-sm animate-fade-in" onClick={() => setOpen(false)}>
          <div className="w-full max-w-xl card border-border-default shadow-2xl overflow-hidden animate-slide-up" onClick={(e) => e.stopPropagation()}>
            <div className="flex items-center gap-2 px-4 py-3 border-b border-border-subtle">
              <Search className="h-4 w-4 text-text-secondary shrink-0" />
              <input
                ref={inputRef}
                value={query}
                onChange={(e) => setQuery(e.target.value)}
                onKeyDown={(e) => e.key === "Enter" && query.trim() && goToSearch()}
                placeholder="Search by name, ticker, or global symbol..."
                className="flex-1 bg-transparent outline-none text-sm placeholder:text-text-secondary"
              />
              <button onClick={() => setOpen(false)} className="text-text-secondary hover:text-text-primary">
                <X className="h-4 w-4" />
              </button>
            </div>
            <div className="max-h-96 overflow-y-auto">
              {results.length === 0 && query.trim() && (
                <p className="px-4 py-6 text-sm text-text-secondary text-center">No matches for "{query}"</p>
              )}
              {results.length === 0 && !query.trim() && (
                <p className="px-4 py-6 text-sm text-text-secondary text-center">Try "OGDC", "Meezan", "Apple", or "Cement"</p>
              )}
              {results.map((r) => {
                const c = companyByTicker.get(r.ticker);
                const hasPrice = c != null && c.price > 0;
                return (
                  <button
                    key={`${r.source}-${r.ticker}`}
                    onClick={() => goToCompany(r.ticker)}
                    className="flex items-center gap-3 w-full px-4 py-2.5 hover:bg-bg-hover text-left transition-colors"
                  >
                    <CompanyLogo
                      initials={c?.logoInitials ?? r.ticker.slice(0, 2).toUpperCase()}
                      color={c?.logoColor ?? "#64748b"}
                      size={32}
                    />
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2">
                        <p className="text-sm font-medium text-text-primary truncate">{c?.name ?? r.name}</p>
                        <SourceBadge source={r.source} />
                      </div>
                      <p className="text-xs text-text-secondary truncate">
                        {r.ticker} · {r.exchange}
                        {c?.sector ? ` · ${c.sector}` : ""}
                      </p>
                    </div>
                    <div className="text-right shrink-0">
                      {hasPrice ? (
                        <>
                          <p className="text-sm font-medium text-text-primary">{formatCurrency(c.price, c.currency)}</p>
                          <PriceChange percent={c.changePercent} />
                        </>
                      ) : (
                        <p className="text-xs text-text-secondary">No local quote</p>
                      )}
                    </div>
                  </button>
                );
              })}
            </div>
            {query.trim() && (
              <button onClick={goToSearch} className="w-full px-4 py-2.5 text-xs text-brand-400 hover:bg-bg-hover border-t border-border-subtle">
                View all results for "{query}" →
              </button>
            )}
          </div>
        </div>
      )}
    </>
  );
}
