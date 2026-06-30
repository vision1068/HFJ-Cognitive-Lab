import { useState } from "react";
import { Bar, BarChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from "recharts";
import type { FinancialPeriod } from "@/types";
import { formatCompactNumber } from "@/lib/format";

type Metric = "revenue" | "netProfit" | "freeCashFlow" | "debt";

const METRICS: { key: Metric; label: string }[] = [
  { key: "revenue", label: "Revenue" },
  { key: "netProfit", label: "Net Profit" },
  { key: "freeCashFlow", label: "Free Cash Flow" },
  { key: "debt", label: "Debt" },
];

export function FinancialsChart({ annual, quarterly }: { annual: FinancialPeriod[]; quarterly: FinancialPeriod[] }) {
  const [metric, setMetric] = useState<Metric>("revenue");
  const [period, setPeriod] = useState<"annual" | "quarterly">("annual");

  const data = period === "annual" ? annual : quarterly;

  return (
    <div>
      <div className="flex flex-wrap items-center justify-between gap-2 mb-3">
        <div className="flex items-center gap-1">
          {METRICS.map((m) => (
            <button
              key={m.key}
              onClick={() => setMetric(m.key)}
              className={`rounded-md px-2.5 py-1 text-xs font-medium transition-colors ${
                metric === m.key ? "bg-brand-500 text-white" : "text-text-secondary hover:bg-bg-hover"
              }`}
            >
              {m.label}
            </button>
          ))}
        </div>
        <div className="flex items-center gap-1">
          <button
            onClick={() => setPeriod("annual")}
            className={`rounded-md px-2.5 py-1 text-xs font-medium transition-colors ${
              period === "annual" ? "bg-bg-hover text-text-primary" : "text-text-secondary"
            }`}
          >
            Annual
          </button>
          <button
            onClick={() => setPeriod("quarterly")}
            className={`rounded-md px-2.5 py-1 text-xs font-medium transition-colors ${
              period === "quarterly" ? "bg-bg-hover text-text-primary" : "text-text-secondary"
            }`}
          >
            Quarterly
          </button>
        </div>
      </div>
      <div className="h-64">
        <ResponsiveContainer width="100%" height="100%">
          <BarChart data={data} margin={{ top: 5, right: 5, bottom: 0, left: 0 }}>
            <CartesianGrid strokeDasharray="3 3" stroke="var(--border-subtle)" vertical={false} />
            <XAxis dataKey="period" tick={{ fontSize: 11, fill: "var(--text-secondary)" }} axisLine={false} tickLine={false} />
            <YAxis tickFormatter={(v) => formatCompactNumber(v)} tick={{ fontSize: 11, fill: "var(--text-secondary)" }} axisLine={false} tickLine={false} width={48} />
            <Tooltip
              contentStyle={{ background: "var(--bg-elevated)", border: "1px solid var(--border-default)", borderRadius: 8, fontSize: 12 }}
              labelStyle={{ color: "var(--text-secondary)" }}
              formatter={(value) => [formatCompactNumber(Number(value)), METRICS.find((m) => m.key === metric)?.label ?? ""]}
            />
            <Bar dataKey={metric} fill="var(--color-brand-500)" radius={[4, 4, 0, 0]} isAnimationActive={false} />
          </BarChart>
        </ResponsiveContainer>
      </div>
    </div>
  );
}
