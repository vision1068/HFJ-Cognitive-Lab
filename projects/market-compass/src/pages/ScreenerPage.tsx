import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import type { Company } from "@/types";
import { companies } from "@/data/companies";
import { PageHeader } from "@/components/ui/PageHeader";
import { CompanyLogo } from "@/components/ui/CompanyLogo";
import { PriceChange } from "@/components/ui/PriceChange";
import { SignalBadge } from "@/components/ui/Badge";
import { formatCurrency } from "@/lib/format";

interface ReadyScreen {
  name: string;
  description: string;
  filter: (c: Company) => boolean;
}

const READY_SCREENS: ReadyScreen[] = [
  { name: "Best Pakistani Dividend Stocks", description: "PSX-listed companies with strong dividend yield", filter: (c) => c.region === "Pakistan" && c.dividendYield > 4 },
  { name: "High-Growth PSX Companies", description: "Pakistani companies with strong growth scores", filter: (c) => c.region === "Pakistan" && c.health.revenueGrowth > 65 },
  { name: "Undervalued US Companies", description: "US-listed companies trading below fair value", filter: (c) => c.region === "United States" && c.valuationVerdict === "Undervalued" },
  { name: "Strong Global Technology Stocks", description: "Technology sector leaders with strong overall scores", filter: (c) => c.sector === "Technology" && c.scoreBreakdown.overall > 65 },
  { name: "Low-Debt Companies", description: "Companies with strong debt & liquidity scores", filter: (c) => c.health.debtLiquidity > 70 },
  { name: "Strong Cash Flow Companies", description: "Companies with strong cash flow scores", filter: (c) => c.health.cashFlow > 70 },
  { name: "Companies with Positive Momentum", description: "Strong technical momentum scores", filter: (c) => c.health.technicalMomentum > 65 },
  { name: "Companies with Improving Earnings", description: "Strong profit growth scores", filter: (c) => c.health.profitGrowth > 65 },
  { name: "High-Risk, High-Reward Companies", description: "High growth potential but elevated risk", filter: (c) => c.health.riskScore > 55 && c.growthRating.includes("High") },
  { name: "Long-Term Compounder Candidates", description: "Consistently strong fundamentals and low risk", filter: (c) => c.scoreBreakdown.fundamentals.score > 70 && c.health.riskScore < 40 },
];

export function ScreenerPage() {
  const [activeScreen, setActiveScreen] = useState<string | null>(null);
  const [minMarketCap, setMinMarketCap] = useState(0);
  const [minDividend, setMinDividend] = useState(0);
  const [maxPE, setMaxPE] = useState(100);
  const [minHealth, setMinHealth] = useState(0);
  const [minGrowth, setMinGrowth] = useState(0);
  const [maxRisk, setMaxRisk] = useState(100);
  const [sector, setSector] = useState("All");

  const sectors = useMemo(() => ["All", ...Array.from(new Set(companies.map((c) => c.sector))).sort()], []);

  const results = useMemo(() => {
    let list = companies;
    const screen = READY_SCREENS.find((s) => s.name === activeScreen);
    if (screen) list = list.filter(screen.filter);
    list = list.filter(
      (c) =>
        c.marketCap >= minMarketCap &&
        c.dividendYield >= minDividend &&
        c.peRatio <= maxPE &&
        c.health.overall >= minHealth &&
        c.health.revenueGrowth >= minGrowth &&
        c.health.riskScore <= maxRisk &&
        (sector === "All" || c.sector === sector)
    );
    return list.sort((a, b) => b.scoreBreakdown.overall - a.scoreBreakdown.overall);
  }, [activeScreen, minMarketCap, minDividend, maxPE, minHealth, minGrowth, maxRisk, sector]);

  return (
    <div className="p-4 md:p-6 max-w-[1400px] mx-auto">
      <PageHeader title="Stock Screener" subtitle="Discover companies using fundamental, valuation, technical, and risk filters." />

      <div className="mb-5">
        <p className="text-xs font-medium text-text-secondary mb-2">Ready-Made Screeners</p>
        <div className="flex flex-wrap gap-1.5">
          {READY_SCREENS.map((s) => (
            <button
              key={s.name}
              onClick={() => setActiveScreen(activeScreen === s.name ? null : s.name)}
              title={s.description}
              className={`rounded-full px-3 py-1.5 text-xs font-medium transition-colors ${
                activeScreen === s.name ? "bg-brand-500 text-white" : "bg-bg-elevated text-text-secondary hover:bg-bg-hover"
              }`}
            >
              {s.name}
            </button>
          ))}
        </div>
      </div>

      <div className="card p-4 mb-5 grid sm:grid-cols-2 lg:grid-cols-3 gap-4">
        <FilterRange label="Min Market Cap" value={minMarketCap} max={5e9} step={1e7} onChange={setMinMarketCap} format={(v) => `$${(v / 1e6).toFixed(0)}M`} />
        <FilterRange label="Min Dividend Yield" value={minDividend} max={10} step={0.25} onChange={setMinDividend} format={(v) => `${v}%`} />
        <FilterRange label="Max P/E Ratio" value={maxPE} max={100} step={1} onChange={setMaxPE} format={(v) => `${v}x`} />
        <FilterRange label="Min Health Score" value={minHealth} max={100} step={1} onChange={setMinHealth} format={(v) => `${v}`} />
        <FilterRange label="Min Revenue Growth Score" value={minGrowth} max={100} step={1} onChange={setMinGrowth} format={(v) => `${v}`} />
        <FilterRange label="Max Risk Score" value={maxRisk} max={100} step={1} onChange={setMaxRisk} format={(v) => `${v}`} />
        <div>
          <label className="text-xs text-text-secondary mb-1.5 block">Sector</label>
          <select value={sector} onChange={(e) => setSector(e.target.value)} className="w-full bg-bg-elevated border border-border-subtle rounded-lg px-3 py-2 text-sm text-text-primary outline-none">
            {sectors.map((s) => (
              <option key={s} value={s}>{s}</option>
            ))}
          </select>
        </div>
      </div>

      <p className="text-xs text-text-secondary mb-3">{results.length} companies match your criteria</p>

      <div className="overflow-x-auto card">
        <table className="w-full text-sm">
          <thead>
            <tr className="text-xs text-text-secondary border-b border-border-subtle">
              <th className="text-left py-3 px-4">Company</th>
              <th className="text-right py-3 px-3">Price</th>
              <th className="text-right py-3 px-3">Change</th>
              <th className="text-right py-3 px-3">Mkt Cap</th>
              <th className="text-right py-3 px-3">P/E</th>
              <th className="text-right py-3 px-3">Div Yield</th>
              <th className="text-right py-3 px-3">Health</th>
              <th className="text-right py-3 px-4">Signal</th>
            </tr>
          </thead>
          <tbody>
            {results.map((c) => (
              <tr key={c.ticker} className="border-b border-border-subtle/50 hover:bg-bg-hover">
                <td className="py-2.5 px-4">
                  <Link to={`/company/${c.ticker}`} className="flex items-center gap-2.5">
                    <CompanyLogo initials={c.logoInitials} color={c.logoColor} size={26} />
                    <div>
                      <p className="text-text-primary font-medium">{c.ticker}</p>
                      <p className="text-[11px] text-text-secondary">{c.sector}</p>
                    </div>
                  </Link>
                </td>
                <td className="text-right px-3 text-text-primary">{formatCurrency(c.price, c.currency)}</td>
                <td className="text-right px-3"><PriceChange percent={c.changePercent} /></td>
                <td className="text-right px-3 text-text-secondary">{formatCurrency(c.marketCap, c.currency, { compact: true })}</td>
                <td className="text-right px-3 text-text-secondary">{c.peRatio.toFixed(1)}x</td>
                <td className="text-right px-3 text-text-secondary">{c.dividendYield.toFixed(2)}%</td>
                <td className="text-right px-3 text-text-secondary">{c.health.overall}</td>
                <td className="text-right px-4"><SignalBadge signal={c.scoreBreakdown.signal} /></td>
              </tr>
            ))}
          </tbody>
        </table>
        {results.length === 0 && <p className="text-center text-sm text-text-secondary py-10">No companies match these filters.</p>}
      </div>
    </div>
  );
}

function FilterRange({ label, value, max, step, onChange, format }: { label: string; value: number; max: number; step: number; onChange: (v: number) => void; format: (v: number) => string }) {
  return (
    <div>
      <div className="flex items-center justify-between mb-1.5">
        <label className="text-xs text-text-secondary">{label}</label>
        <span className="text-xs font-medium text-text-primary">{format(value)}</span>
      </div>
      <input type="range" min={0} max={max} step={step} value={value} onChange={(e) => onChange(Number(e.target.value))} className="w-full accent-brand-500" />
    </div>
  );
}
