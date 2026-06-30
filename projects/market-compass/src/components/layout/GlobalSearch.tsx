import { useEffect, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { Search, X } from "lucide-react";
import { companies } from "@/data/companies";
import { CompanyLogo } from "@/components/ui/CompanyLogo";
import { PriceChange } from "@/components/ui/PriceChange";
import { formatCurrency } from "@/lib/format";

export function GlobalSearch() {
  const [open, setOpen] = useState(false);
  const [query, setQuery] = useState("");
  const inputRef = useRef<HTMLInputElement>(null);
  const navigate = useNavigate();

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

  const results = query.trim()
    ? companies
        .filter((c) =>
          [c.name, c.ticker, c.country, c.sector, c.industry, c.region].some((f) =>
            f.toLowerCase().includes(query.toLowerCase())
          )
        )
        .slice(0, 8)
    : [];

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
        <span className="flex-1 text-left">Search companies, tickers, sectors...</span>
        <kbd className="hidden sm:inline-flex items-center gap-0.5 rounded border border-border-default px-1.5 py-0.5 text-[10px] font-mono text-text-secondary">
          ⌘K
        </kbd>
      </button>

      {open && (
        <div className="fixed inset-0 z-50 flex items-start justify-center pt-24 px-4 bg-black/60 backdrop-blur-sm animate-fade-in" onClick={() => setOpen(false)}>
          <div
            className="w-full max-w-xl card border-border-default shadow-2xl overflow-hidden animate-slide-up"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="flex items-center gap-2 px-4 py-3 border-b border-border-subtle">
              <Search className="h-4 w-4 text-text-secondary shrink-0" />
              <input
                ref={inputRef}
                value={query}
                onChange={(e) => setQuery(e.target.value)}
                onKeyDown={(e) => e.key === "Enter" && query.trim() && goToSearch()}
                placeholder="Search by name, ticker, country, sector..."
                className="flex-1 bg-transparent outline-none text-sm placeholder:text-text-secondary"
              />
              <button onClick={() => setOpen(false)} className="text-text-secondary hover:text-text-primary">
                <X className="h-4 w-4" />
              </button>
            </div>
            <div className="max-h-96 overflow-y-auto">
              {results.length === 0 && query.trim() && (
                <p className="px-4 py-6 text-sm text-text-secondary text-center">No companies found for "{query}"</p>
              )}
              {results.length === 0 && !query.trim() && (
                <p className="px-4 py-6 text-sm text-text-secondary text-center">Try "Apple", "ENGRO", "Banking", or "Pakistan"</p>
              )}
              {results.map((c) => (
                <button
                  key={c.ticker}
                  onClick={() => goToCompany(c.ticker)}
                  className="flex items-center gap-3 w-full px-4 py-2.5 hover:bg-bg-hover text-left transition-colors"
                >
                  <CompanyLogo initials={c.logoInitials} color={c.logoColor} size={32} />
                  <div className="flex-1 min-w-0">
                    <p className="text-sm font-medium text-text-primary truncate">{c.name}</p>
                    <p className="text-xs text-text-secondary">
                      {c.ticker} · {c.exchange} · {c.country}
                    </p>
                  </div>
                  <div className="text-right shrink-0">
                    <p className="text-sm font-medium text-text-primary">{formatCurrency(c.price, c.currency)}</p>
                    <PriceChange percent={c.changePercent} />
                  </div>
                </button>
              ))}
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
