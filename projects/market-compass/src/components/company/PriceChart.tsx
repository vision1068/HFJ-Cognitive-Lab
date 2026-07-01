import { useMemo, useState } from "react";
import { Area, AreaChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from "recharts";
import type { PricePoint } from "@/types";
import { formatCurrency } from "@/lib/format";
import type { Currency } from "@/types";

const RANGES = [
  { label: "1D", days: 1 },
  { label: "5D", days: 5 },
  { label: "1M", days: 30 },
  { label: "3M", days: 90 },
  { label: "6M", days: 182 },
  { label: "1Y", days: 365 },
  { label: "5Y", days: 365 * 3 },
  { label: "Max", days: Infinity },
] as const;

export function PriceChart({ history, currency, positive }: { history: PricePoint[]; currency: Currency; positive: boolean }) {
  const [range, setRange] = useState<(typeof RANGES)[number]["label"]>("1Y");

  const data = useMemo(() => {
    const r = RANGES.find((r) => r.label === range)!;
    const slice = r.days === Infinity ? history : history.slice(-r.days);
    return slice.map((p) => ({ date: p.date, close: p.close, volume: p.volume }));
  }, [history, range]);

  const color = positive ? "var(--color-positive)" : "var(--color-negative)";

  return (
    <div>
      <div className="flex items-center gap-1 mb-3">
        {RANGES.map((r) => (
          <button
            key={r.label}
            onClick={() => setRange(r.label)}
            className={`rounded-md px-2.5 py-1 text-xs font-medium transition-colors ${
              range === r.label ? "bg-brand-500 text-white" : "text-text-secondary hover:bg-bg-hover"
            }`}
          >
            {r.label}
          </button>
        ))}
      </div>
      <div className="h-72">
        <ResponsiveContainer width="100%" height="100%">
          <AreaChart data={data} margin={{ top: 5, right: 5, bottom: 0, left: 0 }}>
            <defs>
              <linearGradient id="priceFill" x1="0" y1="0" x2="0" y2="1">
                <stop offset="5%" stopColor={color} stopOpacity={0.3} />
                <stop offset="95%" stopColor={color} stopOpacity={0} />
              </linearGradient>
            </defs>
            <CartesianGrid strokeDasharray="3 3" stroke="var(--border-subtle)" vertical={false} />
            <XAxis dataKey="date" tick={{ fontSize: 11, fill: "var(--text-secondary)" }} minTickGap={40} axisLine={false} tickLine={false} />
            <YAxis domain={["auto", "auto"]} tick={{ fontSize: 11, fill: "var(--text-secondary)" }} axisLine={false} tickLine={false} width={56} />
            <Tooltip
              contentStyle={{ background: "var(--bg-elevated)", border: "1px solid var(--border-default)", borderRadius: 8, fontSize: 12 }}
              labelStyle={{ color: "var(--text-secondary)" }}
              formatter={(value) => [formatCurrency(Number(value), currency), "Close"]}
            />
            <Area type="monotone" dataKey="close" stroke={color} strokeWidth={2} fill="url(#priceFill)" isAnimationActive={false} />
          </AreaChart>
        </ResponsiveContainer>
      </div>
    </div>
  );
}
