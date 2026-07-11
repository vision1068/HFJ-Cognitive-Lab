import { useState } from "react";
import { Link, useParams } from "react-router-dom";
import { Globe, ExternalLink, Plus, Check } from "lucide-react";
import { CompanyLogo } from "@/components/ui/CompanyLogo";
import { PriceChange } from "@/components/ui/PriceChange";
import { Disclaimer } from "@/components/ui/Disclaimer";
import { PriceChart } from "@/components/company/PriceChart";
import { LoadingState, ErrorState, DataUnavailable, orNA } from "@/components/ui/DataState";
import { FinancialStatements } from "@/components/company/FinancialStatements";
import { useCompany, useStatements } from "@/hooks/useMarketData";
import { formatCurrency, formatCompactNumber, formatRelativeTime } from "@/lib/format";
import { useAppStore } from "@/store/useAppStore";

export function CompanyDetailPage() {
  const { ticker } = useParams<{ ticker: string }>();
  const { data: c, isLoading, isError, error, refetch } = useCompany(ticker);
  // PSX symbols carry a `.KA` suffix so the statements service returns the honest
  // "no free PSX statements source" state rather than attempting a Yahoo fetch.
  // PSX companies -> `${symbol}.KA` (honest "no free PSX statements" N/A);
  // global companies -> the raw symbol (real multi-year statements).
  const statementsSymbol = c ? (c.isPsx ? `${c.psxSymbol}.KA` : c.ticker) : undefined;
  const { data: statements, isLoading: statementsLoading } = useStatements(statementsSymbol);
  const watchlists = useAppStore((s) => s.watchlists);
  const toggleWatch = useAppStore((s) => s.toggleWatch);
  const [showWatchMenu, setShowWatchMenu] = useState(false);

  if (isLoading) return <div className="p-6"><LoadingState label={`Loading live data for ${ticker}…`} /></div>;
  if (isError || !c) {
    return (
      <div className="p-6 max-w-2xl mx-auto">
        <ErrorState error={error ?? new Error("Company not found.")} onRetry={() => refetch()} />
        <Link to="/search" className="text-brand-400 text-sm mt-4 inline-block">Back to search</Link>
      </div>
    );
  }

  const isWatchedAnywhere = watchlists.some((w) => w.entries.some((e) => e.ticker === c.ticker));

  return (
    <div className="p-4 md:p-6 max-w-[1200px] mx-auto space-y-6">
      <Disclaimer compact />

      {/* Company Overview */}
      <section className="card p-5">
        <div className="flex flex-wrap items-start justify-between gap-4">
          <div className="flex items-start gap-4">
            <CompanyLogo initials={c.logoInitials} color={c.logoColor} size={56} />
            <div>
              <div className="flex items-center gap-2 flex-wrap">
                <h1 className="text-xl font-bold text-text-primary">{c.name}</h1>
                <span className="text-sm text-text-secondary">{c.ticker}</span>
              </div>
              <p className="text-sm text-text-secondary mt-0.5">
                {c.exchange} · {c.country} · {c.psxSector ?? c.sector}
              </p>
              <div className="flex items-center gap-3 mt-2">
                <a href={`https://${c.website}`} target="_blank" rel="noreferrer" className="text-xs text-brand-400 flex items-center gap-1 hover:underline">
                  <Globe className="h-3 w-3" /> {c.website} <ExternalLink className="h-2.5 w-2.5" />
                </a>
                <span className="text-xs text-text-secondary">CEO: {c.ceo}</span>
              </div>
            </div>
          </div>
          <div className="text-right">
            <p className="text-3xl font-bold text-text-primary">{formatCurrency(c.price, c.currency)}</p>
            <PriceChange percent={c.changePercent} abs={c.changeAbs} size="md" />
            <div className="relative mt-3">
              <button
                onClick={() => setShowWatchMenu((s) => !s)}
                className={`inline-flex items-center gap-1.5 rounded-lg px-3 py-1.5 text-xs font-medium transition-colors ${
                  isWatchedAnywhere ? "bg-positive-bg text-positive" : "bg-brand-500 text-white hover:bg-brand-600"
                }`}
              >
                {isWatchedAnywhere ? <Check className="h-3.5 w-3.5" /> : <Plus className="h-3.5 w-3.5" />}
                {isWatchedAnywhere ? "On Watchlist" : "Add to Watchlist"}
              </button>
              {showWatchMenu && (
                <div className="absolute right-0 mt-1 w-48 card border-border-default shadow-xl z-10 py-1">
                  {watchlists.map((w) => {
                    const watched = w.entries.some((e) => e.ticker === c.ticker);
                    return (
                      <button key={w.id} onClick={() => toggleWatch(w.id, c.ticker)} className="w-full flex items-center justify-between px-3 py-2 text-xs text-text-primary hover:bg-bg-hover">
                        {w.name}
                        {watched && <Check className="h-3.5 w-3.5 text-positive" />}
                      </button>
                    );
                  })}
                </div>
              )}
            </div>
          </div>
        </div>

        <div className="grid grid-cols-2 sm:grid-cols-4 lg:grid-cols-6 gap-3 mt-5 pt-5 border-t border-border-subtle">
          <Stat label="Market Cap" value={orNA(c.marketCap, (v) => formatCurrency(v, c.currency, { compact: true }))} />
          <Stat label="52W High" value={orNA(c.week52High, (v) => formatCurrency(v, c.currency))} />
          <Stat label="52W Low" value={orNA(c.week52Low, (v) => formatCurrency(v, c.currency))} />
          <Stat label="P/E Ratio (TTM)" value={orNA(c.peRatio, (v) => `${v.toFixed(2)}x`)} />
          <Stat label="EPS" value={orNA(c.eps, (v) => formatCurrency(v, c.currency))} />
          <Stat label="Prev Close" value={formatCurrency(c.previousClose, c.currency)} />
          <Stat label="Open" value={orNA(c.dayOpen, (v) => formatCurrency(v, c.currency))} />
          <Stat label="Day High" value={orNA(c.dayHigh, (v) => formatCurrency(v, c.currency))} />
          <Stat label="Day Low" value={orNA(c.dayLow, (v) => formatCurrency(v, c.currency))} />
          <Stat label="Volume" value={orNA(c.volume, (v) => formatCompactNumber(v))} />
          <Stat label="Shares Out." value={orNA(c.sharesOutstanding, (v) => formatCompactNumber(v))} />
        </div>
        <p className="text-sm text-text-secondary leading-relaxed mt-5">{c.description}</p>
        <p className="text-[11px] text-text-secondary mt-3">
          Price &amp; history: {c.priceSource}
          {c.fundamentalsSource ? ` · Fundamentals: ${c.fundamentalsSource}` : ""} · Updated {formatRelativeTime(c.lastUpdated)}
          {c.asOf ? ` · Quote as of ${formatRelativeTime(c.asOf)}` : ""}
        </p>
      </section>

      {/* Price chart */}
      <section className="card p-5">
        <h2 className="text-sm font-semibold text-text-primary mb-3">Price Performance</h2>
        <PriceChart history={c.priceHistory} currency={c.currency} positive={c.changePercent >= 0} />
        <p className="text-[11px] text-text-secondary mt-2">
          End-of-day closing prices from the PSX Data Portal. Intraday high/low are not published on the free feed.
        </p>
      </section>

      {/* Analytics not available on free data */}
      <section className="card p-5">
        <h2 className="text-sm font-semibold text-text-primary mb-4">Analysis &amp; Scores</h2>
        <DataUnavailable
          title="Health &amp; valuation scores not available"
          reason="Financial-health breakdowns, fair-value estimates, and growth/risk ratings require paid fundamental and analyst-research feeds. Rather than fabricate them, they are omitted. The market data above (price, P/E, EPS, market cap, history) is real and live from PSX."
        />
      </section>

      <section className="card p-5">
        <h2 className="text-sm font-semibold text-text-primary mb-4">Financial Statements</h2>
        <FinancialStatements data={statements} isLoading={statementsLoading} currency={c.currency} />
        <p className="text-[11px] text-text-secondary mt-3">
          Live fundamentals (P/E, EPS, market cap, 52-week range) are shown in the overview above where PSX reports them
          {c.asOf ? `, as of ${formatRelativeTime(c.asOf)}` : ""}.
        </p>
      </section>

      <section className="card p-5">
        <h2 className="text-sm font-semibold text-text-primary mb-4">News &amp; Sentiment</h2>
        <DataUnavailable
          title="News &amp; sentiment not available"
          reason="No reliable free per-company news feed exists for PSX tickers, so headlines and sentiment are not shown rather than generated."
        />
      </section>

      <Disclaimer />
    </div>
  );
}

function Stat({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <p className="text-[11px] text-text-secondary">{label}</p>
      <p className="text-sm font-semibold text-text-primary mt-0.5">{value}</p>
    </div>
  );
}
