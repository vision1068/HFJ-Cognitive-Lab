import type { Scorecard as ScorecardData, Verdict } from "@/services/predict/types";

// Verdict → semantic colour. This is a DIVERGING status scale (good → bad), not a
// brand accent: green poles for buy, amber neutral for hold, red poles for sell —
// deliberately kept clear of brand blue (#3b82f6) so the verdict never reads as
// "the app's colour". Hex is used inline because the fill width/colour is data-
// driven and Tailwind can't see a dynamic class at build time.
const VERDICT_COLOR: Record<Verdict, string> = {
  "Strong Buy": "#15803d", // green-700
  Buy: "#22c55e", // green-500 (positive token)
  Hold: "#f59e0b", // amber-500 (warning token)
  Sell: "#f97316", // orange-500
  "Strong Sell": "#ef4444", // red-500 (negative token)
  "Not Rated": "#6b7280", // gray-500 — neutral: no judgement, not enough scorable data
};

export function Scorecard({ scorecard }: { scorecard: ScorecardData }) {
  const { total100, verdict, categories, coverage } = scorecard;
  const color = VERDICT_COLOR[verdict];
  const clamped = Math.max(0, Math.min(100, total100));
  const scorable = categories.filter((c) => c.score10 !== null).length;

  return (
    <section className="card p-5" aria-label="Predict scorecard">
      {/* Headline: verdict + /100 */}
      <div className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <p className="text-xs uppercase tracking-wide text-text-secondary">Overall verdict</p>
          <p className="text-2xl font-bold tracking-tight" style={{ color }}>
            {verdict}
          </p>
        </div>
        <div className="text-right">
          <p className="text-xs text-text-secondary">Composite score</p>
          <p className="text-3xl font-bold text-text-primary tabular-nums">
            {total100.toFixed(0)}
            <span className="text-lg font-medium text-text-secondary"> / 100</span>
          </p>
        </div>
      </div>

      {/* Score meter — single-value gauge, verdict-coloured fill, thin, rounded end */}
      <div className="mt-4">
        <div
          className="relative h-3 w-full rounded-full bg-bg-elevated overflow-hidden"
          role="meter"
          aria-valuemin={0}
          aria-valuemax={100}
          aria-valuenow={clamped}
          aria-label={`Composite score ${total100.toFixed(0)} of 100, verdict ${verdict}`}
        >
          <div
            className="absolute inset-y-0 left-0 rounded-full"
            style={{ width: `${clamped}%`, backgroundColor: color }}
          />
        </div>
        <p className="mt-1.5 text-[11px] text-text-secondary">
          Based on {scorable} of {categories.length} weighted dimensions ·{" "}
          {Math.round(coverage * 100)}% coverage
        </p>
      </div>

      {/* Per-category /10 rows */}
      <div className="mt-5 space-y-2.5">
        {categories.map((cat) => (
          <CategoryRow key={cat.key} label={cat.label} score10={cat.score10} naReason={cat.naReason} />
        ))}
      </div>
    </section>
  );
}

function CategoryRow({
  label,
  score10,
  naReason,
}: {
  label: string;
  score10: number | null;
  naReason?: string;
}) {
  const isNa = score10 === null;
  const pct = isNa ? 0 : Math.max(0, Math.min(10, score10)) * 10;

  return (
    <div className="grid grid-cols-[minmax(0,1fr)_auto] items-center gap-x-3 gap-y-1">
      <span className="text-xs text-text-secondary truncate">{label}</span>
      {isNa ? (
        <span className="text-xs text-text-secondary italic" title={naReason}>
          n/a — no data
        </span>
      ) : (
        <div className="flex items-center gap-2 justify-self-end">
          <div className="h-1.5 w-24 rounded-full bg-bg-elevated overflow-hidden">
            <div className="h-full rounded-full bg-brand-500" style={{ width: `${pct}%` }} />
          </div>
          <span className="text-xs font-semibold text-text-primary tabular-nums w-12 text-right">
            {score10.toFixed(1)} / 10
          </span>
        </div>
      )}
    </div>
  );
}
