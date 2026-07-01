import { useState } from "react";
import { Link } from "react-router-dom";
import { Plus, Trash2, X } from "lucide-react";
import { useAppStore } from "@/store/useAppStore";
import { getCompany } from "@/data/companies";
import { PageHeader } from "@/components/ui/PageHeader";
import { CompanyLogo } from "@/components/ui/CompanyLogo";
import { PriceChange } from "@/components/ui/PriceChange";
import { SignalBadge } from "@/components/ui/Badge";
import { formatCurrency, formatDate } from "@/lib/format";

export function WatchlistsPage() {
  const watchlists = useAppStore((s) => s.watchlists);
  const addWatchlist = useAppStore((s) => s.addWatchlist);
  const removeWatchlist = useAppStore((s) => s.removeWatchlist);
  const toggleWatch = useAppStore((s) => s.toggleWatch);
  const [activeId, setActiveId] = useState(watchlists[0]?.id);
  const [showCreate, setShowCreate] = useState(false);
  const [newName, setNewName] = useState("");

  const active = watchlists.find((w) => w.id === activeId) ?? watchlists[0];
  const activeCompanies = active?.entries.map((e) => getCompany(e.ticker)).filter(Boolean) ?? [];

  function handleCreate() {
    if (!newName.trim()) return;
    addWatchlist(newName.trim(), "Personal");
    setNewName("");
    setShowCreate(false);
  }

  return (
    <div className="p-4 md:p-6 max-w-[1400px] mx-auto">
      <PageHeader
        title="Watchlists"
        subtitle="Group companies by market, strategy, or sector and track their performance."
        actions={
          <button onClick={() => setShowCreate(true)} className="inline-flex items-center gap-1.5 bg-brand-500 hover:bg-brand-600 text-white text-sm font-medium px-3.5 py-2 rounded-lg transition-colors">
            <Plus className="h-4 w-4" /> New Watchlist
          </button>
        }
      />

      {showCreate && (
        <div className="card p-4 mb-5 flex items-center gap-2">
          <input
            autoFocus
            value={newName}
            onChange={(e) => setNewName(e.target.value)}
            onKeyDown={(e) => e.key === "Enter" && handleCreate()}
            placeholder="Watchlist name (e.g. High-Risk Investments)"
            className="flex-1 bg-bg-elevated border border-border-subtle rounded-lg px-3 py-2 text-sm outline-none focus:border-brand-500"
          />
          <button onClick={handleCreate} className="bg-brand-500 hover:bg-brand-600 text-white text-sm font-medium px-3.5 py-2 rounded-lg">
            Create
          </button>
          <button onClick={() => setShowCreate(false)} className="text-text-secondary p-2"><X className="h-4 w-4" /></button>
        </div>
      )}

      <div className="flex flex-wrap gap-1.5 mb-5">
        {watchlists.map((w) => (
          <div key={w.id} className="flex items-center">
            <button
              onClick={() => setActiveId(w.id)}
              className={`rounded-l-full pl-3.5 pr-2.5 py-1.5 text-xs font-medium transition-colors ${
                activeId === w.id ? "bg-brand-500 text-white" : "bg-bg-elevated text-text-secondary hover:bg-bg-hover"
              }`}
            >
              {w.name} <span className="opacity-70">({w.entries.length})</span>
            </button>
            <button
              onClick={() => removeWatchlist(w.id)}
              className={`rounded-r-full pr-3 pl-1.5 py-1.5 text-xs ${activeId === w.id ? "bg-brand-500 text-white" : "bg-bg-elevated text-text-secondary"}`}
            >
              <Trash2 className="h-3 w-3" />
            </button>
          </div>
        ))}
      </div>

      {active && (
        <div className="space-y-2.5">
          {activeCompanies.map((c) => c && (
            <div key={c.ticker} className="card card-hover p-4 flex items-center gap-3 flex-wrap sm:flex-nowrap">
              <Link to={`/company/${c.ticker}`} className="flex items-center gap-3 flex-1 min-w-0">
                <CompanyLogo initials={c.logoInitials} color={c.logoColor} size={34} />
                <div className="min-w-0">
                  <p className="text-sm font-medium text-text-primary truncate">{c.name}</p>
                  <p className="text-xs text-text-secondary">{c.ticker} · Next earnings {formatDate(c.nextEarningsDate)}</p>
                </div>
              </Link>
              <div className="text-right w-28 shrink-0">
                <p className="text-sm font-medium text-text-primary">{formatCurrency(c.price, c.currency)}</p>
                <PriceChange percent={c.changePercent} />
              </div>
              <SignalBadge signal={c.scoreBreakdown.signal} className="shrink-0 hidden sm:inline-flex" />
              <button onClick={() => toggleWatch(active.id, c.ticker)} className="text-text-secondary hover:text-negative p-1.5">
                <Trash2 className="h-4 w-4" />
              </button>
            </div>
          ))}
          {activeCompanies.length === 0 && (
            <div className="card p-10 text-center">
              <p className="text-sm text-text-secondary">This watchlist is empty. Add companies from the company detail page or search.</p>
              <Link to="/search" className="text-brand-400 text-sm mt-2 inline-block">Browse companies →</Link>
            </div>
          )}
        </div>
      )}
    </div>
  );
}
