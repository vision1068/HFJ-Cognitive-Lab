import { Moon, Sun } from "lucide-react";
import { useAppStore } from "@/store/useAppStore";
import { PageHeader } from "@/components/ui/PageHeader";
import { Disclaimer } from "@/components/ui/Disclaimer";
import type { Currency } from "@/types";

const CURRENCIES: Currency[] = ["USD", "PKR", "GBP", "EUR", "JPY"];

export function SettingsPage() {
  const theme = useAppStore((s) => s.theme);
  const toggleTheme = useAppStore((s) => s.toggleTheme);
  const baseCurrency = useAppStore((s) => s.baseCurrency);
  const setBaseCurrency = useAppStore((s) => s.setBaseCurrency);

  return (
    <div className="p-4 md:p-6 max-w-2xl mx-auto space-y-6">
      <PageHeader title="Settings" subtitle="Manage your preferences and account." />

      <section className="card p-5">
        <h2 className="text-sm font-semibold text-text-primary mb-4">Appearance</h2>
        <div className="flex items-center justify-between">
          <div>
            <p className="text-sm text-text-primary">Theme</p>
            <p className="text-xs text-text-secondary">Dark mode is recommended for extended research sessions.</p>
          </div>
          <button
            onClick={toggleTheme}
            className="inline-flex items-center gap-2 rounded-lg border border-border-default px-3.5 py-2 text-sm text-text-primary hover:bg-bg-hover"
          >
            {theme === "dark" ? <Moon className="h-4 w-4" /> : <Sun className="h-4 w-4" />}
            {theme === "dark" ? "Dark" : "Light"}
          </button>
        </div>
      </section>

      <section className="card p-5">
        <h2 className="text-sm font-semibold text-text-primary mb-4">Portfolio Base Currency</h2>
        <p className="text-xs text-text-secondary mb-3">Convert your portfolio valuation into a single base currency.</p>
        <div className="flex flex-wrap gap-1.5">
          {CURRENCIES.map((c) => (
            <button
              key={c}
              onClick={() => setBaseCurrency(c)}
              className={`rounded-full px-3.5 py-1.5 text-xs font-medium transition-colors ${
                baseCurrency === c ? "bg-brand-500 text-white" : "bg-bg-elevated text-text-secondary hover:bg-bg-hover"
              }`}
            >
              {c}
            </button>
          ))}
        </div>
      </section>

      <section className="card p-5">
        <h2 className="text-sm font-semibold text-text-primary mb-4">Account</h2>
        <div className="space-y-3">
          <div>
            <label className="text-xs text-text-secondary mb-1.5 block">Display Name</label>
            <input defaultValue="Jane Investor" className="w-full bg-bg-elevated border border-border-subtle rounded-lg px-3 py-2 text-sm outline-none focus:border-brand-500" />
          </div>
          <div>
            <label className="text-xs text-text-secondary mb-1.5 block">Email</label>
            <input defaultValue="jane@example.com" className="w-full bg-bg-elevated border border-border-subtle rounded-lg px-3 py-2 text-sm outline-none focus:border-brand-500" />
          </div>
        </div>
      </section>

      <section className="card p-5">
        <h2 className="text-sm font-semibold text-text-primary mb-4">Data Sources</h2>
        <p className="text-xs text-text-secondary leading-relaxed">
          Market Compass currently displays illustrative mock data. The application is structured to connect to live providers
          (Alpha Vantage, Finnhub, Financial Modeling Prep, Polygon.io, Twelve Data, PSX data feeds, and news/economic calendar APIs)
          through a modular adapter layer once API credentials are configured.
        </p>
      </section>

      <Disclaimer />
    </div>
  );
}
