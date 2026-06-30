import { Link } from "react-router-dom";
import { marketIndices, dailyBriefing } from "@/data/markets";
import { companies } from "@/data/companies";
import { PriceChange } from "@/components/ui/PriceChange";
import { formatRelativeTime } from "@/lib/format";
import { Badge } from "@/components/ui/Badge";

export function RightPanel() {
  const latestNews = companies
    .flatMap((c) => c.news.map((n) => ({ ...n, companyName: c.name })))
    .sort((a, b) => +new Date(b.publishedAt) - +new Date(a.publishedAt))
    .slice(0, 6);

  return (
    <aside className="hidden xl:flex flex-col w-80 shrink-0 border-l border-border-subtle bg-bg-surface overflow-y-auto">
      <div className="p-4 border-b border-border-subtle">
        <h3 className="text-sm font-semibold text-text-primary mb-3">Global Indices</h3>
        <div className="space-y-2.5">
          {marketIndices.slice(0, 6).map((idx) => (
            <div key={idx.name} className="flex items-center justify-between text-sm">
              <span className="text-text-secondary">{idx.name}</span>
              <div className="text-right">
                <p className="font-medium text-text-primary">{idx.value.toLocaleString()}</p>
                <PriceChange percent={idx.changePercent} />
              </div>
            </div>
          ))}
        </div>
      </div>

      <div className="p-4 border-b border-border-subtle">
        <h3 className="text-sm font-semibold text-text-primary mb-3">Commodities &amp; FX</h3>
        <div className="grid grid-cols-2 gap-2.5">
          {dailyBriefing.commodities.map((c) => (
            <div key={c.name} className="rounded-lg bg-bg-elevated px-2.5 py-2">
              <p className="text-[11px] text-text-secondary">{c.name}</p>
              <p className="text-sm font-medium text-text-primary">{c.value}</p>
              <PriceChange percent={c.change} />
            </div>
          ))}
        </div>
      </div>

      <div className="p-4 flex-1">
        <div className="flex items-center justify-between mb-3">
          <h3 className="text-sm font-semibold text-text-primary">Latest News</h3>
          <Link to="/news" className="text-xs text-brand-400 hover:underline">
            View all
          </Link>
        </div>
        <div className="space-y-3">
          {latestNews.map((n) => (
            <Link key={n.id} to={`/company/${n.ticker}`} className="block group">
              <p className="text-sm text-text-primary group-hover:text-brand-400 transition-colors leading-snug">{n.headline}</p>
              <div className="flex items-center gap-2 mt-1">
                <Badge tone={n.sentiment === "Positive" ? "positive" : n.sentiment === "Negative" ? "negative" : "neutral"} className="!px-1.5 !py-0 text-[10px]">
                  {n.sentiment}
                </Badge>
                <span className="text-[11px] text-text-secondary">{n.source} · {formatRelativeTime(n.publishedAt)}</span>
              </div>
            </Link>
          ))}
        </div>
      </div>
    </aside>
  );
}
