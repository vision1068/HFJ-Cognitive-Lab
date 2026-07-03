import { useState } from "react";
import { Area, AreaChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis, ReferenceLine } from "recharts";
import { PageHeader } from "@/components/ui/PageHeader";
import { DataStatusBadge } from "@/components/market/DataStatusBadge";
import { MarketDataError } from "@/components/market/MarketDataError";
import { useMarketAnalysis } from "@/hooks/useMarketData";
import type { Timeframe } from "@/lib/fmpClient";
import type { SignalStrength } from "@/lib/indicators";
import { clsx } from "clsx";

const TIMEFRAMES: { label: string; value: Timeframe }[] = [
  { label: "1m", value: "1min" },
  { label: "5m", value: "5min" },
  { label: "15m", value: "15min" },
  { label: "30m", value: "30min" },
  { label: "1H", value: "1hour" },
  { label: "4H", value: "4hour" },
  { label: "1D", value: "1day" },
];

const SIGNAL_STYLES: Record<SignalStrength, string> = {
  "Strong Buy": "bg-positive-bg text-positive border-positive/30",
  Buy: "bg-positive-bg text-positive border-positive/20",
  Neutral: "bg-neutral-bg text-neutral border-neutral/20",
  Sell: "bg-negative-bg text-negative border-negative/20",
  "Strong Sell": "bg-negative-bg text-negative border-negative/30",
};

export function LiveAnalysisPage() {
  const [symbolInput, setSymbolInput] = useState("AAPL");
  const [symbol, setSymbol] = useState("AAPL");
  const [timeframe, setTimeframe] = useState<Timeframe>("1day");

  const { status, errorMessage, quote, candles, analysis, lastUpdated, refetch } = useMarketAnalysis(symbol, timeframe);

  const chartData =
    candles?.map((c, i) => ({
      date: c.date,
      close: c.close,
      sma20: analysis?.indicators.sma20[i] ?? undefined,
      sma50: analysis?.indicators.sma50[i] ?? undefined,
    })) ?? [];

  return (
    <div>
      <PageHeader
        title="Live Market Analysis"
        subtitle="Real-time prices, indicators, and signals — calculated only from live Financial Modeling Prep data."
        actions={<DataStatusBadge status={status} lastUpdated={lastUpdated} />}
      />

      <form
        onSubmit={(e) => {
          e.preventDefault();
          setSymbol(symbolInput.trim().toUpperCase());
        }}
        className="flex flex-wrap items-center gap-3 mb-6"
      >
        <input
          value={symbolInput}
          onChange={(e) => setSymbolInput(e.target.value)}
          placeholder="Symbol (e.g. AAPL, EURUSD, BTCUSD)"
          className="rounded-lg border border-border-default bg-bg-surface px-3 py-2 text-sm text-text-primary placeholder:text-text-secondary focus:outline-none focus:ring-2 focus:ring-brand-500"
        />
        <button type="submit" className="rounded-lg bg-brand-500 hover:bg-brand-600 px-4 py-2 text-sm font-medium text-white transition-colors">
          Load
        </button>

        <div className="flex items-center gap-1 ml-auto">
          {TIMEFRAMES.map((tf) => (
            <button
              key={tf.value}
              type="button"
              onClick={() => setTimeframe(tf.value)}
              className={clsx(
                "rounded-md px-2.5 py-1.5 text-xs font-medium transition-colors",
                timeframe === tf.value ? "bg-brand-500 text-white" : "text-text-secondary hover:bg-bg-hover"
              )}
            >
              {tf.label}
            </button>
          ))}
        </div>
      </form>

      {status === "loading" && (
        <div className="rounded-xl border border-border-subtle bg-bg-surface p-8 text-center text-sm text-text-secondary">
          Loading market data…
        </div>
      )}

      {(status === "no-key" || status === "no-data" || status === "error") && (
        <MarketDataError status={status} message={errorMessage} onRetry={refetch} />
      )}

      {status === "connected" && quote && analysis && (
        <div className="space-y-6">
          {/* Price header */}
          <div className="rounded-xl border border-border-subtle bg-bg-surface p-5 flex flex-wrap items-baseline gap-x-6 gap-y-2">
            <div>
              <div className="text-xs text-text-secondary">{quote.symbol}</div>
              <div className="text-3xl font-bold text-text-primary tabular-nums">{quote.price.toFixed(2)}</div>
            </div>
            <div className={quote.change >= 0 ? "text-positive" : "text-negative"}>
              {quote.change >= 0 ? "+" : ""}
              {quote.change.toFixed(2)} ({quote.changePercentage.toFixed(2)}%)
            </div>
            <div className="text-xs text-text-secondary ml-auto">
              Day range: {quote.dayLow.toFixed(2)} – {quote.dayHigh.toFixed(2)} · Vol: {quote.volume.toLocaleString()}
            </div>
          </div>

          {/* Chart with SMA overlays and support/resistance lines */}
          <div className="rounded-xl border border-border-subtle bg-bg-surface p-5">
            <h3 className="text-sm font-semibold text-text-primary mb-4">Price · SMA20 · SMA50 · Support/Resistance</h3>
            <div className="h-80">
              <ResponsiveContainer width="100%" height="100%">
                <AreaChart data={chartData} margin={{ top: 5, right: 5, bottom: 0, left: 0 }}>
                  <defs>
                    <linearGradient id="closeFill" x1="0" y1="0" x2="0" y2="1">
                      <stop offset="5%" stopColor="var(--color-brand-500)" stopOpacity={0.25} />
                      <stop offset="95%" stopColor="var(--color-brand-500)" stopOpacity={0} />
                    </linearGradient>
                  </defs>
                  <CartesianGrid strokeDasharray="3 3" stroke="var(--border-subtle)" vertical={false} />
                  <XAxis dataKey="date" tick={{ fontSize: 10, fill: "var(--text-secondary)" }} minTickGap={50} axisLine={false} tickLine={false} />
                  <YAxis domain={["auto", "auto"]} tick={{ fontSize: 11, fill: "var(--text-secondary)" }} axisLine={false} tickLine={false} width={60} />
                  <Tooltip
                    contentStyle={{ background: "var(--bg-elevated)", border: "1px solid var(--border-default)", borderRadius: 8, fontSize: 12 }}
                    labelStyle={{ color: "var(--text-secondary)" }}
                  />
                  {analysis.supportResistance.support.map((lvl) => (
                    <ReferenceLine key={`s-${lvl}`} y={lvl} stroke="var(--color-positive)" strokeDasharray="4 4" strokeOpacity={0.5} />
                  ))}
                  {analysis.supportResistance.resistance.map((lvl) => (
                    <ReferenceLine key={`r-${lvl}`} y={lvl} stroke="var(--color-negative)" strokeDasharray="4 4" strokeOpacity={0.5} />
                  ))}
                  <Area type="monotone" dataKey="close" stroke="var(--color-brand-500)" strokeWidth={2} fill="url(#closeFill)" isAnimationActive={false} />
                  <Area type="monotone" dataKey="sma20" stroke="var(--color-accent-amber)" strokeWidth={1.5} fill="none" isAnimationActive={false} dot={false} />
                  <Area type="monotone" dataKey="sma50" stroke="var(--color-accent-violet)" strokeWidth={1.5} fill="none" isAnimationActive={false} dot={false} />
                </AreaChart>
              </ResponsiveContainer>
            </div>
          </div>

          {/* Signal panel */}
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
            <div className="lg:col-span-1 rounded-xl border border-border-subtle bg-bg-surface p-5">
              <h3 className="text-sm font-semibold text-text-primary mb-3">Signal</h3>
              <div className={clsx("rounded-lg border px-4 py-3 text-center mb-3", SIGNAL_STYLES[analysis.signal])}>
                <div className="text-lg font-bold">{analysis.signal}</div>
                <div className="text-xs mt-1">Confidence Score: {analysis.confidenceScore}/100</div>
              </div>
              <div className="text-xs text-text-secondary mb-2">Market structure: <span className="text-text-primary capitalize">{analysis.marketStructure}</span></div>
              <ul className="space-y-1.5 text-xs text-text-secondary list-disc list-inside">
                {analysis.signalReasons.map((r, i) => (
                  <li key={i}>{r}</li>
                ))}
              </ul>
            </div>

            <div className="lg:col-span-2 rounded-xl border border-border-subtle bg-bg-surface p-5">
              <h3 className="text-sm font-semibold text-text-primary mb-3">Indicators (latest value)</h3>
              <div className="grid grid-cols-2 sm:grid-cols-3 gap-4 text-sm">
                <IndicatorTile label="RSI(14)" value={last(analysis.indicators.rsi14)} />
                <IndicatorTile label="MACD" value={last(analysis.indicators.macd)} />
                <IndicatorTile label="MACD Signal" value={last(analysis.indicators.macdSignal)} />
                <IndicatorTile label="EMA(12)" value={last(analysis.indicators.ema12)} />
                <IndicatorTile label="EMA(26)" value={last(analysis.indicators.ema26)} />
                <IndicatorTile label="ATR(14)" value={last(analysis.indicators.atr14)} />
                <IndicatorTile label="Bollinger Upper" value={last(analysis.indicators.bollingerUpper)} />
                <IndicatorTile label="Bollinger Mid" value={last(analysis.indicators.bollingerMiddle)} />
                <IndicatorTile label="Bollinger Lower" value={last(analysis.indicators.bollingerLower)} />
              </div>

              <h4 className="text-xs font-semibold text-text-secondary mt-5 mb-2">Liquidity Sweeps (last 5)</h4>
              {analysis.liquiditySweeps.length === 0 ? (
                <p className="text-xs text-text-secondary">No liquidity sweeps detected in the loaded range.</p>
              ) : (
                <ul className="space-y-1 text-xs text-text-secondary">
                  {analysis.liquiditySweeps.map((s, i) => (
                    <li key={i}>
                      {s.date} — <span className="capitalize">{s.type.replace("-", " ")}</span> sweep of {s.sweptLevel.toFixed(2)}
                    </li>
                  ))}
                </ul>
              )}
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

function last<T>(arr: T[]): T | undefined {
  return arr.length ? arr[arr.length - 1] : undefined;
}

function IndicatorTile({ label, value }: { label: string; value?: number | null }) {
  return (
    <div>
      <div className="text-xs text-text-secondary">{label}</div>
      <div className="font-mono text-text-primary tabular-nums">{value != null ? value.toFixed(2) : "—"}</div>
    </div>
  );
}
