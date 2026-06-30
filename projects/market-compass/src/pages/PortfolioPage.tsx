import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { Plus, Trash2, X } from "lucide-react";
import { Cell, Pie, PieChart, ResponsiveContainer, Tooltip } from "recharts";
import { useAppStore } from "@/store/useAppStore";
import { getCompany } from "@/data/companies";
import { PageHeader } from "@/components/ui/PageHeader";
import { CompanyLogo } from "@/components/ui/CompanyLogo";
import { Disclaimer } from "@/components/ui/Disclaimer";
import { formatCurrency, formatPercent, formatDate } from "@/lib/format";
import { convertCurrency } from "@/lib/currency";
import type { Currency } from "@/types";

const PIE_COLORS = ["#3b82f6", "#8b5cf6", "#14b8a6", "#f59e0b", "#22c55e", "#ef4444", "#60a5fa", "#a78bfa"];

export function PortfolioPage() {
  const portfolio = useAppStore((s) => s.portfolio);
  const addHolding = useAppStore((s) => s.addHolding);
  const removeHolding = useAppStore((s) => s.removeHolding);
  const baseCurrency = useAppStore((s) => s.baseCurrency);
  const setBaseCurrency = useAppStore((s) => s.setBaseCurrency);
  const [showAdd, setShowAdd] = useState(false);

  const rows = useMemo(
    () =>
      portfolio
        .map((h) => {
          const c = getCompany(h.ticker);
          if (!c) return null;
          const currentValue = c.price * h.shares;
          const costBasis = h.purchasePrice * h.shares;
          const pl = currentValue - costBasis;
          const plPercent = (pl / costBasis) * 100;
          const currentValueBase = convertCurrency(currentValue, c.currency, baseCurrency);
          const costBasisBase = convertCurrency(costBasis, c.currency, baseCurrency);
          return { holding: h, company: c, currentValue, costBasis, pl, plPercent, currentValueBase, costBasisBase };
        })
        .filter((r): r is NonNullable<typeof r> => r !== null),
    [portfolio, baseCurrency]
  );

  const totalValue = rows.reduce((s, r) => s + r.currentValueBase, 0);
  const totalCost = rows.reduce((s, r) => s + r.costBasisBase, 0);
  const totalPL = totalValue - totalCost;
  const totalPLPercent = totalCost ? (totalPL / totalCost) * 100 : 0;
  const dividendIncome = rows.reduce((s, r) => s + (r.company.dividendYield / 100) * r.currentValueBase, 0);

  const sectorAlloc = useMemo(() => {
    const map = new Map<string, number>();
    rows.forEach((r) => map.set(r.company.sector, (map.get(r.company.sector) ?? 0) + r.currentValueBase));
    return [...map.entries()].map(([name, value]) => ({ name, value }));
  }, [rows]);

  const countryAlloc = useMemo(() => {
    const map = new Map<string, number>();
    rows.forEach((r) => map.set(r.company.country, (map.get(r.company.country) ?? 0) + r.currentValueBase));
    return [...map.entries()].map(([name, value]) => ({ name, value }));
  }, [rows]);

  const currencyAlloc = useMemo(() => {
    const map = new Map<string, number>();
    rows.forEach((r) => map.set(r.company.currency, (map.get(r.company.currency) ?? 0) + r.currentValueBase));
    return [...map.entries()].map(([name, value]) => ({ name, value }));
  }, [rows]);

  const sorted = [...rows].sort((a, b) => b.plPercent - a.plPercent);
  const best = sorted[0];
  const worst = sorted[sorted.length - 1];

  return (
    <div className="p-4 md:p-6 max-w-[1400px] mx-auto space-y-6">
      <PageHeader
        title="Portfolio"
        subtitle="Manually track your holdings, performance, and allocation. Not connected to any brokerage."
        actions={
          <button onClick={() => setShowAdd(true)} className="inline-flex items-center gap-1.5 bg-brand-500 hover:bg-brand-600 text-white text-sm font-medium px-3.5 py-2 rounded-lg transition-colors">
            <Plus className="h-4 w-4" /> Add Holding
          </button>
        }
      />

      {showAdd && <AddHoldingForm onClose={() => setShowAdd(false)} onAdd={addHolding} />}

      <div className="flex items-center justify-between flex-wrap gap-2">
        <p className="text-xs text-text-secondary">Portfolio valued in base currency. Holdings are converted using illustrative FX rates.</p>
        <div className="flex items-center gap-1.5">
          {(["USD", "PKR", "GBP", "EUR", "JPY"] as Currency[]).map((cur) => (
            <button
              key={cur}
              onClick={() => setBaseCurrency(cur)}
              className={`rounded-full px-3 py-1 text-xs font-medium transition-colors ${
                baseCurrency === cur ? "bg-brand-500 text-white" : "bg-bg-elevated text-text-secondary hover:bg-bg-hover"
              }`}
            >
              {cur}
            </button>
          ))}
        </div>
      </div>

      <div className="grid sm:grid-cols-2 lg:grid-cols-4 gap-3">
        <SummaryCard label="Total Value" value={formatCurrency(totalValue, baseCurrency, { compact: true })} />
        <SummaryCard label="Total Cost Basis" value={formatCurrency(totalCost, baseCurrency, { compact: true })} />
        <SummaryCard label="Total P/L" value={`${totalPL >= 0 ? "+" : ""}${formatCurrency(totalPL, baseCurrency, { compact: true })}`} tone={totalPL >= 0 ? "positive" : "negative"} sub={formatPercent(totalPLPercent)} />
        <SummaryCard label="Est. Annual Dividend Income" value={formatCurrency(dividendIncome, baseCurrency, { compact: true })} />
      </div>

      <div className="grid lg:grid-cols-3 gap-4">
        <AllocCard title="Sector Allocation" data={sectorAlloc} />
        <AllocCard title="Country Allocation" data={countryAlloc} />
        <AllocCard title="Currency Exposure" data={currencyAlloc} />
      </div>

      {best && worst && (
        <div className="grid sm:grid-cols-2 gap-4">
          <div className="card p-4">
            <p className="text-xs text-text-secondary mb-2">Best Performer</p>
            <div className="flex items-center justify-between">
              <Link to={`/company/${best.company.ticker}`} className="font-medium text-text-primary">{best.company.name}</Link>
              <span className="text-positive font-medium">{formatPercent(best.plPercent)}</span>
            </div>
          </div>
          <div className="card p-4">
            <p className="text-xs text-text-secondary mb-2">Worst Performer</p>
            <div className="flex items-center justify-between">
              <Link to={`/company/${worst.company.ticker}`} className="font-medium text-text-primary">{worst.company.name}</Link>
              <span className={worst.plPercent >= 0 ? "text-positive font-medium" : "text-negative font-medium"}>{formatPercent(worst.plPercent)}</span>
            </div>
          </div>
        </div>
      )}

      <div className="overflow-x-auto card">
        <table className="w-full text-sm">
          <thead>
            <tr className="text-xs text-text-secondary border-b border-border-subtle">
              <th className="text-left py-3 px-4">Holding</th>
              <th className="text-right py-3 px-3">Shares</th>
              <th className="text-right py-3 px-3">Avg Cost</th>
              <th className="text-right py-3 px-3">Current Price</th>
              <th className="text-right py-3 px-3">Market Value</th>
              <th className="text-right py-3 px-3">P/L</th>
              <th className="text-right py-3 px-4"></th>
            </tr>
          </thead>
          <tbody>
            {rows.map((r) => (
              <tr key={r.holding.id} className="border-b border-border-subtle/50 hover:bg-bg-hover">
                <td className="py-2.5 px-4">
                  <Link to={`/company/${r.company.ticker}`} className="flex items-center gap-2.5">
                    <CompanyLogo initials={r.company.logoInitials} color={r.company.logoColor} size={26} />
                    <div>
                      <p className="text-text-primary font-medium">{r.company.ticker}</p>
                      <p className="text-[11px] text-text-secondary">Bought {formatDate(r.holding.purchaseDate)}</p>
                    </div>
                  </Link>
                </td>
                <td className="text-right px-3 text-text-secondary">{r.holding.shares}</td>
                <td className="text-right px-3 text-text-secondary">{formatCurrency(r.holding.purchasePrice, r.company.currency)}</td>
                <td className="text-right px-3 text-text-secondary">{formatCurrency(r.company.price, r.company.currency)}</td>
                <td className="text-right px-3 text-text-primary font-medium">{formatCurrency(r.currentValue, r.company.currency)}</td>
                <td className={`text-right px-3 font-medium ${r.pl >= 0 ? "text-positive" : "text-negative"}`}>{formatPercent(r.plPercent)}</td>
                <td className="text-right px-4">
                  <button onClick={() => removeHolding(r.holding.id)} className="text-text-secondary hover:text-negative">
                    <Trash2 className="h-4 w-4" />
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        {rows.length === 0 && <p className="text-center text-sm text-text-secondary py-10">No holdings yet. Add your first investment above.</p>}
      </div>

      <Disclaimer compact />
    </div>
  );
}

function SummaryCard({ label, value, sub, tone }: { label: string; value: string; sub?: string; tone?: "positive" | "negative" }) {
  return (
    <div className="card p-4">
      <p className="text-xs text-text-secondary">{label}</p>
      <p className={`text-xl font-semibold mt-1 ${tone === "positive" ? "text-positive" : tone === "negative" ? "text-negative" : "text-text-primary"}`}>{value}</p>
      {sub && <p className={`text-xs mt-0.5 ${tone === "positive" ? "text-positive" : "text-negative"}`}>{sub}</p>}
    </div>
  );
}

function AllocCard({ title, data }: { title: string; data: { name: string; value: number }[] }) {
  return (
    <div className="card p-4">
      <h3 className="text-sm font-semibold text-text-primary mb-2">{title}</h3>
      <div className="h-40">
        <ResponsiveContainer width="100%" height="100%">
          <PieChart>
            <Pie data={data} dataKey="value" nameKey="name" cx="50%" cy="50%" innerRadius={35} outerRadius={60} paddingAngle={2}>
              {data.map((_, i) => (
                <Cell key={i} fill={PIE_COLORS[i % PIE_COLORS.length]} />
              ))}
            </Pie>
            <Tooltip
              contentStyle={{ background: "var(--bg-elevated)", border: "1px solid var(--border-default)", borderRadius: 8, fontSize: 12 }}
              formatter={(v) => `$${Number(v).toLocaleString(undefined, { maximumFractionDigits: 0 })}`}
            />
          </PieChart>
        </ResponsiveContainer>
      </div>
      <div className="space-y-1 mt-1">
        {data.map((d, i) => (
          <div key={d.name} className="flex items-center gap-2 text-xs">
            <span className="h-2 w-2 rounded-full shrink-0" style={{ backgroundColor: PIE_COLORS[i % PIE_COLORS.length] }} />
            <span className="text-text-secondary flex-1 truncate">{d.name}</span>
          </div>
        ))}
      </div>
    </div>
  );
}

function AddHoldingForm({ onClose, onAdd }: { onClose: () => void; onAdd: (h: { ticker: string; shares: number; purchasePrice: number; purchaseDate: string; currency: Currency }) => void }) {
  const [ticker, setTicker] = useState("");
  const [shares, setShares] = useState("");
  const [price, setPrice] = useState("");
  const [date, setDate] = useState(new Date().toISOString().slice(0, 10));
  const [currency, setCurrency] = useState<Currency>("USD");

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    const company = getCompany(ticker.toUpperCase());
    if (!company || !shares || !price) return;
    onAdd({ ticker: company.ticker, shares: Number(shares), purchasePrice: Number(price), purchaseDate: date, currency: company.currency });
    onClose();
  }

  return (
    <form onSubmit={handleSubmit} className="card p-4 grid sm:grid-cols-2 lg:grid-cols-5 gap-3 items-end">
      <div>
        <label className="text-xs text-text-secondary mb-1.5 block">Ticker</label>
        <input value={ticker} onChange={(e) => setTicker(e.target.value)} placeholder="e.g. AAPL" required className="w-full bg-bg-elevated border border-border-subtle rounded-lg px-3 py-2 text-sm outline-none focus:border-brand-500" />
      </div>
      <div>
        <label className="text-xs text-text-secondary mb-1.5 block">Shares</label>
        <input value={shares} onChange={(e) => setShares(e.target.value)} type="number" min="0" step="any" required className="w-full bg-bg-elevated border border-border-subtle rounded-lg px-3 py-2 text-sm outline-none focus:border-brand-500" />
      </div>
      <div>
        <label className="text-xs text-text-secondary mb-1.5 block">Purchase Price</label>
        <input value={price} onChange={(e) => setPrice(e.target.value)} type="number" min="0" step="any" required className="w-full bg-bg-elevated border border-border-subtle rounded-lg px-3 py-2 text-sm outline-none focus:border-brand-500" />
      </div>
      <div>
        <label className="text-xs text-text-secondary mb-1.5 block">Purchase Date</label>
        <input value={date} onChange={(e) => setDate(e.target.value)} type="date" required className="w-full bg-bg-elevated border border-border-subtle rounded-lg px-3 py-2 text-sm outline-none focus:border-brand-500" />
      </div>
      <div className="flex items-center gap-2">
        <button type="submit" className="flex-1 bg-brand-500 hover:bg-brand-600 text-white text-sm font-medium px-3.5 py-2 rounded-lg">Add</button>
        <button type="button" onClick={onClose} className="text-text-secondary p-2"><X className="h-4 w-4" /></button>
      </div>
      <input type="hidden" value={currency} onChange={() => setCurrency(currency)} />
    </form>
  );
}
