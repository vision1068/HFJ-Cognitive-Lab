import { Line, LineChart, ResponsiveContainer, YAxis } from "recharts";

export function Sparkline({ data, positive, height = 36, width = 100 }: { data: number[]; positive: boolean; height?: number; width?: number }) {
  const chartData = data.map((value, i) => ({ i, value }));
  const color = positive ? "var(--color-positive)" : "var(--color-negative)";
  return (
    <div style={{ width, height }}>
      <ResponsiveContainer width="100%" height="100%">
        <LineChart data={chartData} margin={{ top: 2, bottom: 2, left: 0, right: 0 }}>
          <YAxis domain={["dataMin", "dataMax"]} hide />
          <Line type="monotone" dataKey="value" stroke={color} strokeWidth={1.75} dot={false} isAnimationActive={false} />
        </LineChart>
      </ResponsiveContainer>
    </div>
  );
}
