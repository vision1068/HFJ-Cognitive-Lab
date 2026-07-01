import { clsx } from "clsx";
import { scoreColor, scoreLabel } from "@/lib/signal";

export function ScoreGauge({ score, size = 140, label }: { score: number; size?: number; label?: string }) {
  const radius = size / 2 - 10;
  const circumference = 2 * Math.PI * radius;
  const offset = circumference * (1 - score / 100);
  const color = score >= 65 ? "var(--color-positive)" : score >= 50 ? "var(--color-neutral)" : score >= 35 ? "var(--color-warning)" : "var(--color-negative)";

  return (
    <div className="relative inline-flex flex-col items-center" style={{ width: size, height: size }}>
      <svg width={size} height={size} className="-rotate-90">
        <circle cx={size / 2} cy={size / 2} r={radius} stroke="var(--border-subtle)" strokeWidth={10} fill="none" />
        <circle
          cx={size / 2}
          cy={size / 2}
          r={radius}
          stroke={color}
          strokeWidth={10}
          fill="none"
          strokeDasharray={circumference}
          strokeDashoffset={offset}
          strokeLinecap="round"
          style={{ transition: "stroke-dashoffset 0.6s ease" }}
        />
      </svg>
      <div className="absolute inset-0 flex flex-col items-center justify-center">
        <span className="text-3xl font-bold text-text-primary">{score}</span>
        <span className={clsx("text-xs font-medium", scoreColor(score))}>{label ?? scoreLabel(score)}</span>
      </div>
    </div>
  );
}

export function ScoreBar({ label, score }: { label: string; score: number }) {
  return (
    <div>
      <div className="flex items-center justify-between mb-1.5">
        <span className="text-sm text-text-secondary">{label}</span>
        <span className={clsx("text-sm font-semibold", scoreColor(score))}>{score}</span>
      </div>
      <div className="h-1.5 rounded-full bg-bg-elevated overflow-hidden">
        <div
          className={clsx("h-full rounded-full", score >= 65 ? "bg-positive" : score >= 50 ? "bg-neutral" : score >= 35 ? "bg-warning" : "bg-negative")}
          style={{ width: `${score}%` }}
        />
      </div>
    </div>
  );
}
