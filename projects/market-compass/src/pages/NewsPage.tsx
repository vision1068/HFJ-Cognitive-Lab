import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { companies } from "@/data/companies";
import { PageHeader } from "@/components/ui/PageHeader";
import { Badge } from "@/components/ui/Badge";
import { CompanyLogo } from "@/components/ui/CompanyLogo";
import { formatRelativeTime } from "@/lib/format";

type SentimentFilter = "All" | "Positive" | "Neutral" | "Negative";

export function NewsPage() {
  const [sentiment, setSentiment] = useState<SentimentFilter>("All");

  const allNews = useMemo(
    () =>
      companies
        .flatMap((c) => c.news.map((n) => ({ ...n, company: c })))
        .sort((a, b) => +new Date(b.publishedAt) - +new Date(a.publishedAt)),
    []
  );

  const filtered = sentiment === "All" ? allNews : allNews.filter((n) => n.sentiment === sentiment);

  return (
    <div className="p-4 md:p-6 max-w-4xl mx-auto">
      <PageHeader title="News & Insights" subtitle="Latest company news with sentiment and impact analysis across all covered markets." />

      <div className="flex gap-1.5 mb-5">
        {(["All", "Positive", "Neutral", "Negative"] as SentimentFilter[]).map((s) => (
          <button
            key={s}
            onClick={() => setSentiment(s)}
            className={`rounded-full px-3.5 py-1.5 text-xs font-medium transition-colors ${
              sentiment === s ? "bg-brand-500 text-white" : "bg-bg-elevated text-text-secondary hover:bg-bg-hover"
            }`}
          >
            {s}
          </button>
        ))}
      </div>

      <div className="space-y-3">
        {filtered.map((n) => (
          <Link key={n.id} to={`/company/${n.ticker}`} className="card card-hover p-4 flex gap-3">
            <CompanyLogo initials={n.company.logoInitials} color={n.company.logoColor} size={36} />
            <div className="flex-1 min-w-0">
              <p className="text-sm font-medium text-text-primary">{n.headline}</p>
              <p className="text-xs text-text-secondary mt-1">{n.summary}</p>
              <div className="flex items-center gap-2 mt-2 flex-wrap">
                <Badge tone={n.sentiment === "Positive" ? "positive" : n.sentiment === "Negative" ? "negative" : "neutral"}>{n.sentiment}</Badge>
                <Badge tone="neutral">{n.impact} Impact</Badge>
                <span className="text-[11px] text-text-secondary">
                  {n.company.ticker} · {n.source} · {formatRelativeTime(n.publishedAt)}
                </span>
              </div>
            </div>
          </Link>
        ))}
      </div>
    </div>
  );
}
