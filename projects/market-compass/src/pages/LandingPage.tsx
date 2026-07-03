import { Link } from "react-router-dom";
import { Compass, ArrowRight, LineChart, ShieldCheck, Search, Building2, Gauge, Bell } from "lucide-react";
import { Disclaimer } from "@/components/ui/Disclaimer";
import { CompanyLogo } from "@/components/ui/CompanyLogo";
import { PriceChange } from "@/components/ui/PriceChange";
import { useCompanies } from "@/hooks/useMarketData";
import { formatCurrency } from "@/lib/format";

const FEATURES = [
  { icon: Gauge, title: "Live PSX Prices", desc: "Real end-of-day and last-traded prices for major PSX companies, sourced directly from the Pakistan Stock Exchange Data Portal." },
  { icon: Building2, title: "Pakistan-Focused", desc: "Purpose-built for the Pakistan Stock Exchange — KSE-100, KMI-30, and the country's most-traded blue chips." },
  { icon: LineChart, title: "Real Fundamentals", desc: "P/E ratio, market capitalisation, EPS, and multi-year price history — as reported by PSX, not estimated." },
  { icon: Search, title: "Screener & Search", desc: "Filter PSX companies by live valuation, sector, and daily performance to find what you're looking for." },
  { icon: Bell, title: "Price Alerts", desc: "Set alerts on live PSX prices and track your holdings in a manual, PKR-denominated portfolio." },
  { icon: ShieldCheck, title: "Honest by Design", desc: "Where free data can't provide something, the app clearly says \"data not available\" instead of showing made-up numbers." },
];

export function LandingPage() {
  const { data: companies } = useCompanies();
  const featured = (companies ?? []).slice(0, 6);

  return (
    <div className="min-h-screen bg-bg-base">
      <header className="flex items-center justify-between px-6 md:px-10 h-20 max-w-7xl mx-auto">
        <div className="flex items-center gap-2">
          <div className="flex items-center justify-center h-9 w-9 rounded-xl bg-gradient-to-br from-brand-500 to-accent-violet">
            <Compass className="h-5 w-5 text-white" />
          </div>
          <span className="font-bold text-lg text-text-primary tracking-tight">Market Compass</span>
        </div>
        <div className="flex items-center gap-3">
          <Link to="/login" className="text-sm font-medium text-text-secondary hover:text-text-primary">Log in</Link>
          <Link to="/signup" className="text-sm font-medium bg-brand-500 hover:bg-brand-600 text-white px-4 py-2 rounded-lg transition-colors">Get Started</Link>
        </div>
      </header>

      <section className="max-w-5xl mx-auto px-6 pt-16 pb-20 text-center">
        <span className="inline-flex items-center gap-1.5 rounded-full border border-border-default bg-bg-surface px-3 py-1 text-xs font-medium text-text-secondary mb-6">
          <span className="h-1.5 w-1.5 rounded-full bg-positive" /> Live Pakistan Stock Exchange data
        </span>
        <h1 className="text-4xl md:text-6xl font-bold tracking-tight text-text-primary leading-[1.1]">
          Research the PSX <br /> with <span className="text-gradient">real, live data.</span>
        </h1>
        <p className="mt-6 text-lg text-text-secondary max-w-2xl mx-auto">
          Market Compass tracks the Pakistan Stock Exchange with real prices, indices, and fundamentals — sourced live from the
          PSX Data Portal. No mock numbers.
        </p>
        <div className="mt-8 flex flex-wrap items-center justify-center gap-3">
          <Link to="/signup" className="inline-flex items-center gap-2 bg-brand-500 hover:bg-brand-600 text-white font-medium px-6 py-3 rounded-xl transition-colors">
            Start Researching Free <ArrowRight className="h-4 w-4" />
          </Link>
          <Link to="/dashboard" className="inline-flex items-center gap-2 border border-border-default hover:bg-bg-hover text-text-primary font-medium px-6 py-3 rounded-xl transition-colors">
            Explore Dashboard
          </Link>
        </div>
        <div className="mt-12 max-w-lg mx-auto">
          <Disclaimer compact />
        </div>
      </section>

      {featured.length > 0 && (
        <section className="max-w-6xl mx-auto px-6 pb-20">
          <div className="grid grid-cols-2 md:grid-cols-3 gap-3">
            {featured.map((c) => (
              <Link key={c.ticker} to="/login" className="card card-hover p-4 text-left animate-fade-in">
                <div className="flex items-center gap-2.5 mb-3">
                  <CompanyLogo initials={c.logoInitials} color={c.logoColor} size={32} />
                  <div className="min-w-0">
                    <p className="text-sm font-medium text-text-primary truncate">{c.ticker}</p>
                    <p className="text-xs text-text-secondary truncate">{c.exchange}</p>
                  </div>
                </div>
                <p className="text-lg font-semibold text-text-primary">{formatCurrency(c.price, c.currency)}</p>
                <PriceChange percent={c.changePercent} />
              </Link>
            ))}
          </div>
        </section>
      )}

      <section className="max-w-6xl mx-auto px-6 pb-24">
        <div className="grid md:grid-cols-3 gap-5">
          {FEATURES.map((f) => (
            <div key={f.title} className="card p-6">
              <div className="flex items-center justify-center h-10 w-10 rounded-xl bg-brand-500/15 text-brand-400 mb-4">
                <f.icon className="h-5 w-5" />
              </div>
              <h3 className="font-semibold text-text-primary mb-1.5">{f.title}</h3>
              <p className="text-sm text-text-secondary leading-relaxed">{f.desc}</p>
            </div>
          ))}
        </div>
      </section>

      <footer className="border-t border-border-subtle py-10 px-6">
        <div className="max-w-6xl mx-auto">
          <Disclaimer />
          <p className="text-xs text-text-secondary mt-6 text-center">
            © {new Date().getFullYear()} Market Compass. Live data from the PSX Data Portal and Yahoo Finance (USD/PKR); may be delayed.
          </p>
        </div>
      </footer>
    </div>
  );
}
