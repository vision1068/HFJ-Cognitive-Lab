import { Moon, Sun } from "lucide-react";
import { useAppStore } from "@/store/useAppStore";
import { PageHeader } from "@/components/ui/PageHeader";
import { Disclaimer } from "@/components/ui/Disclaimer";

export function SettingsPage() {
  const theme = useAppStore((s) => s.theme);
  const toggleTheme = useAppStore((s) => s.toggleTheme);

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
          Market Compass shows only real, live data for the Pakistan Stock Exchange (PSX). Prices, price history, indices
          (KSE-100, KMI-30), and fundamentals (P/E, EPS, market cap) come from the PSX Data Portal (dps.psx.com.pk); the USD/PKR
          rate comes from Yahoo Finance. Data auto-refreshes every 60 seconds. Features that no free data source can support
          (financial statements, AI scores, news, economic calendar) are clearly marked "data not available" rather than filled
          with estimated numbers.
        </p>
      </section>

      <Disclaimer />
    </div>
  );
}
