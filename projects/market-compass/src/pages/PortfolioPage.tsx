import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { Plus, Trash2, X } from "lucide-react";
import { Cell, Pie, PieChart, ResponsiveContainer, Tooltip } from "recharts";
import { useAppStore } from "@/store/useAppStore";
import { PageHeader } from "@/components/ui/PageHeader";
import { CompanyLogo } from "@/components/ui/CompanyLogo";
import { Disclaimer } from "@/components/ui/Disclaimer";
import { LoadingState, DataUnavailable } from "@/components/ui/DataState";
import { useCompanies } from "@/hooks/useMarketData";
import { formatCurrency, formatPercent, formatDate } from "@/lib/format";

const PIE_COLORS = ["#3b82f6", "#8b5cf6", "#14b8a6", "#f59e0b", "#22c55e", "#ef4444", "#60a5fa", "#a78bfa"];

export function PortfolioPage() {
  const portfolio = useAppStore((s) => s.portfolio);
  const addHolding = useAppStore((s) => s.addHolding);
  const removeHolding = useAppStore((s) => s.removeHolding);
  const [showAdd, setShowAdd] = useState(false);

  const { data: companies, isLoading } = useCompanies();
  const byTicker = useMemo(() => new Map((companies ?? []).map((c) => [c.ticker, c])), [companies]);

  const rows = useMemo(
    () =>
      portfolio
        .map((h) => {
          const c = byTicker.get(h.ticker);
          if (!c) return null;
          const currentValue = c.price * h.shares;
          const costBasis = h.purchasePrice * h.shares;
          const pl = currentValue - costBasis;
          const plPercent = (pl / costBasis) * 100;
          return { holding: h, company: c, currentValue, costBasis, pl, plPercent };
        })
        .filter((r): r is NonNullable<typeof r> => r !== null),
    [portfolio, byTicker]
  );

  const totalValue = rows.reduce((s, r) => s + r.currentValue, 0);
  const totalCost = rows.reduce((s, r) => s + r.costBasis, 0);
  const totalPL = totalValue - totalCost;
  const totalPLPercent = totalCost ? (totalPL / totalCost) * 100 : 0;

  const sectorAlloc = useMemo(() => {
    const map = new Map<string, number>();
    rows.forEach((r) => map.set(r.company.sector, (map.get(r.company.sector) ?? 0) + r.currentValue));
    return [...map.entries()].map(([name, value]) => ({ name, value }));
  }, [rows]);

  const sorted = [...rows].sort((a, b) => b.plPercent - a.plPercent);
  const best = sorted[0];
  const worst = sorted[sorted.length - 1];

  return (
    <div className="p-4 md:p-6 max-w-[1400px] mx-auto space-y-6">
      <PageHeader
        title="Portfolio"
        subtitle="Manually track your PSX holdings, valued live in PKR. Not connected to any brokerage."
        actions={
          <button onClick={() => setShowAdd(true)} className="inline-flex items-center gap-1.5 bg-brand-500 hover:bg-brand-600 text-white text-sm font-medium px-3.5 py-2 rounded-lg transition-colors">
            <Plus className="h-4 w-4" /> Add Holding
          </button>
        }
      />

      {showAdd && <AddHoldingForm onClose={() => setShowAdd(false)} onAdd={addHolding} tickers={(companies ?? []).map((c) => c.ticker)} />}

      {isLoading && <LoadingState label="Loading live prices…" />}

      <div className="grid sm:grid-cols-2 lg:grid-cols-4 gap-3">
        <SummaryCard label="Total Value" value={formatCurrency(totalValue, "PKR", { compact: true })} />
        <SummaryCard label="Total Cost Basis" value={formatCurrency(totalCost, "PKR", { compact: true })} />
        <SummaryCard label="Total P/L" value={`${totalPL >= 0 ? "+" : ""}${formatCurrency(totalPL, "PKR", { compact: true })}`} tone={totalPL >= 0 ? "positive" : "negative"} sub={formatPercent(totalPLPercent)} />
        <div className="card p-4">
          <p className="text-xs text-text-secondary">Est. Dividend Income</p>
          <DataUnavailable compact title="Not available" className="mt-2" />
        </div>
      </div>

      <div className="grid lg:grid-cols-2 gap-4">
        <div className="card p-4">
          <h3 className="text-sm font-semibold text-text-primary mb-2">Sector Allocation</h3>
          <div className="h-40">
            <ResponsiveContainer width="100%" height="100%">
              <PieChart>
                <Pie data={sectorAlloc} dataKey="value" nameKey="name" cx="50%" cy="50%" innerRadius={35} outerRadius={60} paddingAngle={2}>
                  {sectorAlloc.map((_, i) => (
                    <Cell key={i} fill={PIE_COLORS[i % PIE_COLORS.length]} />
                  ))}
                </Pie>
                <Tooltip
                  contentStyle={{ background: "var(--bg-elevated)", border: "1px solid var(--border-default)", borderRadius: 8, fontSize: 12 }}
                  formatter={(v) => `Rs ${Number(v).toLocaleString(undefined, { maximumFractionDigits: 0 })}`}
                />
              </PieChart>
            </ResponsiveContainer>
          </div>
          <div className="space-y-1 mt-1">
            {sectorAlloc.map((d, i) => (
              <div key={d.name} className="flex items-center gap-2 text-xs">
                <span className="h-2 w-2 rounded-full shrink-0" style={{ backgroundColor: PIE_COLORS[i % PIE_COLORS.length] }} />
                <span className="text-text-secondary flex-1 truncate">{d.name}</span>
              </div>
            ))}
          </div>
        </div>

        {best && worst && (
          <div className="grid grid-rows-2 gap-4">
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
      </div>

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
                <td className="text-right px-3 text-text-secondary">{formatCurrency(r.holding.purchasePrice, "PKR")}</td>
                <td className="text-right px-3 text-text-secondary">{formatCurrency(r.company.price, "PKR")}</td>
                <td className="text-right px-3 text-text-primary font-medium">{formatCurrency(r.currentValue, "PKR")}</td>
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

function AddHoldingForm({ onClose, onAdd, tickers }: { onClose: () => void; onAdd: (h: { ticker: string; shares: number; purchasePrice: number; purchaseDate: string; currency: "PKR" }) => void; tickers: string[] }) {
  const [ticker, setTicker] = useState("");
  const [shares, setShares] = useState("");
  const [price, setPrice] = useState("");
  const [date, setDate] = useState(new Date().toISOString().slice(0, 10));

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    const match = tickers.find((t) => t === ticker.toUpperCase());
    if (!match || !shares || !price) return;
    onAdd({ ticker: match, shares: Number(shares), purchasePrice: Number(price), purchaseDate: date, currency: "PKR" });
    onClose();
  }

  return (
    <form onSubmit={handleSubmit} className="card p-4 grid sm:grid-cols-2 lg:grid-cols-4 gap-3 items-end">
      <div>
        <label className="text-xs text-text-secondary mb-1.5 block">Ticker</label>
        <input value={ticker} onChange={(e) => setTicker(e.target.value)} placeholder="e.g. OGDC" required list="psx-tickers" className="w-full bg-bg-elevated border border-border-subtle rounded-lg px-3 py-2 text-sm outline-none focus:border-brand-500" />
        <datalist id="psx-tickers">{tickers.map((t) => <option key={t} value={t} />)}</datalist>
      </div>
      <div>
        <label className="text-xs text-text-secondary mb-1.5 block">Shares</label>
        <input value={shares} onChange={(e) => setShares(e.target.value)} type="number" min="0" step="any" required className="w-full bg-bg-elevated border border-border-subtle rounded-lg px-3 py-2 text-sm outline-none focus:border-brand-500" />
      </div>
      <div>
        <label className="text-xs text-text-secondary mb-1.5 block">Purchase Price (PKR)</label>
        <input value={price} onChange={(e) => setPrice(e.target.value)} type="number" min="0" step="any" required className="w-full bg-bg-elevated border border-border-subtle rounded-lg px-3 py-2 text-sm outline-none focus:border-brand-500" />
      </div>
      <div className="flex items-center gap-2">
        <input value={date} onChange={(e) => setDate(e.target.value)} type="date" required className="flex-1 bg-bg-elevated border border-border-subtle rounded-lg px-3 py-2 text-sm outline-none focus:border-brand-500" />
        <button type="submit" className="bg-brand-500 hover:bg-brand-600 text-white text-sm font-medium px-3.5 py-2 rounded-lg">Add</button>
        <button type="button" onClick={onClose} className="text-text-secondary p-2"><X className="h-4 w-4" /></button>
      </div>
    </form>
  );
}
