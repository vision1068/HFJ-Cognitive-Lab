import { useState } from "react";
import { Link, useParams } from "react-router-dom";
import {
  Globe,
  ExternalLink,
  Plus,
  Check,
  TrendingUp,
  TrendingDown,
  AlertTriangle,
  Info,
} from "lucide-react";
import { getCompany } from "@/data/companies";
import { CompanyLogo } from "@/components/ui/CompanyLogo";
import { PriceChange } from "@/components/ui/PriceChange";
import { SignalBadge, Badge } from "@/components/ui/Badge";
import { ScoreGauge, ScoreBar } from "@/components/ui/ScoreGauge";
import { Disclaimer } from "@/components/ui/Disclaimer";
import { PriceChart } from "@/components/company/PriceChart";
import { FinancialsChart } from "@/components/company/FinancialsChart";
import { formatCurrency, formatCompactNumber, formatRelativeTime } from "@/lib/format";
import { useAppStore } from "@/store/useAppStore";
import { scoreColor } from "@/lib/signal";

export function CompanyDetailPage() {
  const { ticker } = useParams<{ ticker: string }>();
  const company = ticker ? getCompany(ticker) : undefined;
  const watchlists = useAppStore((s) => s.watchlists);
  const toggleWatch = useAppStore((s) => s.toggleWatch);
  const [showWatchMenu, setShowWatchMenu] = useState(false);

  if (!company) {
    return (
      <div className="p-10 text-center">
        <p className="text-text-secondary">Company not found.</p>
        <Link to="/search" className="text-brand-400 text-sm">Back to search</Link>
      </div>
    );
  }

  const c = company;
  const isWatchedAnywhere = watchlists.some((w) => w.entries.some((e) => e.ticker === c.ticker));

  return (
    <div className="p-4 md:p-6 max-w-[1200px] mx-auto space-y-6">
      <Disclaimer compact />

      {/* AI Research Summary */}
      <section className="card p-5 bg-gradient-to-br from-brand-500/5 to-accent-violet/5">
        <div className="flex items-center gap-2 mb-3">
          <Info className="h-4 w-4 text-brand-400" />
          <h2 className="text-sm font-semibold text-text-primary">AI Investment Research Summary</h2>
        </div>
        <div className="flex items-center gap-2 mb-3">
          <span className="text-xs text-text-secondary">Overall Signal:</span>
          <SignalBadge signal={c.aiSummary.overallSignal} />
        </div>
        <p className="text-sm text-text-secondary leading-relaxed mb-4">{c.aiSummary.thesis}</p>
        <div className="grid md:grid-cols-2 gap-4 mb-4">
          <div>
            <p className="text-xs font-medium text-positive mb-2 flex items-center gap-1.5">
              <TrendingUp className="h-3.5 w-3.5" /> Positive Signals
            </p>
            <ul className="space-y-1.5">
              {c.aiSummary.positiveSignals.map((s) => (
                <li key={s} className="text-sm text-text-secondary flex gap-2">
                  <span className="text-positive">•</span> {s}
                </li>
              ))}
            </ul>
          </div>
          <div>
            <p className="text-xs font-medium text-warning mb-2 flex items-center gap-1.5">
              <AlertTriangle className="h-3.5 w-3.5" /> Warning Signs
            </p>
            <ul className="space-y-1.5">
              {c.aiSummary.warningSigns.map((s) => (
                <li key={s} className="text-sm text-text-secondary flex gap-2">
                  <span className="text-warning">•</span> {s}
                </li>
              ))}
            </ul>
          </div>
        </div>
        <div className="flex flex-wrap items-center gap-4 border-t border-border-subtle pt-4">
          <div>
            <p className="text-xs text-text-secondary mb-1.5">Suggested Investor Profile</p>
            <div className="flex flex-wrap gap-1.5">
              {c.aiSummary.investorProfiles.map((p) => (
                <Badge key={p} tone="brand">{p}</Badge>
              ))}
            </div>
          </div>
          <div className="ml-auto">
            <p className="text-xs text-text-secondary mb-1.5">Suggested Action</p>
            <Badge tone={c.aiSummary.overallSignal === "High Risk" ? "negative" : c.aiSummary.overallSignal === "Caution" ? "warning" : "positive"}>
              {c.aiSummary.suggestedAction}
            </Badge>
          </div>
        </div>
      </section>

      {/* A. Company Overview */}
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
                {c.exchange} · {c.country} · {c.sector} — {c.industry}
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
                      <button
                        key={w.id}
                        onClick={() => toggleWatch(w.id, c.ticker)}
                        className="w-full flex items-center justify-between px-3 py-2 text-xs text-text-primary hover:bg-bg-hover"
                      >
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
          <Stat label="Market Cap" value={formatCurrency(c.marketCap, c.currency, { compact: true })} />
          <Stat label="52W High" value={formatCurrency(c.week52High, c.currency)} />
          <Stat label="52W Low" value={formatCurrency(c.week52Low, c.currency)} />
          <Stat label="P/E Ratio" value={c.peRatio.toFixed(1)} />
          <Stat label="EPS" value={formatCurrency(c.eps, c.currency)} />
          <Stat label="Beta" value={c.beta.toFixed(2)} />
          <Stat label="Dividend Yield" value={`${c.dividendYield.toFixed(2)}%`} />
          <Stat label="Analyst Target" value={c.analystTarget ? formatCurrency(c.analystTarget, c.currency) : "N/A"} />
        </div>
        <p className="text-sm text-text-secondary leading-relaxed mt-5">{c.description}</p>
        <p className="text-[11px] text-text-secondary mt-3">Data as of {formatRelativeTime(c.lastUpdated)} · Delayed mock data for demonstration</p>
      </section>

      {/* B. Price chart */}
      <section className="card p-5">
        <h2 className="text-sm font-semibold text-text-primary mb-3">Price Performance</h2>
        <PriceChart history={c.priceHistory} currency={c.currency} positive={c.changePercent >= 0} />
      </section>

      {/* C. Financial Health Score */}
      <section className="card p-5">
        <h2 className="text-sm font-semibold text-text-primary mb-4">Financial Health Score</h2>
        <div className="grid md:grid-cols-3 gap-6 items-center">
          <div className="flex justify-center">
            <ScoreGauge score={c.health.overall} size={150} />
          </div>
          <div className="md:col-span-2 grid sm:grid-cols-2 gap-x-6 gap-y-3">
            <ScoreBar label="Revenue Growth" score={c.health.revenueGrowth} />
            <ScoreBar label="Profit Growth" score={c.health.profitGrowth} />
            <ScoreBar label="Earnings Quality" score={c.health.earningsQuality} />
            <ScoreBar label="Cash Flow" score={c.health.cashFlow} />
            <ScoreBar label="Debt & Liquidity" score={c.health.debtLiquidity} />
            <ScoreBar label="Valuation" score={c.health.valuation} />
            <ScoreBar label="Dividend Sustainability" score={c.health.dividendSustainability} />
            <ScoreBar label="Technical Momentum" score={c.health.technicalMomentum} />
            <ScoreBar label="Market Sentiment" score={c.health.marketSentiment} />
            <ScoreBar label="Overall Risk (inverse)" score={100 - c.health.riskScore} />
          </div>
        </div>
      </section>

      {/* D. Financial Performance */}
      <section className="card p-5">
        <h2 className="text-sm font-semibold text-text-primary mb-3">Financial Performance</h2>
        <FinancialsChart annual={c.financialsAnnual} quarterly={c.financialsQuarterly} />
        <div className="overflow-x-auto mt-4">
          <table className="w-full text-xs">
            <thead>
              <tr className="text-text-secondary border-b border-border-subtle">
                <th className="text-left py-2 pr-4">Period</th>
                <th className="text-right py-2 px-2">Revenue</th>
                <th className="text-right py-2 px-2">Net Profit</th>
                <th className="text-right py-2 px-2">Margin</th>
                <th className="text-right py-2 px-2">ROE</th>
                <th className="text-right py-2 pl-2">EPS</th>
              </tr>
            </thead>
            <tbody>
              {c.financialsAnnual.map((f) => (
                <tr key={f.period} className="border-b border-border-subtle/50">
                  <td className="py-2 pr-4 text-text-primary">{f.period}</td>
                  <td className="text-right py-2 px-2 text-text-secondary">{formatCompactNumber(f.revenue)}</td>
                  <td className="text-right py-2 px-2 text-text-secondary">{formatCompactNumber(f.netProfit)}</td>
                  <td className="text-right py-2 px-2 text-text-secondary">{f.netMargin}%</td>
                  <td className="text-right py-2 px-2 text-text-secondary">{f.roe}%</td>
                  <td className="text-right py-2 pl-2 text-text-secondary">{formatCurrency(f.eps, c.currency)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>

      {/* E. Valuation Analysis */}
      <section className="card p-5">
        <h2 className="text-sm font-semibold text-text-primary mb-3">Valuation Analysis</h2>
        <div className="flex items-center gap-3 mb-4">
          <Badge tone={c.valuationVerdict === "Undervalued" ? "positive" : c.valuationVerdict === "Overvalued" ? "negative" : "neutral"}>
            {c.valuationVerdict}
          </Badge>
          <span className="text-sm text-text-secondary">
            Estimated fair value range: {formatCurrency(c.fairValueLow, c.currency)} – {formatCurrency(c.fairValueHigh, c.currency)}
          </span>
        </div>
        <div className="relative h-2 rounded-full bg-bg-elevated mb-5">
          <div
            className="absolute top-1/2 -translate-y-1/2 h-3 w-3 rounded-full bg-brand-400 ring-4 ring-brand-400/20"
            style={{ left: `${Math.min(Math.max(((c.price - c.fairValueLow * 0.7) / (c.fairValueHigh * 1.3 - c.fairValueLow * 0.7)) * 100, 2), 98)}%` }}
          />
        </div>
        <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
          <Stat label="P/E vs Sector" value={`${c.peRatio}x / ${c.sectorAvgPE}x`} />
          <Stat label="P/B Ratio" value={`${c.pbRatio}x`} />
          <Stat label="PEG Ratio" value={`${c.pegRatio}`} />
          <Stat label="EV/EBITDA" value={`${c.evEbitda}x`} />
        </div>
        <p className="text-xs text-text-secondary mt-4">
          Valuation calculations are estimates derived from comparable multiples and historical trends — not guaranteed predictions of future price.
        </p>
      </section>

      {/* F. Growth Potential */}
      <section className="card p-5">
        <h2 className="text-sm font-semibold text-text-primary mb-3">Future Growth Potential</h2>
        <Badge tone={c.growthRating.includes("High") ? "positive" : c.growthRating.includes("Moderate") ? "neutral" : "warning"} className="mb-3">
          {c.growthRating}
        </Badge>
        <ul className="grid sm:grid-cols-2 gap-2">
          {c.growthDrivers.map((g) => (
            <li key={g} className="text-sm text-text-secondary flex gap-2">
              <span className="text-brand-400">•</span> {g}
            </li>
          ))}
        </ul>
      </section>

      {/* G. Risk Analysis */}
      <section className="card p-5">
        <h2 className="text-sm font-semibold text-text-primary mb-4">Risk Analysis</h2>
        <div className="grid sm:grid-cols-2 gap-3">
          {c.riskFactors.map((r) => (
            <div key={r.label} className="rounded-xl border border-border-subtle p-3">
              <div className="flex items-center justify-between mb-1">
                <span className="text-sm font-medium text-text-primary">{r.label}</span>
                <Badge tone={r.level === "Low" ? "positive" : r.level === "Medium" ? "warning" : "negative"}>{r.level} Risk</Badge>
              </div>
              <p className="text-xs text-text-secondary">{r.description}</p>
            </div>
          ))}
        </div>
      </section>

      {/* H. News & Sentiment */}
      <section className="card p-5">
        <h2 className="text-sm font-semibold text-text-primary mb-4">News &amp; Sentiment</h2>
        <div className="space-y-3">
          {c.news.map((n) => (
            <div key={n.id} className="flex items-start gap-3 pb-3 border-b border-border-subtle/50 last:border-0 last:pb-0">
              <div className={`mt-1.5 h-2 w-2 rounded-full shrink-0 ${n.sentiment === "Positive" ? "bg-positive" : n.sentiment === "Negative" ? "bg-negative" : "bg-neutral"}`} />
              <div className="flex-1 min-w-0">
                <p className="text-sm text-text-primary">{n.headline}</p>
                <p className="text-xs text-text-secondary mt-0.5">{n.summary}</p>
                <div className="flex items-center gap-2 mt-1.5">
                  <Badge tone={n.sentiment === "Positive" ? "positive" : n.sentiment === "Negative" ? "negative" : "neutral"}>{n.sentiment}</Badge>
                  <Badge tone="neutral">{n.impact} Impact</Badge>
                  <span className="text-[11px] text-text-secondary">{n.source} · {formatRelativeTime(n.publishedAt)}</span>
                </div>
              </div>
            </div>
          ))}
        </div>
      </section>

      {/* Stock Signal Engine */}
      <section className="card p-5">
        <h2 className="text-sm font-semibold text-text-primary mb-1">Stock Signal Engine</h2>
        <p className="text-xs text-text-secondary mb-4">Transparent score breakdown — click each category to understand why it was assigned.</p>
        <div className="flex items-center gap-4 mb-5">
          <div className={`text-4xl font-bold ${scoreColor(c.scoreBreakdown.overall)}`}>{c.scoreBreakdown.overall}</div>
          <div>
            <SignalBadge signal={c.scoreBreakdown.signal} />
            <p className="text-xs text-text-secondary mt-1">out of 100</p>
          </div>
        </div>
        <div className="space-y-3">
          <ScoreReasonRow label="Fundamentals" weight={30} data={c.scoreBreakdown.fundamentals} />
          <ScoreReasonRow label="Growth Potential" weight={20} data={c.scoreBreakdown.growth} />
          <ScoreReasonRow label="Valuation" weight={15} data={c.scoreBreakdown.valuation} />
          <ScoreReasonRow label="Technical Indicators" weight={15} data={c.scoreBreakdown.technical} />
          <ScoreReasonRow label="News Sentiment" weight={10} data={c.scoreBreakdown.sentiment} />
          <ScoreReasonRow label="Risk Factors" weight={10} data={c.scoreBreakdown.risk} />
        </div>
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

function ScoreReasonRow({ label, weight, data }: { label: string; weight: number; data: { score: number; reason: string } }) {
  const [open, setOpen] = useState(false);
  return (
    <div className="rounded-xl border border-border-subtle overflow-hidden">
      <button onClick={() => setOpen((o) => !o)} className="w-full flex items-center gap-3 px-3.5 py-3 hover:bg-bg-hover transition-colors">
        {data.score >= 50 ? <TrendingUp className="h-4 w-4 text-positive shrink-0" /> : <TrendingDown className="h-4 w-4 text-negative shrink-0" />}
        <span className="text-sm font-medium text-text-primary">{label}</span>
        <span className="text-xs text-text-secondary">({weight}% weight)</span>
        <div className="flex-1 h-1.5 rounded-full bg-bg-elevated overflow-hidden mx-2">
          <div className={`h-full rounded-full ${data.score >= 65 ? "bg-positive" : data.score >= 50 ? "bg-neutral" : data.score >= 35 ? "bg-warning" : "bg-negative"}`} style={{ width: `${data.score}%` }} />
        </div>
        <span className={`text-sm font-semibold ${scoreColor(data.score)}`}>{data.score}/100</span>
      </button>
      {open && (
        <div className="px-3.5 pb-3 text-xs text-text-secondary border-t border-border-subtle pt-2.5">{data.reason}</div>
      )}
    </div>
  );
}
