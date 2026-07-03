import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { Bell, BellOff, Plus, Trash2, X } from "lucide-react";
import { useAppStore } from "@/store/useAppStore";
import { PageHeader } from "@/components/ui/PageHeader";
import { CompanyLogo } from "@/components/ui/CompanyLogo";
import { Badge } from "@/components/ui/Badge";
import { useCompanies } from "@/hooks/useMarketData";
import { formatCurrency } from "@/lib/format";

const NOTIFICATION_TYPES = [
  "Price reaches target",
  "Price drops below target",
  "Price rises above target",
  "Significant intraday move (> 5%)",
  "New 52-week high or low",
];

export function AlertsPage() {
  const alerts = useAppStore((s) => s.alerts);
  const addAlert = useAppStore((s) => s.addAlert);
  const removeAlert = useAppStore((s) => s.removeAlert);
  const toggleAlert = useAppStore((s) => s.toggleAlert);
  const [showAdd, setShowAdd] = useState(false);
  const [ticker, setTicker] = useState("");
  const [type, setType] = useState<"above" | "below">("above");
  const [price, setPrice] = useState("");
  const [notificationToggles, setNotificationToggles] = useState<Record<string, boolean>>(
    Object.fromEntries(NOTIFICATION_TYPES.map((t, i) => [t, i < 3]))
  );

  const { data: companies } = useCompanies();
  const byTicker = useMemo(() => new Map((companies ?? []).map((c) => [c.ticker, c])), [companies]);

  function handleAdd(e: React.FormEvent) {
    e.preventDefault();
    const match = byTicker.get(ticker.toUpperCase());
    if (!match || !price) return;
    addAlert({ ticker: match.ticker, type, targetPrice: Number(price), active: true });
    setShowAdd(false);
    setTicker("");
    setPrice("");
  }

  return (
    <div className="p-4 md:p-6 max-w-3xl mx-auto space-y-6">
      <PageHeader
        title="Alerts"
        subtitle="Price alerts on live PSX prices and notification preferences."
        actions={
          <button onClick={() => setShowAdd(true)} className="inline-flex items-center gap-1.5 bg-brand-500 hover:bg-brand-600 text-white text-sm font-medium px-3.5 py-2 rounded-lg transition-colors">
            <Plus className="h-4 w-4" /> New Price Alert
          </button>
        }
      />

      {showAdd && (
        <form onSubmit={handleAdd} className="card p-4 grid sm:grid-cols-4 gap-3 items-end">
          <div>
            <label className="text-xs text-text-secondary mb-1.5 block">Ticker</label>
            <input value={ticker} onChange={(e) => setTicker(e.target.value)} placeholder="e.g. HBL" required list="alert-tickers" className="w-full bg-bg-elevated border border-border-subtle rounded-lg px-3 py-2 text-sm outline-none focus:border-brand-500" />
            <datalist id="alert-tickers">{[...byTicker.keys()].map((t) => <option key={t} value={t} />)}</datalist>
          </div>
          <div>
            <label className="text-xs text-text-secondary mb-1.5 block">Condition</label>
            <select value={type} onChange={(e) => setType(e.target.value as "above" | "below")} className="w-full bg-bg-elevated border border-border-subtle rounded-lg px-3 py-2 text-sm outline-none">
              <option value="above">Price rises above</option>
              <option value="below">Price drops below</option>
            </select>
          </div>
          <div>
            <label className="text-xs text-text-secondary mb-1.5 block">Target Price (PKR)</label>
            <input value={price} onChange={(e) => setPrice(e.target.value)} type="number" min="0" step="any" required className="w-full bg-bg-elevated border border-border-subtle rounded-lg px-3 py-2 text-sm outline-none focus:border-brand-500" />
          </div>
          <div className="flex items-center gap-2">
            <button type="submit" className="flex-1 bg-brand-500 hover:bg-brand-600 text-white text-sm font-medium px-3.5 py-2 rounded-lg">Create</button>
            <button type="button" onClick={() => setShowAdd(false)} className="text-text-secondary p-2"><X className="h-4 w-4" /></button>
          </div>
        </form>
      )}

      <section>
        <h2 className="text-sm font-semibold text-text-primary mb-3">Active Price Alerts</h2>
        <div className="space-y-2.5">
          {alerts.map((a) => {
            const c = byTicker.get(a.ticker);
            return (
              <div key={a.id} className="card p-4 flex items-center gap-3">
                {c && <CompanyLogo initials={c.logoInitials} color={c.logoColor} size={34} />}
                <div className="flex-1 min-w-0">
                  <Link to={`/company/${a.ticker}`} className="text-sm font-medium text-text-primary hover:text-brand-400">{c?.name ?? a.ticker}</Link>
                  <p className="text-xs text-text-secondary">
                    Alert when price {a.type === "above" ? "rises above" : "drops below"} {formatCurrency(a.targetPrice, "PKR")}
                    {c ? ` (current: ${formatCurrency(c.price, "PKR")})` : ""}
                  </p>
                </div>
                <Badge tone={a.active ? "positive" : "neutral"}>{a.active ? "Active" : "Paused"}</Badge>
                <button onClick={() => toggleAlert(a.id)} className="text-text-secondary hover:text-text-primary p-1.5">
                  {a.active ? <Bell className="h-4 w-4" /> : <BellOff className="h-4 w-4" />}
                </button>
                <button onClick={() => removeAlert(a.id)} className="text-text-secondary hover:text-negative p-1.5">
                  <Trash2 className="h-4 w-4" />
                </button>
              </div>
            );
          })}
          {alerts.length === 0 && <p className="text-sm text-text-secondary">No alerts set up yet.</p>}
        </div>
      </section>

      <section className="card p-4">
        <h2 className="text-sm font-semibold text-text-primary mb-3">Notification Preferences</h2>
        <div className="space-y-2.5">
          {NOTIFICATION_TYPES.map((t) => (
            <label key={t} className="flex items-center justify-between text-sm cursor-pointer">
              <span className="text-text-secondary">{t}</span>
              <input
                type="checkbox"
                checked={notificationToggles[t]}
                onChange={() => setNotificationToggles((s) => ({ ...s, [t]: !s[t] }))}
                className="accent-brand-500 h-4 w-4"
              />
            </label>
          ))}
        </div>
        <p className="text-xs text-text-secondary mt-3">
          Alerts are evaluated against live PSX prices while the app is open. Background/push delivery requires a server component not included in this build.
        </p>
      </section>
    </div>
  );
}
