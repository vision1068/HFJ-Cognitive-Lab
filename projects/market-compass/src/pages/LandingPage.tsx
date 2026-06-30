import { Link } from "react-router-dom";
import { Compass, ArrowRight, LineChart, ShieldCheck, Search, Globe2, Gauge, Bell } from "lucide-react";
import { Disclaimer } from "@/components/ui/Disclaimer";
import { companies } from "@/data/companies";
import { CompanyLogo } from "@/components/ui/CompanyLogo";
import { PriceChange } from "@/components/ui/PriceChange";
import { SignalBadge } from "@/components/ui/Badge";
import { formatCurrency } from "@/lib/format";

const FEATURES = [
  { icon: Gauge, title: "Transparent Signal Engine", desc: "Every score is explainable — see exactly why a company earned its fundamentals, growth, valuation, technical, sentiment, and risk scores." },
  { icon: Globe2, title: "Pakistan & Global Markets", desc: "Research PSX-listed companies alongside NYSE, NASDAQ, LSE, TSE, and European markets in one unified workspace." },
  { icon: LineChart, title: "Deep Financial Analysis", desc: "Revenue, profit, cash flow, valuation, and growth potential — visualized with professional-grade charts." },
  { icon: Search, title: "Powerful Screener", desc: "Filter by sector, market cap, growth, valuation, risk, and technical signals to discover new opportunities." },
  { icon: Bell, title: "Smart Alerts", desc: "Get notified on price targets, earnings, dividends, and meaningful changes in a company's signal." },
  { icon: ShieldCheck, title: "Risk-Aware by Design", desc: "Every company page surfaces risk factors clearly — debt, valuation, volatility, and sentiment — never just the upside." },
];

export function LandingPage() {
  const featured = companies.slice(0, 6);

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
          <Link to="/login" className="text-sm font-medium text-text-secondary hover:text-text-primary">
            Log in
          </Link>
          <Link to="/signup" className="text-sm font-medium bg-brand-500 hover:bg-brand-600 text-white px-4 py-2 rounded-lg transition-colors">
            Get Started
          </Link>
        </div>
      </header>

      <section className="max-w-5xl mx-auto px-6 pt-16 pb-20 text-center">
        <span className="inline-flex items-center gap-1.5 rounded-full border border-border-default bg-bg-surface px-3 py-1 text-xs font-medium text-text-secondary mb-6">
          <span className="h-1.5 w-1.5 rounded-full bg-positive" /> Now covering PSX + 6 global markets
        </span>
        <h1 className="text-4xl md:text-6xl font-bold tracking-tight text-text-primary leading-[1.1]">
          Navigate every market <br /> with <span className="text-gradient">data-driven clarity.</span>
        </h1>
        <p className="mt-6 text-lg text-text-secondary max-w-2xl mx-auto">
          Market Compass blends fundamentals, valuation, technicals, news sentiment, and risk into one transparent
          score — so you can research smarter and decide with confidence.
        </p>
        <div className="mt-8 flex flex-wrap items-center justify-center gap-3">
          <Link
            to="/signup"
            className="inline-flex items-center gap-2 bg-brand-500 hover:bg-brand-600 text-white font-medium px-6 py-3 rounded-xl transition-colors"
          >
            Start Researching Free <ArrowRight className="h-4 w-4" />
          </Link>
          <Link
            to="/dashboard"
            className="inline-flex items-center gap-2 border border-border-default hover:bg-bg-hover text-text-primary font-medium px-6 py-3 rounded-xl transition-colors"
          >
            Explore Demo Dashboard
          </Link>
        </div>
        <div className="mt-12 max-w-lg mx-auto">
          <Disclaimer compact />
        </div>
      </section>

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
              <div className="flex items-center justify-between mt-1">
                <PriceChange percent={c.changePercent} />
                <SignalBadge signal={c.scoreBreakdown.signal} className="!px-2 !py-0.5 !text-[10px]" />
              </div>
            </Link>
          ))}
        </div>
      </section>

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
            © {new Date().getFullYear()} Market Compass. All market data shown is illustrative / delayed mock data for demonstration purposes.
          </p>
        </div>
      </footer>
    </div>
  );
}
