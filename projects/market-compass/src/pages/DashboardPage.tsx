import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import {
  TrendingUp,
  TrendingDown,
  Flame,
  ShieldAlert,
  Sparkles,
  Newspaper,
  Calendar,
  Gift,
} from "lucide-react";
import { companies } from "@/data/companies";
import { marketIndices, economicEvents, dailyBriefing } from "@/data/markets";
import type { MarketRegion } from "@/types";
import { CompanyLogo } from "@/components/ui/CompanyLogo";
import { PriceChange } from "@/components/ui/PriceChange";
import { Sparkline } from "@/components/ui/Sparkline";
import { SignalBadge } from "@/components/ui/Badge";
import { Disclaimer } from "@/components/ui/Disclaimer";
import { formatCurrency, formatCompactNumber, formatDate } from "@/lib/format";
import { useAppStore } from "@/store/useAppStore";

const REGIONS: (MarketRegion | "All")[] = ["All", "Pakistan", "United States", "United Kingdom", "Japan", "Europe", "Global"];

export function DashboardPage() {
  const [region, setRegion] = useState<MarketRegion | "All">("All");

  const filtered = useMemo(
    () => (region === "All" ? companies : companies.filter((c) => c.region === region)),
    [region]
  );

  const gainers = useMemo(() => [...filtered].sort((a, b) => b.changePercent - a.changePercent).slice(0, 5), [filtered]);
  const losers = useMemo(() => [...filtered].sort((a, b) => a.changePercent - b.changePercent).slice(0, 5), [filtered]);
  const mostActive = useMemo(
    () => [...filtered].sort((a, b) => b.priceHistory[b.priceHistory.length - 1].volume - a.priceHistory[a.priceHistory.length - 1].volume).slice(0, 5),
    [filtered]
  );

  const opportunities = useMemo(
    () => [...filtered].sort((a, b) => b.scoreBreakdown.overall - a.scoreBreakdown.overall).slice(0, 6),
    [filtered]
  );
  const riskAlerts = useMemo(
    () => [...filtered].sort((a, b) => a.scoreBreakdown.overall - b.scoreBreakdown.overall).slice(0, 6),
    [filtered]
  );

  const sectorPerf = useMemo(() => {
    const map = new Map<string, { total: number; count: number }>();
    filtered.forEach((c) => {
      const cur = map.get(c.sector) ?? { total: 0, count: 0 };
      map.set(c.sector, { total: cur.total + c.changePercent, count: cur.count + 1 });
    });
    return [...map.entries()].map(([sector, v]) => ({ sector, avg: v.total / v.count })).sort((a, b) => b.avg - a.avg);
  }, [filtered]);

  const sentimentScore = Math.round(filtered.reduce((s, c) => s + c.health.marketSentiment, 0) / Math.max(filtered.length, 1));

  const watchlists = useAppStore((s) => s.watchlists);
  const watchedTickers = useMemo(() => new Set(watchlists.flatMap((w) => w.entries.map((e) => e.ticker))), [watchlists]);
  const watchedCompanies = companies.filter((c) => watchedTickers.has(c.ticker));
  const watchGainers = [...watchedCompanies].sort((a, b) => b.changePercent - a.changePercent).slice(0, 3);
  const watchLosers = [...watchedCompanies].sort((a, b) => a.changePercent - b.changePercent).slice(0, 3);
  const upcomingEarnings = [...watchedCompanies].sort((a, b) => +new Date(a.nextEarningsDate) - +new Date(b.nextEarningsDate)).slice(0, 4);

  const relevantIndices = region === "All" ? marketIndices.slice(0, 6) : marketIndices.filter((i) => i.region === region || i.region === "Global").slice(0, 6);
  const relevantEvents = (region === "All" ? economicEvents : economicEvents.filter((e) => e.region === region || e.region === "Global")).slice(0, 5);

  return (
    <div className="p-4 md:p-6 max-w-[1400px] mx-auto space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold text-text-primary tracking-tight">Dashboard</h1>
          <p className="text-sm text-text-secondary mt-1">Your daily market overview and signal-driven opportunities.</p>
        </div>
        <div className="flex items-center gap-1.5 overflow-x-auto">
          {REGIONS.map((r) => (
            <button
              key={r}
              onClick={() => setRegion(r)}
              className={`shrink-0 rounded-full px-3.5 py-1.5 text-xs font-medium transition-colors ${
                region === r ? "bg-brand-500 text-white" : "bg-bg-elevated text-text-secondary hover:bg-bg-hover"
              }`}
            >
              {r === "All" ? "Global" : r}
            </button>
          ))}
        </div>
      </div>

      <Disclaimer compact />

      {/* Market overview cards */}
      <section>
        <h2 className="text-sm font-semibold text-text-primary mb-3">Market Overview</h2>
        <div className="grid grid-cols-2 lg:grid-cols-6 gap-3">
          {relevantIndices.map((idx) => (
            <div key={idx.name} className="card p-3.5">
              <p className="text-xs text-text-secondary truncate">{idx.name}</p>
              <p className="text-base font-semibold text-text-primary mt-1">{idx.value.toLocaleString()}</p>
              <PriceChange percent={idx.changePercent} abs={idx.changeAbs} />
            </div>
          ))}
        </div>
      </section>

      <div className="grid lg:grid-cols-3 gap-4">
        <PanelList title="Top Gainers" icon={TrendingUp} iconTone="text-positive" companies={gainers} />
        <PanelList title="Top Losers" icon={TrendingDown} iconTone="text-negative" companies={losers} />
        <PanelList title="Most Active" icon={Flame} iconTone="text-warning" companies={mostActive} showVolume />
      </div>

      <div className="grid lg:grid-cols-3 gap-4">
        <div className="card p-4 lg:col-span-2">
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
        <div className="card p-4 flex flex-col">
          <h3 className="text-sm font-semibold text-text-primary mb-3">Market Sentiment</h3>
          <div className="flex-1 flex flex-col items-center justify-center">
            <div className="relative h-28 w-28">
              <svg viewBox="0 0 36 36" className="h-28 w-28 -rotate-90">
                <circle cx="18" cy="18" r="15.5" fill="none" stroke="var(--border-subtle)" strokeWidth="3" />
                <circle
                  cx="18"
                  cy="18"
                  r="15.5"
                  fill="none"
                  stroke={sentimentScore >= 65 ? "var(--color-positive)" : sentimentScore >= 45 ? "var(--color-warning)" : "var(--color-negative)"}
                  strokeWidth="3"
                  strokeDasharray={`${(sentimentScore / 100) * 97.4} 97.4`}
                  strokeLinecap="round"
                />
              </svg>
              <div className="absolute inset-0 flex items-center justify-center text-xl font-bold text-text-primary">{sentimentScore}</div>
            </div>
            <p className="text-xs text-text-secondary mt-3 text-center">
              {sentimentScore >= 65 ? "Bullish sentiment across covered companies" : sentimentScore >= 45 ? "Mixed / neutral market sentiment" : "Cautious sentiment, elevated risk signals"}
            </p>
          </div>
        </div>
      </div>

      {/* Top opportunities & risk alerts */}
      <div className="grid lg:grid-cols-2 gap-4">
        <SignalSection title="Top Opportunities Today" icon={Sparkles} iconTone="text-positive" companies={opportunities} />
        <SignalSection title="Risk Alerts" icon={ShieldAlert} iconTone="text-negative" companies={riskAlerts} />
      </div>

      {/* Watchlist summary */}
      <section className="card p-4">
        <div className="flex items-center justify-between mb-4">
          <h3 className="text-sm font-semibold text-text-primary">Watchlist Summary</h3>
          <Link to="/watchlists" className="text-xs text-brand-400 hover:underline">
            Manage watchlists →
          </Link>
        </div>
        <div className="grid md:grid-cols-3 gap-4">
          <div>
            <p className="text-xs text-text-secondary mb-2">Biggest Gainers</p>
            <div className="space-y-2">
              {watchGainers.map((c) => (
                <CompanyRow key={c.ticker} company={c} />
              ))}
              {watchGainers.length === 0 && <p className="text-xs text-text-secondary">No watchlist companies yet.</p>}
            </div>
          </div>
          <div>
            <p className="text-xs text-text-secondary mb-2">Biggest Losers</p>
            <div className="space-y-2">
              {watchLosers.map((c) => (
                <CompanyRow key={c.ticker} company={c} />
              ))}
              {watchLosers.length === 0 && <p className="text-xs text-text-secondary">No watchlist companies yet.</p>}
            </div>
          </div>
          <div>
            <p className="text-xs text-text-secondary mb-2 flex items-center gap-1.5">
              <Calendar className="h-3.5 w-3.5" /> Upcoming Earnings
            </p>
            <div className="space-y-2">
              {upcomingEarnings.map((c) => (
                <div key={c.ticker} className="flex items-center justify-between text-sm">
                  <Link to={`/company/${c.ticker}`} className="text-text-primary hover:text-brand-400">
                    {c.ticker}
                  </Link>
                  <span className="text-xs text-text-secondary">{formatDate(c.nextEarningsDate)}</span>
                </div>
              ))}
            </div>
            <p className="text-xs text-text-secondary mt-3 flex items-center gap-1.5">
              <Gift className="h-3.5 w-3.5" /> {watchedCompanies.filter((c) => c.dividendYield > 0).length} watchlist companies pay dividends
            </p>
          </div>
        </div>
      </section>

      {/* Daily briefing */}
      <section className="card p-5">
        <div className="flex items-center gap-2 mb-3">
          <Newspaper className="h-4 w-4 text-brand-400" />
          <h3 className="text-sm font-semibold text-text-primary">Daily Market Briefing</h3>
          <span className="text-[11px] text-text-secondary">AI-generated summary</span>
        </div>
        <p className="text-sm text-text-secondary leading-relaxed mb-4">{dailyBriefing.summary}</p>
        <ul className="space-y-1.5 mb-4">
          {dailyBriefing.keyEvents.map((e) => (
            <li key={e} className="text-sm text-text-secondary flex gap-2">
              <span className="text-brand-400">•</span> {e}
            </li>
          ))}
        </ul>
        <div className="border-t border-border-subtle pt-4">
          <p className="text-xs text-text-secondary mb-2">Upcoming Economic Calendar</p>
          <div className="space-y-2">
            {relevantEvents.map((e) => (
              <div key={e.title} className="flex items-center justify-between text-sm">
                <div>
                  <span className="text-text-primary">{e.title}</span>
                  <span className="text-text-secondary"> · {e.region}</span>
                </div>
                <span className="text-xs text-text-secondary">{formatDate(e.date)}</span>
              </div>
            ))}
          </div>
        </div>
      </section>
    </div>
  );
}

function CompanyRow({ company }: { company: (typeof companies)[number] }) {
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
  companies: list,
  showVolume,
}: {
  title: string;
  icon: typeof TrendingUp;
  iconTone: string;
  companies: typeof companies;
  showVolume?: boolean;
}) {
  return (
    <div className="card p-4">
      <div className="flex items-center gap-2 mb-3">
        <Icon className={`h-4 w-4 ${iconTone}`} />
        <h3 className="text-sm font-semibold text-text-primary">{title}</h3>
      </div>
      <div className="space-y-3">
        {list.map((c) => (
          <Link key={c.ticker} to={`/company/${c.ticker}`} className="flex items-center gap-3 hover:bg-bg-hover -mx-1.5 px-1.5 py-1 rounded-lg transition-colors">
            <CompanyLogo initials={c.logoInitials} color={c.logoColor} size={28} />
            <div className="flex-1 min-w-0">
              <p className="text-sm font-medium text-text-primary truncate">{c.ticker}</p>
              <p className="text-[11px] text-text-secondary truncate">
                {showVolume ? `Vol ${formatCompactNumber(c.priceHistory[c.priceHistory.length - 1].volume)}` : c.exchange}
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

function SignalSection({
  title,
  icon: Icon,
  iconTone,
  companies: list,
}: {
  title: string;
  icon: typeof Sparkles;
  iconTone: string;
  companies: typeof companies;
}) {
  return (
    <div className="card p-4">
      <div className="flex items-center gap-2 mb-3">
        <Icon className={`h-4 w-4 ${iconTone}`} />
        <h3 className="text-sm font-semibold text-text-primary">{title}</h3>
      </div>
      <div className="grid sm:grid-cols-2 gap-2.5">
        {list.map((c) => (
          <Link key={c.ticker} to={`/company/${c.ticker}`} className="rounded-xl border border-border-subtle hover:border-border-default p-3 transition-colors">
            <div className="flex items-center gap-2 mb-2">
              <CompanyLogo initials={c.logoInitials} color={c.logoColor} size={26} />
              <div className="min-w-0 flex-1">
                <p className="text-xs font-medium text-text-primary truncate">{c.name}</p>
                <p className="text-[10px] text-text-secondary">{c.ticker} · {c.sector}</p>
              </div>
            </div>
            <div className="flex items-center justify-between">
              <Sparkline data={c.sparkline} positive={c.changePercent >= 0} width={64} height={24} />
              <SignalBadge signal={c.scoreBreakdown.signal} className="!px-1.5 !py-0.5 !text-[10px]" />
            </div>
          </Link>
        ))}
      </div>
    </div>
  );
}
